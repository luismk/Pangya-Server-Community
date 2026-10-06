using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Threading.Tasks;
using static Pangya_GameServer.Feature.Personal.PersonalShop;
using static System.Collections.Specialized.BitVector32;

namespace Pangya_GameServer.Feature.GM
{
    public class IdentityCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        {
            try
            {
                // 1. Leitura do Pacote
                PlayerCapability requestedCap = new PlayerCapability(pkt.ReadInt32());
                string nickProvided = pkt.ReadString();

                // 2. Validações de Segurança
                if (string.IsNullOrEmpty(nickProvided) || nickProvided != session.UserInfo.NickName)
                {
                    throw new exception("Nick inválido ou não coincide com a sessão.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 12, 0x5700100));
                }

                // 3. Verificação de permissão de GM (prevenção de exploit)
                if (!session.UserInfo.UserCapabilities.IsGameMasterNormal && !session.UserInfo.UserCapabilities.IsGameMaster)
                {
                    throw new exception("Acesso negado: Player não possui privilégios administrativos.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 13, 0x5700100));
                }

                // 4. Validação no Banco de Dados (Sincronizada)
                var dbVerify = new CmdVerifyCapability(session.UserInfo.UID);

                NormalManagerDB.Instance.add(0, dbVerify);

                if (!dbVerify.IsValid())
                {
                    throw new exception("Falha na validação de identidade no Banco de Dados.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                }

                // 5. Aplicação da Identidade
                bool toAdmin = requestedCap.Value == -1;

                ApplyCapabilityFlags(session, toAdmin);

                // 6. Sincronização de Rede
                SyncIdentityWithServer(session);

                // Log de Auditoria
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[GM::Identity] {session.UserInfo.NickName} alterou HoleMode para: {(toAdmin ? "TITLE GM" : "NORMAL")}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[IdentityCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }

        private void ApplyCapabilityFlags(Player s, bool toAdmin)
        {
            if (toAdmin)
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