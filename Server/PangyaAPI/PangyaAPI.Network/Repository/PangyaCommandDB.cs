using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PangyaAPI.Network.Repository
{
    public class PangyaCommandDB
    {

        public static (bool getLastCheck, int getServerUID) IsLogonCheck(uint uid, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd = new CmdLogonCheck((int)uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, _callback_response, _arg);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return (cmd.getLastCheck(), cmd.getServerUID());
        }


        public static void SaveNick(uint _uid, string wnick, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_sn = new CmdSaveNick(_uid, wnick);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_sn, _callback_response, _arg);

            if (cmd_sn.getException().getCodeError() != 0)
                throw cmd_sn.getException();
        }

        public static bool VerifyNick(string wnick, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            CmdVerifyNick cmd_vn = new CmdVerifyNick(wnick);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_vn, _callback_response, _arg);

            if (cmd_vn.getException().getCodeError() != 0)
                throw cmd_vn.getException();

            return cmd_vn.getLastCheck();
        }

        public static int VerifyID(string id, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_verifyId = new CmdVerifyID(id); // ID

            snmdb.NormalManagerDB.getInstance().add(0, cmd_verifyId, _callback_response, _arg);

            if (cmd_verifyId.getException().getCodeError() != 0)
                throw cmd_verifyId.getException();

            return cmd_verifyId.getUID();
        }

        public static bool VerifyPass(uint uid, string pass, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_verifyPass = new CmdVerifyPass(uid, pass); // PASSWORD

            snmdb.NormalManagerDB.getInstance().add(0, cmd_verifyPass, _callback_response, _arg);

            if (cmd_verifyPass.getException().getCodeError() != 0)
                throw cmd_verifyPass.getException();

            return cmd_verifyPass.getLastVerify();
        }

        public static string GetAuthKeyGame(uint uid, uint server_uid, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_auth_key_game = new CmdAuthKeyGame(uid, server_uid);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_auth_key_game, _callback_response, _arg);

            if (cmd_auth_key_game.getException().getCodeError() != 0)
                throw cmd_auth_key_game.getException();

            return cmd_auth_key_game.getAuthKey();
        }

        public static string GetAuthKeyLogin(uint uid, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_auth_key_login = new CmdAuthKeyLogin((int)uid);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_auth_key_login, _callback_response, _arg);

            if (cmd_auth_key_login.getException().getCodeError() != 0)
                throw cmd_auth_key_login.getException();

            return cmd_auth_key_login.getAuthKey();
        }

        public static List<ServerInfo> GetMsn(int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_server_list = new CmdServerList(TYPE_SERVER.MSN);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_server_list, _callback_response, _arg);

            if (cmd_server_list.getException().getCodeError() != 0)
                throw cmd_server_list.getException();

            return cmd_server_list.getServerList();
        }

        public static List<ServerInfo> GetGame(int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_server_list = new CmdServerList(TYPE_SERVER.GAME);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_server_list, _callback_response, _arg);

            if (cmd_server_list.getException().getCodeError() != 0)
                throw cmd_server_list.getException();

            return cmd_server_list.getServerList();
        }

        public static chat_macro_user GetMacroUser(uint uid, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_macro_user = new CmdChatMacroUser(uid);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_macro_user, _callback_response, _arg);

            if (cmd_macro_user.getException().getCodeError() != 0)
                throw cmd_macro_user.getException();

            return cmd_macro_user.getMacroUser();
        }

        public static CharacterInfo AddCharacter(uint uid, CharacterInfo ci, byte value = 0, byte value2 = 1, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_ac = new CmdAddCharacter(uid, ci, value, value2);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_ac, _callback_response, _arg);

            if (cmd_ac.getException().getCodeError() != 0)
                throw cmd_ac.getException();

            // Info Character Add com o Id gerado no banco de dados
            return cmd_ac.getInfo();
        }

        public static void UpdateCharacterEquiped(uint uid, int id, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd_uce = new CmdUpdateCharacterEquiped(uid, id);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_uce, _callback_response, _arg);

            if (cmd_uce.getException().getCodeError() != 0)
                throw cmd_uce.getException();
        }

        public static void InsertBlockIP(string _ip, string mask = "255.255.255.255", int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd = new CmdInsertBlockIp(_ip, mask);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, _callback_response, _arg);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();
        }

        public static void InsertBlockMAC(string _mac_adress, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd = new CmdInsertBlockMac(_mac_adress);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, _callback_response, _arg);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();
        }

        public static void RegisterLogon(uint _uid, int _option, int _id = 0, Action<int, Pangya_DB, object> _callback_response = null, object _arg = null)
        {
            var cmd = new CmdRegisterLogon(_uid, _option);

            snmdb.NormalManagerDB.getInstance().add(_id, cmd, _callback_response, _arg);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();
        }



        public static bool GameServerExist(uint server_uid)
        {
            var servers = GetGame();
            return servers.Any(c => c.uid == server_uid);
        } 
    }
}
