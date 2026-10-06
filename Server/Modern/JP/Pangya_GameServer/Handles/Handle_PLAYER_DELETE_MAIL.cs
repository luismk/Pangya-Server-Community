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
    public class Handle_PLAYER_DELETE_MAIL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            uint[] a_email_id = null;

            try
            {
                // 1. Leitura dos dados do pacote
                int num_email = Packet.ReadInt32();
                uint pagina = 1;

                // Lê o array de IDs de e-mail baseado na quantidade enviada
                a_email_id = Packet.ReadUInt32(num_email);
                pagina = Packet.ReadUInt32();

                // 2. Validação da página
                if ((int)pagina <= 0)
                {
                    throw new exception("[Handle_PLAYER_DELETE_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] pediu para deletar email(s)[COUNT=" + num_email + "] da pagina(" + (int)pagina + "), mas a pagina é invalida.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x791002));
                }

                // 3. Atualização no Banco de Dados (Deleção)
                Player.UserInfo.MailBox.deleteEmail(a_email_id, (uint)num_email);

                // 4. Busca a lista atualizada para a página solicitada
                var mails = Player.UserInfo.MailBox.GetPage(pagina);

                // 5. Envio da resposta via pacote 215
                if (mails != null && mails.Any())
                {
                    // Página ainda contém e-mails após a exclusão
                    Player.Send(Handle_PACKET_RESPONSE.pacote215(mails, (int)pagina, (int)Player.UserInfo.MailBox.getTotalPages()));
                }
                else
                {
                    // MailBox vazio ou página ficou sem e-mails
                    Player.Send(Handle_PACKET_RESPONSE.pacote215(new List<MailBox>(), (int)pagina, 1));
                }
            }
            catch (exception e)
            {
                // Log de erro
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_DELETE_MAIL][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x215);

                // Tratamento de erro padrão Pangya
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x5500150;

                p.WriteUInt32(errorCode);

                Player.Send(p);
            }
            finally
            {
                // Limpeza do array temporário
                if (a_email_id != null)
                {
                    a_email_id = null;
                }
            }
        }
    }
}