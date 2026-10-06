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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL_CONFIRM : HandleBase<Player, Packet_EXAMPLE>
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
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou confirma o Up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        300, 0x5300301));
                }

                if (Player.Inventory.WorkshopLastUpLevel.stat > 4)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou confirma o Up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas o stat é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        302, 0x5300303));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevelConfirm][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou confirma o Up Level[stat=" + (Player.Inventory.WorkshopLastUpLevel.stat) + "] do ClubSet[ID=" + (Player.Inventory.WorkshopLastUpLevel.clubset_id) + "], mas nao existe esse ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        301, 0x5300302));
                }

                // UPDATE ON SERVER

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

                // Resposta para o ClubSet Wrokshop Up Level Confirm
                p.init_plain(0x23E);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(Player.Inventory.WorkshopLastUpLevel.stat);
                p.WriteInt32(Player.Inventory.WorkshopLastUpLevel.clubset_id);

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A2u);

                sys_achieve.finish_and_update(Player);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopUpLevelConfirm][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23E);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300300);

                Player.Send(p);
            }

        await Task.CompletedTask;
        }
    }
}