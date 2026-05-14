namespace Pangya_GameServer.PangyaEnums
{
    public enum eChatMsg : int
    {
        CHAT_NORMAL = 0x00,
        CHAT_NOT_EXIST_IN_CHANNEL = 0x01,
        CHAT_OUT_OF_GAME = 0x02,
        CHAT_BANISH_VOTE_REJECTED = 0x03,
        CHAT_REFUSE_WHISPER = 0x04,
        CHAT_NOT_EXIST_IN_SERVER = 0x05,
        CHAT_OFFLINE = 0x06,
        CHAT_NOTICE = 0x07,
        CHAT_BLOCKED = 0x08,
        CHAT_OUT_OF_SKINSGAME = 0x09,
        CHAT_ONLINE_MSG = 0x0A,
        CHAT_MAX = 0x80,              // 128
        CHAT_GM = 0x80   // 128 (bit type)
    }
}
