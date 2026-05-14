using System;

namespace Pangya_GameServer.PangyaEnums
{
    [Flags]
    public enum PlaceFlags : byte
    {
        None = 0,
        MainLobby = 1 << 0,   // 0000 0001 (bit 0)
        WebLinkOrMyRoom = 1 << 1, // 0000 0010 (bit 1)
        GamePlay = 1 << 1 | 1 << 3 // 0000 1010 (bits 1 e 3)
    }
}
