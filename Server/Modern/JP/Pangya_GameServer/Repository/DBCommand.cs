using Pangya_GameServer.Handles;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Server;
using PangyaAPI.DataBase;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Pangya_GameServer.Repository
{
    public class CommandDB : DBCommand
    {
        public static TrophyInfo LoadTrophy(uint _uid, CmdTrofelInfo.TYPE_SEASON season = CmdTrofelInfo.TYPE_SEASON.ONE)
        {

            var cmd = new CmdTrofelInfo(_uid, season);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }


        public static List<TrophySpecialInfo> LoadTrophySpecial(uint _uid, CmdTrophySpecial.TYPE_SEASON season = CmdTrophySpecial.TYPE_SEASON.ONE, CmdTrophySpecial.TYPE tipo = CmdTrophySpecial.TYPE.NORMAL)
        {

            var cmd = new CmdTrophySpecial(_uid, season, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }



        public static DolfiniLocker LoadDolfineInfo(uint _uid)
        {

            var cmd = new CmdDolfiniLockerInfo(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static PremiumTicket LoadPremium(uint _uid)
        {

            var cmd = new CmdPremiumTicketInfo(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }


        public static MyRoomConfig LoadMyRoomConfig(uint _uid)
        {

            var cmd = new CmdMyRoomConfig(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getMyRoomConfig();
        }

        public static List<MyRoomItem> LoadMyRoomItem(uint _uid, CmdMyRoomItem.TYPE tipo = CmdMyRoomItem.TYPE.ONE)
        {

            var cmd = new CmdMyRoomItem(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getMyRoomItem();
        }

        public static List<ItemBuffEx> LoadItemBuff(uint _uid)
        {

            var cmd = new CmdItemBuffInfo(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.GetInfo();
        }

        public static CharacterManager LoadCharacter(uint _uid, CmdCharacterInfo.TYPE tipo = CmdCharacterInfo.TYPE.ALL)
        {

            var cmd = new CmdCharacterInfo(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getAllInfo();
        }


        public static CharacterInfo LoadCharacterOne(uint _uid, CmdCharacterInfo.TYPE tipo = CmdCharacterInfo.TYPE.ONE)
        {

            var cmd = new CmdCharacterInfo(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static ItemWarehouseManager LoadWarehouse(uint _uid, CmdWarehouseItem.TYPE tipo = CmdWarehouseItem.TYPE.ALL)
        {

            var cmd = new CmdWarehouseItem(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static CaddieManager LoadCaddie(uint _uid, CmdCaddieInfo.TYPE tipo = CmdCaddieInfo.TYPE.ALL)
        {

            var cmd = new CmdCaddieInfo(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }


        public static MascotManager LoadMascot(uint _uid, CmdMascotInfo.TYPE tipo = CmdMascotInfo.TYPE.ALL)
        {

            var cmd = new CmdMascotInfo(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }


        public static CardManager LoadCard(uint _uid, CmdCardInfo.TYPE tipo = CmdCardInfo.TYPE.ALL)
        {
            var cmd = new CmdCardInfo(_uid, tipo);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static CardEquipManager LoadCardEquip(uint _uid)
        {

            var cmd = new CmdCardEquipInfo(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static UserEquip LoadUserEquip(uint _uid)
        {

            var cmd = new CmdUserEquip(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getInfo();
        }

        public static CouponGacha LoadCouponGacha(uint _uid)
        {

            var cmd = new CmdCouponGacha(_uid);

            NormalManagerDB.Instance.add(0, cmd);

            if (cmd.getException().getCodeError() != 0)
                throw new exception(cmd.getException().getFullMessageError(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 277, 0));


            return cmd.getCouponGacha();
        }

        public static PlayerUserStatistics LoadUserInfo(uint _uid)
        {
            var cmd = new CmdUserInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static TutorialInfo LoadTutorial(uint _uid)
        {
            var cmd = new CmdTutorialInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static ulong LoadCookie(uint _uid)
        {
            var cmd = new CmdCookie(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getCookie();
        }

        public static GuildInfo LoadGuildInfo(uint _uid)
        {
            var cmd = new CmdGuildInfo(_uid, 0);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static ulong LoadGrandZodiacPoints(uint _uid)
        {
            var cmd = new CmdGrandZodiacPontos(_uid, CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_GET);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getPontos();
        }

        public static List<GrandPrixClear> LoadGrandPrixClear(uint _uid)
        {
            var cmd = new CmdGrandPrixClear(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static ulong LoadTikiShop(uint _uid)
        {
            var cmd = new CmdLegacyTikiShopInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        // --- 2. MÉTODOS DE ESTATÍSTICAS DE MAPA (Os 6 tipos do Switch Case) ---

        public static List<MapStatisticsEx> LoadMapStats(uint _uid, CmdMapStatistics.TYPE_SEASON season, CmdMapStatistics.TYPE type, CmdMapStatistics.TYPE_MODO mode)
        {
            var cmd = new CmdMapStatistics(_uid, season, type, mode);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getMapStatistics();
        }

        // --- 3. SISTEMA DE ACHIEVEMENTS (Casos 18 e 19) ---

        public static bool CheckAchievement(uint _uid)
        {
            var cmd = new CmdCheckAchievement(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getLastState(); // Retorna se já possui ou não
        }

        public static Dictionary<uint, List<AchievementInfoEx>> LoadAchievementInfo(uint _uid)
        {
            var cmd = new CmdAchievementInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.GetInfo();
        }


        public static Dictionary<int, EmailInfoEx> LoadMailBox(uint _uid)
        {
            var cmd = new CmdMailBoxInfo2(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static Dictionary<uint, FriendInfo> LoadFriends(uint _uid)
        {
            var cmd = new CmdFriendInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }


        public static long LoadWebPoints(uint _uid)
        {
            var cmd = new CmdWebShopPoint(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getPoints();
        }

        public static DailyQuestInfoUser LoadDailyQuest(uint _uid)
        {
            var cmd = new CmdDailyQuestInfoUser(_uid, CmdDailyQuestInfoUser.TYPE.GET);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.GetInfo();
        }

        public static bool LoadDailyQuestCheck(uint _uid)
        {
            var cmd = new CmdDailyQuestInfoUser(_uid, CmdDailyQuestInfoUser.TYPE.CHECK);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.Check();
        }

        public static List<MsgOffInfo> LoadMsgOff(uint _uid)
        {
            var cmd = new CmdMsgOffInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.GetInfo();
        }

        public static ChatMacroUser LoadChatMacro(uint _uid)
        {
            var cmd = new CmdChatMacroUser(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getMacroUser();
        }

        public static Last5PlayersGame LoadLastPlayerGame(uint _uid)
        {
            var cmd = new CmdLastPlayerGameInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static AttendanceRewardInfoEx LoadAttendanceReward(uint _uid)
        {
            var cmd = new CmdAttendanceRewardInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static PlayerMemberInfo LoadMemberInfo(uint _uid)
        {
            var cmd = new CmdMemberInfo(_uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static bool InsertFriendNote(uint uid, uint targetUid, string msg)
        {
            var cmd = new CmdInsertMsgOff(uid, targetUid, msg);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return true;
        }

        public static string GenerationSecurityKey(uint uid, int id)
        {
            var cmd = new CmdGeraUCCWebKey(uid, id);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getKey();
        }

        public static WarehouseItemEx FindUCC(int id)
        {
            var cmd = new CmdFindUCC(id);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static bool UpdateUCC(uint uid, WarehouseItemEx item, SystemTime time, CmdUpdateUCC.T_UPDATE mod = CmdUpdateUCC.T_UPDATE.FOREVER)
        {
            var cmd = new CmdUpdateUCC(uid, item, time, CmdUpdateUCC.T_UPDATE.FOREVER);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return true;
        }


        public static void UpdateMacroUser(uint uid, ChatMacroUser cmu)
        {
           snmdb.NormalManagerDB.Instance.add(0, new CmdUpdateChatMacroUser(uid, cmu));
        }

        public static void InsertTicker(uint uid, uint serverID, string msg)
        {
           snmdb.NormalManagerDB.Instance.add(0, new CmdInsertTicker(uid, serverID, msg)); 
        }

        public static string WEBKeyGeneration(uint uid)
        {
            // 2. Criação do Comando de Banco de Dados
            // O UID do jogador é enviado para a Procedure que gera a chave MD5/GUID
            var cmd = new CmdGeraWebKey(uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getKey();
        }

        public static bool LoadAssist(uint uid)
        { 
            var cmd = new CmdGreenAssist(uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getCheck(); 
        }

        public static ulong LoadLegacyTikiShopInfo(uint uid)
        { 
            var cmd = new CmdLegacyTikiShopInfo(uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getInfo();
        }

        public static long LoadPointEvent(uint uid)
        {  
            var cmd = new CmdWebShopPoint(uid);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getPoints();
        }

        public static bool LoadUpdateAssist(uint uid, bool assist)
        {
            var cmd = new CmdUpdateGreenAssist(uid, assist);
            NormalManagerDB.Instance.add(0, cmd);
            ValidarErro(cmd);
            return cmd.getCheck();
        }

    }
}
