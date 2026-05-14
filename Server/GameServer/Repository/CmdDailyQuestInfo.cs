using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdDailyQuestInfo : Pangya_DB
    {
        public CmdDailyQuestInfo()
        {
            this.m_dqi = new DailyQuestInfo();
        }


        public DailyQuestInfo getInfo()
        {
            return m_dqi;
        }

        public void setInfo(DailyQuestInfo _dqi)
        {
            m_dqi = _dqi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(4);

            for (var i = 0; i < 3; ++i)
            {
                m_dqi._typeid[i] = IFNULL<uint>(_result.data[0 + i]); // 0 + 3 = 3
            }

            if (_result.data[3] != null)
            {
                m_dqi.date.CreateTime(_translateDate(_result.data[3]));
            }
        }

        protected override Response prepareConsulta()
        {

            m_dqi.clear();

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar o Daily Quest Info");

            return r;
        }

        private DailyQuestInfo m_dqi = new DailyQuestInfo();

        private const string m_szConsulta = "SELECT achieve_quest_1, achieve_quest_2, achieve_quest_3, Reg_Date FROM pangya.pangya_daily_quest";
    }
}