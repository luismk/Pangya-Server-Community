using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
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
    public class Handle_PLAYER_CLUB_SET_RESET : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                uint item_typeid = Packet.ReadUInt32();
                int clubset_id = Packet.ReadInt32();

                if (item_typeid != 0x1A00024B && item_typeid != 0x1A000247)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas o item[TYPEID=" + (item_typeid) + "] é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        505, 0x5300506));
                }

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o item[TYPEID=" + (item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        500, 0x5300501));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem quantidade suficiente do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", Request=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        501, 0x5300502));
                }

                var pClub = Player.Inventory.FindWarehouseItemById(clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        502, 0x5300503));
                }

                var clubset = sIff.Instance.findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas o ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        503, 0x5300504));
                }

                int rank_base = WarehouseItemEx.ClubsetWorkshop.s_calcRank(clubset.SlotStats.getSlot);
                int rank = pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot);

                if (rank_base == -1 || rank == -1)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], nao conseguiu pegar o Rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + ", RankPosition=" + (rank) + ", rank_base=" + (rank_base) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        505, 0x5300506));
                }

                var rank_up_exp = sIff.Instance.findClubSetWorkShopRankExp(clubset.work_shop.tipo_rank_s);

                if (rank_up_exp == null)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas nao encontrou o Rank Up Exp[Type=" + (clubset.work_shop.tipo_rank_s) + "] no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        504, 0x5300505));
                }

                // Item reset ClubSet
                item = new stItemEx();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestClubSetReset][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas nao conseguiu remover o Item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]. ErrorSystem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        506, 0x5300507));
                }

                v_item.Add(new stItemEx(item));

                uint mastery = 0;
                long pang = 0;

                if (item_typeid == 0x1A00024B)
                { // Hard Reset devolve 50% do Pang e Mastery gasto no ClubSet

                    Enchant enchant = null;

                    // Soma Todo Mastery Gasto no ClubSet
                    for (var i = rank_base + 1; i <= rank; ++i)
                    {
                        mastery += rank_up_exp.rank[i];
                    }

                    // Soma Todo Pang Gasto no ClubSet
                    for (var i = 0u; i < (pClub.c.Length); ++i)
                    {
                        for (var j = 0u; j < (uint)pClub.c[i]; ++j)
                        {
                            if ((enchant = sIff.Instance.findEnchant(((Convert.ToUInt32(sIff.Instance.ENCHANT) << 26) | (i << 20) + j))) != null)
                            {
                                pang += enchant.Pang;
                            }
                        }
                    }

                    // Metade
                    mastery = (uint)(mastery * 0.5f);
                    pang = (long)(pang * 0.5f);

                    pClub.clubset_workshop.mastery += mastery;

                    // Só atualiza os pangs se for maior que zero
                    if (pang > 0)
                    {
                        Player.UserInfo.addPang((ulong)pang);
                    }

                    p.init_plain(0xC8);

                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteInt64(pang);

                    Player.Send(p);

                }

                // UPDATE ON SERVER

                // Reseta ClubSet Workshop Stats 
                pClub.clubset_workshop.c = new short[5];

                pClub.clubset_workshop.level = 0;
                pClub.clubset_workshop.rank = 0;
                pClub.clubset_workshop.recovery_pts = 0;
                // Reseta ClubSet Stats 
                pClub.c = new short[5];

                // Atualiza o stats do ClubSet Workshop
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

                // Atualiza os stats do ClubSet
                item.type = 0xC9;
                item.c = pClub.c;

                v_item.Add(new stItemEx(item));

                // UPDATE ON DB

                // Reset ON DB ClubSet Workshop
                NormalManagerDB.Instance.add(12,
                     new CmdUpdateClubSetWorkshop(Player.UserInfo.UID,
                         pClub,
                         CmdUpdateClubSetWorkshop.FLAG.F_RESET),
                    null, null);

                // Reset ON DB ClubSet Stats
                NormalManagerDB.Instance.add(8,
                     new CmdUpdateClubSetStats(Player.UserInfo.UID,
                         pClub, 0),
                    null, null);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[ClubSet::Reset][Sucess] Normal [UID=" + Player.UserInfo.UID + "] resetou o ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] " + (item_typeid == 0x1A00024B ? ("Hard[Pang=" + (pang) + ", Mastery=" + (mastery) + "] Item") : "Soft Item"), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                    p.WriteInt16(el.c);
                    p.WriteZero(15);
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                Player.Send(p);

                // Resposta para o ClubSet Reset
                p.init_plain(0x247);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(pClub._typeid);
                p.WriteInt32(pClub.id);

                Player.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetReset][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x247);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300500);

                Player.Send(p);
            }
        } 
    }
}