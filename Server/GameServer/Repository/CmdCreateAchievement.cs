using System;
using System.Data;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdCreateAchievement : Pangya_DB
    { 
        public CmdCreateAchievement(uint _uid)
        {
            this.m_uid = _uid;
            this.m_typeid = 0;
            this.m_name = "";
            this.m_status = 0;
        }

        public CmdCreateAchievement(uint _uid, bool wait) : base(wait)
        {
            this.m_uid = _uid;
            this.m_typeid = 0;
            this.m_name = "";
            this.m_status = 0;
        }

        public CmdCreateAchievement(uint _uid,
            uint _typeid, string _name,
            uint _status)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_name = _name;
            this.m_status = _status;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getTypeid()
        {
            return (m_typeid);
        }

        public void setTypeid(uint _typeid)
        {
            m_typeid = _typeid;
        }

        public string getName()
        {
            return m_name;
        }

        public void setName(string _name)
        {
            m_name = _name;
        }

        public uint getStatus()
        {
            return (m_status);
        }

        public void setStatus(uint _status)
        {
            m_status = _status;
        }

        public void setAchievement(uint _typeid,
            string _name, uint _status)
        {
            setTypeid((_typeid));
            setName(_name);
            setStatus(_status);
        }

        public int getID()
        {
            return m_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_id = (int)IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_typeid == 0)
            {
                throw new exception("[CmdCreateAchievement::prepareConsulta][Error] achievement invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_id = -1;

            var r = procedure(m_szConsulta, (m_uid) + ", " + makeText(m_name) + ", " + (m_typeid) + ", 1, " + (m_status));

            checkResponse(r, "nao conseguiu adicionar Achievement para o player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private uint m_typeid = new uint();
        private uint m_status = new uint();
        private string m_name = "";

        private const string m_szConsulta = "pangya.ProcInsertNewAchievement";
    }
}