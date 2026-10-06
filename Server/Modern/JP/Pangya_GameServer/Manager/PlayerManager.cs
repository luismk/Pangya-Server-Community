using Pangya_GameServer.Feature;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network.Flags;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer
{
    public class PlayerManager : AppSessionManager<Player>
    { 
        public PlayerManager(int maxUser) : base(maxUser)
        {
            _lock = new object();
        }

        public new Player Get(int id)
        {
            return base.Get(id) as Player;
        }

        public Player FindByUID(uint uid)
        {
            return GetAllSessions()
                .OfType<Player>()
                .FirstOrDefault(p => p.Inventory?.uid == uid);
        }

        public Player FindByNickname(string nickname)
        {
            return GetAllSessions()
                .OfType<Player>()
                .FirstOrDefault(p => p.UserInfo?.Login == nickname);
        }

        public bool IsAlreadyLoggedIn(uint uid)
        {
            return GetAllSessions()
                .OfType<Player>()
                .Any(p => p.Connected && p.Inventory?.uid == uid);
        }
          
        public Player FindPlayer(uint uid, bool oid)
        {
            Player p = null;
            foreach (var el in this._sessions.Values)
            {
                if (el.Connected && ((!oid) ? el.GetUID() : (uint)el.ConnectionID) == uid)
                {
                    p = el;
                    break;
                }
            }

            return p;
        }

        public void CheckPlayersItens() // Mudou para async Task
        {
            try
            {
                List<Player> sessionsCopy;

                // 1. Snapshot rápido das sessões para não segurar o lock do Manager
                lock (_lock)
                {
                    sessionsCopy = _sessions.Values
                        .Where(player => player != null && player.Connected && player.ConnectionID != -1)
                        .ToList();
                }

                foreach (Player player in sessionsCopy)
                {
                    // Verificação de segurança adicional
                    if (player.Inventory == null) continue;

                    // Rodamos as verificações de memória (são rápidas e síncronas agora)
                    CheckItemBuff(player);
                    CheckCardSpecial(player);
                    CheckCaddie(player);
                    CheckMascot(player);
                    CheckWarehouse(player);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PlayerManager::CheckPlayersItens][ErrorSystem] " + e.ToString(), 0));
            }
        }


        private readonly object _sessionLock = new object();
        private object _lock;

        public static void CheckItemBuff(Player _session) // Removido async void, não há await
        {
            // Corrigido: Se NÃO estiver conectado ou _Inventory nulo, ignora.
            if (!_session.Connected || _session.Inventory == null)
                return;

            try
            {
                lock (_session.Inventory.ItemBuffs)
                {
                    // 1. Identifica e Loga (Usando interpolação para melhor performance no .NET 10)
                    // Filtramos uma vez para o log
                    var expiredItems = _session.Inventory.ItemBuffs
                        .Where(it => UtilTime.GetLocalTimeDiffDESC(it.end_date) > 0)
                        .ToList();

                    if (expiredItems.Count > 0)
                    {
                        foreach (var it in expiredItems)
                        {
                            _smp.LogManager.Instance.push(new AppMessage(
                                $"[Buff::Expired] Normal[{_session.Inventory.uid}] Buff TypeID: {it._typeid} expirou.",
                                type_msg.CL_ONLY_FILE_LOG));
                        }

                        // 2. Remove todos de uma vez (Predicado deve ser idêntico ao do Where)
                        _session.Inventory.ItemBuffs.RemoveAll(it => UtilTime.GetLocalTimeDiffDESC(it.end_date) > 0);

                        // Dica: Se o buff alterar Pang ou Exp Rate, você pode disparar 
                        // um recálculo de buffs da sessão aqui após o lock.
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[CheckItemBuff][Error] Normal[{_session.Inventory.uid}] Erro: {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void CheckCardSpecial(Player _session) // Removido async void
        {
            // Lógica correta: se NÃO estiver conectado, não processa (evita NullReference)
            if (!_session.Connected || _session.Inventory == null)
                return;

            try
            {
                lock (_session.Inventory.CardEquipment)
                {
                    // O RemoveAll é excelente pois é atômico dentro do lock
                    int removedCount = _session.Inventory.CardEquipment.RemoveAll(it =>
                        it.tipo == (uint)CARD_SUB_TYPE.T_SPECIAL && UtilTime.IsExpired(it.end_date));

                    if (removedCount > 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[CheckCardSpecial][Log] Normal[UID={_session.Inventory.uid}] {removedCount} Card(s) Especial(ais) expirado(s) e removido(s).",
                            type_msg.CL_ONLY_FILE_LOG));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[CheckCardSpecial][Error] Normal[UID={_session.Inventory.uid}] Erro: {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void CheckCaddie(Player _session)
        {
            // Corrigido: Se NÃO estiver conectado, sai.
            if (!_session.Connected || _session.Inventory == null)
                return;

            try
            {
                var caddiesToUpdate = new List<CaddieInfoEx>();

                lock (_session.Inventory.Caddies)
                {
                    foreach (var el in _session.Inventory.Caddies.Values)
                    {
                        bool changed = false;

                        // 1. Verificação de Caddie Expirado (Aluguel)
                        if (el.rent_flag == 2 && UtilTime.IsExpired(el.end_date))
                        {
                            lock (_session.Inventory.UpdateItems)
                            {
                                if (_session.Inventory.FindUpdateItemById(el.id) == null)
                                {
                                    _session.Inventory.UpdateItems.Add(el.id,
                                        new UpdateItem(UpdateItem.UI_TYPE.CADDIE, el._typeid, el.id));

                                    // Desequipa se necessário
                                    if ((_session.Inventory.UserEquippedItem.CaddieEquiped != null && _session.Inventory.UserEquippedItem.CaddieEquiped.id == el.id) ||
                                        _session.Inventory.UserEquipment.caddie_id == el.id)
                                    {
                                        _session.Inventory.UserEquippedItem.CaddieEquiped = null;
                                        _session.Inventory.UserEquipment.caddie_id = 0;
                                    }
                                }
                            }
                        }

                        // 2. Verificação de Partes (Roupas) do Caddie Expiradas
                        if (el.parts_typeid != 0 && !el.end_parts_date.IsEmpty && UtilTime.IsExpired(el.end_parts_date))
                        {
                            lock (_session.Inventory.UpdateItems)
                            {
                                if (_session.Inventory.FindUpdateItemById(el.id) == null)
                                { 
                                    _session.Inventory.UpdateItems.Add(el.id, new UpdateItem(UpdateItem.UI_TYPE.CADDIE_PARTS, el._typeid, el.id));
                                }
                            }

                            // Reseta os dados da parte
                            el.parts_typeid = 0;
                            el.parts_end_date_unix = 0;
                            el.end_parts_date = new SystemTime();

                            changed = true;
                        }

                        if (changed)
                        {
                            caddiesToUpdate.Add(el);
                        }
                    }
                }

                foreach (var cad in caddiesToUpdate)
                {
                    NormalManagerDB.Instance.add(1,
                        new CmdUpdateCaddieInfo(_session.Inventory.uid, cad), SQLDBResponse, null);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[CheckCaddie][Error] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void CheckMascot(Player _session)
        {
            // 1. Correção: Se NÃO estiver conectado, sai (evita NullReference)
            if (!_session.Connected || _session.Inventory == null)
                return;

            try
            {
                bool mascotChanged = false;

                // 2. Trava o dicionário de Mascotes
                lock (_session.Inventory.Mascots)
                {
                    foreach (var el in _session.Inventory.Mascots.Values)
                    {
                        // Mascot por Tempo (Type 1) e Expirado
                        if (el.tipo == 1 && UtilTime.IsExpired(el.data))
                        {
                            lock (_session.Inventory.UpdateItems)
                            {
                                // Verifica se já não está na fila de update para não duplicar a chave
                                if (_session.Inventory.FindUpdateItemById(el.id) == null)
                                {
                                    _session.Inventory.UpdateItems.Add(el.id, new UpdateItem(UpdateItem.UI_TYPE.MASCOT, el._typeid, el.id));

                                    // Log de Expiração
                                    _smp.LogManager.Instance.push(new AppMessage(
                                        $"[PlayerManager::CheckMascot][Log] Normal[UID={_session.Inventory.uid}] Mascote[TYPEID={el._typeid}, ID={el.id}] expirou em {el.data}.",
                                        type_msg.CL_ONLY_FILE_LOG));

                                    // 3. Verifica se o Mascot está equipado e desequipa na memória
                                    if ((_session.Inventory.UserEquippedItem.MascotEquiped != null && _session.Inventory.UserEquippedItem.MascotEquiped.id == el.id) ||
                                        _session.Inventory.UserEquipment.mascot_id == el.id)
                                    {
                                        _session.Inventory.UserEquippedItem.MascotEquiped = null;
                                        _session.Inventory.UserEquipment.mascot_id = 0;
                                        mascotChanged = true;

                                        _smp.LogManager.Instance.push(new AppMessage(
                                            $"[PlayerManager::CheckMascot][Log] Normal[UID={_session.Inventory.uid}] Desequipando Mascote expirado [ID={el.id}].",
                                            type_msg.CL_ONLY_FILE_LOG));
                                    }
                                }
                            }
                        }
                    }
                }

                // 4. Se houve mudança crítica (desequipar), você pode disparar um pacote de atualização aqui
                if (mascotChanged)
                {
                    // Exemplo: Enviar pacote 0x4B ou similar para atualizar o status do player no cliente
                    // _session.SendMascotStatusUpdate(); 
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[CheckMascot][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void CheckWarehouse(Player _session)
        {
            // 1. Correção: Se NÃO estiver conectado, sai.
            if (!_session.Connected || _session.Inventory == null)
                return;

            try
            {
                // Lista para salvar personagens que precisam de update no DB (fora do lock)
                var charactersToUpdate = new List<CharacterInfo>();

                lock (_session.Inventory.WarehouseItems) // Lock no dicionário, não nos Values
                {
                    foreach (var el in _session.Inventory.WarehouseItems.Values)
                    {
                        // Verifica se o item é temporário e se EXPIROU
                        if ((el.flag & (0x20 | 0x40 | 0x60)) != 0 && el.end_date_unix_local > 0)
                        {
                            var st = UtilTime.UnixToSystemTime(el.end_date_unix_local);

                            // CORREÇÃO: Removido o '!' para processar apenas se ESTIVER expirado
                            if (UtilTime.IsExpired(st))
                            {
                                lock (_session.Inventory.UpdateItems)
                                {
                                    // Verifica se já não está na fila de update
                                    if (_session.Inventory.FindUpdateItemById(el.id) == null)
                                    {
                                        _session.Inventory.UpdateItems.Add(el.id, new UpdateItem(UpdateItem.UI_TYPE.WAREHOUSE, el._typeid, el.id));

                                        LogExpiringItem(_session, el);

                                        // --- LÓGICA DE DESEQUIPAR ---

                                        // 1. PART (Roupas/Acessórios)
                                        if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.PART && _session.Inventory.isPartEquiped(el._typeid, el.id))
                                        {
                                            var ci = _session.Inventory.FindCharacterByTypeid((uint)((Convert.ToUInt32(sIff.Instance.CHARACTER << 26)) | sIff.Instance.getItemCharIdentify(el._typeid)));// Sugestão de método mais direto
                                            if (ci != null)
                                            {
                                                var part = sIff.Instance.findPart(el._typeid);
                                                if (part != null) ci.unequipPart(part);
                                                else ManualUnequip(ci, el); // Fallback manual se não achar no IFF

                                                if (!charactersToUpdate.Contains(ci)) charactersToUpdate.Add(ci);
                                            }
                                        }

                                        // 2. CLUBSET (Tacos)
                                        if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.CLUBSET &&
                                           (_session.Inventory.UserEquippedItem.Club_WI?.id == el.id || _session.Inventory.UserEquipment.clubset_id == el.id))
                                        {
                                            EquipDefaultClubSet(_session);
                                        }

                                        // 3. BALL (Comet)
                                        if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.BALL &&
                                           (_session.Inventory.UserEquippedItem.Ball_WI?.id == el.id || _session.Inventory.UserEquipment.ball_typeid == el._typeid))
                                        {
                                            EquipDefaultBall(_session);
                                        }

                                        // 4. SKIN
                                        HandleSkinExpiration(_session, el);

                                        // 5. PREMIUM TICKET
                                        if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.ITEM && sPremiumSystem.Instance.isPremium(el._typeid))
                                        {
                                            sPremiumSystem.Instance.removePremiumUser(_session);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var ci in charactersToUpdate)
                {
                    NormalManagerDB.Instance.add(2, new CmdUpdateCharacterAllPartEquiped(_session.Inventory.uid, ci), SQLDBResponse, null);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[CheckWarehouse][Error] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Métodos auxiliares para manter o código limpo (Clean Code)
        private static void LogExpiringItem(Player _session, WarehouseItem el)
        {
            _smp.LogManager.Instance.push(new AppMessage($"[Warehouse::Expired] Normal[UID={_session.Inventory.uid}] Item[TYPEID={el._typeid}] expirou.", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private static void ManualUnequip(CharacterInfo ci, WarehouseItem el)
        {
            for (int i = 0; i < 24; i++)
            {
                if (ci.parts_id[i] == el.id)
                {
                    ci.parts_id[i] = 0;
                    ci.parts_typeid[i] = 0;
                }
            }
        }

        private static void EquipDefaultClubSet(Player _session)
        {
            // Busca o Air Knight no warehouse do player
            var it = _session.Inventory.FindWarehouseItemByTypeid(DEFAULT_CLUB_TYPEID);
            if (it != null)
            {
                _session.Inventory.UserEquippedItem.Club_WI = it;
                _session.Inventory.UserEquipment.clubset_id = it.id;
                _session.Inventory.UserEquippedItem.ClubEquiped.setValues(it.id, it._typeid, it.c);

                var cs = sIff.Instance.findClubSet(it._typeid);
                if (cs != null)
                {
                    for (var i = 0; i < 5; ++i)
                        _session.Inventory.UserEquippedItem.ClubEquiped.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + it.clubset_workshop.c[i]);
                }
            }
        }

        private static void EquipDefaultBall(Player _session)
        {
            var it = _session.Inventory.FindWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);
            if (it != null)
            {
                _session.Inventory.UserEquippedItem.Ball_WI = it;
                _session.Inventory.UserEquipment.ball_typeid = DEFAULT_COMET_TYPEID;
            }
        }

        private static void HandleSkinExpiration(Player _session, WarehouseItem el)
        {
            if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.SKIN)
            {
                for (var i = 0; i < _session.Inventory.UserEquipment.skin_typeid.Length; ++i)
                {
                    if (_session.Inventory.UserEquipment.skin_typeid[i] == el._typeid && _session.Inventory.UserEquipment.skin_id[i] == el.id)
                    {
                        _session.Inventory.UserEquipment.skin_id[i] = 0;
                        _session.Inventory.UserEquipment.skin_typeid[i] = 0;
                        break;
                    }
                }
            }
        }


        public static async void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                // Static Functions of Class
                _smp.LogManager.Instance.push(new AppMessage("[PlayerManager::SQLDBResponse]WARNING] _arg is null", 0));
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PlayerManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), 0));
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