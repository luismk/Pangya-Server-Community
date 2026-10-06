using Pangya_RankingServer.Manager;
using Pangya_RankingServer.Models;
using Pangya_RankingServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Handles
{
    public class Handle_SEARCH_PLAYER_IN_RANK : HandleBase<Player, Packet_EXAMPLE>
    {
        public enum SEARCH_OPTION : byte
        {
            NICKNAME = 0,
            POSITION = 1
        }

        public override async Task Handle()
        {
            try
            {
                if (!Player.Authorized)
                    throw new Exception($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] Sessão não autorizada para busca no ranking.");

                SEARCH_OPTION option = (SEARCH_OPTION)Packet.ReadByte();

                var sd = new SearchData(Packet);
                if (option == SEARCH_OPTION.NICKNAME)
                {
                    string nickname = Packet.ReadString();

                    if (string.IsNullOrEmpty(nickname))
                    {
                        throw new exception($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] [Search Error] Nickname vazio para UID {Player.UserInfo.UID}.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 331, 0));
                    }

                    // Log de monitoramento opcional
                    _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_REQUEST_PLAYER_INFO)}][Log] PLAYER[UID: {Player.UserInfo.UID}, REQUEST: {nickname},SEARCH_{option}] ", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    sRankRegistryManager.Instance.searchPlayerByNicknameAndSendPage(Player, nickname, sd);
                }
                else if (option == SEARCH_OPTION.POSITION)
                {
                    uint position = Packet.ReadUInt32();

                    if (position == 0)
                    {
                        throw new exception($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] [Search Error] Posição inválida ({position}) para UID {Player.UserInfo.UID}.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 332, 0));
                    } 

                    _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_REQUEST_PLAYER_INFO)}][Log] PLAYER[UID: {Player.UserInfo.UID}, NICK: {Player.UserInfo.NickName}, REQUEST: SEARCH_{option}] ", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    sRankRegistryManager.Instance.searchPlayerByRankAndSendPage(Player, position, sd);
                }
                else
                {
                    throw new exception($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] [Search Error] Opção de busca inválida ({option}) para UID {Player.UserInfo.UID}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 330, 0));
                }
            }
            catch (exception e)
            {
                // Log formatado com o Name da classe
                _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] [Error] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                var p = new Packet(0x138C);
                p.WriteByte(1);
                Player.Send(p);
            }
            catch (Exception ex)
            {
                // Erro crítico/genérico
                _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_SEARCH_PLAYER_IN_RANK)}] [Critical Error] {ex.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}