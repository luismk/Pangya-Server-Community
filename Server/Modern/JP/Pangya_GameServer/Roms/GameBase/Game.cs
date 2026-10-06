using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Roms.GameBase
{
    public abstract partial class Game : IDisposable
    {
        #region Enums

        public enum GameStateFlag
        {
            Default = -1,
            Init = 1,
            GameFinish = 2
        }

        #endregion

        #region Fields

        protected List<Player> Players { get; }
        protected Dictionary<Player, PlayerGameInfo> PlayerInfo { get; }
        protected List<PlayerGameInfo> PlayerOrder { get; }
        protected Dictionary<uint, uint> PlayerReportGame { get; }
        protected GameRoomInfoModel RoomInfo { get; }
        protected RateValue RateValue { get; }

        /// <summary>
        /// Game initialization StateRoom: 1 = started, 2 = finished, -1 = default
        /// </summary>
        public int GameInitState { get; set; } = (int)GameStateFlag.Default;

        protected bool State { get; set; }
        protected DateTime StartTime { get; set; }
        protected PangyaSyncTimer? Timer { get; set; }
        protected bool ChannelRookie { get; set; }
        /// <summary>
        /// Sync ServerFlag for initial data send. Must remain a field (not a property) to work with Interlocked operations.
        /// </summary>
        protected volatile int SyncSendInitData;
        protected CourseManager? Course { get; set; }
        public RoomInfoLog? RoomLog { get; set; }

        private bool _disposed;

        public bool Disposed => _disposed;
        #endregion

        #region Constructor

        public Game(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue)
        {
            Players = players;
            RoomInfo = roomInfo;
            RateValue = rateValue;
            ChannelRookie = roomInfo.IsChannelRookie;
            StartTime = DateTime.MinValue;
            PlayerInfo = new Dictionary<Player, PlayerGameInfo>();
            Course = null;
            GameInitState = (int)GameStateFlag.Default;
            State = false;
            PlayerOrder = new List<PlayerGameInfo>();
            Timer = null;
            PlayerReportGame = new Dictionary<uint, uint>();
            SyncSendInitData = 0;

            InitializeGame();
        }

        private void InitializeGame()
        {
            LoadMapSystem();
            CreateCourse();
        }

        private void LoadMapSystem()
        {
            var mapSystem = MapSystem.Instance;
            if (!mapSystem.isLoad())
            {
                mapSystem.load();
            }

            var map = mapSystem.getMap((byte)((int)RoomInfo.CourseIndex & 0x7F));

            if (map == null)
            {
                var errorMsg = $"[{GetType().Name}::LoadMapSystem][Error][Warning] Could not load map data for CourseIndex[COURSE={Convert.ToString((ushort)((int)RoomInfo.GetMap()))}]";
                _smp.LogManager.Instance.push(new AppMessage(errorMsg, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void CreateCourse()
        {
            var mapSystem = MapSystem.Instance;
            var map = mapSystem.getMap((byte)((int)RoomInfo.CourseIndex & 0x7F));

            Course = new CourseManager(
                RoomInfo,
                ChannelRookie,
                map?.star ?? 1.0f,
                RateValue.rain,
                RateValue.persist_rain);
        }


        public void UpdateTreasureHunterSystem()
        {

            // Atualiza Treasure Hunter System Course

            if (!sTreasureHunterSystem.Instance.isLoad())
            {
                sTreasureHunterSystem.Instance.load();
            }


            var course = sTreasureHunterSystem.Instance.findCourse((byte)(RoomInfo.GetMap() & 0x7F));

            if (course == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Game::UpdateTreasureHunterSystem][Error] tentou pegar o CourseIndex do Treasure Hunter System, mas o CourseIndex[COURSE=" + Convert.ToString((ushort)((byte)(RoomInfo.CourseIndex & RoomCourseFlags.UNK))) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.Instance.UpdateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            } 
        }

        public void InitAllAchievementPlayers(uint TypeID)
        { 
            foreach (var el in Players)
            {

                InitPlayerInfo($"{GetType().Name}", "tentou inicializar o counter item do Tourney",  el, out PlayerGameInfo pgi);

                InitAchievement(el);

                pgi.sys_achieve.incrementCounter(TypeID);
            }
        }
        #endregion

        #region Methods
        public GameRoomInfoModel GetRoomInfo() => RoomInfo;
        public byte GetMap() => RoomInfo?.GetMap() ?? 0x7F;

        public RoomTypeFlags GetTipo() => RoomInfo?.GetRoomType() ?? (RoomTypeFlags)255;
        public short GetRoomId() => RoomInfo?.RoomID ?? -1;

        protected void LogDestruction()
        {
            string className = GetType().Name;
            int roomNum = RoomInfo?.RoomID ?? -1;
            string fullMsg = $"[{className}::Destruction][Warning] Destroyed on Room[Number={roomNum}]";
            _smp.LogManager.Instance.push(new AppMessage(fullMsg, type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        #endregion 

        #region Disposal

        /// <summary>
        /// Releases all resources used by this game instance.
        /// This method must be called last in derived classes to avoid data conflicts.
        /// </summary>
        /// <param Name="disposing">True if called from Dispose(), false if called from finalizer</param>
        public virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Course?.Dispose();
                PlayerOrder.Clear();
                ClearAllPlayerInfo();
                ClearGameTime();

                if (PlayerReportGame.Count > 0)
                    PlayerReportGame.Clear();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
