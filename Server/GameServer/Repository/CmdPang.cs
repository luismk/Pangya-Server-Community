using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdPang : Pangya_DB
    {

        public CmdPang(uint _uid)
        {
            this.m_uid = (_uid);
            this.m_pang = 0Ul;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public ulong getPang()
        {
            return m_pang;
        }

        public void setPang(ulong _pang)
        {
            m_pang = _pang;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(2);

            uint uid_req = IFNULL<uint>(_result.data[0]);

            m_pang = IFNULL<ulong>(_result.data[1]);
            if (uid_req != m_uid)
            {
                throw new exception("[CmdPang::lineResult][Error] retornou outro m_uid do que foi requisitado. uid_req " + Convert.ToString(uid_req) + " != " + Convert.ToString(m_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdPang::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_pang = 0Ul;

            var r = consulta(m_szConsulta + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar o pang do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private ulong m_pang = new ulong();

        private const string m_szConsulta = "SELECT uid, pang FROM pangya.user_info WHERE UID = ";
    }
}
