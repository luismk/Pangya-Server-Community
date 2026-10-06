using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Roms.GameModes
{
    public class GrandZodiacPractice : GrandZodiacBase
    {
        #region Properties & Fields
        private bool _initGrandZodiacState;
        #endregion

        #region Constructor & Destructor
        public GrandZodiacPractice(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue)
            : base(players, roomInfo, rateValue)
        {
            _initGrandZodiacState = false;

            // Inicializa conquistas específicas deste HoleMode
            InitAllAchievementPlayers(0x6C40003Fu);

            State = InitRoomGame();
        }

        ~GrandZodiacPractice()
        {
            Dispose(false);
        }
        #endregion

        #region Core Game Flow (Overrides)
        public override bool InitRoomGame()
        {
            if (Players.Count > 0)
            {
                InitGameTime();
                GameInitState = 1; // Iniciado
                _initGrandZodiacState = true;
            }

            return true;
        }

        public override void ChangeHole(Player session)
        {
            try
            {
                NextHole(session);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacPractice::ChangeHole][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void FinishHole(Player session)
        {
            RequestFinishHole(session, 0);
        }

        public override void UpdateFinishHole(Player session, int option)
        {
            try
            {
                var pgi = InitPlayerInfo("updateFinishHole", "tentou atualizar o finish hole do grand zodiac", session);

                // Notifica a localização do player para os outros na sala
                var p = new Packet(0x1EE);
                p.WriteInt32(session.ConnectionID);
                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.z);
                SendBroadCast(p);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacPractice::UpdateFinishHole][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        #endregion

        #region Time Management
        public override void GameTimeIsOver()
        {
            if (GameInitState == 1 && Players.Count > 0)
            {
                foreach (var entry in PlayerInfo)
                {
                    var session = entry.Key;
                    var pgi = entry.Value;

                    if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && session.Connected)
                    {
                        // Envia pacote notificando que o tempo acabou
                        var p = new Packet(0x8D);
                        p.WriteUInt32(RoomInfo.TimeMin);
                        session.Send(p);
                    }
                }
            }
        }
        #endregion

        #region Finish Game Logic
        public override bool FinishGame(Player session, int option)
        {
            if (Players.Count > 0)
            {
                if (option == 0x12C || option == 2)
                {
                    bool isHackerOrBug = false;

                    if (Timer != null)
                    {
                        // Validação de integridade de tempo
                        isHackerOrBug = ((int)(RoomInfo.TimeMin - Timer.getElapsed()) / 60000) >= 1;

                        if (isHackerOrBug && option == 0x12C)
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacPractice::FinishGame][Warning] Normal[UID={session.UserInfo.UID}] Sala[{RoomInfo.RoomID}] Tempo inconsistente. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    if (_initGrandZodiacState)
                    {
                        ExecuteFinishGrandZodiacPractice(session, (option == 0x12C && !isHackerOrBug) ? 0 : 1);
                    }
                }
            }

            return (PlayersCompleteGameAndClear() && _initGrandZodiacState);
        }

        public void ExecuteFinishGrandZodiacPractice(Player session, int option)
        {
            if (Players.Count > 0 && GameInitState == 1)
            {
                var pgi = InitPlayerInfo("finish_gz_practice", "tentou terminar o HoleMode Practice", session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {
                    UpdatePlayerAssist(session);
                }

                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);
                pgi.time_finish.CreateTime();

                SetEndGame(pgi);
                SetFinishGameFlag(pgi, 1);

                // Envia pacote de fim de jogo individual 0x1F2 
                session.Send(new Packet(0x1F2));
                if (AllCompleteGameAndClear() && GameInitState == 1)
                {
                    FinalizeRoom(option);
                }
            }
        }

        private void FinalizeRoom(int option)
        {
            GameInitState = 2; // Estado: Finalizado

            CalculeRankPlace();
            CalculateAndDistributeExp();

            foreach (var player in Players)
            {
                var pgi = InitPlayerInfo("finish", "finalizando dados da sala", player);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    ProcessRequestFinishData(player);
                }
            }
        }
        #endregion

        #region Data Processing (Rewards & Save)
        private void CalculateAndDistributeExp()
        {
            foreach (var el in PlayerInfo)
            {
                if (el.Value == null) continue;

                int exp = 0;
                var pgi = el.Value;

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                {
                    exp = 15;
                    exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(pgi.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                }
                else if (pgi.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                {
                    var gzInfo = (PlayerGrandZodiacInfo)pgi;
                    exp = (int)(gzInfo.m_gz.hole_in_one / 2);
                    exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(pgi.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                }

                // Se não for Level máximo, limpa para recalcular no addExp
                if (pgi.level < 70) pgi.data.exp = exp;
            }
        }

        public void ProcessRequestFinishData(Player player)
        {
            try
            {
                var pgi = InitPlayerInfo("requestFinishData", "finalizando dados do player", player);

                // Salva informações no Banco de Dados
                RequestSaveInfo(player, 0);
                RequestUpdateItemUsedGame(player);
                RequestFinishItemUsedGame(player);
                RequestSaveDrop(player);

                SendTimeIsOver(player);
                SendPlacar(player);

                // Packets de sincronização de moeda e estado (0xC8, 0xA7, 0xAA)
                SendCurrencyUpdates(player);

                // Atualiza Mascote
                if (player.Inventory.UserEquippedItem.MascotEquiped?.id > 0)
                {
                   player.Send(Handle_PACKET_RESPONSE.pacote06B(player.Inventory, 8));
                }

                pgi.sys_achieve.finish_and_update(player);

                // Packet específico 0x24F (JP Server)
                var packet = new Packet(0x24F);
                packet.WriteUInt32(0);
                player.Send(packet);

                if (pgi.data.exp > 0)
                {
                    player.addExp(pgi.data.exp, true);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacPractice::ProcessRequestFinishData][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void SendCurrencyUpdates(Player player)
        {
            // Update Pang 0xC8
            var packet = new Packet(0xC8);
            packet.WriteUInt64(player.UserInfo.Statistics.pang);
            packet.WriteUInt64(0);
            player.Send(packet);

            // 0xA7
            packet.init_plain(0xA7);
            packet.WriteByte(0);
            player.Send(packet);

            // 0xAA
            packet.init_plain(0xAA);
            packet.WriteUInt16(0);
            packet.WriteUInt64(player.UserInfo.Statistics.pang);
            packet.WriteUInt64(player.UserInfo.Cookie);
            player.Send(packet);
        }
        #endregion

        #region Unused Overrides
        public override void EndGoldenBeam() { }
        public override void StartGoldenBeam() { }
        public override void DrawDropItem(Player session) { }
        #endregion

        #region Cleanup
        public override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _initGrandZodiacState = false;

                if (GameInitState != 2)
                {
                    FinalizeRoom(2);
                }

                // Aguarda com timeout máximo para evitar bloqueio infinito no Dispose.
                int maxWaitMs = 5000;
                int waited = 0;
                while (!PlayersCompleteGameAndClear() && waited < maxWaitMs)
                {
                    Task.Delay(500).Wait();
                    waited += 500;
                }

                DeleteAllPlayer();
                LogDestruction();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}