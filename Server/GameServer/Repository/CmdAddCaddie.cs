using System;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddCaddie : CmdAddItemBase
    {
        public CmdAddCaddie(uint _uid,
            CaddieInfoEx _ci,
            byte _purchase,
            byte _gift_flag) : base(_uid,
                _purchase, _gift_flag)
        {
            this.m_ci = _ci;
        }


        public CaddieInfoEx getInfo()
        {
            return m_ci;
        }

        public void setInfo(CaddieInfoEx _ci)
        {
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(2);

            m_ci.id = IFNULL<int>(_result.data[0]);
            if (_result.IsNotNull(1))
            {
                m_ci.end_date.CreateTime(_translateDate(_result.data[1]));
            }
            m_ci.Check();
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdAddCaddie::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString(m_ci._typeid) + ", " + Convert.ToString((ushort)m_gift_flag) + ", " + Convert.ToString((ushort)m_purchase) + ", " + Convert.ToString((ushort)m_ci.rent_flag) + ", " + Convert.ToString(m_ci.end_date_unix));

            checkResponse(r, "nao conseguiu adicionar o caddie[TYPEID=" + Convert.ToString(m_ci._typeid) + "] para o player: " + Convert.ToString(m_uid));

            return r;
        }

        private CaddieInfoEx m_ci = new CaddieInfoEx();

        private const string m_szConsulta = "pangya.ProcAddCaddie";
    }
}
