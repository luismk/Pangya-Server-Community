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
    public class Handle_PLAYER_CONFIRM_FRIEND : HandleBase<Player, Packet_EXAMPLE>//<Packet_PLAYER_CONFIRM_FRIEND, MPlayer>
    {
        public override async Task Handle()
        {
            var p = new Packet();

            try
            {
                uint uid = Packet.ReadUInt32();

                // Validação de segurança básica
                if (uid == 0)
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou aceitar Amigo[UID={uid}], mas UID é zero.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200801));

                var pFi = Player.UserInfo.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou aceitar UID={uid}, mas não está na lista.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200802));

                // Regra de negócio: Você não pode aceitar um convite que VOCÊ enviou (request_friend == 1 significa "eu pedi")
                if (pFi.state.request_friend.IsTrue())
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou aceitar convite enviado por ele mesmo para UID={uid}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200803));

                if (pFi.state._friend.IsTrue())
                    throw new exception($"Player[UID={Player.UserInfo.UID}] tentou aceitar UID={uid}, mas já são amigos.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200804));

                // Busca a sessão do amigo no Manager do Core
                var s = MessengerServer.Instance.FindSessionByUid(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {
                    // --- CASO: AMIGO ONLINE ---
                    if ((pFi2 = s.UserInfo.m_friend_manager.findFriend(Player.UserInfo.UID)) == null)
                        throw new exception($"Player[UID={Player.UserInfo.UID}] aceitou online, mas não está na lista de UID={uid}.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200804));

                    // Atualiza estados
                    pFi.state._friend = 1;
                    pFi2.state.request_friend = 0; // Remove a pendência de quem enviou
                    pFi2.state._friend = 1;

                    // Update Async no DB/Cache

                        Player.UserInfo.m_friend_manager.requestUpdateFriendInfo(pFi);
                        s.UserInfo.m_friend_manager.requestUpdateFriendInfo(pFi2); 

                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CONFIRM_FRIEND][Log] {Player.UserInfo.UID} aceitou {s.UserInfo.UID} (Online)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para quem ACEITOU (0x109)
                    p.init_plain(0x30);
                    p.Write((ushort)0x109);
                    p.Write((uint)0); // OK
                    p.Write((uint)s.UserInfo.UID);
                    Player.Send(p);

                    // Resposta para quem ENVIOU o convite (0x10A)
                    p.init_plain(0x30);
                    p.Write((ushort)0x10A);
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
                    if (pi.UID == 0)
                        throw new exception("[MessengerService::requestConfirmFriend] Player offline não existe no DB.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200806));

                    var fm = new FriendManager(pi);
                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerService::requestConfirmFriend] Falha ao iniciar FriendManager offline.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 7, 0x5200807));

                    if ((pFi2 = fm.findFriend(Player.UserInfo.UID)) == null)
                        throw new exception("Player não está na lista offline do amigo.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200805));

                    pFi.state._friend = 1;
                    pFi2.state.request_friend = 0;
                    pFi2.state._friend = 1; 

                        Player.UserInfo.m_friend_manager.requestUpdateFriendInfo(pFi);
                        fm.requestUpdateFriendInfo(pFi2); 

                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CONFIRM_FRIEND][Log] {Player.UserInfo.UID} aceitou {pi.UID} (Offline)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta 0x109
                    p.init_plain(0x30);
                    p.Write((ushort)0x109);
                    p.Write((uint)0);
                    p.Write((uint)pi.UID);
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CONFIRM_FRIEND][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);
                p.Write((ushort)0x109);
                uint err = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200800;
                p.Write((uint)err);
                Player.Send(p);
            }
        }
    }
}
