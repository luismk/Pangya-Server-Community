using Pangya_AuthServer.Models;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Service.Auth;
using PangyaAPI.Network.Session;
using PangyaAPI.Network.Utils;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pangya_AuthServer.Session
{
    public class Player : AppSession
    {
        public SystemTime last_activity; 
        public PlayerInfo UserInfo { get; set; }
        public Player(IAppServer server, Socket socket, int id) : base(server, socket, id)
        {
            last_activity = new SystemTime(DateTime.Now);
            UserInfo = new PlayerInfo(); 
        }

        public override string GetNickname()
        {
            return UserInfo.NickName;
        }

        public override uint GetUID()
        {
            return UserInfo.UID;
        }

        public override string GetID()
        {
            return UserInfo.Login;
        }

        public override uint GetCapability() { return (uint)UserInfo.tipo; }

    }
}
