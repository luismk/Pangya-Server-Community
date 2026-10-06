using Microsoft.VisualBasic.FileIO;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Roms.GameModes
{
    public class GrandZodiac : GrandZodiacBase
    {
        #region Properties & Fields
        private bool _initGrandZodiacState;
        #endregion

        #region Constructor & Destructor
        public GrandZodiac(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue)
            : base(players, roomInfo, rateValue)
        {
            _initGrandZodiacState = false;

            // Inicializa conquistas específicas deste HoleMode
            InitAllAchievementPlayers(0x6C40003Cu /*/ *Grand Zodiac * /*/);

            State = InitRoomGame();
        }

        ~GrandZodiac()
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
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiac::ChangeHole][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                if (GetTipo() == RoomTypeFlags.GRAND_ZODIAC_INT)
                {
                    SendBroadCast(p);
                }
                else
                {
                    session.Send(p);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiac::UpdateFinishHole][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                            _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiac::FinishGame][Warning] Normal[UID={session.UserInfo.UID}] Sala[{RoomInfo.RoomID}] Tempo inconsistente. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    if (_initGrandZodiacState)
                    {
                        ExecuteFinishGrandZodiac(session, (option == 0x12C && !isHackerOrBug) ? 0 : 1);
                    }
                }
            }

            return (PlayersCompleteGameAndClear() && _initGrandZodiacState);
        }

        public void ExecuteFinishGrandZodiac(Player session, int option)
        {
            if (Players.Count > 0 && GameInitState == 1)
            {
                var pgi = InitPlayerInfo("ExecuteFinishGrandZodiac", "tentou terminar o Grand Zodiac no jogo", session);

                if (pgi != null && pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    CalculePang(session);

                    // Atualizar os Pang do player se ele estiver com assist ligado, e for maior que beginner E
                    UpdatePlayerAssist(session);

                    if (GameInitState == 1 && option == 0)
                    { 
                        // Achievement Counter
                        pgi.sys_achieve.incrementCounter(0x6C400004u); 
                    }
                }

                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                // Terminou o jogo no Grand Zodiac
                SetEndGame(pgi);

                SetFinishGameFlag(pgi, 1);

                // End Game
                session.Send(new Packet(0x1F2));

                if (AllCompleteGameAndClear() && GameInitState == 1)
                {
                    FinalizeRoom(option);// Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }

        private void FinalizeRoom(int option)
        {
            GameInitState = 2; // Estado: Finalizado

            CalculeRankPlace();
            MakeTrofel();
            CalculePoints();
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

        public void MakeTrofel()
        { 
            // Trofe  Grand Zodiac
            uint trofeu_base = 0x2D0A6200;
            uint qntd = 0;

            var players_num = PlayerInfo.Count;

            if (players_num >= 20u && players_num < 30u) // Bronza
                qntd = 1u;
            else if (players_num >= 30 && players_num < 50) // Silver and Bronza
                qntd = 2u;
            else if (players_num >= 50) // Gold, Silver and Bronze
                qntd = 3u; 

            trofeu_base = trofeu_base + ((3 - qntd) << 8);

            PlayerGrandZodiacInfo pgzi = null;

            foreach (var el in PlayerInfo.ToList())
            { 
                if ((pgzi = (PlayerGrandZodiacInfo)el.Value) != null && pgzi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                { 
                    if (pgzi.m_gz.position <= qntd)
                    {
                        pgzi.m_gz.trofeu = trofeu_base + ((pgzi.m_gz.position - 1) << 8);
                    }
                }
            }
        }

        public void CalculePoints()
        {

            PlayerGrandZodiacInfo pgzi = null;

            float pontos_base = (GetTipo() == RoomTypeFlags.GRAND_ZODIAC_INT ? 3.5f : 5.0f);

            foreach (var el in PlayerInfo)
            { 
                if ((pgzi = (PlayerGrandZodiacInfo)(el.Value)) != null && pgzi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    pgzi.m_gz.pontos = (uint)(pgzi.m_gz.total_score * pontos_base);
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
                var pgi = (PlayerGrandZodiacInfo)el.Value;


                if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                {

                    if ((FindSessionByUID(pgi.uid)) != null)
                    {

                        exp = (int)(45.0f * ((121 - pgi.m_gz.position) / 100.0f));
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp)); 
                    }

                }
                else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                {

                    exp = (int)(pgi.m_gz.hole_in_one / 2);
                    exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp)); 
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
                // Atualiza itens usados no Grand Zodiac
                RequestUpdateItemUsedGame(player);
                // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
                RequestFinishItemUsedGame(player);
                // Salva pontos do Grand Zodiac ganho
                if (pgi.m_gz.pontos > 0)
                {
                    player.UserInfo.addGrandZodiacPontos(pgi.m_gz.pontos);
                }
                // Salve trofe 
                SendTrophy(player); 
                 
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
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiac::ProcessRequestFinishData][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void SendTrophy(Player _session)
        {
            var pgi = InitPlayerInfo("SendTrophy", "tentou enviar o trofeu do player no jogo",  _session);

            if (pgi.m_gz.trofeu > 0)
            {

                stItem item = new stItem(); 
                item.type = 2;
                item.id = -1;
                item._typeid = pgi.m_gz.trofeu;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                // Update on Server and Database
                if (ItemManager.addItem(item, _session, 0, 0) >= RetAddItem.SUCCESS)
                {

                    // Adicionou o Trof u com sucesso para o player
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::sendTrofel][Log] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] ganhou Grand Zodiac Trofeu[TYPEID=" + Convert.ToString(pgi.m_gz.trofeu) + "] na Posicao[RANK=" + Convert.ToString(pgi.m_gz.position) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Update Trof u on Game
                    var p = new Packet(0x1FA); 
                    p.WriteUInt32(item._typeid); 
                    p.WriteInt32(item.id);
                    _session.Send(p);

                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::sendTrofel][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou adicionar Grand Zodiac Trofeu[TYPEID=" + Convert.ToString(item._typeid) + "] na Posicao[RANK=" + Convert.ToString(pgi.m_gz.position) + "], mas nao conseguiu adicionar o item.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
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
        public override void EndGoldenBeam() 
        {
            try
            {

                // Acabou o tempo do golden beam time
                Interlocked.Exchange(ref StateGoldenBeam, 0);

                // Golden Beam End
                SendBroadCast(new Packet((ushort)0x1F1));
                if (PlayersGoldenBeam.Count > 0)
                {

                    ulong jackpot = (ulong)(Players.Count * (Random.Shared.Next(1, 5) * 500)); // rand de 500 a 2500 por player

                    var seed = Random.Shared.Next(0, 1);

                    if (PlayersGoldenBeam.Count == 1 || seed == 0)
                    {

                        int index = Random.Shared.Next(0, PlayersGoldenBeam.Count); // até Count - 1 incluso

                        var it = PlayersGoldenBeam.ElementAt(index);

                        var pgi = InitPlayerInfo("endGoldenBeam",
                            "tentou enviar o prensete do Golden Beam",
                            it.session);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                        { 
                            // Log
                            _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::endGoldenBeam][Log] Normal[UID=" + Convert.ToString(it.session.UserInfo.UID) + "] ganhou jackpot(" + Convert.ToString(jackpot) + ") sozinho.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            pgi.m_gz.jackpot = jackpot;

                            var p = new Packet(0x40); 
                            p.WriteByte(13); // 1 Ganhou sozinho o jackpot no Grand Zodiac 
                            p.WriteString(it.session.UserInfo.NickName);
                            p.WriteUInt16(0); // Msg empty 
                            p.WriteUInt32(0x1A000010); // Jackpot Pangs Pouch
                            p.WriteUInt64(jackpot);
                            SendBroadCast(p); 
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::endGoldenBeam][Log][Warning] Normal[UID=" + Convert.ToString(pgi.uid) + "] ganhou jackpot, mas ele nao esta mais no jogo, para receber o jackpot.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    else if (seed == 1)
                    {

                        var equal_jackpot = Convert.ToUInt32(jackpot) / PlayersGoldenBeam.Count;

                        foreach (var el in PlayersGoldenBeam)
                        {

                            var pgi = InitPlayerInfo("endGoldenBeam",
                                "tentou enviar o presente do Golden Beam",
                                el.session);

                            if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                            {

                                // Log
                                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::endGoldenBeam][Log] Normal[UID=" + Convert.ToString(el.session.UserInfo.UID) + "] ganhou jackpot(" + Convert.ToString(jackpot) + ") igual ao de todo mundo.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                pgi.m_gz.jackpot = (ulong)equal_jackpot;

                                var p = new Packet(0x40);
                                p.WriteByte(14); // Todos que fizeram hio no golden beam garanham o jackpot 
                                p.WriteString(el.session.UserInfo.NickName);
                                p.WriteUInt16(0); // Msg Empty 
                                p.WriteUInt32(0x1A000010); // Jackpot Pangs Pouch
                                p.WriteInt64(equal_jackpot);
                                el.session.Send(p); 
                            }
                            else
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::endGoldenBeam][Log][Warning] Normal[UID=" + Convert.ToString(pgi.uid) + "] ganhou jackpot, mas ele nao esta mais no jogo, para receber o jackpot.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::endGoldenBeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
       
        public override void StartGoldenBeam()
        {
            Interlocked.Exchange(ref StateGoldenBeam, 1); 
            SendBroadCast(new Packet((ushort)0x1F0));
        }

        public override void DrawDropItem(Player session)
        {
            try
            {

                var pgi = InitPlayerInfo("drawDropItem",
                    "tentou sortear item drop para o jogador no jogo",
                    session);

                if (pgi.shot_sync.state_shot.display.acerto_hole)
                {

                    var seed = Random.Shared.Next(1, 10000);

                    if (seed > 9000)
                    {  
                        // 0x1800002C - Silent Nerver Stabilizer
                        // 0x1800002D - Safe Silent
                        DropItem di = new();

                        di.numero_hole = 1;
                        di.course = GetMap();

                        seed = Random.Shared.Next(0, 1);

                        di._typeid = 0x1800002C + (uint)seed;
                        di.qntd = (short)((seed == 0) ? 5 : 3);
                        di.type = DropItem.eTYPE.NORMAL_QNTD;

                        // Add Droped item to pgi player
                        pgi.drop_list.v_drop.Add(di);

                        // Update item game, show msg
                        var p = new Packet(0x40); 
                        p.WriteByte(15); // Dropou item 
                        p.WriteString(session.UserInfo.NickName);
                        p.WriteUInt16(0); // Message empty 
                        p.WriteUInt32(di._typeid);
                        p.WriteUInt32((uint)di.qntd); 
                        // Envia para todos
                        SendBroadCast(p);

                    }
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiac::drawDropItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
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
