using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.Generic;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_JOIN_ROOM : HandleBase<Player, Packet_EXAMPLE>
    { 
        public override async Task Handle()
        {
            Packet p = new Packet();
            try
            {
                short sala_numero = Packet.ReadInt16();
                string senha = Packet.ReadString();

                var r = GameServer.Instance.FindRoom(sala_numero);

                if (r == null)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas ela nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0));
                }

                // ServerFlag Server
                var flag = Player.UserInfo.BlockFlag.Flag;

                // Player não pode criar sala, exceto Lounge, se ele não estiver bloqueado
                if (flag.AllGame && (r.GetTipo() != RoomTypeFlags.LOUNGE || flag.Lounge))
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar um sala[NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x780001));
                }

                switch (r.GetTipo())
                {
                    case RoomTypeFlags.STROKE:
                        if (flag.Stroke)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Stroke. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                2, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.MATCH:
                        if (flag.Match)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Match. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.TOURNEY:
                        if (flag.Tourney)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                4, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.TOURNEY_TEAM:
                        if (flag.TeamTourney)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Team Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                5, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GUILD_BATTLE:
                        if (flag.GuildBattle)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Guild Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                6, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.PANG_BATTLE:
                        if (flag.PangBattle)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Pang Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                7, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.APPROCH:
                        if (flag.Approach)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Approach. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                8, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.LOUNGE:
                        if (flag.Lounge)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Lounge. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                9, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GRAND_ZODIAC_INT:
                    case RoomTypeFlags.GRAND_ZODIAC_ADV:
                    case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                        if (flag.GrandZodiac)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Grand Zodiac. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                10, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GRAND_PRIX:
                        if (flag.GrandPrix)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                11, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                        if (flag.SpecialShufflerCourse)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Special Shuffle Course. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                12, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.PRACTICE:
                        if (flag.Practice)
                        {
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar Practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                13, 0x770001));
                        }
                        break;
                }

                if (r.GetInfo().SpecialModeRoom.IsShotMode && (flag.TeamTourney || flag.ShortGame))
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas ele nao pode entrar sala Short Game. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 770001));
                }

                if (r.GetTipo() == RoomTypeFlags.GRAND_PRIX)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[TIPO=" + r.GetInfo() + ", NUMERO=" + r.GetRoomId() + "], mas nao pode entrar na sala Grand Prix com esse pacote. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        15, 0x770001));
                }

                if (r.GameRun() && Player.UserInfo.UserCapabilities.IsGameMaster) // GM Entra na sala depois que o jogo começou
                {
                    r.SendTimeGame(Player);
                }
                else if (r.CurrentGame != null) // não é GM envia error para o Player que ele nao pode entrar na sala depois de ter começado
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a sala ja comecou o jogo. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }
                else
                {
                    if (!r.IsLocked() || r.IsInvited(Player) || (Player.UserInfo.UserCapabilities.IsGameMaster) || (!senha.empty() && r.CheckPass(senha)))
                    {
                        if (r.IsInvited(Player))
                        {
                            // Deleta convite

                            // Add Invite a sala
                            if (!r.IsFull() && r.GetInvited(Player) != null)
                            {
                                var ici = r.DeleteInvited(Player);

                                r.EnterToRoom(Player);

                                Player.GetChannel().DeleteInviteTimeRequest(ici);
                            }
                        }
                        else if (!r.IsFull())
                        {
                            // Verifica se o Player foi Invite em outra sala
                            // e tira o convite dele
                            Player.GetChannel().DeleteInviteTimeResquestByInvited(Player);

                            r.EnterToRoom(Player);
                        }
                        else
                        {
                            throw new exception("[Handle_PLAYER_JOIN_ROOM][Warning] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a sala esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0));
                        }
                    }
                    else
                    {
                        throw new exception("[Handle_PLAYER_JOIN_ROOM][Warning] Normal[UID=" + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a Password nao é igual a da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            4, 0));
                    }

                    // Att PlayerCanalInfo
                    Player.GetChannel().UpdatePlayerInfo(Player);

                    r.SendUpdateRoom();

                    r.SendMakeRoom(Player);

                    r.SendPlayerInfo(Player, 0); //zero e a lista

                    r.SendPlayerInfo(Player, 1); //1 e o criador

                    r.SendPlayerStateLounge(Player);

                    r.SendWeatherLounge(Player);

                    Player.GetChannel().SendUpdateRoomInfo(r.GetInfo(), 3);

                    if (r.GetTipo() != RoomTypeFlags.PRACTICE && r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                    {
                        Player.GetChannel().SendUpdatePlayerInfo(Player, 3);
                    }

                    // Guild Battle precisa enviar o sendCharacter opção 0 duas vezes.
                    // Uma na sua posição Normal e outra depois de atualizar o info da sala na lobby
                    if (r.GetTipo() == RoomTypeFlags.GUILD_BATTLE)
                    {
                        r.SendPlayerInfo(Player, 0);
                    }
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_JOIN_ROOM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x49);

                p.WriteByte(1); // Error

                Player.Send(p);
            }

            await Task.CompletedTask;
        }
    }
}