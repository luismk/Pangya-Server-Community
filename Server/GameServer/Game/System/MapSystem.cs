using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public class Map
    {
        Dictionary<byte, stCtx> m_map = new Dictionary<byte, stCtx>();
        bool m_load = false;

        public Map()
        {
            initialize();
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
                if (!sIff.getInstance().isLoad())
                    sIff.getInstance().initilation();

                var courses = sIff.getInstance().getCourse();

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
                    var tipo = (RoomInfo.ROOM_INFO_COURSE)(el.ID & 0xFF);
                    var course = (byte)(el.ID & 0xFF);
                    switch (tipo)
                    {
                        case RoomInfo.ROOM_INFO_COURSE.BLUE_LAGOON: ctx.clear_bonus = 20; break;
                        case RoomInfo.ROOM_INFO_COURSE.BLUE_WATER: ctx.clear_bonus = 50; break;
                        case RoomInfo.ROOM_INFO_COURSE.BLUE_MOON: ctx.clear_bonus = 50; break;
                        case RoomInfo.ROOM_INFO_COURSE.SEPIA_WIND: ctx.clear_bonus = 55; break;
                        case RoomInfo.ROOM_INFO_COURSE.PINK_WIND: ctx.clear_bonus = 20; break;
                        case RoomInfo.ROOM_INFO_COURSE.WIND_HILL: ctx.clear_bonus = 80; break;
                        case RoomInfo.ROOM_INFO_COURSE.WIZ_WIZ: ctx.clear_bonus = 65; break;
                        case RoomInfo.ROOM_INFO_COURSE.WHITE_WIZ: ctx.clear_bonus = 55; break;
                        case RoomInfo.ROOM_INFO_COURSE.WEST_WIZ: ctx.clear_bonus = 24; break;
                        case RoomInfo.ROOM_INFO_COURSE.WIZ_CITY: ctx.clear_bonus = 40; break;
                        case RoomInfo.ROOM_INFO_COURSE.DEEP_INFERNO: ctx.clear_bonus = 80; break;
                        case RoomInfo.ROOM_INFO_COURSE.ICE_SPA: ctx.clear_bonus = 20; break;
                        case RoomInfo.ROOM_INFO_COURSE.ICE_CANNON: ctx.clear_bonus = 40; break;
                        case RoomInfo.ROOM_INFO_COURSE.ICE_INFERNO: ctx.clear_bonus = 70; break;
                        case RoomInfo.ROOM_INFO_COURSE.SILVIA_CANNON: ctx.clear_bonus = 70; break;
                        case RoomInfo.ROOM_INFO_COURSE.SHINNING_SAND: ctx.clear_bonus = 40; break;
                        case RoomInfo.ROOM_INFO_COURSE.EASTERN_VALLEY: ctx.clear_bonus = 40; break;
                        case RoomInfo.ROOM_INFO_COURSE.LOST_SEAWAY: ctx.clear_bonus = 20; break;
                        case RoomInfo.ROOM_INFO_COURSE.GRAND_ZODIAC: ctx.clear_bonus = 0; break;
                        case RoomInfo.ROOM_INFO_COURSE.CHRONICLE_1_CHAOS: ctx.clear_bonus = 360; break;
                        case RoomInfo.ROOM_INFO_COURSE.ABBOT_MINE: ctx.clear_bonus = 40; break;
                        case RoomInfo.ROOM_INFO_COURSE.MYSTIC_RUINS: ctx.clear_bonus = 40; break;
                    }
                    m_map[course] = (ctx);
                }

                if (m_map.Count == 0)
                    _smp.message_pool.getInstance().push(new message("[Map::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_load = true;
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Map::initialize][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
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
