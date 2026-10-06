using Pangya_GameServer.Flags;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Feature.GM
{
    public class WindCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        { 
            try
            {  
                var room = session.GetGameRoom();

                // 3. Validação de Integridade
                if (room == null)
                {
                    throw new exception($"[GM::Wind] Sala {session.UserInfo.Member.RoomID} não encontrada para o player [UID={session.UserInfo.UID}].",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0x5700100));
                }
                
                if (room.GetTipo() != Flags.RoomTypeFlags.LOUNGE || room.GetTipo() != RoomTypeFlags.PANG_BATTLE || room.GetTipo() != RoomTypeFlags.STROKE || room.GetTipo() != RoomTypeFlags.MATCH)
                {
                    throw new exception("[Room::RequestExecCCGChangeWindVersus] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou executar o comando de troca de vento na sala[NUMERO=" + room.GetRoomId() + ", TIPO=" + Convert.ToString(room.GetTipo()) + "], mas o Type da sala nao é Stroke ou Match HoleMode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5700100));
                }

                //somente no lounger é bloqueado...
                if (room != null && room.GameInitState > 0)
                {
                    // Delegamos a lógica de alteração do vento Versus para a instância da sala
                    room.RequestExecCCGChangeWind(session, pkt);

                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::Wind][Success] {session.UserInfo.NickName} alterou o vento na Sala {room.GetRoomId()} (Canal: {session.GetChannel().getName()})",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::Wind][Warning] {session.UserInfo.NickName} tentou alterar o vento sem estar em uma sala ativa.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[WindCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}