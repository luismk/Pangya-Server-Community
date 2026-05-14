using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{

    public class CmdUpdateLegacyTikiShopPoint : Pangya_DB
    {

        public CmdUpdateLegacyTikiShopPoint(uint _uid,
            ulong _tiki_pts)
        {

            this.m_uid = _uid;

            this.m_tiki_shop_point = _tiki_pts;
        }

        public CmdUpdateLegacyTikiShopPoint()
        {
            this.m_uid = 0;
            this.m_tiki_shop_point = 0Ul;
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

        public ulong getTikiShopPoint()
        {
            return (m_tiki_shop_point);
        }

        public void setTikiShopPoint(ulong _tiki_pts)
        {
            m_tiki_shop_point = _tiki_pts;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateLegacyTikiShopPoint::prepareConsulta][Error] m_uid is invalid(" + Convert.ToString(m_uid) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_tiki_shop_point) + m_szConsulta[1] + Convert.ToString(m_uid));

            checkResponse(r, "Nao conseguiu atualizar o Legacy Tiki Shop Point[POINT=" + Convert.ToString(m_tiki_shop_point) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private ulong m_tiki_shop_point = new ulong();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_tiki_points SET Tiki_Points = ", ", MOD_DATE = CURRENT_TIMESTAMP WHERE UID = " };
    }
}
