using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.PangyaEnums;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;

namespace Pangya_GameServer.Game.System
{
    public static class GameMasterSystem
    {
        public static void requestCommonCmdGM(this Player _session, packet _packet)
        {
            if (_session.m_gi.visible > 0)
            {
                _session.m_pi.mi.capability = new uCapability();
            }
            else
                _session.m_pi.mi.capability = _session.m_pi.m_cap;

            try
            {
                var cmd = (COMMON_CMD_GM)_packet.ReadInt16();

                // Verifica se o valor de cmd é válido no enum PacketIDClient
                if (Enum.IsDefined(typeof(COMMON_CMD_GM), cmd))
                    _smp.message_pool.getInstance().push($"[GameMasterSystem.requestCommonCmdGM][Log]: PLAYER[UID: {_session.m_pi.uid}, COMMAND: {Enum.GetName(typeof(COMMON_CMD_GM), cmd).Replace("CCG_", "")}]", type_msg.CL_FILE_LOG_AND_CONSOLE);


                switch (cmd)
                {
                    case COMMON_CMD_GM.CCG_VISIBLE:
                        {
                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][VISIBLE][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][VISIBLE][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /visible mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGVisible(_session, _packet);
                        }
                        break;
                    case COMMON_CMD_GM.CCG_WHISPER:
                        {
                            ushort whisper = _packet.ReadUInt16();

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][WHISPER][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            _session.m_gi.whisper = _session.m_pi.mi.state_flag.whisper = (byte)whisper;

                            // Se Whisper ON, Channel OFF
                            _session.m_gi.channel = _session.m_gi.whisper;
                        }
                        break;
                    case COMMON_CMD_GM.CCG_CHANNEL:
                        {
                            ushort channel = _packet.ReadUInt16();

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CHANNEL][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            _session.m_gi.channel = _session.m_pi.mi.state_flag.channel = (byte)channel;

                            // Se Channel ON, Whisper OFF
                            _session.m_gi.whisper = _session.m_gi.channel;
                        }
                        break;
                    case COMMON_CMD_GM.CCG_OPEN_WHISPER_PLAYER_LIST:
                        {
                            string nickname = _packet.ReadPStr();

                            if (nickname.empty())
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][OPEN_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou add um player a lista de Whisper, mas o nickname esta vazio. Hacker ou Bug",
                                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700108));

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][OPEN_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var s = (Player)sgs.gs.getInstance().FindSessionByNickname(nickname);

                            if (s == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][OPEN_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou add um player a lista de Whisper, mas nao encontrou o nickname no Server.",
                                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700109));

                            _session.m_gi.openPlayerWhisper(s.m_pi.uid);

                        }
                        break;
                    case COMMON_CMD_GM.CCG_CLOSE_WHISPER_PLAYER_LIST:
                        {
                            string nickname = _packet.ReadPStr();

                            if (nickname.empty())
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CLOSE_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou deletar um player da lista de Whisper, mas o nickname esta vazio. Hacker ou Bug",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700108));

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CLOSE_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var s = (Player)sgs.gs.getInstance().FindSessionByNickname(nickname);

                            if (s == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CLOSE_WHISPER_PLAYER_LIST][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou deletar um player da lista de Whisper, mas nao encontrou o nickname no Server.",
                                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700109));

