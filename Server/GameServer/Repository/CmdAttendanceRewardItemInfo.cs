using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdAttendanceRewardItemInfo : Pangya_DB
    {
        public CmdAttendanceRewardItemInfo()
        {
            v_item = new List<AttendanceRewardItemCtx>();
        }
        public List<AttendanceRewardItemCtx> getInfo()
        {
            return v_item;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(3);

            AttendanceRewardItemCtx aric = new AttendanceRewardItemCtx
            {
                _typeid = IFNULL<uint>(_result.data[0]),
                qntd = IFNULL<uint>(_result.data[1]),
                tipo = IFNULL<byte>(_result.data[2])
            };

            v_item.Add(aric);
        }

        protected override Response prepareConsulta()
        {

            if (v_item.Count > 0)
            {
                v_item.Clear();
            }

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar os Itens do Attendance Reward");

            return r;
        }


        private List<AttendanceRewardItemCtx> v_item = new List<AttendanceRewardItemCtx>();

        private const string m_szConsulta = "SELECT typeid, quantidade, tipo FROM pangya.pangya_attendance_table_item_reward";

    }
}