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
    public class Handle_PLAYER_PLAYER_STATE_CHARACTER_LOUNGE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetRoom() ?? throw new exception("[Error] sala[NUMERO=" + Player.UserInfo.Member.RoomID + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));

                if (r.GetTipo() != RoomTypeFlags.LOUNGE)
                {
                    throw new exception("[Error] sala[NUMERO=" + Player.UserInfo.Member.RoomID + "] nao é um Lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                if (!Player.UserInfo.CharacterLoungeStates.TryGetValue(Player.Inventory.UserEquippedItem.CharacterEquiped.id, out StateCharacterLounge state))
                {
                    throw new exception("[Error] sala[NUMERO=" + Player.UserInfo.Member.RoomID + "] nao tem os estados do character na Lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0));
                }

                Packet p = new(0x196);
                p.WriteInt32(Player.ConnectionID);
                p.WriteBytes(state.ToArray());
                r.SendBroadCast(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_PLAYER_STATE_CHARACTER_LOUNGE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}