using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

public class Handle_PLAYER_COMET_REFILL : HandleBase<Player, Packet_EXAMPLE>
{
    public override async Task Handle()
    {
        Packet p = new Packet();
        try
        {
            uint item_typeid = Packet.ReadUInt32();
            uint ball_typeid = Packet.ReadUInt32();

            if (!sCometRefillSystem.Instance.isLoad()) sCometRefillSystem.Instance.load();

            var pBall = Player.Inventory.FindWarehouseItemByTypeid(ball_typeid);
            var pItem = Player.Inventory.FindWarehouseItemByTypeid(item_typeid);

            if (pBall == null || pItem == null || pItem.STDA_C_ITEM_QNTD < 1)
                throw new exception("Item ou bola faltando", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x5600101));

            var ctx = sCometRefillSystem.Instance.findCometRefill(pItem._typeid);
            if (ctx == null) throw new exception("Refill nao no sistema", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x5600100));

            var qntd = sCometRefillSystem.Instance.drawsCometRefill(ctx);

            stItem it_rm = new stItem { type = 2, id = (int)pItem.id, _typeid = pItem._typeid, qntd = 1, STDA_C_ITEM_QNTD = -1 };
            if (ItemManager.removeItem(it_rm, Player) <= 0) throw new exception("Erro remover", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x5600106));

            stItem it_add = new stItem { type = 2, id = (int)pBall.id, _typeid = pBall._typeid, qntd = (int)qntd, STDA_C_ITEM_QNTD = (short)qntd };
            if (ItemManager.addItem(it_add, Player, 0, 0) < 0) throw new exception("Erro add", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0x5600107));

            p.init_plain(0x197);
            p.WriteByte(1);
            p.WriteUInt32(pItem._typeid);
            p.WriteUInt32(pBall._typeid);
            p.WriteUInt16((ushort)pBall.STDA_C_ITEM_QNTD);
            Player.Send(p);
        }
        catch (exception e)
        {
            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_COMET_REFILL][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            p.init_plain(0x197);
            p.WriteByte(0);
            p.WriteZero(10);
            Player.Send(p);
        }
    }
}