using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
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
    public class CourseManager : IDisposable
    {

        protected Dictionary<short, HoleManager> m_hole = new Dictionary<short, HoleManager>();
        protected List<Sequencia> m_seq = new List<Sequencia>();

        protected bool m_channel_rookie;
        protected uint m_rate_rain = 0;
        protected byte m_rain_persist_flag;

        protected float m_star;

        protected short[] m_wind_range = new short[2];
        protected short m_wind_flag;

        protected int m_seed_rand_game = new int();

        protected GameRoomInfoModel m_ri;

        protected HolesRain m_holes_rain = new HolesRain(); // N�mero de holes que est� chovendo no CourseIndex
        protected ConsecutivosHolesRain m_chr = new ConsecutivosHolesRain(); // N�mero de Rain em holes consecutivos, 2, 3 e 4+

        protected bool m_grand_prix_special_hole; // ServerFlag de Special hole Grand Prix, true tem Special hole, false n�o tem

        private short m_flag_cube_coin; // 1 Tem Cube e Coin, 0 sem
        private bool disposedValue;
        public CourseManager(GameRoomInfoModel _ri,
            bool _channel_rookie,
            float _star,
            uint _rate_rain,
            byte _rain_persist_flag)
        {
            this.m_ri = _ri;
            this.m_channel_rookie = _channel_rookie;
            this.m_star = _star;
            this.m_rate_rain = _rate_rain;
            this.m_rain_persist_flag = _rain_persist_flag;
            this.m_hole = new Dictionary<short, HoleManager>();
            this.m_seq = new List<Sequencia>();
            this.m_seed_rand_game = 0;
            this.m_flag_cube_coin = 1;
            this.m_wind_flag = 0;
            this.m_wind_range = new short[9];
            this.m_chr = new ConsecutivosHolesRain();
            this.m_holes_rain = new HolesRain();
            this.m_grand_prix_special_hole = false;

            init_seq();

            init_hole();

            init_dados_rain(); // Inicializar os dados de Rain no CourseIndex, para ser usado no achievement

            // Deixa esse s� com int16(short), por que s� vejo n�mero baixo, n�o passa do valor m�ximo do int16
            m_seed_rand_game = Random.Shared.Next(1, short.MaxValue);
        }

        // Get
        public int getSeedRandGame()
        {
            return m_seed_rand_game;
        }

        public short getFlagCubeCoin()
        {
            return m_flag_cube_coin;
        }

        public float getStar()
        {
            return m_star;
        }

        /// Finders

        // Find Hole, se n�o achar retorna um ponteiro nulo
        public HoleManager findHole(short _number)
        {
            if (_number < 0)
                return null;

            foreach (var it in m_hole)
            {
                if (it.Value.GetRoomId() == _number)
                    return it.Value;
            }

            return null;
        }

        public HoleManager findHoleBySeq(short _seq)
        {

            if ((short)_seq <= 0 || _seq > m_hole.Count)
            {
                return null;
            }

            var it = m_hole.FirstOrDefault(c => c.Key == _seq);

            if (it.Value == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Course::findHoleBySeq][WARNIG] nao encontrou a seq[value=" + Convert.ToString(_seq) + "] no map de hole. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return it.Value;
        }

        // Find Hole Sequ�ncia
        public short findHoleSeq(short _number)
        {
            if (_number < 0)
                return short.MaxValue; // Erro

            foreach (var it in m_hole)
            {
                if (it.Value.GetRoomId() == _number)
                    return it.Key;
            }

            return -1; // Não encontrado
        }

        // Find intervalo de hole do n�mero fornecido at� o ultimo do map
        public IEnumerable<KeyValuePair<short, HoleManager>> findRange(short _number)
        {
            if (_number >= 0)
                return m_hole.Where(kv => kv.Value.GetRoomId() == _number);

            return Enumerable.Empty<KeyValuePair<short, HoleManager>>();
        }



        // Random Wind and Degree
        public stHoleWind shuffleWind(int _seed = 777)
        {
            stHoleWind wind = new stHoleWind();

            Random rand = new Random(_seed);  // Use sempre a mesma seed para consistência

            if (m_wind_flag != 0)
            {
                do
                {
                    wind.wind = (byte)(m_wind_range[0] + rand.Next(m_wind_range[1] - m_wind_range[0] + 1));
                } while (m_wind_flag == 2 ? ((wind.wind + 1) % 2 == 1) : ((wind.wind + 1) % 2 == 0));
            }
            else
            {
                wind.wind = (byte)(m_wind_range[0] + rand.Next(m_wind_range[1] - m_wind_range[0] + 1));
            }

            wind.degree.setDegree((byte)(rand.Next(LIMIT_DEGREE)));

            return wind;
        }


        // Random wind next hole(s)
        public void shuffleWindNextHole(short _number)
        {

            if ((short)_number < 0)
            {
                throw new exception("[Course::shuffleWindNextHole][Error] _number[VALUE=" + Convert.ToString((short)_number) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COURSE,
                    1, 0));
            }


            var it = m_hole.FirstOrDefault(_el =>
            {
                return _el.Value.GetRoomId() == _number;
            });

            if (it.Value == null)
            {
                throw new exception("[Course::shuffleWindNextHole][Error] nao conseguiu encontrar o hole[NUMERO=" + Convert.ToString(_number) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COURSE,
                    2, 0));
            }

            var wind = shuffleWind();

            foreach (var _it in m_hole)
            {
                _it.Value.SetWind(wind);
            }
        }

        // Make Packet Buffer Hole(s) Info
        public void makePacketHoleInfo(Packet _p, int _option = 0)
        {

            // Hole(s) Info
            foreach (var el in m_hole)
            {
                _p.WriteUInt32(el.Value.getId());
                _p.WriteByte(el.Value.getPin());

                if (_option == 0)
                    _p.WriteByte((int)el.Value.getCourse());

                _p.WriteByte((byte)el.Value.GetRoomId());
            }
            // Course Seed Random
            _p.WriteInt32(m_seed_rand_game);

            // Hole(s) Spinning Cube / Coin Info
            makePacketHoleSpinningCubeInfo(_p);
        }

        // Make Packet Buffer Hole(s) Spinning Cube(s) Info
        public void makePacketHoleSpinningCubeInfo(Packet _p)
        {

            foreach (var el in m_hole)
            {

                _p.WriteByte((byte)el.Value.getCubes().Count); // Size

                foreach (var el2 in el.Value.getCubes())
                {
                    _p.WriteUInt32((uint)el2.tipo);
                    _p.WriteUInt32(el2.id);
                    _p.WriteUInt32(el2.flag_unknown);
                    _p.WriteUInt32(el.Value.getCourse());
                    _p.WriteByte((byte)(el.Value.getModo() == RoomHoleType.M_REPEAT ? el.Value.getHoleRepeat() : el.Value.GetRoomId()));
                    _p.WriteByte(el.Key - 1); // Index
                    _p.WriteInt16(m_flag_cube_coin);
                    _p.Write(el2.location.x);//float
                    _p.Write(el2.location.y);//float
                    _p.Write(el2.location.z);//float
                    _p.WriteUInt32((uint)el2.flag_location);
                }
            }
        }

        public uint countHolesRain()
        {
            return m_holes_rain.getCountHolesRain();
        }

        public uint countHolesRainBySeq(uint _seq)
        {
            return m_holes_rain.getCountHolesRainBySeq(_seq);
        }

        // retorna Media de tacadas do CourseIndex para fazer par em todos os holes
        public float getMediaAllParHoles()
        {

            if (!m_hole.Any())
            {
                return 1.0f; // N�o tem nenhum hole inicializado
            }

            int count = 0;

            foreach (var el in m_hole)
            {
                count += el.Value.getPar().par;
            }

            return (float)(count / (float)m_hole.Count);
        }

        public float getMediaAllParHolesBySeq(uint _seq)
        {

            if (_seq <= 0 || _seq > m_hole.Count)
            {
                return 1.0f; // Sequ�ncia inv�lida
            }

            if (!m_hole.Any())
            {
                return 1.0f; // N�o tem nenhum hole inicializado
            }

            int count = 0;

            foreach (var it in m_hole)
            {
                if (it.Key > _seq)
                    break;

                count += it.Value.getPar().par;
            }

            return (float)(count / (float)_seq);
        }

        public ConsecutivosHolesRain getConsecutivesHolesRain()
        {
            return m_chr;
        }
        protected void init_seq()
        {
            // Grand Prix Special Hole
            if (m_ri.grand_prix.active == 1 && m_ri.grand_prix.dados_typeid > 0)
            {
                var sh = sIff.Instance.findGrandPrixSpecialHole(m_ri.grand_prix.rank_typeid);
                
                if (sh.Any())
                {
                    // Ordena do menor para o maior por Hole
                    sh.Sort((a, b) => a.Hole.CompareTo(b.Hole));

                    foreach (var el in sh)
                        m_seq.Add(new Sequencia((byte)el.Map, (short)el.Hole));

                    // Completa até 18
                    for (short i = (short)(m_seq.Count + 1); i <= 18; i++)
                        m_seq.Add(new Sequencia((byte)m_ri.CourseIndex, i));

                    m_grand_prix_special_hole = true;
                    return;
                }
            }

            // Funções auxiliares
            void AddSequence(short start, int end)
            {
                for (short i = start; i <= end; i++)
                    m_seq.Add(new Sequencia(i));
            }

            List<short> Shuffle18()
            {
                var list = Enumerable.Range(1, 18).Select(i => (short)i).ToList();
                
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Random.Shared.Next(0, i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                return list;
            }

            // Normal modes
            switch (m_ri.GetHoleType())
            {
                case RoomHoleType.M_FRONT:
                case RoomHoleType.M_REPEAT:
                    AddSequence(1, 18);
                    break;

                case RoomHoleType.M_BACK:
                    AddSequence(10, 18);
                    AddSequence(1, 9);
                    break;

                case RoomHoleType.M_RANDOM:
                    {
                        short rand = (short)Random.Shared.Next(1, 18); // 1 a 17
                        for (int i = 0; i < 18; i++)
                            m_seq.Add(new Sequencia((short)((rand + i - 1) % 18 + 1)));
                    }
                    break;

                case RoomHoleType.M_SHUFFLE:
                    foreach (var v in Shuffle18())
                        m_seq.Add(new Sequencia(v));
                    break;

                case RoomHoleType.M_SHUFFLE_COURSE:
                    {

                        short hole_ssc = (short)(Random.Shared.Next(2) + 1); // 1 ou 2
                        var shuffled = Shuffle18().Where(v => v != hole_ssc).ToList();
                        foreach (var v in shuffled)
                            m_seq.Add(new Sequencia(v));
                        m_seq.Add(new Sequencia(hole_ssc)); // último hole SSC
                    }
                    break;
            }
        }


        protected void init_hole()
        {
            try
            {

                uCubeCoinFlag cube_coin = new uCubeCoinFlag();

                // Enable Coin e Cube in Course Default
                cube_coin.enable = 1;
                cube_coin.enable_coin = 1;

                // Type Cube Game Mode
                if (m_ri.GetHoleType() == RoomHoleType.M_REPEAT)
                {
                    cube_coin.type = 1;
                }
                else if (m_ri.GetRoomType() == RoomTypeFlags.STROKE || m_ri.GetRoomType() == RoomTypeFlags.TOURNEY || m_ri.GetRoomType() == RoomTypeFlags.GUILD_BATTLE || m_ri.GetRoomType() == RoomTypeFlags.MATCH || m_ri.GetRoomType() == RoomTypeFlags.PRACTICE || m_ri.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
                {
                    cube_coin.type = 2;
                }

                switch (m_ri.ItemIDArtifact)
                {
                    case ORCHID_BLOSSOM_ART: // 1 a 8m
                        m_wind_range[1] = 8;
                        break;
                    case PENNE_ABACUS_ART: // Wind Impar
                        m_wind_flag = 1;
                        break;
                    case TITAN_WINDMILL_ART: // Wind Par
                        m_wind_flag = 2;
                        break;
                }

                if (m_ri.grand_prix.active == 1 && m_ri.grand_prix.dados_typeid > 0)
                {

                    // Grand Prix n�o tem cube
                    cube_coin.enable = 0;

                    try
                    {

                        var gp = sIff.Instance.findGrandPrixData(m_ri.grand_prix.dados_typeid);

                        // Grand Prix Data -> Rule
                        if (gp != null)
                        {

                            // Aqui inicializa as regras do Grand Prix de vento
                            switch (gp.rule)
                            {
                                case ONLY_1M_RULE:
                                    m_wind_range[1] = 1;
                                    break;
                                case SUPER_WIND_RULE:
                                    m_wind_range[0] = 9;
                                    m_wind_range[1] = 15;
                                    break;
                                case HOLE_CUP_MAGNET_RULE: // Ainda n�o sei esses aqui, como funciona
                                case NO_TURNING_BACK_RULE: // Ainda n�o sei esses aqui, como funciona
                                    break;
                                case WIND_3M_A_5M_RULE:
                                    m_wind_range[0] = 2;
                                    m_wind_range[1] = 5;
                                    break;
                                case WIND_7M_A_9M_RULE:
                                    m_wind_range[0] = 6;
                                    break;
                            }

                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Course::init_hole][Error] tentou pegar o Grand Prix[TYPEID=" + Convert.ToString(m_ri.grand_prix.dados_typeid) + "] no IFF_STRUCT do server mais ele nao existe. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[Course::init_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else if (m_channel_rookie)
                {
                    m_wind_range[1] = 5;
                }

                byte new_course = (byte)((byte)m_ri.CourseIndex & 0x7F);
                byte pin = 0;
                byte weather = 0;

                byte persist_rain = 0;

                stHoleWind wind = new stHoleWind();

                // Lottery Wind
                LotterySystem loterry = new LotterySystem();

                var rate_good_weather = (m_rate_rain <= 0) ? 1000 : ((m_rate_rain < 1000) ? 1000 - m_rate_rain : 1);

                // Coloquei 4 pra 1, antes estava 3 pra 1
                loterry.Add(rate_good_weather, 0);
                loterry.Add(rate_good_weather, 0);
                loterry.Add(rate_good_weather, 0);
                loterry.Add(rate_good_weather, 0);
                loterry.Add(m_rate_rain, 2);

                // Lottery Course
                LotterySystem lottery_map = new LotterySystem();

                byte course_id = 0;

                foreach (var el in sIff.Instance.getCourse())
                {

                    course_id = (byte)sIff.Instance.getItemIdentify(el.ID);

                    if (course_id != 17 && course_id != 0x40)
                    {
                        lottery_map.Add(100, course_id);
                    }
                }

                for (short i = 1; i <= 18; ++i)
                {

                    // Reseta type cube
                    cube_coin.enable_cube = 0;
                    cube_coin.enable_coin = 0;

                    if (i <= m_ri.HoleCount)
                    {

                        if (m_ri.HoleMode == (int)RoomHoleType.M_REPEAT && i == 1)
                        {
                            wind = shuffleWind(i);
                        }
                        else if (m_ri.HoleMode != (int)RoomHoleType.M_REPEAT)
                        {
                            wind = shuffleWind(i);
                        }

                        if (m_ri.HoleFixed == 7 && i == 1)
                        {
                            pin = (byte)(Random.Shared.Next() % 3);
                        }
                        else if (m_ri.HoleFixed != 7)
                        {
                            pin = (byte)(Random.Shared.Next() % 3);
                        }

                        weather = 0;

                        var lc = loterry.SpinRoleta();

                        if (lc?.Value != null && Convert.ToInt32(lc.Value) != 0)
                        {
                            weather = Convert.ToByte(lc.Value);
                        }

                        if (persist_rain != 0 || weather == 2)
                        {

                            if (persist_rain == 0
                                && weather == 2
                                && m_rain_persist_flag == 1)
                            {
                                persist_rain = 1;
                            }
                            else if (persist_rain == 1)
                            {
                                weather = 2;
                                persist_rain = 0;
                            }

                            try
                            {
                                if (i > 1 && m_hole[(short)(i - 1)].getWeather() == 0)
                                {
                                    m_hole[(short)(i - 1)].SetWeather(1);
                                }

                            }
                            catch (IndexOutOfRangeException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }

                        if (m_ri.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && m_ri.GetHoleType() == RoomHoleType.M_SHUFFLE_COURSE)
                        {

                            if (i == 18) // Ultimo Hole � do SSC
                            {
                                new_course = (byte)RoomCourseFlags.CHRONICLE_1_CHAOS;
                            }
                            else
                            {

                                lc = lottery_map.SpinRoleta();

                                if (lc != null && lc.Value != null)
                                {
                                    new_course = Convert.ToByte(lc.Value);
                                }
                            }
                        }

                        // Cube a cada 3 hole
                        if (i % 3 == 0)
                        {
                            cube_coin.enable_cube = 1;
                        }

                        // Coin todos os holes
                        if (cube_coin.enable == 1)
                        {
                            cube_coin.enable_coin = 1;
                        }

                        if (m_ri.grand_prix.active == 1
                            && m_ri.grand_prix.dados_typeid > 0
                            && m_grand_prix_special_hole)
                        {

                            // A fun��o init_seq j� inicializa a sequ�ncia se for Grand Prix e se ele tiver Special Hole
                            m_hole.Add(i, new HoleManager(m_seq[(short)(i - 1)].Course,
                                m_seq[(i - 1)].m_hole, pin,
                               (RoomHoleType)(m_ri.HoleMode),
                                m_ri.IDHoleRepeted,
                                weather, wind.wind,
                             wind.degree.getDegree(),
                                cube_coin));

                        }
                        else
                        {
                            m_hole.Add(i, new HoleManager(new_course,
                                m_seq[(i - 1)].m_hole, pin,
                               (RoomHoleType)(m_ri.HoleMode),
                                m_ri.IDHoleRepeted,
                                    weather, wind.wind,
                                    wind.degree.getDegree(),
                                cube_coin));
                        }

                    }
                    else
                    {
                        m_hole.Add(i, new HoleManager(new_course,
                            m_seq[(short)(i - 1)].m_hole,
                            (byte)(Random.Shared.Next() % 3),
                            (RoomHoleType)(m_ri.HoleMode),
                            m_ri.IDHoleRepeted,
                            weather, wind.wind,
                           wind.degree.getDegree(),
                            cube_coin)); 
                    }
                }
            }
            catch (Exception e)
            {
                throw; //e;e;
            }

        }

        protected void init_dados_rain()
        {
            try
            {
                // Inicializa dados de Rain em holes consecutivos
                m_chr.clear();

                // Inicializa dados do n�mero de holes com Rain
                m_holes_rain.clear();

                uint count = 0;

                foreach (var el in m_hole)
                {

                    // Quantidade de holes que tem o Game
                    if (el.Key <= m_ri.HoleCount)
                    {

                        if (el.Value.getWeather() == 2)
                        {

                            // Chuva
                            m_holes_rain.setRain((uint)(el.Key - 1), 1);

                            count++;
                        }

                        // �ltimo hole ou acabou a sequ�ncia de Rain consecutivas
                        if (count > 1u && (el.Value.getWeather() != 2 || el.Key == m_ri.HoleCount))
                        {

                            if (count >= 4) // 4 ou mais Holes consecutivos
                            {
                                m_chr._4_pluss_count.setRain((uint)(el.Key - 1), 1);
                            }
                            else if (count == 3) // 3 Holes consecutivos
                            {
                                m_chr._3_count.setRain((uint)(el.Key - 1), 1);
                            }
                            else // 2 Holes consecutivos
                            {
                                m_chr._2_count.setRain((uint)(el.Key - 1), 1);
                            }

                            // Zera
                            count = 0;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw; //e;e;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_hole.Clear();
                    m_seq.Clear();
                }

                // TODO: liberar recursos não gerenciados (objetos não gerenciados) e substituir o finalizador
                // TODO: definir campos grandes como nulos
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        public class Sequencia
        {
            public Sequencia(short _hole)
            {
                clear();

                m_hole = _hole;
            }
            public Sequencia(byte _course, short _hole)
            {
                this.Course = _course;
                this.m_hole = _hole;
            }
            public void clear()
            {
                Course = 127;
                m_hole = -1;
            }
            public byte Course;
            public short m_hole;
        }

    }
}
