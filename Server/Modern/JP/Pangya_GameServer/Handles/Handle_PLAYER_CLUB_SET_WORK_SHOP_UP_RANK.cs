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
using System.Windows.Input;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_RANK : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                CWUpRank cwup = new CWUpRank().ToRead(Packet);
                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                uint stat = 2; // PWR, CTRL, ACCRY, SPIN e CURVE

                if (cwup.qntd > 0)
                {
                    var pCi = Player.Inventory.FindCardByTypeid(cwup.item_typeid);

                    if (pCi == null)
                    {
                        throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem o Card[TYPEID=" + (cwup.item_typeid) + "] para upar o RankPosition. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            350, 0x5300351));
                    }

                    if (pCi.qntd < (int)cwup.qntd)
                    {
                        throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem quantidade suficiente de Card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", QNTD=" + (pCi.qntd) + ", Request=" + (cwup.qntd) + "] para upar o RankPosition. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            351, 0x5300532));
                    }

                    // Card
                    item = new stItemEx();

                    item.type = 2;
                    item.id = (int)pCi.id;
                    item._typeid = pCi._typeid;
                    item.qntd = cwup.qntd;
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);
                }

                var pClub = Player.Inventory.FindWarehouseItemById(cwup.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 0x5300353));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas esse ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 0x5300354));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas esse ClubSet nao é permitido upar de RankPosition. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        354, 0x5300355));
                }

                // Stat Up
                var level_up_limit = sIff.Instance.findClubSetWorkShopLevelUpLimit(clubset.work_shop.tipo);

                if (level_up_limit.Count == 0)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], IFF_STRUCT level_up_limit not found. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        208, 0x5300209));
                }

                // 
                var limit = level_up_limit.FirstOrDefault(el =>
                {
                    return el.rank == (pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot) + 1);
                });

                if (limit == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], nao encontrou o Level para upar no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        210, 0x5300211));
                }

                if (cwup.qntd > 4)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas a quantidade de card[TYPEID=" + (cwup.item_typeid) + ", QNTD=" + (cwup.qntd) + "] é desconhecida", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        355, 0x5300356));
                }

                // Stat Up, quando upar o Rank do ClubSet
                stat = (uint)(cwup.qntd == 0 ? 2 : (cwup.qntd == 1 ? 4 : (cwup.qntd == 2 ? 0 : (cwup.qntd == 3 ? 3 : (cwup.qntd == 4 ? 1 : 2)))));

                if (limit.c[stat] <= (pClub.clubset_workshop.c[stat] + clubset.SlotStats.getSlot[stat]))
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas o Player nao pode mais upar esse stat[value=" + (stat) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        357, 0x5300358));
                }

                var rank_up_exp = sIff.Instance.findClubSetWorkShopRankExp(clubset.work_shop.tipo_rank_s);

                if (rank_up_exp == null)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas nao encontrou o Rank Up Exp no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        358, 0x5300359));
                }

                // Rank do ClubSet +1 que ele vai tornar-se
                int rank = pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot) + 1;

                if (rank == -1)
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas pegou um RankPosition desconhecido, System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        360, 0x5300361));
                }

                if ((uint)pClub.clubset_workshop.mastery < rank_up_exp.rank[(uint)rank])
                {
                    throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID=" + Player.Inventory.uid + "] tentou upar RankPosition[RankPosition=" + (rank) + "] do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas ele nao tem mastery[value=" + (pClub.clubset_workshop.mastery) + ", Request=" + (rank_up_exp.rank[(uint)rank]) + "] suficiente para upar o RankPosition. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        359, 0x5300360));
                }

                // Remove Card
                if (item._typeid != 0)
                {
                    if (ItemManager.removeItem(item, Player) <= 0)
                    {
                        throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID = " + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], mas nao conseguiu remover Card[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            356, 0x5300357));
                    }

                    v_item.Add(new stItemEx(item));
                }

                // UPDATE ON SERVER

                // Upa Stat do RankPosition S, que da 1 de bonus
                if (rank == 5)
                {
                    if (clubset.work_shop.rank_s_stat > 4)
                    {
                        throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID = " + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], mas o ClubSet Stat[value=" + (clubset.work_shop.rank_s_stat) + "] Rank S do IFF_STRUCT do Server é invalido. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            361, 0x5300362));
                    }

                    // Rank S Bonus Stat
                    pClub.clubset_workshop.c[clubset.work_shop.rank_s_stat]++;
                }

                // Up Stat
                pClub.clubset_workshop.c[stat]++;
                pClub.clubset_workshop.recovery_pts = 0;
                pClub.clubset_workshop.rank = rank;
                pClub.clubset_workshop.level = pClub.clubset_workshop.calcLevel(clubset.SlotStats.getSlot);
                pClub.clubset_workshop.mastery -= rank_up_exp.rank[(uint)rank];

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
                     new CmdUpdateClubSetWorkshop(Player.Inventory.uid,
                         pClub,
                         CmdUpdateClubSetWorkshop.FLAG.F_UP_RANK),
                    null, null);

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
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                Player.Send(p);

                // Check Se Ele Pode Transformar e se ele transformou
                if (clubset.work_shop.flag_transformar == 1)
                { // Esse Clubset pode transformar-se em um ClubSet Special
                    LotterySystem lottery = new LotterySystem();

                    lottery.Add(250, 0x1000005D); // Wingtross Evo-Knight Club Set
                    lottery.Add(250, 0x1000005E); // Giga Yard Totem Pole Club Set
                    lottery.Add(250, 0x1000005F); // Duostar Manapikal Club Set
                    lottery.Add(750 * 14, 0); // Não Transforma nada

                    var lc = lottery.SpinRoleta();

                    if (lc != null && Convert.ToInt32(lc.Value) != 0)
                    { // Transformou
                        var clubset_original = sIff.Instance.findClubSetOriginal((uint)lc.Value);

                        if (clubset_original.Count == 0)
                        {
                            throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID = " + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao encontrou o Special ClubSet Original no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                362, 0x5300363));
                        }

                        if (clubset_original.Count <= (uint)(rank - 1))
                        {
                            throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID = " + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao tem o Rank[value=" + (rank) + "] do Special ClubSet Original no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                363, 0x5300364));
                        }

                        // 
                        var it = clubset_original.FirstOrDefault(el =>
                        {
                            return WarehouseItemEx.ClubsetWorkshop.s_calcRank(el.SlotStats.getSlot) == rank;
                        });

                        if (it == null)
                        {
                            throw new exception("[Lobby::RequestClubSetWorkShopUpRank][Error] Normal [UID = " + Player.Inventory.uid + "] tentou upar RankPosition do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao encontrou o Rank[value=" + (rank) + "] no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                364, 0x5300365));
                        }

                        // Não tem o ClubSet Sorteado, Envia para cliente um dialog se ele quer transformar o ClubSet ou não
                        if (!Player.Inventory.ownerItem(it.ID))
                        {
                            // Att taqueira que ele pode transformar se ele confirmar depois
                            Player.Inventory.WorkshopTransform.clubset_id = pClub.id;
                            Player.Inventory.WorkshopTransform.stat = stat;
                            Player.Inventory.WorkshopTransform.transform_typeid = it.ID;

                            // Log
                            _smp.LogManager.Instance.push(new AppMessage("[ClubSetWorkshop::UpRank][Sucess] Normal [UID=" + Player.Inventory.uid + "] transformou o ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] no ClubSet[TYPEID=" + (it.ID) + "] Special, aguardando confirmacao do cliente.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Dialog de Transformação do ClubSet
                            p.init_plain(0x241);

                            Player.Send(p);

                            return;
                        }
                    }
                }
                // Fim do Check Transform ClubSet

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[ClubSetWorkshop::UpRank][Sucess] Normal [UID=" + Player.Inventory.uid + "] upou Rank[value=" + (rank) + "] do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] Stat[value=" + (stat) + "" + ((rank == 5) ? (", Rank S bonus=" + (clubset.work_shop.rank_s_stat) + "") : "") + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para o ClubSet Workshop Up Rank
                p.init_plain(0x240);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(stat);
                p.WriteInt32(pClub.id);

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                // Add +1 ao contado do Up Rank S ClubSet
                if (rank == 5)
                {
                    sys_achieve.incrementCounter(0x6C4000A7u);
                }

                sys_achieve.incrementCounter(0x6C4000A3u);

                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetWorkShopUpRank][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x240);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300350);

                Player.Send(p);
            }
        } 
    }
}