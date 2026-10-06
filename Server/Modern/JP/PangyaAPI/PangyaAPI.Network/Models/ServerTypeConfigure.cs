using System;
using System.Collections.Generic;
using System.Text;

namespace PangyaAPI.Network.Models
{
    public enum ServerTypeConfigure : int
    {
        Default = -1,
        GameServer,
        MessengerServer,
        LoginServer,
        RankServer,
        AuthServer,
    }

    public enum ServerType : int
    {
        Default = -1,
        GameServer = 1,
        MessengerServer = 2,
        LoginServer = 3,
        RankServer = 4,
        AuthServer = 5,
    }
}
