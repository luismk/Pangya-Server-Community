using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SEND_TICKER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                if (Player.UserInfo.BlockFlag.Flag.TIcker)
                {
                    throw new exception(
                        $"[Handle_PLAYER_SEND_TICKER][Error] Normal[UID={Player.UserInfo.UID}] está com TIcker bloqueado.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 10, 1));
                }

                // 2. Leitura e Sanitização
                string msg = Packet.ReadString();

                if (string.IsNullOrEmpty(msg))
                {
                    throw new exception(
                        $"[Handle_PLAYER_SEND_TICKER][Error] Normal[UID={Player.UserInfo.UID}] enviou TIcker vazio.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                if (!Tools.Sanitize(msg))
                {
                    throw new exception(
                        $"[Handle_PLAYER_SEND_TICKER][Security] Normal[UID={Player.UserInfo.UID}] tentou Injection no TIcker.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                try
                {
                    // 3. Lógica de Consumo (Moeda: Cookie)
                    // Inicializa o log de gastos
                    CPLog cp_log = new();
                    cp_log.setType(CPLog.TYPE.TICKER);
                    cp_log.setCookie(1);

                    // Deduz o cookie (Lança exception se não tiver saldo ou erro no DB)
                    Player.UserInfo.consomeCookie(1);

                    // 4. Adiciona à fila de Broadcast do servidor local
                    // Tempo de expiração: Agora + 3 segundos
                    int expireTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3;

                    GameServer.Instance.SendTicker.push_back(expireTime, Player.UserInfo.NickName, msg, Manager.BroadcastManager.TYPE.TICKER);

                    // 5. Persistência e Sincronização Global
                    // Comando 6: Insere o TIcker no DB para o Auth/outros GameServers lerem

                    CommandDB.InsertTicker(Player.UserInfo.UID, GameServer.Instance.getUID(), msg);
                    // Salva o log de gastos
                    Player.saveCPLog(cp_log);

                    // Log de Sucesso no Console
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Ticker::Success] Normal[UID={Player.UserInfo.UID}] enviou: {msg}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // 6. Resposta ao Cliente (0x96 - Atualiza saldo de Cookies)
                    using (var response = new Packet())
                    {
                        response.init_plain(0x96);
                        response.WriteUInt64(Player.UserInfo.Cookie);
                        Player.Send(response);
                    }
                }
                catch (exception e)
                {
                    // Tratamento específico de erros de Cookie
                    if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.PLAYER_INFO, 20))
                    {
                        throw new exception($"[Ticker][Error] UID={Player.UserInfo.UID} sem saldo suficiente.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 4));
                    }

                    // Se o erro não for de saldo, mas falhou após o consumo, devolvemos o cookie
                    Player.UserInfo.addCookie(1);
                    throw;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_SEND_TICKER][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta de erro ao cliente (Usando o pacote de feedback genérico)
                SendErrorResponse(Player, e);
            }

        await Task.CompletedTask;
        }

        private void SendErrorResponse(Player session, exception e)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x50); // Pacote genérico de erro/status
                uint errorCode = (ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME_SERVER)
                                 ? ExceptionError.STDA_ERROR_DECODE(e.getCodeError())
                                 : 1;
                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}