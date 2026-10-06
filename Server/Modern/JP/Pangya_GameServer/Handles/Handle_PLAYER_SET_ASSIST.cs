using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SET_ASSIST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            if (Packet == null)
            {
                throw new exception("[Handle_PLAYER_SET_ASSIST] [Error] Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            Packet p = new();

            try
            {
                if (Player.GetGameRoom() == null)
                {

                    var (rt, item) = Player.UserInfo.AssistFlag ? RemoveAssistItem(Player) : AddAssistItem(Player);
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH && item != null)
                    {
                        p.init_plain(0x216);
                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                        p.WriteUInt32(1);
                        p.WriteByte(item.type);
                        p.WriteUInt32(item._typeid);
                        p.WriteInt32(item.id);
                        p.WriteUInt32(item.flag_time);
                        p.WriteBytes(item.stat.ToArray());
                        p.WriteInt32(item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                        p.WriteZero(25);
                        Player.Send(p);
                    }

                    p.init_plain(0x26A);
                    p.WriteUInt32(0);
                    p.WriteUInt32(ASSIST_ITEM_TYPEID);
                    p.WriteUInt32(Player.UserInfo.UID);
                    Player.Send(p);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SET_ASSIST][ErrorSystem] é hacker de packet: " + Player.UserInfo.UID, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    p.init_plain(0x16A);
                    p.WriteUInt32(0);
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SET_ASSIST][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                p.init_plain(0x26A);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200800);
                Player.Send(p);
            }
        }

        private (int rt, stItem? item) AddAssistItem(Player Player)
        {
            if (Player.Inventory.ItemExist(ASSIST_ITEM_TYPEID))
            {
                stItem item = new stItem();
                var rt = RetAddItem.INIT_VALUE;
                item.type = 2;
                item.id = -1;
                item._typeid = ASSIST_ITEM_TYPEID;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = 1;
                if ((rt = ItemManager.addItem(item, Player, 0, 0)) < 0)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou ativar o Assist[TYPEID=" + Convert.ToString(ASSIST_ITEM_TYPEID) + "], mas nao conseguiu adicionar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200801));
                }

                Player.UserInfo.AssistFlag = true;
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SET_ASSIST][Info] Normal[UID=" + Player.UserInfo.UID + "] Ligou o Assist Modo", type_msg.CL_FILE_LOG_AND_CONSOLE));

                CommandDB.LoadUpdateAssist(Player.UserInfo.UID, Player.UserInfo.AssistFlag);

                return (rt, item);
            }
            return (-1, null);

        }

        private (int rt, stItem? item) RemoveAssistItem(Player Player)
        {
            var pWi = Player.Inventory.FindWarehouseItemByTypeid(ASSIST_ITEM_TYPEID);

            if (pWi != null)
            {
                stItem item = new();
                var rt = RetAddItem.INIT_VALUE;
                item.type = 2;
                item._typeid = ASSIST_ITEM_TYPEID;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = 1;
                Player.UserInfo.AssistFlag = false;
                item.id = pWi.id;
                item.qntd = (int)((pWi.STDA_C_ITEM_QNTD <= 0) ? 1 : pWi.STDA_C_ITEM_QNTD);
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);
                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou desativar o Assist[TYPEID=" + Convert.ToString(ASSIST_ITEM_TYPEID) + "], mas nao conseguiu remover o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2, 0x5200802));
                }
                CommandDB.LoadUpdateAssist(Player.UserInfo.UID, Player.UserInfo.AssistFlag);
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SET_ASSIST][Info] Normal[UID=" + Player.UserInfo.UID + "] Desligou o Assist Modo", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return (rt, item);
            }
            return (-1, null);
        }
    }
}