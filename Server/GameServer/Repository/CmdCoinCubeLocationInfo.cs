using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using MAP_HOLE_COIN_CUBE = System.Collections.Generic.Dictionary<byte, System.Collections.Generic.List<Pangya_GameServer.Models.CubeEx>>;
namespace Pangya_GameServer.Repository
{
    public class CmdCoinCubeLocationInfo : Pangya_DB
    {
        public CmdCoinCubeLocationInfo()
        {
            this.m_coin_cube = new MAP_HOLE_COIN_CUBE();
            this.m_course = 0;
        }

        public CmdCoinCubeLocationInfo(byte _course)
        {
            this.m_coin_cube = new MAP_HOLE_COIN_CUBE();
            this.m_course = _course;
        }

        public byte getCourse()
        {
            return m_course;
        }

        public MAP_HOLE_COIN_CUBE getInfo()
        {
            return new MAP_HOLE_COIN_CUBE(m_coin_cube);
        }

        public void setCourse(byte _course)
        {
            m_course = _course;
        }

        public List<CubeEx> getAllCoinCubeHole(byte _hole_number)
        {

            List<CubeEx> v_coin_cube = new List<CubeEx>();

            var it = m_coin_cube.FirstOrDefault(c => c.Key == _hole_number);

            if (it.Key != 0 && it.Value != null)
            {
                v_coin_cube = it.Value;
            }
            return new List<CubeEx>(v_coin_cube);
        }
        protected override void lineResult(ctx_res _result, uint _index)
        {

            checkColumnNumber(9);

            uint course = IFNULL<uint>(_result.data[1]);
            byte hole = IFNULL<byte>(_result.data[2]);

            if (course != m_course)
            {
                throw new exception("[CmdCoinCubeLocationInfo::lineResult][Error] course retornado é diferento do requisitado[REQ=" + Convert.ToString((byte)m_course) + ", RET=" + Convert.ToString(course) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }

            CubeEx cube = new CubeEx(IFNULL<uint>(_result.data[0]),
                (Cube.eTYPE)(IFNULL<uint>(_result.data[3])),
                0u,
               (Cube.eFLAG_LOCATION)(IFNULL<uint>(_result.data[4])),
           IFNULL<float>(_result.data[6]),
                IFNULL<float>(_result.data[7]),
                IFNULL<float>(_result.data[8]),
                IFNULL<uint>(_result.data[5]));

            var it = m_coin_cube.Any(c => c.Key == hole);

            if (it)
            {
                m_coin_cube[hole].Add(cube);
            }
            else
            {

                m_coin_cube.Add(hole, new List<CubeEx>() { cube });

                if (!m_coin_cube.ContainsKey(hole))
                {
                    _smp.message_pool.getInstance().push(new message("[CmdCoinCubeLocationInfo::lineResult][Warning] nao conseguiu inserir hole[NUMBER=" + Convert.ToString((ushort)hole) + "] e cube no map<>", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        protected override Response prepareConsulta()
        {

            if (!m_coin_cube.empty())
            {
                m_coin_cube.Clear();
            }

            var r = consulta($"SELECT {makeEscapeKeyword("index")}, course, hole, tipo, tipo_location, rate, x, y, z FROM pangya.pangya_coin_cube_location WHERE course = {m_course} ORDER BY course, hole");

            checkResponse(r, "nao conseguiu pegar os coin, cube do course[ID=" + Convert.ToString((ushort)m_course) + "]");

            return r;
        }

        private byte m_course;
        private MAP_HOLE_COIN_CUBE m_coin_cube = new MAP_HOLE_COIN_CUBE();
    }
}