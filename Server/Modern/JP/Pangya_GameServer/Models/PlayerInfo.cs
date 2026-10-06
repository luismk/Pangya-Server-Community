using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using PangyaAPI.DataBase;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using static Pangya_GameServer.Models.DefineConstants; 
namespace Pangya_GameServer.Models
{
    public partial class PlayerInfo : PlayerInfoBase
    {
        /// <summary> Sinaliza se o HoleMode de assistência (mira/guia) está ativado. </summary>
        public bool AssistFlag { get; set; }

        /// <summary> Quantidade de Cookies do jogador. </summary>
        public ulong Cookie { get; set; }

        /// <summary> Informações básicas de conta e nível de acesso do membro. </summary>
        public PlayerMemberInfo Member { get; set; }

        /// <summary> Estatísticas gerais de jogo (Pang, experiência, vitórias, derrotas, etc). </summary>
        public PlayerUserStatistics Statistics { get; set; }

        /// <summary> Estado atual do tutorial para o jogador. </summary>
        public TutorialInfo Tutorial { get; set; }

        /// <summary> Macros de chat personalizadas pelo usuário. </summary>
        public ChatMacroUser ChatMacro { get; set; }

        #region Estatísticas de Mapas (Temporada Atual)

        /// <summary> Estatísticas de mapas no HoleMode Normal (Temporada Atual). </summary>
        public List<MapStatisticsEx> NormalMapStatistics { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Estatísticas acumuladas de mapas no HoleMode Normal com Assistência. </summary>
        public List<MapStatisticsEx> NormalMapStatisticsAll { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Estatísticas de mapas no HoleMode Natural (Temporada Atual). </summary>
        public List<MapStatisticsEx> NaturalMapStatistics { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Estatísticas acumuladas de mapas no HoleMode Natural com Assistência. </summary>
        public List<MapStatisticsEx> NaturalMapStatisticsAll { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Estatísticas de mapas no HoleMode Grand Prix (Temporada Atual). </summary>
        public List<MapStatisticsEx> GrandPrixMapStatistics { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Estatísticas acumuladas de mapas no HoleMode Grand Prix com Assistência. </summary>
        public List<MapStatisticsEx> GrandPrixMapStatisticsAll { get; set; } = new List<MapStatisticsEx>(MS_NUM_MAPS);

        /// <summary> Matriz de estatísticas históricas de todas as temporadas [Season, MapId]. </summary>
        public MapStatistics[,] AllSeasonsMapStatistics { get; set; } = new MapStatistics[9, MS_NUM_MAPS];

        #endregion

        /// <summary> cabeca, corpo, nao entendo direito ainda. </summary>
        public Dictionary<int, StateCharacterLounge> CharacterLoungeStates { get; set; }

        /// <summary> Histórico de eventos Grand Prix que o jogador já completou. </summary>
        public List<GrandPrixClear> GrandPrixHistory { get; set; }

        /// <summary> Lista de amigos do jogador (Chave: UID do Amigo). </summary>
        public Dictionary<uint, FriendInfo> Friends { get; set; }

        /// <summary> Controle de recompensas de presença (Daily Attendance). </summary>
        public AttendanceRewardInfoEx Attendance { get; set; }

        /// <summary> Gerenciador de conquistas (Achievements) e progresso do jogador. </summary>
        public AchievementManager Achievements { get; set; }

        /// <summary> Informações sobre a guilda a qual o jogador pertence. </summary>
        public GuildInfoEx Guild { get; set; }

        /// <summary> Estado das missões diárias (Daily Quests) do usuário. </summary>
        public DailyQuestInfoUser DailyQuests { get; set; }

        /// <summary> Histórico dos últimos 5 jogadores com quem este usuário jogou. </summary>
        public Last5PlayersGame GameHistory { get; set; }

        #region Localização e Estado de Conexão

        /// <summary> Localizacao geometrica do jogador na sala, lounger, etc.. </summary>
        public stLocation CurrentLocation { get; set; }

        /// <summary> Identificador do lugar/sala onde o player está (-1 se nenhum). </summary>
        public sbyte Place { get; set; } = -1;

        /// <summary> ID do Lobby onde o jogador está conectado, multi play ou grand prix. </summary>
        public byte Lobby { get; set; } = 255;

        /// <summary> ID do Canal atual do jogador. </summary>
        public sbyte Channel { get; set; } = -1;

        /// <summary> Estado do sussurro (Whisper): 0 = OFF, 1 = ON. </summary>
        public byte WhisperState { get; set; } = 1;

        /// <summary> Postura/Pose do personagem dentro da sala. </summary>
        public uint PostureRoom { get; set; }

        /// <summary> Estado de interação do jogador no Lounge. </summary>
        public uint LoungeState { get; set; }

        /// <summary> Dados de animação ativa do personagem no Lounge. </summary>
        public byte[] ChatSpecial { get; set; }
        #endregion

        /// <summary> Flags de capacidades e permissões especiais do usuário (Ex: GM, VIP). </summary>
        public PlayerCapability UserCapabilities { get; set; }

        /// <summary> Pontos acumulados no HoleMode Grand Zodiac. </summary>
        public ulong GrandZodiacPoints { get; set; }

        /// <summary> Moeda da loja de pontos Tiki (Sistema de troca antigo). </summary>
        public ulong PointShopLegacy { get; set; }

        /// <summary> Saldo de pontos da loja integrada via Web. </summary>
        public long PointShopWeb { get; set; } = 0;

        /// <summary> Gerenciador da caixa de entrada de e-mails/mensagens do jogador. </summary>
        public MailBoxManager MailBox { get; set; }

        #region Sincronização com Banco de Dados

        /// <summary> Estrutura de localização persistida no DB para reconexão. </summary>
        public stPlayerLocationDB LocationDB { get; set; }

        /// <summary> Controle de sincronização de Pang (Moeda Gold) com o banco de dados. </summary>
        public stSyncUpdateDB SyncPangDB { get; set; }

        /// <summary> Controle de sincronização de Cookies (Moeda Cash) com o banco de dados. </summary>
        public stSyncUpdateDB SyncCookieDB { get; set; }

        #endregion
        public PlayerInfo()
        {
            CurrentLocation = new stLocation();
            BlockFlag = new PlayerBlockFlag();
            LocationDB = new stPlayerLocationDB();
            SyncPangDB = new stSyncUpdateDB();
            SyncCookieDB = new stSyncUpdateDB();
            UserCapabilities = new PlayerCapability();
            Member = new PlayerMemberInfo();
            Statistics = new PlayerUserStatistics();
            Tutorial = new TutorialInfo();
            ChatMacro = new ChatMacroUser();
            UID = 0;
            for (sbyte i = 0; i < MS_NUM_MAPS; i++)
            {
                var map = new MapStatisticsEx();
                map.clear(i);
                NormalMapStatistics.Add(map);
                NormalMapStatisticsAll.Add(map);
                NaturalMapStatistics.Add(map);
                NaturalMapStatisticsAll.Add(map);
                GrandPrixMapStatistics.Add(map);
                GrandPrixMapStatisticsAll.Add(map);
            }

            // Inicializando cada sessão com 21 mapas (ou MS_NUM_MAPS mapas)
            for (int j = 0; j < 9; j++)
            {
                for (sbyte i = 0; i < MS_NUM_MAPS; i++)
                {
                    var map = new MapStatisticsEx();
                    map.clear(i);
                    AllSeasonsMapStatistics[j, i] = (map);  // Inicializa cada mapa
                }
            }

            CharacterLoungeStates = [];
            Friends = new Dictionary<uint, FriendInfo>();   // Friend List 
            Attendance = new AttendanceRewardInfoEx();
            Achievements = new AchievementManager();   // Manager Achievement 
            GrandPrixHistory = new List<GrandPrixClear>(); // Grand Prix Clear os grand prix que o player já jogou
            AssistFlag = false;
            Guild = new GuildInfoEx();
            DailyQuests = new DailyQuestInfoUser();
            GameHistory = new Last5PlayersGame();
            init(false);
        }

        private void init(bool _init = true)
        {
            // --- 1. RESET DE TIPOS PRIMITIVOS E STATUS (Onde os hackers costumam deixar lixo) ---
            this.Cookie = 0;
            this.PointShopWeb = 0;
            this.GrandZodiacPoints = 0;
            this.PointShopLegacy = 0;
            this.Place = -1;
            this.Lobby = 255;
            this.Channel = -1;
            this.WhisperState = 1;
            this.PostureRoom = 0;
            this.LoungeState = 0;
            this.ChatSpecial = new byte[12]; // Limpa o buffer de animação
            this.AssistFlag = false;
        }

        public bool Load()
        {
            try
            {
                // Carregamento de Dados Básicos
                Statistics = CommandDB.LoadUserInfo(UID);
                Cookie = CommandDB.LoadCookie(UID);
                MailBox = new(CommandDB.LoadMailBox(UID), UID);
                ChatMacro = CommandDB.LoadChatMacro(UID);
                Friends = CommandDB.LoadFriends(UID);
                Attendance = CommandDB.LoadAttendanceReward(UID);
                DailyQuests = CommandDB.LoadDailyQuest(UID);
                GameHistory = CommandDB.LoadLastPlayerGame(UID);

                // Pontos e Progressão
                GrandPrixHistory = CommandDB.LoadGrandPrixClear(UID);
                GrandZodiacPoints = CommandDB.LoadGrandZodiacPoints(UID);
                PointShopLegacy = CommandDB.LoadLegacyTikiShopInfo(UID);
                PointShopWeb = CommandDB.LoadWebPoints(UID);

                // Carregamento de Estatísticas de Mapas (Temporários)
                var normalStats = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_NORMAL);
                var normalStatsAll = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_NORMAL);

                var naturalStats = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_NATURAL);
                var naturalStatsAll = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_NATURAL);

                var gpStats = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX);
                var gpStatsAll = CommandDB.LoadMapStats(UID, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX);

                // Mapeamento das Estatísticas para os Arrays/Listas
                normalStats?.ForEach(s => NormalMapStatistics[s.course] = s);
                normalStatsAll?.ForEach(s => NormalMapStatisticsAll[s.course] = s);
                naturalStats?.ForEach(s => NaturalMapStatistics[s.course] = s);
                naturalStatsAll?.ForEach(s => NaturalMapStatisticsAll[s.course] = s);
                gpStats?.ForEach(s => GrandPrixMapStatistics[s.course] = s);
                gpStatsAll?.ForEach(s => GrandPrixMapStatisticsAll[s.course] = s);

                // Sistema de Achievements (Conquistas)
                bool hasAchievements = CommandDB.CheckAchievement(UID);
                if (!hasAchievements)
                {
                    Achievements.initAchievement(UID, true);
                }
                else
                {
                    var achievementData = CommandDB.LoadAchievementInfo(UID);
                    Achievements.initAchievement(UID, achievementData);
                }

                return true;
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[PlayerInfo::Load][Error] UID {UID}: {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }
        }

        public override void Clear()
        {
            init(false);
            AssistFlag = false;

            // --- 3. ESTATÍSTICAS DE MAPAS (CORREÇÃO CRÍTICA: Limpar antes de dar Add) ---
            NormalMapStatistics.Clear();
            NormalMapStatisticsAll.Clear();
            NaturalMapStatistics.Clear();
            NaturalMapStatisticsAll.Clear();
            GrandPrixMapStatistics.Clear();
            GrandPrixMapStatisticsAll.Clear();

            for (sbyte i = 0; i < MS_NUM_MAPS; i++)
            {
                var map = new MapStatisticsEx();
                map.clear(i);
                // Adicionando instâncias limpas
                NormalMapStatistics.Add(map);
                NormalMapStatisticsAll.Add(map);
                NaturalMapStatistics.Add(map);
                NaturalMapStatisticsAll.Add(map);
                GrandPrixMapStatistics.Add(map);
                GrandPrixMapStatisticsAll.Add(map);
            }

            // Inicializando Array Multidimensional [Season, Mapa]
            AllSeasonsMapStatistics = new MapStatistics[9, MS_NUM_MAPS];
            for (int j = 0; j < 9; j++)
            {
                for (sbyte i = 0; i < MS_NUM_MAPS; i++)
                {
                    var m = new MapStatisticsEx();
                    m.clear(i);
                    AllSeasonsMapStatistics[j, i] = m;
                }
            }

            CharacterLoungeStates?.Clear();

            Friends?.Clear(); // Friend List

            // Achievement e Cards
            Achievements?.clear();
            GrandPrixHistory?.Clear();

            // MailBox
            MailBox?.clear();
        }


        public StateCharacterLounge FindStateCharacterLounger(int id)
            => CharacterLoungeStates.FirstOrDefault(c => c.Key == id).Value;

        public int addExp(int expGain)
        {
            if (expGain <= 0) return 0;

            int levelsGained = 0;
            int totalExpBefore = Statistics.exp; // Para o log final

            try
            {
                // Trava para nível máximo (Índice 69 do seu array de 70 elementos)
                if (Level >= 69)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[AddExp][MaxLevel] Normal[UID={UID}] já é Level Máximo (70). Ignorando {expGain} EXP.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return -1;
                }

                // Soma a EXP ganha ao que o player já tinha
                Statistics.exp += expGain;
                byte oldLevel = (byte)Level;

                // Loop de processamento de Level Up
                while (Level < 69)
                {
                    // Pega quanto custa para sair do nível atual (Ex: Level 0 precisa de 30)
                    int costToLevelUp = Convert.ToInt32(ExpByLevel[(byte)Level]);

                    // Se encontrar o 0 no final do seu array, para.
                    if (costToLevelUp <= 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[AddExp][Info] Normal[UID={UID}] atingiu o limite da tabela de EXP no Level {Level}.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }

                    // Verifica se a EXP acumulada paga o próximo nível
                    if (Statistics.exp >= costToLevelUp)
                    {
                        Statistics.exp -= costToLevelUp; // Subtrai o custo (Ex: 32 - 30 = 2)
                        Level++;                       // Sobe o nível
                        levelsGained++;

                        // Atualiza as estruturas
                        Member.GameLevel = (byte)Level;
                        Statistics.level = (byte)Level;
                    }
                    else
                    {
                        // Se a sobra (Ex: 2) for menor que o custo do próximo Level (Ex: 40), para aqui.
                        break;
                    }
                }

                // Envia para o Banco de Dados
                NormalManagerDB.Instance.add(
                    3, new CmdUpdateLevelAndExp(UID, (byte)Level, Statistics.exp),
                    SQLDBResponse,
                    this
                );
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[AddExp][CriticalError] Normal[UID={UID}]: {e.Message} | Stack: {e.StackTrace}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return levelsGained;
        }

        public void addGrandZodiacPontos(ulong _pontos)
        {
            if (_pontos < 0)
                throw new exception("[PlayerInfo::addGrandZodiacPontos][Error] invalid _pontos(" + _pontos + "), ele é negativo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 101, 0));

            GrandZodiacPoints += _pontos;

            // Update no Banco de dados
            NormalManagerDB.Instance.add(8, new CmdGrandZodiacPontos(UID, (uint)GrandZodiacPoints, CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_UPDATE), SQLDBResponse, this);
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
                    throw new exception("[PlayerInfo::consomeCookie][Error] Normal[UID=" + (UID) + "] cookie on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

                if ((Cookie - _cookie) < 0)
                    throw new exception("[PlayerInfo::consomeCookie][Error] O Normal[UID=" + (UID) + "] nao tem cookies suficiente para consumir", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 20, 0));

                Cookie -= _cookie;

                SyncCookieDB.requestUpdateOnDB();

                NormalManagerDB.Instance.add(2, new CmdUpdateCookie(UID, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.DECREASE), SQLDBResponse, this);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::consomeCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void consomePang(ulong _pang)
        {

            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::consomePang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on Pang of DB
                if (checkAlterationPangOnDB())
                    throw new exception("[PlayerInfo::consomePang][Error] Normal[UID=" + (UID) + "] Pang on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));
                // 3. CORREÇÃO AQUI: Comparação direta antes da subtração
                if (Statistics.pang < _pang)
                {
                    throw new exception($"[PlayerInfo::consomePang] Normal[UID={UID}] saldo insuficiente (Saldo: {Statistics.pang}, Custo: {_pang})",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 20, 0));
                }

                Statistics.pang -= _pang;

                SyncPangDB.requestUpdateOnDB();

                NormalManagerDB.Instance.add(1, new CmdUpdatePang(UID, _pang, CmdUpdatePang.T_UPDATE_PANG.DECREASE), SQLDBResponse, this);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::consomePang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                    throw new exception("[PlayerInfo::addCookie][Error] Normal[UID=" + (UID) + "] cookie on db is different of server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

                Cookie += _cookie;

                SyncCookieDB.requestUpdateOnDB();

                NormalManagerDB.Instance.add(2, new CmdUpdateCookie(UID, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.INCREASE), SQLDBResponse, this);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::addCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void addPang(ulong _pang)
        {

            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::addPang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            try
            {

                // Check alteration on Pang of DB 
                if (checkAlterationPangOnDB())
                {
                    var old_pang = Statistics.pang;

                    // Atualiza o valor do Pang do server com o do banco de dados
                    updatePang();
                }

                // Add o Pang para o player
                Statistics.pang += _pang;

                SyncPangDB.requestUpdateOnDB();

                NormalManagerDB.Instance.add(1, new CmdUpdatePang(UID, _pang, CmdUpdatePang.T_UPDATE_PANG.INCREASE), SQLDBResponse, this);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::addPang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                var cmd_cp = new CmdCookie(UID);    // Waiter

                NormalManagerDB.Instance.add(0, cmd_cp);

                if (cmd_cp.getException().getCodeError() != 0)
                    throw cmd_cp.getException();

                Cookie = cmd_cp.getCookie();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::updateCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relanção por que essa função não tem retorno para verifica, então a exception garante que o código não vai continua
                throw;
            }
        }

        public void updatePang()
        {
            try
            {

                var cmd_pang = new CmdPang(UID);    // Waiter

                NormalManagerDB.Instance.add(0, cmd_pang);

                if (cmd_pang.getException().getCodeError() != 0)
                    throw cmd_pang.getException();

                Statistics.pang = cmd_pang.getPang();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::updatePang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relanção por que essa função não tem retorno para verifica, então a exception garante que o código não vai continua
                throw;
            }
        }

        // Adiciona Pang Estático
        public static void addPang(uint _uid, ulong _pang)
        {
            if ((long)_pang <= 0)
                throw new exception("[PlayerInfo::addPang][Error] _pang valor invalido: " + ((long)_pang), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            NormalManagerDB.Instance.add(1, new CmdUpdatePang(_uid, _pang, CmdUpdatePang.T_UPDATE_PANG.INCREASE), SQLDBResponse, null);
        }

        // Adiciona Cookie Point(CP) Estático
        public static void addCookie(uint _uid, ulong _cookie)
        {

            if ((long)_cookie <= 0)
                throw new exception("[PlayerInfo::addCookie][Error] _cookie valor invalido: " + ((long)_cookie), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 21, 0));

            NormalManagerDB.Instance.add(2, new CmdUpdateCookie(_uid, _cookie, CmdUpdateCookie.T_UPDATE_COOKIE.INCREASE), SQLDBResponse, null);
        }

        public void addUserInfo(PlayerUserStatistics _ui, ulong _total_pang_win_game = 0)
        {
            Statistics.add(_ui, (uint)_total_pang_win_game);

            // Update User Info ON DB
            updateUserInfo();
        }

        public bool checkAlterationCookieOnDB()
        {
            var cmd_cp = new CmdCookie(UID);    // Waiter

            NormalManagerDB.Instance.add(0, cmd_cp);

            if (cmd_cp.getException().getCodeError() != 0)
                throw cmd_cp.getException();

            return (cmd_cp.getCookie() != Cookie);
        }

        public bool checkAlterationPangOnDB()
        {
            var cmd_pang = new CmdPang(UID);    // Waiter

            NormalManagerDB.Instance.add(0, cmd_pang);

            if (cmd_pang.getException().getCodeError() != 0)
                throw cmd_pang.getException();

            return (cmd_pang.getPang() != Statistics.pang);
        }

        public int getSizeCupGrandZodiac()
        {
            int size_cup = 1;

            if (GrandZodiacPoints < 300)
                size_cup = 9;
            else if (GrandZodiacPoints < 600)
                size_cup = 8;
            else if (GrandZodiacPoints < 1200)
                size_cup = 7;
            else if (GrandZodiacPoints < 1800)
                size_cup = 6;
            else if (GrandZodiacPoints < 4000)
                size_cup = 5;
            else if (GrandZodiacPoints < 5200)
                size_cup = 4;
            else if (GrandZodiacPoints < 7600)
                size_cup = 3;
            else if (GrandZodiacPoints < 10000)
                size_cup = 2;

            return size_cup;
        }


        public int getSumRecordGrandPrix()
        {
            int grand_prix_record_sum = 0;

            foreach (var el in GrandPrixMapStatistics)
                if (el.isRecorded())
                    grand_prix_record_sum += el.best_score;

            return grand_prix_record_sum;
        }


        public bool isFriend(int _uid)
        {
            if (_uid == 0u)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::isFriend][Error] _uid is invalid(0)", 0));

                return false;
            }
            return Friends.ContainsKey((uint)_uid);
        }

        public bool isMasterCourse()
        {
            sbyte[] clear_course = new sbyte[MS_NUM_MAPS];

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(NormalMapStatistics[i].isRecorded() ? 1 : 0);

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(NaturalMapStatistics[i].isRecorded() ? 1 : 0);

            for (int i = 0; i < MS_NUM_MAPS; ++i)
                clear_course[i] |= (sbyte)(GrandPrixMapStatistics[i].isRecorded() ? 1 : 0);

            // Conta quantos mapas foram completados (el == 1)
            int count = clear_course.Count(el => el == 1);
            // Deve ter completado todos menos os dois mapas excluídos
            return count == (MS_NUM_MAPS - 2)/*-2 por que tira o map 12 que nunca foi feito e o 17 que é o SSC*/;

        }


        public bool updateGrandPrixClear(uint _typeid, int _position)
        {

            if (_typeid == 0)
                throw new exception("[PlayerInfo::updateGrandPrixClear][Error] invliad _typeid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 100, 0));

            bool uptClient = false;

            // Procura se já existe o GP na lista
            var gp = GrandPrixHistory.Find(el => el._typeid == _typeid);

            if (gp == null)
            {
                // Não tem esse GP, acabou de completar um novo
                gp = new GrandPrixClear(_typeid, _position);

                GrandPrixHistory.Add(gp);

                // Insere no banco de dados
                NormalManagerDB.Instance.add(6, new CmdInsertGrandPrixClear(UID, gp), SQLDBResponse, this);

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
                    NormalManagerDB.Instance.add(7, new CmdUpdateGrandPrixClear(UID, gp), SQLDBResponse, this);

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

                LocationDB.channel = Channel;
                LocationDB.lobby = Lobby;
                LocationDB.room = Member.RoomID;
                LocationDB.place.ulPlace = (byte)Place;

                //// Sincroniza para não ter valores inseridos errados no banco de dados
                LocationDB.requestUpdateOnDB();

                NormalManagerDB.Instance.add(5, new CmdUpdatePlayerLocation(UID, LocationDB), SQLDBResponse, this);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::updateLocationDB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void updateMedal(uMedalWin _medal_win)
        {
            if (_medal_win.ucMedal == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] Normal[UID=" + UID
                        + "] tentou atualizar medalhas, mas passou nenhuma medalha para atualizar. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 600, 0));

            // Update medal info player
            Statistics.medal.add(_medal_win);

            // Update Info do player na database
            updateUserInfo();
        }

        // Update Medal Estático
        public static void updateMedal(uint _uid, uMedalWin _medal_win)
        {
            if (_uid == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] Normal[UID=" + (_uid) + "] tentou atualizar medalhas, mas o UID do player é invalido(zero). Hacker ou Bug.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 601, 0));

            if (_medal_win.ucMedal == 0u)
                throw new exception("[PlayerInfo::updateMedal][Error] Normal[UID=" + (_uid)
                        + "] tentou atualizar medalhas, mas passou nenhuma medalha para atualizar. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 600, 0));

            // Pega o Info do player para atualizar
            var cmd_ui = new CmdUserInfo(_uid);     // Waiter

            NormalManagerDB.Instance.add(0, cmd_ui);

            if (cmd_ui.getException().getCodeError() != 0)
                throw cmd_ui.getException();

            var user_info = cmd_ui.getInfo();

            // Update medal info player
            user_info.medal.add(_medal_win);

            // Update Info do player na database
            updateUserInfo(_uid, user_info);
        }


        public void updateUserInfo()
        {
            NormalManagerDB.Instance.add(3, new CmdUpdateUserInfo(UID, Statistics), SQLDBResponse, this);
        }

        // Update User Info ON DB Estático 
        public static void updateUserInfo(uint _uid, PlayerUserStatistics _ui)
        {
            if (_uid == 0)
                throw new exception("[PlayerInfo::updateUserInfo][Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 300, 0));

            NormalManagerDB.Instance.add(3, new CmdUpdateUserInfo(_uid, _ui), SQLDBResponse, null);
        }


        /// <summary>
        /// Size = 263 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetLoginInfo()
        {
            return Member.ToArray(IncludeRoomID :true);
        }

        /// <summary>
        /// Size = 235 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetUserStatisticInfo()
        {
            return Statistics.ToArray();
        }

        /// <summary>
        /// Normal = 903,
        /// Natural = 903,                           
        /// </summary>
        /// <returns>Bytes Write -> 1806 Size</returns>
        public byte[] GetMapStatisticInfo()
        {
            using var p = new Packet();
            for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                p.WriteBytes(NormalMapStatistics[st_i].ToArray());

            // Map Statistics Natural
            for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                p.WriteBytes(NaturalMapStatistics[st_i].ToArray());

            // Map Statistics Grand Prix
            for (byte st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                p.WriteBytes(GrandPrixMapStatistics[st_i].ToArray());

            // Map Statistics Normal for all seasons
            for (int j = 0; j < 9; j++)
                for (var st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                    p.WriteBytes(AllSeasonsMapStatistics[j, st_i].ToArray());

            return p.GetBytes;
        }


        public static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::SQLDBResponse][Warning] _arg is null na msg_id = " + (_msg_id), 0));
                return;
            }

            try
            {
                var pi = (PlayerInfo)_arg;


                // Por Hora só sai, depois faço outro Type de tratamento se precisar
                if (_pangya_db.getException().getCodeError() != 0)
                {

                    // Trata alguns Type aqui, que são necessários
                    switch (_msg_id)
                    {
                        case 1: // Update Pang
                            {
                                // Error at update on DB
                                pi.SyncPangDB.errorUpdateOnDB();

                                break;
                            }
                        case 2: // Update Cookie
                            {
                                // Error at update on DB
                                pi.SyncCookieDB.errorUpdateOnDB();

                                break;
                            }
                        case 5: // Update Location Player on DB
                            {
                                // Error at update on DB
                                pi.LocationDB.errorUpdateOnDB();

                                break;
                            }
                    }

                    _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), 0));

                    return;
                }

                switch (_msg_id)
                {
                    case 1: // UPDATE Pang
                        {

                            // Success update on DB
                            pi.SyncPangDB.confirmUpdateOnDB();
                            break;
                        }
                    case 2: // UPDATE cookie
                        {

                            // Success update on DB
                            pi.SyncCookieDB.confirmUpdateOnDB();
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
                            pi.LocationDB.confirmUpdateOnDB();

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
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::SQLDBResponse][Error] QUERY_MSG[ID=" + (_msg_id)
                        + "]" + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }  
    }
}
