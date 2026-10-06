using System;
using System.Threading.Tasks;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Feature.GM
{
    public class KickCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        {
            var channel = session.GetChannel();

            try
            {
                // 1. Leitura dos dados do pacote
                int targetOid = pkt.ReadInt32();
                byte forceKick = pkt.ReadByte(); // 1 = Force, 0 = Normal

                // 2. Localiza a sessão do alvo pelo OID
                var target = GameServer.Instance.FindSessionByOid(targetOid);

                if (target == null)
                {
                    throw new exception(
                        $"[GM::Kick] Player[UID={session.UserInfo.UID}] tentou chutar OID={targetOid}, mas o alvo não foi encontrado.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0));
                }

                // 3. Localiza a sala do alvo
                var targetRoom = target.GetRoom();

                if (targetRoom == null)
                {
                    // Se o player não está em uma sala, o comando /kick (que é de sala) não faz sentido.
                    // Para derrubar o player do servidor completamente, seria outro comando (ex: /dc).
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::Kick][Warning] Alvo[UID={target.UserInfo.UID}] não está em uma sala ativa.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                if (target.UserInfo.UID == session.UserInfo.UID)
                {
                    // Enviamos um aviso para o chat do próprio GM em vez de dar erro fatal
                    session.SendChatNotice("no executed, other player");

                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::Kick][Warning] GM {session.UserInfo.NickName} tentou se auto-desconectar (Bloqueado).",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return; // Interrompe a execução aqui
                }

                // 4. Log de Auditoria
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[GM::Kick][Success] GM[UID={session.UserInfo.UID}] expulsou Player[UID={target.UserInfo.UID}, Nick={target.UserInfo.NickName}] da Sala={targetRoom.GetRoomId()} (Force={forceKick})",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // 5. Execução do Kick
                // O método KickPlayerRoom cuida de remover o player da lista da sala e enviar o pacote 0x46 (Kick)
                channel?.KickPlayerRoom(target, forceKick); 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[KickCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}