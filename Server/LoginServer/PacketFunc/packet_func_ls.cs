using Pangya_LoginServer.DataBase;
using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using sls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace Pangya_LoginServer.PacketFunc
{
    public class packet_func_ls : packet_func_base
    {
        public static void SUCCESS_LOGIN(string from, object arg1, Player session)
        {
            session.m_pi.m_state = 1;

            _smp.message_pool.getInstance().push(new message($"[packet_func_ls::{from}][Log] PLAYER[ID: {session.m_pi.id}, UID: {session.m_pi.uid} Logged in]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            succes_login(arg1, session);
        }


        public static int packet001(object param, ParamDispatch pd)
        {
            try
            {
                ls.getInstance().requestLogin((Player)pd._session, pd._packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet001][Log][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != (uint)STDA_ERROR_TYPE.LOGIN_SERVER)
                    throw;
            }

            return 0;
        }

        public static int packet003(object param, ParamDispatch pd)
        {


            string auth_key_game = "";

            try
            {
                uint server_uid = pd._packet.ReadUInt32();

                if (server_uid <= 0)
                {
                    throw new exception("[packet_func_ls::packet003][Error] UID Server: " + (server_uid) + " is worng.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_LS, 21, 0));
                }

                var servers = CommandDB.GetGame();
                // Registra o logon no server_uid do player_uid
                if (servers.Any(c => c.uid == server_uid))
                {
                    CommandDB.RegisterLogonServer(((Player)pd._session).m_pi.uid, server_uid);

                    auth_key_game = CommandDB.GetAuthKeyGame(((Player)pd._session).m_pi.uid, server_uid);

                    session_send(pacote003(auth_key_game), ((Player)pd._session), 1);
                }
                else
                {
                    throw new exception("[packet_func_ls::packet003][Error] UID Server: " + (server_uid) + " no exist.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_LS, 21, 0));
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet003][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR(e.getCodeError(), (uint)STDA_ERROR_TYPE.EXEC_QUERY, 6/*AuthKeyLogin*/))
                    throw;
            }

            return 0;
        }

        public static int packet004(object param, ParamDispatch pd)
        {


            try
            {
                ls.getInstance().requestDownPlayerOnGameServer(((Player)pd._session));
            }
            catch (exception e)
            {
                session_send(pacote00E(((Player)pd._session), "", 12, 500053), ((Player)pd._session), 1);

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet004][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet006(object param, ParamDispatch pd)
        {
            string wnick = "";
            try
            {

                wnick = pd._packet.ReadString();


                CommandDB.SaveNick(((Player)pd._session).m_pi.uid, wnick);

                CommandDB.AddFirstLogin(((Player)pd._session).m_pi.uid, 1);

                // Aqui colocar para verificar se ele já fez o first set, se não envia o pacote do first set, se não success_login
                var result = CommandDB.IsFirstSet(((Player)pd._session).m_pi.uid);

                if (!result)
                {   // Verifica se fez o primeiro set do character

                    // FIRST_SET 
                    session_send(pacote001(((Player)pd._session), 0xD9), ((Player)pd._session), 1);
                }
                else
                    SUCCESS_LOGIN("packet006", param, ((Player)pd._session));

            }
            catch (exception e)
            {

                session_send(pacote00E(((Player)pd._session), wnick, 1/*UNKNOWN ERROR*/), ((Player)pd._session), 1);

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet006][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet007(object param, ParamDispatch pd)
        {
            NICK_CHECK nc = NICK_CHECK.SUCCESS;
            uint error_info = 0;
            string wnick = "";

            Player _session = (Player)pd._session;
            try
            {
                wnick = pd._packet.ReadString();

                if (wnick.Equals(_session.m_pi.id, StringComparison.Ordinal))
                {
                    nc = NICK_CHECK.SAME_NICK_USED;
                    _smp.message_pool.getInstance().push(new message($"[packet_func_ls::packet007][Error] O nick igual ao ID nao pode. Nick: {wnick} Player: {_session.m_pi.uid}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS && !(_session.m_pi.m_cap >= 4) && Regex.IsMatch(wnick, "(.*GM.*)|(.*ADM.*)", RegexOptions.IgnoreCase))
                {
                    nc = NICK_CHECK.HAVE_BAD_WORD;
                    _smp.message_pool.getInstance().push(new message($"[packet_func_ls::packet007][Error] O nick contem palavras inapropriadas: {wnick} Player: {_session.m_pi.uid}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS && wnick.Contains(" "))
                {
                    nc = NICK_CHECK.EMPETY_ERROR;
                    _smp.message_pool.getInstance().push(new message($"[packet_func_ls::packet007][Error] O nick contem espaco em branco: {wnick} Player: {_session.m_pi.uid}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS && (wnick.Length < 4 || Regex.IsMatch(wnick, @"[\^\$\?,`´~|""@#¨'%*!\\\]]")))
                {
                    nc = NICK_CHECK.INCORRECT_NICK;
                    _smp.message_pool.getInstance().push(new message($"[packet_func_ls::packet007][Error] O nick eh menor que 4 letras ou tem caracteres que nao pode: {wnick} Player: {_session.m_pi.uid}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS)
                {
                    var result = CommandDB.VerifyNick(wnick);

                    if (result)
                    {
                        nc = NICK_CHECK.NICK_IN_USE;
                        _smp.message_pool.getInstance().push(new message($"[packet_func_ls::packet007][Error] O nick ja esta em uso: {wnick} Player: {_session.m_pi.uid}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet007][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.PANGYA_DB)
                    nc = NICK_CHECK.ERROR_DB;
                else
                    nc = NICK_CHECK.UNKNOWN_ERROR;

            }


            session_send(pacote00E(_session, wnick, (int)nc, error_info), _session, 1);

            return 0;
        }


        public static int packet008(object param, ParamDispatch pd)
        {
            try
            {

                uint _typeid = pd._packet.ReadUInt32();
                var default_hair = pd._packet.ReadUInt8();
                var default_shirts = pd._packet.ReadUInt8();

                // Verifica se session está varrizada para executar esse ação, 
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                // CHECK_SESSION_IS_AUTHORIZED("packet008");

                if (sIff.getInstance().findCharacter(_typeid) == null)
                    throw new exception("[packet_func_ls::packet008][Error] typeid character: " + (_typeid) + " is worng.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_LS, 21, 0));

                if (default_hair > 9)
                    throw new exception("[packet_func_ls::packet008][Error] default_hair: " + (default_hair) + " is wrong. character: " + (_typeid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 22, 0));

                if (default_shirts != 0)
                    throw new exception("[packet_func_ls::packet008][Error] default_shirts: " + (default_shirts) + " is wrong. character: " + (_typeid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_LS, 23, 0));

                CharacterInfo ci = new CharacterInfo
                {
                    id = -1,
                    _typeid = _typeid,
                    default_hair = default_hair,
                    default_shirts = default_shirts
                };

                // Default Parts
                ci.initComboDef();

                CommandDB.AddFirstSet(((Player)pd._session).m_pi.uid);
                // Info Character Add com o Id gerado no banco de dados
                ci = CommandDB.AddCharacter(((Player)pd._session).m_pi.uid, ci, 0, 1);

                // Update Character Equipado no banco de dados
                CommandDB.UpdateCharacterEquiped(((Player)pd._session).m_pi.uid, ci.id);

                // Ok
                session_send(pacote011(), ((Player)pd._session));

                // Success Login
                SUCCESS_LOGIN("packet008", param, ((Player)pd._session));
            }
            catch (exception e)
            {
                // Erro na hora de salvar o character 
                session_send(pacote011(), ((Player)pd._session));

                session_send(pacote00E(((Player)pd._session), "", 12, 500051), ((Player)pd._session), 1);

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet008][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet00B(object param, ParamDispatch pd)
        {


            try
            {

                ls.getInstance().requestTryReLogin(((Player)pd._session), pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet00B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != (uint)STDA_ERROR_TYPE.LOGIN_SERVER)
                    throw;
            }

            return 0;
        }

        public static int packet_sv003(object param, ParamDispatch pd)
        {


            // Delete player "Desconnecta player"
            //pp.m_pw._session_pool.deleteSession(pp.m_session);
            // Parece que ele desconectar sozinho já
            //::shutdown(pp.m_session.m_client, SD_RECEIVE);

            return 0;
        }

        public static int packet_sv006(object param, ParamDispatch pd)
        {


            _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet_sv006][Log] Time: " + ((Environment.TickCount - ((Player)pd._session).m_time_start) / (double)10000), type_msg.CL_ONLY_FILE_TIME_LOG));

            _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet_sv006][Log] Send SUCCESS LOGIN Time: " + ((Environment.TickCount - ((Player)pd._session).m_tick_bot) / (double)10000), type_msg.CL_ONLY_FILE_TIME_LOG));

            return 0;
        }

        public static int packet_svFazNada(object param, ParamDispatch pd)
        {


            // Faz Nada

            return 0;
        }

        public static int packet_svDisconectPlayerBroadcast(object param, ParamDispatch pd)
        {
            return 0;
        }

        public static int packet_as001(object param, ParamDispatch pd)
        {
            try
            {

                // Log Teste
                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet_as001][Log] Teste, so para deixar aqui, quando for usar um dia.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::packet_as001][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static PangyaBinaryWriter pacote001(Player _session, byte option = 0, int sub_opt = 0, string message  = "")
        {
            var subID = (SubLoginCode)option;
            var p = new PangyaBinaryWriter();

            p.init_plain(0x001); 
            p.WriteByte(option);
            switch (option)
            {
                case 0: 
                    p.WriteString(_session.m_pi.id);
                    p.WriteUInt32(_session.m_pi.uid);
                    p.WriteUInt32(_session.m_pi.m_cap);
                    p.WriteByte(1);           // 1 level, 1 pc bang(ACHO), com base no S4
                    p.WriteInt32(0);// valor 0 Unknown
                    p.WriteByte(1);// nada
                    p.WriteInt32(5);// valor 5 Unknown, opcao 0 é pra enviar sem a chave, 
                    p.WriteTime();   // - JP S9 ler mais ignora ele
                    p.WriteString(_session.m_pi.acess_code);// Alguma AuthKey aleatória para minha conta que eu não sei - JP S9 ler mais ignora ele
                    p.WriteUInt64(0); // Unknown valor - JP S9 ler mais ignora ele
                    p.WriteString(_session.m_pi.nickname); 
                    break;
                case 6:
                case 1:
                    p.WriteInt32(0);  // add 4 bytes vazios
                    break;
                case 0xD8:
                    // First Login
                    p.WriteInt32(-1);
                    p.WriteInt16(0);
                    break;
                case 0xD9:
                    p.WriteInt16(0);
                    break;
                case 0x0c:
                case 0xE2:
                case 16:
                    p.WriteInt32(sub_opt);
                    break;
                case 7: 
                    var tempo = _session.m_pi.block_flag.m_id_state.block_time / 60 / 60/*Hora*/; // Hora
                    //24(Horas)x15(Dias)=360(horas)
                    p.WriteInt32(_session.m_pi.block_flag.m_id_state.block_time == -1 || tempo == 0 ? 360/*Menos de uma hora*/ : tempo);   // Block Por Tempo
                    if (!string.IsNullOrEmpty(message))
                    {
                        p.WriteString(message);
                    }
                    break;

                default:
                    break;
            }
            return p;
        }

        public static PangyaBinaryWriter pacote002(List<ServerInfo> v_element)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x002);

            p.WriteByte((byte)(v_element.Count & 0xFF)); // 1 Game Server online

            for (int i = 0; i < v_element.Count; i++)
                p.WriteBytes(v_element[i].ToArray());

            return p;
        }

        public static PangyaBinaryWriter pacote003(string AuthKeyLogin, int option = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x003);

            p.WriteInt32(option);

            p.WriteString(AuthKeyLogin);

            return p;
        }

        public static PangyaBinaryWriter pacote006(chat_macro_user _mu)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x006);

            p.WriteBytes(_mu.ToArray());

            return p;
        }

        public static PangyaBinaryWriter pacote009(List<ServerInfo> v_element)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x009);

            p.WriteByte((byte)(v_element.Count & 0xFF)); // nenhum Msn Server on

            for (int i = 0; i < v_element.Count; i++)
                p.WriteBytes(v_element[i].ToArray());

            return p;
        }


        public static PangyaBinaryWriter pacote00E(Player _session, string nick, int option = 0, uint error = 0)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x0E);

            p.WriteInt32(option);

            if (option == 0)
                p.WriteString(nick);
            else if (option == 12)
                p.WriteUInt32(error);

            return p;
        }

        // Mensagem do Tutorial
        public static PangyaBinaryWriter pacote00F(Player _session, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x0F); 
            p.WriteByte((sbyte)option); 
            p.WriteString(_session.m_pi.id); 
            p.WriteUInt32(0);                             // valor 0 Unknown
            p.WriteUInt32(5);                             // valor 5 Unknown
            p.WriteString(UtilTime.formatDateLocal(0));   // Time Build Login Server (ACHO)							- JP S9 ler mais ignora ele
            p.WriteString(_session.m_pi.acess_code);                      // Alguma AuthKey aleatória para minha conta que eu não sei - JP S9 ler mais ignora ele
            return p;
        }

        public static PangyaBinaryWriter pacote010(string AuthKey)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x10);

            p.WriteString(AuthKey);

            return p;
        }

        public static PangyaBinaryWriter pacote011(int option = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x11);

            p.WriteUInt16((ushort)option);

            return p;
        }

        public static void succes_login(object _arg, Player _session, int option = 0)
        {
            List<ServerInfo> sis = new List<ServerInfo>(), msns = new List<ServerInfo>();
            chat_macro_user _cmu = new chat_macro_user();
            string auth_key_login = "";

            /* OPTION
            *  0 PRIMEIRO LOGIN
            *  1 RELOGA DEPOIS QUE CAIU DO GAME SERVER, COM A AUTH KEY
            */

            try
            {
                //get game server
                sis = CommandDB.GetGame();
                //get messenger server
                msns = CommandDB.GetMsn();
                //get auth key login
                auth_key_login = CommandDB.GetAuthKeyLogin(_session.m_pi.uid);

                if (option == 0)
                    _cmu = CommandDB.GetMacroUser(_session.m_pi.uid);

                // RegisterLogin do Player
                CommandDB.RegisterPlayerLogin(_session.m_pi.uid, _session.getIP(), ls.getInstance().getUID());
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func_ls::succes_login][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.EXEC_QUERY)
                {
                    if (ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) != 7/*getServerList*/ && ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) != 9/*MacroUser*/
               && ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) != 8/*getMsnList*/ && ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) != 5/*AuthKey*/)
                        throw;
                }
                else
                    throw;
            }

            session_send(pacote010(auth_key_login), _session, 1);
            //gerarando a chave do cookies
            _session.m_pi.acess_code = CommandDB.GetWebKey(_session.m_pi.uid);

            if (option == 0)
            {
                session_send(pacote001(_session), _session, 1);
            }
            session_send(pacote002(sis), _session, 1);

            session_send(pacote009(msns), _session, 1);

            if (option == 0)
            {
                session_send(pacote006(_cmu), _session, 1);
            }
        }

        public static void session_send(PangyaBinaryWriter p, Player _session, int _debug = 1)
        {
            MAKE_SEND_BUFFER(p.GetBytes, _session);
        }
    }
}
