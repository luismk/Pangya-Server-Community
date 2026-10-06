using Pangya_GameServer.Channels;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Handles;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase.Helpers;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Service;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System.Diagnostics;

namespace Pangya_GameServer.Server
{
    public class GameService : AppServer<Player, PacketIDClient>
    {
        private readonly PlayerManager _playerManager;
        public int SameLoginDup { get; private set; }
        public DailyQuestInfo DailyQuestsInfo;
        protected List<Channel> Channels = new();
        public BroadcastManager SendTicker = new(30/*30 segundos para o TIcker*/);
        public BroadcastManager SendNotice = new(60/*60 segundos 1 minuto para o notice*/);
        public bool SaveRoomLog { get; private set; }
        public GameService() : base(new PlayerManager(500), new PacketDispatcher<Player, PacketIDClient>(), ServerType.GameServer)
        {
            // Fazemos o cast do sessionManager para o seu PlayerManager
            _playerManager = (PlayerManager)SessionsManager;

            LoadConfig();
            //inicia os registros dos packet 
            RegisterHandlers();
            //inicia os canais do pangya
            InitializeChannels();
            // Carrega IFF_STRUCT
            if (!sIff.Instance.isLoad())
                sIff.Instance.Init();

            //inicia os sistemas do pangya
            InitializeSystems();
        }

