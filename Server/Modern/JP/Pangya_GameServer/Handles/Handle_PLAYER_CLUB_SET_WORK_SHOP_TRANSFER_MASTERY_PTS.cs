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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_TRANSFER_MASTERY_PTS : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 300 mastery pts transfere por cada UCIM chip
                ClubSetWorkShopTransferMasteryPts tmp = new ClubSetWorkShopTransferMasteryPts().ToRead(Packet);

                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                var pUCIM_chip = Player.Inventory.FindWarehouseItemByTypeid(tmp.UCIM_chip_typeid);

                if (pUCIM_chip == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele nao tem UCIM Chip[TYPEID=" + (tmp.UCIM_chip_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        103, 0x5300104));
                }

                if (pUCIM_chip.STDA_C_ITEM_QNTD < (short)tmp.qntd)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele nao tem quantidade suficiente de UCIM Chip[TYPEID=" + (tmp.UCIM_chip_typeid) + ", QNTD=" + (pUCIM_chip.STDA_C_ITEM_QNTD) + ", Request=" + (tmp.qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        104, 0x5300105));
                }

                var pClub_src = Player.Inventory.FindWarehouseItemById(tmp.clubset[0]);

                if (pClub_src == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] mas o Player nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        100, 0x5300101));
                }

                var pClub_dst = Player.Inventory.FindWarehouseItemById(tmp.clubset[1]);

                if (pClub_dst == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts para o ClubSet[ID=" + (tmp.clubset[1]) + "] mas o Player nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        100, 0x5300101));
                }

                if (sIff.Instance.findClubSet(pClub_src._typeid) == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts do ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] mas o clubset nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        101, 0x5300102));
                }

                var clubset = sIff.Instance.findClubSet(pClub_dst._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] mas o clubset nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        101, 0x5300102));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "] mas ele nao pode receber mastery de outros ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        102, 0x5300103));
                }

                if (pClub_dst.clubset_workshop.calcRank(clubset.SlotStats.getSlot) == 5)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "] mas o ClubSet é Rank S nao pode transferir Mastery Pts mais para ele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        107, 0x5300108));
                }

                if ((tmp.qntd * 300) > (uint)pClub_src.clubset_workshop.mastery && (uint)((pClub_src.clubset_workshop.mastery % 300 == 0) ? pClub_src.clubset_workshop.mastery / 300 : pClub_src.clubset_workshop.mastery / 300 + 1) > tmp.qntd)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele tentou usar UCIM chip mais que o necessario. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        105, 0x5300106));
                }

                uint mastery = ((tmp.qntd * 300) > (uint)pClub_src.clubset_workshop.mastery) ? (uint)pClub_src.clubset_workshop.mastery : (uint)tmp.qntd * 300;

                // Transferi os Mastery Points
                pClub_dst.clubset_workshop.mastery += mastery;
                pClub_src.clubset_workshop.mastery -= mastery;

                // UCIM Chip
                item = new stItemEx();

                item.type = 2;
                item.id = (int)pUCIM_chip.id;
                item._typeid = pUCIM_chip._typeid;
                item.qntd = (int)tmp.qntd;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopTransferMasteryPts][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] mas nao conseguiu", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        106, 0x5300107));
                }

                v_item.Add(new stItemEx(item));

                // ClubSet Font
                item = new stItemEx
                {
                    type = 0xCC,
                    id = (int)pClub_src.id,
                    _typeid = pClub_src._typeid
                };
                item.clubset_workshop.c = pClub_src.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub_src.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub_src.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub_src.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub_src.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // ClubSet Destino
                item = new stItemEx
                {
                    type = 0xCC,
                    id = (int)pClub_dst.id,
                    _typeid = pClub_dst._typeid
                };
                item.clubset_workshop.c = pClub_dst.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub_dst.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub_dst.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub_dst.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub_dst.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // Atualiza ON DB
                NormalManagerDB.Instance.add(12,
                     new CmdUpdateClubSetWorkshop(Player.UserInfo.UID,
                         pClub_src,
                         CmdUpdateClubSetWorkshop.FLAG.F_TRANSFER_MASTERY_PTS),
                    null, null); 

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[ClubSet Workshop::TransferMasteryPts][Sucess] Normal [UID=" + Player.UserInfo.UID + "] transferiu mastery pts[value=" + (mastery) + "] do ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Atualiza ON Jogo
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
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : (int)el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                Player.Send(p);

                // Resposta do transfer Mastery Pts
                p.init_plain(0x245);

                p.WriteUInt32(0); // OK

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A5u);

                sys_achieve.finish_and_update(Player);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopTransferMasteryPts][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x245);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300100);

                Player.Send(p);
            }
        } 
    }
}