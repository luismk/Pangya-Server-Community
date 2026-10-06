using Pangya_MessengerServer.Manager;
using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Repository;
using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_DELETE_FRIEND : HandleBase<Player, Packet_EXAMPLE>//<Packet_PLAYER_DELETE_FRIEND, MPlayer>
    {

        public override async Task Handle()
        {
            // REQUEST_BEGIN("DeleteFriend");
            var p = new Packet();

            try
            {
                uint uid = Packet.ReadUInt32();
                var nickname = Packet.ReadString();

                // Validações de integridade
                if (uid == 0)
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou deletar UID={uid}, mas UID é inválido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200701));

                if (string.IsNullOrEmpty(nickname))
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou deletar UID={uid}, mas Nickname está vazio.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200702));

                // Busca o amigo na lista local do player
                var pFi = Player.UserInfo.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou deletar UID={uid}, mas não são amigos.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200703));

                // Tenta encontrar a sessão ativa do alvo
                var s = MessengerServer.Instance.FindSessionByUid(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {
                    // --- CASO: AMIGO ONLINE ---
                    if (!nickname.Equals(s.UserInfo.NickName, StringComparison.OrdinalIgnoreCase))
                        throw new exception($"Nickname não bate para o amigo online UID={uid}.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 6, 0x5200705));

                    if ((pFi2 = s.UserInfo.m_friend_manager.findFriend(Player.UserInfo.UID)) == null)
                        throw new exception($"Inconsistência: Player não está na lista do amigo UID={uid}.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 4, 0x5200704));

                    // Remove de ambos os managers e sincroniza com o DB (Async)
                  
                        Player.UserInfo.m_friend_manager.requestDeleteFriend(pFi);
                        s.UserInfo.m_friend_manager.requestDeleteFriend(pFi2); 

                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_DELETE_FRIEND][Log] {Player.UserInfo.UID} removeu {s.UserInfo.UID} (Online)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para quem deletou
                    p.init_plain(0x30);
                    p.Write((ushort)0x10B);
                    p.Write((uint)0); // OK
                    p.Write((uint)s.UserInfo.UID);
                    Player.Send(p);

                    // Resposta para quem FOI deletado (remove da lista dele em tempo real)
                    p.init_plain(0x30);
                    p.Write((ushort)0x10B);
                    p.Write((uint)0); // OK
                    p.Write((uint)Player.UserInfo.UID);
                    s.Send(p);
                }
                else
                {
                    // --- CASO: AMIGO OFFLINE ---
                    var cmd_pi = new CmdPlayerInfo(uid);

                    snmdb.NormalManagerDB.Instance.add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0) throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();
                    if (pi.UID == 0 || !nickname.Equals(pi.NickName, StringComparison.OrdinalIgnoreCase))
                        throw new exception("Player offline inválido ou nick não bate.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200705));

                    var fm = new FriendManager(pi);
                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("Erro ao carregar FriendManager offline.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 7, 0x5200707));

                    if ((pFi2 = fm.findFriend(Player.UserInfo.UID)) == null)
                        throw new exception("Player não consta na lista offline do amigo.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 8, 0x5200708));

                    Player.UserInfo.m_friend_manager.requestDeleteFriend(pFi);
                    fm.requestDeleteFriend(pFi2);

                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_DELETE_FRIEND][Log] {Player.UserInfo.UID} removeu {pi.UID} (Offline)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o solicitante
                    p.init_plain(0x30);
                    p.Write((ushort)0x10B);
                    p.Write((uint)0);
                    p.Write((uint)pi.UID);
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_DELETE_FRIEND][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);
                p.Write((ushort)0x10B);
                uint err = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200700;
                p.Write((uint)err);
                Player.Send(p);
            }
        }
    }
}
