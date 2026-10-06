using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_CHECK_NICK : HandleBase<Player, Packet_EXAMPLE>//<Packet_PLAYER_CHAT_GUILD, MPlayer>
    {
        public override async Task Handle()
        { 
            var p = new Packet();
            var nickname = "";

            try
            {
                nickname = Packet.ReadString();

                // CHECK_SESSION_IS_AUTHORIZED("CheckNickname");

                if (string.IsNullOrEmpty(nickname)) // Adaptado de NickName.empty()
                    throw new exception("player[UID=" + (Player.UserInfo.UID) + "] tentou verificar o Nickname[value="
                            + nickname + "], mas o NickName is empty. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200501));

if(!Tools.Sanitize(nickname))
throw new exception("player[UID=" + (Player.UserInfo.UID) + "] tentou verificar o Nickname[value="
                            + nickname + "], mas o NickName is empty. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200501));
							
							
                var cmd_vn = new CmdVerifyNick(nickname);    // Waiter
                 
                cmd_vn.exec();

                if (cmd_vn.getException().getCodeError() != 0)
                    throw cmd_vn.getException();

                if (!cmd_vn.getLastCheck())
                    throw new exception("player[UID=" + (Player.UserInfo.UID) + "] tentou verificar o Nickname[value="
                        + nickname + "], mas o NickName nao existe.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 1));

                // Log original
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHECK_NICK][Log] player[UID=" + (Player.UserInfo.UID) + "] pediu para verificar o Nickname[value=" + nickname + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para Check Nickname (Protocolo 0x30 / Sub 0x117)
                p.init_plain(0x30);
                p.Write((ushort)0x117);   // Sub packet Id
                p.Write((uint)0);         // Status OK 
                p.WriteString(nickname);
                p.Write((uint)cmd_vn.getUID()); 
                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHECK_NICK][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);
                p.Write((ushort)0x117);   // Sub packet Id

                // Lógica original de decodificação de erro
                uint error_code = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER)
                                  ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                                  : 0x5200500;

                p.Write((uint)error_code);
                p.WriteString(nickname); 
                Player.Send(p);
            }
        }
    }
}
