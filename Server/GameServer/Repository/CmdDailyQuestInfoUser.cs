using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    internal class CmdDailyQuestInfoUser : Pangya_DB
    {
        public enum TYPE : byte
        {
            GET,
            CHECK
        }

        public CmdDailyQuestInfoUser()
        {
            m_uid = 0;
            m_type = TYPE.GET;
            m_ok = false;
            m_dqiu = new DailyQuestInfoUser(0);
        }

        public CmdDailyQuestInfoUser(uint uid, TYPE type)
        {
            m_uid = uid;
            m_type = type;
            m_ok = false;
            m_dqiu = new DailyQuestInfoUser(0);
        }

        public DailyQuestInfoUser GetInfo()
        {
            return m_dqiu;
        }

        public void SetInfo(DailyQuestInfoUser dqiu)
        {
            m_dqiu = dqiu;
        }

        public bool Check()
        {
            return m_ok;
        }

        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint uid)
        {
            m_uid = uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void SetType(TYPE type)
        {
            m_type = type;
        }

        protected override void lineResult(ctx_res result, uint indexResult)
        {
            if (m_type == TYPE.GET)
            {
                m_dqiu.now_date = IFNULL(result.data[0]);
                m_dqiu.accept_date = IFNULL(result.data[1]);
                m_dqiu.count = 3; // Temporariamente estático, ajustar no banco posteriormente
                m_dqiu.current_date = IFNULL(result.data[2]);
                for (var i = 0; i < 3; i++)
                    m_dqiu._typeid[i] = IFNULL(result.data[3 + i]);
            }
            else if (m_type == TYPE.CHECK)
            {
                m_ok = IFNULL(result.data[0]) == 1;
            }
        }

        protected override Response prepareConsulta()
        {
            m_ok = false;
            Response response;

            switch (m_type)
            {
                case TYPE.GET:
                    response = procedure(m_szConsulta[0],
                        Convert.ToString(m_uid));

                    checkResponse(response, $"Não conseguiu pegar o daily quest info do player: {m_uid}");
                    break;

                case TYPE.CHECK:
                    response = procedure(m_szConsulta[1],
                        Convert.ToString(m_uid));

                    checkResponse(response, $"Não conseguiu verificar o daily quest info do player: {m_uid}");
                    break;

                default:
                    throw new InvalidOperationException("Tipo de operação desconhecido.");
            }

            return response;
        }

        private uint m_uid;
        private TYPE m_type;
        private bool m_ok;
        private DailyQuestInfoUser m_dqiu;

        private readonly string[] m_szConsulta = { "pangya.ProcGetDailyQuest_New", "pangya.ProcCheckPlayerDailyQuest" };
    }
}
