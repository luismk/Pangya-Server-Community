using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase.Models;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_GM_CHANGE_IDENTITY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 1. Leitura do Pacote
                PlayerCapability requestedCap = new PlayerCapability(Packet.ReadInt32());
                string nickProvided = Packet.ReadString();

                // 2. Validações de Segurança
                if (string.IsNullOrEmpty(nickProvided) || nickProvided != Player.UserInfo.NickName)
                {
                    throw new exception("Nick inválido ou não coincide com a sessão.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 12, 0x5700100));
                }

                // 3. Verificação de permissão de GM (prevenção de exploit)
                if (!Player.UserInfo.UserCapabilities.IsGameMasterNormal && !Player.UserInfo.UserCapabilities.IsGameMaster)
                {
                    throw new exception("Acesso negado: Player não possui privilégios administrativos.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 13, 0x5700100));
                }

                // 4. Validação no Banco de Dados (Sincronizada)
                var dbVerify = new CmdVerifyCapability(Player.UserInfo.UID);

                NormalManagerDB.Instance.add(0, dbVerify);

                if (!dbVerify.IsValid())
                {
                    throw new exception("Falha na validação de identidade no Banco de Dados.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                }
                 
                ApplyCapabilityFlags(Player);

                // 6. Sincronização de Rede
                SyncIdentityWithServer(Player);

                // Log de Auditoria
                _smp.LogManager.Instance.push(new AppMessage($"[Lobby::Identity][Sucess]CHANGE[NICK: {Player.UserInfo.NickName}, TYPE: {(Player.UserInfo.UserCapabilities.IsGameMasterTitle ? "TITLE GM" : "NORMAL")}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Lobby::Identity][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }

        private void ApplyCapabilityFlags(Player s)
        {
            if (s.UserInfo.UserCapabilities.IsGameMasterNormal)
            {
                s.UserInfo.UserCapabilities.IsGameMaster = true;
                s.UserInfo.UserCapabilities.IsGameMasterTitle = true;
                s.UserInfo.UserCapabilities.IsGameMasterNormal = false;
            }
            else
            {
                s.UserInfo.UserCapabilities.IsGameMaster = false;
                s.UserInfo.UserCapabilities.IsGameMasterTitle = false;
                s.UserInfo.UserCapabilities.IsGameMasterNormal = true;
            }
        }

        private void SyncIdentityWithServer(Player s)
        {
            //atualiza a capacidade nova.
            s.UserInfo.Member.Capability = s.UserInfo.UserCapabilities;

            var channel = s.GetChannel();
            var room = s.GetRoom();

            // Atualiza Info no Lobby/Sala
            channel?.Lobby.UpdatePlayerInfo(s);
            room?.UpdatePlayerInfo(s);
            s.Send(Handle_PACKET_RESPONSE.pacote09A(s.UserInfo.UserCapabilities.Value));

            // Broadcast (Tipo 3: State Update)
            channel?.Lobby.SendUpdatePlayerInfo(s, 3);
            room?.SendPlayerInfo(s, 3);
        }
    }
}