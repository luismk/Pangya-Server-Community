using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;

using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public static class UCCSystem
    {
        public static void HandleUCCLoad(Player _session)
        {
            var p = new PangyaBinaryWriter();

            try
            {
                List<WarehouseItemEx> all_ucc = new List<WarehouseItemEx>();

                foreach (var part in _session.m_pi.mp_wi)
                {
                    if (part.Value.IsUCC())
                    {
                        var ucc_part = sIff.getInstance().findPart(part.Value._typeid);

                        if (ucc_part != null &&
                            (ucc_part.type_item == PART_TYPE.UCC_DRAW_ONLY || ucc_part.type_item == PART_TYPE.UCC_COPY_ONLY))
                        {
                            all_ucc.Add(part.Value);
                        }
                    }
                }
                if (all_ucc.Count > 0)
                {
                    UCC_Load_Ctx[] ucc_ctxs = new UCC_Load_Ctx[all_ucc.Count];

                    for (int i = 0; i < all_ucc.Count; i++)
                    {
                        var item = all_ucc[i];
                        ucc_ctxs[i]._typeid = item._typeid;
                        ucc_ctxs[i].id = item.id;
                        ucc_ctxs[i].ucc_idx = item.ucc.idx?.PadRight(8, '\0').Substring(0, 8) ?? "\0";
                    }

                    byte[] rawData = Tools.StructArrayToByteArray(ucc_ctxs);
                     
                    byte[] tmp = new byte[rawData.Length + 10];//nao vi nada de diferente, somente 34
                    uint compress_out = (uint)rawData.Length;//ele pega o tamamnho real, e compressiona so ate nessa parte do tamanho
                    try
                    { 
                        MiniLzo.compress_data(rawData, compress_out, tmp, (uint)tmp.Length);//aqui ele compressionou, pegou o temp e compressionou
                         
                        p.init_plain(0x1B1);
                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                        p.WriteUInt32(compress_out);
                        p.WriteBytes(rawData); 
                        packet_func.session_send(p, _session, 1);
                    }
                    catch (Exception)
                    {
                        // log do erro, se desejar
                    }
                }
                //padrao
                else
                {
                    p.init_plain(0x1B1);
                    p.WriteUInt64(0x190132DC55);
                    p.WriteUInt64(0x2211000000);
                    p.WriteZeroByte(13);
                    p.WriteUInt32(0x1100);

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch
            {
                p.init_plain(0x1B1);
                p.WriteUInt64(0x190132DC55);
                p.WriteUInt64(0x2211000000);
                p.WriteZeroByte(13);
                p.WriteUInt32(0x1100);

                packet_func.session_send(p, _session, 1);
            }
        }
        public static void HandleUCC(this Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();
            try
            {

                byte opt = _packet.ReadUInt8();

                // Verifica se session está varrizada para executar esse ação, 
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                //CHECK_SESSION_IS_AUTHORIZED("UCCSystem");

                switch (opt)
                {
                    case 0: // Salva para sempre[definitivo]
                        {
                            uint ucc_typeid = _packet.ReadUInt32();
                            string ucc_idx = _packet.ReadString();
                            string ucc_name = _packet.ReadString();

                            // INICIO CHECK UCC VALID FOR SERVER
                            if (sIff.getInstance().getItemGroupIdentify(ucc_typeid) != IFF_GROUP.PART)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o UCC nao é um part valido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 13, 0x5200113));

                            var part = sIff.getInstance().findPart(ucc_typeid);

                            if (part == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas nao tem a UCC no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 11, 0x5200111));

                            if (!part.IsUCC())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas nao é uma UCC valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 12, 0x5200112));
                            // FIM CHECK UCC VALID FOR SERVER

                            if (ucc_typeid == 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o typeid is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5200104));

                            if (ucc_idx.empty())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o idx é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0x5200106));

                            if (ucc_name.empty())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar definitivo a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o name é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0x5200107));

                            // Save definitivo UCC

                            // UPDATE ON SERVER
                            var it = _session.m_pi.mp_wi.FirstOrDefault(el => el.Value._typeid == ucc_typeid &&
                           (string.IsNullOrEmpty(el.Value.ucc.name) || el.Value.ucc.name == "0") &&
                           el.Value.ucc.idx == ucc_idx);

                            if (it.Value == null)
                                throw new Exception($"[GameServer.requestUCCSystem][Error] PLAYER[UID={_session.m_pi.uid}] tentou salvar definitivo a UCC[TYPEID={ucc_typeid}, IDX={ucc_idx}], mas ele não tem essa UCC. Hacker ou Bug");

                            // TEMPORARY 2, FOREVER 1
                            it.Value.ucc.status = 1; // Definitivo
                            it.Value.ucc.name = ucc_name;
                            it.Value.ucc.copier_nick = _session.m_pi.nickname;
                            it.Value.ucc.copier = _session.m_pi.uid;

                            // Date
                            DateTime si = DateTime.Now;

                            // UPDATE ON DB
                            var cmd_uu = new CmdUpdateUCC(_session.m_pi.uid, it.Value, new SYSTEMTIME(1), CmdUpdateUCC.T_UPDATE.FOREVER);   // Waiter

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_uu, null, null);

                            if (cmd_uu.getException().getCodeError() != 0)
                                throw cmd_uu.getException();

                            // Log
                            _smp.message_pool.getInstance().push(new message("[UCC::Self Design System][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] salvo definitivo a UCC[TYPEID="
                                + (it.Value._typeid) + ", ID=" + (it.Value.id) + ", IDX=" + (it.Value.ucc.idx) + ", NAME=" + (it.Value.ucc.name) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0x12E);

                            p.WriteByte(opt);

                            p.WriteByte(1);    // no outro fala que � op��o de erro, mas n�o sei n�o

                            p.WriteInt32(it.Value.id);
                            p.WriteUInt32(it.Value._typeid);
                            p.WritePStr(it.Value.ucc.idx);
                            p.WritePStr(it.Value.ucc.name);
                            packet_func.session_send(p, _session);
                            break;
                        }
                    case 1: // Info
                        {
                            var ucc_id = _packet.ReadInt32();
                            var owner = _packet.ReadUInt8();  // acho que 1 � do pr�prio player, 0 de outro player

                            if (ucc_id <= 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou ver info da UCC[ID="
                                        + (ucc_id) + "], mas o id da ucc é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 2, 0x5200102));

                            var pWi = _session.m_pi.findWarehouseItemById(ucc_id);

                            // N�o achou o UCC no Player, tenta no DB para ver se � de outro player
                            // Por Hora envia uma Exception
                            if (pWi == null)
                            {

                                var cmd_fu = new CmdFindUCC(ucc_id);    // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0, cmd_fu, null, null);

                                if (cmd_fu.getException().getCodeError() != 0)
                                    throw cmd_fu.getException();

                                if (cmd_fu.getInfo().id <= 0)
                                    throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou ver info da UCC[ID="
                                            + (ucc_id) + "], mas nao encontrou essa UCC. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5200103));

                                pWi = cmd_fu.getInfo();
                            }

                            // UPDATE ON GAME
                            p.init_plain(0x12E);

                            p.WriteByte(opt);

                            p.WriteUInt32(pWi._typeid);
                            p.WritePStr(pWi.ucc.idx);
                            p.WriteByte(owner);

                            p.WriteBytes(pWi.ToArray());
                            packet_func.session_send(p, _session);

                            break;
                        }
                    case 2: // C�piar
                        {
                            uint ucc_typeid = _packet.ReadUInt32();
                            string ucc_idx = _packet.ReadString();
                            ushort seq = _packet.ReadUInt16();
                            int cpy_id = _packet.ReadInt32();

                            // INICIO CHECK UCC VALID FOR SERVER
                            if (sIff.getInstance().getItemGroupIdentify(ucc_typeid) != IFF_GROUP.PART)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o UCC nao é um part valido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 13, 0x5200113));

                            var part = sIff.getInstance().findPart(ucc_typeid);

                            if (part == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas nao tem a UCC no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 11, 0x5200111));

                            if (!part.IsUCC())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas nao é uma UCC valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 12, 0x5200112));
                            // FIM CHECK UCC VALID FOR SERVER

                            if (ucc_typeid == 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o typeid is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5200104));

                            if (ucc_idx.empty())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o idx é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0x5200106));

                            if (seq == 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas seq[value=" + (seq) + "] is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5200108));

                            if (cpy_id <= 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o copy_id is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5200109));

                            var pWi = _session.m_pi.findWarehouseItemById(cpy_id);

                            if (pWi == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o ele nao tem a UCC_CPY, Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 10, 0x5200110));

                            // INICIO CHECK UCC VALID FOR SERVER
                            if (sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.PART)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas o UCC nao é um part valido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 13, 0x5200113));

                            part = sIff.getInstance().findPart(pWi._typeid);

                            if (part == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas nao tem a UCC no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 11, 0x5200111));

                            if (!part.IsUCC())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "] para UCC_CPY[ID=" + (cpy_id) + "], mas nao é uma UCC valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 12, 0x5200112));
                            // FIM CHECK UCC VALID FOR SERVER

                            // Copiar UCC
 
                            if (!_session.m_pi.mp_wi.Any(el => el.Value._typeid == ucc_typeid))
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou copiar a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas ele nao tem essa UCC. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0x5200105));

                            var it = _session.m_pi.mp_wi.FirstOrDefault(el => el.Value._typeid == ucc_typeid);

                            // Copia permanente
                            pWi.ucc.status = 1;
                            pWi.ucc.idx = it.Value.ucc.idx;
                            pWi.ucc.name = it.Value.ucc.name;
                            pWi.ucc.copier_nick = _session.m_pi.nickname;
                            pWi.ucc.copier = _session.m_pi.uid;

                            // Date
                            SYSTEMTIME draw_dt = new SYSTEMTIME();

                            draw_dt.CreateTime();

                            // UPDATE ON DB
                            var cmd_uu = new CmdUpdateUCC(_session.m_pi.uid, pWi, draw_dt, CmdUpdateUCC.T_UPDATE.COPY);   // Waiter

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_uu, null, null);

                            if (cmd_uu.getException().getCodeError() != 0)
                                throw cmd_uu.getException();

                            pWi = cmd_uu.getInfo();

                            // Log
                            _smp.message_pool.getInstance().push(new message("[UCC::Self Design System][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] fez um copia da UCC[TYPEID="
                                    + (it.Value._typeid) + ", ID=" + (it.Value.id) + ", IDX=" + (it.Value.ucc.idx) + "] na UCC_CPY[TYPEID="
                                    + (pWi._typeid) + ", ID=" + (pWi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0x12E);

                            p.WriteByte(opt);

                            p.WriteUInt32(it.Value._typeid);
                            p.WritePStr(it.Value.ucc.idx);
                            p.WriteUInt16(it.Value.ucc.seq);

                            p.WriteInt32(pWi.id);
                            p.WriteInt32(pWi.id);
                            p.WriteUInt32(pWi._typeid);
                            p.WritePStr(pWi.ucc.idx);
                            p.WriteUInt16(pWi.ucc.seq);

                            p.WriteByte(1);    // no outro fala que � op��o de erro, mas n�o sei n�o
                            packet_func.session_send(p, _session);
                            break;
                        }
                    case 3: // Salve tempor�rio
                        {
                            uint ucc_typeid = _packet.ReadUInt32();
                            string ucc_idx = _packet.ReadString();

                            // INICIO CHECK UCC VALID FOR SERVER
                            if (sIff.getInstance().getItemGroupIdentify(ucc_typeid) != IFF_GROUP.PART)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o UCC nao é um part valido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 13, 0x5200113));

                            var part = sIff.getInstance().findPart(ucc_typeid);

                            if (part == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas nao tem a UCC no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 11, 0x5200111));

                            if (!part.IsUCC())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                    + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas nao é uma UCC valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 12, 0x5200112));
                            // FIM CHECK UCC VALID FOR SERVER

                            if (ucc_typeid == 0)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o typeid is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5200104));

                            if (ucc_idx.empty())
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas o idx é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0x5200106));

                            // Save tempor�rio UCC

                            // UPDATE ON SERVER
                            var it = _session.m_pi.mp_wi.FirstOrDefault(el => el.Value._typeid == ucc_typeid);

                            if (it.Value == null)
                                throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou salvar temporario a UCC[TYPEID="
                                        + (ucc_typeid) + ", IDX=" + ucc_idx + "], mas ele nao tem essa UCC. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0x5200105));

                            // TEMPORARY 2, FOREVER 1
                            it.Value.ucc.status = 2;       // Tempor�rio
                            it.Value.ucc.name = "0";
                            // UPDATE ON DB
                            var cmd_uu = new CmdUpdateUCC(_session.m_pi.uid, it.Value, new SYSTEMTIME(1), CmdUpdateUCC.T_UPDATE.TEMPORARY);   // Waiter

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_uu, null, null);

                            if (cmd_uu.getException().getCodeError() != 0)
                                throw cmd_uu.getException();

                            // Log
                            _smp.message_pool.getInstance().push(new message("[UCC::Self Design System][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] salvo temporario a UCC[TYPEID="
                                    + (it.Value._typeid) + ", ID=" + (it.Value.id) + ", IDX=" + (it.Value.ucc.idx) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0x12E);

                            p.WriteByte(opt);

                            p.WriteUInt32(it.Value._typeid);
                            p.WritePStr(it.Value.ucc.idx);
                            p.WriteByte(1);    // no outro fala que � op��o de erro, mas n�o sei n�o

                            packet_func.session_send(p, _session);

                            break;
                        }
                    default:
                        throw new exception("[GameServer.requestUCCSystem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou usar UCC System, mas forneceu uma option desconhecida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5200101));
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameServer.requestUCCSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x12E);

                p.WriteSByte(-1);    // Error

                packet_func.session_send(p, _session);
            }
        }
    }
}
