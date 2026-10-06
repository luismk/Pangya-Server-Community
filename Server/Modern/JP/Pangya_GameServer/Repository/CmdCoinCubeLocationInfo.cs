using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;
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
            this.Course = 0;
        }

        public CmdCoinCubeLocationInfo(byte _course)
        {
            this.m_coin_cube = new MAP_HOLE_COIN_CUBE();
            this.Course = _course;
        }
         
        public MAP_HOLE_COIN_CUBE getInfo()
        {
            return m_coin_cube;
        }
         

        protected override void lineResult(ctx_res _result, uint _index)
        {

            checkColumnNumber(9);

            uint course = IFNULL<uint>(_result.data[1]);
            byte hole = IFNULL<byte>(_result.data[2]);

            if (course != Course)
            {
                throw new exception("[CmdCoinCubeLocationInfo::lineResult][Error] CourseIndex retornado é diferento do requisitado[REQ=" + Convert.ToString((byte)Course) + ", RET=" + Convert.ToString(course) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
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
                    _smp.LogManager.Instance.push(new AppMessage("[CmdCoinCubeLocationInfo::lineResult][Warning] nao conseguiu inserir hole[NUMBER=" + Convert.ToString((ushort)hole) + "] e cube no map<>", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_coin_cube.Any())
            {
                m_coin_cube.Clear();
            }

            var r = consulta($"SELECT {makeEscapeKeyword("index")}, CourseIndex, hole, Type, tipo_location, Rate, X, Y, Z FROM pangya.pangya_coin_cube_location WHERE CourseIndex = {Course} ORDER BY CourseIndex, hole");

            checkResponse(r, "nao conseguiu pegar os coin, cube do CourseIndex[ID=" + Convert.ToString((ushort)Course) + "]");

            return r;
        }

        private byte Course;
        private MAP_HOLE_COIN_CUBE m_coin_cube = new MAP_HOLE_COIN_CUBE();
    }
}