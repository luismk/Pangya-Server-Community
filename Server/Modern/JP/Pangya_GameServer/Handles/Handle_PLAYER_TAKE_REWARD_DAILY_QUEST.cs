using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_TAKE_REWARD_DAILY_QUEST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {

            var p = new Packet();

            int[] quest_id = null;

            try
            {
                if (Packet == null)
                {
                    throw new exception("Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        2, 0));
                }

                int num_quest = Packet.ReadInt32();

                if (num_quest <= 0u)
                {
                    throw new exception("RoomID de quest para pegar recompensa e 0", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        5005, 0));
                }

                quest_id = Packet.ReadInt32(4 * num_quest);

                var v_quest = DailyQuestManager.LeaveQuestUser(Player, quest_id, num_quest);

                QuestItem qi = null;

                List<stItem> v_item = [];
                stItem item = new();

                // UPADATE Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achievement = new AchievementSystem();

                // Add Reward Item do Player
                foreach (var el in v_quest)
                {

                    // Item Reward, d� para o Player
                    if ((qi = sIff.Instance.findQuestItem(el._typeid)) != null)
                    {
                        for (var i = 0; i < (qi.reward._typeid.Length); ++i)
                        {

                            if (qi.reward._typeid[i] != 0)
                            {

                                item = new stItem(); 
                                item.type = 2;
                                item.id = -1;
                                item._typeid = qi.reward._typeid[i];
                                item.qntd = (int)qi.reward.qntd[i];
                                item.c[0] = (short)item.qntd;
                                item.c[3] = (short)qi.reward.time[i];

                                // Add Item no db e no Player
                                var rt = RetAddItem.INIT_VALUE; 
                                if ((rt = ItemManager.addItem(item,  Player, 0, 0)) < 0)
                                {
                                    throw new exception("[DailyQuestManager::requestTakeRewardQuest][Error] Normal[UID=" + Convert.ToString(Player.UserInfo.UID) + "] tentou pegar a recompensa da Quest[TYPEID=" + Convert.ToString(el._typeid) + ", ID=" + Convert.ToString(el.id) + "], mas nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST, 1500, 0));
                                }

                                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {
                                    v_item.Add(new stItem(item));
                                }
                            }
                        }

                        // S� add o contador de clear quest pega recompensa nas quest normais, a box de 10 clear quest n�o add ao contador
                        if (el._typeid != CLEAR_10_DAILY_QUEST_TYPEID)
                        {
                            sys_achievement.incrementCounter(0x6C40009F/*Pega Recompessa de clear quest*/);
                        }
                    }
                }

                // Add os Counter Item Excluido do Player
                foreach (var el in v_quest)
                {

                    if (el.map_counter_item.Any())
                    {

                        foreach (var el2 in el.map_counter_item.Values)
                        {

                            item = new stItem
                            {
                                type = 2,
                                id = el2.id,
                                _typeid = el2._typeid,
                                qntd = el2.value * -1
                            };
                            item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            item.stat.qntd_ant = el2.value;
                            item.stat.qntd_dep = item.stat.qntd_ant + item.qntd;
                            v_item.Add(new stItem(item));
                        }
                    }
                }

                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_TAKE_REWARD_DAILY_QUEST][Sucess] Normal[UID: {Player.UserInfo.UID}] Pegou recompensa da Daily Quest com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));


                // UPDATE ON GAME
                p.init_plain(0x216); 
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix()); 
                p.WriteInt32(v_item.Count); // Att 2 Item 
                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id); // Login do item no banco de dados
                    p.WriteUInt32(el.flag_time); // type
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                }
                Player.Send(p);


                Player.Send(Handle_PACKET_RESPONSE.pacote227(v_quest));

                // UPADATE Achievement ON SERVER, DB and GAME
                sys_achievement.finish_and_update(Player);

                if (quest_id != null)
                {
                    quest_id = null;
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TAKE_REWARD_DAILY_QUEST][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                Player.Send(Handle_PACKET_RESPONSE.pacote227(new List<AchievementInfoEx>(), 1));
                 
                if (quest_id != null)
                {
                    quest_id = null;
                }
            }

            await Task.CompletedTask;
        }
    }
}