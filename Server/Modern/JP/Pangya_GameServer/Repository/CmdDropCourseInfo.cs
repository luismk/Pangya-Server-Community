using System.Collections.Generic;
using Pangya_GameServer.Feature;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDropCourseInfo : Pangya_DB
    {
        public CmdDropCourseInfo()
        {
            this.Course = new Dictionary<byte, DropSystem.stDropCourse>();
        }

        public Dictionary<byte, DropSystem.stDropCourse> getInfo()
        {
            return Course;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(9);



            DropSystem.stDropCourse dc = new DropSystem.stDropCourse();
            DropSystem.stDropCourse.stDropItem di = new DropSystem.stDropCourse.stDropItem();

            dc.course = (byte)IFNULL<uint>(_result.data[0]);

            di.tipo = (byte)IFNULL<uint>(_result.data[1]);
            di._typeid = IFNULL<uint>(_result.data[2]);
            di.qntd = IFNULL<uint>(_result.data[3]);

            for (var i = 0; i < 4; ++i)
            {
                di.probabilidade[i] = IFNULL<uint>(_result.data[4 + i]); // i + 4
            }

            di.active = (byte)IFNULL<uint>(_result.data[8]); // 4 + 4 = 8

            if (!Course.ContainsKey(dc.course))
            { // N�o tem cria um novo Drop Course

                dc.v_item.Add(di);

                Course.Add(dc.course, dc);
            }
            else // J� tem, adiciona o item ao CourseIndex
            {
                Course[dc.course].v_item.Add(di);
            }
        }

        protected override Response prepareConsulta()
        {

            if (Course.Any())
            {
                Course.Clear();
            }

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar os Drop Course");

            return r;
        }

        private Dictionary<byte, DropSystem.stDropCourse> Course = new Dictionary<byte, DropSystem.stDropCourse>();

        private const string m_szConsulta = "SELECT CourseIndex, Type, typeid, quantidade, probabilidade_3H, probabilidade_6H, probabilidade_9H, probabilidade_18H, State FROM pangya.pangya_new_course_drop_item WHERE State = 1";
    }
}