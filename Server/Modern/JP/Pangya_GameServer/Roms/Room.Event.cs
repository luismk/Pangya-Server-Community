using Pangya_GameServer.Engine;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameModes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log; 
using static Pangya_GameServer.Models.DefineConstants; 
namespace Pangya_GameServer.Roms
{
    public partial class Room
    {

        public virtual bool RequestStartGame(Player session, Packet packet)
        {
            if (!session.getState())
            {
                throw new exception("[Room::RequestStartGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (packet == null)
            {
                throw new exception("[Room::RequestStartGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            Packet p = new();

            bool ret = true;

            try
            {

                if (RoomInfo.OwnerUID != session.UserInfo.UID)
                {
                    if (!session.UserInfo.Member.Capability.IsGameMaster)
                        throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o jogo na sala[NUMERO=" + RoomInfo.RoomID + "], mas ele nao é o Master da sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                            1, 0x5900201));
                }

                // Verifica se já tem um jogo inicializado e lança error se tiver, para o cliente receber uma resposta
                if (CurrentGame != null || session.GetGameRoom() != null)
                {
                    throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o jogo na sala[NUMERO=" + RoomInfo.RoomID + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        8, 0x5900202));
                }

                // Verifica se todos estão prontos se não da erro
                if (!IsAllReady())
                {
                    throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o jogo na sala[NUMERO=" + RoomInfo.RoomID + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    8, 0x5900202));
                }

                // Coloquei para verificar se a type de Bot Tourney não está ativo verifica o resto das condições
                if (!BotTourney
                    && Players.Count == 1
                    && RoomInfo.GetRoomType() != RoomTypeFlags.PRACTICE
                    && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_PRIX
                    && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_INT
                    && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_ADV
                    && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                {
                    session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                    return false;
                }

                // Match
                if (RoomInfo.GetRoomType() == RoomTypeFlags.MATCH)
                {

                    if (Teams.Count == 0)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (Teams.Count() == 1)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (Players.Count() % 2 == 1)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (Players.Count() == 2 && (Teams[0].GetNumPlayers() == 0 || Teams[1].GetNumPlayers() == 0))
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (Players.Count() == 4 && (Teams[0].GetNumPlayers() < 2 || Teams[1].GetNumPlayers() < 2))
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (RoomInfo.MaxUsers == 4 && Players.Count() < 4)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }
                }

                // Guild Battle
                if (RoomInfo.GetRoomType() == RoomTypeFlags.GUILD_BATTLE)
                {

                    if (Players.Count() % 2 == 1)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    var error_check = GuildManager.isGoodToStart();

                    if (error_check <= 0)
                    {

                        switch (error_check)
                        {
                            case 0: // Não tem duas GuildBattle na sala
                                throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o Guild Battle na sala[NUMERO=" + RoomInfo.RoomID + "], mas nao tem GuildBattle suficientes para comecar o jogo. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    10, 0x5900202));
                            case -1: // Não tem o mesmo número de jogadores na sala as duas GuildBattle
                                throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o Guild Battle na sala[NUMERO=" + RoomInfo.RoomID + "], mas as duas GuildBattle nao tem o mesmo RoomID de jogadores na sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    11, 0x5900202));
                            case -2: // Uma das Guilds ou as duas não tem 2 jogadores
                                throw new exception("[Room::RequestStartGame] [Error] Normal[UID=" + session.UserInfo.UID + "] tentou comecar o Guild Battle na sala[NUMERO=" + RoomInfo.RoomID + "], mas uma ou as duas GuildBattle tem menos que 2 jogadores na sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    12, 0x5900202));
                        }
                    }
                }

                // Chip-in Practice
                if (RoomInfo.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                {

                    var pTicket = session.Inventory.FindWarehouseItemByTypeid(CHIP_IN_PRACTICE_TICKET_TYPEID);

                    if (pTicket == null)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    if (pTicket.c[0] < 1)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    stItem item = new stItem
                    {
                        id = pTicket.id,
                        type = 2,
                        _typeid = pTicket._typeid,
                        qntd = 1
                    };
                    item.c[0] = (short)((short)item.qntd * -1);

                    // UPDATE ON SERVER AND DB
                    if (ItemManager.removeItem(item, session) <= 0)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                        return false;
                    }

                    // UPDATE ON GAME
                    p.init_plain(0x216);

                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1); // Count;

                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteInt32(item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD); // qntd
                    p.WriteZero(25);

                    session.Send(p);
                }

                if (RoomInfo.GetMap() >= 0x7Fu)
                {

                    // Special Shuffle Course
                    if (RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && RoomInfo.GetHoleType() == RoomHoleType.M_SHUFFLE_COURSE)
                    {

                        RoomInfo.CourseIndex = (RoomCourseFlags)(0x80 | (int)RoomCourseFlags.CHRONICLE_1_CHAOS);
                    }
                    else
                    { // Random Normal

                        LotterySystem lottery = new LotterySystem();

                        foreach (var el in sIff.Instance.getCourse())
                        {

                            var course_id = sIff.Instance.getItemIdentify(el.ID);

                            if (course_id != 17 && course_id != 0x40)
                            {
                                lottery.Add(100, course_id);
                            }
                        }

                        var lc = lottery.SpinRoleta();

                        if (lc != null)
                        {
                            RoomInfo.CourseIndex = (RoomCourseFlags)(0x80 | Convert.ToByte(lc.Value));
                        }
                    }
                }

                // Corrigido: era || — ativava para qualquer player que não fosse
                // simultaneamente premium E GM. Com && só entra quem não é nem premium nem GM.
                if (!session.UserInfo.UserCapabilities.UserPremium && !session.UserInfo.UserCapabilities.IsGameMaster)
                {
                    // Verifica se ele tem o ticket para criar o Bot se não manda mensagem dizenho que ele não tem ticket para criar o bot
                    var pWi = session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID) != null ? session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID) : session.Inventory.FindWarehouseItemByTypeid(TICKET_BOT_TYPEID2);

                    if (pWi != null && pWi.STDA_C_ITEM_QNTD > 0)
                    {

                        stItem item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = 1;
                        item.c[0] = (short)(item.qntd * -1);

                        if (ItemManager.removeItem(item, session) > 0)
                        {

                            // Atualiza o item no Jogo e Add o Bot e manda a mensagem que o bot foi add
                            p.init_plain(0x216);

                            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                            p.WriteUInt32(1); // Count;

                            p.WriteByte(item.type);
                            p.WriteUInt32(item._typeid);
                            p.WriteInt32(item.id);
                            p.WriteUInt32(item.flag_time);
                            p.WriteBytes(item.stat.ToArray());
                            p.WriteInt32(item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD); // qntd
                            p.WriteZero(25);

                            session.Send(p);
                        }
                        else
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[Room::makeBot] [Error] Normal[UID=" + session.UserInfo.UID + "] nao conseguiu deletar o TICKET_BOT[TYPEID=" + Convert.ToString(TICKET_BOT_TYPEID) + ", ID=" + Convert.ToString(item.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }

                    }

                }

                if (!MakeGameRoom())//falha ao criar
                {
                    session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
                    return false;
                }

                // Update Room State
                RoomInfo.StateRoom = 0; // IN GAME

                p.init_plain(0x230);

                SendBroadCast(p);

                p.init_plain(0x231);

                SendBroadCast(p);

                p.init_plain(0x77);

                p.WriteUInt32((uint)GameServer.Instance.getInfo().Rate.Pang); // Rate Pang

                SendBroadCast(p);

                RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala

                //insert dados do player
                //foreach (var _sessions in Players)
                //    CreateRoomLogSql(_sessions);//criar de todos 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Room::RequestStartGame] [Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain(0x253);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x5900200);
                session.Send(p);
                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        public bool MakeGameRoom(GrandPrixData grandPrixData = null)
        {

            RateValue _RateValue = new()
            {
                exp = RoomInfo.RateExperience = (uint)GameServer.Instance.getInfo().Rate.Experience,
                pang = RoomInfo.RatePangs = (uint)GameServer.Instance.getInfo().Rate.Pang,
                clubset = (uint)GameServer.Instance.getInfo().Rate.ClubMastery,
                rain = (uint)GameServer.Instance.getInfo().Rate.Rain,
                treasure = (uint)GameServer.Instance.getInfo().Rate.Treasure,
                persist_rain = 0//so ativa la na gamebase 
            };
            // Angel Event 
            RoomInfo.IsAngelQuiterEvent = GameServer.Instance.getInfo().Rate.AngelEvent == 1; 
            switch (RoomInfo.GetRoomType())
            {
                case RoomTypeFlags.STROKE://VERSUS
                    CurrentGame = new Stroke(Players, RoomInfo, _RateValue);
                    break;
                //case RoomTypeFlags.MATCH:
                //    CurrentGame = new Match(Players, GameRoomInfoModel, _RateValue, m_teans);
                //    break;
                //case RoomTypeFlags.PANG_BATTLE: // Ainda não está feio, usa o  Versus Normal
                //    CurrentGame = new PangBattle(Players, GameRoomInfoModel, _RateValue);
                //    break;
                //case RoomTypeFlags.APPROCH:
                //    CurrentGame = new Approach(Players, GameRoomInfoModel, _RateValue);
                //    break;
                case RoomTypeFlags.PRACTICE:
                    CurrentGame = new Practice(Players, RoomInfo, _RateValue);
                    break;
                case RoomTypeFlags.TOURNEY:
                    CurrentGame = new Tourney(Players, RoomInfo, _RateValue);
                    break;
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                    CurrentGame = new SSC(Players, RoomInfo, _RateValue);
                    break;
                //case RoomTypeFlags.GUILD_BATTLE:
                //    CurrentGame new GuildBattle(Players, GameRoomInfoModel, _RateValue, m_guild_manager);
                //    break;
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                    CurrentGame = new GrandZodiacPractice(Players, RoomInfo, _RateValue);
                    break;
                case RoomTypeFlags.GRAND_ZODIAC_INT:
                case RoomTypeFlags.GRAND_ZODIAC_ADV:
                    CurrentGame = new GrandZodiac(Players, RoomInfo, _RateValue);
                    break;
                case RoomTypeFlags.GRAND_PRIX:
                    CurrentGame = new GrandPrix(Players, RoomInfo, _RateValue, grandPrixData);
                    break;
                default:
                    break;
            }
            return CurrentGame != null;
        }
    }
}
