using Pangya_LoginServer.DataBase;
using Pangya_LoginServer.Models;

using Pangya_LoginServer.Server;
using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Text.RegularExpressions;
namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_LOGIN : HandleBase<Player, Packet_EXAMPLE>
    {
        private static readonly Regex InvalidIdRegex = new(@".*[\^$&,\\?`´~\|""@#¨'%*!\\].*", RegexOptions.Compiled);

        public override async Task Handle()
        {
            try
            {  
                Player.ResetHandShake(); // Reseta o handshake para evitar problemas de sincronização
                var login = new LoginData(Packet);
                if (!ValidatePacket(login))
                    return;

                // 2. Validações de Fluxo (Early Return)
                if (!ValidateInput(Player, login.id, login.password))
                {
                    Player.SafeClose();
                    return;
                }

                Player.UserInfo.MacAddress = login.mac_address; 
                // 3. Verificação de Segurança (IP/Ban/Manutenção)
                if (!CheckServerStatus(Player))
                {
                    Player.SafeClose();
                    return;
                }

                // 4. Autenticação no Banco
                var uid = Authenticate(Player, login.id, login.password);
                if (uid == 0)
                {
                    Player.SafeClose();
                    return;
                }


                // 5. Verificação de Multi-Login (Kick ou Bloqueio)
                if (!HandleDuplicateLogin(Player, (uint)uid))
                {
                    Player.SafeClose();
                    return;
                }
                // 6. Carregamento de Dados
                await ProcessPlayerState(Player, (uint)uid);

            }
            catch (exception e)
            {

                LoginServer.Instance.Disconnect(Player);

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LOGIN][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }

        private bool ValidatePacket(LoginData login)
        {
            if (string.IsNullOrEmpty(login.id))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.id + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

            if (!Tools.Sanitize(login.id))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.id + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

            if (string.IsNullOrEmpty(login.mac_address))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.mac_address + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

            if (!Tools.Sanitize(login.mac_address))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.mac_address + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


            if (string.IsNullOrEmpty(login.password))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.password + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

            if (!Tools.Sanitize(login.password))
                throw new exception("PLAYER[UID=" + login.id + "] tentou contra o server[MESSAGE="
                        + login.password + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

            return true;
        }

        private bool ValidateInput(Player Player, string id, string pw)
        {
            if (string.IsNullOrEmpty(id) || id.Length <= 2 || InvalidIdRegex.IsMatch(id))
            {
                // Erro de ID Inválido (Packet 0x01, erro 6)
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0x6));
                return false;
            }
            return true;
        }

        private bool CheckServerStatus(Player Player)
        {
            // Aqui você move a lógica de m_access_flag e IsUnderMaintenance
            if (LoginServer.Instance.IsUnderMaintenance && !Player.IsGM())
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0x01, 7));
                return false;
            }

            if (LoginServer.Instance.haveBanList(Player.GetIP(), Player.UserInfo.MacAddress))
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 16)); 
                _smp.LogManager.Instance.push("[HANDLE_PLAYER_LOGIN::CheckServerStatus][Log] Block por Regiao o IP/MAC: " + Player.GetIP() + "/" + Player.UserInfo.MacAddress, type_msg.CL_FILE_LOG_AND_CONSOLE);
                  
                return false;
            }

            return true;
        }

        private int Authenticate(Player Player, string id, string pw)
        {
            var uid = CommandDB.VerifyID(id);
            if (uid <= 0)
            {
                // Lógica de auto-create ou erro de senha
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0x06, 1));
                return 0;
            }
            //so em modo release... para evitar problemas de teste com contas não confirmadas
#if RELEASE
 if (uid > 0 && !CommandDB.AccountConfirm(id))//verifica antes
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0x07, 0, "Confirm you accout in Email"));
                _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN::Authenticate][Log] PLAYER[ID: {id}, BETA ACCOUNT: FALSE]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return 0;
            }
