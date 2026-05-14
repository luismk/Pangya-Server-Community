using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGeraUCCWebKey : Pangya_DB
    {
        public CmdGeraUCCWebKey(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_ucc_id = -1;
            this.m_key = "";
        }

        public CmdGeraUCCWebKey(uint _uid,
            int _ucc_id,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_ucc_id = _ucc_id;
            this.m_key = "";
        }
        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getUCCID()
        {
            return m_ucc_id;
        }

        public void setUCCID(int _ucc_id)
        {
            m_ucc_id = _ucc_id;
        }

        public string getKey()
        {
            return m_key;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            if (is_valid_c_string(_result.data[0]))
            {
                m_key = _result.data[0].ToString();
            }

            if (m_key.Length == 0)
            {
                throw new exception("[CmdGeraUCCWebKey::lineResult][Error] m_key is empty, nao conseguiu pegar uma ucc key do banco de dados.", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdGeraUCCWebKey::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ucc_id <= 0)
            {
                throw new exception("[CmdGeraUCCWebKey::prepareConsulta][Error] m_ucc_id[value=" + Convert.ToString(m_ucc_id) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ucc_id));

            checkResponse(r, "nao conseguiu gerar um UCC[ID=" + Convert.ToString(m_ucc_id) + "] Web Key para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private int m_ucc_id = new int();
        private string m_key = "";

        private const string m_szConsulta = "pangya.ProcGeraSecurityKey";
    }
}