        private async void RegisterHandlers()
        {
            #region REGISTER PACKET HANDLES

            // --- LOGIN / BASE ---
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LOGIN, new Handle_PLAYER_LOGIN());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHATMSG, new Handle_PLAYER_CHAT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ENTER_CHANNEL, new Handle_PLAYER_ENTER_CHANNEL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_FINISH_GAME, new Handle_PLAYER_FINISH_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USERINFO_OFFLINE, new Handle_PLAYER_CHECK_NICK());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CREATE_ROOM, new Handle_PLAYER_MAKE_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_JOIN_ROOM, new Handle_PLAYER_JOIN_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ROOMINFO_CHANGED, new Handle_PLAYER_CHANGE_INFO_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SYNC_ITEM_MAIN_LOBBY, new Handle_PLAYER_SYNC_ITEM_MAIN_LOBBY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SYNC_ITEM_ROOM, new Handle_PLAYER_SYNC_ITEM_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SET_READY, new Handle_PLAYER_CHANGE_STATE_READY_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_START_GAME, new Handle_PLAYER_START_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_EXIT_ROOM, new Handle_PLAYER_EXIT_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHANGE_TEAM, new Handle_PLAYER_CHANGE_TEAM_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LOAD_OK, new Handle_PLAYER_FINISH_LOAD_HOLE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SHOT, new Handle_PLAYER_INIT_SHOT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CAMERA, new Handle_PLAYER_CHANGE_MIRA());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLICK, new Handle_PLAYER_CHANGE_STATE_BAR_SPACE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_POWER_SHOT, new Handle_PLAYER_ACTIVE_POWER_SHOT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUB, new Handle_PLAYER_CHANGE_CLUB());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USE_ITEM, new Handle_PLAYER_USE_ACTIVE_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_EMOTICON, new Handle_PLAYER_CHANGE_STATE_TYPEING());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DROP, new Handle_PLAYER_MOVE_BALL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_HOLE_INFO, new Handle_PLAYER_INIT_HOLE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SHOT_RESULT, new Handle_PLAYER_SYNC_SHOT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SHOT_ACK, new Handle_PLAYER_FINISH_SHOT());
            //----------------------------------------- SHOP --------------------------------------\\
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_BUY_ITEM, new Handle_PLAYER_BUY_ITEM_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GIFT_ITEM, new Handle_PLAYER_GIFT_ITEM_SHOP());
            //----------------------------------------- SHOP --------------------------------------\\
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SYNC_ITEM_MY_ROOM, new Handle_PLAYER_SYNC_ITEM_MY_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TIMECHECK, new Handle_PLAYER_START_TURN_TIME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_BANISH, new Handle_PLAYER_KICK_PLAYER_OF_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_INVITE, new Handle_PLAYER_INVITE()); 
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WHISPER, new Handle_PLAYER_PRIVATE_MESSAGE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DETAIL_ROOM_INFO, new Handle_PLAYER_SHOW_INFO_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USERINFO, new Handle_PLAYER_REQUEST_INFO());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_PAUSE, new Handle_PLAYER_UN_OR_PAUSE_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_HOLE_STAT, new Handle_PLAYER_FINISH_HOLE_DATA());//estatisticas ler.
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SLEEP, new Handle_PLAYER_CHANGE_STATE_AFKROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_REPORT_ERROR, new Handle_PLAYER_EXCEPTION_PLAYER_REQ_MESSAGE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TEESHOT_READY, new Handle_PLAYER_FINISH_CHAR_INTRO());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TEAM_HOLEIN_PANG, new Handle_PLAYER_TEAM_FINISH_HOLE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ANSWER_GOSTOP, new Handle_PLAYER_REPLY_CONTINUE_VERSUS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_END_STROKE_GAME, new Handle_PLAYER_LAST_PLAYER_FINISH_VERSUS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_REEMPLOY_CADDIE, new Handle_PLAYER_PAY_CADDIE_HOLY_DAY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_REPORT, new Handle_PLAYER_PLAYER_REPORT_CHAT_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MSN_PROTOCOL, new Handle_PLAYER_MSN_PROTOCOL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CASH, new Handle_PLAYER_COOKIE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_JOIN_ROOM_GALLERY, new Handle_PLAYER_JOIN_ROOM_GALLERY());
            _dispatcher.Register(PacketIDClient.PLAYER_GM_REQ_CHANGE_IDENTITY, new Handle_PLAYER_GM_CHANGE_IDENTITY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SHOT_COMMAND, new Handle_PLAYER_INIT_SHOT_ARROW_SEQ());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SERVER_LIST, new Handle_PLAYER_SERVER_LIST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RANKADDRESS, new Handle_PLAYER_CONNECT_RANKSERVER());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LOADING_INFO, new Handle_PLAYER_LOAD_GAME_PERCENT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_REPLAY_ONLINE, new Handle_PLAYER_ACTIVE_REPLAY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ENCHANT, new Handle_PLAYER_CLUB_SET_STATS_UPDATE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHAT_PENALITY, new Handle_PLAYER_CHANGE_STATE_CHAT_BLOCK());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TEAMCHAT, new Handle_PLAYER_CHAT_TEAM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ALLOW_WHISPER, new Handle_PLAYER_CHANGE_WHISPER_STATE());//nao lembro o que e 
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_NOTICE, new Handle_PLAYER_NOTICE_GM());//noticias do gm
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SERVER_TIME, new Handle_REQUEST_SERVER_TIME());//tempo para sicronizar entre o server, projectg(client)
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DESTROY_ROOM, new Handle_PLAYER_EXEC_CCG_DESTROY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_KICK, new Handle_PLAYER_KICK_FROM_ROOM());//kick diretamente, HoleMode GM
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SYNC_ACTIVITY, new Handle_PLAYER_SYNC_ACTION_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DELETE_ITEM, new Handle_PLAYER_DELETE_ACTIVE_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SPEED_RATE, new Handle_PLAYER_ACTIVE_BOOSTER());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ONELINE, new Handle_PLAYER_SEND_TICKER());//noticia do ticket
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ONELINE_QUERY, new Handle_PLAYER_TICKER_QUEUE_INFO());//noticia do ticket
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UPDATE_GAME_OPTIONI, new Handle_PLAYER_CHANGE_CHAT_MACRO());//atualiza chat macro
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHECK_CADDIE_WARNNING, new Handle_PLAYER_SET_NOTICE_BEGIN_CADDIE_HOLY_DAY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHANGE_MASCOT, new Handle_PLAYER_CHANGE_MASCOT_MESSAGE());
            //---------------------------- LOUNGER SHOP
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_OPEN_SHOP, new Handle_PLAYER_TRADE_OPEN_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_CLOSE_SHOP, new Handle_PLAYER_TRADE_CLOSE_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_EDIT_SHOP, new Handle_PLAYER_TRADE_OPEN_EDIT_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_ENTER_SHOP, new Handle_PLAYER_TRADE_ENTER_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_EXIT_SHOP, new Handle_PLAYER_TRADE_EXIT_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_EDIT_TITLE, new Handle_PLAYER_TRADE_EDIT_TITLE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_SHOW_VISITOR, new Handle_PLAYER_TRADE_SHOW_SHOP_VISITOR());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_INCOME, new Handle_PLAYER_TRADE_INCOME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_EDIT_ITEM, new Handle_PLAYER_TRADE_EDIT_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TRADE_BUY_ITEM, new Handle_PLAYER_TRADE_BUY_ITEM());
      //---------------------------------------------- end ----------------------------------------------------------
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ENTER_LOBBY, new Handle_PLAYER_ENTER_LOBBY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LEAVE_LOBBY, new Handle_PLAYER_EXIT_LOBBY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHANGE_LOBBY, new Handle_PLAYER_CHANGE_LOBBY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RESPONSE_GGCSAUTH, new Handle_DUMMY());//new Handles.Handle_PLAYER_CHECK_GAME_GUARD_AUTH_ANSWER());//esse eo auth da ntreev(nprotect)
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_BS_USABLE_TIMES, new Handle_PLAYER_INIT_SHOT_SENDED());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MESSENGER_SERVER_LIST, new Handle_PLAYER_CONNECT_MSN());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GM_COMMAND, new Handle_PLAYER_COMMAND_GM());//dividir em partes. ficou mais bonito e mais legivel.
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_PLAYTIME_UPDATE, new Handle_PLAYER_OPEN_PAPEL_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UPDATE_PCBANG_MASCOTMSG, new Handle_PLAYER_UPDATE_PCBANG_MASCOT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USER_MATCH_HISTORY, new Handle_PLAYER_USER_MATCH_HISTORY());//do vs
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_INTRUSION, new Handle_PLAYER_ENTER_GAME_AFTER_STARTED());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UPDATE_GACHA_TICKETS, new Handle_PLAYER_UPDATE_GACHA_COUPON());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UPDATE_INGAME_WEBPAGE, new Handle_PLAYER_ENTER_WEB_LINK_STATE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_PANG_INFO, new Handle_PLAYER_EXITED_FROM_WEB_GUILD());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USE_TIKI_REPORT, new Handle_PLAYER_USE_TICKET_REPORT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_OPEN_TIKI_REPORT, new Handle_PLAYER_OPEN_TICKET_REPORT_SCROLL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_COMPLETE_TUTORIAL_QUEST, new Handle_PLAYER_MAKE_TUTORIAL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_OPEN_LUCKY_POUCH, new Handle_PLAYER_OPEN_BOX_MY_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DIRECTJOIN_ROOM, new Handle_PLAYER_DIRECT_JOIN_ROOM());//convite...
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CREATE_REALMYRROM, new Handle_PLAYER_CREATE_REALMYRROM());//talvez s4
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RMR_OBJECTLOAD, new Handle_PLAYER_ENTER_MY_ROOM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UCC, new Handle_PLAYER_REQUEST_UCC());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHECK_INVITE, new Handle_PLAYER_CHECK_INVITE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USE_CARD, new Handle_PLAYER_USE_CARD_SPECIAL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_UPDATE_USER_PLACE, new Handle_PLAYER_UPDATE_USER_PLACE());//simples, so leitura
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SECURITY_KEY, new Handle_PLAYER_UCC_SECURITY_KEY());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_OPEN_CARDPACK, new Handle_PLAYER_OPEN_CARD_PACK());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CAN_APPROACH_READY, new Handle_PLAYER_FINISH_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_ACCESS, new Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PAGE, new Handle_PLAYER_DOLFINI_LOCKER_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PUSH, new Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_POP, new Handle_PLAYER_REMOVE_DOLFINI_LOCKER_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_SET_PASSWORD, new Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_CHANGE_PW, new Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_SET_LOCK, new Handle_PLAYER_CHANGE_DOLFINI_LOCKER_MODE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_STATE, new Handle_PLAYER_DOLFINI_LOCKER_STATE());//nao lembro se eu tenho
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PANG_INOUT, new Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PANG, new Handle_PLAYER_DOLFINI_LOCKER_PANG());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ITEM_BUFF, new Handle_PLAYER_USE_ITEM_BUFF());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WHISPEROFF_TASK, new Handle_PLAYER_NOTIFY_NOT_DISPLAY_PRIVATE_MESSAGE_NOW());//nao lembro se eu tenho
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CUTIN, new Handle_PLAYER_ACTIVE_CUTIN());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_EXTEND_RENTAL, new Handle_PLAYER_EXTEND_RENTAL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DELETE_RENTAL, new Handle_PLAYER_DELETE_RENTAL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STAT_IN_CHATROOM, new Handle_PLAYER_PLAYER_STATE_CHARACTER_LOUNGE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SUPPLY_PACK_OPEN, new Handle_PLAYER_COMET_REFILL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_USE_NEW_RANDOMBOX, new Handle_PLAYER_OPEN_BOX_MAIL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_HEARTBEAT, new Handle_PLAYER_HEARTBEAT());//PACKET F4
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WEB_AUTH_KEY, new Handle_PLAYER_WEB_AUTH_KEY());//isso e enviado talvez quando troca de srv
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ALL_UCC_FROM_ALL_PLAYER, new Handle_PLAYER_LOAD_UCC());//CARREGA TODOS AS UCC DO Normal
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHANGE_GAME_SERVER, new Handle_PLAYER_CHANGE_SERVER());//MUDA DE SERVER
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_POINT_SHOP_OPEN, new Handle_PLAYER_OPEN_LEGACY_TIKI_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_POINT_SHOP_POINT, new Handle_PLAYER_POINT_LEGACY_TIKI_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_POINT_SHOP_EXCHANGE_TP_BY_ITEM, new Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_POINT_SHOP_EXCHANGE_ITEM_BY_TP, new Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CAN_GRANDZODIAC_READY, new Handle_PLAYER_FINISH_GAME());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CAMERA_INITIAL_GRANDZODIAC, new Handle_PLAYER_REPLY_INITIAL_VALUE_GRAND_ZODIAC());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MARK_ON_COURSE, new Handle_PLAYER_MARKER_ON_COURSE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SHOT_END_LOCATION_DATA, new Handle_PLAYER_SHOT_END_DATA());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LEAVE_PRACTICE, new Handle_PLAYER_LEAVE_PRACTICE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LEAVE_CHIP_IN_PRACTICE, new Handle_PLAYER_LEAVE_CHIP_IN_PRACTICE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_START_FIRST_HOLE_GRANDZODIAC, new Handle_PLAYER_START_FIRST_HOLE_GRAND_ZODIAC());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WING_EFFECT, new Handle_PLAYER_ACTIVE_WING());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ENTER_SHOP, new Handle_PLAYER_ENTER_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHANGE_WIND_NEXT_HOLE_REPEAT, new Handle_PLAYER_CHANGE_WIND_NEXT_HOLE_REPEAT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MAILBOX_OPEN_MAILBOX, new Handle_PLAYER_OPEN_MAIL_BOX());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MAILBOX_OPEN_MAIL, new Handle_PLAYER_INFO_MAIL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MAILBOX_SEND_MAIL, new Handle_PLAYER_SEND_MAIL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MAILBOX_MOVE_ITEM_TO_MYROOM, new Handle_PLAYER_TAKE_ITEM_FROM_MAIL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MAILBOX_DELETE_MAIL, new Handle_PLAYER_DELETE_MAIL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_BONGDARISHOP_PLAY_NORMAL, new Handle_PLAYER_PLAY_PAPEL_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DAILYQUEST_OPEN, new Handle_PLAYER_DAILY_QUEST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DAILYQUEST_AGREE, new Handle_PLAYER_ACCEPT_DAILY_QUEST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DAILYQUEST_GET_REWARD, new Handle_PLAYER_TAKE_REWARD_DAILY_QUEST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_DAILYQUEST_FORFEIT, new Handle_PLAYER_LEAVE_DAILY_QUEST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_LOLO_CARD_COMPOSE, new Handle_PLAYER_LOLO_CARD_COMPOSE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_AUTO_COMMAND, new Handle_PLAYER_ACTIVE_AUTO_COMMAND());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ACHIEVEMENT_OPEN, new Handle_PLAYER_ACHIEVEMENT_OPEN());//CHAMA O ACHIEVEMENT DO Normal ou de outros...
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CADIE_MAGICBOX_EXCHANGE_ITEM, new Handle_PLAYER_CADIE_CAULDRON_EXCHANGE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_PAWS_EFFECT, new Handle_PLAYER_ACTIVE_PAWS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_EFFECT, new Handle_PLAYER_ACTIVE_RING());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_UP_LEVEL, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CONFIRM_UP_LEVEL, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL_CONFIRM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CANCEL_UP_LEVEL, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_LEVEL_CANCEL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_UP_RANK, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_RANK());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CONFIRM_UP_RANK_TRANSFORM, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_RANK_TRANSFORM_CONFIRM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CANCEL_UP_RANK_TRANSFORM, new Handle_PLAYER_CLUB_SET_WORK_SHOP_UP_RANK_TRANSFORM_CANCEL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_RECOVERY_POINT, new Handle_PLAYER_CLUB_SET_WORK_SHOP_RECOVERY_PTS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_TRANSFER_MASTERY_POINT, new Handle_PLAYER_CLUB_SET_WORK_SHOP_TRANSFER_MASTERY_PTS());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_RESET_CLUBSET, new Handle_PLAYER_CLUB_SET_RESET());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ATTENDENCE_REWARD_OPEN, new Handle_PLAYER_CHECK_ATTENDANCE_REWARD());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ATTENDENCE_REWARD_CHECK_DAY, new Handle_PLAYER_ATTENDANCE_REWARD_LOGIN_COUNT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_EARCUFF_EFFECT, new Handle_PLAYER_ACTIVE_EARCUFF());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_2013_OPEN, new Handle_PLAYER_OPEN_CLUB_WORK_SHOP_EVENT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UN_173, new Handle_PLAYER_CLUB_WORK_SHOP_EVENT_COUNT());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UNK_174, new Handle_DUMMY());// new Handle_PLAYER_WORKSHOP_EVENT_UNK_174());//nao tenho o packet-> nao sei oque é haha
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UNK_173, new Handle_DUMMY());// new Handle_PLAYER_WORKSHOP_EVENT_UNK_173());//nao tenho o packet-> nao sei oque é haha
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GRANDPRIX_ENTER_LOBBY, new Handle_PLAYER_ENTER_LOBBY_GRAND_PRIX());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GRANDPRIX_LEAVE_LOBBY, new Handle_PLAYER_EXIT_LOBBY_GRAND_PRIX());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GRANDPRIX_JOIN_ROOM, new Handle_PLAYER_JOIN_ROOM_GRAND_PRIX());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GRANDPRIX_LEAVE_ROOM, new Handle_PLAYER_EXIT_ROOM_GRAND_PRIX());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MEMORIALSHOP_PLAY, new Handle_PLAYER_PLAY_MEMORIAL());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_GLOVE_EFFECT, new Handle_PLAYER_ACTIVE_GLOVE());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_GROUND_EFFECT, new Handle_PLAYER_ACTIVE_RING_GROUND());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_SET_ASSIST, new Handle_PLAYER_SET_ASSIST());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ASSIST_GREEN, new Handle_PLAYER_ACTIVE_ASSIST_GREEN());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_MEMORIALSHOP_PLAY_BIG, new Handle_PLAYER_PLAY_BIG_PAPEL_SHOP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EXPAND_MASTERY, new Handle_PLAYER_CHARACTER_MASTERY_EXPAND());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_UPGRADE_STAT, new Handle_PLAYER_CHARACTER_STATS_UP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_DOWNGRADE_STAT, new Handle_PLAYER_CHARACTER_STATS_DOWN());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EQUIP_CARD, new Handle_PLAYER_CHARACTER_CARD_EQUIP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EQUIP_CARD_WITH_CLUB_PATCHER, new Handle_PLAYER_CHARACTER_CARD_EQUIP_WITH_PATCHER());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_CHARACTER_STATS_REMOVE_CARD, new Handle_PLAYER_CHARACTER_REMOVE_CARD());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_TIKISHOP_EXCHANGE_ITEM, new Handle_PLAYER_TIKI_SHOP_EXCHANGE_ITEM());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_ARIN_EVENT_2014_OPEN, new Handle_DUMMY());//new Handle_PLAYER_ARIN_EVENT_2014_OPEN());//nao tenho o packet, um cara, falou que ja conseguiu abrir esse evento.
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_PAWS_RAINBOW_EFFECT, new Handle_PLAYER_ACTIVE_RING_PAWS_RAINBOW_JP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_POWER_GAUGE_EFFECT, new Handle_PLAYER_ACTIVE_RING_POWER_GAGUE_JP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_MIRACLE_SIGN_EFFECT, new Handle_PLAYER_ACTIVE_RING_MIRACLE_SIGN_JP());
            _dispatcher.Register(PacketIDClient.PLAYER_REQ_RING_PAWS_RING_SET_EFFECT, new Handle_PLAYER_ACTIVE_RING_PAWS_RING_SET_JP());
            #endregion

            await Task.CompletedTask;
        }

        public void InitializeChannels()
        {
            try
            {
                using var m_reader_ini = ServerConfig.GetLoadConfigIni(ServerType);
                int num_channel = m_reader_ini.readInt("CHANNELINFO", "NUM_CHANNEL");

                for (sbyte i = 0; i < num_channel; ++i)
                {
                    ChannelInfo ci = new ChannelInfo();
                    try
                    {
                        ci.id = i;
                        ci.name = m_reader_ini.ReadString("CHANNEL" + (i + 1), "NAME");
                        ci.max_user = m_reader_ini.ReadInt16("CHANNEL" + (i + 1), "MAXUSER");
                        ci.min_level_allow = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "LOWLEVEL");
                        ci.max_level_allow = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "MAXLEVEL");
                        ci.type.ulFlag = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "FLAG");
                    }
                    catch (Exception e)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[GameService::InitializeChannels][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    Channels.Add(new Channel(ci, m_si.Property));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::InitializeChannels][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();
              
            if (s.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var process = Process.GetCurrentProcess();
                var memoryUsage = process.PrivateMemorySize64 / 1024 / 1024; // MB  
                _smp.LogManager.Instance.push(new AppMessage($"[GameService::CheckCommand][Debug] STATUS[USERS: {Sessions?.Count() ?? 0}, MEMORY: {memoryUsage}, UPTIME: {DateTime.Now - process.StartTime}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_files")
            {
                ReloadFiles();
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "Rate")
            {
                string sTipo = _command.Dequeue();
                int tipo = -1;

                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "Pang": tipo = 0; break;
                        case "Experience": tipo = 1; break;
                        case "club": tipo = 2; break;
                        case "Rain": tipo = 3; break;
                        case "Treasure": tipo = 4; break;
                        case "Scratchy": tipo = 5; break;
                        case "pprareitem": tipo = 6; break;
                        case "ppcookieitem": tipo = 7; break;
                        case "memorial": tipo = 8; break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown Command: \"Rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown Command: \"Rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 8)
                {
                    if (uint.TryParse(_command.Dequeue(), out uint qntd) && qntd > 0)
                    {
                        UpdateRateAndEvent(tipo, qntd);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown value, Command: \"Rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "event")
            {
                s = _command.Dequeue();
                uint qntd = 0;

                if (!string.IsNullOrEmpty(s))
                {
                    qntd = uint.Parse(_command.Dequeue());

                    switch (s)
                    {
                        case "grand_zodiac_event":
                            UpdateRateAndEvent(9, qntd);
                            break;
                        case "AngelEvent":
                            UpdateRateAndEvent(10, qntd);
                            break;
                        case "GrandPrixMode":
                            UpdateRateAndEvent(11, qntd);
                            break;
                        case "golden_time":
                            UpdateRateAndEvent(12, qntd);
                            break;
                        case "login_reward":
                            UpdateRateAndEvent(13, qntd);
                            break;
                        case "GMEventBot":
                            UpdateRateAndEvent(14, qntd);
                            break;
                        case "smart_calc":
                            UpdateRateAndEvent(15, qntd);
                            break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown Comamnd: \"Event {s}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_system")
            {
                string sTipo = _command.Dequeue();
                int tipo = -1;

                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "all": tipo = 0; break;
                        case "iff": tipo = 1; break;
                        case "card": tipo = 2; break;
                        case "comet_refill": tipo = 3; break;
                        case "PapelShop": tipo = 4; break;
                        case "box": tipo = 5; break;
                        case "MemorialShop": tipo = 6; break;
                        case "cube_coin": tipo = 7; break;
                        case "treasure_hunter": tipo = 8; break;
                        case "drop": tipo = 9; break;
                        case "attendance_reward": tipo = 10; break;
                        case "map_course": tipo = 11; break;
                        case "approach_mission": tipo = 12; break;
                        case "grand_zodiac_event": tipo = 13; break;
                        case "coin_cube_location": tipo = 14; break;
                        case "golden_time": tipo = 15; break;
                        case "login_reward": tipo = 16; break;
                        case "GMEventBot": tipo = 17; break;
                        case "smart_calc": tipo = 18; break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[GameService::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 18)
                {
                    ReloadGlobalSystem(tipo);
                }
                return true; 
            }
            else if (s == "notice")     // !@ Teste: Envia aviso para todos os canais
            {
                // Verifica se existe algum texto após o comando
                if (_command.Count == 0)
                { 
                    return false;
                }

                var p = new Packet();

                // Une o restante da fila em uma única string (caso a mensagem tenha espaços)
                string mensagemCompleta = string.Join(" ", _command.ToArray());

                p.init_plain(0x42); // 0x42 costuma ser o ID de Notice/Mensagem do Sistema
                p.WriteString(mensagemCompleta);
                Channels.SendBroadCast(p); 
                return true;
            }
            else if (s == "upt_coin_cube_location")		// !@ Teste
            {
                sCoinCubeLocationUpdateSystem.Instance.forceUpdate();
                return true;
            }
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            else if (s.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                var msg = _smp.LogManager.Instance;
                msg.push(new AppMessage("======= COMMAND LIST =======", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("status                      - Mostra uso de memória, usuários e uptime.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("clear / cls                 - Limpa o console.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_files                - Recarrega arquivos básicos do servidor.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("upt_coin_cube_location      - Força atualização do Coin Cube.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("notice [msg]                - Envia uma mensagem global para todos os canais.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("---------------------------------------------------", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("Rate [Type] [valor]         - Tipos: Pang, Experience, club, Rain, Treasure, Scratchy, pprareitem, ppcookieitem, memorial", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("event [Name] [valor]        - Nomes: grand_zodiac_event, AngelEvent, GrandPrixMode, golden_time, login_reward, GMEventBot, smart_calc", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_system [Type]        - Tipos: all, iff, card, PapelShop, box, MemorialShop, cube_coin, drop, etc.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("===================================================", type_msg.CL_ONLY_CONSOLE));
                return true;
            } 
            return false;
        }

        protected override void OnClientConnected(IAppSession session)
        {
            if (session is not Player player)
            {
                Console.WriteLine($"[Erro] A sessão conectada não é do Type Player! Tipo real: {session.GetType().Name}");
                return;
            }

            try
            {
                var packet = new Packet(0x3F);
                packet.WriteByte(1); // OPTION 1
                packet.WriteByte(1); // OPTION 1
                packet.WriteInt32(player._ParseKey);
                packet.WriteString(session.GetIP());
                player.Send(packet, true);
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientConnected][Sucess] Normal[IP: {player.GetIP()}, OID: {player.ConnectionID}", 0));
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
              $"[GameService.OnClientConnected][ErrorSt]: {ex.getFullMessageError()}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override async void OnClientDisconnected(IAppSession session)
        {
            if (session == null)
                throw new exception("[GameService::OnClientDisconnected][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)session;

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientDisconnected][Warning] Normal[ID: {p.UserInfo?.Login} UID: {p.UserInfo.UID}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            NormalManagerDB.Instance.add(5, new CmdRegisterLogon(p.UserInfo.UID, 1/*Logout*/), DBResponse, this);

            var _channel = p.GetChannel();

            try
            {
                if (_channel != null)//tem que deslogar/.
                    _channel.LeaveChannel(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::OnClientDisconnecteded][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        protected override bool CheckPacket(IAppSession session, Packet packet)
        {
            if (packet == null)//tem pacote que só tem o ID dele...
                return false;

            var Type = (PacketIDClient)packet.Type;
            var player = (Player)session;

            if (Type != PacketIDClient.PLAYER_REQ_HEARTBEAT)
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::CheckPacket][Debug] Normal[UID: " + (player.UserInfo.UID == 0 ? player._IpAddress : player.UserInfo.UID.ToString()) + ", PID: " + Type + "]", type_msg.CL_ONLY_CONSOLE));

            if (Type == PacketIDClient.PLAYER_REQ_LOGIN)
            {
                return true;
            }

            if (!session.Authorized)
            {
                return false;
            }

            switch (Type)
            {
                case PacketIDClient.PLAYER_REQ_NONE:
                    break;
                case PacketIDClient.PLAYER_REQ_TIMETOALIVE:
                    break;
                case PacketIDClient.PLAYER_REQ_CHATMSG:
                    break;
                case PacketIDClient.PLAYER_REQ_ENTER_CHANNEL:
                    break;
                case PacketIDClient.PLAYER_REQ_LEAVE_CHANNEL:
                    break;
                case PacketIDClient.PLAYER_REQ_FINISH_GAME:
                    break;
                case PacketIDClient.PLAYER_REQ_USERINFO_OFFLINE:
                    break;
                case PacketIDClient.PLAYER_REQ_CREATE_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_JOIN_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_ROOMINFO_CHANGED:
                    break;
                case PacketIDClient.PLAYER_REQ_SYNC_ITEM_MAIN_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_SYNC_ITEM_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_SET_READY:
                    break;
                case PacketIDClient.PLAYER_REQ_START_GAME:
                    break;
                case PacketIDClient.PLAYER_REQ_EXIT_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_TEAM:
                    break;
                case PacketIDClient.PLAYER_REQ_LOAD_OK:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOT:
                    break;
                case PacketIDClient.PLAYER_REQ_CAMERA:
                    break;
                case PacketIDClient.PLAYER_REQ_CLICK:
                    break;
                case PacketIDClient.PLAYER_REQ_POWER_SHOT:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUB:
                    break;
                case PacketIDClient.PLAYER_REQ_USE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_EMOTICON:
                    break;
                case PacketIDClient.PLAYER_REQ_DROP:
                    break;
                case PacketIDClient.PLAYER_REQ_HOLE_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOT_RESULT:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOT_ACK:
                    break;
                case PacketIDClient.PLAYER_REQ_BUY_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_SELL_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_GIFT_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_SYNC_ITEM_MY_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_FIRST_LOGIN:
                    break;
                case PacketIDClient.PLAYER_REQ_TIMECHECK:
                    break;
                case PacketIDClient.PLAYER_REQ_GIFT_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_MOVE_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_SKIP:
                    break;
                case PacketIDClient.PLAYER_REQ_BANISH:
                    break;
                case PacketIDClient.PLAYER_REQ_VOTE_FOR_BANISH:
                    break;
                case PacketIDClient.PLAYER_REQ_COMMIT_MASTER:
                    break;
                case PacketIDClient.PLAYER_REQ_INVITE:
                    break;
                case PacketIDClient.PLAYER_REQ_WHISPER:
                    break;
                case PacketIDClient.PLAYER_REQ_USERLIST:
                    break;
                case PacketIDClient.PLAYER_REQ_ROOMLIST:
                    break;
                case PacketIDClient.PLAYER_REQ_DETAIL_ROOM_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_FINISH_TUTORIAL:
                    break;
                case PacketIDClient.PLAYER_REQ_USERINFO:
                    break;
                case PacketIDClient.PLAYER_REQ_PAUSE:
                    break;
                case PacketIDClient.PLAYER_REQ_HOLE_STAT:
                    break;
                case PacketIDClient.PLAYER_REQ_SLEEP:
                    break;
                case PacketIDClient.PLAYER_REQ_REPORT_ERROR:
                    break;
                case PacketIDClient.PLAYER_REQ_TEESHOT_READY:
                    break;
                case PacketIDClient.PLAYER_REQ_TEAM_HOLEIN_PANG:
                    break;
                case PacketIDClient.PLAYER_REQ_ANSWER_GOSTOP:
                    break;
                case PacketIDClient.PLAYER_REQ_END_STROKE_GAME:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_NICK:
                    break;
                case PacketIDClient.PLAYER_REQ_REEMPLOY_CADDIE:
                    break;
                case PacketIDClient.PLAYER_REQ_REPORT:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_SCHOOL:
                    break;
                case PacketIDClient.PLAYER_REQ_MSN_PROTOCOL:
                    break;
                case PacketIDClient.PLAYER_REQ_CASH:
                    break;
                case PacketIDClient.PLAYER_REQ_JOIN_ROOM_GALLERY:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_TARGET:
                    break;
                case PacketIDClient.PLAYER_REQ_PLAYINFO:
                    break;
                case PacketIDClient.PLAYER_GM_REQ_CHANGE_IDENTITY:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOT_COMMAND:
                    break;
                case PacketIDClient.PLAYER_REQ_SERVER_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_TITLE_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_TITLE:
                    break;
                case PacketIDClient.PLAYER_REQ_SET_JJANG:
                    break;
                case PacketIDClient.PLAYER_REQ_RANKADDRESS:
                    break;
                case PacketIDClient.PLAYER_REQ_LOADING_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_REPLAY_OFFLINE:
                    break;
                case PacketIDClient.PLAYER_REQ_REPLAY_ONLINE:
                    break;
                case PacketIDClient.PLAYER_REQ_ENCHANT:
                    break;
                case PacketIDClient.PLAYER_REQ_BANISH_ALL:
                    break;
                case PacketIDClient.PLAYER_REQ_DRAWBACK_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_SET_SYSTEM:
                    break;
                case PacketIDClient.PLAYER_REQ_CHAT_PENALITY:
                    break;
                case PacketIDClient.PLAYER_REQ_FIND_USER:
                    break;
                case PacketIDClient.PLAYER_REQ_TITLE:
                    break;
                case PacketIDClient.PLAYER_REQ_MATCH_HOLEIN_PANG:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_EXP:
                    break;
                case PacketIDClient.PLAYER_REQ_TEAMCHAT:
                    break;
                case PacketIDClient.PLAYER_REQ_ALLOW_WHISPER:
                    break;
                case PacketIDClient.PLAYER_REQ_CHECK_PCBANG:
                    break;
                case PacketIDClient.PLAYER_REQ_NOTICE:
                    break;
                case PacketIDClient.PLAYER_REQ_RESTORE:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_FORTUNE:
                    break;
                case PacketIDClient.PLAYER_REQ_OFFLINE_GAME:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_SERVER_TIME:
                    break;
                case PacketIDClient.PLAYER_REQ_SPY_ENTER_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_SPY_PLAY_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MATCH_RECORD:
                    break;
                case PacketIDClient.PLAYER_REQ_DESTROY_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_KICK:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_SYNC_ACTIVITY:
                    break;
                case PacketIDClient.PLAYER_REQ_DELETE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_SPEED_RATE:
                    break;
                case PacketIDClient.PLAYER_REQ_ONELINE:
                    break;
                case PacketIDClient.PLAYER_REQ_ONELINE_QUERY:
                    break;
                case PacketIDClient.PLAYER_REQ_COMPOUND:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_GAME_OPTIONI:
                    break;
                case PacketIDClient.PLAYER_REQ_REFLECTED_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_CHECK_CADDIE_WARNNING:
                    break;
                case PacketIDClient.PLAYER_REQ_FLUSH_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_BONGDARISHOP_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_INSERT_COUPON:
                    break;
                case PacketIDClient.PLAYER_REQ_APPLY_EVENT:
                    break;
                case PacketIDClient.PLAYER_REQ_SCRATCH_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_SCRATCH_SERIAL_NUMBER:
                    break;
                case PacketIDClient.PLAYER_REQ_SCRATCH_CARD_NUMBER:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_MASCOT:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_OPEN_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_CLOSE_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_EDIT_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_ENTER_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_EXIT_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_EDIT_TITLE:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_SHOW_VISITOR:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_INCOME:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_EDIT_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_TRADE_BUY_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_RECYCLE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_GAME_END_EARLY:
                    break;
                case PacketIDClient.PLAYER_REQ_USERINFO_SEASON2:
                    break;
                case PacketIDClient.PLAYER_REQ_ENTER_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_LEAVE_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_BLOCK_CHAT:
                    break;
                case PacketIDClient.PLAYER_REQ_PROFILE:
                    break;
                case PacketIDClient.PLAYER_REQ_LOCATE_USER:
                    break;
                case PacketIDClient.PLAYER_REQ_SECURITYKEY_CHECK:
                    break;
                case PacketIDClient.PLAYER_REQ_RESPONSE_GGCSAUTH:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOWMETHEMONEY:
                    break;
                case PacketIDClient.PLAYER_REQ_BS_USABLE_TIMES:
                    break;
                case PacketIDClient.PLAYER_REQ_MESSENGER_SERVER_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_EVENT_POINT:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_EVENT_EXCHANGE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_EVENT_REMAINED_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_GM_COMMAND:
                    break;
                case PacketIDClient.PLAYER_REQ_CHECK_PARAN_KID:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_NICK_FREE:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_NIC_PARAN:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_NEWYEAR_MONEY:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_EVENT_GIFTBOX:
                    break;
                case PacketIDClient.PLAYER_REQ_GIFT_LIST_PAGE:
                    break;
                case PacketIDClient.PLAYER_REQ_GIFT_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_ALL_GIFT_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_PLAYTIME_UPDATE:
                    break;
                case PacketIDClient.PLAYER_REQ_ONLINE_MESSAGE_BLOCK:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_PCBANG_MASCOTMSG:
                    break;
                case PacketIDClient.PLAYER_REQ_LOG_ROOMINFO_BY_CLIENT:
                    break;
                case PacketIDClient.PLAYER_REQ_USER_MATCH_HISTORY:
                    break;
                case PacketIDClient.PLAYER_REQ_INTRUSION:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_GACHA_TICKETS:
                    break;
                case PacketIDClient.PLAYER_REQ_BUY_GACHA_TICKETS:
                    break;
                case PacketIDClient.PLAYER_REQ_REFRESH_ITEMLIST:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_INGAME_WEBPAGE:
                    break;
                case PacketIDClient.PLAYER_REQ_PANG_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_FORTUNE_NEW:
                    break;
                case PacketIDClient.PLAYER_REQ_PANGYA_QUIZ_LEVEL:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_WAITING_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_INVITE_MEMBER:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_ACCEPT_MEMBER:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_ACCEPT_INVITATION:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_INVITATION_LIST:
                    break;
                case PacketIDClient.PLAYER_REQ_USE_TIKI_REPORT:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_TIKI_REPORT:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_EXP_TIKI:
                    break;
                case PacketIDClient.PLAYER_REQ_WORLD_TOUR_EVENT_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_COMPLETE_TUTORIAL_QUEST:
                    break;
                case PacketIDClient.PLAYER_REQ_COMPLETE_QUEST_FLAG:
                    break;
                case PacketIDClient.PLAYER_REQ_SELECT_QUEST_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_NEWYEARPOPPER:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_LUCKY_POUCH:
                    break;
                case PacketIDClient.PLAYER_REQ_NOTICE_TO_SERVER:
                    break;
                case PacketIDClient.PLAYER_REQ_DIRECTJOIN_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_CREATE_REALMYRROM:
                    break;
                case PacketIDClient.PLAYER_REQ_RMR_OBJECTSAVE:
                    break;
                case PacketIDClient.PLAYER_REQ_RMR_OBJECTLOAD:
                    break;
                case PacketIDClient.PLAYER_REQ_RMR_AUTHORITY:
                    break;
                case PacketIDClient.PLAYER_REQ_UCC:
                    break;
                case PacketIDClient.PLAYER_REQ_CHECK_INVITE:
                    break;
                case PacketIDClient.PLAYER_REQ_CANCEL_INVITE:
                    break;
                case PacketIDClient.PLAYER_REQ_TGAUGE:
                    break;
                case PacketIDClient.PLAYER_REQ_USE_CARD:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_USER_INSTREST:
                    break;
                case PacketIDClient.PLAYER_REQ_MATCHING:
                    break;
                case PacketIDClient.PLAYER_REQ_MATCHING_CANCEL:
                    break;
                case PacketIDClient.PLAYER_REQ_UPDATE_USER_PLACE:
                    break;
                case PacketIDClient.PLAYER_REQ_GAME_GHOST:
                    break; 
                case PacketIDClient.PLAYER_REQ_PARTS_ATTACH_CARD:
                    break;
                case PacketIDClient.PLAYER_REQ_SECURITY_KEY:
                    break;
                case PacketIDClient.PLAYER_REQ_OPEN_CARDPACK:
                    break;
                case PacketIDClient.PLAYER_REQ_CAN_APPROACH_READY:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_ACCESS:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PAGE:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PUSH:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_POP:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_SET_PASSWORD:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_CHANGE_PW:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_SET_LOCK:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_STATE:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PANG_INOUT:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_PANG:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEMSTORAGE_EDIT:
                    break;
                case PacketIDClient.PLAYER_REQ_FURNITURE_ABILITY_USE:
                    break;
                case PacketIDClient.PLAYER_REQ_ITEM_BUFF:
                    break;
                case PacketIDClient.PLAYER_REQ_PRIVATE_TRADE:
                    break;
                case PacketIDClient.PLAYER_REQ_VALENTINE_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_VALENTIME_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_TIKI_MAGICBOX_VERSION:
                    break;
                case PacketIDClient.PLAYER_REQ_TIKI_MAGICBOX_OUTPUT:
                    break;
                case PacketIDClient.PLAYER_REQ_WHISPEROFF_TASK:
                    break;
                case PacketIDClient.PLAYER_REQ_TICKET:
                    break;
                case PacketIDClient.PLAYER_REQ_USE_CARD_REMOVE:
                    break;
                case PacketIDClient.PLAYER_REQ_MISSION_EVENT_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_MISSION_EVENT_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_BINGO_EVENT_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILIST:
                    break;
                case PacketIDClient.PLAYER_REQ_CUTIN:
                    break;
                case PacketIDClient.PLAYER_REQ_EXTEND_RENTAL:
                    break;
                case PacketIDClient.PLAYER_REQ_DELETE_RENTAL:
                    break;
                case PacketIDClient.PLAYER_REQ_UPGRADE_CADDIE:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STAT_IN_CHATROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_SUPPLY_PACK_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_KOOH_BIRTHDAY_BINGO:
                    break;
                case PacketIDClient.PLAYER_REQ_KOOH_BIRTHDAY_GIFT:
                    break;
                case PacketIDClient.PLAYER_REQ_USE_NEW_RANDOMBOX:
                    break;
                case PacketIDClient.PLAYER_REQ_HEARTBEAT:
                    break;
                case PacketIDClient.PLAYER_REQ_WEB_AUTH_KEY:
                    break;
                case PacketIDClient.PLAYER_REQ_ALL_UCC_FROM_ALL_PLAYER:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_CREATE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_CHECK_NAME:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_CHANGE_NAME:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_CHANGE_NOTICE_MSG:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_CHANGE_INFO_MSG:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_DELETE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_PAGE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_SEARCH:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_PAST_ACTIVITIES:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_JOIN:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_JOIN_CANCEL:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_AGREE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_PROMOTE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_CHANGE_MSG:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_INFO:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_LEAVE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_MEMBER_KICK:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_EMBLEM_CHANGE:
                    break;
                case PacketIDClient.PLAYER_REQ_GUILD_MNGR_EMBLEM_CHANGE_CONFIRM:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_GAME_SERVER:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_SHOP_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_SHOP_POINT:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_SHOP_EXCHANGE_TP_BY_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_POINT_SHOP_EXCHANGE_ITEM_BY_TP:
                    break;
                case PacketIDClient.PLAYER_REQ_SCRATCH_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_SCRATCH_PLAY:
                    break;
                case PacketIDClient.PLAYER_REQ_CAN_GRANDZODIAC_READY:
                    break;
                case PacketIDClient.PLAYER_REQ_CAMERA_INITIAL_GRANDZODIAC:
                    break;
                case PacketIDClient.PLAYER_REQ_MARK_ON_COURSE:
                    break;
                case PacketIDClient.PLAYER_REQ_SHOT_END_LOCATION_DATA:
                    break;
                case PacketIDClient.PLAYER_REQ_LEAVE_PRACTICE:
                    break;
                case PacketIDClient.PLAYER_REQ_LEAVE_CHIP_IN_PRACTICE:
                    break;
                case PacketIDClient.PLAYER_REQ_START_FIRST_HOLE_GRANDZODIAC:
                    break;
                case PacketIDClient.PLAYER_REQ_WING_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_ENTER_SHOP:
                    break;
                case PacketIDClient.PLAYER_REQ_CHANGE_WIND_NEXT_HOLE_REPEAT:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILBOX_OPEN_MAILBOX:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILBOX_OPEN_MAIL:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILBOX_SEND_MAIL:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILBOX_MOVE_ITEM_TO_MYROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_MAILBOX_DELETE_MAIL:
                    break;
                case PacketIDClient.PLAYER_REQ_BONGDARISHOP_PLAY_NORMAL:
                    break;
                case PacketIDClient.PLAYER_REQ_DAILYQUEST_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_DAILYQUEST_AGREE:
                    break;
                case PacketIDClient.PLAYER_REQ_DAILYQUEST_GET_REWARD:
                    break;
                case PacketIDClient.PLAYER_REQ_DAILYQUEST_FORFEIT:
                    break;
                case PacketIDClient.PLAYER_REQ_LOLO_CARD_COMPOSE:
                    break;
                case PacketIDClient.PLAYER_REQ_AUTO_COMMAND:
                    break;
                case PacketIDClient.PLAYER_REQ_ACHIEVEMENT_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_CADIE_MAGICBOX_EXCHANGE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_PAWS_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_UP_LEVEL:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CONFIRM_UP_LEVEL:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CANCEL_UP_LEVEL:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_UP_RANK:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CONFIRM_UP_RANK_TRANSFORM:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_CANCEL_UP_RANK_TRANSFORM:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_RECOVERY_POINT:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_TRANSFER_MASTERY_POINT:
                    break;
                case PacketIDClient.PLAYER_REQ_CLUBSETWORKSHOP_RESET_CLUBSET:
                    break;
                case PacketIDClient.PLAYER_REQ_ATTENDENCE_REWARD_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_ATTENDENCE_REWARD_CHECK_DAY:
                    break;
                case PacketIDClient.PLAYER_REQ_EARCUFF_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_2013_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UN_173:
                    break;
                case PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UNK_174:
                    break;
                case PacketIDClient.PLAYER_REQ_WORKSHOP_EVENT_UNK_173:
                    break;
                case PacketIDClient.PLAYER_REQ_GRANDPRIX_ENTER_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_GRANDPRIX_LEAVE_LOBBY:
                    break;
                case PacketIDClient.PLAYER_REQ_GRANDPRIX_JOIN_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_GRANDPRIX_LEAVE_ROOM:
                    break;
                case PacketIDClient.PLAYER_REQ_MEMORIALSHOP_PLAY:
                    break;
                case PacketIDClient.PLAYER_REQ_GLOVE_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_GROUND_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_SET_ASSIST:
                    break;
                case PacketIDClient.PLAYER_REQ_ASSIST_GREEN:
                    break;
                case PacketIDClient.PLAYER_REQ_MEMORIALSHOP_PLAY_BIG:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EXPAND_MASTERY:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_UPGRADE_STAT:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_DOWNGRADE_STAT:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EQUIP_CARD:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_EQUIP_CARD_WITH_CLUB_PATCHER:
                    break;
                case PacketIDClient.PLAYER_REQ_CHARACTER_STATS_REMOVE_CARD:
                    break;
                case PacketIDClient.PLAYER_REQ_TIKISHOP_EXCHANGE_ITEM:
                    break;
                case PacketIDClient.PLAYER_REQ_ARIN_EVENT_2014_OPEN:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_PAWS_RAINBOW_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_POWER_GAUGE_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_MIRACLE_SIGN_EFFECT:
                    break;
                case PacketIDClient.PLAYER_REQ_RING_PAWS_RING_SET_EFFECT:
                    break;
                default:
                    break;
            }

            return true;
        }

        protected override void OnHeartBeat()
        {
            try
            {
                // Server ainda n�o est� totalmente iniciado
                if (!IsRunning)
                    return;

                OnStart();

                // Check Invite Time Channels
                foreach (var el in Channels)
                    el.startInviteTime();

                // Begin Check System Singleton Static
                // Carrega IFF_STRUCT
                if (!sIff.Instance.isLoad())
                    sIff.Instance.Init();

                //// Map Dados Estáticos
                if (!MapSystem.Instance.isLoad())
                    MapSystem.Instance.load();

                // Carrega Card System
                if (!sCardSystem.Instance.isLoad())
                    sCardSystem.Instance.load();

                //// Carrega Comet Refill System
                if (!sCometRefillSystem.Instance.isLoad())
                    sCometRefillSystem.Instance.load();

                // Carrega Papel Shop System
                if (!sPapelShopSystem.Instance.isLoad())
                    sPapelShopSystem.Instance.load();

                //// Carrega Box System
                if (!sBoxSystem.Instance.isLoad())
                    sBoxSystem.Instance.load();

                //// Carrega Memorial System
                if (!sMemorialSystem.Instance.isLoad())
                    sMemorialSystem.Instance.load();

                //// Carrega Cube Coin System(SobreCarga)
                if (!sCubeCoinSystem.Instance.isLoad())
                    sCubeCoinSystem.Instance.load();

                //// Treasure Hunter System
                if (!sTreasureHunterSystem.Instance.isLoad())
                    sTreasureHunterSystem.Instance.load();

                //// Drop System
                if (!sDropSystem.Instance.isLoad())
                    sDropSystem.Instance.load();

                // Attendance Reward System
                if (!sAttendanceRewardSystem.Instance.isLoad())
                    sAttendanceRewardSystem.Instance.load();

                //// Approach Mission
                if (!sApproachMissionSystem.Instance.isLoad())
                    sApproachMissionSystem.Instance.load();

                //// Grand Zodiac Event
                if (!sGrandZodiacEvent.Instance.isLoad())
                    sGrandZodiacEvent.Instance.load();

                //// Coin Cube Location System
                if (!sCoinCubeLocationUpdateSystem.Instance.isLoad())
                    sCoinCubeLocationUpdateSystem.Instance.load();

                //// Golden Time System
                if (!sGoldenTimeSystem.Instance.isLoad())
                    sGoldenTimeSystem.Instance.load();

                //// Login Reward System
                if (!sLoginRewardSystem.Instance.isLoad())
                    sLoginRewardSystem.Instance.load();
                 
                //// check Grand Zodiac Event Time
                //if (m_si.Rate.GrandZodiacEventTime == 1 && sGrandZodiacEvent.Instance.CheckTimeToMakeRoom())
                //    makeGrandZodiacEventRoom();

                //////// check Bot GM Event Time
                //if (m_si.Rate.GMEventBot == 1 && sBotGMEvent.Instance.CheckTimeToMakeRoom())
                //    makeBotGMEventRoom();


                ////// check Golden Time Round Update
                //if (m_si.Rate.GoldenTimeEvent == 1 && sGoldenTimeSystem.Instance.CheckRound())
                //    makeListOfPlayersToGoldenTime();

                //// update Login Reward
                if (/*m_si.Rate.LoginRewardEvent == 1 && */sLoginRewardSystem.Instance.isLoad())
                    sLoginRewardSystem.Instance.UpdateLoginReward();

                //// Check Daily Quest
                if (DailyQuestManager.CheckCurrentQuest(DailyQuestsInfo))
                    DailyQuestManager.UpdateDailyQuest(ref DailyQuestsInfo);  // Atualiza daily quest

                //// Check Update Dia do Papel Shop System
                if (sPapelShopSystem.Instance.isLoad())
                    sPapelShopSystem.Instance.UpdateDay();

                if (!sShopGiftSystem.Instance.isLoad())
                    sShopGiftSystem.Instance.UpdateItemList();

                //if (!sWorldTourSystem.Instance.isLoad())
                //    sWorldTourSystem.Instance.CheckEventFinish();

                if (sTreasureHunterSystem.Instance.CheckUpdateTimePointCourse())
                {
                    Channels.SendBroadCast(Handle_PACKET_RESPONSE.pacote131());
                }
                // End Check Treasure Hunter

                // Check Notice (GM or Cube Win Rare)
                BroadcastManager.RetNoticeCtx rt = SendNotice.peek();

                var p = new Packet();

                if (rt.ret == BroadcastManager.RET_TYPE.OK)
                {
                    if (rt.nc.type == BroadcastManager.TYPE.GM_NOTICE)
                    {
                        // GM Notice 
                        p.init_plain(0x42);
                        p.WriteString(rt.nc.notice); 
                    }
                    else if (rt.nc.type == BroadcastManager.TYPE.CUBE_WIN_RARE)
                    {
                        // Cube Win Rare Notice 
                        p.init_plain(0x1D3);
                        p.WriteUInt32(1);             // Count 
                        p.WriteUInt32(rt.nc.option);
                        p.WriteString(rt.nc.notice);
                    }

                    // Broadcast to All Channels
                    Channels.SendBroadCast(p);
                }

                //// Check Ticker
                rt = SendTicker.peek();

                if (rt.ret == BroadcastManager.RET_TYPE.OK && rt.nc.type == BroadcastManager.TYPE.TICKER)
                {
                    // Ticker Msg 
                    p.init_plain(0xC9);
                    p.WriteString(rt.nc.nickname);
                    p.WriteString(rt.nc.notice);
                    // Broadcast to All Channels
                    Channels.SendBroadCast(p);
                }

                _playerManager.CheckPlayersItens();
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void OnStart()
        {
            Console.Title = $"Game Service - P: {m_si.CurrentUsers}, Auth: {(m_unit_connect != null && m_unit_connect.isLive()? "ON": "OFF")}";
        }

        public override async void LoadConfig()
        {
            base.LoadConfig();

            // Server Tipo
            m_si.Type = ServerType.GameServer;
            using (var m_reader_ini = ServerConfig.GetLoadConfigIni(ServerType))
            {
                try
                {

                    m_si.ServerIcon = m_reader_ini.ReadInt16("SERVERINFO", "ICONINDEX");
                    m_si.Rate.Experience = (short)m_reader_ini.readInt("SERVERINFO", "EXPRATE");
                    m_si.Rate.Scratchy = (short)m_reader_ini.readInt("SERVERINFO", "SCRATCHY_RATE");
                    m_si.Rate.Pang = (short)m_reader_ini.readInt("SERVERINFO", "PANGRATE");
                    m_si.Rate.ClubMastery = (short)m_reader_ini.readInt("SERVERINFO", "CLUBMASTERYRATE");
                    m_si.Rate.PapelShopRareItem = (short)m_reader_ini.readInt("SERVERINFO", "PAPEL_rate_RATE");
                    m_si.Rate.PapelShopCookieItem = (short)m_reader_ini.readInt("SERVERINFO", "PAPEL_COOKIE_ITEM_RATE");
                    m_si.Rate.Treasure = (short)m_reader_ini.readInt("SERVERINFO", "TREASURE_RATE");
                    m_si.Rate.MemorialShop = (short)m_reader_ini.readInt("SERVERINFO", "MEMORIAL_RATE");
                    m_si.Rate.Rain = (short)m_reader_ini.readInt("SERVERINFO", "CHUVA_RATE");
                    m_si.Rate.GrandZodiacEventTime = (short)(m_reader_ini.readInt("SERVERINFO", "GZ_EVENT") >= 1 ? 1 : 0);// Ativo por padrão
                    m_si.Rate.GrandPrixEvent = (short)(m_reader_ini.readInt("SERVERINFO", "GP_EVENT") >= 1 ? 1 : 0);// Ativo por padrão
                    m_si.Rate.GoldenTimeEvent = ((short)(m_reader_ini.readInt("SERVERINFO", "GOLDEN_TIME_EVENT") >= 1 ? 1 : 0));// Ativo por padrão
                    m_si.Rate.LoginRewardEvent = ((short)(m_reader_ini.readInt("SERVERINFO", "LOGIN_REWARD") >= 1 ? 1 : 0));// Ativo por padrão
                    m_si.Rate.GMEventBot = ((short)(m_reader_ini.readInt("SERVERINFO", "BOT_GM_EVENT") >= 1 ? 1 : 0));// Ativo por padrão
                    m_si.Rate.SmartCalculation = (/*m_reader_ini.readInt("SERVERINFO", "SMART_CALC") >= 1 ? true :*/ 0);// Atibo por padrão
                    m_si.Rate.AngelEvent = ((short)(m_reader_ini.readInt("SERVERINFO", "ANGEL_EVENT") >= 1 ? 1 : 0));// Atibo por padrão
                    SaveRoomLog = (m_reader_ini.readInt("LOG", "ACTIVE_ROOM_LOG") >= 1 ? true : false);// Atibo por padrão

                    m_si.Flag.Value = m_reader_ini.ReadUInt64("SERVERINFO", "FLAG");

                    SaveRoomLog = (m_reader_ini.readInt("LOG", "ACTIVE_ROOM_LOG") >= 1 ? true : false);// Atibo por padrão

                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameService::config_init][ErrorSystem] Config.FLAG" + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }


            // Recupera Valores de Rate do gs do banco de dados
            var cmd_rci = new CmdRateConfigInfo(m_si.UID);  // Waiter

            NormalManagerDB.Instance.add(0, cmd_rci, DBResponse, this);


            if (cmd_rci.getInfo() != null)
            {

                if (cmd_rci.getException().getCodeError() != 0)
                    _smp.LogManager.Instance.push(new AppMessage("[GameService::config_init][ErrorSystem] " + cmd_rci.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));


                SetNewAngelEvent(m_si.Rate.AngelEvent);
                SetNewRatePang(m_si.Rate.Pang);
                SetNewRateExp(m_si.Rate.Experience);
                SetNewRateClubMastery(m_si.Rate.ClubMastery);

                NormalManagerDB.Instance.add(8, new CmdUpdateRateConfigInfo(m_si.UID, m_si.Rate), DBResponse, this);
            }
            else
            {   // Conseguiu recuperar com sucesso os valores do gs

               SetNewAngelEvent(m_si.Rate.AngelEvent);
                SetNewRatePang(m_si.Rate.Pang);
                SetNewRateExp(m_si.Rate.Experience);
                SetNewRateClubMastery(m_si.Rate.ClubMastery);
            }
            m_si.AppRate = 100;    // Esse aqui nunca usei, deixei por que no DB do s4 tinha só cópiei
        }


        #region INIT/RELOAD SYSTEMS

        public void InitializeSystems()
        {
            // SINCRONAR por que se não alguem pode pegar lixo de memória se ele ainda nao estiver inicializado
            var cmd_dqi = new CmdDailyQuestInfo();

            NormalManagerDB.Instance.add(1, cmd_dqi, DBResponse, this);

            if (cmd_dqi.getException().getCodeError() != 0)
                throw new exception("[GameService::InitializeSystems][Error] nao conseguiu pegar o Daily Quest Info[Exption: "
                    + cmd_dqi.getException().getFullMessageError() + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 277, 0));


            //// Carrega Map Dados Estáticos
            if (!MapSystem.Instance.isLoad())
                MapSystem.Instance.load();

            //// Carrega Card System
            if (!sCardSystem.Instance.isLoad())
                sCardSystem.Instance.load();

            //// Carrega Comet Refill System
            if (!sCometRefillSystem.Instance.isLoad())
                sCometRefillSystem.Instance.load();

            // Carrega Papel Shop System
            if (!sPapelShopSystem.Instance.isLoad())
                sPapelShopSystem.Instance.load();

            //// Carrega Box System
            if (!sBoxSystem.Instance.isLoad())
                sBoxSystem.Instance.load();

            //// Carrega Memorial System
            if (!sMemorialSystem.Instance.isLoad())
                sMemorialSystem.Instance.load();

            //// Carrega Cube Coin System
            if (!sCubeCoinSystem.Instance.isLoad())
                sCubeCoinSystem.Instance.load();

            //// Carrega Treasure Hunter System
            if (!sTreasureHunterSystem.Instance.isLoad())
                sTreasureHunterSystem.Instance.load();

            //// Carrega Drop System
            if (!sDropSystem.Instance.isLoad())
                sDropSystem.Instance.load();

            // Carrega Attendance Reward System
            if (!sAttendanceRewardSystem.Instance.isLoad())
                sAttendanceRewardSystem.Instance.load();

            //// Carrega Approach Mission System
            if (!sApproachMissionSystem.Instance.isLoad())
                sApproachMissionSystem.Instance.load();

            //// Carrega Grand Zodiac Event System
            if (!sGrandZodiacEvent.Instance.isLoad())
                sGrandZodiacEvent.Instance.load();

            //// Carrega Coin Cube Location Update Syatem
            if (!sCoinCubeLocationUpdateSystem.Instance.isLoad())
                sCoinCubeLocationUpdateSystem.Instance.load();

            //// Carrega Golden Time System
            if (!sGoldenTimeSystem.Instance.isLoad())
                sGoldenTimeSystem.Instance.load();

            //// Carrega Login Reward System
            if (!sLoginRewardSystem.Instance.isLoad())
                sLoginRewardSystem.Instance.load();

            //// Carrega Bot GM Event
            if (!sBotGMEvent.Instance.isLoad())
                sBotGMEvent.Instance.load();

            // Carrega Shop Gift System
            if (!sShopGiftSystem.Instance.isLoad())
                sShopGiftSystem.Instance.load();

            // Carrega World Tour System
            if (!sWorldTourSystem.Instance.isLoad())
                sWorldTourSystem.Instance.load();

            // Coloca aqui para ele não dá erro na hora de destruir o Room Grand Prix static instance
            RoomGrandPrix.initFirstInstance();

            ////// Coloca aqui para ele não dá erro na hora de destruir o Room Grand Zodiac Event static instance
            //RoomGrandZodiacEvent.initFirstInstance;

            ////// Coloca aqui para ele não dá erro na hora de destruir o Room Bot GM Event static instance
            //RoomBotGMEvent.initFirstInstance;
        }

        public void ReloadSystems()
        {

            // Recarrega IFF_STRUCT
            sIff.Instance.reload();

            // Recarrega Card System
            sCardSystem.Instance.load();

            // Recarrega Comet Refill System
            sCometRefillSystem.Instance.load();

            // Recarrega Papel Shop System
            sPapelShopSystem.Instance.load();

            // Recarrega Box System
            sBoxSystem.Instance.load();

            // Recarrega Memorial System
            sMemorialSystem.Instance.load();

            // Recarrega Cube Coin System
            sCubeCoinSystem.Instance.load();

            // Recarrega Treasure Hunter System
            sTreasureHunterSystem.Instance.load();

            // Recarrega Drop System
            sDropSystem.Instance.load();

            // Recarrega Attendance Reward System
            sAttendanceRewardSystem.Instance.load();

            // Recarrega Map Dados Estáticos
            MapSystem.Instance.load();

            //// Recarrega Approach Mission System
            sApproachMissionSystem.Instance.load();

            //// Recarrega Grand Zodiac Event System
            sGrandZodiacEvent.Instance.load();

            // Recarrega Coin Cube Location Update Syatem
            sCoinCubeLocationUpdateSystem.Instance.load();

            // Recarrega Golden Time System
            sGoldenTimeSystem.Instance.load();

            // Recarrega Login Reward System
            sLoginRewardSystem.Instance.load();

            // Recarrega Bot GM Event
            sBotGMEvent.Instance.load();

            // Recarrega Shop Gift Event			   
            sShopGiftSystem.Instance.load();
        }

        private void ReloadFiles()
        {
            base.LoadConfig();
            LoadConfig();

            // Reload All Globals Systems
            ReloadSystems();

            _smp.LogManager.Instance.push(new AppMessage("[GameService::ReloadFiles][Log] Reload System now sucess!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // UPDATE ON GAME
            var p = new Packet(0xF9); 
            p.WriteBytes(m_si.ToArray());
            Channels.SendBroadCast(p);
        }

        #endregion  
      
        #region AUTH COMMAND SEND 

        public override void authCmdShutdown(int _time_sec)
        {
            //base.authCmdShutdown(_time_sec);
        }

        public override void authCmdBroadcastNotice(string _notice)
        {
            try
            {
                SendNotice.push_back(0, _notice, BroadcastManager.TYPE.GM_NOTICE);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdBroadcastNotice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdBroadcastTicker(string _nickname, string _msg)
        {
            try
            {
                SendTicker.push_back(0, _nickname, _msg, BroadcastManager.TYPE.TICKER);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdBroadcastTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdBroadcastCubeWinRare(string _msg, uint _option)
        {

            try
            {
                SendNotice.push_back(0, _msg, _option, BroadcastManager.TYPE.CUBE_WIN_RARE);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdBroadcastCubeWinRare][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force)
        {

            try
            {

                var s = FindPlayer(_player_uid);

                if (s != null)
                {
                    // Deconecta o Player
                    if (_force == 1) // Força o Disconect do player, sem verificar as regras do Game Server
                        OnClientDisconnected(s);
                    else
                    {
                        OnClientDisconnected(s);
                    }

                }
                else
                {

                    // Não encontrou o player no server, então desconecta no banco de dados
                    snmdb.NormalManagerDB.Instance.add(5, new CmdRegisterLogon(_player_uid, 1/*Logout*/), DBResponse, this);

                    // Log
                    //_smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdDisconnectPlayer][Warning] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                    //        + "] pediu para desconectar o Normal[UID=" + (_player_uid) + "], mas nao encontrou ele no server, entao desconecta ele no banco de dados.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // UPDATE ON Auth Server
                m_unit_connect.SendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            // Game Server não usa esse Comando
            return;
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {

            try
            {

                var s = FindPlayer(_player_uid);

                if (s == null || !s.Connected || s.UserInfo.MailBox.getTotalPages() <= 0)
                {
                    return;
                }
                if (_player_uid <= 0)
                {
                    return;
                }

                s.UserInfo.MailBox.addNewEmailArrived(_player_uid, _mail_id);

                var v_mi = s.UserInfo.MailBox.getAllUnreadEmail();

                if (v_mi.Count == 0)
                    throw new exception("[GameService::authCmdNewMailArrivedMailBox][Error] Auth Server Comando New Mail[ID=" + (_mail_id)
                            + "] Arrived no Mailbox do Normal[UID=" + (_player_uid) + "], mas nao tem nenhum email nao lido no Mailbox dele.",
                           ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 131, 0));

                // UPDATE ON GAME
                var p = new Packet(0x210);

                p.WriteUInt32(0);   // OK

                p.WriteInt32(v_mi.Count);   // Count

                foreach (var el in v_mi)
                    p.WriteBytes(el.ToArray());

                s.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdNewMailArrivedMailBox][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {

            try
            {

                UpdateRateAndEvent((int)_tipo, _qntd);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {

            try
            {
                ReloadGlobalSystem((int)_tipo); 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdReloadGlobalSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            try
            {

                var s = FindPlayer(_aspi.uid);

                if (s != null)
                {
                    _aspi.id = s.UserInfo.Login;
                    _aspi.ip = s.GetIP();
                    _aspi.option = 1;
                    if (m_unit_connect != null)
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[GameService] Player[UID={_aspi.uid}] confirmado no Server[UID={_req_server_uid}]", type_msg.CL_ONLY_CONSOLE));
                        m_unit_connect.SendInfoPlayerOnline((uint)m_si.UID, _aspi);
                    }
                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdConfirmSendInfoPlayerOnline][Warning] Normal[UID=" + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID=" + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        #endregion

        #region FIND PLAYER/CHANNEL/ROOM
        public Channel FindChannel(sbyte _channel)
        {
            if (_channel == -1)
                return null;

            for (var i = 0; i < Channels.Count; ++i)
                if (Channels[i].getId() == _channel)
                    return Channels[i];

            return null;
        }

        public Channel FindChannel(Channel? _channel)
        {
            if (_channel == null)
                return null;

            for (var i = 0; i < Channels.Count; ++i)
                if (Channels[i].getId() == _channel.getId())
                    return Channels[i];

            return null;
        }

        public Room FindRoom(short _room)
        {
            if (_room == -1)
                return null;

            foreach (var channel in Channels)
            {
                var foundRoom = channel.FindRoom(_room);
                if (foundRoom != null)
                    return foundRoom;
            }

            return null;
        }

        public Room FindRoom(Room _room)
        {
            // If the object is null or has an invalid ID, don't bother searching
            if (_room == null || _room.GetRoomId() == -1)
                return null;

            foreach (var channel in Channels)
            {
                var foundRoom = channel.FindRoom(_room);
                // If the Channel found it, foundRoom is already the reference we need
                if (foundRoom != null && foundRoom.GetRoomId() == _room.GetRoomId())
                    return foundRoom;
            }

            return null;
        }
         
        public Room? FindRoomGrandPrix(uint _room)
        {

            if (_room == 0)
                return null;

            foreach (var channel in Channels)
            {
                var foundRoom = channel.FindRoomGrandPrix(_room);
                if (foundRoom != null)
                    return foundRoom;
            }

            return null;
        }
        //aqui eu posso logar, ver dados e outras coisas...
        public Room? MakeRoom(Channel _channel_owner, GameRoomInfoModel _ri, Player _session, int _option = 0)
        {
            return _channel_owner.MakeRoom(_ri, _session);
        }
        //aqui eu posso logar, ver dados e outras coisas...
        public RoomGrandPrix? MakeRoomGrandPrix(Channel _channel_owner, GameRoomInfoModel _ri, Player _session, GrandPrixData _gp, int _option = 0)
        {
            return _channel_owner.MakeRoomGrandPrix(_ri, _session, _gp, _option);
        }

        public override List<Player> FindAllGM()
        {
            return _playerManager.FindAllGM();
        }

        public override Player FindSessionByOid(int oid)
        {
            return _playerManager.FindSessionByOid(oid);
        }

        public override Player FindSessionByUid(uint uid)
        {
            return _playerManager.FindSessionByUID(uid);
        }

        public override List<Player> FindAllSessionByUid(uint uid)
        {
            return _playerManager.FindAllSessionByUid(uid);
        }

        public override Player FindSessionByNickname(string nickname)
        {
            return _playerManager.FindSessionByNickname(nickname);
        }

        public Player FindPlayer(uint member_uid)
        {
            return _playerManager.FindPlayer(member_uid, false);
        }

        #endregion

        #region CREATE/DELETE TIME 

        public PangyaSyncTimer MakeTimer(uint milliseconds, Action job, bool autoRepeted = false)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTimer] job is invalid");

            var timer = m_timer_mgr.CreateTimer(milliseconds, job, autoRepeted);
            if (timer == null)
                throw new Exception("[GameService::MakeTimer] não conseguiu criar o timer");

            return timer;
        }

        public PangyaSyncTimer MakeTimer(uint milliseconds, Action job, List<long> tableInterval, PangyaSyncTimer.TIMER_TYPE tipo = PangyaSyncTimer.TIMER_TYPE.PERIODIC)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTimer] job is invalid");

            var timer = m_timer_mgr.CreateTimer(milliseconds, job, tableInterval, tipo);
            if (timer == null)
                throw new Exception("[GameService::MakeTimer] não conseguiu criar o timer");

            return timer;
        }

        public PangyaSyncTimer MakeTimer(uint milliseconds, List<long> tableInterval, Action job, PangyaSyncTimer.TIMER_TYPE tipo = PangyaSyncTimer.TIMER_TYPE.PERIODIC)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTimer] job is invalid");

            var timer = m_timer_mgr.CreateTimer(milliseconds, job, tableInterval, tipo);
            if (timer == null)
                throw new Exception("[GameService::MakeTimer] não conseguiu criar o timer");

            return timer;
        }

        public void DeleteTimer(PangyaSyncTimer timer)
        {
            if (timer == null)
            {
                throw new exception("[GameService::DeleteTimer][Error] Tentou deletar o timer, mas o argumento é null",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 52, 0));
            }

            m_timer_mgr.DeleteTimer(timer);
        }

        #endregion

        #region OLD GS

        // Update Daily Quest Info
        public void UpdateDailyQuest(DailyQuestInfo _dqi)
        {
            if (_dqi != null)
                DailyQuestsInfo = _dqi;
        }


        public bool canSameIDLogin()
        {
            return SameLoginDup == 1;
        }

        // Set Event Server
        private void SetNewAngelEvent(short _angel_event)
        {
            // Evento para reduzir o quit Rate, diminui 1 quit a cada jogo concluído
            m_si.EventFlag.ReduceQuitRate = _angel_event > 0;
            // Update Rate Pang
            m_si.Rate.AngelEvent = _angel_event; //precisa fazer isso, pois pode querer desativar
        }

        private void SetNewRatePang(short _pang)
        {
            // Update ServerFlag Event
            m_si.EventFlag.PangPlus = (_pang >= 200) ? true : false;

            // Update Rate Pang
            m_si.Rate.Pang = _pang;
        }

        private void SetNewRateExp(short _exp)
        {// Reseta ServerFlag antes de atualizar ela 
            m_si.EventFlag.ExperienceDouble = m_si.EventFlag.ExperiencePlus = false;

            // Update ServerFlag Event
            if (_exp > 200)
                m_si.EventFlag.ExperiencePlus = true;
            else if (_exp == 200)
                m_si.EventFlag.ExperienceDouble = true;
            else
                m_si.EventFlag.ExperienceDouble = m_si.EventFlag.ExperiencePlus = false;

            // Update Rate Experiência
            m_si.Rate.Experience = _exp;
        }

        private void SetNewRateClubMastery(short _club_mastery)
        {
            // Update ServerFlag Event
            m_si.EventFlag.ClubMasteryPlus = (_club_mastery >= 200) ? true : false;

            // Update Rate Club Mastery
            m_si.Rate.ClubMastery = _club_mastery;
        }


        public void ReloadGlobalSystem(int _tipo)
        {
            try
            {
                switch (_tipo)
                {
                    case 0:     // Reload All Globals Systems
                        ReloadSystems();
                        break; 
                    case 1:     // IFF 
                        sIff.Instance.reload();
                        break; 
                    case 2:     // Card
                        sCardSystem.Instance.load();
                        break; 
                    case 3:     // Comet Refill
                        sCometRefillSystem.Instance.load();
                        break; 
                    case 4:     // Papel Shop
                        sPapelShopSystem.Instance.load();
                        break; 
                    case 5:     // Box
                        sBoxSystem.Instance.load();
                        break;
                    case 6:     // Memorial Shop
                        sMemorialSystem.Instance.load();
                        break;
                    case 7:     // Cube e Coin
                        sCubeCoinSystem.Instance.load();
                        break; 
                    case 8:     // Treasure Hunter
                        sTreasureHunterSystem.Instance.load();
                        break;
                    case 9:     // Drop
                        sDropSystem.Instance.load();
                        break;
                    case 10:    // Attendance Reward
                        sAttendanceRewardSystem.Instance.load();
                        break; 
                    case 11:    // Map Course Dados
                        MapSystem.Instance.load();
                        break; 
                    case 12:    // Approach Mission
                        sApproachMissionSystem.Instance.load();
                        break; 
                    case 13:    // Grand Zodiac Event
                        sGrandZodiacEvent.Instance.load();
                        break;
                    case 14:    // Coin Cube Location Update System
                        sCoinCubeLocationUpdateSystem.Instance.load();
                        break;
                    case 15:    // Golden Time System
                        sGoldenTimeSystem.Instance.load();
                        break; 
                    case 16:    // Login Reward System
                        sLoginRewardSystem.Instance.load();
                        break; 
                    case 17:    // Bot GM Event
                        sBotGMEvent.Instance.load();
                        break;  
                    default:
                        throw new Exception($"[GameService::ReloadGlobalSystem][Error] Tipo[VALUE={_tipo}] desconhecido.");
                }

                // Log
                _smp.LogManager.Instance.push(
                     new AppMessage($"[GameService::ReloadGlobalSystem][Log] Recarregou o Sistema[Tipo={_tipo}] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(
                     new AppMessage($"[GameService::ReloadGlobalSystem][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
        }


        // Update Rate e Event of Server

        public void UpdateRateAndEvent(int _tipo, uint _qntd)
        {
            try
            {

                if (_qntd == 0u && _tipo != 9/*Grand Zodiac Event Time*/ && _tipo != 10/*Angel Event*/
                    && _tipo != 11/*Grand Prix Event*/ && _tipo != 12/*Golden Time Event*/ && _tipo != 13/*Login Reward Event*/
                    && _tipo != 14/*Bot GM Event*/ && _tipo != 15/*Smart Calculator*/)
                    throw new exception("[GameService::UpdateRateAndEvent][Error] Rate[TIPO=" + (_tipo) + ", QNTD="
                            + (_qntd) + "], qntd is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));

                switch (_tipo)
                {
                    case 0: // Pang
                        SetNewRatePang((short)_qntd);
                        break;
                    case 1: // Exp
                        SetNewRateExp((short)_qntd);
                        break;
                    case 2: // Mastery
                        SetNewRateClubMastery((short)_qntd);
                        break;
                    case 3: // Chuva
                        m_si.Rate.Rain = (short)_qntd;
                        break;
                    case 4: // Treasure Hunter
                        m_si.Rate.Treasure = (short)_qntd;
                        break;
                    case 5: // Scratchy
                        m_si.Rate.Scratchy = (short)_qntd;
                        break;
                    case 6: // Papel Shop Rare Item
                        m_si.Rate.PapelShopRareItem = (short)_qntd;
                        break;
                    case 7: // Papel Shop Cookie Item
                        m_si.Rate.PapelShopCookieItem = (short)_qntd;
                        break;
                    case 8: // Memorial ShopRoom
                        m_si.Rate.MemorialShop = (short)_qntd;
                        break;
                    case 9: // Event Grand Zodiac Time Event [Active/Desactive]
                        {
                            m_si.Rate.GrandZodiacEventTime = (short)_qntd;

                            // Recarrega o Grand Zodiac Event se ele foi ativado
                            if (m_si.Rate.GrandZodiacEventTime == 1)
                                ReloadGlobalSystem(13/*Grand Zodiac Event*/);

                            break;
                        }
                    case 10: // Event Angel (Reduce 1 quit per game done)
                        SetNewAngelEvent((short)_qntd);
                        break;
                    case 11: // Grand Prix Event
                        m_si.Rate.GrandPrixEvent = (short)_qntd;
                        break;
                    case 12: // Golden Time Event
                        {
                            m_si.Rate.GoldenTimeEvent = (short)_qntd;

                            // Recarrega o Golden Time Event se ele foi ativado
                            if (m_si.Rate.GoldenTimeEvent == 1)
                                ReloadGlobalSystem(15/*Golden Time Event*/);

                            break;
                        }
                    case 13: // Login Reward System Event
                        {
                            m_si.Rate.LoginRewardEvent = (short)_qntd;

                            // Recarrega o Login Reward Event se ele foi ativado
                            if (m_si.Rate.LoginRewardEvent == 1)
                                ReloadGlobalSystem(16/*Login Reward Event*/);

                            break;
                        }
                    case 14: // Bot GM Event
                        {
                            m_si.Rate.GMEventBot = (short)_qntd;

                            // Recarrega o Bot GM Event se ele foi ativado
                            if (m_si.Rate.GMEventBot == 1)
                                ReloadGlobalSystem(17/*Bot GM Event*/);

                            break;
                        }
                    case 15: // Smart Calculator
                        {
                            m_si.Rate.SmartCalculation = (short)_qntd;

                            // Recarrega o Smart Calculator System se ele foi ativado
                            if (m_si.Rate.SmartCalculation == 1)
                                ReloadGlobalSystem(18/*Smart Calculator*/);

                            break;
                        }
                    default:
                        throw new exception("[GameService::UpdateRateAndEvent][Error] troca Rate[TIPO=" + (_tipo) + ", QNTD="
                                + (_qntd) + "], Type desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));
                }

                // Update no DB os server do server que foram alterados
                snmdb.NormalManagerDB.Instance.add(8, new CmdUpdateRateConfigInfo(m_si.UID, m_si.Rate), DBResponse, this);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[GameService::UpdateRateAndEvent][Error] New Rate[Tipo=" + (_tipo) + ", QNTD="
                        + (_qntd) + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                var p = new Packet(0xF9);

                p.WriteBytes(m_si.ToArray());
                Channels.SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::UpdateRateAndEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void DestroyRoom(Room r)
        {
            try
            {
                //procura o canal pelo Login dele
                var c = FindChannel(r.GetChannelId());

                if (c != null)//destroi a room
                {
                    c.Lobby.DestroyRoom(r);

                    c.Lobby.SendUpdateRoomInfo(r.GetInfo(), 2);
                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::destroyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void sendUpdateRoomInfo(Room _r, int _option)
        {
            try
            {

                if (_r != null)
                    _r.SendUpdateRoomInfo(_option);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameService::sendUpdateRoomInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void SendUpdateServerList(Player _session)
        {
            _session.Send(Handle_PACKET_RESPONSE.pacote09F(m_server_list, Channels));
        }

        public void SendChannelList(Player _session)
        {
            _session.Send(Handle_PACKET_RESPONSE.pacote04D(Channels));
        }

        public bool getActiveRoomLog()
        {
            return SaveRoomLog;
        }

        
        #endregion

        protected override void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameService::DBResponse][Error] _arg is null na msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
                throw new exception("[GameService::DBResponse][Error] " + _pangya_db.getException().getFullMessageError());

            var gs = (GameService)(_arg);

            switch (_msg_id)
            {
                case 1: // DailyQuest Info
                    {
                        var dqi_db = ((CmdDailyQuestInfo)_pangya_db).getInfo();  // cmd_dqi.getInfo();

                        // Atualiza daily quest
                        if (DailyQuestManager.CheckCurrentQuest(dqi_db))
                        {
                            DailyQuestManager.UpdateDailyQuest(ref dqi_db);

                            Thread.Sleep(100);  // Espera 100 milli segundo
                            NormalManagerDB.Instance.add(1, new CmdDailyQuestInfo(), DBResponse, _arg);
                        }
                        // Initialize Daily Quest of Server
                        DailyQuestsInfo = dqi_db;
                        break;
                    }
                case 2: // Atualiza DailyQuest Info do server
                    {
                        // Atualiza daily quest
                        DailyQuestsInfo = ((CmdDailyQuestInfo)_pangya_db).getInfo();
                        break;
                    }
                case 3: // Atualiza Chat Macro User
                    {
                        break;
                    }
                case 4: // Insert Msg Off
                    {
                        break;
                    }
                case 5: // Register Player Logon ON DB, 0 Login, 1 Logout
                    {
                        // Não usa por que é um UPDATE
                        break;
                    }
                case 6: // Insert Ticker no DB
                    {
                        break;
                    }
                case 7: // Register Logon do player no Server
                    {
                        // Não usa por que é um update
                        break;
                    }
                case 8: // Update Server Rate Config Info
                    {
                        break;
                    }
                case 9:     // Insert Block IP
                    {
                        break;
                    }
                case 10:    // Insert Block MAC
                    {
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        public Channel EnterChannel(Player session, sbyte channelId)
        {
            try
            {
                // 1. Busca o canal alvo
                var targetChannel = FindChannel(channelId);
                if (targetChannel == null)
                {
                    session.Send(Handle_PACKET_RESPONSE.pacote04E(3)); // Canal não existe
                    return null;
                }

                // 2. Verifica se já está no canal (Evita re-processamento)
                if (session.GetChannel()?.getId() == channelId)
                {
                    session.Send(Handle_PACKET_RESPONSE.pacote04E(1)); // Já está aqui
                    return targetChannel;
                }

                // 3. Validação de Lotação e Permissões (GM, Level, etc)
                if (targetChannel.IsFull())
                {
                    session.Send(Handle_PACKET_RESPONSE.pacote04E(2)); // Cheio
                    return null;
                }

                // 4. Check (ex: se é canal de iniciante)
                targetChannel.CheckEnterChannel(session);

                // --- INÍCIO DA TRANSIÇÃO ---

                // 5. Sai do canal anterior se houver um
                if (session.GetChannel() != null)
                {
                    session.GetChannel().LeaveChannel(session);
                }

                // 6. Entra no novo canal
                // O método AddPlayer do Channel deve ser Thread-Safe (usando lock ou ConcurrentDictionary)
                bool success = targetChannel.EnterChannel(session);

                if (success)
                {
                    // O Set final do estado
                    session.SetChannel(targetChannel); 
                    return targetChannel;
                }

                return null;
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Channel::Enter][Error] UID={session.UserInfo.UID}: {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                session.Send(Handle_PACKET_RESPONSE.pacote04E(3)); // Erro genérico
                return null;
            }
        }

        public void SendChannelBroadCast(Packet p)
        {
            Channels.SendBroadCast(p);
        }
    }

    public partial class GameServer : Singleton<GameService>
    {  
    }
}