using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_CHAT_FRIEND : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var p = new Packet();

            try
            {
                // 1. Leitura do Pacote
                uint targetUid = Packet.ReadUInt32();
                string msg = Packet.ReadPStr();

                // 2. Validações de Segurança (Sanitize e Empty Check)
                if (string.IsNullOrWhiteSpace(msg) || !Tools.Sanitize(msg))
                {
                    throw new exception($"Mensagem inválida ou tentativa de inject de UID: {Player.UserInfo.UID}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                if (targetUid == 0)
                {
                    throw new exception($"UID Alvo inválido para player: {Player.UserInfo.UID}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200301));
                }

                // 3. Verificação de Amizade (Quem envia)
                var friendInfo = Player.UserInfo.m_friend_manager.findFriendInAllFriend(targetUid);
                if (friendInfo == null || friendInfo.state.block == 1)
                {
                    throw new exception($"Amizade não encontrada ou bloqueada. De: {Player.UserInfo.UID} Para: {targetUid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200303));
                }

                // 4. Localiza Sessão do Amigo (Online Check)
                var targetSession = (Player)MessengerServer.Instance.FindSessionByUid(targetUid);
                if (targetSession == null)
                {
                    throw new exception($"Amigo Offline. UID: {targetUid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200305));
                }

                // 5. Verificação de Amizade (Quem recebe)
                var targetFriendEntry = targetSession.UserInfo.m_friend_manager.findFriendInAllFriend(Player.UserInfo.UID);
                if (targetFriendEntry == null || targetFriendEntry.state.block == 1)
                {
                    throw new exception($"Destinatário não tem remetente na lista ou bloqueou. De: {Player.UserInfo.UID} Para: {targetUid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200306));
                }

                // 6. Monitoramento para GMs (Dispara sem travar a resposta principal)
                var gm = MessengerServer.Instance.FindAllGM();

                if (gm.Count > 0)
                {

                    var msg_gm = "\\5" + (Player.UserInfo.NickName) + ">" + (targetSession.UserInfo.NickName) + ": '" + msg + "'";

                    foreach (Player el in gm)
                    {

                        // Nao envia o log de MSN.PM novamente para o GM que enviou ou recebeu MSN.PM
                        if (el.UserInfo.UID != Player.UserInfo.UID && el.UserInfo.UID != targetSession.UserInfo.UID)
                        {
                            // Responde no chat do player
                            p.init_plain(0x40);

                            p.WriteByte(0);

                            p.WriteString("\\1[MSN.PM]");   // Nickname

                            p.WriteString(msg_gm);  // Message

                            el.Send(p); 
                        }
                    }
                    //await DiscordWebhook.ChatLog("[MessengerService::ChatFriend][Log] player[UID=" + (Player.m_pi.NickName) + "] enviou Message[MSG="
                    //    + msg + "] para seu Amigo[UID=" + (s.m_pi.NickName) + "]");
                }

                // 7. Log do Servidor
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CHAT_FRIEND][Log] {Player.UserInfo.NickName} -> {targetSession.UserInfo.NickName}: {msg}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // 8. Resposta: Envia para o Amigo (Packet 0x30, Sub 0x113)
                p.init_plain(0x30);
                p.WriteUInt16(0x113);
                p.WriteUInt32(Player.UserInfo.UID);
                p.WriteString(Player.UserInfo.NickName);
                p.WriteString(msg);
                p.WriteByte(0); 
                targetSession.Send(p);
            }
            catch (exception e)
            {
                // Resposta de Erro para o remetente
                p.init_plain(0x30);
                p.WriteUInt16(0x113);
                p.WriteInt32(-1); // Status de Erro

                Player.Send(p);

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHAT_FRIEND][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        } 
    }
}
