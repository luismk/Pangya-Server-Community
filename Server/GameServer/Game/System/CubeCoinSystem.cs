// CubeCoinSystem.cs - versão otimizada e compatível com C# 6
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.RoomInfo;

namespace Pangya_GameServer.Game.System
{
    public class CubeCoinSystem
    {
        private ConcurrentDictionary<uint, CourseCtx> m_course = new ConcurrentDictionary<uint, CourseCtx>();
        private volatile bool m_load = false;

        public CubeCoinSystem() { m_load = false; }

        public void load()
        {
            if (isLoad()) Clear();
            initialize();
        }

        public bool isLoad() { return m_load && !m_course.IsEmpty; }

        public CourseCtx FindCourse(uint course_typeid)
        {
            m_course.TryGetValue(course_typeid, out var course);
            return course;
        }

        private void initialize()
        {
            try
            {
                // 1. Pega informações de quais mapas têm o sistema ativo no Banco
                CmdCoinCubeInfo cmd_cci = new CmdCoinCubeInfo();
                NormalManagerDB.getInstance().add(0, cmd_cci, null, null);

                if (cmd_cci.getException().getCodeError() != 0u)
                    throw cmd_cci.getException();

                var coursesIff = sIff.getInstance().getCourse();
                var dbInfo = cmd_cci.getInfo();

                foreach (var el in coursesIff)
                {
                    // Identify do mapa (Ex: Blue Lagoon = 0)
                    byte course_id = (byte)sIff.getInstance().getItemIdentify(el.ID);

                    // Verifica se o mapa está ativo no banco
                    bool isActive = dbInfo.ContainsKey(course_id) && dbInfo[course_id];

                    var courseCtx = new CourseCtx(el.ID, isActive);

                    courseCtx.loadLocations();

                    m_course.TryAdd(el.ID, courseCtx);
                }

                if (m_course.Count == 0)
                    _smp.message_pool.getInstance().push(new message("[CubeCoinSystem::initialize][Warning] No courses loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_load = true;
            }
            catch (Exception ex)
            {
                m_load = false;
                _smp.message_pool.getInstance().push(new message("[CubeCoinSystem::initialize][Error] " + ex.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void Clear()
        {
            m_course.Clear();
            m_load = false;
        }

        public class CourseCtx
        {
            public class Hole
            {
                public byte numero;
                public uint number_of_cube;
                public uint max_coin_and_cube;
                public List<CubeEx> v_cube = new List<CubeEx>();

                public Hole(byte numero, uint number_of_cube, uint max_coin_and_cube)
                {
                    this.numero = numero;
                    this.number_of_cube = number_of_cube;
                    this.max_coin_and_cube = max_coin_and_cube;
                }


                // Método 1: Lógica específica para Wiz City
                public List<CubeEx> getAllCoinCubeWizCity(bool _cube)
                {
                    if (v_cube.Count == 0) return v_cube;

                    // std::shuffle
                    var cpy = v_cube.OrderBy(x => Guid.NewGuid()).ToList();
                    var ret = new List<CubeEx>();
                    var lottery = new Lottery();

                    // Initialize the Roleta (Somente COIN em CARPET)
                    foreach (var it in cpy)
                    {
                        if (it.tipo == Cube.eTYPE.COIN && it.flag_location == Cube.eFLAG_LOCATION.CARPET)
                            lottery.Push(100 * it.rate, it);
                    }

                    // All coin edge green (Sempre adicionados)
                    foreach (var it in cpy)
                    {
                        if (it.flag_location == (byte)Cube.eFLAG_LOCATION.EDGE_GREEN)
                            ret.Add(it);
                    }

                    if (lottery.getCountItem() > 0)
                    {
                        // Sorteio de Cubos (Transforma a moeda em cubo em Wiz City)
                        if (_cube)
                        {
                            uint count_cube = number_of_cube;
                            count_cube = Math.Min(count_cube, (uint)lottery.getCountItem());

                            while (count_cube-- > 0)
                            {
                                var ctx = lottery.spinRoleta(true);
                                if (ctx?.Value != null)
                                {
                                    var dice = (CubeEx)ctx.Value;
                                    ret.Add(new CubeEx(
                                        dice.id,
                                       Cube.eTYPE.CUBE,
                                        0,
                                        dice.flag_location,
                                        dice.location.x, dice.location.y, dice.location.z,
                                        dice.rate
                                    ));
                                }
                            }
                        }

                        // Preenche o resto com o que sobrou na roleta (Moedas normais)
                        int current_count = ret.Count;
                        uint rest_count = (max_coin_and_cube > (uint)current_count) ? (max_coin_and_cube - (uint)current_count) : 0;
                        rest_count = Math.Min(rest_count, (uint)lottery.getCountItem());

                        while (rest_count-- > 0)
                        {
                            var ctx = lottery.spinRoleta(true);
                            if (ctx?.Value != null)
                                ret.Add((CubeEx)ctx.Value);
                        }
                    }

                    return ret;
                }

                public List<CubeEx> getAllCoinCube(bool _cube)
                {
                    if (v_cube.Count == 0) return new List<CubeEx>(v_cube);

                    var cpy = v_cube.OrderBy(x => Guid.NewGuid()).ToList();
                    var ret = new List<CubeEx>();
                    var lottery = new Lottery();

                    // 1. Sorteio de Cubos de AR
                    if (_cube)
                    {
                        foreach (var it in cpy)
                        {
                            if (it.tipo == Cube.eTYPE.CUBE && it.flag_location == Cube.eFLAG_LOCATION.AIR)
                                lottery.Push(100 * it.rate, it);
                        }

                        if (lottery.getCountItem() > 0)
                        {
                            uint count_cube = number_of_cube;
                            count_cube = Math.Min(count_cube, (uint)lottery.getCountItem());

                            while (count_cube-- > 0)
                            {
                                var ctx = lottery.spinRoleta(true);
                                if (ctx?.Value != null)
                                    ret.Add((CubeEx)ctx.Value);
                            }
                        }
                    }

                    // Limpa a roleta para o próximo sorteio (Moedas)
                    lottery.Clear();

                    // 2. Sorteio de Moedas de CHÃO
                    foreach (var it in cpy)
                    {
                        if (it.tipo == (byte)Cube.eTYPE.COIN && it.flag_location == Cube.eFLAG_LOCATION.GROUND)
                            lottery.Push(100 * it.rate, it);
                    }

                    if (lottery.getCountItem() > 0)
                    {
                        int current_count = ret.Count;
                        uint rest_count = (max_coin_and_cube > (uint)current_count) ? (max_coin_and_cube - (uint)current_count) : 0;
                        rest_count = Math.Min(rest_count, (uint)lottery.getCountItem());

                        while (rest_count-- > 0)
                        {
                            var ctx = lottery.spinRoleta(true);
                            if (ctx?.Value != null)
                                ret.Add((CubeEx)ctx.Value);
                        }
                    }

                    return ret;
                }
            }

            private uint m_typeid;
            private bool m_active;
            private ConcurrentDictionary<byte, Hole> m_holes = new ConcurrentDictionary<byte, Hole>();

            public CourseCtx(uint typeid, bool active)
            {
                m_typeid = typeid;
                m_active = active;
            }

            public void loadLocations()
            {
                byte course_id = (byte)sIff.getInstance().getItemIdentify(m_typeid);
                var cmd_ccli = new CmdCoinCubeLocationInfo(course_id);
                NormalManagerDB.getInstance().add(0, cmd_ccli, null, null);

                if (cmd_ccli.getException().getCodeError() != 0u) return;

                var allLocations = cmd_ccli.getInfo();
                // Parallel para preencher os 18 buracos
                Parallel.For(1, 19, i =>
                {
                    byte hole_idx = (byte)i;
                    var cbih = (course_id == (byte)ROOM_INFO_COURSE.WIZ_CITY)
                        ? getAllCoinCubeInHoleWizCity(hole_idx)
                        : getAllCoinCubeInHole(course_id, hole_idx);

                    var hole = new Hole(hole_idx, cbih.m_all_cube, cbih.m_all_coin_and_cube);

                    if (allLocations.ContainsKey(hole_idx))
                        hole.v_cube.AddRange(allLocations[hole_idx]);

                    m_holes.TryAdd(hole_idx, hole);
                });
            }

            public bool IsActive() => m_active;
            public Hole FindHole(byte hole) { m_holes.TryGetValue(hole, out var h); return h; }
        }
        public static CoinCubeInHole getAllCoinCubeInHoleWizCity(byte _number_hole)
        {
            CoinCubeInHole cbih = new CoinCubeInHole { m_all_cube = 0, m_all_coin_and_cube = 0 };

            switch (_number_hole)
            {
                case 3:
                case 12:
                    cbih.m_all_cube = 5;
                    cbih.m_all_coin_and_cube = 60;
                    break;
                case 14:
                    cbih.m_all_cube = 2;
                    cbih.m_all_coin_and_cube = 48;
                    break;
                case 18:
                    cbih.m_all_cube = 3;
                    cbih.m_all_coin_and_cube = 33;
                    break;
                default:
                    cbih.m_all_cube = 0;
                    cbih.m_all_coin_and_cube = 20; // Coin green edge
                    break;
            }

            return cbih;
        }

        public static CoinCubeInHole getAllCoinCubeInHole(byte _course_id, byte _number_hole)
        {
            CoinCubeInHole cbih = new CoinCubeInHole { m_all_cube = 0, m_all_coin_and_cube = 0 };

            if (_number_hole == 0 || _number_hole > 18)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[CubeCoinSystem::CoinCubeInHole][WARNING] invalid number hole({_number_hole})",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return cbih;
            }

            var course = MapSystem.getInstance().getMap(_course_id);

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[CubeCoinSystem::CoinCubeInHole][WARNING] Course[={_course_id}] nao foi encontrado no singleton sMap.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return cbih;
            }

            // Lógica baseada no PAR do buraco (index - 1)
            switch (course.range_score.par[_number_hole - 1])
            {
                case 3:
                    cbih.m_all_cube = 1;
                    cbih.m_all_coin_and_cube = 1;
                    break;
                case 4:
                    cbih.m_all_cube = 1;
                    cbih.m_all_coin_and_cube = 5;
                    break;
                case 5:
                    cbih.m_all_cube = 2;
                    cbih.m_all_coin_and_cube = 8;
                    break;
            }

            return cbih;
        }

        public class CoinCubeInHole { public uint m_all_cube; public uint m_all_coin_and_cube; }
    }

    public class sCubeCoinSystem : Singleton<CubeCoinSystem> { }
}
