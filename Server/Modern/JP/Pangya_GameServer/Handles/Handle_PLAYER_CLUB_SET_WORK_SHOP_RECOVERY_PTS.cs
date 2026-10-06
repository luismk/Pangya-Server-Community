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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_RECOVERY_PTS : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                uint item_typeid = Packet.ReadUInt32();
                int clubset_id = Packet.ReadInt32();

                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o item[TYPEID=" + (item_typeid) + "] para isso. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        150, 0x5300151));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem quantidade do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", Request=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        151, 0x5300152));
                }

                var pClub = Player.Inventory.FindWarehouseItemById(clubset_id);

                if (pClub == null)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        152, 0x5300153));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas nao tem esse ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        153, 0x5300154));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas esse ClubSet nao pode Recuperar o Recovery Pts. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        154, 0x5300155));
                }

                if (pClub.clubset_workshop.recovery_pts == 0)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas o ClubSet do Player ja foi recuperado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        156, 0x5300157));
                }

                // Corneta de recuperar recovery pts do ClubSet
                item = new stItemEx();

                item.type = 2;
                item.id = pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[RequestClubSetWorkShopRecoveryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas nao conseguiu remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        155, 0x5300156));
                }

                v_item.Add(new stItemEx(item));

                pClub.clubset_workshop.recovery_pts = 0;

                // ClubSet
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // UPDATE ON DB
                NormalManagerDB.Instance.add(12,
                     new CmdUpdateClubSetWorkshop(Player.UserInfo.UID,
                         pClub,
                         CmdUpdateClubSetWorkshop.FLAG.F_R_RECOVERY_PTS),
                     null, null);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[ClubSet WorkShop::RecoveryPts][Sucess] Normal [UID=" + Player.UserInfo.UID + "] recuperou os pontos do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON Jogo
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
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                Player.Send(p);

                // Resposta para o recovery ClubSet Pts
                p.init_plain(0x246);

                p.WriteUInt32(0); // OK

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A6);

                sys_achieve.finish_and_update(Player);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopRecoveryPts][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x246);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300150);

                Player.Send(p);
            }
        }
    }
}