#endif

            var pwd_md5 = Tools.MD5Hash(pw);
            // Valida senha (MD5/SHA1 conforme seu banco)
            if (!CommandDB.VerifyPass((uint)uid, pwd_md5))
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0x06, 1));
                return 0;
            }

            return uid;
        }

        private bool HandleDuplicateLogin(Player Player, uint uid)
        { 
            var manager = LoginServer.Instance.HasLoggedWithOuterSocket(Player);
            if (manager != null)
            {
                if (!LoginServer.Instance.canSameIDLogin())
                {
                    LoginServer.Instance.Disconnect(manager);
                    return true;
                }
                return false;
            }
            else
            {
                var lc = CommandDB.IsLogonCheck(uid);
                if (lc.getLastCheck)//login duplicado...
                {
                    Player.Authorized = true;
                    // Carrega o PlayerInfo (m_pi)
                    Player.UserInfo.Set(CommandDB.GetPlayerInfo(uid));
                    Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 4));
                    return true;//tem que ser true
                }
            }

            return true;
        }

        private async Task ProcessPlayerState(Player Player, uint uid)
        { 
            Player.Authorized = true;
            // Carrega o PlayerInfo (m_pi)
            Player.UserInfo.Set(CommandDB.GetPlayerInfo(uid));
            //atualiza o mac adress
            CommandDB.UpdatePlayerMacAddress(uid, Player.UserInfo.MacAddress);
            // Lógica de Estados que estava no LoginServer.cs
            if (!CommandDB.IsFirstLogin(uid))
            {
                // Movemos o FIRST_LOGIN para cá
                Player.UserInfo.m_state = 2;
                Player.Send(Handle_PACKET_RESPONSE.pacote00F(Player, 1));
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0xD8));//seta o nick
                return;
            }

            if (!CommandDB.IsFirstSet(uid))
            {
                // Movemos o FIRST_SET para cá
                Player.UserInfo.m_state = 3;
                Player.Send(Handle_PACKET_RESPONSE.pacote00F(Player, 1));
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0xD9));//cria o personagem
                return;
            }

            // Se chegou aqui, login com sucesso total 
            await SUCCESS_LOGIN(Player);
        }

        public static async Task SUCCESS_LOGIN(Player Player, byte option = 0)
        {
            Player.UserInfo.m_state = 1;

            _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_LOGIN][Log] PLAYER[UID: {Player.UserInfo.UID}, ID: {Player.UserInfo.Login}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Inicializamos as variáveis para evitar null reference
            List<ServerInfo> sis = new List<ServerInfo>();
            List<ServerInfo> msns = new List<ServerInfo>();
            ChatMacroUser _cmu = new ChatMacroUser();
            string auth_key_login = "";

            try
            {  
                sis = CommandDB.GetGame();
                msns = CommandDB.GetMsn();
                auth_key_login = CommandDB.GetAuthKeyLogin(Player.UserInfo.UID);

                if (option == 0)
                    _cmu = CommandDB.GetMacroUser(Player.UserInfo.UID);

                // Registro de Login (Pode ser await ou não, dependendo se você precisa confirmar o sucesso)
                CommandDB.RegisterPlayerLogin(Player.UserInfo.UID, Player.GetIP(), LoginServer.Instance.getUID());
            }
            catch (Exception e) // Use Exception padrão do sistema ou a sua customizada
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[Handle_PLAYER_LOGIN][Log][ErrorSystem] " + e.Message,
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Lógica de tratamento de erro do seu sistema de DB
                // (Mantenha sua lógica de filtros de erro aqui se necessário)
                return; // Interrompe o login se houver falha crítica
            }

            // --- ENVIO DE PACOTES (A ordem importa no Pangya) ---

            // 1. Envia Auth Key (Pacote 0x10)
            Player.Send(Handle_PACKET_RESPONSE.pacote010(auth_key_login));

            // 2. Cookie/Session Confirm (Pacote 0x01)
            if (option == 0)
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote001(Player));
            }

            // 3. Server List (Pacote 0x02)
            Player.Send(Handle_PACKET_RESPONSE.pacote002(sis));

            // 4. Messenger List (Pacote 0x09)
            Player.Send(Handle_PACKET_RESPONSE.pacote009(msns));

            // 5. Chat Macros (Pacote 0x06)
            if (option == 0)
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote006(_cmu));
            }
        }
    }
}
