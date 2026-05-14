using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer
{
    public class PlayerManager : SessionManager
    {
        class uIndexOID
        {

            public byte ucFlag;
            public struct stFlag
            {
                public byte busy;
                public bool block;
            }
            public stFlag flag;

            public byte getFlag()
            { return ucFlag; }
        }

        SortedList<int, uIndexOID> m_indexes;       // Index de OID

        public PlayerManager()
        {
            m_indexes = new SortedList<int, uIndexOID>();
            for (int i = 0; i < m_max_session; i++)
            {
                var s = new Player()
                {
                    m_oid = -1
                };
                s.setState(false); // livre
                m_sessions.Add(s);
            }
        }

        public Player findPlayer(uint _uid, bool _oid = false)
        {
            foreach (var el in m_sessions)
            {
                // Inverti a lógica: Se for OID, compara OID. Se não, compara UID.
                uint idToCompare = _oid ? (uint)el.m_oid : el.getUID();

                if (idToCompare == _uid)
                {
                    return el as Player; // 'as' retorna null se falhar, evitando exceção
                }
            }
            return null;
        }

        public Player FindPlayer(uint uid, bool oid)
        {
            Player p = null;
            foreach (var el in m_sessions)
            {
                if (el.m_client != null && ((!oid) ? el.getUID() : (uint)el.m_oid) == uid)
                {
                    p = (Player)el;
                    break;
                }
            }

            return p;
        }


        public override Session FindSessionByOid(uint oid)
        {
            return base.FindSessionByOid(oid);
        }

        public override Session findSessionByUID(uint uid)
        {
            return base.findSessionByUID(uid);
        }

        public override List<Session> FindAllSessionByUid(uint uid)
        {
            return base.FindAllSessionByUid(uid);
        }

        public override Session FindSessionByNickname(string nickname)
        {
            return base.FindSessionByNickname(nickname);
        }
        // Override methods

        public override bool DeleteSession(Session _session)
        {
            if (_session == null) return false;

            lock (_lock) // Importante: Use um lock aqui também!
            {
                int tmp_oid = _session.m_oid;

                // Só processa se o OID for válido (maior que 0 no Pangya)
                if (tmp_oid != -1)
                {
                    if (_session.clear())
                    {

                        // 2. Libera o OID no seu sistema de gerenciamento de IDs
                        freeOID((uint)tmp_oid);

                        // 3. Reseta o OID da sessão para evitar reuso acidental
                        _session.m_oid = -1;

                        // 1. Fecha o Socket e limpa buffers de rede
                        _session.ClearConnection();

                        // 2. Reseta flags de estado
                        _session.setConnected(false);
                        _session.setState(false);

                        // 3. deleta da memoria
                        m_sessions[tmp_oid] = _session;
                        if (m_count > 0) m_count--;

                        return true;
                    }
                }
            }
            return false;
        }

        public void checkPlayersItens()
        {
            try
            {
                // Criamos uma cópia local para iterar sem travar a lista original por muito tempo
                List<Session> sessionsCopy = new List<Session>();

                lock (_lock)
                {
                    sessionsCopy = m_sessions.Where(player => player.m_oid != -1).ToList();
                }

                foreach (Player player in sessionsCopy)
                {
                    // O check de null e isCreated continua sendo importante
                    if (player != null)
                    {
                        lock (player.m_pi)
                        {
                            checkItemBuff(player);
                            checkCardSpecial(player);
                            checkCaddie(player);
                            checkMascot(player);
                            checkWarehouse(player);
                        }
                    }
                }
            }
            catch (Exception e) // Corrigi 'exception' para 'Exception'
            {
                _smp.message_pool.getInstance().push(new message("[PlayerManager::checkPlayersItens][ErrorSystem] " + e.ToString(), 0));
            }
        }

        public void blockOID(int _oid)
        {
            // Acesso direto por chave O(1) - Muito mais rápido que LINQ
            if (m_indexes.TryGetValue(_oid, out var indexInfo))
            {
                lock (indexInfo) // Garante que a flag não mude enquanto outra thread lê
                {
                    indexInfo.flag.block = true;
                }
            }
        }

        public void unblockOID(int _oid)
        {
            if (m_indexes.TryGetValue(_oid, out var indexInfo))
            {
                lock (indexInfo)
                {
                    // CORREÇÃO: Aqui deve ser false para desbloquear
                    indexInfo.flag.block = false;
                }
            }
        }

        public static void checkItemBuff(Player _session)
        {
            if (_session.getConnectTime() != 1) return;

            lock (_session.m_pi.v_ib)
            {

                // 1. Identifica quem deve ser removido primeiro (Snapshot para o Log)
                var expiredItems = _session.m_pi.v_ib.Where(it => UtilTime.GetLocalTimeDiffDESC(it.end_date) > 0).ToList();

                foreach (var it in expiredItems)
                {
                    _smp.message_pool.getInstance().push(new message("[Log] PLAYER[" + _session.m_pi.uid + "] Acabou Tempo Buff: " + it._typeid, 0));
                }

                // 2. Remove todos de uma vez de forma segura
                if (expiredItems.Count > 0)
                {
                    _session.m_pi.v_ib.RemoveAll(it => UtilTime.GetLocalTimeDiffDESC(it.end_date) > 0);
                }
            }
        }

        public static void checkCardSpecial(Player _session)
        {
            if (_session.getConnectTime() != 1)
                return;

            lock (_session.m_pi.v_cei)
            {
                // Remove todos os itens que atendem à condição de uma só vez
                _session.m_pi.v_cei.RemoveAll(it =>
                {
                    if (it.tipo == (uint)CARD_SUB_TYPE.T_SPECIAL && UtilTime.IsExpired(it.end_date))
                    { 
                        return true; // Retorna true para remover
                    }
                    return false;
                });
            }
        }

        public static void checkCaddie(Player _session)
        {
            if (_session.getConnectTime() != 1)
                return;//nao ta conectado
            // Caddie
            lock (_session.m_pi.mp_ci)
            {

                foreach (var el in _session.m_pi.mp_ci.Values)
                {
                    // Caddie por tempo
                    if (el.rent_flag == 2 && UtilTime.IsExpired(el.end_date))
                    {
                        lock (_session.m_pi.mp_ui)
                        {
                            // Put Update Item on vector update item of player
                            if ((_session.m_pi.findUpdateItemByTypeidAndType((uint)el.id, UpdateItem.UI_TYPE.CADDIE).Values) != null)
                            {
                                _session.m_pi.mp_ui.Add(new stIdentifyKey(el._typeid, el.id), new UpdateItem(UpdateItem.UI_TYPE.CADDIE, el._typeid, el.id));

                                // Verifica se o Caddie está equipado e desequipa
                                if ((_session.m_pi.ei.cad_info != null && _session.m_pi.ei.cad_info.id == el.id) || _session.m_pi.ue.caddie_id == el.id)
                                {

                                    _session.m_pi.ei.cad_info = null;
                                    _session.m_pi.ue.caddie_id = 0;
                                }
                            }

                        }
                    }

                    // Parts Caddie End Date
                    if (el.parts_typeid != 0 && !el.end_parts_date.IsEmpty && UtilTime.IsExpired(el.end_parts_date))
                    {

                        lock (_session.m_pi.mp_ui)
                        {
                            // Put Update Item on vector update item of player
                            if (_session.m_pi.findUpdateItemByTypeidAndType((uint)el.id, UpdateItem.UI_TYPE.CADDIE_PARTS) != null)
                            {

                                _session.m_pi.mp_ui.Add(new stIdentifyKey(el._typeid, el.id), new UpdateItem(UpdateItem.UI_TYPE.CADDIE_PARTS, el._typeid, el.id));

                                el.parts_typeid = 0;
                                el.parts_end_date_unix = 0;
                                el.end_parts_date = new SYSTEMTIME();

                                snmdb.NormalManagerDB.getInstance().add(1, new CmdUpdateCaddieInfo(_session.m_pi.uid, el), SQLDBResponse, null);
                            }
                        }
                    }
                }

            }
        }

        public static void checkMascot(Player _session)
        {
            if (_session.getConnectTime() != 1)
                return;//nao ta conectado
                       // Mascot
            lock (_session.m_pi.mp_mi)
            {
                foreach (var el in _session.m_pi.mp_mi.Values)
                {

                    // Mascot por Tempo
                    if (el.tipo == 1 && UtilTime.IsExpired(el.data))
                    {

                        // Put Update Item on vector update item of player
                        if (_session.m_pi.findUpdateItemByTypeidAndType((uint)el.id, UpdateItem.UI_TYPE.MASCOT).Count > 0)
                        {

                            lock (_session.m_pi.mp_ui)
                            {

                                _session.m_pi.mp_ui.insert(new stIdentifyKey(el._typeid, el.id), new UpdateItem(UpdateItem.UI_TYPE.MASCOT, el._typeid, el.id));

                                // Log
                                _smp.message_pool.getInstance().push(new message("[PlayerManager::checkMascot][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] Mascout[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                        + ", END_DATE=" + (el.data) + "] acabou o tempo dele, coloca no vector de update itens.", 0));

                                // Verifica se o Mascot está equipado e desequipa
                                if ((_session.m_pi.ei.mascot_info != null && _session.m_pi.ei.mascot_info.id == el.id) || _session.m_pi.ue.mascot_id == el.id)
                                {

                                    _session.m_pi.ei.mascot_info = null;
                                    _session.m_pi.ue.mascot_id = 0;

                                    _smp.message_pool.getInstance().push(new message("[PlayerManager::checkMascot][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] Desequipando Mascot[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                            + "].", 0));
                                }
                            }
                        }
                    }
                }

            }
        }

        public static void checkWarehouse(Player _session)
        {
            if (_session.getConnectTime() != 1)
                return;//nao ta conectado
            lock (_session.m_pi.mp_wi.Values)
            {
                foreach (var el in _session.m_pi.mp_wi.Values)
                { 
                    // Item Por tempo
                    if ((el.flag & (32 | 64 | 96)) != 0 && el.end_date_unix_local > 0)
                    {

                        var st = UtilTime.UnixToSystemTime(el.end_date_unix_local);

                        if (!UtilTime.IsExpired(st))
                        {

                            // Put Update Item on vector update item of player
                            if (_session.m_pi.findUpdateItemByTypeidAndType((uint)el.id, UpdateItem.UI_TYPE.WAREHOUSE).Count > 0)
                            {
                                lock (_session.m_pi.mp_ui)
                                {
                                    _session.m_pi.mp_ui.insert(new stIdentifyKey(el._typeid, el.id), new UpdateItem(UpdateItem.UI_TYPE.WAREHOUSE, el._typeid, el.id));

                                    // Log
                                    _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] Warehouse Item[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                            + ", END_DATE=" + UtilTime.UnixToSystemTime(el.end_date_unix_local) + "] acabou o tempo dele, coloca no vector de update itens.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Verifica se o item é um PART e se ele está equipado e deseequipa ele
                                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == IFF_GROUP.PART && _session.m_pi.isPartEquiped(el._typeid, el.id))
                                    {

                                        var ci = _session.m_pi.findCharacterByTypeid((uint)((Convert.ToUInt32(sIff.getInstance().CHARACTER << 26)) | sIff.getInstance().getItemCharIdentify(el._typeid)));

                                        if (ci != null)
                                        {

                                            var part = sIff.getInstance().findPart(el._typeid);

                                            if (part != null)
                                            {

                                                // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                                ci.unequipPart(part);

                                                _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] Desequipando Part[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                                        + "] do Character[TYPEID=" + (ci._typeid) + "], coloca parts default no lugar do part que estava equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            }
                                            else
                                            {

                                                for (var i = 0; i < 24; ++i)
                                                {

                                                    if (ci.parts_id[i] == el.id && ci.parts_typeid[i] == el._typeid)
                                                    {
                                                        ci.parts_typeid[i] = 0;
                                                        ci.parts_id[i] = 0;
                                                    }
                                                }

                                                _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Error] player[UID=" + (_session.m_pi.uid)
                                                        + "] nao tem o Part[TYPEID=" + (el._typeid) + "] do Character[TYPEID=" + (ci._typeid) + "], no IFF_STRUCT desequipa ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                            // Update no DB
                                            snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateCharacterAllPartEquiped(_session.m_pi.uid, ci), SQLDBResponse, null);

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Error][WARNING] player[UID=" + (_session.m_pi.uid)
                                                    + "] nao tem o Character[TYPEID=" + ((Convert.ToUInt32(sIff.getInstance().CHARACTER << 26)) | sIff.getInstance().getItemCharIdentify(el._typeid)) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    }

                                    // Verifica se é ClubSet e desequipa ele, e coloca o CV
                                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == IFF_GROUP.CLUBSET && _session.m_pi.ei.clubset != null && _session.m_pi.ei.clubset.id == el.id || _session.m_pi.ue.clubset_id == el.id)
                                    {

                                        var it = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                        if (it != null)
                                        {

                                            _session.m_pi.ei.clubset = it;
                                            _session.m_pi.ue.clubset_id = it.id;

                                            // Atualiza o ClubSet Enchant no Equiped Item do Player
                                            _session.m_pi.ei.csi.setValues(it.id, it._typeid, it.c);

                                            var cs = sIff.getInstance().findClubSet(it._typeid);

                                            if (cs != null)
                                                for (var i = 0; i < 5; ++i)
                                                    _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + it.clubset_workshop.c[i]);
                                        }

                                        _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] Desequipando ClubSet[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                                + "]" + (it != null ? ", e colocando o Air Knight Set[TYPEID=" + (it._typeid) + ", ID="
                                                + (it.id) + "] no lugar." : "."), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }

                                    // Verifica se é um Comet(Ball) e desequipa ele, e coloca a bola padrão
                                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == IFF_GROUP.BALL && _session.m_pi.ei.comet != null
                                            && _session.m_pi.ei.comet.id == el.id || _session.m_pi.ue.ball_typeid == el._typeid)
                                    {

                                        var it = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                                        if (it != null)
                                        {

                                            _session.m_pi.ei.comet = it;
                                            _session.m_pi.ue.ball_typeid = DEFAULT_COMET_TYPEID;
                                        }

                                        _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] Desequipando Ball[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                                + "]" + (it != null ? ", e colocando a Ball[TYPEID=" + (it._typeid) + ", ID="
                                                + (it.id) + "] padrao no lugar." : "."), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }

                                    // Verifica se é SKIN, para desequipar ele
                                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == IFF_GROUP.SKIN)
                                    {

                                        for (var i = 0; i < _session.m_pi.ue.skin_typeid.Length; ++i)
                                        {

                                            if (_session.m_pi.ue.skin_typeid[i] == el._typeid && _session.m_pi.ue.skin_id[i] == el.id)
                                            {

                                                _session.m_pi.ue.skin_id[i] = 0;
                                                _session.m_pi.ue.skin_typeid[i] = 0;

                                                _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] player[UID=" + (_session.m_pi.uid)
                                                        + "] Desequipando SKIN[TYPEID=" + (el._typeid) + ", ID=" + (el.id)
                                                        + ", SLOT=" + (i) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                                break;
                                            }
                                        }
                                    }

                                    // Verifica se é o Premium Ticket
                                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == IFF_GROUP.ITEM && sPremiumSystem.getInstance().isPremium(el._typeid))
                                    {

                                        // Log
                                        _smp.message_pool.getInstance().push(new message("[PlayerManager::checkWarehouse][Log] player[UID=" + (_session.m_pi.uid)
                                                + "] Tirando o Modo Premium User do Player, acabou o tempo do ticket, tirando a capacidade e a Comet(Ball)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        sPremiumSystem.getInstance().removePremiumUser(_session);
                                    }

                                }
                            }
                        }
                    }
                }

            }
        }

        private readonly object _sessionLock = new object();

        public override int findSessionFree()
        {
            // 1. Sincronização é obrigatória aqui para evitar que duas threads 
            // peguem o mesmo slot de sessão ao mesmo tempo.
            lock (_sessionLock)
            {
                for (int i = 0; i < m_sessions.Count; ++i)
                {
                    // No Pangya, -1 ou 0 costuma indicar sessão livre/desconectada
                    if (m_sessions[i].m_oid == -1)
                    {
                        // 2. Pegamos um OID disponível no gerenciador de IDs
                        int newOid = getNewOID();

                        // 3. Atribuímos à sessão para que ela não seja mais considerada "Free"
                        m_sessions[i].m_oid = newOid;

                        return i; // Retornamos o ÍNDICE da sessão no array/lista
                    }
                }
            }

            // Se chegou aqui, o servidor está lotado
            return int.MaxValue;
        }

        public int getNewOID()
        {
            lock (_lock)
            {
                // 1. Tenta encontrar um slot que já existia mas foi liberado (flag == 0)
                var it = m_indexes.FirstOrDefault(c => c.Value.ucFlag == 0);

                if (it.Value != null)
                {
                    it.Value.ucFlag = 1; // Marca como ocupado
                    return it.Key;
                }

                // 2. Se não houver slots livres, cria um novo ID baseado no tamanho atual
                int newOid = m_indexes.Count;
                m_indexes.Add(newOid, new uIndexOID() { ucFlag = 1 });

                return newOid;
            }
        }

        public void freeOID(uint _oid)
        {
            // 1. Tenta pegar o valor direto pela chave (Alta performance)
            if (m_indexes.TryGetValue((int)_oid, out var indexInfo))
            {
                // 2. Verifica se está bloqueado antes de liberar
                if (indexInfo.flag.block)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[PlayerManager::freeOID][WARNING] index[OID={_oid}] esta bloqueado, nao pode liberar ele agora", 0));
                    return; // Sai sem liberar o BUSY
                }

                // 3. Libera o slot para reuso (WAITING)
                indexInfo.flag.busy = 0;
            }
            else
            {
                // 4. OID não existe no mapa
                _smp.message_pool.getInstance().push(new message(
                    $"[PlayerManager::freeOID][WARNING] index[OID={_oid}] nao esta no mapa.", 0));
            }
        }

        public static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                // Static Functions of Class
                _smp.message_pool.getInstance().push(new message("[PlayerManager::SQLDBResponse]WARNING] _arg is null", 0));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[PlayerManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), 0));
                return;
            }

            //var pm = reinterpret_cast< PlayerManager* >(_arg);

            switch (_msg_id)
            {
                case 1: // Update Caddie Info
                    {
                        var cmd_uci = (CmdUpdateCaddieInfo)(_pangya_db);
                        break;
                    }
                case 2: // Update All parts of Character
                    {
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}