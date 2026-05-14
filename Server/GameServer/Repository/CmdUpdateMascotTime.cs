//Convertion By LuisMK
using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateMascotTime : Pangya_DB
    { 
        public CmdUpdateMascotTime(uint _uid,
            int _id, string _time)
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_time = _time;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getID()
        {
            return (m_id);
        }

        public void setID(int _id)
        {
            m_id = _id;
        }

        public string getTime()
        {
            return m_time;
        }

        public void setTime(string _time)
        {
            m_time = _time;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
        }

        protected override Response prepareConsulta()
        {

            if (m_id <= 0)
            {
                throw new exception("[CmdUpdateMascotTime::prepareConsulta][Error] mascot id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_time.Length == 0)
            {
                throw new exception("[CmdUpdateMascotTime::prepareConsulta][Error] time is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_id) + ", " + makeText(m_time));

            checkResponse(r, "nao conseguiu atualizar o tempo do mascot[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private string m_time = "";

        private const string m_szConsulta = "pangya.ProcUpdateMascotTime";
    }
}
