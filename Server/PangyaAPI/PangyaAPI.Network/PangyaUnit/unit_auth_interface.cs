using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.Network.PangyaUnit
{
    public class IUnitAuthServer
    {
        //criados no outros
        public virtual void authCmdShutdown(int _time_sec) { }
        public virtual void authCmdBroadcastNotice(string _notice) { }
        public virtual void authCmdBroadcastTicker(string _nickname, string _msg) { }
        public virtual void authCmdBroadcastCubeWinRare(string _msg, uint _option) { }
        public virtual void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force) { }
        public virtual void authCmdConfirmDisconnectPlayer(uint _player_uid) { }
        public virtual void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id) { }
        public virtual void authCmdNewRate(uint _tipo, uint _qntd) { }
        public virtual void authCmdReloadGlobalSystem(uint _tipo) { }

        //criados no server.cs
        public virtual void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid) { }
        public virtual void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi) { }

        // requests Comandos e respostas dinâmicas
        public virtual void authCmdSendCommandToOtherServer(packet _packet) { }
        public virtual void authCmdSendReplyToOtherServer(packet _packet) { }

        /// <summary>
        /// Server envia comandos e resposta para outros server com o Auth Server
        ///,tem que ser P.binary. por que eu uso pra responder 
        /// </summary>
        /// <param name="_packet">BinaryWriter</param>
        /// <param name="_send_server_uid_or_type">id do server ou tipo</param>
        public virtual void sendCommandToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type) { }
        /// <summary>
        /// Server envia comandos e resposta para outros server com o Auth Server
        ///,tem que ser P.binary. por que eu uso pra responder 
        /// </summary>
        /// <param name="_packet">BinaryWriter</param>
        /// <param name="_send_server_uid_or_type">id do server ou tipo</param>
        public virtual void sendReplyToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type) { } 
    }
}
