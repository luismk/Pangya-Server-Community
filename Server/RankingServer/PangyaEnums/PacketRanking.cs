using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_RankingServer.PangyaEnums
{
    public enum PacketIDClient
    {
        CLIENT_CONNECT = 0x00,
        CLIENT_REQUEST_PLAYER_INFO = 0x01,
        CLIENT_REQ_SEARCH_PLAYER_IN_RANKING = 0x02,
        CLIENT_UNKNOWN3 = 0x03,
        CLIENT_UNKNOWN4 = 0x04,
        CLIENT_UNKNOWN5 = 0x05,
    }

    public enum PacketIDServer
    {
        SERVER_CONNECT_LOGIN = 0x1388,
        SERVER_SEND_FIRST_PAGE = 0x1389,
        SERVER_SEND_PLAYER_FULL_INFO = 0x138A,
        SERVER_UNKNOWN_0x138B = 0x138B,
        SERVER_PAGE_NOT_FOUND_0x138C = 0x138C,
        SERVER_UNKNONW_0x138D = 0x138D,
    }
}
