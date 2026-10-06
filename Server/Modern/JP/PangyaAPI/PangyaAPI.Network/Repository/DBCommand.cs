using PangyaAPI.DataBase;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Service;
using PangyaAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PangyaAPI.Network.Repository
{
    public class DBCommand
    {

        public static void UpdatePlayerMacAddress(uint uid, string address)
        {
            var cmd = new CmdUpdatePlayerMacAdress(uid, address);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static (bool getLastCheck, int getServerUID) IsLogonCheck(uint uid)
        {
            var cmd = new CmdLogonCheck((int)uid);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return (cmd.getLastCheck(), cmd.getServerUID());
        }


        public static void SaveNick(uint _uid, string wnick)
        {
            var cmd = new CmdSaveNick(_uid, wnick);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static bool VerifyNick(string wnick)
        {
            CmdVerifyNick cmd = new CmdVerifyNick(wnick);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getLastCheck();
        }

        public static int VerifyID(string id)
        {
            var cmd = new CmdVerifyID(id); // ID
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getUID();
        }

        public static bool VerifyPass(uint uid, string pass)
        {
            var cmd = new CmdVerifyPass(uid, pass); // PASSWORD
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getLastVerify();
        }

        public static string GetAuthKeyGame(uint uid, uint server_uid)
        {
            var cmd = new CmdAuthKeyGame(uid, server_uid);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getAuthKey();
        }

        public static string GetAuthKeyLogin(uint uid)
        {
            var cmd = new CmdAuthKeyLogin((int)uid);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getAuthKey();
        }

        public static byte UpdateAuthKeyLogin(uint uid, byte valid = 1)
        {
            var cmd = new CmdUpdateAuthKeyLogin(uid, valid);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getValid();
        }

        public static List<ServerInfo> GetMsn(int _id = 0)
        {
            var cmd = new CmdServerList(TYPE_SERVER.MSN);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getServerList();
        }


        public static List<ServerInfo> GetRank()
        {
            var cmd = new CmdServerList(TYPE_SERVER.RANK);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getServerList();
        }

        public static List<ServerInfo> GetGame()
        {
            var cmd = new CmdServerList(TYPE_SERVER.GAME);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getServerList();
        }


        public static ServerInfo RegisterServer(ServerInfo server)
        {
            var cmd = new CmdRegisterServer(server);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getServerList();
        }

        public static ChatMacroUser GetMacroUser(uint uid)
        {
            var cmd = new CmdChatMacroUser(uid);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getMacroUser();
        }

        public static CharacterInfo AddCharacter(uint uid, CharacterInfo ci, byte value = 0, byte value2 = 1)
        {
            var cmd = new CmdAddCharacter(uid, ci, value, value2);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static void UpdateCharacterEquiped(uint uid, int id)
        {
            var cmd = new CmdUpdateCharacterEquiped(uid, id);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static void InsertBlockIP(string _ip, string mask = "255.255.255.255")
        {
            var cmd = new CmdInsertBlockIp(_ip, mask);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static void InsertBlockMAC(string _mac_adress)
        {
            var cmd = new CmdInsertBlockMac(_mac_adress);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static void RegisterLogon(uint _uid, int _option)
        {
            var cmd = new CmdRegisterLogon(_uid, _option);
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
        }

        public static List<IPBan> ListIPBan()
        {
            var cmd = new CmdListIpBan();
            snmdb.NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getListIPBan();
        }

        public static List<string> ListMacBan(int _id = 0)
        {
            var cmd = new CmdListMacBan();
            snmdb.NormalManagerDB.Instance.add(_id, cmd);
            ValidarErro(cmd);
            return cmd.getList();
        }

        public static bool GameServerExist(uint server_uid)
        {
            var servers = GetGame();
            return servers.Any(c => c.UID == server_uid);
        }



        // --- MÉTODO AUXILIAR PARA EVITAR REPETIÇÃO ---

        public static void ValidarErro(Pangya_DB cmd)
        {
            try
            {
                if (cmd.getException().getCodeError() != 0)
                    throw new exception(cmd.getException().getFullMessageError(),
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
