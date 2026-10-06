using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Linq;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_ASSING_NICK : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            var p = new Packet();

            try
            {
                uint uid = Packet.ReadUInt32();
                var apelido = Packet.ReadString();
                  
                if (uid == 0)
                    throw new exception("[MessengerService::requestAssingApelido][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o UID is invalid(zero). Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200901));

                if (string.IsNullOrEmpty(apelido)) // Compatibilidade para apelido.empty()
                    throw new exception("[MessengerService::requestAssingApelido][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o apelido is empty. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200902));

                if (apelido.Length >= 11) // Usando Length para o Count() de string
                    throw new exception("[MessengerService::requestAssingApelido][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o comprimento do apelido[max=11, request=" + (apelido.Length) + "] eh invalido.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200903));

                var pFi = Player.UserInfo.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerService::requestAssingApelido][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas ele nao tem esse player como amigo. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200903));

                // UPDATE ON SERVER 
                pFi.apelido = apelido;

                // UPDATE ON DB - Usando await para garantir a persistência no banco
                Player.UserInfo.m_friend_manager.requestUpdateFriendInfo(pFi);

                // Log original
                _smp.LogManager.Instance.push(new AppMessage("[AssingApelido][Log] player[UID=" + (Player.UserInfo.UID) + "] colocou apelido[VALUE="
                        + apelido + "] no Amigo[UID=" + (pFi.uid) + ", NICKNAME=" + (pFi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para assing apelido (Protocolo 0x30 / Sub 0x119)
                p.init_plain(0x30);
                p.Write((ushort)0x119); // Sub packet Id
                p.Write((uint)0);       // OK

                p.Write((uint)pFi.uid);
                p.WriteString(pFi.apelido); 
                Player.Send(p);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::requestAssingApelido][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);
                p.Write((ushort)0x119);

                uint error_code = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER)
                                  ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                                  : 0x5200900;

                p.Write((uint)error_code);

                Player.Send(p);
            }
        }
    }
}
