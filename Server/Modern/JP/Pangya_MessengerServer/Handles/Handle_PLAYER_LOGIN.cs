using Pangya_MessengerServer.Repository;
using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Text.RegularExpressions;
namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_LOGIN : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            { 
                uint uid = Packet.ReadUInt32();
                var nickname = Packet.ReadString();

                Player.ResetHandShake(); // Reseta o handshake para evitar problemas de sincronização


                // 1. Validações Básicas (Anti-Hacker)
                if (uid == 0)
                    throw new Exception($"[Login Error] UID inválido para o nick {nickname}.");

                if (string.IsNullOrEmpty(nickname))
                    throw new Exception($"[Login Error] Nickname vazio para o UID {uid}.");

                // 2. Busca info no Banco de Dados (Assíncrono)
                var cmd_pi = new CmdPlayerInfo(uid);

                cmd_pi.exec();

                if (cmd_pi.getException().getCodeError() != 0)
                    throw cmd_pi.getException();

                // 3. Vincula os dados ao Player
                Player.UserInfo.Set(cmd_pi.getInfo());

                // 4. Verificação de Integridade (Nick DB vs Nick Packet)
                if (nickname != Player.UserInfo.NickName)
                    throw new Exception("[Login Error] Nickname divergente do Database.");

                // 5. Verificação de Bloqueio (Ban)
                if (Player.UserInfo.BlockFlag.State.Value != 0)
                {
                   await CheckPlayerBlock(Player); // Podemos isolar essa lógica num método private
                }

                // 6. Gerenciamento de Conexão Duplicada
                var PlayerAntiga = MessengerServer.Instance.HasLoggedWithOuterSocket(Player);
                if (PlayerAntiga != null)
                    MessengerServer.Instance.Disconnect(PlayerAntiga);

                // 7. Confirmação com o Auth Server
                if (MessengerServer.Instance.m_unit_connect != null)
                {
                    MessengerServer.Instance.m_unit_connect.getInfoPlayerOnline(Player.UserInfo.ServerIndex, Player.UserInfo.UID);
                }
                else
                {
                    MessengerServer.Instance.Disconnect(Player);
                }

                // Se o confirm for assíncrono, use await aqui também
                Player.UserInfo.m_friend_manager.init(Player.UserInfo);

                // Estado 4 = Online/Lobby
                Player.UserInfo.m_state = 4;
                Player.Authorized = true;

                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_LOGIN] Player[UID={Player.UserInfo.UID}, NICK={nickname}, NICK_DB={Player.GetNickname()}] logou com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));
                // Resposta de Sucesso (0x2F)
                var p = new Packet(0x2F);
                 p.WriteByte(0); // OK
                p.WriteUInt32(Player.UserInfo.UID);

               Player.Send(p);

            }
            catch (exception e)
            {
                var p = new Packet(0x2F);
                // Envia resposta de erro para o cliente (Packet 0x2F no Pangya)
                p.init_plain(0x2F);
                p.WriteByte(1); // ServerFlag de erro

                Player.Send(p);

                MessengerServer.Instance.Disconnect(Player);

                // Log de erro centralizado
                Console.WriteLine($"[Login Error] {e.Message}");
                _smp.LogManager.Instance.push(new AppMessage("[Handle_UPDATE_CHANNEL_INFO][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            await Task.CompletedTask;
        } 

        private async Task CheckPlayerBlock(Player Player)
        {
            var state = Player.UserInfo.BlockFlag.State;

            // Se o Value for 0, não há bloqueio, então saímos cedo (Early Return)
            if (state.Value == 0) return;

            // 1. Bloqueio Temporário
            if (state.BlockByTime && (state.TimeBlock == -1 || state.TimeBlock > 0))
            {
                string tempo = state.TimeBlock == -1
                    ? "indeterminado"
                    : $"{state.TimeBlock / 60}min {state.TimeBlock % 60}sec";

                throw new exception(
                    $"[MessengerServer] Bloqueado por tempo [{tempo}]. Player [UID={Player.UserInfo.UID}, ID={Player.UserInfo.Login}]",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1029, 0)
                );
            }

            // 2. Bloqueio Permanente
            if (state.BlockForever)
            {
                throw new exception(
                    $"[MessengerServer] Bloqueado permanente. Player [UID={Player.UserInfo.UID}, ID={Player.UserInfo.Login}]",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1030, 0)
                );
            }

            // 3. Bloqueio por IP (Onde entra a integração com o banco)
            if (state.BlockInIPAll)
            {
                // Aqui você adiciona o IP atual do infeliz na lista de banidos
                // Como é uma operação de escrita, o ideal é que seja disparada sem travar o login
               snmdb.NormalManagerDB.Instance.add(1, new CmdInsertBlockIp(Player.GetIP(), "255.255.255.255"), null, null);

                throw new exception(
                    $"[MessengerServer] Player [UID={Player.UserInfo.UID}, IP={Player.GetIP()}] Block ALL IP.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1031, 0)
                );
            }
        }
    }
}
