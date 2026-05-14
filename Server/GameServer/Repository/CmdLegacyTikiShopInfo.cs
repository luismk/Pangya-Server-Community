using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdLegacyTikiShopInfo : Pangya_DB
    {

        public CmdLegacyTikiShopInfo(uint uid)
        {
            this.m_uid = uid;
            this.m_tiki_pts = 0Ul;
        }
        public CmdLegacyTikiShopInfo()
        {
            this.m_uid = 0;
            this.m_tiki_pts = 0Ul;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public ulong getInfo()
        {
            return m_tiki_pts;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            m_tiki_pts = IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdLegacyTikiShopInfo::prepareConsulta][Error] m_uid is invalind(" + Convert.ToString(m_uid) + ")");
            }

            m_tiki_pts = 0Ul;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "Nao conseguiu pegar o Legacy Tiki Points do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = 0;
        private ulong m_tiki_pts = 0;

        private const string m_szConsulta = "pangya.ProcGetLegacyTikiShopInfo";
    }
}