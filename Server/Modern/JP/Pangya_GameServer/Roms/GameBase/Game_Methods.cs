using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;

using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms.GameBase
{
    public abstract partial class Game
    {
        /// <summary> Para o jogo. </summary>
        public bool GameStop()
        {
            // Garantir que qualquer exception derrube o server
            try
            { 
                if (Timer != null)
                   GameServer.Instance.DeleteTimer(Timer);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::GameStop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            Timer = null;

            return true;
        }
        /// <summary> Verifica se o jogo está pausado. </summary>
        public bool GamePause()
        {

            if (Timer != null)
            {
                Timer.Pause(); 

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::pauseTime][Log] pausou o Timer[Tempo=" + Timer.getTimeLog() + "" + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                 
                return true;
            }

            return false;
        }
        /// <summary> Verifica se o jogo terminou. </summary>
        public bool GameFinish()
        {
            if (Timer != null)
                return Timer.getState() == PangyaSyncTimer.TIMER_STATE.FINISH;

            return false;
        }

        /// <summary> Retoma o tempo do jogo. </summary>
        public bool GameResume()
        {
            if (Timer != null)
            {
                Timer.Resume();

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::resumerTime][Log] Retomou o Timer[Tempo=" + Timer.getTimeLog() + "" + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return true;
            }

            return false;
        }

        // Verifica se o player já esteve na sala
        public bool IsGamingBefore(uint uid)
        {
            if (uid == 0u)
                throw new exception("[GameBase::isGamingBefore][Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                        1000, 0));

            return PlayerInfo.Any(el =>
            {
                return el.Value.uid == uid;
            });
        }

        protected void ClearGameTime()
        {
            // Garantir que qualquer exception derrube o server
            try
            {

                if (Timer != null)
                    GameServer.Instance.DeleteTimer(Timer);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::clear_time][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            Timer = null;
        }

        public virtual void SendInitialData(Player session)
        {

            Packet p = new Packet();

            try
            {

                // Course
                p.init_plain(0x52);

                p.WriteByte((byte)RoomInfo.CourseIndex);
                p.WriteByte(RoomInfo.RoomType);
                p.WriteByte(RoomInfo.HoleMode);
                p.WriteByte(RoomInfo.HoleCount);
                p.WriteUInt32(RoomInfo.TrophyID);
                p.WriteUInt32(RoomInfo.TimeSec);
                p.WriteUInt32(RoomInfo.TimeMin);
                // Hole Info, Hole Spinning Cube, end Seed Random Course
                Course.makePacketHoleInfo(p);
                session.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        protected Player FindSessionByOID(int oid)
        {
            return Players.FirstOrDefault(el => el.ConnectionID == oid);
        }

        protected Player FindSessionByUID(uint uid)
        {
            return Players.FirstOrDefault(el => el.UserInfo.UID == uid);
        }

        protected Player FindSessionByNickname(string nickname)
        {
            return Players.FirstOrDefault(el =>
            {
                return (string.CompareOrdinal(nickname, el.UserInfo.NickName) == 0);
            });
        }

        protected Player FindSessionByPlayerGameInfo(PlayerGameInfo pgi)
        {

            if (pgi == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::FindSessionByPlayerGameInfo][Error] PlayerGameInfo* _pgi is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return null;
            }

            return PlayerInfo.FirstOrDefault(el =>
            {
                return el.Value == pgi;
            }).Key;
        }

        public PlayerGameInfo GetPlayerInfo(Player session)
        {

            if (session == null)
            {
                throw new exception("[GameBase::GetPlayerInfo][Error] _session is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }

            return PlayerInfo.FirstOrDefault(el =>
            {
                return el.Key == session;
            }).Value;
        }



        // Se session for diferente de null retorna todas as session, menos a que foi passada no session
        public List<Player> GetSessions(Player session = null)
        {

            List<Player> v_sessions = new List<Player>();
            // Se session for diferente de null retorna todas as session, menos a que foi passada no session
            foreach (var el in Players)
            {
                if (el != null
                    && el.getState()
                    && el.UserInfo.Member.RoomID != -1
                    && (session == null || session != el))
                {
                    v_sessions.Add(el);
                }
            }
            return v_sessions;
        }

        public DateTime GetTimeStart()
        {
            return StartTime;
        }

        public void AddPlayer(Player session)
        {
            Players.Add(session);

            MakePlayerInfo(session);
        }

        public void MakePlayerInfo(Player session)
        {
            try
            {
                PlayerGameInfo pgi = MakePlayerInfoObject(session);

                // Bloqueia o OID para ninguém pegar ele até o torneio acabar
                //fazer isso depois luis
                /// GameServer.Instance.blockOID(session.ConnectionID);

                // Update Place player
                session.UserInfo.Place = 0;   // Jogando

                pgi.uid = session.UserInfo.UID;
                pgi.oid = session.ConnectionID;
                pgi.level = session.UserInfo.Member.GameLevel;

                // Entrou no Jogo depois de ele ter começado
                if (State)
                    pgi.enter_after_started = 1;

                // Typeid do Mascot Equipado
                if (session.Inventory.UserEquippedItem.MascotEquiped != null && session.Inventory.UserEquippedItem.MascotEquiped._typeid > 0)
                    pgi.mascot_typeid = session.Inventory.UserEquippedItem.MascotEquiped._typeid;

                // Premium User
                if (session.UserInfo.UserCapabilities.UserPremium)
                    pgi.premium_flag = true;

                // Card Wind ServerFlag
                pgi.card_wind_flag = getPlayerWindFlag(session);

                // Treasure Hunter Points Card Player Initialize Data
                // Não pode ser chamado depois do Init Item Used Game, por que ele vai add os pontos dos itens que dá Drop Rate e Treasure hunter point
                pgi.thi = getPlayerTreasureInfo(session);

                // ServerFlag Assist 
                if (session.UserInfo.AssistFlag)
                    pgi.assist_flag = 1;

                // Verifica se o player está com o motion item equipado
                pgi.char_motion_item = CheckCharMotionItem(session);

                // Motion Item da Treasure Hunter Point também
                if (pgi.char_motion_item == 1)
                    pgi.thi.all_score += 20;    // +20 all score

                pgi.data.clear();
                pgi.location.clear();
                if (!PlayerInfo.ContainsKey(session)) // ainda nao
                    PlayerInfo.Add(session, pgi);
                else //ja tem ele 
                {
                    try
                    {

                        // pega o antigo PlayerGameInfo para usar no Log
                        var pgi_ant = PlayerInfo[session];

                        // Novo PlayerGameInfo
                        PlayerInfo[session] = pgi;

                        // Log de que trocou o PlayerGameInfo da session
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::makePlayerInfo][Warning][Log] Normal[UID=" + (session.UserInfo.UID)
                                + "] esta trocando o PlayerGameInfo[UID=" + (pgi_ant.uid) + "] do player anterior que estava conectado com essa session, pelo o PlayerGameInfo[UID="
                                + (pgi.uid) + "] do player atual da session.", type_msg.CL_FILE_LOG_AND_CONSOLE));


                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::makePlayerInfo][Error][Warning] Normal[UID=" + (session.UserInfo.UID)
                                + "], nao conseguiu atualizar o PlayerGameInfo da session para o novo PlayerGameInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                // Init Item Used Game(Dados)
                RequestInitItemUsedGame(session, pgi);
            }
            catch
            { }
            // Exceções propagam diretamente — o catch vazio original não adicionava valor
            // e mascarava a origem real do erro.
        }

        public void ClearAllPlayerInfo()
        {
            PlayerInfo.Clear();
        }

        public void InitAllPlayerInfo()
        {
            foreach (var el in Players.ToArray())
                MakePlayerInfo(el);
        }

        // Make Object Player Info Polimofirsmo
        public virtual PlayerGameInfo MakePlayerInfoObject(Player session)
        {
            return new PlayerGameInfo();
        }

        public virtual bool DeletePlayer(Player session, int option)
        {
            if (session == null)
            {
                throw new exception("[GameBase::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    50, 0));
            }

            var it = Players.Any(c => c == session);

            if (it)
            {
                Players.Remove(session);//limpar ou deletar o jogador da lista
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return false;
        }

        public float TRANSF_SERVER_RATE_VALUE(uint rate)
        {
            return DefineConstants.TRANSF_SERVER_RATE_VALUE((int)rate);
        }

        public void SendBroadCast(Packet p)
        {
            try
            {
                var gamesession = GetSessions();
                for (var i = 0; i < gamesession.Count; ++i)
                    gamesession[i].Send(p);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[SendBroadCast(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void SendBroadCast(List<Packet> v_p)
        {
            try
            {
                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null)
                    {
                        var gamesession = GetSessions();
                        for (var ii = 0; ii < gamesession.Count; ++ii)
                            gamesession[ii].Send(v_p[i]);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage("Error byte[] p is null, Game::SendBroadCast()", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[SendBroadCast(List)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected ShotSyncData DecryptShot(byte[] buffer)
        {
            // Simplificado: < 38 || > 38 é equivalente a != 38
            if (buffer.Length != 38)
                return null;

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(buffer[i] ^ RoomInfo.GameKey[i % 16]);

            //decrypt shot
            var reader = new Packet(buffer);
            var ssd = new ShotSyncData
            {
                oid = reader.ReadInt32(), //OID
                location = new ShotSyncData.Location()
                {
                    x = reader.ReadFloat(),
                    y = reader.ReadFloat(),
                    z = reader.ReadFloat(),
                },
                state = (ShotSyncData.SHOT_STATE)reader.ReadByte(),

                bunker_flag = reader.ReadByte(),
                ucUnknown = reader.ReadByte(),

                pang = reader.ReadUInt32(),

                bonus_pang = reader.ReadUInt32(),

                state_shot = new ShotSyncData.stStateShot()
                {
                    display = new ShotSyncData.stStateShot.uDisplayState()
                    {
                        ulState = reader.ReadUInt32(),
                    },
                    shot = new ShotSyncData.stStateShot.uShotState()
                    {
                        ulState = reader.ReadUInt32()
                    }
                },

                tempo_shot = reader.ReadInt16(),
                grand_prix_penalidade = reader.ReadByte()
            };
            return ssd;
        }

        public virtual PlayerGameInfo InitPlayerInfo(string method, string msg, Player session)
        {
            var info = GetPlayerInfo(session);

            if (info == null)
            {
                // Get the class Name dynamically for the log
                string className = GetType().Name;

                throw new exception(
                    $"[{className}::{method}][Error] Normal[UID={session.UserInfo.UID}] {msg}, " +
                    "mas o game nao tem o info dele guardado. Bug",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4)
                );
            }

            return info;
        }

        // Overload using the logic above to satisfy the 'out' parameter requirement
        public virtual void InitPlayerInfo(string method, string msg, Player session, out PlayerGameInfo pgi)
        {
            pgi = InitPlayerInfo(method, msg, session);
        }


        public void ClearRoomLogInfo()
        {
            RoomLog.clear();
            //seta como default aqui
            RoomLog.roomId = Guid.Empty;
        }


        //somente atualiza no banco de dados
        // Atualiza as informações do jogador no log da sala
        public void UpdateRoomLogSql(Player session)
        {

            try
            {
                if (!GameServer.Instance.getActiveRoomLog())//nao vai logar os dados, melhor reiniciar 
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::UpdateRoomLogSql][Log] not actived", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                if (RoomLog == null)//nao vai logar os dados, melhor reiniciar 
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::UpdateRoomLogSql][Error] RoomLog is null", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                // Obtém as informações do jogador
                var pgi = GetPlayerInfo(session);

                if (pgi == null)
                    return; // Se não encontrou, sai da função

                //evento do world tour...
                if (sWorldTourSystem.Instance.isLoad() &&
     GameServer.Instance.getInfo().Rate.WorldTourEvent == 1 &&
     RoomInfo.HoleCount == 18 &&
     RoomInfo.SpecialModeRoom.IsShotMode == false &&
     (RoomInfo.RealRoomType == 2 || RoomInfo.RealRoomType == 4) && pgi.finish_game == 1)
                {
                    sWorldTourSystem.Instance.MarkCourseCompleted((int)session.UserInfo.UID, (int)RoomInfo.CourseIndex);
                }

                // Obtém o buraco atual no campo (se existir)
                var hole = (Course != null) ? Course.findHole(pgi.hole) : null;

                // Se achou o buraco, pega o par dele; senão, define como 0
                sbyte par = (sbyte)((hole != null) ? hole.getPar().par : 0);

                // Referência para as informações do jogador
                var ei = session.Inventory.UserEquippedItem;

                // Pega os _typeids de cada item do jogador (ou 0 se não existir)
                int char_info_typeid = (int)((ei.CharacterEquiped != null) ? ei.CharacterEquiped._typeid : 0);
                int clubset_typeid = (int)((ei.Club_WI != null) ? ei.Club_WI._typeid : 0u);
                int mascot_info_typeid = (int)((ei.MascotEquiped != null) ? ei.MascotEquiped._typeid : 0);
                int cad_info_typeid = (int)((ei.CaddieEquiped != null) ? ei.CaddieEquiped._typeid : 0);

                // Se o sistema de log de sala estiver desativado, sai
                //if (!GameServer.Instance.getActiveRoomLog())
                //    return;

                var tacada_num = pgi.data.tacada_num;

                // Converte o número de tacadas em uma string de pontuação (ex: BIRDIE, PAR, etc.)
                var score_str = GetScoreStr(tacada_num, par);

                // Verifica se o jogador passou do limite de 7 tacadas

                // Preenche as informações do jogador no objeto de log da sala
                RoomLog.UpdateInfo(
                    session.UserInfo.UID,                      // UID do jogador
                    char_info_typeid,                        // Tipo do personagem
                    clubset_typeid,                          // Tipo do taco
                    mascot_info_typeid,                      // Tipo do mascote
                    cad_info_typeid,                         // Tipo do caddie
                    pgi.hole,                               // Número do buraco
                    pgi.data.score,                         // Pontuação
                    pgi.data.exp,                           // EXP ganha
                    pgi.data.pang,                          // Pang ganho
                    pgi.data.bonus_pang,                    // Bonus Pang
                    (ulong)pgi.data.tacada_num,                    // Número de tacadas
                    (ulong)pgi.data.total_tacada_num,              // Total de tacadas
                    pgi.shot_data.special_shot.ulSpecialShot, // Tipo de tacada especial
                    session.Inventory.PremiumTicket.id > 0,                 // é premium?
                    GetScore(tacada_num, par) > 7,                                 // Is giveUp
                    pgi.data.time_out,                      // Estourou tempo
                    pgi.enter_after_started,                // Entrou após início?
                    pgi.finish_game,                        // Terminou o jogo?
                    pgi.assist_flag,                        // Usou assistência?
                    pgi.trofel,                             // Ganhou troféu? 
                    score_str == "HIO",                      // Hole In One?
                    score_str == "ALBATROSS",                // Albatross?
                    score_str == "EAGLE",                    // Eagle?
                    score_str == "BIRDIE",                   // Birdie?
                    score_str == "PAR",                      // Par?
                    score_str == "BOGEY",                    // Bogey?
                    score_str == "DOUBLE BOGEY",             // Duplo Bogey?
                    score_str == "TRIPLE BOGEY"              // Triplo Bogey?
                    , RoomInfo
                );

                // Se o ID do log ainda estiver vazio, gera um GUID
                if (RoomLog.roomId == Guid.Empty)
                    GenerateRoomLogGuid();

                // Se o Type da sala for de interesse, salva no banco de dados
                if (isLoggableRoomType(RoomInfo.RealRoomType))
                    NormalManagerDB.Instance.add(44, new CmdInsertOrUpdateRoomLog(RoomLog, CmdInsertOrUpdateRoomLog.TYPE.UPDATE), OnDatabaseResponse, session);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::UpdateRoomLogSql][ErrorSystem] Exceção capturada: " + (e.Message), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        bool isLoggableRoomType(byte tipo)
        {

            switch ((RoomTypeFlags)tipo)
            {
                case RoomTypeFlags.GUILD_BATTLE:
                case RoomTypeFlags.TOURNEY_TEAM:
                case RoomTypeFlags.STROKE:
                case RoomTypeFlags.MATCH:
                case RoomTypeFlags.PANG_BATTLE:
                case RoomTypeFlags.APPROCH:
                case RoomTypeFlags.TOURNEY:
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                //aqui deve ser outro Type de log, identificado por 1 ou 0
                case RoomTypeFlags.GRAND_ZODIAC_INT:
                case RoomTypeFlags.GRAND_ZODIAC_ADV:
                case RoomTypeFlags.GRAND_PRIX:
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                case RoomTypeFlags.PRACTICE:
                    return true;
                default:
                    return false;
            }
        }

        // Gera um GUID e formata como string no padrão "{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}"
        private void GenerateRoomLogGuid()
        {
            if (RoomLog == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Game::GenerateRoomLogGuid][Warning] RoomLog é null — GUID não gerado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            RoomLog.roomId = Guid.NewGuid();
        }


        string GetNameMap(uint map)
        {
            switch ((RoomCourseFlags)map)
            {
                case RoomCourseFlags.BLUE_LAGOON:
                    return "Blue Lagoon";
                case RoomCourseFlags.BLUE_WATER:
                    return "Blue Water";
                case RoomCourseFlags.SEPIA_WIND:
                    return "Sepia Wind";
                case RoomCourseFlags.WIND_HILL:
                    return "Wind Hill";
                case RoomCourseFlags.WIZ_WIZ:
                    return "Wiz Wiz";
                case RoomCourseFlags.WEST_WIZ:
                    return "West Wiz";
                case RoomCourseFlags.BLUE_MOON:
                    return "Blue Moon";
                case RoomCourseFlags.SILVIA_CANNON:
                    return "Silvia Cannon";
                case RoomCourseFlags.ICE_CANNON:
                    return "Ice Cannon";
                case RoomCourseFlags.WHITE_WIZ:
                    return "White Wiz";
                case RoomCourseFlags.SHINNING_SAND:
                    return "Shinning Sand";
                case RoomCourseFlags.PINK_WIND:
                    return "Pink Wind";
                case RoomCourseFlags.DEEP_INFERNO:
                    return "Deep Inferno";
                case RoomCourseFlags.ICE_SPA:
                    return "Ice Spa";
                case RoomCourseFlags.LOST_SEAWAY:
                    return "Lost Seaway";
                case RoomCourseFlags.EASTERN_VALLEY:
                    return "Eastern Valley";
                case RoomCourseFlags.ICE_INFERNO:
                    return "Ice Inferno";
                case RoomCourseFlags.WIZ_CITY:
                    return "Wiz City";
                case RoomCourseFlags.ABBOT_MINE:
                    return "Abbot Mine";
                case RoomCourseFlags.MYSTIC_RUINS:
                    return "Mystic Ruins";
                default:
                    return "Unknown";
            }
        }
        //retorna o Type da tacada = 0(HIO), 1(ALBA), 2(EAGLE),3(BIRDIE), 4(PAR), -1(tacadas não feitas )
        public int GetScore(int tacadaNum, int parHole)
        {
            int tipo = Convert.ToInt32(tacadaNum - parHole);
            if (tacadaNum == 1) // HIO
                return 0;
            else
            {
                switch (tipo)
                {
                    case -3:    // Alba
                        return 1;
                    case -2:    // Eagle
                        return 2;
                    case -1:    // Birdie
                        return 3;
                    case 0: // Par
                        return 4;
                    case 1: // bogey
                        return 5;
                    case 2: // Double bogey
                        return 6;
                    case 3: // Triple bogey
                        return 7;
                    default: // give up
                        return 8;

                }
            }
        }

        public int _GetScore(uint tacadaNum, sbyte parHole)
        {
            int tipo = Convert.ToInt32(tacadaNum - parHole);
            if (tacadaNum == 1) // HIO
                return 0;//hio(tava -4)
            else
            {
                switch (tipo)
                {
                    case -3:    // Alba
                        return 1;
                    case -2:    // Eagle
                        return 2;//okay
                    case -1:    // Birdie
                        return 3;
                    case 0: // Par
                        return 4;//nao calcula, pq é zero
                    case 1: // bogey
                        return 5;
                    case 2: // Double bogey
                        return 6;
                    case 3: // Triple bogey
                        return 7;
                    default: // give up
                        return 8;

                }
            }
        }

        string GetScoreStr(int tacadaNum, sbyte parHole)
        {

            var tipo = GetScore(tacadaNum, parHole);
            switch (tipo)
            {
                case 0:
                    return ("HIO");
                case 1:
                    return ("ALBATROSS");
                case 2:
                    return ("EAGLE");
                case 3:
                    return ("BIRDIE");
                case 4:
                    return ("PAR");
                case 5:
                    return ("BOGEY");
                case 6:
                    return ("DOUBLE BOGEY");
                case 7:
                    return ("TRIPLE BOGEY");
                default:
                    return ("GIVE UP");
            }
        }       

        public GameStateFlag GetGameState()
        {
            return (GameStateFlag)GameInitState;
        }

        protected void InitPlayersItemRainRate()
        {

            // Characters Equip
            foreach (var s in Players)
            {
                if (s.getState())
                { // Check Player Connected

                    if (s.Inventory.UserEquippedItem.CharacterEquiped == null)
                    { // Player não está com character equipado, kika dele do jogo
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::initPlayersItemRainRate][Log] Normal[UID=" + Convert.ToString(s.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        continue;
                    }

                    // Devil Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
      devil_wings.Contains(element)))
                    {
                        RateValue.rain += 10;
                    }

                    // Obsidian Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
      obsidian_wings.Contains(element)))
                    {
                        RateValue.rain += 10;
                    }

                    // Corrupt Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
    corrupt_wings.Contains(element)))
                    {
                        RateValue.rain += 15;
                    }

                    // Hasegawa Chirain
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
      hasegawa_chirain.Contains(element)))
                    {
                        RateValue.rain += 10;
                    }

                    // Hat Spooky Halloween -- Só funciona na época do Halloween (ex: outubro)
                    if (DateTime.Now.Month == 10 && s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element => hat_spooky_halloween.Contains(element)))
                    {
                        RateValue.rain += 10;
                    }


                    // Card Efeito 19 Rate Rain
                    var it = s.Inventory.CardEquipment.FirstOrDefault(el =>
                    {
                        return sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 2 && el.efeito == 19;
                    });

                    if (it != null)
                    {
                        if (it.efeito_qntd > 0)
                        {
                            RateValue.rain += it.efeito_qntd;
                        }
                    }

                    // Mascot Poltergeist -- Esse aqui "tenho que colocar a regra para funcionar só na epoca do halloween"
                    if (s.Inventory.UserEquippedItem.MascotEquiped != null && s.Inventory.UserEquippedItem.MascotEquiped._typeid == 0x40000029)
                    {
                        RateValue.rain += 10;
                    }

                    // Caddie Big Black Papel
                    if (s.Inventory.UserEquippedItem.CaddieEquiped != null && s.Inventory.UserEquippedItem.CaddieEquiped._typeid == 0x1C00000E)
                    {
                        RateValue.rain += 10;
                    }
                }
            }
        }

        public void InitPlayersItemRainPersistNextHole()
        {

            // Characters Equip
            foreach (var s in Players)
            {
                if (s.getState())
                { // Check Player Connected

                    if (s.Inventory.UserEquippedItem.CharacterEquiped == null)
                    { // Player não está com character equipado, kika dele do jogo
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::initPlayersItemRainPersistNextHole][Log] Normal[UID=" + Convert.ToString(s.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        continue;
                    }

                    // Devil Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
      devil_wings.Contains(element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        RateValue.persist_rain = 1;
                        return;
                    }

                    // Obsidian Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
     obsidian_wings.Contains(element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        RateValue.persist_rain = 1;
                        return;
                    }

                    // Corrupt Wings
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
     corrupt_wings.Contains(element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        RateValue.persist_rain = 1;
                        return;
                    }

                    // Hasegawa Chirain
                    if (s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
     hasegawa_chirain.Contains(element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        RateValue.persist_rain = 1;
                        return;
                    }

                    // Hat Spooky Halloween -- Esse aqui "tenho que colocar a regra para funcionar só na epoca do halloween"
                    if (DateTime.Now.Month == 10 && s.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
     hat_spooky_halloween.Contains(element)))
                    {
                        RateValue.persist_rain = 1;
                        return;
                    }


                    // Card Efeito 31 Persist Rain para o proximo hole

                    var it = s.Inventory.CardEquipment.FirstOrDefault(el =>
                    {
                        return sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 2 && el.efeito == 31;
                    });

                    if (it != null)
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        RateValue.persist_rain = 1;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// gerar o item do artefato, pode dar Experience, Pang, e etc...
        /// </summary>
        private void initArtefact()
        {

            switch (RoomInfo.ItemIDArtifact)
            {
                // Artefact of EXP
                case ART_LUMINESCENT_CORAL:
                    RateValue.exp += 2;
                    break;
                case ART_TROPICAL_TREE:
                    RateValue.exp += 4;
                    break;
                case ART_TWIN_LUNAR_MIRROR:
                    RateValue.exp += 6;
                    break;
                case ART_MACHINA_WRENCH:
                    RateValue.exp += 8;
                    break;
                case ART_SILVIA_MANUAL:
                    RateValue.exp += 10;
                    break;
                // End
                // Artefact of Rain Rate
                case ART_SCROLL_OF_FOUR_GODS:
                    RateValue.rain += 5;
                    break;
                case ART_ZEPHYR_TOTEM:
                    RateValue.rain += 10;
                    break;
                case ART_DRAGON_ORB:
                    RateValue.rain += 20;
                    break;
                    // End
            }
        }

        private PlayerGameInfo.eCARD_WIND_FLAG getPlayerWindFlag(Player session)
        {

            if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
            { // Player n�o est� com character equipado, kika dele do jogo
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::getPlayerWindFlag][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return PlayerGameInfo.eCARD_WIND_FLAG.NONE;
            }

            // 3 R, 17 SR, 13 SC, 12 N

            var it = session.Inventory.CardEquipment.FirstOrDefault(el =>
            {
                return (session.Inventory.UserEquippedItem.CharacterEquiped.id == el.parts_id && session.Inventory.UserEquippedItem.CharacterEquiped._typeid == el.parts_typeid) && sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 1 && (el.efeito == 3 || el.efeito == 17 || el.efeito == 13 || el.efeito == 12);
            });

            if (it != null)
            {
                switch (it.efeito)
                {
                    case 3:
                        return PlayerGameInfo.eCARD_WIND_FLAG.RARE;
                    case 12:
                        return PlayerGameInfo.eCARD_WIND_FLAG.NORMAL;
                    case 13:
                        return PlayerGameInfo.eCARD_WIND_FLAG.SECRET;
                    case 17:
                        return PlayerGameInfo.eCARD_WIND_FLAG.SUPER_RARE;
                }
            }

            return PlayerGameInfo.eCARD_WIND_FLAG.NONE;
        }

        public int InitCardWindPlayer(PlayerGameInfo pgi, byte wind)
        {

            if (pgi == null)
            {
                throw new exception("[GameBase::InitCardWindPlayer][Error] PlayerGameInfo* _pgi is invalid(null). Ao tentar inicializar o card wind player no jogo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            switch (pgi.card_wind_flag)
            {
                case PlayerGameInfo.eCARD_WIND_FLAG.NORMAL:
                    if (wind == 8) // 9m Wind
                    {
                        return -1;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.RARE:
                    if (wind > 0) // All Wind
                    {
                        return -1;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.SUPER_RARE:
                    if (wind >= 5) // High(strong) Wind
                    {
                        return -2;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.SECRET:
                    if (wind >= 5) // High(strong) Wind
                    {
                        return -2;
                    }
                    else if (wind > 0) // Low(weak) Wind, 1m não precisa diminuir
                    {
                        return -1;
                    }
                    break;
            }

            return 0;
        }

        private PlayerGameInfo.stTreasureHunterInfo getPlayerTreasureInfo(Player session)
        {

            PlayerGameInfo.stTreasureHunterInfo pti = new PlayerGameInfo.stTreasureHunterInfo();

            if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
            { // Player não está com character equipado, kika dele do jogo
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::getPlayerTreasureInfo][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return pti;
            }

            List<CardEquipInfoEx> v_cei = new List<CardEquipInfoEx>();

            // 9 N, 10 R, 14 SR por Score. 8 N, R, SR todos score
            session.Inventory.CardEquipment.ToList().ForEach(el =>
            {
                if ((session.Inventory.UserEquippedItem.CharacterEquiped.id == el.parts_id && session.Inventory.UserEquippedItem.CharacterEquiped._typeid == el.parts_typeid)
                    && sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 1
                    && (el.efeito == 8 || el.efeito == 9 || el.efeito == 10 || el.efeito == 14))
                {
                    v_cei.Add(el);
                }
            });

            if (v_cei.Count > 0)
            {
                foreach (var el in v_cei)
                {
                    switch (el.efeito)
                    {
                        case 8: // Todos Score
                            pti.all_score = (byte)el.efeito_qntd;
                            break;
                        case 9: // Par
                            pti.par_score = (byte)el.efeito_qntd;
                            break;
                        case 10: // Birdie
                            pti.birdie_score = (byte)el.efeito_qntd;
                            break;
                        case 14: // Eagle
                            pti.eagle_score = (byte)el.efeito_qntd;
                            break;
                    }
                }
            }

            // Card Efeito 18 Aumenta o Treasure point para qualquer score por 2 horas

            var it = session.Inventory.CardEquipment.FirstOrDefault(el =>
            {
                return sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 2 && el.efeito == 18;
            });

            if (it != null)
            {
                pti.all_score += (byte)it.efeito_qntd;
            }

            // Verifica se está com asa de anjo equipada (ShopRoom ou gacha), aumenta 30 Treasure hunter point para todos scores
            if (session.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped() == 1 && session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
            {
                pti.all_score += 30; // +30 all score
            }

            return pti;
        }

        public void UpdatePlayerAssist(Player session)
        {

            var pgi = GetPlayerInfo((session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "updatePlayerAssist][Error] Normal[UID=" + Convert.ToString((session).UserInfo.UID) + "] " + "tentou atualizar assist Pang no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (pgi.assist_flag == 1 && pgi.level > 10)
                pgi.data.pang = Convert.ToUInt64(pgi.data.pang * 0.7f); // - 30% dos pangs
        }

        public void InitGameTime()
        {
            StartTime = DateTime.Now;
        }

        public int GetRankPlace(Player session)
        {

            var pgi = GetPlayerInfo((session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "GetRankPlace][Error] Normal[UID=" + Convert.ToString((session).UserInfo.UID) + "] " + "tentou pegar o lugar no RankPosition do jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }
            return PlayerOrder.IndexOf(pgi);
        }

        public int SortPlayerRank(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if (pgi1.data.score == pgi2.data.score)
                return pgi2.data.pang.CompareTo(pgi1.data.pang); // decrescente de Pang (maior Pang primeiro)

            return pgi1.data.score.CompareTo(pgi2.data.score); // crescente de score (menor score primeiro)
        }

        public int GetCountPlayersGame()
        {
            return PlayerInfo.Count(el =>
            {
                return el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT;
            });
        }

        public void InitAchievement(Player session)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::InitAchievement][Error] Normal[UID=" + Convert.ToString((session).UserInfo.UID) + "] " + "tentou inicializar o achievemento do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }


            try
            {

                // Initialize Achievement Player
                pgi.sys_achieve.incrementCounter(0x6C400002u/*Normal Game*/);

                if (RoomInfo.SpecialModeRoom.IsShotMode)
                    pgi.sys_achieve.incrementCounter(0x6C4000BBu/*Short Game*/);

                if (RoomInfo.OwnerUID == session.UserInfo.UID)
                {
                    pgi.sys_achieve.incrementCounter(0x6C400098u/*Master da Sala*/);

                    if (RoomInfo.ItemIDArtifact > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400099u/*Master da Sala com Artefact*/);
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped != null && session.Inventory.UserEquippedItem.CharacterEquiped.id > 0)
                {

                    var ctc = AchievementSystem.getCharacterCounterTypeId(session.Inventory.UserEquippedItem.CharacterEquiped._typeid);

                    if (ctc > 0u)
                        pgi.sys_achieve.incrementCounter(ctc/*Character Counter Typeid*/);
                }

                if (session.Inventory.UserEquippedItem.CaddieEquiped != null && session.Inventory.UserEquippedItem.CaddieEquiped.id > 0)
                {

                    var ctc = AchievementSystem.getCaddieCounterTypeId(session.Inventory.UserEquippedItem.CaddieEquiped._typeid);

                    if (ctc > 0u)
                        pgi.sys_achieve.incrementCounter(ctc/*Caddie Counter Typeid*/);
                }

                if (session.Inventory.UserEquippedItem.MascotEquiped != null && session.Inventory.UserEquippedItem.MascotEquiped.id > 0)
                {

                    var ctm = AchievementSystem.getMascotCounterTypeId(session.Inventory.UserEquippedItem.MascotEquiped._typeid);

                    if (ctm > 0u)
                        pgi.sys_achieve.incrementCounter(ctm/*Mascot Counter Typeid*/);
                }

                var ct = AchievementSystem.getCourseCounterTypeId((uint)(RoomInfo.GetMap() & 0x7F));

                if (ct > 0)
                    pgi.sys_achieve.incrementCounter(ct/*Course Counter Item*/);

                ct = AchievementSystem.getQntdHoleCounterTypeId(RoomInfo.HoleCount);

                if (ct > 0)
                    pgi.sys_achieve.incrementCounter(ct/*Qntd Hole Counter Item*/);

                // Fim do inicializa o Achievement

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::initAchievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void RecordsPlayerAchievement(Player session)
        { 
            InitPlayerInfo("records_player_achievement", "tentou atualizar os achievement de records do player no jogo", session, out PlayerGameInfo pgi);

            try
            {

                if (pgi.ui.ob > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40004Cu/*OB*/, pgi.ui.ob);

                if (pgi.ui.bunker > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40004Eu/*Bunker*/, pgi.ui.bunker);

                if (pgi.ui.tacada > 0 || pgi.ui.putt > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400055u/*Shots*/, pgi.ui.tacada + pgi.ui.putt);

                if (pgi.ui.hole > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400005u/*Holes*/, pgi.ui.hole);

                if (pgi.ui.total_distancia > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400056u/*Yards*/, pgi.ui.total_distancia);

                // Bug o valor é 0 por que (int)0.9f é 0 ele trunca não arredondo, e tem que truncar mesmo
                // Para fixa esse bug é só fazer >= 1.f sempre vai ser (int) >= 1(truncado)
                if (pgi.ui.best_drive >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400057u/*Best Drive*/, (int)pgi.ui.best_drive);

                if (pgi.ui.best_chip_in >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400058u/*Best Chip-in*/, (int)pgi.ui.best_chip_in);

                if (pgi.ui.best_long_putt >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400077u/*Best Long-putt*/, (int)pgi.ui.best_long_putt);

                if (pgi.ui.acerto_pangya > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Bu/*Acerto PangYa*/, pgi.ui.acerto_pangya);

                if (pgi.data.pang > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Du/*Pangs Ganho em 1 jogo*/, (int)pgi.data.pang);

                if (pgi.data.score != 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Cu/*Score*/, pgi.data.score);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::records_player_achievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void UpdateSyncShotAchievement(Player session, Location lastLocation)
        {
            InitPlayerInfo("UpdateSyncShotAchievement", "tentou atualizar o achievement de Desafios no jogo", session, out PlayerGameInfo pgi);

            try
            {

                // Só conta se o player acertou o hole
                if (pgi.shot_sync.state_shot.display.acerto_hole)
                {

                    // Long-putt
                    if (pgi.shot_sync.state_shot.display.long_putt && pgi.shot_sync.state_shot.shot.club_putt == 1)
                    {
                        var diff = pgi.location.diffXZ(lastLocation) * MEDIDA_PARA_YARDS;

                        if (diff >= 30.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400035u/*Long Putt 30y+*/);

                        if (diff >= 25.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400034u/*Long Putt 25y+*/);

                        if (diff >= 20.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400033u/*Long Putt 20y+*/);

                        if (diff >= 17.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400032u/*Long Putt 17y+*/);
                    }

                    //Fez o hole de Beam Impact
                    if (pgi.shot_sync.state_shot.display.beam_impact)
                        pgi.sys_achieve.incrementCounter(0x6C40006Fu/*Beam Impact*/);

                    // Fez o hole com
                    if (pgi.shot_sync.state_shot.shot.spin_front == 1)
                        pgi.sys_achieve.incrementCounter(0x6C400064u/*Spin Front*/);

                    if (pgi.shot_sync.state_shot.shot.spin_back == 1)
                        pgi.sys_achieve.incrementCounter(0x6C400065u/*Spin Back*/);

                    if (pgi.shot_sync.state_shot.shot.curve_left > 0 || pgi.shot_sync.state_shot.shot.curve_right > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400066u/*Curve*/);

                    if (pgi.shot_sync.state_shot.shot.tomahawk > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400067u/*Tomahawk*/);

                    if (pgi.shot_sync.state_shot.shot.spike > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400068u/*Spike*/);

                    if (pgi.shot_sync.state_shot.shot.cobra > 0)
                        pgi.sys_achieve.incrementCounter(0x6C40006Eu/*Cobra*/);

                    ////Fez sem usar power shot
                    if (pgi.shot_sync.state_shot.display.chip_in_without_special_shot && !pgi.shot_sync.state_shot.display.special_shot/*Nega*/)
                        pgi.sys_achieve.incrementCounter(0x6C40005Bu/*Fez sem usar power shot*/);

                    // o pacote12 passa primeiro depois que o server response ele passa esse pacote1B, então esse valor sempre vai está certo
                    // Fez Errando pangya
                    if ((pgi.shot_data.acerto_pangya_flag & 2/*Errou pangya*/ ).IsTrue() && !pgi.shot_sync.state_shot.shot.club_putt.IsTrue()/*Nega*/)
                        pgi.sys_achieve.incrementCounter(0x6C400059u/*Fez errando pangya*/);
                }

                // Tacada Power Shot ou Double Power Shot
                if (pgi.shot_sync.state_shot.shot.power_shot > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400051u/*Power Shot*/);

                if (pgi.shot_sync.state_shot.shot.double_power_shot > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400052u/*Double Power Shot*/);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::update_sync_shot_achievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void RainHoleSeqCount(Player session)
        {

            var chr = Course.getConsecutivesHolesRain();

            InitPlayerInfo("rain_hole_consecutivos_count", "tentou atualizar o achievement count de Rain em holes consecutivos do player no jogo", session, out PlayerGameInfo pgi);

            try
            {
                var seq = (uint)Course.findHoleSeq(pgi.hole);
                var count = 0;
                if (chr.isValid())
                {

                    // 2 Holes consecutivos
                    if ((count = chr._2_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Bu/*2 Holes consecutivos*/, count);

                    if ((count = chr._3_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Cu/*3 Holes consecutivos*/, count);

                    if ((count = chr._4_pluss_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Du/*4 ou mais Holes consecutivos*/, count);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::rain_hole_consecutivos_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void ScoreSeqCount(Player session)
        {

            int score = -2, last_score = -2;

            InitPlayerInfo("rain_score_consecutivos", "tentou atualizar o achievement contador de score consecutivos do player no jogo", session, out PlayerGameInfo pgi);

            try
            {
                int count = 0;
                for (var i = 0; i < RoomInfo.HoleCount; ++i)
                {
                    score = AchievementSystem.getScoreNum(pgi.progress.tacada[i], pgi.progress.par_hole[i]);

                    // Change Score, Soma o Count do Score
                    if ((score != last_score || i == (RoomInfo.HoleCount - 1)/*Ultimo hole*/) && last_score != -2/*Primeiro Hole*/)
                    {

                        // 1 == 2, 2 ou mais Holes com o mesmo score
                        if (count >= 1u && last_score >= 0/*Scores que tem no achievement*/)
                        {

                            switch (last_score)
                            {
                                case 0: // HIO
                                    pgi.sys_achieve.incrementCounter(0x6C400063u/*HIO*/);
                                    break;
                                case 1: // Alba
                                    pgi.sys_achieve.incrementCounter(0x6C400062u/*Alba*/);
                                    break;
                                case 2: // Eagle
                                    pgi.sys_achieve.incrementCounter(0x6C400061u/*Eagle*/);
                                    break;
                                case 3: // Birdie
                                    pgi.sys_achieve.incrementCounter(0x6C40005Du/*Birdie*/);
                                    break;
                                case 4: // Par
                                    pgi.sys_achieve.incrementCounter(0x6C40005Eu/*Par*/);
                                    break;
                                case 5: // Bogey
                                    pgi.sys_achieve.incrementCounter(0x6C40005Fu/*Bogey*/);
                                    break;
                                case 6: // Double Bogey
                                    pgi.sys_achieve.incrementCounter(0x6C400060u/*Double Bogey*/);
                                    break;
                            }
                        }

                        // Reseta o count
                        count = 0;

                    }
                    else if (score == last_score)
                        count++;

                    // Update Last Score
                    last_score = score;
                }
            }

            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::score_consecutivos_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void RainCount(Player session)
        {
            try
            {

                // Recovery, Chuva, Neve/*Tempo Ruim*/
                if (Course.countHolesRain() > 0)
                {
                    InitPlayerInfo("rain_count", "tentou atualizar o achievement contador de Rain do player no jogo", session, out PlayerGameInfo pgi);

                    // Pega pela quantidade de holes jogados
                    int seq = Course.findHoleSeq(pgi.hole);

                    uint count;
                    if ((count = Course.countHolesRainBySeq((uint)seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Au/*Chuva*/, (int)count);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::rain_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public void SetEffectActiveInShot(Player session, ulong effect)
        {
            try
            {

                InitPlayerInfo("setEffectActiveInShot", "tentou setar o efeito ativado na tacada", session, out PlayerGameInfo pgi);

                pgi.effect_flag_shot.ullFlag |= effect; // Ativa o efeito na tacada
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::setEffectActiveInShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Limpa os dados que são usados para cada tacada, reseta ele para usar na próxima tacada 
        public void ClearDataEndShot(PlayerGameInfo pgi)
        {

            if (pgi == null)
                throw new exception("[GameBase::clearDataEndShot][Error] PlayerGameInfo *_pgi is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 100, 0));

            try
            {
                pgi.effect_flag_shot.clear();
                pgi.item_active_used_shot = 0;
                pgi.earcuff_wind_angle_shot = 0.0f;
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::clearDataEndShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void CheckEffectItemAndSet(Player session, uint _typeid)
        {
            //CHECK_SESSION("checkEffectitemAndSet");

            try
            {

                var ability = sIff.Instance.findAbility(_typeid);

                if (ability != null)
                {

                    for (var i = 0; i < ability.Efeito.Type.Length; ++i)
                    {

                        if (ability.Efeito.Type[i] == 0u)
                            continue;

                        if (ability.Efeito.Type[i] == (uint)AbilityEffect.COMBINE_ITEM_EFFECT)
                        {

                            // find item setEffectTable
                            var effectTable = sIff.Instance.findSetEffectTable((uint)ability.Efeito.Rate[i]);

                            if (effectTable != null)
                            {

                                for (var j = 0; j < effectTable.effect.effect.Length; ++j)
                                {

                                    if (effectTable.effect.effect[j] == 0u || effectTable.effect.effect[j] < 4u)
                                        continue;

                                    switch ((eEFFECT)effectTable.effect.effect[j])
                                    {
                                        case eEFFECT.PIXEL:
                                            SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.PIXEL));
                                            break;
                                        case eEFFECT.ONE_ALL_STATS:
                                            SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.ONE_IN_ALL_STATS));
                                            break;
                                        case eEFFECT.WIND_DECREASE:
                                            SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.DECREASE_1M_OF_WIND));
                                            break;
                                        case eEFFECT.PATINHA:
                                            SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));
                                            break;
                                    }
                                }
                            }

                        }
                        else
                            SetEffectActiveInShot(session, enumToBitValue((AbilityEffect)(ability.Efeito.Type[i])));
                    }
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::checkEffectitemAndSet][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void OnDatabaseResponse(int msgId, Pangya_DB pangyaDb, object arg)
        {

            if (arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::SQLDBResponse][Warning] _arg is null com msg_id = " + (msgId), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (pangyaDb.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::SQLDBResponse][Error] " + pangyaDb.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (msgId)
            {
                case 12:    // Update ClubSet Workshop
                    {
                        break;
                    }
                case 1: // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 43:    // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 44:    // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 0:
                default:    // 25 é update item equipado slot
                    break;
            }
        }
    }
}
