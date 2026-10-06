using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
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
    public class Handle_PLAYER_OPEN_MAIL_BOX : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Verificação de Bloqueio
                if (Player.UserInfo.BlockFlag.Flag.MailBox)
                {
                    throw new exception("[Handle_PLAYER_OPEN_MAIL_BOX][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Mail Box, mas ele nao pode. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 5, 0x790001));
                }

                // 2. Leitura da Página
                int pagina = Packet.ReadInt32();

                if (pagina <= 0)
                {
                    throw new exception("[Handle_PLAYER_OPEN_MAIL_BOX][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Mail Box[Pagina=" + (pagina) + "], mas a pagina é invalida.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x790002));
                }

                // 3. Busca de E-mails e Envio
                var mails = Player.UserInfo.MailBox.GetPage((uint)pagina);

                if (mails != null && mails.Any())
                {
                    // Pagina existe, envia ela
                    Player.Send(Handle_PACKET_RESPONSE.pacote211(mails, pagina, (int)Player.UserInfo.MailBox.getTotalPages()));
                }
                else
                {
                    // MailBox Vazio ou Página não encontrada
                    Player.Send(Handle_PACKET_RESPONSE.pacote211(new List<MailBox>(), pagina, 1));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_OPEN_MAIL_BOX][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x211);

                // Tratamento de erro padrão Pangya
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x5500200;

                p.WriteUInt32(errorCode);

                Player.Send(p);
            }
        }
    }
}