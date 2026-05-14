using System.Collections.Generic;
using Pangya_GameServer.Game.System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDropCourseInfo : Pangya_DB
    {
        public CmdDropCourseInfo()
        {
            this.m_course = new Dictionary<byte, DropSystem.stDropCourse>();
        }

        public Dictionary<byte, DropSystem.stDropCourse> getInfo()
        {
            return m_course;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(9);



            DropSystem.stDropCourse dc = new DropSystem.stDropCourse();
            DropSystem.stDropCourse.stDropItem di = new DropSystem.stDropCourse.stDropItem();

            dc.course = (byte)IFNULL(_result.data[0]);

            di.tipo = (byte)IFNULL(_result.data[1]);
            di._typeid = IFNULL(_result.data[2]);
            di.qntd = IFNULL(_result.data[3]);

            for (var i = 0; i < 4; ++i)
            {
                di.probabilidade[i] = IFNULL(_result.data[4 + i]); // i + 4
            }

            di.active = (byte)IFNULL(_result.data[8]); // 4 + 4 = 8

            if (!m_course.ContainsKey(dc.course))
            { // N�o tem cria um novo Drop Course

                dc.v_item.Add(di);

                m_course.Add(dc.course, dc);
            }
            else // J� tem, adiciona o item ao course
            {
                m_course[dc.course].v_item.Add(di);
            }
        }

        protected override Response prepareConsulta()
        {

            if (!m_course.empty())
            {
                m_course.Clear();
            }

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar os Drop Course");

            return r;
        }

        private Dictionary<byte, DropSystem.stDropCourse> m_course = new Dictionary<byte, DropSystem.stDropCourse>();

        private const string m_szConsulta = "SELECT course, tipo, typeid, quantidade, probabilidade_3H, probabilidade_6H, probabilidade_9H, probabilidade_18H, active FROM pangya.pangya_new_course_drop_item WHERE active = 1";
    }
}