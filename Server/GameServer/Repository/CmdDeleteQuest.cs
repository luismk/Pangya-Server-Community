using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteQuest : Pangya_DB
    {
        public CmdDeleteQuest(uint _uid,
            List<QuestStuffInfo> _v_id)
        {
            this.m_uid = _uid;
            this.v_id = new List<int>();

            foreach (var el in _v_id)
            {
                v_id.Add((int)el.id);
            }
        }
        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getId()
        {
            if (v_id.Count > 0)
            {
                return v_id.First();
            }
            return -1;
        }

        public List<int> getIds()
        {
            return v_id;
        }

        public void setId(int _id)
        {

            if (v_id.Count > 0)
            {
                v_id.Clear();
            }

            v_id.Add(_id);
        }

        public void setId(List<QuestStuffInfo> _v_id)
        {

            if (v_id.Count > 0)
            {
                v_id.Clear();
            }

            foreach (var el in _v_id)
            {
                v_id.Add((int)el.id);
            }
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um DELETE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdDeleteQuest::prepareConsulta][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (v_id.Count == 0)
            {
                throw new exception("[CmdDeleteQuest::prepareConsulta][Error] v_quest_id is empty.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 1));
            }

            string str_ids = string.Join(",", v_id);

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_uid) + m_szConsulta[1] + str_ids + m_szConsulta[2]);

            checkResponse(r, "nao conseguiu deletar Quest[ID = { " + str_ids + " }] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        protected uint m_uid = new uint();
        protected List<int> v_id = new List<int>();

        protected string[] m_szConsulta = { "DELETE FROM pangya.pangya_quest WHERE UID = ", " AND id IN(", ")" };

    }
}