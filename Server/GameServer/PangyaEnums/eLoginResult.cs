namespace Pangya_GameServer.PangyaEnums
{
    /// <summary>
    /// for packet login -> 0x44
    /// </summary>
    public enum eLoginAck : int
    {
        ACK_LOGIN_OK = 0x00,
        ACK_LOGIN_FAIL = 0x01,
        ACK_INVALID_ID = 0x02,
        ACK_LOGIN_ERROR = 0x03,
        ACK_ALREADY_LOGIN = 0x04,
        ACK_BLOCK_ID = 0x05,
        ACK_INVALID_PASSWORD = 0x06,
        ACK_TIME_BLOCK_ID = 0x07,
        ACK_BLOCK_PASSWORD_AUTH = 0x08,
        ACK_14_UNDER_NO_PARENT_AGREE = 0x09,
        ACK_14_UNDER_NO_INFO = 0x0A,
        ACK_INVALID_VERSION = 0x0B,
        ACK_INVALID_JID = 0x0C,
        ACK_DELETE_ID = 0x0D,
        ACK_NOT_QUALIFIED = 0x0E,
        ACK_LOGIN_SERVICE_PAUSE = 0x0F,
        ACK_BLOCKED_IP_ADDR = 0x10,
        ACK_UNDER_18_BLOCK = 0x11,
        ACK_SECURITY_KEY = 0x12,
        ACK_UNKNOWN_ID = 0x13,
        ACK_PARAN_SERVICE_STOP = 0x14,

        ACK_OUT_OF_TICKET = 0xC8,            // 200
        ACK_GAMANIA_GASH_ERR = 0xC9,         // 201
        ACK_NOT_WEBLOGIN = 0xCA,             // 202
        ACK_LOGIN_NICK_CHANGE = 0xCB,        // 203
        ACK_CHANGE_NICK_OK = 0xCC,           // 204
        ACK_CHANGE_NICK_ERROR = 0xCD,        // 205
        ACK_REQUEST_BPKEY_ERROR = 0xCE,      // 206
        ACK_LIMITED_LOGIN_DATE = 0xCF,       // 207
        ACK_NONE_BETAUSER = 0xD0,            // 208
        ACK_QUERY_FAILED = 0xD1,             // 209
        ACK_UPDATE_LOGIN_UNIT = 0xD2,        // 210
        ACK_AUTO_RECONNECT = 0xD3,           // 211
        ACK_NOT_APPLY = 0xD4,                // 212
        ACK_NOT_AGREEMENT = 0xD5,            // 213
        ACK_CHAMPION_OVERTIME = 0xD6,        // 214
        ACK_CHAMPION_NOTFIND = 0xD7,         // 215
        ACK_AUTH_STATE_BLOCK = 0xD8          // 216
    }
}
