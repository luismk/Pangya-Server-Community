using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Feature.CubeCoinSystem;

namespace Pangya_GameServer.Repository
{
    public class CmdCoinCubeInfo : Pangya_DB
    {

        public CmdCoinCubeInfo()
        {
            _CourseCtx = new CourseCtx(0, false);
            this.Course_info = new Dictionary<byte, bool>();
        }

        public Dictionary<byte, bool> getInfo()
        {
            return Course_info;
        }

        protected override void lineResult(ctx_res _result, uint _index)
        {

            checkColumnNumber(2);

            byte course_id = (byte)IFNULL<uint>(_result.data[0]);
            bool active = IFNULL<uint>(_result.data[1]) == 1;  
            if (Course_info.Any(c => c.Key == course_id))
            {
                Course_info[course_id] = active;
             }
            else
            {

                Course_info.Add(course_id, active);

                if (!Course_info.ContainsKey(course_id))
                {
                    _smp.LogManager.Instance.push(new AppMessage("[CmdCoinCubeInfo::lineResult][Warning] nao conseguiu adicionar o CourseIndex[ID=" + Convert.ToString((ushort)course_id) + ", ACTIVE=" + Convert.ToString(active) + "] no map<>.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        protected override Response prepareConsulta()
        {

            if (Course_info.Count > 0)
            {
                Course_info.Clear();
            }

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar coin cube info dos CourseIndex");

            return r;
        }

        private Dictionary<byte, bool> Course_info = new Dictionary<byte, bool>();
        private CourseCtx _CourseCtx;
        private ConcurrentDictionary<uint, CourseCtx> CourseCtxes = new ConcurrentDictionary<uint, CourseCtx>();
        private const string m_szConsulta = "SELECT course_id, State FROM pangya.pangya_coin_cube_info";
    }
}