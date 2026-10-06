using Pangya_GameServer.Channels;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Helpers;
using Pangya_GameServer.Roms.GameModes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Numerics;

using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms
{
    public partial class Room
    {
        public bool AddPlayer(Player player, bool Invited)
        {
            lock (_cs)
            {
                if (Players.Contains(player))
                    return false;

                Players.Add(player);

                player.SetRoom(this, Invited);


                // Update Place Player
                if (RoomInfo.GetRoomType() == RoomTypeFlags.PRACTICE || RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                {
                    player.UserInfo.Place = 2;
                }
                else
                {
                    player.UserInfo.Place = 0;
                }

                return true;
            }
        }

        public void RemovePlayer(Player player)
        {  
            lock (_cs)
            {
                if (Players.Remove(player))
                  { 
                    player.SetRoom(null);  
                    return; 
                }
                player.SetRoom(null);
                //se for pra sair da sala 100%

            }
        } 

        
        private void GenerateSecurityKey()
        {
            Random.Shared.NextBytes(RoomInfo.GameKey);
        }

        public string getName()
        {
            return RoomInfo.Name;
        }

        public string getPass()
        {
            return RoomInfo.IsPublicRoom == 1 ? "" : RoomInfo.Password;
        }

        public RoomTypeFlags GetTipo()
        {
            return RoomInfo.GetRoomType();
        }  

        private void ClearPlayersKicked()
        {
            if (PlayersKickeds.Any())
                PlayersKickeds.Clear();
        }

        private void UpdatePosition()
        {
            for (int i = 0; i < Players.Count; ++i)//255 e o limite
            {
                PlayersInfo[Players[i]].RankPosition = (byte)Math.Min(i + 1, byte.MaxValue);
            }
        }

        // private porque é um método inseguro (sem thread safety)
        public uint GetCountPlayersWithoutInvited()
        {
            return (uint)Players.Count(el =>
            {
                if (el == null)
                    return false;

                return PlayersInfo.TryGetValue(el, out var playerInfo) && !(playerInfo.Invite == 1);
            });
        }

        public GameRoomInfoModel GetInfo()
        {
            return RoomInfo;
        }

        public byte[] ToArray()
        {
            return RoomInfo.ToArray();
        }

        public Player FindSessionByOid(uint oid)
        { 
            return Players.FirstOrDefault(el => el.ConnectionID == oid);
        }

        public Player FindSessionByUid(uint uid)
        { 
            return Players.FirstOrDefault(el => el.UserInfo.UID == uid);
        }

        public Player FindMaster() => Players.FirstOrDefault(el => el.UserInfo.UID == RoomInfo.OwnerUID);
        public int FindIndexSession(Player session)
        {
            // Verifica se a lista ou o objeto passado não são nulos para evitar NullReferenceException
            if (Players == null || Players.Count == 0 || session == null)
                return -1;

            // Retorna o índice do primeiro elemento que satisfaz a condição
            return Players.FindIndex(s => s.UserInfo.UID == session.UserInfo.UID);
        }

        public int FindIndexSession(uint uid)
        { 
            if (uid == 0)
            {
                throw new ArgumentException("O UID fornecido é inválido (zero).", nameof(uid));
            }
             
            return Players.FindIndex(el => el?.UserInfo?.UID == uid);
        }

        public void @lock()
        {
            // 1. Tenta obter o lock real do objeto
            Monitor.Enter(_cs);

            // 2. Verifica se a sala está sendo destruída após obter o lock
            if (Destroying)
            {
                Monitor.Exit(_cs);
                throw new exception("[room::lock] Room em destruição.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 150, 0));
            }

            Interlocked.Increment(ref m_lock_spinstate);
        }

        public bool trylock()
        {
            // Tenta entrar sem bloquear a thread se já estiver ocupado
            if (!Monitor.TryEnter(_cs))
                return false;

            if (Destroying)
            {
                Monitor.Exit(_cs);
                return false;
            }

            Interlocked.Increment(ref m_lock_spinstate);
            return true;
        }

        public void unlock()
        {
            // Decrementa o estado de forma atômica
            int state = Interlocked.Decrement(ref m_lock_spinstate);

            if (state < 0)
            {
                // Resetamos para zero para evitar estados inconsistentes
                Interlocked.Exchange(ref m_lock_spinstate, 0);

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Room::UnLock][Warning] Sala {RoomInfo.RoomID} já estava desbloqueada.",
                    type_msg.CL_ONLY_CONSOLE));
            }

            // Libera o lock real
            if (Monitor.IsEntered(_cs))
            {
                Monitor.Exit(_cs);
            }
        }

        public void SetDestroying()
        {
            Destroying = true;
        }

        public bool GetDestroying()
        {
            return Destroying;
        }

        public List<InviteChannelInfo> getAllInvite()
        {
            return Invites;
        }
         
        #region METHODS SET/GET GAME.ROOM
         
        public sbyte GetChannelId()
        {
            return ChannelOwner.getId();
        }
          
        public short GetRoomId()
        {
            return RoomInfo.RoomID;
        }

        public bool GameRun()
        {
            return (CurrentGame != null && CurrentGame.GameInitState == 1) || RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE;
        }
         
        public bool IsGamingBefore(uint uid)
        {

            if (uid == 0)
            {
                throw new exception("[room::isGamingBefore] [Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1000, 0));
            }

            if (CurrentGame == null)
            {
                throw new exception("[room::isGamingBefore] [Error] a sala[NUMERO=" + RoomInfo.RoomID + "] nao tem um jogo inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1001, 0));
            }

            return CurrentGame.IsGamingBefore(uid);
        }


        public uint GetRealNumPlayersWithoutInvited()
        {

            uint num = 0;



            try
            {

                num = GetCountPlayersWithoutInvited();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[room::getRealNumPlayerWithoutInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }



            return (num);
        }

        public bool HaveInvited()
        {

            bool question = false;
            try
            {
                question = HaveInvitedInternal();
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[room::haveInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return question;
        }

        public bool CheckSecurityKey(byte[] senhaEncriptSala)
        {
            if (senhaEncriptSala == null || senhaEncriptSala.Length == 0)
                return false;

            byte[] storedKey = RoomInfo.GameKey;

            if (storedKey == null || storedKey.Length != senhaEncriptSala.Length)
                return false;

            // Comparação segura (evita timing attack)
            bool equals = true;
            for (int i = 0; i < storedKey.Length; i++)
                equals &= storedKey[i] == senhaEncriptSala[i];

            return equals;
        }



        public void SetNome(string nome)
        {
            if (string.IsNullOrEmpty(nome))
            {
                throw new exception("Error Name esta vazio. Em room::SetNome()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    6, 0));
            }
            RoomInfo.Name = nome;
        }

        public void SetSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                // Sem Password: limpa e marca sala como aberta (IsPublicRoom = 0 = sem Password)
                RoomInfo.Password = "";
                RoomInfo.IsPublicRoom = 1;
            }
            else
            {
                // Com Password: define e marca sala como bloqueada (IsPublicRoom = 1 = tem Password)
                RoomInfo.Password = senha;
                RoomInfo.IsPublicRoom = 0;
            }
        }

        public void SetType(byte tipo)
        {

            if (tipo == (byte)RoomTypeFlags.MATCH || tipo == (byte)RoomTypeFlags.GUILD_BATTLE)
                InitTeams();
            else if (tipo != (byte)RoomTypeFlags.MATCH && RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
                ClearTeams();

            RoomInfo.RealRoomType = tipo;

            // Atualizar Type da sala
            if (RoomInfo.RealRoomType > (byte)RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                RoomInfo.RoomType = 4;
            else if (RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_ADV || RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                RoomInfo.RoomType = (byte)RoomTypeFlags.GRAND_ZODIAC_INT;
            else
                RoomInfo.RoomType = RoomInfo.RealRoomType;


            if (RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT)
                RoomInfo.SpecialRoomFLag = RoomInfo.RealRoomType;
            else
                RoomInfo.SpecialRoomFLag = 255;

            // Atualiza Trofel se for Tourney
            if (RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY || (RoomInfo.OwnerUID != -2 && RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT && RoomInfo.RealRoomType <= (byte)RoomTypeFlags.GRAND_ZODIAC_ADV))
            {

                if ((RoomInfo.MaxUsers > 30 && RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY) || (RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT && RoomInfo.RealRoomType <= (byte)RoomTypeFlags.GRAND_ZODIAC_ADV))
                {

                    RoomInfo.IsGameMaster = 1;

                    RoomInfo.SpecialFlag = 0x100;

                    RoomInfo.TrophyID = TROFEL_GM_EVENT_TYPEID;

                }
                else if (RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY || RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT)
                {
                    UpdateTrofel();
                }

            }
            else
            {
                RoomInfo.TrophyID = 0;
            }
        }

        public void SetCourse(byte course)
        {
            RoomInfo.CourseIndex = (RoomCourseFlags)course;
        }

        public void SetQntdHole(byte qntdHole)
        {
            RoomInfo.HoleCount = qntdHole;
        }

        public void SetModo(byte modo)
        {
            RoomInfo.HoleMode = modo;
        }

        public void SetStrokeTime(uint tempo)
        {
            RoomInfo.TimeSec = tempo;
        }

        public void SetMaxUsers(byte maxPlayer)
        {

            if (Players.Count > maxPlayer)
            {
                throw new exception("[room::setMaxPlayer] [Error] MASTER[UID=" + Convert.ToString(RoomInfo.OwnerUID) + "] _max_PLAYER[VALUE=" + Convert.ToString(maxPlayer) + "] é menor que o RoomID de jogadores[VALUE=" + Convert.ToString(Players.Count) + "] na sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    250, 0x588000));
            }

            // New Max Player room
            RoomInfo.MaxUsers = maxPlayer;

            // Atualiza Trofeu se for Tourney
            if (RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY || (RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT && RoomInfo.RealRoomType <= (byte)RoomTypeFlags.GRAND_ZODIAC_ADV))
            {

                if ((RoomInfo.MaxUsers > 30 && RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY) || (RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT && RoomInfo.RealRoomType <= (byte)RoomTypeFlags.GRAND_ZODIAC_ADV))
                {

                    RoomInfo.IsGameMaster = 1;

                    RoomInfo.TrophyID = TROFEL_GM_EVENT_TYPEID;

                }
                else if (RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY || RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT)
                {
                    UpdateTrofel();
                }
            }
        }

        public void SetTime30S(uint tempo)
        {
            RoomInfo.TimeMin = tempo;
        }

        public void SetHoleRepeted(byte holeRepeat)
        {
            RoomInfo.IDHoleRepeted = holeRepeat;
        }
        
        public void SetGalleryLimit(byte fixedHole)
        {
            RoomInfo.GalleryLimite = fixedHole;
        }

        public void SetFixedHole(uint fixedHole)
        {
            RoomInfo.HoleFixed = fixedHole;
        }

        public void SetArtefato(uint artefato)
        {
            RoomInfo.ItemIDArtifact = artefato;
        }

        public void SetNatural(uint natural)
        {
            RoomInfo.SpecialModeRoom.Value = natural;
        }

        public void SetState(byte state)
        {
            RoomInfo.StateRoom = state;
        }

        public void SetFlag(byte flag)
        {
            RoomInfo.FlagRoom = flag;
        }

        public void SetStateAFK(byte stateAfk)
        {
            RoomInfo.StateSleep = stateAfk;
        }

        public void SetAllReady()
        {
            byte ready = 0;
            foreach (var el in Players)
            {
                var pri = GetPlayerInfo(el);
                pri.State.Ready = (byte)(ready == 0 ? 1 : 0);//invertido
                if (pri.State.Ready == 1 && !pri.Capability.IsGameMaster)
                {
                    UpdatePlayerInfo(el);
                    var p = new Packet();
                    p.init_plain(0x78);
                    p.WriteInt32(pri.OID);
                    p.WriteByte(ready);
                    SendBroadCast(p);
                }
            }
        }

        public bool IsWithBot()
        {
            return BotTourney;
        } 

        public bool CheckPass(string pass)
        {

            if (!IsLocked())
            {
                throw new exception("[Room::checkPass] [Error] sala nao tem Password", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1, 0));
            }

            return string.Compare(RoomInfo.Password, pass) == 0;
        }

        public bool IsFull()
        {
            RoomInfo.CurrentUsers = (byte)Players.Count;
            return RoomInfo.CurrentUsers >= RoomInfo.MaxUsers;
        }
         
        public bool IsKickedPlayer(uint uid)
        {
            return PlayersKickeds.Any(el => el.Key == uid);
        }

        public virtual bool IsAllReady()
        {

            var master = FindMaster();

            if (master == null)
            {
                return false;
            }

            // Corrigido: && tem precedência sobre ||, então a condição original
            // (A && B || C) era lida como (A && B) || C — adicionados parênteses explícitos.
            if ((BotTourney && Players.Count == 1 && RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY)
                || RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
            {
                return true;
            }

            // se a sala for Practice, CHIP-IN Practice, e GRAND_PRIX_NOVICE não precisa o Player está pronto
            if (RoomInfo.GetRoomType() == RoomTypeFlags.PRACTICE
                || RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE
                || RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_PRIX)
            {
                return true;
            }

            // Se o Master for GM então não precisar todos está Ready(prontos)
            if (master.UserInfo.UserCapabilities.IsGameMaster && !HaveInvitedInternal())
            {
                return true;
            }


            var count = Players.Count(el =>
            {
                var pri = GetPlayerInfo(el);
                return (pri != null && pri.State.Ready == 1);
            });

            // Conta com o Master por que o Master sempre está pronto(Ready)
            return count == Players.Count;
        }
         
        // private por que é o método unsave(inseguro), sem thread safe
        public bool HaveInvitedInternal()
        {
            return Players.Any(el =>
            {
                if (el == null)
                    return false;

                return PlayersInfo.TryGetValue(el, out var playerInfo) && playerInfo.Invite == 1;
            });
        }

        public bool IsInvited(Player session)
        {

            var it = PlayersInfo.FirstOrDefault(c => c.Key.GetUID() == session.GetUID());

            return (it.Value != null && it.Value.Invite == 1);
        }



        public bool IsRoomGM()
        {
            return RoomInfo.IsGameMaster == 1 &&
             RoomInfo.SpecialFlag == 0x100;
        }

        public uint GetMaster() => (uint)RoomInfo.OwnerUID;
        public uint GetNumPlayers() => (uint)Players.Count;
        public bool IsLocked() => RoomInfo.IsPublicRoom != 1;

        public byte GetPosition(Player session)
        {
            byte position = 255;

            for (byte i = 0; i < Players.Count; ++i)
            {
                if (Players[i] == session)
                {
                    position = i;
                    break;
                }
            }
            return position;
        }

        public PlayerRoomInfo GetPlayerInfo(Player session)
        {

            if (session == null)
            {
                throw new exception("Error _session is null. Em room::GetPlayerInfo()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    7, 0));
            }

            PlayerRoomInfo pri = PlayersInfo.FirstOrDefault(c => c.Key == session).Value;
            if (pri == null)
                return null;

            return pri;
        }

        public List<Player> GetSessions(Player? session = null, bool withInvited = true)
        {
            var result = new List<Player>(Players.Count);
            var addedUids = new HashSet<uint>(Players.Count);

            foreach (var el in Players)
            {
                // 1. Verificação básica de nulidade
                if (el?.UserInfo == null) continue;

                // 2. Filtros de lógica
                bool isTargetSession = session != null && el.UserInfo.UID == session.UserInfo.UID;
                if (isTargetSession) continue;

                if (el.UserInfo.Member.RoomID == -1) continue;

                // 3. Filtro de convidados (Invertido para legibilidade)
                if (!withInvited && IsInvited(el)) continue;

                // 4. Verificação de duplicidade e adição
                if (addedUids.Add(el.UserInfo.UID))
                {
                    result.Add(el);
                }
            }

            return result;
        }

        public virtual bool IsDropRoom()
        {
            return true; // class room Normal é sempre true
        }

        #endregion

        public List<Team> GetTeam()
        {
            return Teams;
        }

        public void SendUpdateRoomInfo(int opt = 3)
        {
            if (RoomInfo.GetRoomType() != RoomTypeFlags.PRACTICE && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                ChannelOwner.SendBroadcast(Handle_PACKET_RESPONSE.MakeGameRoomList(new List<GameRoomInfoModel>() { RoomInfo }, opt), 0);
        }

       
        public void SendBroadCast(Packet p)
        {
            try
            {
                var roomsession = GetSessions(null, false/*without invited*/);
                for (var i = 0; i < roomsession.Count; ++i)
                {
                    if (roomsession[i] != null)
                    {
                        roomsession[i].Send(p);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomBroadcast] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void SendBroadCast(List<Player> roomsession, Packet p)
        {
            try
            {
                for (var i = 0; i < roomsession.Count; ++i)
                {
                    if (roomsession[i] != null)
                    {
                        roomsession[i].Send(p);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomBroadcast] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                        var roomsession = GetSessions();
                        for (var ii = 0; ii < roomsession.Count; ++ii)
                            roomsession[ii].Send(v_p[i]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomBroadcast] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void StartGame(Player session)
        {

            try
            {

                if (CurrentGame == null)
                    throw new exception("[room::startGame] [Error] Normal[UID=" + (session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO="
                        + (RoomInfo.RoomID) + "], mas a sala nao tem nenhum jogo iniciado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1, 0x5200101));

                if (RoomInfo.FlagRoom == 0)
                {

                    CurrentGame.SendInitialData(session);//aqui

                    if (RoomInfo.GetRoomType() == RoomTypeFlags.STROKE || RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
                        SendPlayerInfo(session, 0x103);
                }
                else
                {   // Entra depois

                    try
                    {
                        List<PlayerRoomInfo> v_element = new List<PlayerRoomInfo>();
                        PlayerRoomInfo pri = null;

                        foreach (var el in Players)
                        {
                            if ((pri = GetPlayerInfo(el)) != null)
                                v_element.Add(pri);
                        }


                        // Send Make Room
                        var p = new Packet(0x113);

                        p.WriteByte(4);  // Cria sala
                        p.WriteByte(0);

                        p.WriteBytes(RoomInfo.ToArray());

                        session.Send(p);

                        // Send All Player Of Room
                        p.init_plain(0x113);

                        p.WriteByte(4);
                        p.WriteByte(1);

                        p.WriteByte((int)v_element.Count);

                        for (var i = 0; i < v_element.Count; i++)
                            p.WriteBytes(v_element[i].ToArray());

                        session.Send(p);

                        // Rate Pang
                        p.init_plain(0x113);

                        p.WriteByte(4);
                        p.WriteByte(2);

                        p.WriteUInt32(RoomInfo.RatePangs);

                        session.Send(p);

                        // Send Initial of Game
                        CurrentGame.SendInitialDataAfter(session);

                        // Add Player ON GAME to ALL players
                        p.init_plain(0x113);

                        p.WriteByte(7);
                        p.WriteByte(0);

                        p.WriteString(session.UserInfo.NickName);

                        p.WriteBytes(RoomInfo.ToArray());

                        p.WriteByte(v_element.Count);

                        p.WriteInt32(Players.Count);

                        for (var i = 0; i < v_element.Count; i++)
                            p.WriteBytes(v_element[i].ToArray());

                        // Send ALL players of room exceto ele
                        SendBroadCast(GetSessions(session), p); 
                    }
                    catch (exception e)
                    {
                        throw e;
                    }
                }

            }

            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[room::startGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

    }
}
