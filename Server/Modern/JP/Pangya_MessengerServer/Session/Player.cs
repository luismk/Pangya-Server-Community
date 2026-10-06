using Pangya_MessengerServer.Models;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Pangya_MessengerServer.Session
{
    public class Player : AppSession
    {
        public PlayerInfo UserInfo { get; set; }
        public Player(IAppServer server, Socket socket, int id) : base(server, socket, id)
        {
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

        public override uint GetCapability() { return UserInfo.Capability; }

        public override byte GetStateLogged()
        {
            return 1;
        }

        public override bool Clear()
        {
            lock (this)
            {
                bool ret;
                if (ret = base.Clear())
                {
                    // Player Info
                    UserInfo.Clear();
                }
                return ret;
            }
        }
    }
}
