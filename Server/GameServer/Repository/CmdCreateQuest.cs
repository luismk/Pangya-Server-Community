using System;
using System.Data;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdCreateQuest : Pangya_DB
    {
        public CmdCreateQuest()
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_counter_item_id = -1;
            this.m_quest = new QuestStuff();
            this.m_include_counter = false;
        }

        public CmdCreateQuest(uint _uid, bool wait) : base(wait)
        {
            this.m_uid = _uid;
            this.m_id = -1;
            this.m_counter_item_id = -1;
            this.m_quest = new QuestStuff();
            this.m_include_counter = false;
        }

        public CmdCreateQuest(uint _uid, uint _achievement_id)
        {
            this.m_uid = _uid;
            this.m_id = -1;
            this.m_counter_item_id = -1;
            this.m_quest = new QuestStuff();
            this.m_include_counter = false;
            m_achievement_id = _achievement_id;
        }

        public CmdCreateQuest(uint _uid,
            uint _achievement_id,
            QuestStuff _quest,
            bool _include_counter)
        {
            this.m_uid = _uid;
            this.m_achievement_id = _achievement_id;
            this.m_id = -1;
            this.m_counter_item_id = -1;
            this.m_quest = _quest;
            this.m_include_counter = _include_counter;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getAchievementID()
        {
            return (m_achievement_id);
        }

        public void setAchievementID(uint _achievement_id)
        {
            m_achievement_id = _achievement_id;
        }

        public QuestStuff getQuest()
        {
            return m_quest;
        }

        public void setQuest(QuestStuff _quest, bool _include_counter)
        {
            m_quest = _quest;
            m_include_counter = _include_counter;
        }

        public bool getIncludeCounter()
        {
            return m_include_counter;
        }

        public int getID()
        {
            return m_id;
        }

        public int getCounterItemID()
        {
            return m_counter_item_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(2);

            m_id = IFNULL<int>(_result.data[0]);
            m_counter_item_id = IFNULL<int>(_result.data[1]);
        }

        protected override Response prepareConsulta()
        {

            if (m_quest.ID == 0)
            {
                throw new exception("[CmdCreateQuest::prepareConsulta][Error] QuestStuff invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_quest.counter_item._typeid[0] == 0 && m_quest.counter_item._typeid[0] == 0)
            {
                throw new exception("[CmdCreateQuest::prepareConsulta][Error] IFF::QuestStuff not have counter item, invalid create quest", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 1));
            }

            m_id = -1;
            m_counter_item_id = -1;
 
            var r = procedure(m_szConsulta, (m_uid) + ", " + makeText(m_quest.Name) + ", " + (m_achievement_id)
                + ", " + (m_quest.ID) + ", " + ((m_include_counter ? m_quest.counter_item._typeid[0] : 0)));

            checkResponse(r, "nao conseguiu adicionar Quest para o player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private uint m_achievement_id = new uint();
        private int m_id = new int();
        private int m_counter_item_id = new int();
        private bool m_include_counter;
        private QuestStuff m_quest = new QuestStuff();

        private const string m_szConsulta = "pangya.ProcInsertNewQuest";
    }
}