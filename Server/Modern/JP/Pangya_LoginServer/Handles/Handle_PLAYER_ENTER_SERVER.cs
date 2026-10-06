using Pangya_LoginServer.DataBase;
using Pangya_LoginServer.Server;
namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_ENTER_SERVER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint server_uid = Packet.ReadUInt32();

                if (server_uid == 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_PLAYER_ENTER_SERVER][Log] UID inválido de {Player.UserInfo.Login}", type_msg.CL_ONLY_CONSOLE));
                    LoginServer.Instance.Disconnect(Player);
                    return;
                }

                // 2. Busca lista de servidores (Idealmente em cache para não pesar o DB)
                var servers = CommandDB.GetGame();
                var selectedServer = servers.FirstOrDefault(c => c.UID == server_uid);

                if (selectedServer != null)
                {
                    // 3. Registra e pega a chave 
                    string authKey = CommandDB.RegisterAndGetAuthKey(Player.UserInfo.UID, server_uid);

                    if (string.IsNullOrEmpty(authKey))
                        throw new Exception("Falha ao gerar AuthKey no Banco de Dados.");

                    // 4. Envia chave de transição
                    Player.Send(Handle_PACKET_RESPONSE.pacote003(authKey));
                    //enviar um aviso ao server que ele vai entrar..


                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_ENTER_SERVER][Log] PLAYER[UID: {Player.UserInfo.UID}, ID: {Player.UserInfo.Login}, SRV: {selectedServer.Name}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_PLAYER_ENTER_SERVER][Log] Servidor {server_uid} offline ou inexistente.", type_msg.CL_ONLY_CONSOLE));

                    // Opcional: Enviar erro antes de desconectar
                    // Player.Send(LoginPackets.pacoteError(0x0E));
                    LoginServer.Instance.Disconnect(Player);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SelectServer Error] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}