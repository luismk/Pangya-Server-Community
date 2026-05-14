using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;
using System.Globalization;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCoinCubeLocation : Pangya_DB
    {

        public CmdUpdateCoinCubeLocation()
        {
            this.m_ccu = new CoinCubeUpdate();
        }

        public CmdUpdateCoinCubeLocation(CoinCubeUpdate _ccu)
        {
            this.m_ccu = _ccu;
        }

        public CoinCubeUpdate getInfo()
        {
            return m_ccu;
        }

        public void setInfo(CoinCubeUpdate _ccu)
        {
            m_ccu = _ccu;
        }

        protected override void lineResult(ctx_res _result, uint _index)
        {
            // N�o usa por que � UPDATE e INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_ccu.hole_number < 1 || m_ccu.hole_number > 18)
            {
                throw new exception("[CmdUpdateCoinCubeLocation::prepareConsulta][Error] m_ccu.hole_number(" + Convert.ToString((ushort)m_ccu.hole_number) + ") invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            // Prote��o contra os jogos random & 0x7F
            if (sIff.getInstance().findCourse(((Convert.ToUInt32(sIff.getInstance().COURSE << 0x1A)) | (m_ccu.course_id & 0x7Fu))) == null)
            {
                throw new exception("[CmdUpdateCoinCubeLocation::prepareConsulta][Error] m_ccu.course_id(" + Convert.ToString((ushort)m_ccu.course_id) + ") not exists in IFF_STRUCT", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            Response r = null;
            string x = m_ccu.cube.location.x.ToString(CultureInfo.InvariantCulture);
            string y = m_ccu.cube.location.y.ToString(CultureInfo.InvariantCulture);
            string z = m_ccu.cube.location.z.ToString(CultureInfo.InvariantCulture);

            string args = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                (ushort)m_ccu.course_id,
                (ushort)m_ccu.hole_number,
                (int)m_ccu.cube.tipo,
                (int)m_ccu.cube.flag_location,
                m_ccu.cube.rate,
                x, y, z);

            if (m_ccu.type == CoinCubeUpdate.eTYPE.UPDATE)
            {

                if (m_ccu.cube.id == 0u)
                {
                    throw new exception("[CmdUpdateCoinCubeLocation::prepareConsulta][Error] invalid coin/cube id(" + Convert.ToString(m_ccu.cube.id) + ") to Update in Database", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                        4, 0));
                }
              

                r = procedure(m_szConsulta[1],
                    Convert.ToString(m_ccu.cube.id) + ", " + args);

                checkResponse(r, "Nao conseguiu atualizar o Coin/Cube[ID=" + Convert.ToString(m_ccu.cube.id) + ", COURSE_ID=" + Convert.ToString((ushort)m_ccu.course_id) + ", HOLE=" + Convert.ToString((ushort)m_ccu.hole_number) + ", TIPO=" + Convert.ToString(m_ccu.cube.tipo) + ", TIPO_LOCATION=" + Convert.ToString(m_ccu.cube.flag_location) + ", RATE=" + Convert.ToString(m_ccu.cube.rate) + ", X=" + Convert.ToString(m_ccu.cube.location.x) + ", Y=" + Convert.ToString(m_ccu.cube.location.y) + ", Z=" + Convert.ToString(m_ccu.cube.location.z) + "]");

            }
            else
            { 
                r = procedure(m_szConsulta[0], args);

                checkResponse(r, "Nao conseguiu adicionar o Coin/Cube[COURSE_ID=" + Convert.ToString((ushort)m_ccu.course_id) + ", HOLE=" + Convert.ToString((ushort)m_ccu.hole_number) + ", TIPO=" + Convert.ToString(m_ccu.cube.tipo) + ", TIPO_LOCATION=" + Convert.ToString(m_ccu.cube.flag_location) + ", RATE=" + Convert.ToString(m_ccu.cube.rate) + ", X=" + Convert.ToString(m_ccu.cube.location.x) + ", Y=" + Convert.ToString(m_ccu.cube.location.y) + ", Z=" + Convert.ToString(m_ccu.cube.location.z) + "]");
            }

            return r;
        }

        private CoinCubeUpdate m_ccu = new CoinCubeUpdate();

        private string[] m_szConsulta = { "pangya.ProcInsertCoinCubeLocation", "pangya.ProcUpdateCoinCubeLocation" };
    }
}
