using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateNormalTrofel : Pangya_DB
    {
        public CmdUpdateNormalTrofel()
        {
            this.m_uid = 0;
        }

        public CmdUpdateNormalTrofel(uint _uid,
            TrofelInfo _ti)
        {
            this.m_uid = _uid;
            this.m_ti = (_ti);
        }

        public virtual void Dispose()
        {
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public TrofelInfo getInfo()
        {
            return m_ti;
        }

        public void setInfo(TrofelInfo _ti)
        {
            m_ti = _ti;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateNormalTrofel::prepareConsulta,Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ti.ama_6_a_1[5, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[5, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[5, 2]) + ", " + Convert.ToString(m_ti.ama_6_a_1[4, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[4, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[4, 2]) + ", " + Convert.ToString(m_ti.ama_6_a_1[3, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[3, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[3, 2]) + ", " + Convert.ToString(m_ti.ama_6_a_1[2, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[2, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[2, 2]) + ", " + Convert.ToString(m_ti.ama_6_a_1[1, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[1, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[1, 2]) + ", " + Convert.ToString(m_ti.ama_6_a_1[0, 0]) + ", " + Convert.ToString(m_ti.ama_6_a_1[0, 1]) + ", " + Convert.ToString(m_ti.ama_6_a_1[0, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[0, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[0, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[0, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[1, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[1, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[1, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[2, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[2, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[2, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[3, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[3, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[3, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[4, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[4, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[4, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[5, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[5, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[5, 2]) + ", " + Convert.ToString(m_ti.pro_1_a_7[6, 0]) + ", " + Convert.ToString(m_ti.pro_1_a_7[6, 1]) + ", " + Convert.ToString(m_ti.pro_1_a_7[6, 2]));

            checkResponse(r, "PLAYER[UID=" + Convert.ToString(m_uid) + "] nao consiguiu atualizar o Trofel Normal[AMA_1_G=" + Convert.ToString(m_ti.ama_6_a_1[5, 0]) + ", AMA_1_S=" + Convert.ToString(m_ti.ama_6_a_1[5, 1]) + ", AMA_1_B=" + Convert.ToString(m_ti.ama_6_a_1[5, 2]) + ", AMA_2_G=" + Convert.ToString(m_ti.ama_6_a_1[4, 0]) + ", AMA_2_S=" + Convert.ToString(m_ti.ama_6_a_1[4, 1]) + ", AMA_2_B=" + Convert.ToString(m_ti.ama_6_a_1[4, 2]) + ", AMA_3_G=" + Convert.ToString(m_ti.ama_6_a_1[3, 0]) + ", AMA_3_S=" + Convert.ToString(m_ti.ama_6_a_1[3, 1]) + ", AMA_3_B=" + Convert.ToString(m_ti.ama_6_a_1[3, 2]) + ", AMA_4_G=" + Convert.ToString(m_ti.ama_6_a_1[2, 0]) + ", AMA_4_S=" + Convert.ToString(m_ti.ama_6_a_1[2, 1]) + ", AMA_4_B=" + Convert.ToString(m_ti.ama_6_a_1[2, 2]) + ", AMA_5_G=" + Convert.ToString(m_ti.ama_6_a_1[1, 0]) + ", AMA_5_S=" + Convert.ToString(m_ti.ama_6_a_1[1, 1]) + ", AMA_5_B=" + Convert.ToString(m_ti.ama_6_a_1[1, 2]) + ", AMA_6_G=" + Convert.ToString(m_ti.ama_6_a_1[0, 0]) + ", AMA_6_S=" + Convert.ToString(m_ti.ama_6_a_1[0, 1]) + ", AMA_6_B=" + Convert.ToString(m_ti.ama_6_a_1[0, 2]) + ", PRO_1_G=" + Convert.ToString(m_ti.pro_1_a_7[0, 0]) + ", PRO_1_S=" + Convert.ToString(m_ti.pro_1_a_7[0, 1]) + ", PRO_1_B=" + Convert.ToString(m_ti.pro_1_a_7[0, 2]) + ", PRO_2_G=" + Convert.ToString(m_ti.pro_1_a_7[1, 0]) + ", PRO_2_S=" + Convert.ToString(m_ti.pro_1_a_7[1, 1]) + ", PRO_2_B=" + Convert.ToString(m_ti.pro_1_a_7[1, 2]) + ", PRO_3_G=" + Convert.ToString(m_ti.pro_1_a_7[2, 0]) + ", PRO_3_S=" + Convert.ToString(m_ti.pro_1_a_7[2, 1]) + ", PRO_3_B=" + Convert.ToString(m_ti.pro_1_a_7[2, 2]) + ", PRO_4_G=" + Convert.ToString(m_ti.pro_1_a_7[3, 0]) + ", PRO_4_S=" + Convert.ToString(m_ti.pro_1_a_7[3, 1]) + ", PRO_4_B=" + Convert.ToString(m_ti.pro_1_a_7[3, 2]) + ", PRO_5_G=" + Convert.ToString(m_ti.pro_1_a_7[4, 0]) + ", PRO_5_S=" + Convert.ToString(m_ti.pro_1_a_7[4, 1]) + ", PRO_5_B=" + Convert.ToString(m_ti.pro_1_a_7[4, 2]) + ", PRO_6_G=" + Convert.ToString(m_ti.pro_1_a_7[5, 0]) + ", PRO_6_S=" + Convert.ToString(m_ti.pro_1_a_7[5, 1]) + ", PRO_6_B=" + Convert.ToString(m_ti.pro_1_a_7[5, 2]) + ", PRO_7_G=" + Convert.ToString(m_ti.pro_1_a_7[6, 0]) + ", PRO_7_S=" + Convert.ToString(m_ti.pro_1_a_7[6, 1]) + ", PRO_7_B=" + Convert.ToString(m_ti.pro_1_a_7[6, 2]) + "] do player");

            return r;
        }
        private uint m_uid = new uint();
        private TrofelInfo m_ti = new TrofelInfo();

        private const string m_szConsulta = "pangya.ProcUpdateTrofelNormal";
    }
}
