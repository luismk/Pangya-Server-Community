// Arquivo CmdAchievementInfo.cs
// Criado em 21/03/2018 às 21:56 por Acrisio
// Implementação da classe CmdAchievementInfo

using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdAchievementInfo : Pangya_DB
    {
        private uint m_uid;
        private readonly Dictionary<uint, List<AchievementInfoEx>> map_ai = new Dictionary<uint, List<AchievementInfoEx>>();


        public CmdAchievementInfo(uint _uid)
        {
            m_uid = _uid; 
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(10);

            var ai = new AchievementInfoEx();
            var qsi = new QuestStuffInfo();
            var cii = new CounterItemInfo { active = 1 };

            ai._typeid = IFNULL(_result.data[1]);
            ai.id = IFNULL<int>(_result.data[2]);

            qsi.id = IFNULL<int>(_result.data[4]);
            qsi._typeid = IFNULL(_result.data[5]);
            cii._typeid = IFNULL(_result.data[6]);
            cii.id = qsi.counter_item_id = IFNULL<int>(_result.data[7]);
            cii.value = IFNULL<int>(_result.data[8]);
            qsi.clear_date_unix = IFNULL(_result.data[9]);

            if (!map_ai.ContainsKey(ai._typeid))
                map_ai[ai._typeid] = new List<AchievementInfoEx>();

            var existingAi = map_ai[ai._typeid].FirstOrDefault(a => a.id == ai.id);

            if (existingAi == null)
            {
                ai.active = (byte)IFNULL(_result.data[0]);
                ai.status = IFNULL<int>(_result.data[3]);

                CheckAchievementRetorno(ai);
                CheckQuestAchievement(ai, qsi);

                ai.v_qsi.Add(qsi);

                if (cii.id > 0)
                    ai.map_counter_item[cii.id] = cii;

                map_ai[ai._typeid].Add(ai);
            }
            else
            {
                CheckQuestAchievement(existingAi, qsi);
                existingAi.v_qsi.Add(qsi);

                if (cii.id > 0)
                    existingAi.map_counter_item[cii.id] = cii;
            }
        }

        protected override Response prepareConsulta()
        {
            map_ai.Clear();

            var response = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(response, $"Failed to retrieve achievement info for player: {m_uid}");

            return response;
        }

        private void CheckAchievementRetorno(AchievementInfoEx ai)
        {
            var achievement = sIff.getInstance().findAchievement(ai._typeid);

            if (sIff.getInstance().getItemGroupIdentify(ai._typeid) != IFF_GROUP.QUEST_ITEM && achievement != null)
            {
                ai.quest_base_typeid = achievement.TypeID_Quest_Index;
            }
            else if (sIff.getInstance().getItemGroupIdentify(ai._typeid) == IFF_GROUP.ACHIEVEMENT)
                Console.WriteLine($"[CmdAchievementInfo::LineResult][Warning] Achievement[TypeId={ai._typeid}] not found in .iff file for player: {m_uid}");
        }

        private void CheckQuestAchievement(AchievementInfoEx ai, QuestStuffInfo qsi)
        {
            if (ai.status == 3 && (ai.quest_base_typeid == 0 || qsi._typeid == ai.quest_base_typeid) && qsi.counter_item_id <= 0)
            {
                Console.WriteLine($"[CmdAchievementInfo::LineResult][Warning] Quest achievement[TypeId={qsi._typeid}] does not have a counter item. Player: {m_uid}");
            }
        }

        public Dictionary<uint, List<AchievementInfoEx>> GetInfo()
        {
            return map_ai;
        }

        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint uid)
        {
            m_uid = uid;
        }

        string m_szConsulta = "pangya.ProcGetNewAchievement";
    }
}
