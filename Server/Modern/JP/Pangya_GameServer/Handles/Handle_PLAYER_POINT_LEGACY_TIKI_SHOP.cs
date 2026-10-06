using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_POINT_LEGACY_TIKI_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Verificação de Bloqueio
                if (Player.UserInfo.BlockFlag.Flag.LegacyTikiShop)
                {
                    throw new exception("[Handle_PLAYER_POINT_LEGACY_TIKI_SHOP][Error] Normal [UID=" + Player.UserInfo.UID + "] está bloqueado no Legacy Tiki Shop.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4000, 1));
                }

                // 2. Preparação do Pacote 0x1E8
                p.init_plain(0x1E8);

                // Status OK (0)
                p.WriteUInt32(0);

                // Quantidade de pontos da Legacy Tiki Shop (convertido para uint)
                p.WriteUInt32((uint)Player.UserInfo.PointShopLegacy);

                // 3. Envio da resposta
                Player.Send(p);
            }
            catch (exception e)
            {
                // Log de Erro no Sistema
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_POINT_LEGACY_TIKI_SHOP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E8);

                // Decodifica erro para o canal ou envia 1 como fallback
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 1;

                p.WriteUInt32(errorCode);

                Player.Send(p);
            }
        }
    }
}