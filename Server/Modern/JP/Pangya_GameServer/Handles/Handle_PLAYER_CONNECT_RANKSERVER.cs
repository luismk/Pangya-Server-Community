using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CONNECT_RANKSERVER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 1. Validação de bloqueio
                if (Player.UserInfo.BlockFlag.Flag.RankService)
                {
                    throw new exception($"[Handle][UID={Player.UserInfo.UID}] Jogador bloqueado para Rank Server.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7010, 0));
                }

                // 2. Busca servidor no DB 
                var serverList = DBCommand.GetRank();

                // 3. Verifica disponibilidade
                if (serverList == null || serverList.Count == 0)
                {
                    throw new exception($"[Handle][UID={Player.UserInfo.UID}] Requisitou Rank Server, mas nenhum está online no DB.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7011, 0));
                }

                // 4. Envio de sucesso (conecta ao primeiro disponível)
                using var p = new Packet(0xA2);
                p.WriteString(serverList[0].IpAddress);
                p.WriteInt32(serverList[0].Port);

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CONNECT_RANKSERVER][Error] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta de falha (IP vazio e porta 0)
                using var p = new Packet(0xA2);
                p.WriteUInt16(0); // String IP vazia
                p.WriteUInt32(0); // Port zero

                Player.Send(p);
            }
        }
    }
}