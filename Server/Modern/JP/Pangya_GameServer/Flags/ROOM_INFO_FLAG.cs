using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Flags
{ 
    public enum RoomCourseFlags : byte
    {
        BLUE_LAGOON,
        BLUE_WATER,
        SEPIA_WIND,
        WIND_HILL,
        WIZ_WIZ,
        WEST_WIZ,
        BLUE_MOON,
        SILVIA_CANNON,
        ICE_CANNON,
        WHITE_WIZ,
        SHINNING_SAND,
        PINK_WIND,
        NEW_MAP,//not exist
        DEEP_INFERNO,
        ICE_SPA,
        LOST_SEAWAY,
        EASTERN_VALLEY,
        CHRONICLE_1_CHAOS,
        ICE_INFERNO,
        WIZ_CITY,
        ABBOT_MINE,
        MYSTIC_RUINS,
        GRAND_ZODIAC = 64,
        RANDOM = 127,
        UNK = 0x7F
    }

    public enum RoomTypeFlags : byte
    {
        STROKE,
        MATCH,
        LOUNGE,
        GAME_TYPE,
        TOURNEY,
        TOURNEY_TEAM,
        GUILD_BATTLE,
        PANG_BATTLE,
        GAME_TYPE_08,
        GAME_TYPE_09,//
        APPROCH,
        GRAND_ZODIAC_INT,// GM_EVENT = 0x0B,
        GAME_TYPE_12,
        GRAND_ZODIAC_ADV,
        GRAND_ZODIAC_PRACTICE,
        GAME_TYPE_15,
        GAME_TYPE_16,
        GAME_TYPE_17,
        SPECIAL_SHUFFLE_COURSE,
        PRACTICE,
        GRAND_PRIX,
    }

    public enum RoomHoleType : byte
    {
        M_FRONT,
        M_BACK,
        M_RANDOM,
        M_SHUFFLE,
        M_REPEAT,
        M_SHUFFLE_COURSE,
    }
    
    public enum ROOM_INFO_CHANGE : uint
    {
        NAME,
        SENHA,
        TIPO,
        COURSE,
        QNTD_HOLE,
        MODO,
        TEMPO_VS,
        MAX_PLAYER,
        TEMPO_30S,
        STATE_FLAG,
        GALLERY_LIMIT,
        HOLE_REPEAT,
        FIXED_HOLE,
        ARTEFATO,
        NATURAL,
    }
}
