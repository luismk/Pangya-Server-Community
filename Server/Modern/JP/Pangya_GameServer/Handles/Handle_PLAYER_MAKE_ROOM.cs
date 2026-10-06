using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
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
    public class Handle_PLAYER_MAKE_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

           var _channel = Player.GetChannel();
            try
            {
                // 1. Validação de tamanho mínimo do pacote (Prevenção de Buffer Overflow/Crash)
                if (Packet.Size < 20)
                {
                    throw new exception($"[Handle_PLAYER_MAKE_ROOM] Normal[UID= {Player.UserInfo.UID}, ID: {Player.UserInfo.Login} ] Packet size ({Packet.Size}) too small. Hacker attempt.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                int option;
                GameRoomInfoModel ri = new GameRoomInfoModel();
                string s_tmp = "";

                option = Packet.ReadByte();

                ri.TimeSec = Packet.ReadUInt32();
                ri.TimeMin = Packet.ReadUInt32();
                ri.MaxUsers = Packet.ReadByte();
                ri.RealRoomType = Packet.ReadByte();
                ri.HoleCount = Packet.ReadByte();
                ri.CourseIndex = (RoomCourseFlags)(Packet.ReadByte());

                // 3. Verificação de Course Válido
                if (!Enum.IsDefined(typeof(RoomCourseFlags), ri.CourseIndex))
                {
                    throw new exception($"[Handle_PLAYER_MAKE_ROOM] Normal[UID= {Player.UserInfo.UID}, ID: {Player.UserInfo.Login} ] Course ID {(int)ri.CourseIndex} inválido.",
                       ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                ri.HoleMode = Packet.ReadByte();
                if (!Enum.IsDefined(typeof(RoomHoleType), ri.HoleMode))
                {
                    throw new exception($"[Handle_PLAYER_MAKE_ROOM] Normal[UID= {Player.UserInfo.UID}, ID: {Player.UserInfo.Login} ] Modo ID {(int)ri.HoleMode} inválido.",
                       ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                var len = Packet.Size;

                bool practice = false; 
                ri.IDHoleRepeted = 0;
                ri.HoleFixed = 0;
                //hole repeted = 68, chip-in = 63
                if ((len == 52) && ri.RealRoomType == 19) //hole repeted tem NaturalMode
                {
                    Packet.ReadBytes(5);//seria esses dados abaixo...
                    ri.IDHoleRepeted = 1;
                    ri.HoleFixed = 7;
                    practice = true;
                }
                else if (len == 47 && ri.RealRoomType == 14) 
                    // Chip-in Practice, so pra passar true mesmo...
                {
                    practice = true;
                }

                if (!Player.UserInfo.UserCapabilities.IsGameMaster && ri.MaxUsers > 30)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] limite atingido, Hacker, por que o cliente nao deixa criar uma sala maior que 30, pois o cliente nao e gm/adm.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                ri.SpecialModeRoom.Value = Packet.ReadUInt32();

                // CHECK DE SEGURANÇA: 
                // Natural (Bit 0) + Short Game (Bit 1) = Valor máximo 3
                if (ri.SpecialModeRoom.Value > 3)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala com NaturalAndShortGame inválido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                      7, 0));
                }
                s_tmp = Packet.ReadString();

                if (s_tmp.Length == 0)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] Nome da sala vazio, Hacker, por que o cliente nao deixa enviar esse pacote sem um Name da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                if (s_tmp.Length > 32)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] Nome da sala muito longo, Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                if (practice)
                {
                    s_tmp = "Single Player Practice Mode";
                    if (ri.MaxUsers > 1)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + (_channel?.getId())
                            + "] Numero de jogadores errado, Hacker, por que o cliente nao deixa enviar esse pacote assim.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 7));
                    }
                }

                ri.Name = s_tmp;
                s_tmp = Packet.ReadString();

                if (s_tmp.Length > 8)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tamanho da Password esta errado, Code[0].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                if (practice)
                {
                    if (s_tmp.empty()) 
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + (_channel?.getId())
                            + "] Password da sala practice esta errada!.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                    }

                    if (s_tmp.Length < 8)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tamanho da Password esta errado, Code[2].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            7, 0));
                    }
                }

                if (!s_tmp.empty())
                {
                    ri.IsPublicRoom = 0;
                    ri.Password = s_tmp;
                }

                ri.ItemIDArtifact = Packet.ReadUInt32();

                // Check De Regras
                _channel?.CheckRoom(Player, ri);

                if (ri.SpecialModeRoom.IsShotMode && ri.GetRoomType() != RoomTypeFlags.TOURNEY
                    && ri.GetRoomType() != RoomTypeFlags.SPECIAL_SHUFFLE_COURSE
                    && ri.GetRoomType() != RoomTypeFlags.GRAND_PRIX)
                {
                    ri.SpecialModeRoom.IsShotMode = false;
                }

                if (_channel.getProperty().NaturalMode)
                {
                    ri.SpecialModeRoom.IsNaturalMode = true;
                }

                var flag = Player.UserInfo.BlockFlag.Flag;

                if (flag.AllGame && (ri.GetRoomType() != RoomTypeFlags.LOUNGE || flag.Lounge))
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar um sala, mas ele nao pode criar nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x780001));
                }

                switch (ri.GetRoomType())
                {
                    case RoomTypeFlags.STROKE:
                        if (flag.Stroke)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + (_channel?.getId()) + "] tentou criar sala[TIPO=" + (ri.GetRoomType()) + "], mas ele nao pode criar Stroke.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.MATCH:
                        if (flag.Match)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Match.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.TOURNEY:
                        if (flag.Tourney)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Tourney.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.TOURNEY_TEAM:
                        if (flag.TeamTourney)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Team Tourney.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 5, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GUILD_BATTLE:
                        if (flag.GuildBattle)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Guild Battle.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.PANG_BATTLE:
                        if (flag.PangBattle)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Pang Battle.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.APPROCH:
                        if (flag.Approach)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Approach.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.LOUNGE:
                        if (flag.Lounge)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 9, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GRAND_ZODIAC_INT:
                    case RoomTypeFlags.GRAND_ZODIAC_ADV:
                    case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                        if (flag.GrandZodiac)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Grand Zodiac.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.GRAND_PRIX:
                        if (flag.GrandPrix)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Grand Prix.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 11, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                        if (flag.SpecialShufflerCourse)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Special Shuffle Course.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 12, 0x770001));
                        }
                        break;
                    case RoomTypeFlags.PRACTICE:
                        if (flag.Practice)
                        {
                            throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas ele nao pode criar Practice.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 13, 0x770001));
                        }
                        break;
                }

                if (ri.SpecialModeRoom.IsShotMode && (flag.TeamTourney || flag.ShortGame))
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala Short Game, mas ele nao pode.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 770001));
                }

                if (ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE && ri.TimeMin != (30 * 60000))
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala[TIPO=" + ((ushort)ri.GetRoomType()) + "], mas o tempo é diferente do esperado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 780002));
                }

                if ((ri.GetRoomType() >= RoomTypeFlags.GRAND_ZODIAC_INT && ri.GetRoomType() <= RoomTypeFlags.GRAND_ZODIAC_ADV) && !Player.UserInfo.UserCapabilities.IsGameMaster)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala de Grand Zodiac Event sem ser GM.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 760001));
                }

                if (ri.GetRoomType() == RoomTypeFlags.GRAND_PRIX)
                {
                    throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala Grand Prix indevidamente.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x770001));
                }

                ri.IsChannelRookie = true;

                if (ri.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
                {
                    var pWi = Player.Inventory.FindWarehouseItemByTypeid(SPECIAL_SHUFFLE_COURSE_TICKET_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala Special Shuffle Course, mas ele nao tem o Ticket.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 9, 0));
                    }

                    if (pWi.STDA_C_ITEM_QNTD < 1)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] tentou criar a sala Special Shuffle Course sem tickets suficientes.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0));
                    }

                    stItem item = new stItem();
                    item.type = 2;
                    item.id = (int)pWi.id;
                    item._typeid = pWi._typeid;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                    if (ItemManager.removeItem(item, Player) <= 0)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] erro ao remover Ticket SSC.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 11, 0));
                    }

                    if (ri.SpecialModeRoom.IsShotMode)
                    {
                        ri.TimeMin = 20 * 60000;
                    }
                }

                Room? r = null;

                try
                {
                    _channel?.DeleteInviteTimeResquestByInvited(Player);

                    r = GameServer.Instance.MakeRoom(_channel, ri, Player);

                    if (r == null)
                    {
                        throw new exception("[Handle_PLAYER_MAKE_ROOM] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Channel[ID=" + _channel?.getId() + "] erro na criacao da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0));
                    }

                    _channel?.UpdatePlayerInfo(Player);

                    r.SendUpdateRoom();
                    r.SendMakeRoom(Player);
                    r.SendPlayerInfo(Player, 0);
                    r.SendPlayerStateLounge(Player);
                    r.SendWeatherLounge(Player);

                    _channel?.SendUpdateRoomInfo(r.GetInfo(), 1);

                    if (r.GetTipo() != RoomTypeFlags.PRACTICE && r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                    {
                        _channel?.SendUpdatePlayerInfo(Player, 3);
                    }

                    if (r.GetTipo() == RoomTypeFlags.GUILD_BATTLE)
                    {
                        r.SendPlayerInfo(Player, 0);
                    }

                    if (!r.IsWithBot() && !r.IsRoomGM() && (r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE))
                    {
                        try
                        {
                            if (r.IsLocked() && r.CheckPass("bot"))
                            {
                                r.MakeRoomBot(Player);
                            }
                        }
                        catch (exception e)
                        {
                            throw e;
                        }
                    }

                    if (r != null)
                    {
                        _channel?.Lobby.AddRoom(r);
                        _channel?.Lobby.UnlockRoom(r);  
                    }
                }
                catch (exception e)
                {
                    if (r != null)
                    {
                        _channel?.Lobby.UnlockRoom(r);
                    }
                    throw e;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_ROOM][ErrorSystem] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x49);
                p.WriteUInt16(2); // Error
                Player.Send(p);
            }
        }
    }
}