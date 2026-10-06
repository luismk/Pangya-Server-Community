using Pangya_LoginServer.DataBase;

using Pangya_LoginServer.Server;
using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Utilities.Log;

namespace Pangya_LoginServer.Handles
{
    public class Handle_KICK_PLAYER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // Log de tentativa de derrubar login duplicado
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_KICK_PLAYER][Log] Player {Player.UserInfo.Login} (UID: {Player.UserInfo.UID}) solicitou derrubar login duplicado.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE)); 

                // Derruba o Player que está logado no game server
                // Se o Auth Server Estiver ligado manda por ele, se não tira pelo banco de dados mesmo
                if (LoginServer.Instance.m_unit_connect != null)
                {

                    // [Auth Server] . Game Server UID = Player.m_pi.m_server_uid;
                    LoginServer.Instance.m_unit_connect.SendDisconnectPlayer(Player.UserInfo.m_server_uid, Player.UserInfo.UID);

                }
                else
                {

                    // Auth Server não está online, resolver por aqui mesmo
                    CommandDB.RegisterLogon(Player.UserInfo.UID, 0);

                   await Handle_PLAYER_LOGIN.SUCCESS_LOGIN(Player, 0); 
                }

            }
            catch (Exception e)
            {
                // Se falhar (ex: Auth Server offline), envia erro 500053 (Duplicate Login Error)
                Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500053));

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_KICK_PLAYER][Error] Falha ao derrubar Player: {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}