using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_OPEN_LEGACY_TIKI_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Verificação de bloqueio (Block ServerFlag)
                // Verifica se o jogador possui alguma restrição específica para abrir esta loja
                if (Player.UserInfo.BlockFlag.Flag.LegacyTikiShop)
                {
                    throw new exception("[Handle_PLAYER_OPEN_LEGACY_TIKI_SHOP][Error] Normal [UID=" + Player.UserInfo.UID + "] está bloqueado no Legacy Tiki Shop.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4000, 1));
                }

                // 2. Preparação do pacote de resposta 0x1E7
                p.init_plain(0x1E7);

                // Sucesso: 0 (Permite abrir a loja no cliente)
                p.WriteUInt32(0);

                // 3. Envio da resposta
                Player.Send(p);

                // Log opcional de acesso
                _smp.LogManager.Instance.push(new AppMessage("[Legacy Tiki Shop::Open][Success] Normal [UID=" + Player.UserInfo.UID + "] abriu a loja com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                // Tratamento de Erro do Sistema ou Bloqueio
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_OPEN_LEGACY_TIKI_SHOP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E7);

                // Decodifica o código de erro para o cliente (Geralmente 1 para erro genérico nesta loja)
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 1;

                p.WriteUInt32(errorCode);

                Player.Send(p);
            }
        }
    }
}