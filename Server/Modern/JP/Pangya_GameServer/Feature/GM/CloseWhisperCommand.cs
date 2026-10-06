using Pangya_GameServer.Feature.GM;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Feature.GM
{
    public class CloseWhisperCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet packet)
        {
            // 1. Leitura do Nickname (PStr = Pascal String ou padrão do servidor)
            string nickname = packet.ReadPStr();

            // 2. Validação básica de input
            if (string.IsNullOrEmpty(nickname))
            {
                throw new exception($"[CLOSE_WHISPER][Error] UID={session.UserInfo.UID} enviou NickName vazio.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700108));
            }

            // 3. Verificação de permissão GM via PlayerUserStatistics
            if (!session.UserInfo.UserCapabilities.IsGameMaster)
            {
                throw new exception($"[CLOSE_WHISPER][Error] UID={session.UserInfo.UID} não é GM.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));
            }

            // 4. Localização do alvo pelo Nickname
            var targetSession = GameServer.Instance.FindSessionByNickname(nickname);

            if (targetSession == null)
            {
                throw new exception($"[CLOSE_WHISPER][Error] Nickname '{nickname}' não encontrado no servidor.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700109));
            }

            // 5. Execução: Remove o alvo da lista de sussurros do GM
            // Nota: m_gi (GameInterface/Info) gerencia o estado visual/interativo da sessão
            session.m_gi.closePlayerWhisper(targetSession.UserInfo.UID);

            // Log para debug
            Console.WriteLine($"[GM-Whisper] {session.UserInfo.NickName} removeu {nickname} da lista de whispers.");

        await Task.CompletedTask;
        }
    }
}