using Pangya_LoginServer.Models;
using Pangya_LoginServer.Repository;
using Pangya_LoginServer.Session;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using System;

namespace Pangya_LoginServer.DataBase
{
    public class CommandDB : DBCommand
    {
        // ============================================================
        //  🔹 CREATE USER
        // ============================================================
        public static uint CreateUser(string id, string pass, string ip, uint serverUid)
        {
            var cmd = new CmdCreateUser(id, pass, ip, serverUid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getUID();    // Command já executa no construtor
        }

        // ============================================================
        //  🔹 FIRST LOGIN CHECK
        // ============================================================
        public static bool IsFirstLogin(uint uid)
        {
            var cmd = new CmdFirstLoginCheck(uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }

        public static void AddFirstLogin(uint uid, byte flag)
        {
            var cmd = new CmdAddFirstLogin(uid, flag);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();
        }

        // ============================================================
        //  🔹 FIRST SET CHECK
        // ============================================================
        public static bool IsFirstSet(uint uid)
        {
            var cmd = new CmdFirstSetCheck(uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }

        public static uint AddFirstSet(uint uid)
        {
            var cmd = new CmdAddFirstSet(uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

          return  cmd.getUID();
        }

        // ============================================================
        //  🔹 PLAYER INFO
        // ============================================================
        public static PlayerInfoBase GetPlayerInfo(uint uid)
        {
            var cmd = new CmdPlayerInfo(uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getInfo();
        }
         
        // ============================================================
        //  🔹 REGISTER / LOGIN SERVER
        // ============================================================
        public static uint RegisterLogonServer(uint uid, uint serverUid)
        {
            var cmd = new CmdRegisterLogonServer(uid, serverUid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getServerUID();
        }

        public static void RegisterPlayerLogin(uint _uid, string _ip, uint _server_uid)
        {
            var cmd = new CmdRegisterPlayerLogin(_uid, _ip, _server_uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

        }

        // ============================================================
        //  🔹 VERIFY IP
        // ============================================================
        public static bool VerifyIP(uint uid, string ip)
        {
            var cmd = new CmdVerifyIP(uid, ip);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getIP() == ip;
        }

        public static bool AccountConfirm(string uid)
        {
            var cmd = new CmdCheckConfirmAccount(uid);

            snmdb.NormalManagerDB.Instance.add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }

        public static string RegisterAndGetAuthKey(uint uid, uint server_uid)
        {
            CommandDB.RegisterLogonServer(uid, server_uid);
            return CommandDB.GetAuthKeyGame(uid, server_uid);
        }
    }
}
