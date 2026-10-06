using Pangya_GameServer.Manager;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_INFO_MAIL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Leitura do ID do e-mail
                int email_id = Packet.ReadInt32();

                // 2. Busca o e-mail no MailBox da sessão
                var email = Player.UserInfo.MailBox.getEmailInfo(email_id);

                if (email.id == 0)
                {
                    throw new exception("[Handle_PLAYER_INFO_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] pediu para ver o info do Mail[ID=" + (email_id) + "], mas ele nao existe no banco de dados. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x5500251));
                }

                // 3. Verifica itens atachados ao e-mail
                try
                {
                    ItemManager.CheckSetItemOnEmail(Player, email);
                }
                catch (exception e)
                {
                    // Se não for erro de 'item List vazio' (código 20), relança a exception
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE._ITEM_MANAGER, 20))
                    {
                        throw;
                    }
                }

                // 4. Envia pacote 212 com as informações do e-mail
                Player.Send(Handle_PACKET_RESPONSE.pacote212(email));

            }
            catch (exception e)
            {
                // Log original mantido
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_INFO_MAIL][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x212);

                // Lógica de tratamento de erro padrão
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x5500250;

                p.WriteUInt32(errorCode);

                Player.Send(p);
            }
        }
    }
}