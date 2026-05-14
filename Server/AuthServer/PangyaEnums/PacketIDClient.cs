namespace Pangya_AuthServer.PangyaEnums
{
    public enum PacketIDClient : ushort
    {
        CLIENT_LOGIN_REQUEST = 0x01,
        CLIENT_LOGOUT_REQUEST = 0x02,
        CLIENT_LOGOUT_CONFIRM = 0x03,
        CLIENT_INFO_REQUEST = 0x04,
        CLIENT_INFO_ACK = 0x05,
        CLIENT_INTERSERVER_COMMAND = 0x06,
        CLIENT_INTERSERVER_REPLY = 0x07,
        CLIENT_HEARTBEAT = 0xFF
    }
}
