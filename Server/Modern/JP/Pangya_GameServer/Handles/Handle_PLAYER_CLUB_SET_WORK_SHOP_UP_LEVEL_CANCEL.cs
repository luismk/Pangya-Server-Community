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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL_CANCEL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                stItemEx item = new stItemEx();

                var pClub = Player.Inventory.FindWarehouseItemById(Player.Inventory.WorkshopLastUpLevel.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelCancel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou cancelar o up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        250, 0x5300251));
                }

                if (Player.Inventory.WorkshopLastUpLevel.stat > 4)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelCancel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou cancelar o up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas o stat é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        251, 0x5300252));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelCancel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou cancelar o up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas o ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        252, 0x5300253));
                }

                if (clubset.work_shop.total_recovery <= (uint)pClub.clubset_workshop.recovery_pts)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelCancel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou cancelar o up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas o ele nao pode mais cancelar ja gastou todos os seus pts de recovery[ClubSet_IFF_recovery=" + (clubset.work_shop.total_recovery) + ", ClubSet_recovery=" + (pClub.clubset_workshop.recovery_pts) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        253, 0x5300254));
                }

                // UPDATE ON SERVER
                pClub.clubset_workshop.c[Player.Inventory.WorkshopLastUpLevel.stat]--;
                pClub.clubset_workshop.recovery_pts++;

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

                // UPDATE ON DB
                NormalManagerDB.Instance.add(12,
                     new CmdUpdateClubSetWorkshop(Player.UserInfo.UID,
                         pClub,
                         CmdUpdateClubSetWorkshop.FLAG.F_UP_LEVEL_CANCEL),
                     null, null);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[ClubSetWorkshop::UpLevelCancel][Sucess] Normal [UID=" + Player.UserInfo.UID + "] cancelou o Up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);
                p.WriteBytes(item.stat.ToArray());
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                p.WriteZero(25);
                if (item.type == 0xCC)
                {
                    p.WriteBytes(item.clubset_workshop.ToArray());
                }

                Player.Send(p);

                // Resposta para o ClubSet Wrokshop Up Level Cancel
                p.init_plain(0x23F);

                p.WriteUInt32(0); // OK
                p.WriteInt32(Player.Inventory.WorkshopLastUpLevel.clubset_id);

                Player.Send(p);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopUpLevelCancel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23F);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300250);

                Player.Send(p);
            }
        }
    }
}