using Pangya_GameServer.Feature;
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
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHARACTER_MASTERY_EXPAND : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                if (Player.UserInfo.BlockFlag.Flag.CharacterMastery)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir o character mastery, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                uint char_typeid = Packet.ReadUInt32();
                int char_id = Packet.ReadInt32();

                var pCi = Player.Inventory.FindCharacterById(char_id);

                if (pCi == null || pCi._typeid != char_typeid)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas ele nao possui o character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        650, 0x5200651));
                }

                var mastery = sIff.Instance.findCharacterMastery(char_typeid);

                if (mastery.Count == 0)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas nao tem o character mastery no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        651, 0x5200652));
                }

                if (pCi.mastery + 1 > mastery.Count)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas ele ja expandiu todos que é permitido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        652, 0x5200653));
                }

                if (mastery[(int)pCi.mastery].seq != (pCi.mastery + 1))
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas a sequencia do mastery no IFF_STRUCT é diferente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        653, 0x5200654));
                }

                if ((char)mastery[(int)pCi.mastery].level > Player.UserInfo.Member.GameLevel)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas nao tem Level suficiente[have_lvl=" + (mastery[(int)pCi.mastery].level) + ", req_lvl=" + ((short)Player.UserInfo.Member.GameLevel) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        654, 0x5200655));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                var condition = mastery[(int)pCi.mastery].condition;

                for (var i = 0; i < 5; ++i)
                {
                    if (condition.condition[i] > 0)
                    {
                        switch (sIff.Instance.getItemGroupIdentify(condition.condition[i]))
                        {
                            case IFF_GROUP.ITEM:
                                {
                                    var pWi = Player.Inventory.FindWarehouseItemByTypeid(condition.condition[i]);

                                    if (pWi == null)
                                    {
                                        throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] nao tem o item da condicao.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            656, 0x5200657));
                                    }

                                    if (pWi.STDA_C_ITEM_QNTD < (short)condition.qntd[i])
                                    {
                                        throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] o item nao tem quantidade suficiente para a condicao", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            657, 0x5200658));
                                    }

                                    item = new stItem();

                                    item.type = 2;
                                    item._typeid = condition.condition[i];
                                    item.id = (int)pWi.id;
                                    item.qntd = (int)condition.qntd[i];
                                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                                    v_item.Add(new stItem(item));

                                    break;
                                }
                            case IFF_GROUP.QUEST_STUFF:
                                {
                                    var pQsi = Player.UserInfo.Achievements.findQuestStuffByTypeId(condition.condition[i]);

                                    if (pQsi == null)
                                    {
                                        throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] nao tem o QuestStuff da condicao", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            658, 0x5200659));
                                    }

                                    if (!pQsi.isValid())
                                    {
                                        throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] o counter item da condicao esta inativo", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            659, 0x5200660));
                                    }

                                    if (pQsi.counter_item_id == 0 || pQsi.clear_date_unix == 0)
                                    {
                                        throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] o QuestStuff[TYPEID=" + (pQsi._typeid) + "] nao foi concluido", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            660, 0x5200661));
                                    }

                                    break;
                                }
                            default:
                                throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Unknown Condition[TYPEID=" + (condition.condition[i]) + ", QNTD=" + (condition.qntd[i]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    655, 0x5200656));
                        }
                    }
                }

                if (ItemManager.removeItem(v_item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestCharacterMasteryExpand][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu excluir os item(ns) do Player", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        661, 0x5200662));
                }

                pCi.mastery++;

                item = new stItem();

                item._typeid = pCi._typeid;
                item.id = (int)pCi.id;
                item.type = 0xCD;
                item.flag = (byte)pCi.mastery;

                v_item.Add(new stItem(item));

                NormalManagerDB.Instance.add(9, new CmdUpdateCharacterMastery(Player.UserInfo.UID, pCi), null, null);

                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                    if (el.type == 0xCD)
                    {
                        p.WriteUInt32(el.flag);
                    }
                }

                Player.Send(p);

                p.init_plain(0x26E);

                p.WriteUInt32(0);

                Player.Send(p);

                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000C3u);

                sys_achieve.finish_and_update(Player);

                Player.Inventory.SyncCharacter(pCi.id, pCi);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestCharacterMasteryExpand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26E);

                p.WriteUInt32(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200650);

                Player.Send(p);
            }
        }
    }
}