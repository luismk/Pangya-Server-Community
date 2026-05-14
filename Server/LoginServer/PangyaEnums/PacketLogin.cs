using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_LoginServer.PangyaEnums
{
    public enum PacketIDClient
    {
        /// <summary>
        /// Player digita o usuário e senha e clica em login
        /// </summary>
        CLIENT_CONNECT = 0x01,
        /// <summary>
        /// Player Seleciona um Servidor para entrar
        /// </summary>
        CLIENT_SELECT_GS = 0x03,
        /// <summary>
        /// login com duplicidade 
        /// </summary>
        CLIENT_USER_MSG = 0x04,
        /// <summary>
        /// Seta primeiro nickname do usuário
        /// </summary>
        CLIENT_SET_NICK = 0x06,//SEQUENCIA[0] 
        /// <summary>
        /// Ocorre quando o cliente clica em Confirmar (se o nickname está disponível), 
        /// </summary>
        CLIENT_CONFIRM_SET_NICK = 0x07,//SEQUENCIA[1] 
        /// <summary>
        /// Player selecionou seu primeiro personagem
        /// </summary>
        CLIENT_SET_CHARACTER = 0x08,//SEQUENCIA[2] 
        /// <summary>
        /// envia chave de autenficação do login e lista novamente os servers
        /// </summary>
        CLIENT_RECONNECT = 0x0B,
        /// <summary>
        /// naosei
        /// </summary>
        CLIENT_NOTHING = 0xFF
    }

    public enum PacketIDServer
    {
        SERVER_CONNECT = 0x00,
        SERVER_LOGIN = 0x01,//server
        SERVER_GS_LIST = 0x02,//server
        SERVER_AUTH_KEY_GAME = 0x03,
        SERVER_EVENT_PRIZE = 0x05,//??
        SERVER_MACRO_GAME_OPTION = 0x06, // MACRO
        SERVER_MS_LIST = 0x09,
        SERVER_AGREEMENT = 0x0C, 
        SERVER_CHECK_NICK = 0x0E,
        SERVER_TUTORIAL = 0x0F,
        SERVER_AUTH_KEY_LOGIN = 0x10,
        SERVER_CHARACTER_SAVE = 0x11,
        SERVER_NOTHING = 0xFF
    } 
}
