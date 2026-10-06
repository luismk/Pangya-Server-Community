using Pangya_LoginServer.Models;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Network.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Pangya_LoginServer.Session
{
    public class Player : AppSession
    {
        public PlayerInfo UserInfo { get; set; }
        public Player(IAppServer server, Socket socket, int id) : base(server,socket, id)
        {
            UserInfo = new PlayerInfo();
        }

        public override bool Clear()
        {  
            // 2. Limpa recursos específicos do Player (Pangya_PlayerInfo, etc)
            if (base.Clear())
            {
                this.UserInfo.Clear(); // Se a sua PlayerInfo também tiver um Clear
            }

            return true;
        }

        public bool IsGM()
        {
           return UserInfo.Capability == 4 || UserInfo.Capability == 128;
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

        public override uint GetCapability() { return (uint)UserInfo.Capability; }

        public override byte GetStateLogged()
        {
            return 0;
        }

        public bool getState()
        {
            return Authorized && Connected;
        }

        public void SafeClose() => _socket.SafeClose();
    }
}
