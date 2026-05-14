using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_MessengerServer.PangyaEnums
{
    public enum PacketIDClient
    {
        CLIENT_CONNECT_0x12 = 0x12,
        CLIENT_REQ_USERINFO_OFFLINE_0x13,
        CLIENT_REQ_USERINFO_0x14,
        CLIENT_NOTIFY_LOGOUT_0x16 = 0x16,
        CLIENT_REQ_CHECK_NICK_0x17,
        CLIENT_REQ_REGISTER_FRIEND_0x18,
        CLIENT_REQ_FRIEND_AGREE_0x19,
        CLIENT_REQ_FRIEND_BLOCK_0x1A,
        CLIENT_REQ_FRIEND_BLOCK_CANCEL_0x1B,
        CLIENT_REQ_FRIEND_REMOVE_0x1C,
        CLIENT_NOTIFY_UPDATE_MY_STATUS_0x1D,
        CLIENT_REQ_CHAT_FRIEND_0x1E,
        CLIENT_REQ_CHANGE_FRIENDALIAS_0x1F,
        CLIENT_REQ_UPDATE_CHANNEL_INFO_0x23 = 0x23,
        CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_0x24,
        CLIENT_REQ_CHAT_GUILD_0x25,
        CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_GUILD_BATTLE_0x28 = 0x28,
        CLIENT_NOTIFY_PLAYER_GIFT_ITEM_0x29,
        CLIENT_NOTIFY_PLAYER_GUILD_JOINED_0x2A,
        CLIENT_NOTIFY_PLAYER_GUILD_BANISH_0x2B,
        CLIENT_NOTIFY_PLAYER_GUILD_SHIELD_CHANGED_0x2C,
        CLIENT_NOTIFY_PLAYER_GUILD_NAME_CHANGED_0x2D,
    }

    public enum PacketIDServer
    {
        SERVER_NONE_0x100 = 0x100,
        SERVER_USERINFO_OFFLINE_0x101,
        SERVER_USERINFO_0x102,
        //SERVER_UNKNOWN_0x103,
        SERVER_REGISTER_FRIEND_0x104 = 0x104,
        //SERVER_UNKNOWN_0x105,
        SERVER_NEW_FRIEND_MESSAGE_0x106 = 0x106,
        //SERVER_UNKNOWN_0x107_A_0x108,
        SERVER_FRIEND_AGREE_0x109 = 0x109,
        SERVER_FRIEND_ACCEPTED_0x10A,
        SERVER_FRIEND_REMOVE_0x10B,
        SERVER_FRIEND_BLOCK_0x10C,
        SERVER_FRIEND_BLOCK_CANCEL_0x10D,
        //SERVER_UNKNOWN_0x10E,
        SERVER_FRIEND_LOGOUT_0x10F = 0x10F,
        //SERVER_UNKNOWN_0x110_A_0x112,
        SERVER_SEND_TALK_0x113 = 0x113, // FRIEND OR GUILD
                                                 //SERVER_UNKNOWN_0x114,
        SERVER_CHANGE_MY_STATUS_0x115 = 0x115,
        //SERVER_UNKNOWN_0x116,
        SERVER_CHECK_NICK_0x117 = 0x117,
        //SERVER_UNKNOWN_0x118,
        SERVER_CHANGED_FRIENDALIAS_0x119 = 0x119,
        SERVER_CONNECT_0x2E = 0x2E, // RES == Msn
        SERVER_LOGIN_ACK_0x2F,
        SERVER_FRIEND_AND_GUILD_LIST_0x30 
    }
    public enum USER_STATUS
    {
        IS_PLAYING = 0,
        IS_RECONNECT = 1,
        IS_ONLINE = 4,
        IS_IDLE = 3,
    }

    [Flags]
    public enum PlayerState : byte
    {
        None = 0,
        sex = 1 << 0, // Bit 0
        online = 1 << 1, // Bit 1
        _friend = 1 << 2, // Bit 2
        request_friend = 1 << 3, // Bit 3
        block = 1 << 4, // Bit 4
        play = 1 << 5, // Bit 5
        AFK = 1 << 6, // Bit 6
        busy = 1 << 7  // Bit 7
    }
}