                            _session.m_gi.closePlayerWhisper(s.m_pi.uid);
                        }
                        break;
                    case COMMON_CMD_GM.CCG_KICK:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][KICK][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][KICK][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /kick mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGKick(_session, _packet);
                        }
                        break;
                    case COMMON_CMD_GM.CCG_DISCONNECT:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }


                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][DISCONNECT][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var oid = _packet.ReadUInt32();

                            var s = sgs.gs.getInstance().FindSessionByOid(oid);

                            if (s == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][DISCONNECT][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou executar o comando /disconnect or /discon_uid mas nao encontrou o PLAYER[OID="
                                        + (oid) + "] do oid fornecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0));


                            // Disconnect Player
                            s.Disconnect();
                        }
                        break;
                    case COMMON_CMD_GM.CCG_DESTROY:
                        break;
                    case COMMON_CMD_GM.CCG_CHANGE_WIND_VERSUS:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }

                            // Troca o Vento só no modo versus
                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CHANGE_WIND_VERSUS][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CHANGE_WIND_VERSUS][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /wind mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGChangeWindVersus(_session, _packet);

                        }
                        break;
                    case COMMON_CMD_GM.CCG_CHANGE_WEATHER:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }


                            // Troca o tempo(Weather) da sala Lounge ou no Jogo(Game, Tourney, VS e etc)
                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CHANGE_WEATHER][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][CHANGE_WEATHER][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /weather mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGChangeWeather(_session, _packet);
                        }
                        break;
                    case COMMON_CMD_GM.CCG_IDENTITY:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }

                            // Troca a Capacidade do player
                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][IDENTITY][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][IDENTITY][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /identity mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGIdentity(_session, _packet);

                        }
                        break;
                    case COMMON_CMD_GM.CCG_GIVEITEM:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }

                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            if (_session.m_pi.m_cap.block_give_item_gm)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] esta bloqueado para enviar itens pelo comando giveitem.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700100));

                            uint oid_send = _packet.ReadUInt32();
                            uint item_typeid = _packet.ReadUInt32();
                            uint item_qntd = _packet.ReadUInt32();

                            var s = (Player)sgs.gs.getInstance().FindSessionByOid(oid_send);

                            if (s == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[OID="
                                        + (oid_send) + "] mas ele nao esta nesse server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x5700100));

                            if (item_typeid == 0)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] mas o Item[TYPEID=" + (item_typeid) + "QNTD = " + (item_qntd) + "] é invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

                            if (item_qntd > 20000u)//so coloquei pra testar o codigo
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD=" + (item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

                            var @base = sIff.getInstance().findCommomItem(item_typeid);

                            if (@base == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD=" + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                            if (@base.ID != item_typeid)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD=" + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                            stItem item = new stItem();
                            BuyItem bi = new BuyItem
                            {
                                id = -1,
                                _typeid = item_typeid,
                                qntd = item_qntd
                            };

                            ItemManager.initItemFromBuyItem(s.m_pi, item, bi, false, 0, 0, 1/*~nao Check Level*/);

                            if (item._typeid == 0)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][ErrorSystem] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD=" + (item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

                            var msg = "GM Send Gift: item[ " + @base.Name + " ]";

                            if (MailBoxManager.sendMessageWithItem(0, s.m_pi.uid, msg, item) <= 0)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GIVE_ITEM][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER[UID="
                                        + (s.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD=" + (item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));

                        }
                        break;
                    case COMMON_CMD_GM.CCG_GOLDENBELL:
                        {
                            CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                            snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, null, null);

                            if (cmd_cap.getException().getCodeError() != 0)
                            {
                                throw cmd_cap.getException();
                            }

                            if (!cmd_cap.IsValid())
                            {
                                throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                            }

                            // Envia item para todos da sala
                            if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GOLDENBELL][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] nao é GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                            if (_session.m_pi.m_cap.block_give_item_gm)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GOLDENBELL][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] esta bloqueado para enviar itens pelo comando goldenbell.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700100));

                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                            if (c == null)
                                throw new exception("[GameMasterSystem.requestCommonCmdGM][GOLDENBELL][Error] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] tentou executar o comando /goldenbell mas ele nao esta em nenhum canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                            c.requestExecCCGGoldenBell(_session, _packet);
                        }
                        break;
                    case COMMON_CMD_GM.CCG_WEBMATCHMAP:
                        break;
                    default:
                        throw new exception("[GameMasterSystem.requestCommonCmdGM][Error] Comando do GM Comum[value=" + (cmd) + "] nao implementado ou nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 0, 0x5700100));
                }
                EasyNoticeToClient(_session);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameMasterSystem.requestCommonCmdGM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                using (var p = new PangyaBinaryWriter(0x40))   // Msg to Chat of player
                {
                    p.WriteByte(7);  // Notice

                    p.WritePStr(_session.m_pi.nickname);

                    if (ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) == 9/*Não pode enviar item, foi bloqueado*/)
                        p.WritePStr("Nao pode executar esse comando, voce foi bloqueado pelo ADM.");
                    else
                        p.WritePStr("Nao conseguiu executar o comando.");

                    packet_func.session_send(p, _session);
                }
            }
        }

        static void EasyNoticeToClient(Player _session, string notice = "")
        {
            if (_session == null)
                return;

            // Cria o pacote de aviso com o tipo GST_NOTICE_TO_CLIENT
            using (var p = new PangyaBinaryWriter(0x40))   // Msg to Chat of player
            {
                p.WriteByte(7);  // Notice

                p.WritePStr(_session.m_pi.nickname);

                p.WritePStr(string.IsNullOrEmpty(notice) ? "Executed Command." : notice);

                packet_func.session_send(p, _session);
            }
        }
    }
}
