using Pangya_GameServer.Engine;
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
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                CWUpLevel cwul = new CWUpLevel().ToRead(Packet);
                stItem item = new stItem();
                ProbCardExtra pce = new ProbCardExtra();

                uint stat = 0;
                switch (sIff.Instance.getItemGroupIdentify(cwul.item_typeid))
                {
                    case IFF_GROUP.ITEM:
                        {
                            var pWi = Player.Inventory.FindWarehouseItemByTypeid(cwul.item_typeid);

                            if (pWi == null)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem o item[TYPEID=" + (cwul.item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    201, 0x5300202));
                            }

                            if (pWi.STDA_C_ITEM_QNTD < (short)cwul.qntd)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem quantidade suficiente do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", Request=" + (cwul.qntd) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    202, 0x5300203));
                            }

                            if (sIff.Instance.findItem(pWi._typeid) == null)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o Item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    203, 0x5300204));
                            }

                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = pWi._typeid;
                            item.qntd = cwul.qntd;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            break;
                        }
                    case IFF_GROUP.CARD:
                        {
                            var pCi = Player.Inventory.FindCardByTypeid(cwul.item_typeid);

                            if (pCi == null)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem o item[TYPEID=" + (cwul.item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    201, 0x5300202));
                            }

                            if (pCi.qntd < (short)cwul.qntd)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem quantidade suficiente do Card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", QNTD=" + (pCi.qntd) + ", Request=" + (cwul.qntd) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    202, 0x5300203));
                            }

                            if (sIff.Instance.findCard(pCi._typeid) == null)
                            {
                                throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o Card nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    203, 0x5300204));
                            }

                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pCi.id;
                            item._typeid = pCi._typeid;
                            item.qntd = cwul.qntd;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (cwul.qntd > 0)
                            {
                                pce.active = 1;
                                pce.stat = (byte)(cwul.qntd == 1 ? 2 : (cwul.qntd == 2 ? 4 : (cwul.qntd == 3 ? 0 : (cwul.qntd == 4 ? 3 : (cwul.qntd == 5 ? 1 : 2)))));
                                pce.prob = (uint)(cwul.qntd * 200);
                            }

                            break;
                        }
                    default:
                        throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o item[TYPEID=" + (cwul.item_typeid) + "], usado para upar é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            200, 0x5300201));
                }

                var pClub = Player.Inventory.FindWarehouseItemById(cwul.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        204, 0x5300205));
                }

                if (pClub.clubset_workshop.rank == -1)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ClubSet dele ja upou todos os levels permitidos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        209, 0x5300210));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] Level, mas o ClubSet nao existe no IFF_STRUCT so Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        205, 0x5300206));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas esse ClubSet nao pose upar Level. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        206, 0x5300207));
                }

                // Stat Up
                var level_up_limit = sIff.Instance.findClubSetWorkShopLevelUpLimit(clubset.work_shop.tipo);
                var level_up_prob = sIff.Instance.findClubSetWorkShopLevelUpProb(clubset.work_shop.tipo);

                if (level_up_limit.Count == 0 || level_up_prob == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, IFF_STRUCT level_up_limit or level_up_prob not found. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        208, 0x5300209));
                }

                // 
                var limit = level_up_limit.FirstOrDefault(el =>
                {
                    return el.rank == pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot);
                });

                if (limit == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, nao encontrou o Level para upar no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        210, 0x5300211));
                }

                LotterySystem lottery = new LotterySystem();

                for (var ii = 0; ii < (limit.c.Length); ++ii)
                {
                    if (limit.c[ii] > (ushort)(pClub.clubset_workshop.c[ii] + clubset.SlotStats.getSlot[ii]))
                    {
                        lottery.Add(level_up_prob.c[ii] + (pce.active == 1 && ii == pce.stat ? pce.prob : 0), ii);
                    }
                }

                var lc = lottery.SpinRoleta();

                if (lc != null)
                {
                    stat = Convert.ToUInt32(lc.Value);
                }

                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpLevel][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, nao conseguiu remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        207, 0x5300208));
                }

                Player.Inventory.WorkshopLastUpLevel.clubset_id = pClub.id;
                Player.Inventory.WorkshopLastUpLevel.stat = stat;

                pClub.clubset_workshop.c[stat]++;

                // UPDATE ON DB
                NormalManagerDB.Instance.add(12,
                     new CmdUpdateClubSetWorkshop(Player.UserInfo.UID,
                         pClub,
                         CmdUpdateClubSetWorkshop.FLAG.F_UP_LEVEL),
                    null, null);

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1);

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);
                p.WriteBytes(item.stat.ToArray());
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                p.WriteZero(25);

                Player.Send(p);

                // Resposta para o ClubSet Up Level
                p.init_plain(0x23D);

                p.WriteUInt32(0); // OK;
                p.WriteUInt32((uint)stat);

                Player.Send(p);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopUpLevel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23D);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300200);

                Player.Send(p);
            }
        } 
    }
}