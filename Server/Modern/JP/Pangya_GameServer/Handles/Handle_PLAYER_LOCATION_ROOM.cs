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
    public class Handle_PLAYER_SYNC_ACTION_GAME : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetRoom();

                if (r == null)
                    throw new exception("[Handle_PLAYER_SYNC_ACTION_GAME][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar localizacao na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                           10, 0));

                TPLAYER_ACTION type = (TPLAYER_ACTION)Packet.ReadByte();

                var p = new Packet(0xC4); 
                p.WriteInt32(Player.ConnectionID);
                p.WriteByte(type); 
                switch (type)
                {
                    case TPLAYER_ACTION.PLAYER_ACTION_ROTATION: // R - Face
                        {
                            Player.UserInfo.CurrentLocation.r = Packet.ReadFloat();//W

                            p.WriteFloat(Player.UserInfo.CurrentLocation.r); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOTION_ROOM: // Motion In Room
                        {
                            Player.UserInfo.ChatSpecial = Packet.Message; 
                             
                            p.WriteBytes(Player.UserInfo.ChatSpecial); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_LOUNGER_LOC: // X Z R, coordenada inicial do Player no Lounge
                        {
                            var location_add = new PlayerRoomInfo.PlayerRoomLocationInfo().ToRead(Packet);

                            Player.UserInfo.CurrentLocation.x += location_add.x;
                            Player.UserInfo.CurrentLocation.z += location_add.z;
                            Player.UserInfo.CurrentLocation.y += location_add.y;  
                            p.WriteBytes(location_add.ToArray()); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_LOUNGER_STATE: // Estado do Player na sala, se o Player esta sentado, deitado ou em pé
                        {
                            Player.UserInfo.PostureRoom = Packet.ReadUInt32(); 
                            p.WriteUInt32(Player.UserInfo.PostureRoom);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOVE: // Player está andando no Lounge, X, Z, R
                        {
                            var location_add = new PlayerRoomInfo.PlayerRoomLocationInfo().ToRead(Packet);

                            Player.UserInfo.CurrentLocation.x += location_add.x;
                            Player.UserInfo.CurrentLocation.z += location_add.z;
                            Player.UserInfo.CurrentLocation.y += location_add.y; 
                            p.WriteBytes(location_add.ToArray()); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOTION_LOUNGER: // Motion no Lounge
                        {
                            Player.UserInfo.ChatSpecial = Packet.Message; 
                            p.WriteBytes(Player.UserInfo.ChatSpecial); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_ACK_PLAYER: // Estado do Player de icon no Lounge
                        {
                            Player.UserInfo.LoungeState = Packet.ReadUInt32(); 
                            p.WriteUInt32(Player.UserInfo.LoungeState); 
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ANIMATION_WITH_EFFECTS: // Motion no Lounge de item especial
                        {
                            Player.UserInfo.ChatSpecial = Packet.Message; 
                            p.WriteBytes(Player.UserInfo.ChatSpecial); 
                            break;
                        }
                    default:
                        throw new exception("[Handle_PLAYER_PLAYER_LOCATION_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar localizacao na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas o type desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            11, 0));
                } 
                r.UpdatePlayerInfo(Player);
                r.SendBroadCast(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_PLAYER_LOCATION_ROOM][ErrorSystem] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}