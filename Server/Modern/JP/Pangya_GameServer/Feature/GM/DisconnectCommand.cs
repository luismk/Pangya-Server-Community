using System;
using System.Threading.Tasks;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Feature.GM
{
    /// <summary>
    /// esse comando, nao esta sendo usado pelo jogo, porem ele usa o Handle_PLAYER_KICK_FROM_ROOM 
    /// </summary>
    public class DisconnectCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        {
            try
            {
                // 1. Leitura do OID (Object ID) do alvo
                int targetOid = pkt.ReadInt32();

                // 2. Localização da sessão no Singleton do GameServer
                var target = GameServer.Instance.FindSessionByOid(targetOid);

                if (target == null)
                {
                    throw new exception($"[GM::Disconnect] Alvo [OID={targetOid}] não encontrado no pool de sessões.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0));
                }

                if (target.UserInfo.UID == session.UserInfo.UID)
                { 
                    return; // Interrompe a execução aqui
                } 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[DisconnectCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}