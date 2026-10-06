using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdApproachMissions : Pangya_DB
    {
        public CmdApproachMissions(bool _waiter = false)
        {
        }

        public List<mission_approach_dados> getInfo()
        {
            return m_missions;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(5, _result.cols);

            mission_approach_dados mad = new mission_approach_dados
            {
                numero = IFNULL<uint>(_result.data[0]),
                tipo = (eMISSION_TYPE)(IFNULL<uint>(_result.data[1])),
                reward_tipo = IFNULL<uint>(_result.data[2]),
                box = IFNULL<uint>(_result.data[3])
            };
            mad.flag.flag = IFNULL<uint>(_result.data[4]);

            m_missions.Add(mad);
        }

        protected override Response prepareConsulta()
        {

            if (m_missions.Any())
            {
                m_missions.Clear();
            }

            var r = procedure(
                m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar as missions do Approach");

            return r;
        }


        private List<mission_approach_dados> m_missions = new List<mission_approach_dados>();

        private const string m_szConsulta = "pangya.ProcGetApproachMissions";
    }
}