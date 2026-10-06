using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_RANK_TRANSFORM_CONFIRM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                var pClub = Player.Inventory.FindWarehouseItemById(Player.Inventory.WorkshopTransform.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        450, 0x5300451));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, mas nao existe o ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        451, 0x5300452));
                }

                var clubset_transform = sIff.Instance.findClubSet(Player.Inventory.WorkshopTransform.transform_typeid);

                if (clubset_transform == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, mas o ClubSet Special nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        452, 0x5300453));
                }

                // ClubSet que se Transformou
                item = new stItem();

                item.type = 2;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                // Delete ClubSet que vai ser transformado no ClubSet Special
                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, nao conseguiu deletar o ClubSet[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] que vai ser transformado no Special. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        453, 0x5300454));
                }

                v_item.Add(new stItem(item));

                // ClubSet Transformado
                item = new stItem();

                BuyItem bi = new BuyItem();

                bi.id = -1;
                bi._typeid = clubset_transform.ID;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(Player.UserInfo,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, nao conseguiu inicializar o ClubSet[TYPEID=" + (bi._typeid) + "]. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        454, 0x5300455));
                }

                var rt = RetAddItem.INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    Player, 0, 0)) < 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transformar ClubSet[ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (Player.Inventory.WorkshopTransform.transform_typeid) + "] Special, nao conseguiu adicionar o ClubSet[TYPEID=" + (item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        455, 0x5300456));
                }

                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_item.Add(new stItem(item));
                }

                // Log, // Usa o clubset._typeid e Player._Inventory.cwtc.clubset_id por que já excluiu esse ClubSet o "pClub"
                _smp.LogManager.Instance.push(new AppMessage("[ClubSetWokShop::UpRankTransformConfirm][Sucess] Normal [UID=" + Player.UserInfo.UID + "] confirmou a transformacao do ClubSet[TYPEID=" + (clubset.ID) + ", ID=" + (Player.Inventory.WorkshopTransform.clubset_id) + "] no ClubSet[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] Special", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                }

                Player.Send(p);

                // Resposta para o ClubSet Workshop Up Rank Transform Confirm
                p.init_plain(0x242);

                p.WriteUInt32(0); // OK;

                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A4u);

                sys_achieve.finish_and_update(Player);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopUpRankTransformConfirm][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x242);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300450);

                Player.Send(p);
            }
        }
    }
}