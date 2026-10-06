using Pangya_GameServer.Feature.GM;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_COMMAND_GM : HandleBase<Player, Packet_EXAMPLE>
    {
        // Registro centralizado de comandos
        private static readonly Dictionary<COMMON_CMD_GM, IGMCommand> _commands = new()
        {
            { COMMON_CMD_GM.CCG_DESTROY, new DummyCommand() },
            { COMMON_CMD_GM.CCG_VISIBLE, new VisibleCommand() },
            { COMMON_CMD_GM.CCG_KICK, new KickCommand() },
            { COMMON_CMD_GM.CCG_CHANGE_WEATHER, new WeatherCommand() },
            { COMMON_CMD_GM.CCG_CHANGE_WIND, new WindCommand() },
            { COMMON_CMD_GM.CCG_IDENTITY, new IdentityCommand() },//identity agora é packet!!!!!
            { COMMON_CMD_GM.CCG_WHISPER, new WhisperCommand() },
            { COMMON_CMD_GM.CCG_CHANNEL, new ChannelCommand() },
            { COMMON_CMD_GM.CCG_CLOSE_WHISPER, new CloseWhisperCommand() },
            { COMMON_CMD_GM.CCG_OPEN_WHISPER, new OpenWhisperCommand() },
            { COMMON_CMD_GM.CCG_DISCONNECT, new DisconnectCommand() },
            { COMMON_CMD_GM.CCG_MATCH_HOLE, new MatchHoleCommand() },
            { COMMON_CMD_GM.CCG_MATCH_MAP, new MatchMapCommand() },
            { COMMON_CMD_GM.CCG_GOLDENBELL, new GoldenBellCommand() }
        };

        public override async Task Handle()
        {
            try
            {
                // 1. Sincronização de Estado (Capability)
                UpdatePlayerCapability(Player);

                // 2. Identificação do Comando (Int16 padrão Pangya)
                var cmdId = (COMMON_CMD_GM)Packet.ReadInt16();

                // 3. Validação de Segurança (Gatekeeper)
                if (cmdId != COMMON_CMD_GM.CCG_IDENTITY)//se for diferente, ele confisca.
                {
                    if (!Player.UserInfo.UserCapabilities.IsGameMaster)
                    {
                        LogSecurityAlert(Player, cmdId);
                        return;
                    }
                }

                // 4. Despacho para o Comando Específico
                if (_commands.TryGetValue(cmdId, out var command))
                {
                    var room = Player.GetRoom();

                    if ((cmdId == COMMON_CMD_GM.CCG_MATCH_HOLE || cmdId == COMMON_CMD_GM.CCG_MATCH_MAP) && room == null)//jogo esta em percuso, nao pode editar.
                    {
                        Player.SendChatNotice("This needs to be in a room first.");
                        return;
                    }
                    if ((cmdId == COMMON_CMD_GM.CCG_MATCH_HOLE || cmdId == COMMON_CMD_GM.CCG_MATCH_MAP) && room != null && room.GameRun())//jogo esta em percuso, nao pode editar.
                    {
                        Player.SendChatNotice("It can only be executed if you are not in the classroom.");
                        return;
                    }
                    LogCommandExecution(Player, cmdId);
                    await command.Execute(Player, Packet); 
                    if(cmdId != COMMON_CMD_GM.CCG_DISCONNECT)
                    Player.SendChatNotice("Command Executed.");
                }
                else
                {
                    LogUnknownCommand(cmdId);
                }
            }
            catch (Exception ex)
            {
                // Tratamento de erro centralizado para evitar crash da Task de rede
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_GM Error] UID:{Player.UserInfo.UID} | Ex: {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                Player.SendChatNotice("command by GM.");
            }
        }

        private void UpdatePlayerCapability(Player s)
        {
            // Se o Player está invisível (Visible > 0), resetamos a Capability visual 
            // para garantir que o cliente não exiba ícones de GM indevidamente
            s.UserInfo.Member.Capability = (s.m_gi.visible > 0) ? new PlayerCapability() : s.UserInfo.UserCapabilities;
        }

        #region Helpers de Log (Auditoria)

        private void LogCommandExecution(Player s, COMMON_CMD_GM cmdId)
        {
            _smp.LogManager.Instance.push(new AppMessage(
               $"[GM-Command] Executado: {cmdId} | Por: {s.UserInfo.NickName} (UID: {s.UserInfo.UID})",
               type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private void LogSecurityAlert(Player s, COMMON_CMD_GM cmdId)
        {
            _smp.LogManager.Instance.push(new AppMessage(
                $"[SECURITY-ALERT] Tentativa de uso de comando GM sem permissão! UID: {s.UserInfo.UID} | CMD: {cmdId}",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private void LogUnknownCommand(COMMON_CMD_GM cmdId)
        {
            _smp.LogManager.Instance.push(new AppMessage(
                $"[GM-Warning] Comando recebido mas não implementado: {cmdId}",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        #endregion
    }
}