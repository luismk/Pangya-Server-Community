using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
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
    public class Handle_PLAYER_UPDATE_GACHA_COUPON : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                CmdCouponGacha cmd_cg = new CmdCouponGacha(Player.Inventory.uid); // Waiter

                NormalManagerDB.Instance.add(0, cmd_cg, null, null);

                if (cmd_cg.getException().getCodeError() != 0)
                {
                    throw cmd_cg.getException();
                }

                Player.Inventory.CouponGacha = cmd_cg.getCouponGacha();

                // Update no Warehouse Item
                byte find_ticket_and_sub = 0;

                foreach (var el in Player.Inventory.WarehouseItems)
                {
                    switch (el.Value._typeid)
                    {
                        case 0x1A000080: // Gacha Ticket
                            el.Value.STDA_C_ITEM_QNTD = (short)Player.Inventory.CouponGacha.normal_ticket;
                            find_ticket_and_sub = 1;
                            break;
                        case 0x1A000083: // Gacha Sub Ticket
                            el.Value.STDA_C_ITEM_QNTD = (short)Player.Inventory.CouponGacha.partial_ticket;
                            find_ticket_and_sub |= 2;
                            break;
                    }

                    if (find_ticket_and_sub == 3)
                    {
                        break;
                    }
                }

                Player.Send(Handle_PACKET_RESPONSE.pacote102(Player.UserInfo,Player.Inventory.CouponGacha));

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestUpdateGachaCoupon][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error envia o dizendo que deu erro no sistema
                p.init_plain(0x44);

                p.WriteByte(0xE2);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300600);

                Player.Send(p);
            }
        }
    }
}