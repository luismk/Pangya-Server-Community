namespace Pangya_GameServer.PangyaEnums
{
    public enum COMMON_CMD_GM : short
    {
        CCG_DUMMY = 0x0,
        CCG_HELP = 0x1,
        CCG_COMMAND = 0x2,
        CCG_VISIBLE = 3,
        CCG_WHISPER = 4,
        CCG_CHANNEL = 5,
        CCG_OPEN_WHISPER_PLAYER_LIST = 8,
        CCG_CLOSE_WHISPER_PLAYER_LIST = 9,
        CCG_KICK = 10,
        CCG_DISCONNECT = 11,                 // Disconnect UID
        CCG_DESTROY = 0x0D,                  // 13
        CCG_CHANGE_WIND_VERSUS = 14,
        CCG_CHANGE_WEATHER = 15,
        CCG_NOTICE = 0x10,
        CCG_IDENTITY = 16,
        CCG_GIVEITEM = 18,
        CCG_GOLDENBELL = 19,
        CCG_SETPRIZE = 25,
        CCG_HIO_HOLE_CUP_SCALE = 26,
        CCG_SET_MISSION = 28,
        CCG_WEBMATCHMAP = 29,
        CCG_WEBMATCHHOLE = 30,
        CCG_MATCH_MAP = 31
    }
}
