using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Models
{
    public partial class PlayerInfo : player_info
    {

        public PlayerAssistent Assistent { get; set; } = new PlayerAssistent();
        public bool assist_flag { get => Assistent.assist_flag; set => Assistent.assist_flag = value; } //set assistent
        public ulong cookie { get; set; }
        public CouponGacha cg { get; set; }
        public MemberInfoEx mi { get; set; }
        public UserInfoEx ui { get; set; }
        public UserEquipedItem ei { get; set; }//with comet
        public ClubSetWorkshopLasUpLevel cwlul { get; set; }
        public ClubSetWorkshopTransformClubSet cwtc { get; set; }
        public PremiumTicket pt { get; set; }
        public TrofelInfo ti_current_season { get; set; }
        public TrofelInfo ti_rest_season { get; set; }
        public TutorialInfo TutoInfo { get; set; }
        public UserEquip ue { get; set; }
        public chat_macro_user cmu { get; set; }
        public List<MapStatisticsEx> a_ms_normal { get; set; } = new List<MapStatisticsEx>(22);
        public List<MapStatisticsEx> a_msa_normal { get; set; } = new List<MapStatisticsEx>(22);
        public List<MapStatisticsEx> a_ms_natural { get; set; } = new List<MapStatisticsEx>(22);
        public List<MapStatisticsEx> a_msa_natural { get; set; } = new List<MapStatisticsEx>(22);
        public List<MapStatisticsEx> a_ms_grand_prix { get; set; } = new List<MapStatisticsEx>(22);
        public List<MapStatisticsEx> a_msa_grand_prix { get; set; } = new List<MapStatisticsEx>(22);
        public MapStatistics[,] aa_ms_normal_todas_season { get; set; } = new MapStatistics[9, MS_NUM_MAPS];// Esse aqui é diferente, explico ele no pacote InitialLogin

        public Dictionary<int, StateCharacterLounge> mp_scl { get; set; }

        public CharacterManager mp_ce { get; set; }      //  
        public CaddieManager mp_ci { get; set; }
        public MascotManager mp_mi { get; set; }
        public WarehouseManager mp_wi { get; set; }

        public Dictionary<uint/*UID*/, FriendInfo> mp_fi { get; set; }    // Friend List

        public AttendanceRewardInfoEx ari { get; set; }

        public AchievementManager mgr_achievement { get; set; }             // Manager Achievement
        public CardManager v_card_info { get; set; }

        public CardEquipManager v_cei { get; set; }
        public List<ItemBuffEx> v_ib { get; set; }

        public Dictionary<stIdentifyKey/*uint/*ID*/, UpdateItem> mp_ui { get; set; }

        public List<TrofelEspecialInfo> v_tsi_current_season { get; set; }
        public List<TrofelEspecialInfo> v_tsi_rest_season { get; set; }
        public List<TrofelEspecialInfo> v_tgp_current_season { get; set; }   // Trofel Grand Prix
        public List<TrofelEspecialInfo> v_tgp_rest_season { get; set; } // Trofel Grand Prix
        public List<MyRoomItem> v_mri { get; set; }      // MyRoomItem

        public List<GrandPrixClear> v_gpc { get; set; }  // Grand Prix Clear os grand prix que o player já jogou

        public MyRoomConfig mrc { get; set; }
        public DolfiniLocker df { get; set; }   // DolfiniLocker
        public GuildInfoEx gi { get; set; }
        public DailyQuestInfoUser dqiu { get; set; }
        public Last5PlayersGame l5pg { get; set; }
        public stLocation location { get; set; }
        public long Web_Points { get; set; } = 0;

        public sbyte place { get; set; } = -1;            // Lugar que o player está no momento
        public byte lobby { get; set; } = DEFAULT_CHANNEL;            // Lobby
        public byte channel { get; set; } = DEFAULT_CHANNEL;          // Channel
        public byte whisper { get; set; } = 1; // Whisper 0 e 1, 0 OFF, 1 ON
        public uint state { get; set; }
        public uint state_lounge { get; set; }
        public byte[] animation { get; set; }// bytes 
        public uCapability m_cap { get; set; }   //chamar de outra forma
        public ulong grand_zodiac_pontos { get; set; }
        public ulong m_legacy_tiki_pts { get; set; } // Point Shop(Tiki Shop antigo)                                          
        //// Mail Box
        public PlayerMailBox m_mail_box { get; set; }
        public stPlayerLocationDB m_pl { get; set; }
        public stSyncUpdateDB m_update_pang_db { get; set; }
        public stSyncUpdateDB m_update_cookie_db { get; set; }
        public int ToTalClubsetCNT { get; internal set; }
        public int ToTalPartsCNT { get; internal set; }
        private Dictionary<uint/*key*/, stTitleMapCallback> mp_title_callback { get; set; }

        public stTitleMapCallback getTitleCallBack(int _id)
        {
            return mp_title_callback.FirstOrDefault(c => c.Key == _id).Value;
        }
        static int better_hit_pangya_bronze(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_hit_pangya_bronze);

            if (pi.ui.getPangyaShotRate() >= 70.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_hit_pangya_bronze);
        }

        static int better_fairway_bronze(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_fairway_bronze);

            if (pi.ui.getFairwayRate() >= 70.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_fairway_bronze);
        }

        static int better_putt_bronze(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_putt_bronze);

            if (pi.ui.getPuttRate() >= 80.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_putt_bronze);
        }

        static int better_quit_rate_bronze(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_quit_rate_bronze);

            if (pi.ui.getQuitRate() <= 3.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_quit_rate_bronze);
        }

        static int better_hit_pangya_silver(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_hit_pangya_silver);

            if (pi.ui.getPangyaShotRate() >= 77.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_hit_pangya_silver);
        }

        static int better_fairway_silver(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_fairway_silver);

            if (pi.ui.getFairwayRate() >= 72.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_fairway_silver);
        }

        static int better_putt_silver(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_putt_silver);

            if (pi.ui.getPuttRate() >= 90.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_putt_silver);
        }

        static int better_quit_rate_silver(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_quit_rate_silver);

            if (pi.ui.getQuitRate() <= 2.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_quit_rate_silver);
        }

        static int better_hit_pangya_gold(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_hit_pangya_gold);

            if (pi.ui.getPangyaShotRate() >= 85.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_hit_pangya_gold);
        }

        static int better_fairway_gold(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_fairway_gold);

            if (pi.ui.getFairwayRate() >= 90.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_fairway_gold);
        }

        static int better_putt_gold(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_putt_gold);

            if (pi.ui.getPuttRate() >= 95.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_putt_gold);
        }

        static int better_quit_rate_gold(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(better_quit_rate_gold);

            if (pi.ui.getQuitRate() <= 1.0)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(better_quit_rate_gold);
        }

        static int atirador_de_ouro(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(atirador_de_ouro);

            if (pi.ti_current_season.getSumGold() >= 10u)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(atirador_de_ouro);
        }

        static int atirador_de_silver(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(atirador_de_silver);

            if (pi.ti_current_season.getSumSilver() >= 10u)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(atirador_de_silver);
        }

        static int atirador_de_bronze(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(atirador_de_bronze);

            if (pi.ti_current_season.getSumBronze() >= 10u)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(atirador_de_bronze);
        }

        static int master_course(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(master_course);

            if (pi.isMasterCourse())
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(master_course);
        }

        static int natural_record_80(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_80);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -80)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_80);
        }

        static int natural_record_200(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_200);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -200)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_200);
        }

        static int natural_record_300(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_300);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -300)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_300);
        }

        static int natural_record_350(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_350);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -350)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_350);
        }

        static int natural_record_390(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_390);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -390)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_390);
        }

        static int natural_record_420(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_420);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -420)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_420);
        }

        static int natural_record_470(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_470);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -470)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_470);
        }

        static int natural_record_540(object _arg)
        {
            var pi = ((PlayerInfo)_arg);//BEGIN_CALL_BACK_TITLE_CONDITION(natural_record_540);

            // No JP ele pega o do Grand Prix
            if (pi.getSumRecordGrandPrix() <= -540)
                return 1; ; // passa na condição

            return 0; //END_CALL_BACK_TITLE_CONDITION(natural_record_540);
        }

        public PlayerInfo()
        {
            location = new stLocation();
            block_flag = new BlockFlag();
            m_pl = new stPlayerLocationDB();
            m_update_pang_db = new stSyncUpdateDB();
            m_update_cookie_db = new stSyncUpdateDB();
            mp_title_callback = new Dictionary<uint, stTitleMapCallback>();
            m_cap = new uCapability();
            cg = new CouponGacha();
            mi = new MemberInfoEx();
            ui = new UserInfoEx();
            ei = new UserEquipedItem();
            cwlul = new ClubSetWorkshopLasUpLevel();
            cwtc = new ClubSetWorkshopTransformClubSet();
            pt = new PremiumTicket();
            ti_current_season = new TrofelInfo();
            ti_rest_season = new TrofelInfo();
            TutoInfo = new TutorialInfo();
            ue = new UserEquip();
            cmu = new chat_macro_user();
            uid = 0;
            for (sbyte i = 0; i < MS_NUM_MAPS; i++)
            {
                var map = new MapStatisticsEx();
                map.clear(i);
                a_ms_normal.Add(map);
                a_msa_normal.Add(map);
                a_ms_natural.Add(map);
                a_msa_natural.Add(map);
                a_ms_grand_prix.Add(map);
                a_msa_grand_prix.Add(map);
            }

            // Inicializando cada sessão com 21 mapas (ou MS_NUM_MAPS mapas)
            for (int j = 0; j < 9; j++)
            {
                for (sbyte i = 0; i < MS_NUM_MAPS; i++)
                {
                    var map = new MapStatisticsEx();
                    map.clear(i);
                    aa_ms_normal_todas_season[j, i] = (map);  // Inicializa cada mapa
                }
            }

            mp_scl = new Dictionary<int, StateCharacterLounge>();

            mp_ce = new CharacterManager();
            mp_ci = new CaddieManager();
            mp_mi = new MascotManager();
            mp_wi = new WarehouseManager();

            mp_fi = new Dictionary<uint, FriendInfo>();   // Friend List

            ari = new AttendanceRewardInfoEx();

            mgr_achievement = new AchievementManager();   // Manager Achievement
            v_card_info = new CardManager();
            v_cei = new CardEquipManager();
            v_ib = new List<ItemBuffEx>();
            mp_ui = new Dictionary<stIdentifyKey/*uint/*ID*/, UpdateItem>();
            v_tsi_current_season = new List<TrofelEspecialInfo>();
            v_tsi_rest_season = new List<TrofelEspecialInfo>();
            v_tgp_current_season = new List<TrofelEspecialInfo>();   // Trofel Grand Prix
            v_tgp_rest_season = new List<TrofelEspecialInfo>(); // Trofel Grand Prix
            v_mri = new List<MyRoomItem>();     // MyRoomItem
            v_gpc = new List<GrandPrixClear>(); // Grand Prix Clear os grand prix que o player já jogou
            assist_flag = false;//set PCBangMascot assistent . ASSISTENT_TYPEID have in inventory
            mrc = new MyRoomConfig();
            df = new DolfiniLocker();   // DolfiniLocker
            gi = new GuildInfoEx();
            dqiu = new DailyQuestInfoUser();
            l5pg = new Last5PlayersGame();
            m_mail_box = new PlayerMailBox();
            mp_title_callback.Add(0x15, new stTitleMapCallback(better_hit_pangya_bronze, this));
            mp_title_callback.Add(0x16, new stTitleMapCallback(better_fairway_bronze, this));
            mp_title_callback.Add(0x17, new stTitleMapCallback(better_putt_bronze, this));
            mp_title_callback.Add(0x18, new stTitleMapCallback(master_course, this));
            mp_title_callback.Add(0x19, new stTitleMapCallback(atirador_de_ouro, this));
            mp_title_callback.Add(0x1a, new stTitleMapCallback(atirador_de_silver, this));
            mp_title_callback.Add(0x1b, new stTitleMapCallback(atirador_de_bronze, this));
            mp_title_callback.Add(0x1C, new stTitleMapCallback(better_quit_rate_bronze, this));
            mp_title_callback.Add(0x32, new stTitleMapCallback(better_hit_pangya_silver, this));
            mp_title_callback.Add(0x33, new stTitleMapCallback(better_fairway_silver, this));
            mp_title_callback.Add(0x34, new stTitleMapCallback(better_putt_silver, this));
            mp_title_callback.Add(0x35, new stTitleMapCallback(better_quit_rate_silver, this));
            mp_title_callback.Add(0x45, new stTitleMapCallback(natural_record_420, this));
            mp_title_callback.Add(0x46, new stTitleMapCallback(natural_record_390, this));
            mp_title_callback.Add(0x47, new stTitleMapCallback(natural_record_350, this));
            mp_title_callback.Add(0x48, new stTitleMapCallback(natural_record_300, this));
            mp_title_callback.Add(0x49, new stTitleMapCallback(natural_record_200, this));
            mp_title_callback.Add(0x4a, new stTitleMapCallback(natural_record_80, this));
            mp_title_callback.Add(0x7B, new stTitleMapCallback(better_quit_rate_gold, this));
            mp_title_callback.Add(0x7C, new stTitleMapCallback(better_putt_gold, this));
            mp_title_callback.Add(0x7D, new stTitleMapCallback(better_fairway_gold, this));
            mp_title_callback.Add(0x7E, new stTitleMapCallback(better_hit_pangya_gold, this));
            mp_title_callback.Add(0x17C, new stTitleMapCallback(natural_record_470, this));
            mp_title_callback.Add(0x17D, new stTitleMapCallback(natural_record_540, this));
            init(false);
        }
        private void init(bool _init = true)
        { 
            // --- 1. RESET DE TIPOS PRIMITIVOS E STATUS (Onde os hackers costumam deixar lixo) ---
            this.cookie = 0;
            this.Web_Points = 0;
            this.grand_zodiac_pontos = 0;
            this.m_legacy_tiki_pts = 0;
            this.ToTalClubsetCNT = 0;
            this.ToTalPartsCNT = 0;
            this.place = -1;
            this.lobby = DEFAULT_CHANNEL; 
            this.channel = DEFAULT_CHANNEL;
            this.whisper = 1;
            this.state = 0;
            this.state_lounge = 0;
            this.animation = new byte[12]; // Limpa o buffer de animação
            this.assist_flag = false;
        }
        public void clear()
        {
            init(false);

            // --- 3. ESTATÍSTICAS DE MAPAS (CORREÇÃO CRÍTICA: Limpar antes de dar Add) ---
            a_ms_normal.Clear();
            a_msa_normal.Clear();
            a_ms_natural.Clear();
            a_msa_natural.Clear();
            a_ms_grand_prix.Clear();
            a_msa_grand_prix.Clear();

            for (sbyte i = 0; i < MS_NUM_MAPS; i++)
            {
                var map = new MapStatisticsEx();
                map.clear(i);
                // Adicionando instâncias limpas
                a_ms_normal.Add(map);
                a_msa_normal.Add(map);
                a_ms_natural.Add(map);
                a_msa_natural.Add(map);
                a_ms_grand_prix.Add(map);
                a_msa_grand_prix.Add(map);
            }

            // Inicializando Array Multidimensional [Season, Mapa]
            aa_ms_normal_todas_season = new MapStatistics[9, MS_NUM_MAPS];
            for (int j = 0; j < 9; j++)
            {
                for (sbyte i = 0; i < MS_NUM_MAPS; i++)
                {
                    var m = new MapStatisticsEx();
                    m.clear(i);
                    aa_ms_normal_todas_season[j, i] = m;
                }
            }

            // --- 4. MANAGERS E DICIONÁRIOS (Proteção contra NullReference) ---
            mp_scl?.Clear();
            mp_ce?.Clear(); // Character Manager
            mp_ci?.Clear(); // Caddie Manager
            mp_mi?.Clear(); // Mascot Manager
            mp_wi?.Clear(); // Warehouse Manager
            mp_fi?.Clear(); // Friend List
            mp_ui?.Clear(); // Update Item list

            // Achievement e Cards
            mgr_achievement?.clear();
            v_card_info?.Clear();
            v_cei?.Clear();
            v_ib?.Clear();

            // Troféus e Listas de MyRoom
            v_tsi_current_season?.Clear();
            v_tsi_rest_season?.Clear();
            v_tgp_current_season?.Clear();
            v_tgp_rest_season?.Clear();
            v_mri?.Clear();
            v_gpc?.Clear();

            // MailBox
            m_mail_box?.clear();

            // --- 5. RECONSTRUÇÃO DO MAPA DE CALLBACKS (Title System) ---
            mp_title_callback.Clear();

            // Inicializa o map de Title call back condition
            mp_title_callback.Add(0x15, new stTitleMapCallback(better_hit_pangya_bronze, this));
            mp_title_callback.Add(0x16, new stTitleMapCallback(better_fairway_bronze, this));
            mp_title_callback.Add(0x17, new stTitleMapCallback(better_putt_bronze, this));
            mp_title_callback.Add(0x18, new stTitleMapCallback(master_course, this));
            mp_title_callback.Add(0x19, new stTitleMapCallback(atirador_de_ouro, this));
            mp_title_callback.Add(0x1a, new stTitleMapCallback(atirador_de_silver, this));
            mp_title_callback.Add(0x1b, new stTitleMapCallback(atirador_de_bronze, this));
            mp_title_callback.Add(0x1C, new stTitleMapCallback(better_quit_rate_bronze, this));
            mp_title_callback.Add(0x32, new stTitleMapCallback(better_hit_pangya_silver, this));
            mp_title_callback.Add(0x33, new stTitleMapCallback(better_fairway_silver, this));
            mp_title_callback.Add(0x34, new stTitleMapCallback(better_putt_silver, this));
            mp_title_callback.Add(0x35, new stTitleMapCallback(better_quit_rate_silver, this));
            mp_title_callback.Add(0x45, new stTitleMapCallback(natural_record_420, this));
            mp_title_callback.Add(0x46, new stTitleMapCallback(natural_record_390, this));
            mp_title_callback.Add(0x47, new stTitleMapCallback(natural_record_350, this));
            mp_title_callback.Add(0x48, new stTitleMapCallback(natural_record_300, this));
            mp_title_callback.Add(0x49, new stTitleMapCallback(natural_record_200, this));
            mp_title_callback.Add(0x4a, new stTitleMapCallback(natural_record_80, this));
            mp_title_callback.Add(0x7B, new stTitleMapCallback(better_quit_rate_gold, this));
            mp_title_callback.Add(0x7C, new stTitleMapCallback(better_putt_gold, this));
            mp_title_callback.Add(0x7D, new stTitleMapCallback(better_fairway_gold, this));
            mp_title_callback.Add(0x7E, new stTitleMapCallback(better_hit_pangya_gold, this));
            mp_title_callback.Add(0x17C, new stTitleMapCallback(natural_record_470, this));
            mp_title_callback.Add(0x17D, new stTitleMapCallback(natural_record_540, this));
        }

        public int addExp(int expGain)
        {
            if (expGain <= 0) return 0;

            int levelsGained = 0;
            int totalExpBefore = ui.exp; // Para o log final

            try
            {
                // Trava para nível máximo (Índice 69 do seu array de 70 elementos)
                if (level >= 69)
                {
                    _smp.message_pool.getInstance().push(new message($"[AddExp][MaxLevel] PLAYER[UID={uid}] já é Level Máximo (70). Ignorando {expGain} EXP.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return -1;
                }

                // Soma a EXP ganha ao que o player já tinha
                ui.exp += expGain;
                byte oldLevel = (byte)level;

                // Loop de processamento de Level Up
                while (level < 69)
                {
                    // Pega quanto custa para sair do nível atual (Ex: Level 0 precisa de 30)
                    int costToLevelUp = Convert.ToInt32(ExpByLevel[(byte)level]);

                    // Se encontrar o 0 no final do seu array, para.
                    if (costToLevelUp <= 0)
                    {
                        _smp.message_pool.getInstance().push(new message($"[AddExp][Info] PLAYER[UID={uid}] atingiu o limite da tabela de EXP no Level {level}.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }

                    // Verifica se a EXP acumulada paga o próximo nível
                    if (ui.exp >= costToLevelUp)
                    {
                        ui.exp -= costToLevelUp; // Subtrai o custo (Ex: 32 - 30 = 2)
                        level++;                       // Sobe o nível
                        levelsGained++;

                        // Atualiza as estruturas
                        mi.level = (byte)level;
                        ui.level = (byte)level;
                    }
                    else
                    {
                        // Se a sobra (Ex: 2) for menor que o custo do próximo level (Ex: 40), para aqui.
                        break;
                    }
                }
                 
                // Envia para o Banco de Dados
                snmdb.NormalManagerDB.getInstance().add(
                    3,
                    _pangya_db: new CmdUpdateLevelAndExp(uid, (byte)level, ui.exp),
                    SQLDBResponse,
                    this
                );
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[AddExp][CriticalError] PLAYER[UID={uid}]: {e.Message} | Stack: {e.StackTrace}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return levelsGained;
        }

        public void addGrandZodiacPontos(ulong _pontos)
        {
            if (_pontos < 0)
                throw new exception("[PlayerInfo::addGrandZodiacPontos][Error] invalid _pontos(" + _pontos + "), ele é negativo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 101, 0));

            grand_zodiac_pontos += _pontos;

            // Update no Banco de dados
            snmdb.NormalManagerDB.getInstance().add(8, new CmdGrandZodiacPontos(uid, (uint)grand_zodiac_pontos, CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_UPDATE), SQLDBResponse, this);
             
        }

        public void consomeMoeda(ulong _pang, ulong _cookie)
        {

            if (_pang > 0)
                consomePang(_pang);

            if (_cookie > 0)
                consomeCookie(_cookie);
        }

        public void consomeCookie(ulong _cookie)
        {

            if (_cookie <= 0)
                throw new exception("[PlayerInfo::consomeCookie][Error] _cookie valor invalido: " + ((long)_cookie), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on cookie of DB
                if (checkAlterationCookieOnDB())
                    throw new exception("[PlayerInfo::consomeCookie][Error] PLAYER[UID=" + (uid) + "] cookie on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

                if ((cookie - _cookie) < 0)
                    throw new exception("[PlayerInfo::consomeCookie][Error] O PLAYER[UID=" + (uid) + "] nao tem cookies suficiente para consumir", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 20, 0));

                cookie -= _cookie;

                m_update_cookie_db.requestUpdateOnDB();

                snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateCookie(uid, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.DECREASE), SQLDBResponse, this);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::consomeCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void consomePang(ulong _pang)
        {

            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::consomePang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on pang of DB
                if (checkAlterationPangOnDB())
                    throw new exception("[PlayerInfo::consomePang][Error] PLAYER[UID=" + (uid) + "] pang on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));
                // 3. CORREÇÃO AQUI: Comparação direta antes da subtração
                if (ui.pang < _pang)
                {
                    throw new exception($"[PlayerInfo::consomePang] PLAYER[UID={uid}] saldo insuficiente (Saldo: {ui.pang}, Custo: {_pang})",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 20, 0));
                }

                ui.pang -= _pang;

                m_update_pang_db.requestUpdateOnDB();

                NormalManagerDB.getInstance().add(1, new CmdUpdatePang(uid, _pang, CmdUpdatePang.T_UPDATE_PANG.DECREASE), SQLDBResponse, this);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::consomePang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void addMoeda(ulong _pang, ulong _cookie)
        {

            if (_pang > 0)
                addPang(_pang);

            if (_cookie > 0)
                addCookie(_cookie);
        }

        public void addCookie(ulong _cookie)
        {

            if ((long)_cookie <= 0)
                throw new exception("[PlayerInfo::addCookie][Error] _cookie valor invalido: " + ((long)_cookie), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on cookie of DB 
                if (checkAlterationCookieOnDB())
                    throw new exception("[PlayerInfo::addCookie][Error] PLAYER[UID=" + (uid) + "] cookie on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

                cookie += _cookie;

                m_update_cookie_db.requestUpdateOnDB();

                snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateCookie(uid, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.INCREASE), SQLDBResponse, this);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::addCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void addPang(ulong _pang)
        {

            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::addPang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on pang of DB 
                if (checkAlterationPangOnDB())
                {

                    // Pang é diferente atualiza o pang com o valor do banco de daos
                    _smp.message_pool.getInstance().push(new message("[PlayerInfo::addPang][Error] PLAYER[UID=" + (uid) + "] pang on db is different of server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    var old_pang = ui.pang;

                    // Atualiza o valor do pang do server com o do banco de dados
                    updatePang();
                }

                // Add o pang para o player
                ui.pang += _pang;

                m_update_pang_db.requestUpdateOnDB();

                snmdb.NormalManagerDB.getInstance().add(1, new CmdUpdatePang(uid, _pang, CmdUpdatePang.T_UPDATE_PANG.INCREASE), SQLDBResponse, this);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::addPang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void updateMoeda()
        {

            // Update Cookie
            updateCookie();

            // Update Pang
            updatePang();

        }

        public void updateCookie()
        {
            try
            {

                var cmd_cp = new CmdCookie(uid);    // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_cp, null, null);

                if (cmd_cp.getException().getCodeError() != 0)
                    throw cmd_cp.getException();

                cookie = cmd_cp.getCookie();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::updateCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relanção por que essa função não tem retorno para verifica, então a exception garante que o código não vai continua
                throw;
            }
        }

        public void updatePang()
        {
            try
            {

                var cmd_pang = new CmdPang(uid);    // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_pang, null, null);

                if (cmd_pang.getException().getCodeError() != 0)
                    throw cmd_pang.getException();

                ui.pang = cmd_pang.getPang();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::updatePang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relanção por que essa função não tem retorno para verifica, então a exception garante que o código não vai continua
                throw;
            }
        }
       
        // Adiciona Pang Estático
        public static void addPang(uint _uid, ulong _pang)
        {
            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::addPang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            PlayerInfo playerInfo = null;
            if (sgs.gs.getInstance().FindSessionByUid(_uid) != null)
            {
                playerInfo = sgs.gs.getInstance().findPlayer(_uid).m_pi;
            }

            NormalManagerDB.getInstance().add(1, new CmdUpdatePang(_uid, _pang, CmdUpdatePang.T_UPDATE_PANG.INCREASE), SQLDBResponse, playerInfo);
        }

        // Adiciona Cookie Point(CP) Estático
        public static void addCookie(uint _uid, ulong _cookie)
        {

            if ((long)_cookie <= 0)
                throw new exception("[PlayerInfo::addCookie][Error] _cookie valor invalido: " + ((long)_cookie), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            PlayerInfo playerInfo = null;
            if (sgs.gs.getInstance().FindSessionByUid(_uid) != null)
            {
                playerInfo = sgs.gs.getInstance().findPlayer(_uid).m_pi;
            }

            snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateCookie(_uid, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.INCREASE), SQLDBResponse, playerInfo);
        }

        public void addUserInfo(UserInfoEx _ui, ulong _total_pang_win_game = 0)
        {
            ui.add(_ui, (uint)_total_pang_win_game);

            // Update User Info ON DB
            updateUserInfo();
        }

        public bool checkAlterationCookieOnDB()
        {
            var cmd_cp = new CmdCookie(uid);    // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_cp, null, null);


            if (cmd_cp.getException().getCodeError() != 0)
                throw cmd_cp.getException();

            return (cmd_cp.getCookie() != cookie);
        }

        public bool checkAlterationPangOnDB()
        {
            var cmd_pang = new CmdPang(uid);    // Waiter

            NormalManagerDB.getInstance().add(0, cmd_pang, null, null);

            if (cmd_pang.getException().getCodeError() != 0)
                throw cmd_pang.getException();

            return (cmd_pang.getPang() != ui.pang);
        }

        public bool checkEquipedItem(uint _typeid)
        {
            return mp_wi.Any(c=> c.Value._typeid == _typeid);
        }

        public PlayerRoomInfo.uItemBoost checkEquipedItemBoost()
        {

            PlayerRoomInfo.uItemBoost ib = new PlayerRoomInfo.uItemBoost();

            //Pang
            foreach (var _el in mp_wi)
            {
                // Pang Boost X2
                // Verifica a quantidade do item para gastar menos processo se ele não tiver a quantidade necessária para ativar a PCBangMascot
                if (_el.Value.STDA_C_ITEM_QNTD > 0 && passive_item_pang_x2.Any(c => c == _el.Value._typeid))
                    ib.ucPangMastery = 1;

                // Pang Boost X4
                // Verifica a quantidade do item para gastar menos processo se ele não tiver a quantidade necessária para ativar a PCBangMascot
                if (_el.Value.STDA_C_ITEM_QNTD > 0 && passive_item_pang_x4.Any(c => c == _el.Value._typeid))
                    ib.ucPangNitro = 1;

                // Tenta não consumir mais processo, quando já estiver as duas PCBangMascot setada.
                // Tentando verificar outros itens que possa ter ainda no map
                if (ib.ucPangMastery == 1 && ib.ucPangNitro == 1)
                    break;
            }

            return ib;
        }

        public CaddieInfoEx findCaddieById(int _id)
        {
            return mp_ci.findCaddieById(_id);
        }

        public CaddieInfoEx findCaddieByTypeid(uint _typeid)
        {
            return mp_ci.findCaddieByTypeid(_typeid);
        }

        public CaddieInfoEx findCaddieByTypeidAndId(uint _typeid = 0, int _id = 0)
        {
            return mp_ci.findCaddieByTypeidAndId(_typeid, _id);
        }

        public CardInfo findCardById(int _id)
        {
            return v_card_info.findCardById(_id);
        }

        public CardInfo findCardByTypeid(uint _typeid)
        {
            return v_card_info.findCardByTypeid(_typeid);
        }

        public CardEquipInfoEx findCardEquipedById(int _id, int _char_typeid, int _slot)
        {
            return v_cei.FirstOrDefault(_element =>
            {
                return (_element.id == _id && ((_char_typeid == 0 && _slot == 0)
              || (_element.parts_typeid == _char_typeid && _element.slot == _slot)));
            });
        }

        public CardEquipInfoEx findCardEquipedByTypeid(uint _typeid, int _char_typeid = 0, int _slot = 0, int _tipo = 0, int _efeito = 0)
        {
            return v_cei.FirstOrDefault(_element =>
            {
                return ((_element._typeid == _typeid || (_element.tipo == _tipo && _element.efeito == _efeito))
            && ((_char_typeid == 0 && _slot == 0) || (_element.parts_typeid == _char_typeid && _element.slot == _slot)));

            });
        }

        public void UpdateCharacter(int _id, CharacterInfo character)
        {
            if (findCharacterById(_id) != null)
            {
                this.mp_ce[_id] = character;
                if (ei.char_info.id == _id)
                    ei.char_info = character;//atualiza se forem iguais
            }
        }
        public CharacterInfo findCharacterById(int _id)
        {
            return this.mp_ce.findCharacterById(_id);
        }

        public CharacterInfo findCharacterByTypeid(uint _typeid)
        {
            return this.mp_ce.findCharacterByTypeid(_typeid);
        }

        public CharacterInfo findCharacterByTypeidAndId(uint _typeid, int _id)
        {
            return this.mp_ce.findCharacterByTypeidAndId(_typeid, _id);
        }

        public FriendInfo findFriendInfoById(string _id)
        {
            return mp_fi.FirstOrDefault(_el => _el.Value.id == _id).Value;
        }

        public FriendInfo findFriendInfoByNickname(string _nickname)
        {
            return mp_fi.FirstOrDefault(_el => _el.Value.nickname == _nickname).Value;
        }

        public FriendInfo findFriendInfoByUID(int _uid)
        {
            if (_uid == 0u)
            {

                return null;
            }

            var it = mp_fi.Where(c => c.Key == _uid);

            return it.Any() ? it.First().Value : null;
        }

        public GrandPrixClear findGrandPrixClear(uint _typeid)
        {
            if (_typeid == 0u)
                return null;

            var it = v_gpc.FirstOrDefault(_el =>
            {
                return _el._typeid == _typeid;
            });

            return it;
        }

        public MascotInfoEx findMascotById(int _id)
        {
            return mp_mi.findMascotById(_id);
        }

        public MascotInfoEx findMascotByTypeid(uint _typeid)
        {
            return mp_mi.findMascotByTypeid(_typeid);
        }

        public MascotInfoEx findMascotByTypeidAndId(uint _typeid, int _id)
        {
            return mp_mi.findMascotByTypeidAndId(_typeid, _id);
        }

        public MyRoomItem findMyRoomItemById(int _id)
        {
            return v_mri.FirstOrDefault(el => el.id == _id);
        }

        public MyRoomItem findMyRoomItemByTypeid(uint _typeid)
        {
            return v_mri.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrofelEspecialInfo findTrofelEspecialById(int _id)
        {
            return v_tsi_current_season.FirstOrDefault(el => el.id == _id);
        }

        public TrofelEspecialInfo findTrofelEspecialByTypeid(uint _typeid)
        {
            return v_tsi_current_season.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrofelEspecialInfo findTrofelEspecialByTypeidAndId(uint _typeid, int _id)
        {
            return v_tsi_current_season.FirstOrDefault(el => el.id == _id && el._typeid == _typeid);
        }

        public TrofelEspecialInfo findTrofelGrandPrixById(int _id)
        {
            return v_tgp_current_season.FirstOrDefault(el => el.id == _id);
        }

        public TrofelEspecialInfo findTrofelGrandPrixByTypeid(uint _typeid)
        {
            return v_tgp_current_season.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrofelEspecialInfo findTrofelGrandPrixByTypeidAndId(uint _typeid, int _id)
        {
            return v_tgp_current_season.FirstOrDefault(el => el.id == _id && el._typeid == _typeid);
        }

        public WarehouseItemEx findWarehouseItemById(int _id)
        {
            return mp_wi.findWarehouseItemById(_id);
        }

        public bool ItemExist(uint _typeid)
        {
            if (mp_wi.findWarehouseItemByTypeid(_typeid) != null)
                return true;

            if (v_card_info.findCardByTypeid(_typeid) != null)
                return true;

            return false;
        }


        public WarehouseItemEx findWarehouseItemByTypeid(uint _typeid)
        {
            return mp_wi.findWarehouseItemByTypeid(_typeid);
        }

        public WarehouseItemEx findWarehouseItemByTypeidAndId(uint _typeid, int _id)
        {
            return mp_wi.findWarehouseItemByTypeidAndId(_typeid, _id);
        }

        public int getCharacterMaxSlot(CharacterInfo.Stats _stats)
        {

            // pega o número máximo de slot de power do character equipado
            int value = 0;

            if (ei.char_info == null)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getCharacterMaxSlotPower][Error][Warning] PLAYER[UID=" + (uid)
                        + "] nao tem nenhum character equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            var value_part = ei.char_info.getSlotOfStatsFromCharEquipedPartItem(_stats);
            var value_auxpart = ei.char_info.getSlotOfStatsFromCharEquipedAuxPart(_stats);
            var value_set_effect_table = ei.char_info.getSlotOfStatsFromSetEffectTable(_stats);
            var value_card = ei.char_info.getSlotOfStatsFromCharEquipedCard(_stats);

            if (value_part == -1 || value_card == -1 || value_auxpart == -1 || value_set_effect_table == -1)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getCharacterMaxSlotPower][Error][Warning] PLAYER[UID="
                        + (uid) + "], value of slots stat[value=" + (_stats) + "] is invalid. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            // Slot de Part Equiped
            value += value_part;

            // Slot de AuxPart Equiped
            value += value_auxpart;

            // Slot do Set Effect Table
            value += value_set_effect_table;

            // Slot de Card Equiped
            value += value_card;

            // Level + POWER, cada level da +1 de POWER
            if (_stats == CharacterInfo.Stats.S_POWER)
                value += ((mi.level - 1/*Rookie tem uma letra a+*/) / 5/*Levels*/);

            var mastery = sIff.getInstance().findCharacterMastery(ei.char_info._typeid);

            if (mastery == null || mastery.Count ==0 )
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getSlotPower][Error][Warning] PLAYER[UID=" + (uid)
                        + "] tentou pegar os slots stat[value=" + (_stats) + "] do Character[TYPEID=" + (ei.char_info._typeid) + ", ID="
                        + (ei.char_info.id) + "], mas nao tem o Character Mastery no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            if (mastery.Count() < ei.char_info.mastery)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getSlotPower][Error][Warning] PLAYER[UID=" + (uid)
                        + "] tentou pegar os slots stat[value=" + (_stats) + "] do Character[TYPEID=" + (ei.char_info._typeid)
                        + ", ID=" + (ei.char_info.id) + "], mas o CharacterMastery[value=" + (ei.char_info.mastery)
                        + ", vector_Count=" + (mastery.Count()) + "] do player e invalido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            // Character Mastery
            for (var i = 0; i < ei.char_info.mastery; ++i)
                if ((mastery[i].stats - 1) == (byte)_stats)
                    value++;

            return value;
        }

        public int getClubSetMaxSlot(CharacterInfo.Stats _stats)
        {

            int value = 0;

            if (ei.clubset == null || ei.clubset._typeid != ei.csi._typeid)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getClubSetMaxSlotPower][Error][Warning] PLAYER[UID=" + (uid)
                        + "] nao tem o clubset equipado ou o ClubSet Info nao esta inicializado para o clubset equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            var clubset = sIff.getInstance().findClubSet(ei.clubset._typeid);

            if (clubset == null)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::getClubSetMaxSlotPower][Error][Warning] PLAYER[UID=" + (uid)
                        + "] nao tem o ClubSet[TYPEID=" + (ei.clubset._typeid) + "] no IFF_STRUCT do server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            value = (clubset.SlotStats.getSlot[(byte)_stats] - clubset.Stats.getSlot[(byte)_stats]) + ei.clubset.clubset_workshop.c[(byte)_stats];

            return value;
        }

        public int getSizeCupGrandZodiac()
        {
            int size_cup = 1;

            if (grand_zodiac_pontos < 300)
                size_cup = 9;
            else if (grand_zodiac_pontos < 600)
                size_cup = 8;
            else if (grand_zodiac_pontos < 1200)
                size_cup = 7;
            else if (grand_zodiac_pontos < 1800)
                size_cup = 6;
            else if (grand_zodiac_pontos < 4000)
                size_cup = 5;
            else if (grand_zodiac_pontos < 5200)
                size_cup = 4;
            else if (grand_zodiac_pontos < 7600)
                size_cup = 3;
            else if (grand_zodiac_pontos < 10000)
                size_cup = 2;

            return size_cup;
        }

        public int getSlotPower()
        {
            uint power_slot = 0;

            // Check CL to Caddie, Mascot, Character, ClubSet, Ring and Card
            if (ei.cad_info != null)
            {

                var cad = sIff.getInstance().findCaddie(ei.cad_info._typeid);

                if (cad != null)
                    power_slot += cad.Stats.getSlot[0]; // Power
            }

            if (ei.mascot_info != null)
            {

                var mascot = sIff.getInstance().findMascot(ei.mascot_info._typeid);

                if (mascot != null)
                    power_slot += mascot.Power; // Power;
            }

            if (ei.char_info != null)
            {

                var character = sIff.getInstance().findCharacter(ei.char_info._typeid);

                if (character != null)
                {

                    power_slot += character.Power; // Power

                    // Parts
                    for (var i = 0; i < 24; ++i)
                    {

                        if (ei.char_info.parts_typeid[i] != 0)
                        {

                            var parts = sIff.getInstance().findPart(ei.char_info.parts_typeid[i]);

                            if (parts != null)
                                power_slot += parts.Stats.getSlot[0]; // Power

                        }
                    }

                    // Ring
                    for (var i = 0; i < 5; ++i)
                    {

                        if (ei.char_info.auxparts[i] != 0)
                        {

                            var auxpart = sIff.getInstance().findAuxPart(ei.char_info.auxparts[i]);

                            if (auxpart != null)
                                power_slot += auxpart.c[0]; // Power
                        }
                    }
                     
                    // Pega o valor máximo de slot de POWER
                    int value = getCharacterMaxSlot(CharacterInfo.Stats.S_POWER);

                    if (value != -1 && ei.char_info.pcl[0] > value)
                        power_slot += (uint)value;
                    else
                        power_slot += ei.char_info.pcl[0];
                }
            }

            // Card Special
            foreach (var it in v_cei)
            {

                if (it.parts_id == 0 && it.parts_typeid == 0 && sIff.getInstance().getItemSubGroupIdentify22(it._typeid) == 2/*Special*/ && it.efeito == 5)
                    power_slot += it.efeito_qntd;
            }

            if (ei.clubset != null && ei.clubset._typeid == ei.csi._typeid)
            {

                var clubset = sIff.getInstance().findClubSet(ei.clubset._typeid);

                // Base
                if (clubset != null)
                    power_slot += (uint)clubset.Stats.Power;

                // Pega o valor máximo de slot de POWER
                int value = getClubSetMaxSlot(CharacterInfo.Stats.S_POWER);

                if (value != -1 && ei.csi.slot_c[0] > value)
                    power_slot += (uint)value;
                else
                    power_slot += (uint)ei.csi.slot_c[0]; // Power
            }

            return (int)power_slot;
        }

        public int getSumRecordGrandPrix()
        {
            int grand_prix_record_sum = 0;

            foreach (var el in a_ms_grand_prix)
                if (el.isRecorded())
                    grand_prix_record_sum += el.best_score;

            return grand_prix_record_sum;
        }

        public bool isAuxPartEquiped(uint _typeid)
        {
            var it = mp_ce.FirstOrDefault(el =>
            {
                return el.Value.isAuxPartEquiped(_typeid);
            });

            return it.Key != 0;
        }

        public bool isPartEquiped(uint _typeid, int _id)
        {
            var it = mp_ce.FirstOrDefault(el =>
            {
                return el.Value.isPartEquiped(_typeid, _id);
            });

            return it.Key != 0;
        }


        public bool isFriend(int _uid)
        {
            if (_uid == 0u)
            {
                _smp.message_pool.getInstance().push(new message("[PlayerInfo::isFriend][Error] _uid is invalid(0)", 0));

                return false;
            }

            var it = mp_fi.find(_uid);

            return mp_fi.ContainsKey((uint)_uid);
        }

        public bool isMasterCourse()
        {
            sbyte[] clear_course = new sbyte[MS_NUM_MAPS];

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(a_ms_normal[i].isRecorded() ? 1 : 0);

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(a_ms_natural[i].isRecorded() ? 1 : 0);

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(a_ms_grand_prix[i].isRecorded() ? 1 : 0);

            // Conta quantos mapas foram completados (el == 1)
            int count = clear_course.Count(el => el == 1);
            // Deve ter completado todos menos os dois mapas excluídos
            return count == (MS_NUM_MAPS - 2)/*-2 por que tira o map 12 que nunca foi feito e o 17 que é o SSC*/;

        }

        public bool ownerCaddieItem(uint _typeid)
        {
            var cad = findCaddieByTypeid(((Convert.ToUInt32(sIff.getInstance().CADDIE << 26)) | sIff.getInstance().getCaddieIdentify(_typeid)));

            // Se não tiver o caddie não pode ter o caddie item(parts caddie)
            // Verificar se tem o caddie, o caddie item não precisa
            if (cad == null /*|| cad.parts_typeid != _typeid*/)
                return true;

            return false;
        }

        public bool ownerHairStyle(uint _typeid)
        {
            var hair = sIff.getInstance().findHairStyle(_typeid);

            if (hair != null)
            {
                var character = findCharacterByTypeid((uint)((sIff.getInstance().CHARACTER << 26) | hair.Character));

                if (character != null && character.default_hair == hair.Color)
                    return true;
            }

            return false;
        }

        public bool ownerItem(uint _typeid, int option = 0)
        {
            bool ret = false;

            // Verifica se ele tem no Dolfini Locker
            if (df.ownerItem(_typeid))
                return true;

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CHARACTER:
                    if (findCharacterByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.CADDIE:
                    if (findCaddieByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.MASCOT:
                    if (findMascotByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.CARD:
                    if (findCardByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.FURNITURE:
                    if (findMyRoomItemByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.BALL:
                case IFF_GROUP.AUX_PART:
                case IFF_GROUP.CLUBSET:
                case IFF_GROUP.ITEM:
                case IFF_GROUP.PART:
                case IFF_GROUP.SKIN:
                    if (findWarehouseItemByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.SET_ITEM:
                    ret = ownerSetItem(_typeid);
                    break;
                case IFF_GROUP.HAIR_STYLE:
                    ret = ownerHairStyle(_typeid);
                    break;
                case IFF_GROUP.CAD_ITEM:        // Esse aqui verifica se já tem, mas não que não pode ter mais. mas sim para aumentar o tempo
                    ret = ownerCaddieItem(_typeid);
                    break;
            }

            // Player não tem o item no warehouse e nem no Dolfini Locker, Verifica no Mail Box dele
            // Option diferente de 0 não verifica no Mail Box, por que o player está tirando do Mail Box o Item
            if (option == 0 && !ret)
            {

                // Verifica se ele tem no Mail Box
                ret = ownerMailBoxItem(_typeid);
            }

            return ret;
        }

        public bool ownerMailBoxItem(uint _typeid)
        {
            var cmd_fmbi = new CmdFindMailBoxItem(uid, _typeid);    // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_fmbi, null, null);

            if (cmd_fmbi.getException().getCodeError() != 0)
                throw cmd_fmbi.getException();

            if (cmd_fmbi.hasFound())
                return true;

            return false;
        }

        public bool ownerSetItem(uint _typeid)
        {
            var set = sIff.getInstance().findSetItem(_typeid);

            if (set != null)
            {
                for (var i = 0; i < set.packege.item_typeid.Length; ++i)
                {
                    // Eleminar a verificação do character que ele só inclui se o player não tiver ele
                    // se ele tiver não faz diferença não anula o verificação do set
                    if (set.packege.item_typeid[i] != 0 && sIff.getInstance().getItemGroupIdentify(set.packege.item_typeid[i]) != IFF_GROUP.CHARACTER)
                        if (ownerItem(set.packege.item_typeid[i])) // se tiver 1 item que seja não pode ganhar o set se não vai duplicar os itens, que ele tem
                            return true;
                }
            }

            return false;
        }


        public bool updateGrandPrixClear(uint _typeid, int _position)
        {

            if (_typeid == 0)
                throw new exception("[PlayerInfo::updateGrandPrixClear][Error] invliad _typeid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 100, 0));

            bool uptClient = false;

            // Procura se já existe o GP na lista
            var gp = v_gpc.Find(el => el._typeid == _typeid);

            if (gp == null)
            {
                // Não tem esse GP, acabou de completar um novo
                gp = new GrandPrixClear(_typeid, _position);

                v_gpc.Add(gp);

                // Insere no banco de dados
                NormalManagerDB.getInstance().add(6, new CmdInsertGrandPrixClear(uid, gp), SQLDBResponse, this);

                // Atualiza no cliente
                uptClient = true;
            }
            else
            {
                // Player já tem esse GP, verifica se ficou em uma posição melhor
                if (gp.position > _position)
                {
                    gp.position = (uint)_position;

                    // Update no DB
                    NormalManagerDB.getInstance().add(7, new CmdUpdateGrandPrixClear(uid, gp), SQLDBResponse, this);

                    // Update no cliente
                    uptClient = true;
                }
            }

            return uptClient;//nao tinha
        }

        public void updateLocationDB()
        {
            try
            {

                m_pl.channel = channel;
                m_pl.lobby = lobby;
                m_pl.room = mi.sala_numero;
                m_pl.place.ulPlace = (byte)place;

                //// Sincroniza para não ter valores inseridos errados no banco de dados
                m_pl.requestUpdateOnDB();

                snmdb.NormalManagerDB.getInstance().add(5, new CmdUpdatePlayerLocation(uid, m_pl), SQLDBResponse, this);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PlayerInfo::updateLocationDB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void updateMedal(uMedalWin _medal_win)
        {
            if (_medal_win.ucMedal == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] PLAYER[UID=" + uid
                        + "] tentou atualizar medalhas, mas passou nenhuma medalha para atualizar. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 600, 0));

            // Update medal info player
            ui.medal.add(_medal_win);

            // Update Info do player na database
            updateUserInfo();
        }
        // Update Medal Estático
        public static void updateMedal(uint _uid, uMedalWin _medal_win)
        {
            if (_uid == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar medalhas, mas o uid do player é invalido(zero). Hacker ou Bug.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 601, 0));

            if (_medal_win.ucMedal == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] PLAYER[UID=" + (_uid)
                        + "] tentou atualizar medalhas, mas passou nenhuma medalha para atualizar. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 600, 0));

            // Pega o Info do player para atualizar
            var cmd_ui = new CmdUserInfo(_uid);     // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_ui, null, null);

            if (cmd_ui.getException().getCodeError() != 0)
                throw cmd_ui.getException();

            var user_info = cmd_ui.getInfo();

            // Update medal info player
            user_info.medal.add(_medal_win);

            // Update Info do player na database
            updateUserInfo(_uid, user_info);
        }

        public void updateTrofelInfo(uint _trofel_typeid, byte _trofel_rank)
        {

            if (_trofel_typeid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que é invalido(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

            if (_trofel_typeid == TROFEL_GM_EVENT_TYPEID/*GM Event*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é normal, é um trofel de evento GM. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 201, 0));

            uint type = sIff.getInstance().getMatchTypeIdentity(_trofel_typeid);

            // Verifica se é o 2C e se o Tipo do Trofel é menor ou igual a 12, que é o Pro 7 o ultimo
            if (sIff.getInstance().getItemSubGroupIdentify24(_trofel_typeid) != 0 && type > 12/*Pro 7*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é normal, é um outro trofel. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 202, 0));

            if (_trofel_rank == 0u || _trofel_rank > 3)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] rank é invalido. Bug,", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 203, 0));

            // Update Trofel Info Atual (season atual)
            ti_current_season.update(type, _trofel_rank);

            snmdb.NormalManagerDB.getInstance().add(4, new CmdUpdateNormalTrofel(uid, ti_current_season), SQLDBResponse, this);

        }
        // Update Trofel Info Estático
        public static void updateTrofelInfo(uint _uid, uint _trofel_typeid, byte _trofel_rank)
        {

            if (_uid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "], mas uid is invalid(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 204, 0));

            if (_trofel_typeid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que é invalido(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

            if (_trofel_typeid == TROFEL_GM_EVENT_TYPEID/*GM Event*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é normal, é um trofel de evento GM. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 201, 0));

            var type = sIff.getInstance().getMatchTypeIdentity(_trofel_typeid);

            // Verifica se é o 2C e se o Tipo do Trofel é menor ou igual a 12, que é o Pro 7 o ultimo
            if (sIff.getInstance().getItemSubGroupIdentify24(_trofel_typeid) != 0 && type > 12/*Pro 7*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é normal, é um outro trofel. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 202, 0));

            if (_trofel_rank == 0u || _trofel_rank > 3)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] PLAYER[UID=" + (_uid) + "] tentou atualizar um trofel[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] rank é invalido. Bug,", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 203, 0));

            var cmd_ti = new CmdTrofelInfo(_uid, CmdTrofelInfo.TYPE_SEASON.CURRENT); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_ti, null, null);

            if (cmd_ti.getException().getCodeError() != 0)
                throw cmd_ti.getException();

            var ti = cmd_ti.getInfo();

            // Update Trofel Info Atual (season atual)
            ti.update(type, _trofel_rank);

            snmdb.NormalManagerDB.getInstance().add(4, new CmdUpdateNormalTrofel(_uid, ti), SQLDBResponse, null);

        }

        public void updateUserInfo()
        {
            snmdb.NormalManagerDB.getInstance().add(3, new CmdUpdateUserInfo(uid, ui), SQLDBResponse, this);
        }

        // Update User Info ON DB Estático 
        public static void updateUserInfo(uint _uid, UserInfoEx _ui)
        {
            if (_uid == 0)
                throw new exception("[PlayerInfo::updateUserInfo][Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 300, 0));

            snmdb.NormalManagerDB.getInstance().add(3, new CmdUpdateUserInfo(_uid, _ui), SQLDBResponse, null);
        }

        public Dictionary<stIdentifyKey/*int/*ID*/, UpdateItem> findUpdateItemByTypeidAndType(uint _typeid, UpdateItem.UI_TYPE _type)
        {
            return mp_ui
                .Where(it => it.Value._typeid == _typeid && it.Value.type == _type)
                .ToDictionary(it => it.Key, it => it.Value);
        }

        public Dictionary<stIdentifyKey/*int/*ID*/, UpdateItem> findUpdateItemByTypeidAndId(uint _typeid, int _id)
        {
            return mp_ui
                .Where(it => it.Value._typeid == _typeid && it.Value.id == _id)
                .ToDictionary(it => it.Key, it => it.Value);
        }



        public void ReloadMemberInfo()
        {
            try
            {
                var cmd_member_info = new CmdMemberInfo(uid);

                snmdb.NormalManagerDB.getInstance().add(0, cmd_member_info, null, null);

                if (cmd_member_info.getException().getCodeError() != 0)
                    throw cmd_member_info.getException();
                //get new
                var _mi = cmd_member_info.getInfo();
                ui.level = _mi.level;
                mi.level = _mi.level;
                mi.sexo = _mi.sexo;
                mi.do_tutorial = _mi.do_tutorial;
                mi.school = _mi.school;
                mi.capability = _mi.capability;
                m_cap = _mi.capability;
                mi.manner_flag = _mi.manner_flag;
                mi.guild_name = _mi.guild_name;
                mi.guild_pang = _mi.guild_pang;
                mi.guild_point = _mi.guild_point;
                mi.event_1 = _mi.event_1;
                mi.event_2 = _mi.event_2;
                mi.papel_shop = _mi.papel_shop;
                mi.papel_shop_last_update = _mi.papel_shop_last_update;
                mi.flag_block = _mi.flag_block;
                mi.channeling_flag = _mi.channeling_flag;
                this.level = _mi.level;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::updateMemberInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relanção por que essa função não tem retorno para verifica, então a exception garante que o código não vai continua
                throw;
            }
        }


        public static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[PlayerInfo::SQLDBResponse][Warning] _arg is null na msg_id = " + (_msg_id), 0));
                return;
            }

            try
            {
                var pi = (PlayerInfo)_arg;


                // Por Hora só sai, depois faço outro tipo de tratamento se precisar
                if (_pangya_db.getException().getCodeError() != 0)
                {

                    // Trata alguns tipo aqui, que são necessários
                    switch (_msg_id)
                    {
                        case 1: // Update Pang
                            {
                                // Error at update on DB
                                pi.m_update_pang_db.errorUpdateOnDB();

                                break;
                            }
                        case 2: // Update Cookie
                            {
                                // Error at update on DB
                                pi.m_update_cookie_db.errorUpdateOnDB();

                                break;
                            }
                        case 5: // Update Location Player on DB
                            {
                                // Error at update on DB
                                pi.m_pl.errorUpdateOnDB();

                                break;
                            }
                    }

                    _smp.message_pool.getInstance().push(new message("[PlayerInfo::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), 0));

                    return;
                }

                else
                {
                    if (pi != null)
                    {
                        switch (_msg_id)
                        {
                            case 1: // UPDATE pang
                                {

                                    // Success update on DB
                                    pi.m_update_pang_db.confirmUpdadeOnDB();
                                    break;
                                }
                            case 2: // UPDATE cookie
                                {

                                    // Success update on DB
                                    pi.m_update_cookie_db.confirmUpdadeOnDB();
                                    break;
                                }
                            case 3: // UPDATE USER INFO
                                {
                                    break;
                                }
                            case 4: // Update Normal Trofel Info
                                {
                                    break;
                                }
                            case 5: // Update Location Player on DB
                                {
                                    // Success update on DB
                                    pi.m_pl.confirmUpdadeOnDB();

                                    var cmd_upl = (CmdUpdatePlayerLocation)(_pangya_db); break;
                                }
                            case 6: // Insert Grand Prix Clear
                                {

                                    var cmd_igpc = (CmdInsertGrandPrixClear)(_pangya_db);

                                    break;
                                }
                            case 7: // Update Grand Prix Clear
                                {
                                    var cmd_ugpc = (CmdUpdateGrandPrixClear)(_pangya_db);
                                    break;
                                }
                            case 8: // Update Grand Zodiac Pontos
                                {
                                    var cmd_gzp = (CmdGrandZodiacPontos)(_pangya_db);
                                    break;
                                }
                            case 0:
                            default:
                                break;
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PlayerInfo::SQLDBResponse][Error] QUERY_MSG[ID=" + (_msg_id)
                        + "]" + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public List<WarehouseItemEx> findAllPartNotEquiped(uint _typeid)
        {

            List<WarehouseItemEx> v_item = new List<WarehouseItemEx>();

            foreach (var el in mp_wi)
            {

                var item = el.Value;

                bool hasPartEquipped = mp_ce.Any(el2 => el2.Value.isPartEquiped(item._typeid, item.id));

                if (item._typeid == _typeid &&
                    (item.flag & 96) != 96 &&   // Não pode Part Rental
                    (item.flag & 0x20) != 0x20 &&
                    (item.flag & 0x40) != 0x40 &&
                    !hasPartEquipped)
                {

                    v_item.Add(item);
                }
            }

            return v_item;
        }

        public ItemBuffEx findItemBuff(uint _typeid, uint _tipo = 0)
        {
            var it = v_ib.FirstOrDefault(el =>
            {
                return (el._typeid == _typeid || el.tipo == _tipo);
            });

            return it;
        }
        /// <summary>
        /// Normal = 903,
        /// Natural = 903,                           
        /// </summary>
        /// <returns>Bytes Write -> 1806 Size</returns>
        public byte[] GetMapStatistic()
        {
            using (var p = new PangyaBinaryWriter())
            {
                for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                    p.WriteBytes(a_ms_normal[st_i].ToArray());

                // Map Statistics Natural
                for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                    p.WriteBytes(a_ms_natural[st_i].ToArray());

                // Map Statistics Grand Prix
                for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                    p.WriteBytes(a_ms_grand_prix[st_i].ToArray());

                // Map Statistics Normal for all seasons
                for (int j = 0; j < 9; j++)
                    for (var st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                        p.WriteBytes(aa_ms_normal_todas_season[j, st_i].ToArray());

                return p.GetBytes;
            }
        }

        /// <summary>
        /// Character(460 bytes), Caddie(25 bytes), ClubSet(28 bytes), Mascot(62 bytes), Total Size 628 
        /// </summary>
        /// <returns>Equiped Item(628 array of byte)</returns>
        public byte[] getUserEquipedItem()
        {
            return ei.ToArray();
        }

        /// <summary>
        /// Size = 116 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] getUserEquip()
        {
            return ue.ToArray();
        }

        /// <summary>
        /// Size = 235 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] getUserInfo()
        {
            return ui.ToArray();
        }
        /// <summary>
        /// Size = 239 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] getInfoTrophy()
        {
            return ti_current_season.ToArray();
        }

        /// <summary>
        /// Size = 263 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] getLoginInfo()
        {
            return mi.ToArrayEx();
        }
    }
}
