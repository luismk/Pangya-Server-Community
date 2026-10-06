using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ENTER_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var m_ci = Player.GetChannel();
            try
            {
                if (Player.UserInfo.BlockFlag.Flag.BuyShopAndGift)
                {
                    throw new exception("[Lobby::RequestEnterShop][Error] Normal [UID=" + Player.UserInfo.UID
                            + "] tentou jogar no Papel Shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x790002));
                }

                var p = new Packet(0x20E); 
                p.WriteZero(8); 
                Player.Send(p);
            }
            catch (exception e)
            {
                throw;
            }

        await Task.CompletedTask;
        }
    }
}