using System;
using System.Threading.Tasks;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Threading.Tasks;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ENTER_GAME_AFTER_STARTED : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            var m_ci = Player.GetChannel();
            try
            {
                byte option = Packet.ReadByte();

                if (option == 0 || option == 1)
                {
                    short sala_numero = Packet.ReadInt16();

                    if (sala_numero == -1)
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2700, 1));
                    }

                    var r = Player.GetRoom() ?? GameServer.Instance.FindRoom(sala_numero);

                    if (r == null)
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2700, 1));
                    }

                    if (r.GetTipo() != RoomTypeFlags.TOURNEY)
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "] ja em jogo, mas o Type da sala nao é Tourney. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            15, 0x770001));
                    }

                    if (r.IsLocked())
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala é privada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2710, 1));
                    }

                    if (!(r.CurrentGame != null))
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala nao esta em jogo ainda. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2701, 1));
                    }

                    if (r.IsFull())
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala ja esta no seu limite de jogadores.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2702, 1));
                    }

                    if (option == 0)
                    {
                       r.SendTimeGame(Player);
                    }
                    else if (option == 1)
                    {
                        try
                        {
                            m_ci.DeleteInviteTimeResquestByInvited(Player);

                            if (r.EnterGameAfterStarted(Player))
                            {
                                m_ci.SendUpdateRoomInfo(r.GetInfo(), 3);
                                m_ci.UpdatePlayerInfo(Player);

                                if (r.GetTipo() != RoomTypeFlags.PRACTICE && r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                                {
                                    m_ci.SendUpdatePlayerInfo(Player, 3);
                                }
                            }
                        }
                        catch (exception e)
                        {
                            throw;
                        }
                    }
                }
                else if (option == 2)
                {
                    EnterAfterStartInfo easi = new EnterAfterStartInfo();

                    for (int i = 0; i < 18; i++)
                        easi.tacada[i] = Packet.ReadByte();

                    for (int i = 0; i < 18; i++)
                        easi.score[i] = Packet.ReadInt32();

                    for (int i = 0; i < 18; i++)
                        easi.pang[i] = Packet.ReadUInt64();

                    easi.request_oid = Packet.ReadInt32();
                    easi.owner_oid = Packet.ReadUInt32();

                    var r = Player.GetRoom() ?? throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "] ja em jogo, mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2700, 1));

                    if (!(r.CurrentGame != null))
                    {
                        throw new exception("[[ERROR] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "] ja em jogo, mas a sala nao esta em jogo ainda. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2701, 1));
                    }

                   r.CurrentGame?.RequestUpdateEnterAfterStartedInfo(Player, easi);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ENTER_GAME_AFTER_STARTED][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x113);
                p.WriteByte(6);
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1));

                Player.Send(p);
            }
        }
    }
}