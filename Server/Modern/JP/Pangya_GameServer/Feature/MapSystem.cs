using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Engine;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using Pangya_GameServer.Feature;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.DataBase;
using PangyaAPI.Network;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using static Pangya_GameServer.Models.DefineConstants;
using Pangya_GameServer.Handles;
namespace Pangya_GameServer.Feature
{
    public class Map
    {
        Dictionary<byte, stCtx> m_map = new Dictionary<byte, stCtx>();
        bool m_load = false;

        public Map()
        {
        }

        public bool isLoad()
        {
            return m_load && m_map.Count > 0;
        }

        public void load()
        {
            if (isLoad())
                clear();

          initialize();
        }

        public stCtx getMap(byte course)
        {
            if (m_map.TryGetValue((byte)(course & 0x7F), out var ctx))
                return ctx;

            return null;
        }

        public uint calculeClearVS(stCtx ctx, uint num_player, uint qntd_hole)
        {
            return ctx.clear_bonus * qntd_hole * (num_player - 1);
        }

        public uint calculeClearMatch(stCtx ctx, uint qntd_hole)
        {
            return ctx.clear_bonus * qntd_hole;
        }

        public uint calculeClear30s(stCtx ctx, uint qntd_hole)
        {
            if (ctx.clear_bonus == 0 || qntd_hole == 0)
                return 0;

            return (ctx.clear_bonus * qntd_hole) / 2;
        }

        public uint calculeClearSSC(stCtx ctx)
        {
            return ctx.clear_bonus;
        }

        private void initialize()
        {
            try
            { 
                var courses = sIff.Instance.getCourse();

                stCtx ctx;
                foreach (var el in courses)
                {
                    ctx = new stCtx
                    {
                        name = el.Name
                    };
                    ctx.range_score.par = el.Par_Hole;
                    ctx.star = 1f + (el.Star / 10f);
                    // Bonus por curso (substituir com enums se necessário)
                    var tipo = (RoomCourseFlags)(el.ID & 0xFF);
                    var course = (byte)(el.ID & 0xFF);
                    switch (tipo)
                    {
                        case RoomCourseFlags.BLUE_LAGOON: ctx.clear_bonus = 20; break;
                        case RoomCourseFlags.BLUE_WATER: ctx.clear_bonus = 50; break;
                        case RoomCourseFlags.BLUE_MOON: ctx.clear_bonus = 50; break;
                        case RoomCourseFlags.SEPIA_WIND: ctx.clear_bonus = 55; break;
                        case RoomCourseFlags.PINK_WIND: ctx.clear_bonus = 20; break;
                        case RoomCourseFlags.WIND_HILL: ctx.clear_bonus = 80; break;
                        case RoomCourseFlags.WIZ_WIZ: ctx.clear_bonus = 65; break;
                        case RoomCourseFlags.WHITE_WIZ: ctx.clear_bonus = 55; break;
                        case RoomCourseFlags.WEST_WIZ: ctx.clear_bonus = 24; break;
                        case RoomCourseFlags.WIZ_CITY: ctx.clear_bonus = 40; break;
                        case RoomCourseFlags.DEEP_INFERNO: ctx.clear_bonus = 80; break;
                        case RoomCourseFlags.ICE_SPA: ctx.clear_bonus = 20; break;
                        case RoomCourseFlags.ICE_CANNON: ctx.clear_bonus = 40; break;
                        case RoomCourseFlags.ICE_INFERNO: ctx.clear_bonus = 70; break;
                        case RoomCourseFlags.SILVIA_CANNON: ctx.clear_bonus = 70; break;
                        case RoomCourseFlags.SHINNING_SAND: ctx.clear_bonus = 40; break;
                        case RoomCourseFlags.EASTERN_VALLEY: ctx.clear_bonus = 40; break;
                        case RoomCourseFlags.LOST_SEAWAY: ctx.clear_bonus = 20; break;
                        case RoomCourseFlags.GRAND_ZODIAC: ctx.clear_bonus = 0; break;
                        case RoomCourseFlags.CHRONICLE_1_CHAOS: ctx.clear_bonus = 360; break;
                        case RoomCourseFlags.ABBOT_MINE: ctx.clear_bonus = 40; break;
                        case RoomCourseFlags.MYSTIC_RUINS: ctx.clear_bonus = 40; break;
                    }
                    m_map[course] = (ctx);
                }

                if (m_map.Count == 0)
                    _smp.LogManager.Instance.push(new AppMessage("[Map::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_load = true;
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Map::initialize][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        private void clear()
        {
            m_map.Clear();
            m_load = false;
        }

        public class stCtx
        {
            public stCtx(uint ul = 0u)
            {
                clear();
            }

            public void clear()
            {
                name = "";
                range_score = new stParRangeScore();
                clear_bonus = 0;
                star = 0.0f;
            }

            public class stParRangeScore
            {
                public sbyte[] par = new sbyte[18];
                public sbyte[] min = new sbyte[18];
                public sbyte[] max = new sbyte[18];

                public void clear()
                {
                    Array.Clear(par, 0, par.Length);
                    Array.Clear(min, 0, min.Length);
                    Array.Clear(max, 0, max.Length);
                }
            }

            public string name { get; set; }
            public uint clear_bonus;
            public float star;
            public stParRangeScore range_score = new stParRangeScore();
        }
    }

    public class MapSystem : Singleton<Map>
    {
    }
}
