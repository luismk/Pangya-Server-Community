using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdTreasureHunterInfo : Pangya_DB
    {
        public CmdTreasureHunterInfo()
        {
            this.v_thi = new List<TreasureHunterInfo>();
        }

        protected override void lineResult(ctx_res _result, uint _index)
        {

            checkColumnNumber(2);

            TreasureHunterInfo thi = new TreasureHunterInfo
            {
                course = IFNULL<sbyte>(_result.data[0]),
                point = IFNULL<int>(_result.data[1])
            }; // treasure hunter info

            v_thi.Add(thi);
        }
        protected override Response prepareConsulta()
        {

            v_thi.Clear();
            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar Treasure Hunter do server");

            return r;
        }
        public List<TreasureHunterInfo> getInfo()
        {
            return v_thi;
        }

        List<TreasureHunterInfo> v_thi;          // Treasure Hunter Info = THI

        string m_szConsulta = "pangya.ProcGetCourseReward";
    }
}