using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Repository;
using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_ACEEPT_TO_GUILD : HandleBase<Player, Packet_EXAMPLE>
    {
        public const int FRIEND_PAG_LIMIT = 30;

        public override async Task Handle()
        {
            var p = new Packet();

            try
            {
                uint club_id = Packet.ReadUInt32();
                uint member_uid = Packet.ReadUInt32();

                if (club_id == 0u || member_uid == 0u)
                {
                    throw new exception("[MessengerService::HandleAcceptGuildMember][Error] club_id ou member_uid inválido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));
                }

                // Busca todos os membros da Guild que estão online no Messenger
                var v_cm = MessengerServer.Instance.FindAllGuildMember(club_id);

                if (v_cm.Count == 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[HandleAcceptGuildMember][WARNING] Club[ID={club_id}] não tem membros online para atualizar.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Atualiza o FriendManager de todos os membros online da Guild (para incluírem o novo membro)
                foreach (var member in v_cm.Values.Where(m => m != null))
                {
                    member.UserInfo.m_friend_manager.init(member.UserInfo);
                }

                // Verifica se o novo membro está online agora
                var s = MessengerServer.Instance.FindPlayer(member_uid);
                PlayerInfo pi = new();

                if (s == null || !s.Connected)
                {
                    // --- PLAYER ACEITO ESTÁ OFFLINE ---
                    var cmd_pi = new CmdPlayerInfo(member_uid);
                    NormalManagerDB.Instance.add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0) throw cmd_pi.getException();
                    pi.Set(cmd_pi.getInfo());
                }
                else
                {
                    // --- PLAYER ACEITO ESTÁ ONLINE ---
                    s.UserInfo.GuildIndex = club_id;
                    s.UserInfo.m_friend_manager.init(s.UserInfo);
                    pi = s.UserInfo;
                }

                // Sincroniza a lista de amigos/Guild (Packet 0x102) para todos os membros online
                foreach (var member in v_cm.Values.Where(m => m != null))
                {
                    MessengerServer.Instance.SendUpdatedFriendList(member);
                }

                // Notifica a Guild (Broadcast) que o player foi aceito (Packet 0x3B)
                p.init_plain(0x3B);
                p.Write(pi.UID);
                p.Write(club_id);
                p.Write(pi.Gender);
                p.WriteString(pi.Login);
                p.WriteString(pi.NickName);
                p.Write((ushort)0x1F); // ServerFlag padrão Pangya (USA/International)

                MessengerServer.Instance.FriendBroadcast(v_cm, s, p);

                _smp.LogManager.Instance.push(new AppMessage($"[HandleAcceptGuildMember][Log] Player[UID={member_uid}] aceito no Club[UID={club_id}] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::HandleAcceptGuildMember][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        } 
    }
}
