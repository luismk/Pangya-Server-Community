using System;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCookie : Pangya_DB
    {
        private uint m_uid;
        ulong m_cookie;
        public CmdCookie(uint _uid)
        {
            m_uid = (_uid);
            m_cookie = 0;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);
            try
            {
                var uid_req = _result.GetUInt32(0);
                m_cookie = _result.GetUInt64(1);
                if (uid_req != m_uid)
                    throw new Exception("[CmdCookie::lineResult][Error] retornou outro m_uid do que foi requisitado. uid_req: " + (uid_req) + " != " + (m_uid));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = consulta("SELECT uid, cookie FROM pangya.user_info WHERE uid = " + m_uid);
            checkResponse(r, "nao conseguiu pegar o cookie info do player: " + (m_uid));
            return r;
        }


        public ulong getCookie()
        {
            return m_cookie;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }
    }
}