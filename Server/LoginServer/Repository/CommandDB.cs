using Pangya_LoginServer.Repository;
using Pangya_LoginServer.Models;                 
using PangyaAPI.Network.Repository;
using System;

namespace Pangya_LoginServer.DataBase
{
    public class CommandDB : PangyaCommandDB
    {
        // ============================================================
        //  🔹 CREATE USER
        // ============================================================
        public static uint CreateUser(string id, string pass, string ip, uint serverUid)
        {
            var cmd = new CmdCreateUser(id, pass, ip, serverUid);    

            snmdb.NormalManagerDB.getInstance().add(0, cmd, null, null);

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

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }

        public static void AddFirstLogin(uint uid, byte flag)
        {
            var cmd = new CmdAddFirstLogin(uid, flag);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();
        }

        // ============================================================
        //  🔹 FIRST SET CHECK
        // ============================================================
        public static bool IsFirstSet(uint uid)
        {
            var cmd = new CmdFirstSetCheck(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }                  

        public static void AddFirstSet(uint uid)
        {
            var cmd = new CmdAddFirstSet(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            cmd.getUID();
        }

        // ============================================================
        //  🔹 PLAYER INFO
        // ============================================================
        public static player_info GetPlayerInfo(uint uid)
        {
            var cmd = new CmdPlayerInfo(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getInfo();
        }

        public static void UpdatePlayerInfo(uint uid, PlayerInfo info)
        {
            var cmd = new CmdPlayerInfo(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            cmd.updateInfo(info);
        }

        // ============================================================
        //  🔹 REGISTER / LOGIN SERVER
        // ============================================================
        public static uint RegisterLogonServer(uint uid, uint serverUid)
        {
            var cmd = new CmdRegisterLogonServer(uid, serverUid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getServerUID();
        }

        public static uint RegisterPlayerLogin(uint _uid, string _ip, uint _server_uid)
        {
            var cmd = new CmdRegisterPlayerLogin(_uid, _ip, _server_uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getUID();
        }                  

        // ============================================================
        //  🔹 VERIFY IP
        // ============================================================
        public static bool VerifyIP(uint uid, string ip)
        {
            var cmd = new CmdVerifyIP(uid, ip);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);
                                          
            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getIP() == ip;
        }

        public static bool AccountConfirm(string uid)
        {
            var cmd = new CmdCheckConfirmAccount(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastCheck();
        }
		
		public static string GetWebKey(uint uid)
        {
            var cmd = new CmdGeraWebKey(uid);

            snmdb.NormalManagerDB.getInstance().add(_id: 0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getKey();
        }
    }
}
