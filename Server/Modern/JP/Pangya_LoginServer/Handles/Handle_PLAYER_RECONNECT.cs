using Pangya_LoginServer.DataBase;

using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities.Log;
using System.Text.RegularExpressions;

namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_RECONNECT : HandleBase<Player, Packet_EXAMPLE>
    {
        private static readonly Regex InvalidIdRegex = new Regex(@".*[\^$&,\\?`´~\|""@#¨'%*!\\].*", RegexOptions.Compiled);

        // Alterado para Task para suportar await corretamente
        public override async Task Handle()
        {
            try
            { 
                // 1. Leitura dos dados do pacote (Estrutura padrão Re-Login)
                string id = Packet.ReadString();
                int server_uid = Packet.ReadInt32(); // ID do servidor que ele estava
                string auth_key_login_received = Packet.ReadString();

                // 2. Validação básica de caracteres no ID (Segurança)
                if (string.IsNullOrEmpty(id) || InvalidIdRegex.IsMatch(id))
                {
                    Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500052));
                    return;
                }

                // 3. Busca o UID pelo ID (Operação Assíncrona no DB)
                int uid = CommandDB.VerifyID(id);

                if (uid <= 0)
                {
                    Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500052));
                    return;
                }

                // 4. Busca dados do Player e a AuthKey original (Async)
                // Rodando em paralelo para maior performance
                var PlayerInfoTask = CommandDB.GetPlayerInfo((uint)uid);
                var authKeyTask = CommandDB.GetAuthKeyLogin((uint)uid); 

                var info = PlayerInfoTask;
                string akli = authKeyTask;

                // 5. Validação de Integridade (Se a chave bate com o banco)
                if (auth_key_login_received != akli)
                {
                    Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500052));
                    return;
                }

                // 6. Atualiza a sessão com os dados do banco
                Player.UserInfo.Set(info);

                // 7. Verificações de bloqueio e BAN
                if (CheckBlockStatus(Player))
                {
                    // O método CheckBlockStatus lança exceção ou envia erro
                    return;
                }
                 
                // 9. Finaliza o login usando o SUCCESS_LOGIN (option 1 = Reconnect)
                // Isso envia a lista de servidores e confirma a entrada
                await Handle_PLAYER_LOGIN.SUCCESS_LOGIN(Player, 1);
            }
            catch (Exception ex)
            {
                // Log de erro centralizado
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_RECONNECT][Error] {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500052));
            }
            await Task.CompletedTask;
        }

        private bool CheckBlockStatus(Player Player)
        {
            var state = Player.UserInfo.BlockFlag.State;

            if (state.Value == 0) return false;

            if (state.BlockForever || state.BlockByTime)
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500052));
                return true;
            }

            // Adicione outras regras de BAN/IP conforme necessário
            return false;
        }
    }
}