using System;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCookie : Pangya_DB
    {
        public enum T_UPDATE_COOKIE : byte
        {
            INCREASE,
            DECREASE
        }

        public CmdUpdateCookie()
        {
            this.m_uid = 0;
            this.m_cookie = 0Ul;
            this.m_type_update = T_UPDATE_COOKIE.INCREASE;
        }

        public CmdUpdateCookie(uint _uid,
            ulong _cookie,
            T_UPDATE_COOKIE _type_update
            )
        {
            this.m_uid = _uid;
            this.m_cookie = _cookie;
            this.m_type_update = _type_update;
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public ulong getCookie()
        {
            return m_cookie;
        }

        public void setCookie(ulong _cookie)
        {
            m_cookie = _cookie;
        }

        public CmdUpdateCookie.T_UPDATE_COOKIE getTypeUpdate()
        {
            return m_type_update;
        }

        public void setTypeUpdate(T_UPDATE_COOKIE _type_update)
        {
            m_type_update = _type_update;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = _update(m_szConsulta[0] + (m_type_update == T_UPDATE_COOKIE.INCREASE ? " + " : " - ") + Convert.ToString(m_cookie) + m_szConsulta[1] + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu atualizar o cookie[value=" + (m_type_update == T_UPDATE_COOKIE.INCREASE ? " + " : " - ") + Convert.ToString(m_cookie) + "] do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private ulong m_cookie = new ulong();
        private T_UPDATE_COOKIE m_type_update;

        private string[] m_szConsulta = { "UPDATE pangya.user_info SET cookie = cookie ", " WHERE UID = " };
    }
}
