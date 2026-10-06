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
using PangyaAPI.Network.Models;
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
    public class Handle_PLAYER_CLUB_SET_STATS_UPDATE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                byte opt = Packet.ReadByte();
                byte stat = Packet.ReadByte();
                int item_id = Packet.ReadInt32();

                if (opt == 1 || opt == 3)
                { // ClubSet Up/Downgrade

                    AchievementSystem sys_achieve = new AchievementSystem();

                    var pWi = Player.Inventory.FindWarehouseItemById(item_id);

                    if (pWi == null)
                    {
                        throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou " + (opt == 1 ? "updar" : "desupar") + " stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            600, 0x5200601));
                    }

                    if (stat > (int)CharacterInfo.Stats.S_CURVE)
                    {
                        throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou " + (opt == 1 ? "updar" : "desupar") + " um stat[value=" + ((ushort)stat) + "] que nao existe do ClubSet[ID=" + (item_id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            604, 0x5200605));
                    }

                    var clubset = sIff.Instance.findClubSet(pWi._typeid);

                    if (clubset == null)
                    {
                        throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou " + (opt == 1 ? "updar" : "desupar") + " stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "] que nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            601, 0x5200602));
                    }

                    if (opt == 1)
                    { // UPGRADE

                        if (((clubset.SlotStats.getSlot[stat] - clubset.Stats.getSlot[stat]) + pWi.clubset_workshop.c[stat]) < (pWi.c[stat] + 1))
                        {
                            throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas ele ja upou todos os slot's disponiveis. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                602, 0x5200603));
                        }

                        uint enchant_typeid = (uint)((sIff.Instance.ENCHANT << 26) | (stat << 20) + pWi.c[stat]);

                        var enchant = sIff.Instance.findEnchant(enchant_typeid);

                        if (enchant == null)
                        {
                            throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou upar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas nao tem o enchant[TYPEID=" + (enchant_typeid) + "] no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                603, 0x5200604));
                        }

                        Player.UserInfo.consomePang((ulong)enchant.Pang);

                        // Update ON Server
                        pWi.c[stat]++;

                        // Update ON DB
                        NormalManagerDB.Instance.add(8,
                             new CmdUpdateClubSetStats(Player.UserInfo.UID,
                                 pWi, (uint)enchant.Pang),
                            null, null);

                        // Update Achievement ON SERVER, DB and GAME
                        sys_achieve.incrementCounter(0x6C400084u);

                        // Update ON Game
                        p.init_plain(0xA5);

                        p.WriteByte(opt / 2 + 1); // [0, 1] / 2 + 1 = 1, [2, 3] / 2 + 1 = 2    // UPA = 1, DESUPA = 2
                        p.WriteByte(opt % 2); // [0, 2] mod 2 = 0, [1, 3] mod 2 = 1        // Character = 0, ClubSet = 1
                        p.WriteByte(stat);
                        p.WriteInt32(item_id);
                        p.WriteInt64(enchant.Pang);

                        Player.Send(p);

                    }
                    else if (opt == 3)
                    { // DOWNGRADE

                        if ((pWi.c[stat] - 1) < 0)
                        {
                            throw new exception("[Lobby::RequestClubSetStatsUpdate][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas ele ja desupou tudo que podia. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                605, 0x5200606));
                        }

                        // Update ON Server
                        pWi.c[stat]--;

                        // Update ON DB
                        NormalManagerDB.Instance.add(8,
                             new CmdUpdateClubSetStats(Player.UserInfo.UID,
                                 pWi, 0),
                            null, null);

                        // Update Achievement ON SERVER, DB and GAME
                        sys_achieve.incrementCounter(0x6C400085u);

                        // Update ON Game
                        p.init_plain(0xA5);

                        p.WriteByte(opt / 2 + 1); // [0, 1] / 2 + 1 = 1, [2, 3] / 2 + 1 = 2    // UPA = 1, DESUPA = 2
                        p.WriteByte(opt % 2); // [0, 2] mod 2 = 0, [1, 3] mod 2 = 1        // Character = 0, ClubSet = 1
                        p.WriteByte(stat);
                        p.WriteInt32(item_id);
                        p.WriteUInt64(0);

                        Player.Send(p);
                    }

                    // Update Achievement ON SERVER, DB and GAME
                    sys_achieve.finish_and_update(Player);
                } // OPT [0 OR 2] é Character Stats para season passada

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestClubSetStatsUpdate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0xA5);

                p.WriteByte(0); // Error

                Player.Send(p);
            }
        }
    }
}