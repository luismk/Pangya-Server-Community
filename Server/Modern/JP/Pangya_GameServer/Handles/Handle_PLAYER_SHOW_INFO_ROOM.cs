using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SHOW_INFO_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {

                short sala_numero = Packet.ReadInt16();

                // aqui tem que passar o pacote86 com resposta que a sala não existe
                var r = GameServer.Instance.FindRoom(sala_numero) ?? throw new exception("[Handle_PLAYER_SHOW_INFO_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] pediu info da sala[NUMERO=" + (sala_numero) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                
                var ri = r.GetInfo();

                Packet p = new(0x86);
                p.WriteUInt32(ri.CurrentUsers);
                p.WriteByte(ri.HoleCount);
                p.WriteUInt32((ri.GetRoomType() == RoomTypeFlags.STROKE || ri.GetRoomType() == RoomTypeFlags.MATCH || ri.GetRoomType() == RoomTypeFlags.PANG_BATTLE) ? ri.TimeSec : ((ri.GetRoomType() == RoomTypeFlags.GUILD_BATTLE) ? 0 : ri.TimeMin));
                p.WriteByte((byte)ri.CourseIndex);
                p.WriteByte((byte)ri.GetRoomType());
                p.WriteByte(ri.HoleMode);
                p.WriteUInt32(ri.TrophyID);

                List<Player> vPlayer = r.GetSessions();
                PlayerLobbyInfo pci = null;

                for (var i = 0; i < vPlayer.Count; ++i)
                {
                    var _channel = vPlayer[i].GetChannel();

                    pci = _channel?.GetPlayerInfo(vPlayer[i]) ?? throw new exception("[Handle_PLAYER_SHOW_INFO_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] nao tem o info do Player na sala[NUMERO=" + (sala_numero) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            11, 0)); 

                    p.WriteInt32(pci?.OID ?? -1);
                    p.WriteByte(pci?.GameLevel ?? 0);
                    p.WriteByte(GetGameHole(vPlayer[i])); // se estiver jogando, aqui fica o número do hole
                    p.WriteInt32(pci?.Capability.Value ?? 0);
                    p.WriteUInt32(pci?.TitleSkin ?? 0);
                    p.WriteUInt32(pci?.LadderPoints ?? 0);
                }

                Player.Send(p);
            }
            catch (exception e)
            {
                Packet p = new(0x86);
                p.WriteUInt16(0);
                Player.Send(p);
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SHOW_INFO_ROOM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }

        public byte GetGameHole(Player Player)
        {
            var game = Player.GetGameRoom();
            if (game != null)
            {
                return (byte)game.getNumHole(Player);
            }
            return 255;
        }
    }
}