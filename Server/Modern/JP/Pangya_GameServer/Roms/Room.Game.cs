using Microsoft.VisualBasic.FileIO;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Helpers;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;

using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Numerics;

using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms
{
    public partial class Room
    {
        public void EnterToRoom(Player session)
        {

            if (IsFull())
            {
                throw new exception("[room::enter] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na a sala[NUMERO=" + RoomInfo.RoomID + "], mas a sala ja esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2, 0));
            }

            if (session.UserInfo.Member.RoomID != -1)
            {
                throw new exception("[room::enter] [Error] Normal[UID=" + session.UserInfo.UID + "] sala[NUMERO=" + RoomInfo.RoomID + "], ja esta em outra sala[NUMERO=" + Convert.ToString(session.UserInfo.Member.RoomID) + "], nao pode entrar em outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    120, 0));
            }

            if (RoomInfo.GetRoomType() == RoomTypeFlags.GUILD_BATTLE
                && RoomInfo.GuildBattle.guild_1_uid != 0
                && RoomInfo.GuildBattle.guild_2_uid != 0
                && RoomInfo.GuildBattle.guild_1_uid != session.UserInfo.Guild.uid
                && RoomInfo.GuildBattle.guild_2_uid != session.UserInfo.Guild.uid)
            {
                throw new exception("[room::enter] [Error] Normal[UID=" + session.UserInfo.UID + "] sala[NUMERO=" + RoomInfo.RoomID + "], ja tem duas Guild e o Player que quer entrar nao é de nenhum delas. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    11000, 0));
            }

            try
            {
                AddPlayer(session, false);
                 
                RoomInfo.CurrentUsers = (byte)Players.Count;

                // Update Trofel
                if (RoomInfo.TrophyID > 0)
                {
                    UpdateTrofel();
                }

                // Acabou de criar a sala
                if (RoomInfo.OwnerUID == session.UserInfo.UID && RoomInfo.RealRoomType != (byte)RoomTypeFlags.GRAND_PRIX)
                {
                    // Update Trofel
                    if (session.UserInfo.UserCapabilities.IsGameMaster)
                    { // GM

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
                    else if (RoomInfo.GetRoomType() == RoomTypeFlags.TOURNEY || RoomInfo.RealRoomType >= (byte)RoomTypeFlags.GRAND_ZODIAC_INT)
                    {
                        UpdateTrofel();
                    }

                }
                else if (RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_PRIX)
                {
                    UpdateTrofel();
                }

                // Update Master
                // Só trocar o Master da sala se não tiver nenhum jogo inicializado
                if (CurrentGame == null
                    && Players.Count > 0
                    && session.UserInfo.UserCapabilities.IsGameMaster
                    && RoomInfo.SpecialFlag != 0x100
                    && RoomInfo.RealRoomType != (byte)RoomTypeFlags.SPECIAL_SHUFFLE_COURSE
                    && RoomInfo.RealRoomType != (byte)RoomTypeFlags.GRAND_PRIX)
                {
                    UpdateMaster(session);
                }

                // Add o Player ao jogo
                if (CurrentGame != null)
                {
                    CurrentGame.AddPlayer(session);

                    if (RoomInfo.TrophyID > 0)
                    {
                        UpdateTrofel();
                    }
                }

                try
                {
                    // Make Info Room Player
                    MakePlayerInfo(session);

                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[room::enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (RoomInfo.GetRoomType() == RoomTypeFlags.GUILD_BATTLE)
                {
                    UpdateGuild(session);
                } 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[room::enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void UpdateTrofel()
        {

            if (Players.Count() > 0 && (RoomInfo.TrophyID != TROFEL_GM_EVENT_TYPEID || RoomInfo.MaxUsers <= 30) && (RoomInfo.TimeMin > 0 && RoomInfo.RealRoomType != (byte)RoomTypeFlags.GUILD_BATTLE)
                && RoomInfo.OwnerUID != -2 || (RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_PRIX && RoomInfo.grand_prix.dados_typeid > 0))
            {

                if (CurrentGame != null)
                    CurrentGame.RequestUpdateTrofel();
                else
                {

                    uint soma = 0;

                    foreach (var el in Players)
                    {
                        if (el != null)
                            soma += (uint)((el.UserInfo.Level > 60) ? 60 : (el.UserInfo.Level > 0 ? el.UserInfo.Level - 1 : 0));

                    }
                    var new_trofel = STDA_MAKE_TROFEL(soma, Players.Count());

                    if (new_trofel > 0 && new_trofel != RoomInfo.TrophyID)
                    {

                        // Check se o trofeu anterior era o GM e se o novo não é mais, aí tira a type de GM da sala
                        if (RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && new_trofel != TROFEL_GM_EVENT_TYPEID)
                            RoomInfo.IsGameMaster = 0;

                        if (RoomInfo.TrophyID > 0)
                        {

                            RoomInfo.TrophyID = new_trofel;

                            var p = new Packet(0x97);

                            p.WriteUInt32(RoomInfo.TrophyID);

                            SendBroadCast(p);

                        }
                        else
                            RoomInfo.TrophyID = new_trofel;
                    }
                }
            }
        }
         
        public int Leave(Player session, int option)
        { 
            lock (_cs)
            {
                try
                {
                    if (!Players.Contains(session))//evita aquele bugs de sair da sala e o player não estar na sala, ai fica tentando sair e da erro, ou seja, se não tiver na sala, nem tenta sair
                    {
                        if (PlayersInfo.ContainsKey(session))
                            PlayersInfo.Remove(session);


                        _tradeShop.DestroyShop(session);

                        session.SetRoom(null);//sai 
                                               //aqui eo contrario.
                        bool isRoomEmpty = Players.Count == 0 &&(RoomInfo.OwnerUID != -2 || (IsDropRoom() && (RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_INT || RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_ADV)));

                        return isRoomEmpty ? 1 : 0;//deve ser o contrario, significa que vamos destruir a sala...
                    }

                    if (option != 0 && option != 1 && option != 0x800 && option != 10)
                    {
                        AddPlayerKicked(session.UserInfo.UID);
                    }

                    // Verifica se ele está em um jogo e tira ele
                    try
                    {
                        if (CurrentGame != null)
                        {
                            if (CurrentGame.DeletePlayer(session, option) && CurrentGame.FinishGame(session, 2))
                            {
                                FinishGame();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[room::leave][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    RemovePlayer(session); 

                    if ((RoomInfo.CurrentUsers - 1) > 0 || Players.Count == 0)
                    {
                        --RoomInfo.CurrentUsers;
                    }

                    // Sai do Team se for Match
                    if (RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
                    {
                        if (Teams.Count < 2)
                        {
                            throw new Exception($"[room::leave][Error] player[UID={session.UserInfo.UID}] tentou sair da sala[NUMERO={RoomInfo.RoomID}], mas a sala nao tem 2 times.");
                        }

                        var pPri = GetPlayerInfo(session);
                        if (pPri == null) throw new Exception("[room::leave][Error] player info não encontrado.");

                        Teams[pPri.State.Team].deletePlayer(session, option);
                    }
                    else if (RoomInfo.GetRoomType() == RoomTypeFlags.GUILD_BATTLE)
                    {
                        var pPri = GetPlayerInfo(session);
                        if (pPri == null) throw new Exception("[room::leave][Error] player info não encontrado.");

                        var guild = GuildManager.findGuildByPlayer(session);
                        if (guild == null) throw new Exception("[room::leave][Error] player não está em nenhuma Guild da sala.");

                        guild.deletePlayer(session);
                        Teams[pPri.State.Team].deletePlayer(session, option);

                        // Limpa flags
                        pPri.State.Team = 0;

                        if (guild.numPlayers() == 0)
                        {
                            if (guild.getTeam() == Guild.eTEAM.RED)
                            {
                                RoomInfo.GuildBattle.guild_1_uid = 0;
                                RoomInfo.GuildBattle.guild_1_index_mark = 0;
                                RoomInfo.GuildBattle.guild_1_mark = ""; 
                            }
                            else
                            {
                                RoomInfo.GuildBattle.guild_2_uid = 0;
                                RoomInfo.GuildBattle.guild_2_index_mark = 0;
                                RoomInfo.GuildBattle.guild_2_mark = ""; 
                            }
                            GuildManager.deleteGuild(guild);
                        }
                    }

                    // Remove do dicionário/mapa de info
                    PlayersInfo.Remove(session);

                    // Nota: DestroyShop já é chamado no bloco de saída antecipada acima.
                    // Aqui não repete para evitar dupla destruição.

                    UpdatePosition();

                    UpdateTrofel();

                    // Lógica de pacote de Kick
                    if (option == 0x800 || (option != 0 && option != 1 && option != 3))
                    {
                        int opt_kick = 0x800;
                        switch (option)
                        {
                            case 1: opt_kick = 4; break;
                            case 2: opt_kick = 2; break;
                            default: opt_kick = option; break;
                        }

                        var p = new Packet(0x7E);
                        p.WriteInt32(opt_kick);
                        session.Send(p); 
                    }

                    if (RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE)
                    {
                        session.UserInfo.PostureRoom = 0;
                        session.UserInfo.LoungeState = 0;
                    }

                    if (Players.Count > 0)
                    {
                        SendHeadRoom();
                        SendPlayerInfo(session, 2);
                    }

                    // Verificação de Master/GM para deletar a sala
                    if ((CurrentGame == null && RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && session.UserInfo.UID == RoomInfo.OwnerUID)
                        || (session.UserInfo.UserCapabilities.IsGameMaster && RoomInfo.OwnerUID == session.UserInfo.UID && RoomInfo.GetRoomType() != RoomTypeFlags.LOUNGE && RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID))
                    {
                        return 0x801; // deleta todos da sala
                    }
                    else if (CurrentGame == null)
                    {
                        UpdateMaster(null);
                    }
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[room::leave][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Retorno final baseado na lógica original

                bool condition = Players.Count > 0 || (RoomInfo.OwnerUID == -2 && (!IsDropRoom() || (RoomInfo.GetRoomType() >= RoomTypeFlags.GRAND_ZODIAC_INT && RoomInfo.GetRoomType() <= RoomTypeFlags.GRAND_ZODIAC_ADV)));
                return condition ? 0 : 1;
            }
        }

        public void FinishGame()
        {

            try
            {
                if (CurrentGame != null)
                {

                    var toAdd = new List<(Player player, PlayerRoomInfo info)>();
                    // Zera Player Flags
                    var player_info = PlayersInfo.ToList();
                    foreach (var el in player_info)
                    {
                        // Update Place Player
                        if (RoomInfo.GetRoomType() == RoomTypeFlags.PRACTICE || RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                        {
                            el.Value.LadderGrade = 2;
                        }
                        else
                        {
                            el.Value.LadderGrade = 0;
                        }

                        el.Value.State.Sleep = 0;

                        // Aqui só zera quem não é Master da sala, o Master deixa sempre Ready
                        if (RoomInfo.OwnerUID == el.Key.UserInfo.UID)
                        {
                            el.Value.State.Ready = 1;
                        }
                        else
                        {
                            el.Value.State.Ready = 0;
                        }

                        // Update Player info
                        UpdatePlayerInfo(el.Key);

                        // SLast update on room
                        SendPlayerInfo(el.Key, 3);
                    }

                    // Atualiza type da sala, só não atualiza se for GM evento ou GZ Event e SSC
                    if (!(RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID || RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE || RoomInfo.OwnerUID == -2))
                        RoomInfo.StateRoom = 1; //em espera

                    // Att Exp Rate, e Pang Rate, que criou a sala, att ele também quando começa o jogo

                    RoomInfo.RateExperience = (uint)GameServer.Instance.getInfo().Rate.Experience;
                    RoomInfo.RatePangs = (uint)GameServer.Instance.getInfo().Rate.Pang;
                    RoomInfo.IsAngelQuiterEvent = GameServer.Instance.getInfo().Rate.AngelEvent.IsTrue();


                    // Update Course of Hole
                    if (RoomInfo.GetMap() >= 0x7F) // Random Course With Course already draw
                        RoomInfo.CourseIndex = RoomCourseFlags.UNK; // Random Course standard


                    // Update Master da sala
                    UpdateMaster(null);

                    if (RoomInfo.OwnerUID == -2)
                        RoomInfo.OwnerUID = -1; // pode deletar a sala quando sair todos


                    if (Players.Count > 0)
                    {
                        // Atualiza info da sala para quem está na sala 
                        SendUpdateRoom();
                    }

                    // limpa lista de Player kikados
                    ClearPlayersKicked();

                    // Verifica se o Bot Tourney está ativo, kika bot e limpa a type
                    if (BotTourney)
                    {

                        var pMaster = FindMaster();

                        if (pMaster != null)
                        {

                            try
                            {
                                // Kick Bot
                                // Atualiza os Player que estão na sala que o Bot sai por que ele é só visual
                                SendPlayerInfo(pMaster, 0);

                            }
                            catch (exception e)
                            {

                                _smp.LogManager.Instance.push(new AppMessage("[room::finish_game::KickBotTourney][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                        }

                        BotTourney = false;
                    }
                    if (CurrentGame != null)
                    {
                        CurrentGame.GameStop(); // desliga o relógio
                    }

                    // Corrigido: CurrentGame pode ter sido anulado por outra thread entre
                    CurrentGame?.Dispose();
                    CurrentGame = null;
                }
            }
            catch (Exception)
            { 
                throw;
            }
        }
         
        public void UpdateGuild(Player session)
        {
            if (session.UserInfo.Guild.uid == -1)
                throw new Exception($"[Channel::UpdateGuild] [Error] Normal[UID={session.UserInfo.UID}] player nao esta em uma Guild.");

            PlayerRoomInfo pri = GetPlayerInfo(session);

            if (pri == null)
                throw new Exception($"[Channel::UpdateGuild] [Error] Normal[UID={session.UserInfo.UID}] nao tem o info do player na sala[NUMERO={RoomInfo.RoomID}]. Hacker ou Bug.");

            Guild guild = null;

            if (RoomInfo.GuildBattle.guild_1_uid == 0 && RoomInfo.GuildBattle.guild_2_uid != session.UserInfo.Guild.uid)
            {
                RoomInfo.GuildBattle.guild_1_uid = session.UserInfo.Guild.uid;
                RoomInfo.GuildBattle.guild_1_nome = session.UserInfo.Guild.name;
                RoomInfo.GuildBattle.guild_1_mark = session.UserInfo.Guild.mark_emblem;
                RoomInfo.GuildBattle.guild_1_index_mark = (ushort)session.UserInfo.Guild.index_mark_emblem;

                pri.State.Team = 0;
                guild = GuildManager.addGuild(Guild.eTEAM.RED, RoomInfo.GuildBattle.guild_1_uid);
            }
            else if (RoomInfo.GuildBattle.guild_1_uid == session.UserInfo.Guild.uid)
            {
                pri.State.Team = 0;
                guild = GuildManager.findGuildByTeam(Guild.eTEAM.RED);
            }
            else if (RoomInfo.GuildBattle.guild_2_uid == 0)
            {
                RoomInfo.GuildBattle.guild_2_uid = session.UserInfo.Guild.uid;
                RoomInfo.GuildBattle.guild_2_nome = session.UserInfo.Guild.name;
                RoomInfo.GuildBattle.guild_2_mark = session.UserInfo.Guild.mark_emblem;
                RoomInfo.GuildBattle.guild_2_index_mark = (ushort)session.UserInfo.Guild.index_mark_emblem;

                pri.State.Team = 1;
                guild = GuildManager.addGuild(Guild.eTEAM.BLUE, RoomInfo.GuildBattle.guild_2_uid);
            }
            else
            {
                pri.State.Team = 1;
                guild = GuildManager.findGuildByTeam(Guild.eTEAM.BLUE);
            }

            if (guild != null)
            {
                guild.addPlayer(session);
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[room::updateGuild][Warning] Normal[UID={session.UserInfo.UID}] tentou entrar em uma Guild da sala[NUMERO={RoomInfo.RoomID}], mas nao conseguiu criar ou achar nenhum Guild na sala. Bug.",
                   type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            Teams[pri.State.Team].addPlayer(session);
        }

        private void InitTeams()
        {

            // Limpa teans, se tiver teans inicilizados já
            ClearTeams();

            // Init Teans
            Teams.Add(new Team(0));
            Teams.Add(new Team(1));

            PlayerRoomInfo pPri = null;

            // Add Players All Seus Respectivos teans
            foreach (var el in Players)
            {

                if ((pPri = GetPlayerInfo(el)) == null)
                {
                    throw new exception("[room::init_teans] [Error] nao encontrou o info do Normal[UID=" + Convert.ToString(el.UserInfo.UID) + "] na sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1504, 0));
                }

                Teams[pPri.State.Team].addPlayer(el);
            }

        }

        public int LeaveAll(int option)
        {
            // Percorre de trás para frente
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                var player = Players[i];
                if (player != null)
                {
                    try
                    {
                        Leave(player, option);
                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[room::leaveAll] [Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            return 0;
        }

        public InviteChannelInfo AddInvited(uint uidHasInvite, Player session)
        {

            if (IsFull())
            {
                throw new exception("[room::addInvited] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na a sala[NUMERO=" + RoomInfo.RoomID + "], mas a sala ja esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2, 0));
            }

            if (FindIndexSession(uidHasInvite) == (int)~0)
            {
                throw new exception("[room::addInvited] [Error] quem convidou[UID=" + Convert.ToString(uidHasInvite) + "] o Normal[UID=" + session.UserInfo.UID + "] para a sala nao esta na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2010, 0));
            }

            var s = FindSessionByUid(session.UserInfo.UID);

            if (s != null)
            {
                throw new exception("[room::addInvited] [Error] Normal[UID=" + Convert.ToString(uidHasInvite) + "] tentou adicionar o Invite[UID=" + session.UserInfo.UID + "] a sala, mas ele ja esta na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2001, 0));
            }
            AddPlayer(session, true);

            ++RoomInfo.CurrentUsers;

            PlayerRoomInfo pri = null;

            try
            {

                // Make Info Room Player Invited
                pri = MakePlayerInvitedInfo(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[room::addInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            if (pri == null)
            {

                // Pop_back
                Players.Remove(Players.Last());



                throw new exception("[[room::addInvited] [Error] Normal[UID=" + Convert.ToString(uidHasInvite) + "] tentou adicionar o Invite[UID=" + session.UserInfo.UID + "] a sala, nao conseguiu criar o Player Room Info Invited do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2002, 0));
            }

            // Add Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo
            {
                room_number = RoomInfo.RoomID,
                invite_uid = uidHasInvite,
                invited_uid = session.UserInfo.UID,
                time = new SystemTime(DateTime.Now)
            };

            Invites.Add(ici);
            // End Add Invite Channel Info

            // Update Char Invited ON ROOM
            var p = new Packet(0x48);

            p.WriteByte(1);
            p.WriteInt16(-1);

            p.WriteBytes(pri.ToArray(WithCharacter: true));

            p.WriteByte(0); // Final Packet
            SendBroadCast(p);
            return ici;
        }

        public InviteChannelInfo GetInvited(Player session)
        {
            return Invites.FirstOrDefault(el =>
            {
                return (el.room_number == RoomInfo.RoomID && el.invited_uid == session.UserInfo.UID);
            });
        }

        public InviteChannelInfo GetInvited(uint uid)
        {
            return Invites.FirstOrDefault(el => el.room_number == RoomInfo.RoomID && el.invited_uid == uid); ;
        }

        public InviteChannelInfo DeleteInvited(Player session)
        {

            var it = PlayersInfo.FirstOrDefault(c => c.Key.UserInfo.UID == session.UserInfo.UID);

            if (it.Key == null && GetInvited(session) == null)
                throw new exception("[room::DeleteInvited] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou deletar Invite,"
                    + " mas nao tem o info do Invite na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 2003, 0));


            int index = FindIndexSession(session);

            if (index == -1 && GetInvited(session) == null)
            {
                throw new exception("[room::DeleteInvited] [Error] session[UID=" + session.UserInfo.UID + "] nao existe no vector de sessions da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    5, 0));
            }
            
            RemovePlayer(session);

            --RoomInfo.CurrentUsers;

            PlayersInfo.Remove(session);

            // Update Position all Players
            UpdatePosition();

            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();


            var itt = GetInvited(session);

            if (itt != null)
            {

                ici = itt;

                Invites.Remove(itt);

            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Warning] Normal[UID=" + session.UserInfo.UID + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new Packet((ushort)0x130);

            p.WriteUInt32(session.UserInfo.UID);
            SendBroadCast(p);

            _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Info] Deleteou um convite[Convidado=" + session.UserInfo.UID + "] na Sala[NUMERO=" + RoomInfo.RoomID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


            return ici;
        }

        public InviteChannelInfo _DeleteInvited(Player session)
        {


            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();


            var itt = GetInvited(session);

            if (itt != null)
            {

                ici = itt;

                Invites.Remove(itt);

            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Warning] Normal[UID=" + session.UserInfo.UID + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new Packet((ushort)0x130);

            p.WriteUInt32(session.UserInfo.UID);
            SendBroadCast(p);

            _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Info] Deleteou um convite[Convidado=" + session.UserInfo.UID + "] na Sala[NUMERO=" + RoomInfo.RoomID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


            return ici;
        }

        public InviteChannelInfo DeleteInvited(uint uid)
        {
            if (uid == 0)
            {
                throw new exception("[room::DeleteInvited] [Error] UID is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2005, 0));
            }

            var it = PlayersInfo.FirstOrDefault(el => el.Value.Invite == 1 && el.Value.UID == uid);

            // Corrigido: era PlayersInfo.Last().Key — crashes em dicionário vazio e não é
            // um sentinel válido para "não encontrado". Verificação correta é it.Key == null.
            if (it.Key == null)
            {
                throw new exception("[room::DeleteInvited] [Error] Normal[UID=" + Convert.ToString(uid) + "] tentou deletar Invite, mas nao tem o info do Invite na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2003, 0));
            }

            int index = FindIndexSession(uid);

            // Corrigido: (int)~0 == -1, comparação redundante — simplificado para -1
            if (index == -1)
            {
                throw new exception("[room::DeleteInvited] [Error] session[UID=" + Convert.ToString(uid) + "] nao existe no vector de sessions da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    5, 0));
            }

            Players.RemoveAt(index);

            --RoomInfo.CurrentUsers;

            PlayersInfo.Remove(it.Key);

            // Update Position all Players
            UpdatePosition();

            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();

            var itt = Invites.FirstOrDefault(el => el.room_number == RoomInfo.RoomID && el.invited_uid == uid);


            if (itt != null)
            {

                ici = itt;

                Invites.Remove(itt);

            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Warning] Normal[UID=" + Convert.ToString(uid) + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new Packet((ushort)0x130);

            p.WriteUInt32(uid);
            SendBroadCast(p);

            _smp.LogManager.Instance.push(new AppMessage("[room::DeleteInvited][Info] Deleteou um convite[Convidado=" + Convert.ToString(uid) + "] na Sala[NUMERO=" + RoomInfo.RoomID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            return ici;
        }


        private void CalcRainLounge()
        {

            // Só calcRainLounge se for Lounge
            if (RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE)
            {

                WeatherChatRoom = 0; // Good Weather

                short rate_rain = GameServer.Instance.getInfo().Rate.Rain;

                LotterySystem loterry = new LotterySystem();

                uint rate_good_weather = (uint)((rate_rain <= 0) ? 1000 : ((rate_rain < 1000) ? 1000 - rate_rain : 1));

                loterry.Add(rate_good_weather, 0);
                loterry.Add(rate_good_weather, 0);
                loterry.Add(rate_good_weather, 0);
                loterry.Add((uint)rate_rain, 2);

                var lc = loterry.SpinRoleta();

                if (lc != null && Convert.ToInt32(lc.Value) > 0)
                {
                    WeatherChatRoom = (byte)Convert.ToInt32(lc.Value);
                }
            }
        }

        private void ClearTeams()
        {
            if (Teams.Any())
            {
                Teams.Clear();
            }
        }

        private void MakeBotVisual(Player session)
        {
            // Add Bot
            List<PlayerRoomInfo> v_element = new List<PlayerRoomInfo>();
            PlayerRoomInfo pri = new PlayerRoomInfo();

            try
            {
                Players.ForEach(el =>
                {
                    var tmppri = GetPlayerInfo(el);
                    if (tmppri != null)
                    {
                        v_element.Add(tmppri);
                    }
                });


                if (v_element.Count == 0)
                {
                    throw new exception("[room::MakeBot] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou criar Bot na sala[NUMERO=" + RoomInfo.RoomID + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nao nenhum Player na sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 5000));
                }

                // Inicializa os dados do Bot
                pri.UID = session.UserInfo.UID;
                pri.OID = session.ConnectionID;
                pri.State.Ready = 1;
                pri.RankPosition = 0;
                pri.CharacterID = 0x4000000;
                pri.TitleSkin = 0x39800013; // Title Helper
                pri.NickName = "\\1Bot";
                pri.DisplayID = "@NT_" + pri.NickName; 
                // Add o Bot a sala, só no visual
                v_element.Add(pri);

                // Packet
                var p = new Packet();

                if (RoomInfo.GetRoomType() != RoomTypeFlags.STROKE)
                {
                    // Option 0, passa todos que estão na sala
                    if (Handle_PACKET_RESPONSE.MakePlayerRoomInfo(p, session, v_element, 0x100))
                        SendBroadCast(p);

                    // Option 1, passa só o Player que entrou na sala, nesse caso foi o Bot
                    if (Handle_PACKET_RESPONSE.MakePlayerRoomInfo(p, session, [pri], 0x101))
                        SendBroadCast(p);
                } 
                // Criou Bot com sucesso
                BotTourney = true;
            }
            catch (exception)
            { 
                throw;
            }
        }


        public void MakeRoomBot(Player session)
        {
            var p = new Packet();

            try
            {
                if (IsRoomGM())
                {
                    // SLast Message
                    p.init_plain(0x40); // Msg to Chat of Player

                    p.WriteByte(7); // Notice

                    p.WriteString("@NOTICE");
                    p.WriteString("[ \\2Premium ] \\c0xff00ff00\\cNot Need add bot, auto start.");
                    session.Send(p);
                    return;
                }

                else
                {
                    // Bot Ticket TypeId

                    // Premium User Não precisa de ticket não
                    if (session.UserInfo.UserCapabilities.UserPremium || session.UserInfo.UserCapabilities.IsGameMaster)
                    {


                        // Add Bot Tourney Visual para a sala
                        MakeBotVisual(session);

                        // SLast Message
                        p.init_plain(0x40); // Msg to Chat of Player

                        p.WriteByte(7); // Notice

                        p.WriteString("@SuperSS");
                        p.WriteString("[ \\2Premium ] \\c0xff00ff00\\cBot was created.");
                        session.Send(p);
                    }
                    else
                    {

                        // Verifica se ele tem o ticket para criar o Bot se não manda mensagem dizenho que ele não tem ticket para criar o bot
                        var pWi = session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID) != null ? session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID) : session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID2);

                        if (pWi == null)
                        {

                            // Não tem ticket bot suficiente, manda mensagem
                            // SLast Message
                            p.init_plain(0x40); // Msg to Chat of Player

                            p.WriteByte(7); // Notice

                            p.WriteString("@SuperSS");
                            p.WriteString("\\c0xffff0000\\cYou do not have enough ticket to create the Bot.");
                            session.Send(p);
                        }
                        else
                        {

                            // Add Bot Tourney Visual para a sala
                            MakeBotVisual(session);

                            // SLast Message
                            p.init_plain(0x40); // Msg to Chat of Player

                            p.WriteByte(7); // Notice

                            p.WriteString("@SuperSS");
                            p.WriteString("[ \\2Premium ] \\c0xff00ff00\\cBot was created.");
                            session.Send(p);
                        }
                    }
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[room::MakeBot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // SLast Message
                p.init_plain(0x40); // Msg to Chat of Player

                p.WriteByte(7); // Notice

                p.WriteString("@SuperSS");
                p.WriteString("\\c0xffff0000\\cError creating Bot.");
                session.Send(p);
            }
        }

        public void SendHeadRoom()
        {
            SendBroadCast(Handle_PACKET_RESPONSE.pacote04A(RoomInfo, -1/*valor constante*/));
        }

        public void SendPlayerInfo(Player session, int _option)
        {

            int option = !(RoomInfo.GetRoomType() == RoomTypeFlags.STROKE ||
                           RoomInfo.GetRoomType() == RoomTypeFlags.MATCH ||
                           RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE ||
                           RoomInfo.GetRoomType() == RoomTypeFlags.PANG_BATTLE) ? 0x100 : 0;

            option += _option;

            if (option == 0 && RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE)
                option = 7;

            List<PlayerRoomInfo> v_element = new List<PlayerRoomInfo>();
            PlayerRoomInfo pri = null;

            try
            {

                foreach (var sess in Players)
                {
                    pri = GetPlayerInfo(sess);
                    if (pri != null)
                        v_element.Add(pri);
                }

                pri = GetPlayerInfo(session);

                if (pri == null && _option != 2)
                    return;

                var p = new Packet();

                if (Handle_PACKET_RESPONSE.MakePlayerRoomInfo(p, session, (_option == 1 || _option == 4 || _option == 0x103) ? [pri] : v_element, option))
                    SendBroadCast(p);
            }
            catch
            {
                throw;
            }
            // Exceções propagam diretamente — o catch-rethrow vazio original não adicionava valor.
        }

        public void SendPlayerStateLounge(Player session)
        { 
            if (RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE)
            {
                var it = session.UserInfo.FindStateCharacterLounger(session.Inventory.UserEquippedItem.CharacterEquiped.id);
                if (it == null)
                {
                    session.UserInfo.CharacterLoungeStates.Add(session.Inventory.UserEquippedItem.CharacterEquiped.id, new StateCharacterLounge());
                    it = new StateCharacterLounge();
                }
                 
                SendBroadCast(Handle_PACKET_RESPONSE.pacote196(session, it));
            }
        }

        public void SendWeatherLounge(Player session)
        {

            if (RoomInfo.GetRoomType() == RoomTypeFlags.LOUNGE)
            {

                // Envia o tempo(weather) do Lounge só se ele for diferente de tempo bom
                if (WeatherChatRoom != 0)
                {

                    var p = new Packet((ushort)0x9E);

                    p.WriteUInt16(WeatherChatRoom);
                    p.WriteByte(0); // ServerFlag (acho), vou colocar 0 o padrão, colocou 1 aqui só quando eu mudou com o comando GM
                    session.Send(p);
                }
            }
        }

        public void UpdateMaster(Player session)
        {
            var p = new Packet();
            try
            {
                Player master = FindSessionByUid((uint)RoomInfo.OwnerUID);

                if (session != null && session.UserInfo.UserCapabilities.IsGameMaster && RoomInfo.OwnerUID != -2)
                {
                    // Só troca o Master se ele saiu da sala ou se ele não for GM
                    if (master == null || !(master.UserInfo.UserCapabilities.IsGameMaster/* & 4*/))
                    {
                        RoomInfo.OwnerUID = (int)session.UserInfo.UID;
                        RoomInfo.SpecialFlag = 0x100; // GM

                        if (master != null)
                        {
                            UpdatePlayerInfo(master);
                            p.init_plain(0x78);
                            p.WriteInt32(master.ConnectionID);
                            p.WriteByte((byte)~GetPlayerInfo(master).State.Ready);

                            SendBroadCast(p);
                        }

                        p = new Packet();
                        p.init_plain(0x7C);
                        p.WriteInt32(session.ConnectionID);
                        p.WriteInt16(0);

                        SendBroadCast(p);
                    }
                }
                else if (master == null && Players.Count > 0 && RoomInfo.OwnerUID != -2)
                {
                    if (RoomInfo.GetRoomType() != RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_PRIX)
                    {
                        // Find GM 
                        var i = Players.FirstOrDefault(pl => pl.UserInfo.UserCapabilities.IsGameMaster);

                        if (i != null)
                            master = i;
                        else
                            master = Players[0];

                        RoomInfo.OwnerUID = (int)master.UserInfo.UID;
                        RoomInfo.SpecialFlag = (short)(master.UserInfo.UserCapabilities.IsGameMaster ? 0x100 : 0);

                        UpdatePlayerInfo(master);

                        p = new Packet(0x7C);
                        p.WriteInt32(master.ConnectionID);
                        p.WriteInt16(0);

                        SendBroadCast(p);
                    }
                }
            }
            catch
            {
                throw;
            } 
        }


        public void SendMakeRoom(Player session)
        { 
            session.Send(Handle_PACKET_RESPONSE.pacote049(this, 0));
        }

        public void SendUpdateRoom()
        {
            SendBroadCast(Handle_PACKET_RESPONSE.pacote04A(RoomInfo, -1/*valor constante*/));
        }

        private void AddPlayerKicked(uint uid)
        {
            if (IsKickedPlayer(uid))
                _smp.LogManager.Instance.push(new AppMessage("[room::addPlayerKicked] [Error][Warning] Normal[UID=" + (uid) + "] ja foi chutado da sala[NUMERO="
                    + (RoomInfo.RoomID) + "]", type_msg.CL_FILE_TIME_LOG_AND_CONSOLE));
            else
                PlayersKickeds[uid] = true;
        }
         
        private PlayerRoomInfo MakePlayerInfo(Player session)
        {
            PlayerRoomInfo pri = new();

            // Player Room Info Init
            pri.OID = session.ConnectionID;
            pri.NickName = session.UserInfo.NickName;
            pri.GuildName = session.UserInfo.Guild.name;
            pri.RankPosition = (byte)(GetPosition(session) + 1);
            pri.Capability = session.UserInfo.UserCapabilities;
            pri.TitleSkin = session.Inventory.UserEquipment.m_title;
            pri.DisplayID = "@NT_" + session.UserInfo.NickName;
            if (session.Inventory.UserEquippedItem.CharacterEquiped != null)
                pri.CharacterID = session.Inventory.UserEquippedItem.CharacterEquiped._typeid;

            pri.ItemSkin = session.Inventory.UserEquipment.skin_typeid;

            pri.ItemSkin[4] = 0;

            if (GetMaster() == session.UserInfo.UID)
            {
                pri.State.Master = 1;
                pri.State.Ready = 1;// Sempre está pronto(Ready) o Master
            }
            pri.State.Gender = session.UserInfo.Member.Gender;

            if (RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
            {
                if (Players.Count > 1)
                {
                    if (Teams[0].getCount() >= 2 && Teams[1].getCount() >= 2)
                    {
                        throw new exception("[room::MakePlayerInfo] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar em time para todos os times da sala estao cheios. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1500, 0));
                    }
                    else if (Teams[0].getCount() >= 2)
                    {
                        pri.State.Team = 1;
                    }
                    else if (Teams[1].getCount() >= 2)
                    {
                        pri.State.Team = 0;
                    }
                    else
                    {
                        var targetSession = (Players.Count == 2) ? Players[0] : (Players.Count > 2 ? Players[1] : null);
                        var pPri = GetPlayerInfo(targetSession);

                        if (pPri == null)
                        {
                            throw new exception("[room::MakePlayerInfo] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar em um time, mas o ultimo player da sala, nao tem um info no sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1501, 0));
                        }

                        pri.State.Team = (byte)~pPri.State.Team;
                    }
                }
                else
                {
                    pri.State.Team = 0;
                }

                Teams[pri.State.Team].addPlayer(session);
            }
            else if (RoomInfo.GetRoomType() != RoomTypeFlags.GUILD_BATTLE)
            {
                pri.State.Team = (byte)((pri.RankPosition - 1) % 2);
            }

            if (session.UserInfo.Level >= 6 && session.UserInfo.Statistics.jogado >= 50)
            {
                float rate = session.UserInfo.Statistics.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                {
                    pri.State.Wings = 1;
                }
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                {
                    pri.State.Quit10Porcent = 1;
                }
                else if (rate >= QUITER_ICON_2)
                {
                    pri.State.Quit20Porcent = 1;
                }
            }

            pri.GameLevel = session.UserInfo.Member.GameLevel;

            if (session.Inventory.UserEquippedItem.CharacterEquiped != null && session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
                pri.StateAngel.Value = session.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped();
            else
                pri.StateAngel.Value = 0;

            pri.LadderGrade = 10;
            pri.GuildIndex = session.UserInfo.Guild.uid;
            pri.GuildMark = session.UserInfo.Guild.mark_emblem;
            pri.GuildMarkIndex = session.UserInfo.Guild.index_mark_emblem;
            pri.UID = session.UserInfo.UID;
            pri.Action.Animation = session.UserInfo.LoungeState;
            pri.Action.SubRoomID = 0;
            pri.Action.Posture = session.UserInfo.PostureRoom; 
            pri.LocationInfo.X += session.UserInfo.CurrentLocation.x;
            pri.LocationInfo.Z += session.UserInfo.CurrentLocation.z;
            pri.LocationInfo.Y += session.UserInfo.CurrentLocation.y;
            pri.ShopRoom = _tradeShop.getPersonShop(session);

            if (session.Inventory.UserEquippedItem.MascotEquiped != null)
                pri.MascotID = session.Inventory.UserEquippedItem.MascotEquiped._typeid;

            pri.ItemSpecial = session.Inventory.CheckHaveItemBoost();
            pri.ChannelingFlag = 0;
            pri.Invite = 0;
            pri.AvengeScore = session.UserInfo.Statistics.getMediaScore();

            if (session.Inventory.UserEquippedItem.CharacterEquiped != null)
                pri.CharacterInfo = session.Inventory.UserEquippedItem.CharacterEquiped;
            if (!PlayersInfo.TryAdd(session, pri))
            {
                if (PlayersInfo.TryGetValue(session, out var existingPri))
                {
                    if (existingPri.UID != session.UserInfo.UID)
                    {
                        try
                        {
                            var pri_ant = PlayersInfo[session];
                            PlayersInfo[session] = pri;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[room::MakePlayerInfo] [Error][Warning] Normal[UID={session.UserInfo.UID}], nao conseguiu atualizar o PlayerRoomInfo da session para o novo PlayerRoomInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            throw;
                        }
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[room::MakePlayerInfo][Info] Normal[UID={session.UserInfo.UID}] nao conseguiu adicionar o PlayerRoomInfo da session, por que ja tem o mesmo PlayerRoomInfo no map.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[room::MakePlayerInfo] [Error] nao conseguiu inserir o pair de PlayerInfo do Normal[UID={session.UserInfo.UID}] no map de player info do room. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return pri;
        }

        private PlayerRoomInfo MakePlayerInvitedInfo(Player session)
        {

            PlayerRoomInfo pri = new PlayerRoomInfo();

            // Player Room Info Init
            pri.OID = session.ConnectionID;
            pri.RankPosition = (byte)(GetPosition(session) + 1); // posição na sala 
            pri.LadderGrade = 10; // 0x0A dec"10" session.PlayerUserStatistics.LadderGrade, pode ser lugar[LadderGrade]

            pri.UID = session.UserInfo.UID;

            pri.Invite = 1; // ServerFlag Convidado, [Não sei bem por que os que entra na sala Normal tem valor igual aqui, já que é type de Invite waiting], Valor constante da sala para os players(ACHO)

            // Check inset pair in map of room player info
            if (PlayersInfo.ContainsKey(session))
            {
                try
                {

                    // pega o antigo PlayerRoomInfo para usar no Log
                    var pri_ant = PlayersInfo[session];

                    // Novo PlayerRoomInfo
                    PlayersInfo[session] = pri;

                }
                catch (IndexOutOfRangeException)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[room::MakePlayerInfo] [Error][Warning] Normal[UID=" + session.UserInfo.UID + "], nao conseguiu atualizar o PlayerRoomInfo da session para o novo PlayerRoomInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    throw;
                }
            }
            else
                PlayersInfo.Add(session, pri);

            return pri;
        }


        public void UpdatePlayerInfo(Player session)
        {
            PlayerRoomInfo pri = new PlayerRoomInfo();
            PlayerRoomInfo _pri = null;
            try
            {

                if ((_pri = GetPlayerInfo(session)) == null)
                    return;//antes dava exception, agora eu so retorno....

                // Copia do que esta no map
                pri = _pri;

                // Player Room Info Update
                pri.OID = session.ConnectionID;

                pri.RankPosition = (byte)(GetPosition(session) + 1); // posição na sala
                pri.Capability = session.UserInfo.UserCapabilities;
                pri.TitleSkin = session.Inventory.UserEquipment.m_title;

                if (session.Inventory.UserEquippedItem.CharacterEquiped != null)
                    pri.CharacterID = session.Inventory.UserEquippedItem.CharacterEquiped._typeid;


                pri.ItemSkin[4] = 0; // Aqui tem que ser zero, se for outro valor não mostra a imagem do character equipado

                if (GetMaster() == session.UserInfo.UID)
                {
                    pri.State.Master = 1;
                    pri.State.Ready = 1; // Sempre está pronto(Ready) o Master
                }
                else
                {

                    // Só troca o estado de pronto dele na sala, se anterior mente ele era Master da sala ou não estiver pronto
                    if (pri.State.Master == 1 || !(pri.State.Ready == 1))
                    {
                        pri.State.Ready = 0;
                    }

                    pri.State.Master = 0;
                }

                pri.State.Gender = session.UserInfo.Member.Gender;

                // Update Team se for Match
                if (RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
                {

                    // Verifica se o Player está em algum Team para atualizar o Team dele se ele não estiver em nenhum
                    var Player_team = pri.State.Team;
                    Player p_seg_team = null;

                    // atualizar o Team do Player a type de Team dele não bate com o Team dele
                    if (Teams[Player_team].findPlayerByUID(pri.UID) == null && (p_seg_team = Teams[~Player_team].findPlayerByUID(pri.UID)) == null)
                    {

                        // Player não está em nenhum Team
                        if (Players.Count > 1)
                        {

                            if (Teams[0].getCount() >= 2 && Teams[1].getCount() >= 2)
                            {
                                throw new exception("[room::updatePlayerInfo] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar em time para todos os times da sala estao cheios. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    1500, 0));
                            }
                            else if (Teams[0].getCount() >= 2)
                            {
                                pri.State.Team = 1; // Blue
                            }
                            else if (Teams[1].getCount() >= 2)
                            {
                                pri.State.Team = 0; // Red
                            }
                            else
                            {

                                var pPri = GetPlayerInfo((Players.Count == 2) ? Players.FirstOrDefault() : (Players.Count > 2 ? (Players.Skip(1).FirstOrDefault()) : null));

                                if (pPri == null)
                                {
                                    throw new exception("[room::updatePlayerInfo] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar em um time, mas o ultimo Player da sala, nao tem um info no sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                        1501, 0));
                                }

                                pri.State.Team = (byte)~pPri.State.Team;
                            }

                        }
                        else
                        {
                            pri.State.Team = 0;
                        }

                        Teams[pri.State.Team].addPlayer(session);

                    }
                    else if (p_seg_team != null)
                    {
                        // a type de Team do Player está errada, ele está no outro Team, ajeita
                        pri.State.Team = (byte)~Player_team;
                    }
                }
                else if (RoomInfo.RealRoomType != (byte)RoomTypeFlags.GUILD_BATTLE) // O Guild Battle tem sua própria função para inicializar e atualizar o Team e os dados da Guild
                {
                    pri.State.Team = Convert.ToByte(((pri.RankPosition > 0 ? pri.RankPosition : 1) - 1) % 2);
                }

                // Só faz calculo de Quita Rate depois que o Player
                // estiver no Level Beginner E e jogado 50 games
                if (session.UserInfo.Level >= 6 && session.UserInfo.Statistics.jogado >= 50)
                {
                    float rate = session.UserInfo.Statistics.getQuitRate();

                    if (rate < GOOD_PLAYER_ICON)
                    {
                        pri.State.Wings = 1;
                    }
                    else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                    {
                        pri.State.Quit10Porcent = 1;
                    }
                    else if (rate >= QUITER_ICON_2)
                    {
                        pri.State.Quit20Porcent = 1;
                    }
                }

                pri.GameLevel = session.UserInfo.Member.GameLevel;

                if (session.Inventory.UserEquippedItem.CharacterEquiped != null && session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
                    pri.StateAngel.Value = session.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped();
                else
                    pri.StateAngel.Value = 0;

                pri.LadderGrade = 10; // 0x0A dec"10" session.PlayerUserStatistics.LadderGrade
                pri.GuildIndex = session.UserInfo.Guild.uid;

                pri.UID = session.UserInfo.UID;
                pri.Action.Animation = session.UserInfo.LoungeState;
                pri.Action.SubRoomID = 0; // Ví Players com valores 2 e 4 e 0
                pri.Action.Posture = session.UserInfo.PostureRoom;
                pri.LocationInfo.X += session.UserInfo.CurrentLocation.x;
                pri.LocationInfo.Z += session.UserInfo.CurrentLocation.z;
                pri.LocationInfo.Y += session.UserInfo.CurrentLocation.y;

                // Personal Shop
                pri.ShopRoom = _tradeShop.getPersonShop(session);

                if (session.Inventory.UserEquippedItem.MascotEquiped != null)
                    pri.MascotID = session.Inventory.UserEquippedItem.MascotEquiped._typeid;

                pri.ItemSpecial = session.Inventory.CheckHaveItemBoost();
                pri.ChannelingFlag = 0;

                // Só atualiza a type de Invite se for diferente de 1, por que 1 ele é Invite
                if (pri.Invite != 1)
                    pri.Invite = 0; // ServerFlag Convidado, [Não sei bem por que os que entra na sala Normal tem valor igual aqui, já que é type de Invite waiting], Valor constante da sala para os Players(ACHO)

                pri.AvengeScore = session.UserInfo.Statistics.getMediaScore();

                if (session.Inventory.UserEquippedItem.CharacterEquiped != null)
                    pri.CharacterInfo = session.Inventory.UserEquippedItem.CharacterEquiped;

                // Salva novamente
                PlayersInfo[session] = pri;
            }
            catch
            {
                throw;
            } 
        }

        public Team GetTeamInfo(byte team)
        {
            return Teams[team];
        }

        public int TeamCount() => Teams.Count;

        public void AddPlayerTeam(Player session, byte team)
        { Teams[team].addPlayer(session); }

        public void DeletePlayerTeam(Player session, byte opt)
        { Teams[GetPlayerInfo(session).State.Team].deletePlayer(session, opt); }


        public void SendTimeGame(Player session)
        {
            if (!session.getState())
            {
                throw new exception("[Room::RequestSendTimeGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            Packet p = new();

            try
            {
                if (IsKickedPlayer(session.UserInfo.UID))
                {
                    throw new exception("[Room::RequestSendTimeGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + RoomInfo.RoomID + "] ja em jogo, mas o player foi chutado da sala antes de comecar o jogo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2704, 7));
                }

                if (CurrentGame == null)
                {
                    throw new exception("[Room::RequestSendTimeGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou pegar o tempo do Tourney que comecou na sala[NUMERO=" + RoomInfo.RoomID + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2705, 1));
                }

                CurrentGame.RequestSendTimeGame(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Room::RequestSendTimeGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));

                session.Send(p);
            }
        }

        public bool EnterGameAfterStarted(Player session)
        {
            if (!session.getState())
            {
                throw new exception("[Room::RequestEnterGameAfterStarted] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            Packet p = new();

            bool ret = false;

            try
            {

                if (IsKickedPlayer(session.UserInfo.UID))
                {
                    throw new exception("[Room::RequestEnterGameAfterStarted][Warning] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + RoomInfo.RoomID + "] ja em jogo, mas o player foi chutado da sala antes de comecar o jogo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2704, 7));
                }

                if (CurrentGame == null)
                {
                    throw new exception("[Room::RequestEnterGameAfterStarted] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + RoomInfo.RoomID + "] ja em jogo, mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2705, 1));
                }

                if (IsGamingBefore(session.UserInfo.UID))
                {
                    throw new exception("[Room::RequestEnterGameAfterStarted][Warning] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + RoomInfo.RoomID + "] ja em jogo, mas o player ja tinha jogado nessa sala e saiu, e nao pode mais entrar.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2703, 6));
                }

                var tempo = (RoomInfo.HoleCount == 18) ? 10 * 60000 : 5 * 60000;

                var remain = UtilTime.GetLocalDateDiff(session.GetGameRoom().GetTimeStart());

                if (remain > 0)
                {
                    remain /= STDA_10_MICRO_PER_MILLI; // miliseconds
                }

                if (remain >= tempo)
                {
                    throw new exception("[Room::RequestEnterGameAfrerStarted][Warning] Normal[UID=" + session.UserInfo.UID + "] tentou entrar na sala[NUMERO=" + RoomInfo.RoomID + "] ja em jogo, mas o tempo de entrar no Tourney acabou.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, // Acabou o tempo de entrar na sala
                        2706, 2));
                }

                // Add Player a sala
                EnterToRoom(session);

                ret = true;

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Room::RequestEnterGameAfterStarted][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Excluí player da sala se adicionou ele antes
                if (FindSessionByUid(session.UserInfo.UID) != null)
                {
                    Leave(session, 0);
                }

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));

                session.Send(p);
            }

            return ret;
        }
          
        public bool CheckPersonalShopItem(Player session, int itemId)
        {
            return _tradeShop.isItemForSale(session, itemId);
        }
    }
}
