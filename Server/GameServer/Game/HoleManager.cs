using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.RoomInfo;

using uint32_t = System.UInt32;

namespace Pangya_GameServer.Game
{
    public class HoleManager : IDisposable
    {
        public HoleManager(byte _course,
            ushort _numero, byte _pin,
            ROOM_INFO_MODO _modo, byte _hole_repeat,
            byte _weather, byte _wind,
            ushort _degree,
            uCubeCoinFlag _cube_coin)
        {
            this.m_course = (byte)(_course & 0x7F);
            this.m_numero = _numero;
            this.m_pin = _pin;
            this.m_modo = (_modo);
            this.m_hole_repeat = _hole_repeat;
            this.m_weather = _weather;
            this.m_wind = new stHoleWind(_wind, _degree);
            this.m_cube_coin = _cube_coin;
            this.m_par = new stHolePar();
            this.m_cube = new List<CubeEx>();
            this.m_good = false;

            if (sIff.getInstance().findCourse((uint)((sIff.getInstance().COURSE << 26) | (m_course & 0x7F))) == null)
            {
                _smp.message_pool.getInstance().push(new message("[Hole::Hole][Error] course[" + Convert.ToString((ushort)m_course) + "] desconhecido. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            if (m_numero < 1 || m_numero > 18)
            {
                _smp.message_pool.getInstance().push(new message("[Hole::init][Error] numero do hole[" + Convert.ToString(m_numero) + "] nao esta em um intervalo permitido. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            init_from_IFF_STRUCT();

            // n�mero aleat�rio, para o id do hole(ACHO)
            float rand_f = (float)((((int)new Random().Next()) * 2.0f) * new Random().Next());

            // Gerar n�meros grandes
            m_id = (uint)rand_f;
            // Se estiver ativado, inicializa o Coin Cube do Hole
            if (m_cube_coin.enable == 1 && (m_cube_coin.enable_cube == 1 || m_cube_coin.enable_coin == 1))
            {
                init_cube_coin();
            }
            m_cube_coin = new uCubeCoinFlag();
            m_good = true;
        }

        ~HoleManager()
        {

            if (m_cube.Count > 0)
            {
                m_cube.Clear();
            }

            m_good = false;
        }

        public void init(stXZLocation _tee, stXZLocation _pin)
        {

            Location tee = new Location(_tee.x,
                0.0f, _tee.z, 0.0f);
            Location pin = new Location(_pin.x,
                0.0f, _pin.z, 0.0f);

            init(tee, pin);
        }

        public void init(Location _tee, Location _pin)
        {

            if (!isGood())
            {
                throw new exception("[Hole::init][Error] hole nao esta incializado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    1, 0));
            }

            m_tee_location = _tee;
            m_pin_location = _pin;
        }

        public bool isGood()
        {
            return m_good;
        }

        // Get
        public uint32_t getId()
        {
            return (m_id);
        }

        public ushort getNumero()
        {
            return m_numero;
        }

        public byte getTipo()
        {
            return m_tipo;
        }

        public stHoleWind getWind()
        {
            return (m_wind);
        }

        public stHolePar getPar()
        {
            return (m_par);
        }

        public byte getPin()
        {
            return m_pin;
        }

        public byte getWeather()
        {
            return m_weather;
        }

        public uint getCourse()
        {
            return m_course;
        }

        public uCubeCoinFlag getCubeCoin()
        {
            return (m_cube_coin);
        }

        public ROOM_INFO_MODO getModo()
        {
            return m_modo;
        }

        public byte getHoleRepeat()
        {
            return m_hole_repeat;
        }

        public Location getPinLocation()
        {
            return (m_pin_location);
        }

        public Location getTeeLocation()
        {
            return (m_tee_location);
        }

        public List<CubeEx> getCubes()
        {
            return new List<CubeEx>(m_cube);
        }

        // Set
        public void setWeather(byte _weather)
        {
            m_weather = _weather;
        }

        public void setWind(byte _wind, ushort _degree)
        {

            m_wind.wind = _wind;
            m_wind.degree.setDegree(_degree);
        }

        public void setWind(stHoleWind _wind)
        {
            m_wind = _wind;
        }

        // Finders
        public CubeEx findCubeCoin(uint32_t _id)
        {

            var it = m_cube.Where(el =>
            {
                return el.id == _id;
            });

            return (it.Any()) ? it.FirstOrDefault() : null;
        }

        protected void init_cube_coin()
        {

            // Cube ativo ou n�o
            bool cube = false;

            // Modo hole repeat, tem que pegar o n�mero certo do hole
            byte numero = (byte)m_numero;

            if (m_modo == ROOM_INFO_MODO.M_REPEAT)
            {
                numero = m_hole_repeat;
            }

            // Cube Coin Manager
            if (!sCubeCoinSystem.getInstance().isLoad())
            {
                sCubeCoinSystem.getInstance().load();
            }

            var course = sCubeCoinSystem.getInstance().FindCourse((uint)((sIff.getInstance().COURSE << 26) | (m_course & 0x7F)));

            if (course == null)
            {
                throw new exception("[Hole::init_cube_coin][Error] course\"" + Convert.ToString((ushort)(m_course & 0x7F)) + "\" nao existe no Cube Coin System. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    20, 0));
            }
            if (m_course == (byte)RoomInfo.ROOM_INFO_COURSE.WIZ_CITY) // Aqui s� tem cube nos holes 3 12 14 18
            {
                cube = (numero == 3 || numero == 12 || numero == 14 || numero == 18) && (m_modo != ROOM_INFO_MODO.M_REPEAT || m_numero % 3 == 0); // Modo Hole Repeat s� de 3 em 3 holes que tem cube, mesmo em Wiz City
            }
            else
            {
                cube = m_cube_coin.enable_cube == 1u;
            }

            var hole = course.FindHole(numero);

            if (hole == null)
            {
                throw new exception("[Hole::init_cube_coin][Error] numero do hole[NUMERO=" + Convert.ToString(m_numero) + "] is valid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    21, 0));
            }

            //   Wiz City usa a fun��o dela e o resto usa outra fun��o generica
            var all_coin_cube = (m_course == (byte)RoomInfo.ROOM_INFO_COURSE.WIZ_CITY) ? hole.getAllCoinCubeWizCity(cube) : hole.getAllCoinCube(cube);

            m_cube.AddRange(
            all_coin_cube);
        }

        protected void init_from_IFF_STRUCT()
        {

            var course = sIff.getInstance().findCourse((uint)((sIff.getInstance().COURSE << 26) | (m_course & 0x7F)));

            if (course == null)
            {
                throw new exception("[Hole::init_from_IFF_STRUCT][Error] course[" + Convert.ToString((ushort)m_course & 0x7F) + "] desconhecido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    2, 0));
            }

            var numero = m_numero;

            if (m_modo == ROOM_INFO_MODO.M_REPEAT)
            {
                numero = m_hole_repeat;
            }

            if (numero < 1 || numero > 18)
            {
                throw new exception("[Hole::init_from_IFF_STRUCT][Error] numero do hole[" + Convert.ToString(m_numero) + "] nao esta em um intervalo permitido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    3, 0));
            }

            // !!!!@@@@@@------------===
            // Os Valores do Par dos Holes Mysthic Ruins est�o errados no IFF STRUCT,
            // eles colocaram os valores do Abbot Mine, tenho que trocar depois isso
            if ((course.ID & 0xFF) == (uint)RoomInfo.ROOM_INFO_COURSE.CHRONICLE_1_CHAOS)
            {
                m_par.par = 4;

                m_par.range_score[0] = -2;
                m_par.range_score[1] = 5;

                m_par.total_shot = (sbyte)(m_par.par + m_par.range_score[1]);
            }
            else
            {
                m_par.par = course.Par_Hole[numero - 1];

                m_par.range_score[0] = (sbyte)course.Min_Score_Hole[numero - 1];
                m_par.range_score[1] = (sbyte)course.Max_Score_Hole[numero - 1];

                m_par.total_shot = (sbyte)(m_par.par + m_par.range_score[1]);
            }
        }

        protected Location m_pin_location = new Location();
        protected Location m_tee_location = new Location();

        protected List<CubeEx> m_cube = new List<CubeEx>();

        protected uint32_t m_id = new uint32_t();
        protected ushort m_numero;
        protected byte m_tipo;
        protected stHoleWind m_wind = new stHoleWind();
        protected stHolePar m_par = new stHolePar();
        protected byte m_pin;
        protected byte m_weather;
        protected byte m_course;

        protected uCubeCoinFlag m_cube_coin = new uCubeCoinFlag();

        protected ROOM_INFO_MODO m_modo;
        protected byte m_hole_repeat;

        protected bool m_good;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_cube.Clear();
                    m_good = false;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}