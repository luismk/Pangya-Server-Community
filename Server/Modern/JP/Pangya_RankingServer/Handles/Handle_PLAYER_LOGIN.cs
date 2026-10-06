using Pangya_RankingServer.Repository;
using Pangya_RankingServer.Server;
using Pangya_RankingServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Handles
{
    public class Handle_PLAYER_LOGIN : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint uid = Packet.ReadUInt32();
                string id = Packet.ReadString();

                Player.UserInfo.m_sd.ToRead(Packet);

                Player.ResetHandShake(); // Reseta o handshake para evitar problemas de sincronização


                // 1. Validações de Segurança
                if (uid == 0 || string.IsNullOrEmpty(id))
                {
                    throw new exception($"[{nameof(Handle_PLAYER_LOGIN)}] [Login Error] Dados inválidos. UID: {uid}, ID: {id}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 1, 0x5200101));
                }

                if (RankingServer.Instance.haveBanList(Player.GetIP(), "", false))
                {
                    throw new exception($"[{nameof(Handle_PLAYER_LOGIN)}] [Login Error] IP Banido: {Player.GetIP()}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 5, 0x5200105));
                }

                // 2. Consulta ao Banco de Dados (CmdPlayerInfo)
                var cmd_pi = new CmdPlayerInfo(uid);
                cmd_pi.exec();

                if (cmd_pi.getException().getCodeError() != 0)
                    throw cmd_pi.getException();

                // 3. Sincroniza dados do DB com a Sessão
                Player.UserInfo.Set(cmd_pi.getInfo());

                // 4. Verificação de Integridade
                if (string.CompareOrdinal(id.Trim(), Player.UserInfo.Login.Trim()) != 0)
                {
                    throw new exception($"[{nameof(Handle_PLAYER_LOGIN)}] [Login Error] ID divergente! Packet: {id} vs DB: {Player.UserInfo.Login}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 4, 0x5200104));
                }

                // 5. Verificação de Bloqueios
                await CheckPlayerBlock();

                // 6. Gerenciamento de Múltiplas Conexões
                var PlayerAntiga = RankingServer.Instance.HasLoggedWithOuterSocket(Player);
                if (PlayerAntiga != null)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_LOGIN] Derrubando sessão antiga do UID: {uid}", type_msg.CL_ONLY_CONSOLE));
                    RankingServer.Instance.Disconnect(PlayerAntiga);
                }

                if (RankingServer.Instance.m_unit_connect != null)
                {
                    RankingServer.Instance.m_unit_connect.getInfoPlayerOnline(Player.UserInfo.ServerIndex, Player.UserInfo.UID);
                }
                else
                {
                    RankingServer.Instance.Disconnect(Player);
                }
            }
            catch (exception e)
            { 

                // Log de Erro com o Name da classe
                _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_PLAYER_LOGIN)}] [Error] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                RankingServer.Instance.Disconnect(Player);
            }
        }

        private async Task CheckPlayerBlock()
        {
            var state = Player.UserInfo.BlockFlag.State;
            if (state.Value == 0) return;

            if (state.BlockByTime && (state.TimeBlock == -1 || state.TimeBlock > 0))
            {
                throw new exception($"[{nameof(Handle_PLAYER_LOGIN)}] Bloqueio temporário ativo.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 1029, 0));
            }

            if (state.BlockForever)
            {
                throw new exception($"[{nameof(Handle_PLAYER_LOGIN)}] Bloqueio permanente ativo.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 1030, 0));
            }
        }        
    }
}