using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdTrofelInfo : Pangya_DB
    {
        private uint m_uid;
        TYPE_SEASON m_season;
        TrofelInfo m_ti = new TrofelInfo();
        public enum TYPE_SEASON : byte
        {
            ALL,        // Todas SEASON
            ONE,        // 1
            TWO,        // 2
            THREE,      // 3
            FOUR,       // 4
            CURRENT     // Atual
        }
        public CmdTrofelInfo(uint _uid, TYPE_SEASON _season)
        {
            m_uid = (_uid);
            m_season = (_season);
            m_ti = new TrofelInfo();
        }
        public CmdTrofelInfo(uint _uid, uint _season)
        {
            m_uid = (_uid);
            m_season = (TYPE_SEASON)(_season);
            m_ti = new TrofelInfo();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(39);
            try
            {
                short i = 0, j = 0;

                // AMA 6~1
                for (i = 0; i < 6; ++i)
                    for (j = 0; j < 3; ++j)
                        m_ti.ama_6_a_1[i, j] = IFNULL<short>(_result.data[(i * 3) + j]); // 0 a (3 * 6) = 18

                // PRO 1~7
                for (i = 0; i < 7; ++i)
                    for (j = 0; j < 3; ++j)
                        m_ti.pro_1_a_7[i, j] = IFNULL<short>(_result.data[18 + (i * 3) + j]);    // 18 a (3 * 7) = 39 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetTrofel", m_uid.ToString() + ", " + ((int)m_season).ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }


        public TrofelInfo getInfo()
        {
            return m_ti;
        }



        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE_SEASON getSeason()
        {
            return m_season;
        }

        public void getSeason(TYPE_SEASON _type)
        {
            m_season = _type;
        }

    }
}