using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGrandPrixClear : Pangya_DB
    {
        public CmdGrandPrixClear(uint uid)
        {
            this.m_uid = uid;
            m_gpc = new List<GrandPrixClear>();
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);

            GrandPrixClear gpc = new GrandPrixClear();

            gpc._typeid = IFNULL(_result.data[0]);
            gpc.position = IFNULL(_result.data[1]);

            m_gpc.Add(gpc);
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdGrandPrixClear::prepareConsulta][Error] m_uid is invalid(zero)");
            }

            m_gpc.Clear();

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar o  Grand Prix Clear do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private List<GrandPrixClear> m_gpc = new List<GrandPrixClear>();

        private const string m_szConsulta = "pangya.ProcGetGrandPrixClear";

        public List<GrandPrixClear> getInfo()
        {
            return new List<GrandPrixClear>(m_gpc);
        }
    }
}