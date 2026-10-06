using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase;

using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Pangya_GameServer.Models.DefineConstants;
using static Pangya_GameServer.ModelsGameInfo;
namespace Pangya_GameServer.Engine
{
    public class HoleManager : IDisposable
    {
        public HoleManager(byte _course,
            short _numero, byte _pin,
            RoomHoleType _modo, byte _hole_repeat,
            byte _weather, byte _wind,
            ushort _degree,
            uCubeCoinFlag _cube_coin)
        {
            this.Course = (byte)(_course & 0x7F);
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

            if (sIff.Instance.findCourse((uint)((sIff.Instance.COURSE << 26) | (Course & 0x7F))) == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Hole::Hole][Error] CourseIndex[" + Convert.ToString((ushort)Course) + "] desconhecido. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            if (m_numero < 1 || m_numero > 18)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Hole::init][Error] RoomID do hole[" + Convert.ToString(m_numero) + "] nao esta em um intervalo permitido. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            LoadCourse();

            // n�mero aleat�rio, para o Login do hole(ACHO)
            float rand_f = (float)((((int)Random.Shared.Next()) * 2.0f) * Random.Shared.Next());

            // Gerar n�meros grandes
            m_id = (uint)rand_f;
            // Se estiver ativado, inicializa o Coin Cube do Hole
            if (m_cube_coin.enable == 1 && (m_cube_coin.enable_cube == 1 || m_cube_coin.enable_coin == 1))
            {
                Init();
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
        public uint getId()
        {
            return m_id;
        }

        public short GetRoomId()
        {
            return m_numero;
        } 

        public stHoleWind getWind()
        {
            return m_wind;
        }

        public stHolePar getPar()
        {
            return m_par;
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
            return Course;
        }

        public uCubeCoinFlag getCubeCoin()
        {
            return m_cube_coin;
        }

        public RoomHoleType getModo()
        {
            return m_modo;
        }

        public byte getHoleRepeat()
        {
            return m_hole_repeat;
        }

        public Location getPinLocation()
        {
            return m_pin_location;
        }

        public Location getTeeLocation()
        {
            return m_tee_location;
        }

        public List<CubeEx> getCubes()
        {
            return m_cube;
        }

        // Set
        public void SetWeather(byte _weather)
        {
            m_weather = _weather;
        }

        public void SetWind(byte _wind, ushort _degree)
        {

            m_wind.wind = _wind;
            m_wind.degree.setDegree(_degree);
        }

        public void SetWind(stHoleWind _wind)
        {
            m_wind = _wind;
        }

        // Finders
        public CubeEx FindCubeCoin(uint _id)
        {

            var it = m_cube.FirstOrDefault(el =>
            {
                return el.id == _id;
            });

            return it;
        }

        private void Init()
        {

            // Cube ativo ou n�o
            bool cube = false;

            // Modo hole repeat, tem que pegar o n�mero certo do hole
            byte numero = (byte)m_numero;

            if (m_modo == RoomHoleType.M_REPEAT)
            {
                numero = m_hole_repeat;
            }

            // Cube Coin Manager
            if (!sCubeCoinSystem.Instance.isLoad())
            {
                sCubeCoinSystem.Instance.load();
            }

            var ID = sIff.Instance.COURSE << 26 | (Course & 0x7F);
            var course = sCubeCoinSystem.Instance.FindCourse((uint)ID);

            if (course == null)
            {
                throw new exception("[Hole::init_cube_coin][Error] CourseIndex\"" + Convert.ToString((ushort)(Course & 0x7F)) + "\" nao existe no Cube Coin System. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    20, 0));
            }

            // Isso s� desativa os cube, se o CourseIndex e hole tiver coin � para colocar elas s� n�o o cube se ele estiver desativado
            if (Course == (byte)RoomCourseFlags.WIZ_CITY) // Aqui s� tem cube nos holes 3 12 14 18
            {
                cube = (numero == 3 || numero == 12 || numero == 14 || numero == 18) && (m_modo != RoomHoleType.M_REPEAT || m_numero % 3 == 0); // Modo Hole Repeat s� de 3 em 3 holes que tem cube, mesmo em Wiz City
            }
            else
            {
                cube = m_cube_coin.enable_cube == 1;
            }

            var hole = course.FindHole(numero);

            if (hole == null)
            {
                throw new exception("[Hole::init_cube_coin][Error] RoomID do hole[NUMERO=" + Convert.ToString(m_numero) + "] is valid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    21, 0));
            } 

            //   Wiz City usa a fun��o dela e o resto usa outra fun��o generica
            var all_coin_cube = (Course == (byte)RoomCourseFlags.WIZ_CITY) ? hole.getAllCoinCubeWizCity(cube) : hole.getAllCoinCube(cube);
            
            m_cube.AddRange(all_coin_cube);
        }

        private void LoadCourse()
        {

            var course = sIff.Instance.findCourse((uint)((sIff.Instance.COURSE << 26) | (Course & 0x7F)));

            if (course == null)
            {
                throw new exception("[Hole::init_from_IFF_STRUCT][Error] CourseIndex[" + Convert.ToString((ushort)Course & 0x7F) + "] desconhecido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    2, 0));
            }

            var numero = m_numero;

            if (m_modo == RoomHoleType.M_REPEAT)
            {
                numero = m_hole_repeat;
            }

            if (numero < 1 || numero > 18)
            {
                throw new exception("[Hole::init_from_IFF_STRUCT][Error] RoomID do hole[" + Convert.ToString(m_numero) + "] nao esta em um intervalo permitido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.HOLE,
                    3, 0));
            }

            // !!!!@@@@@@------------===
            // Os Valores do Par dos Holes Mysthic Ruins est�o errados no IFF STRUCT,
            // eles colocaram os valores do Abbot Mine, tenho que trocar depois isso
            if ((course.ID & 0xFF) == (uint)RoomCourseFlags.CHRONICLE_1_CHAOS)
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

        private Location m_pin_location = new Location();
        private Location m_tee_location = new(); 
        private List<CubeEx> m_cube = []; 
        private uint m_id = new uint();
        private short m_numero; 
        private stHoleWind m_wind = new stHoleWind();
        private stHolePar m_par = new stHolePar();
        private byte m_pin;
        private byte m_weather;
        private byte Course;

        private uCubeCoinFlag m_cube_coin = new uCubeCoinFlag();

        private RoomHoleType m_modo;
        private byte m_hole_repeat;

        private bool m_good;
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