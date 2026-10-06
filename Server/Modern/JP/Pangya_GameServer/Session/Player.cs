using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Feature.Security;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase;
using Pangya_GameServer.Server;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System.Net.Sockets;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.DataBase.Models.NormalDB;
namespace Pangya_GameServer.Session
{
    public class Player : AppSession
    {  
        public HeartBeat m_HeartBeat { get; set; }
        public ChatPenaltyManager ChatPenalty { get; } = new();
        public InventoryInfo? Inventory { get; set; } = new();// inventorio de itens do jogador....
        public PlayerInfo? UserInfo { get; set; } = new();// info do jogador....
        public GMInfo m_gi { get; set; } = new();// info de GM se for GM
        public Game CurrentGame { get; set; } //nao tive ideia de como usar isso, mas vai ser util quando eu for usar _player.getCurrentGame() pra pegar o jogo que o player esta jogando, ai da pra usar isso pra pegar o RoomID do hole, o score do player, etc...
        public Room? CurrentRoom { get; set; }//sala onde esta o jogador....
        public Channel? CurrentChannel { get; set; } //onde esta o jogador....  
        public string MacAdress { get; set; } = string.Empty;
        private Dictionary<uint/*GameKey*/, TitleMapCallBack> BonusTitle { get; set; } = new();

        public Player(GameService game, Socket socket, int id) : base(game, socket, id)
        {
            InitTitleCallbacks();
        } 

        public override string GetNickname()
        {
            return UserInfo.NickName;
        }

        public override uint GetUID()
        {
            return UserInfo.UID;
        }

        public override string GetID()
        {
            return UserInfo.Login;
        }

        public override uint GetCapability() { return (uint)UserInfo.UserCapabilities.Value; }

        public override byte GetStateLogged()
        {
            return UserInfo.StateLogged;
        }

        public bool getState()
        {
            return Authorized && Connected;
        }

        public override bool Clear()
        {
            lock (this)
            {
                bool ret;
                if (ret = base.Clear())
                {
                    // Player Info
                    UserInfo.Clear();
                    Inventory?.Clear();
                    // Game Master Info
                    m_gi.clear();
                    SetChannel(null);
                    SetRoom(null);
                }
                return ret;
            }
        }

        public bool Load()
        {
            uint uid = UserInfo.UID;

            try
            {
                Inventory.Load(UserInfo.UID); // Iniciamos a Task aqui 
                var sucess = UserInfo.Load();
                var tCaddies = CommandDB.LoadCaddie(uid, CmdCaddieInfo.TYPE.FERIAS);
                var tMsgOff = CommandDB.LoadMsgOff(uid);
                UserInfo.AssistFlag = CommandDB.LoadAssist(uid) && Inventory.ItemExist(ASSIST_ITEM_TYPEID);

                if (sucess)
                {
                    foreach (var characters in Inventory.Characters)//add all
                    {
                        UserInfo.CharacterLoungeStates.Add(characters.Key, new StateCharacterLounge());
                    }
                    // 6. Envio de Pacotes Iniciais
                    int ttl = GameServer.Instance.getBotTTL();
                    this.Send(Handle_PACKET_RESPONSE.pacote1A9(ttl));

                    Inventory.SetPremiumSys();
                    //seta alguns valores para jogar
                    Inventory.SetDefaultCharacter(this);
                    Inventory.SetDefaultCaddie(this);
                    Inventory.SetDefaultMascot(this);
                    Inventory.SetDefaultClub(this);
                    Inventory.SetDefaultComet(this);

                    this.Send(Handle_PACKET_RESPONSE.pacote11F(UserInfo, 3));
                    this.Send(Handle_PACKET_RESPONSE.pacote101());

                    // Envio de Caddies--- FERIAS
                    var v_cif = tCaddies;
                    if (v_cif != null && v_cif.Any())
                    {
                        this.Send(Handle_PACKET_RESPONSE.pacote0D4(v_cif));
                    }

                    //FRIEND NOTE
                    var v_moi = tMsgOff;
                    if (v_moi != null && v_moi.Any())
                    {
                        this.Send(Handle_PACKET_RESPONSE.pacote0B2(v_moi));
                    }

                    PlayerManager.CheckItemBuff(this);

                    for (int i = 0; i < 39; i++)
                    {
                        var p = new Packet(0x44);
                        p.WriteByte(eLoginAck.ACK_UPDATE_LOGIN_UNIT);
                        p.WriteInt32(i);
                        this.Send(p);
                        Thread.Sleep(12);//so dar pra uma imppressao bacanada hahah
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Player::Load][Error] UID {uid}: {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                GameServer.Instance.Disconnect(this);
            }
            return false;
        }

        //ainda vou terminar..
        public void CheckHeartBeat()
        {
            m_HeartBeat.Check();
        }

        #region Handle Channel Player 

        public Channel GetChannel()
        {
            if (CurrentChannel != null)
                return CurrentChannel;

            if (UserInfo.Channel >= 0)
                return GameServer.Instance.FindChannel(UserInfo.Channel);
            return null;
        }

        public void SetChannel(Channel channel)
        {
            if (channel == null)
            {
                LeaveChannel();
            }
            else
            {
                EnterChannel(channel);
            }
        }

        private void LeaveChannel()
        {
            CurrentChannel = null;
            // compatibilidade
            UserInfo.Channel = -1;
        }
         
        private void EnterChannel(Channel channel)
        {
            if (channel == null)
                return;

            CurrentChannel = channel;
            UserInfo.Channel = channel.getId();
        }
        #endregion

        #region Handle Room Player

        public Room GetRoom()
        {
            //verifica se realmente esta em room, por que as vezes o player pode estar na sala, mas o jogo ainda nao ter comecado, entao tem que verificar se tem um jogo nessa sala.
            if (CurrentRoom != null)//verifica primeiro no field
            {
                CurrentGame = CurrentRoom.CurrentGame;//atualiza a memoria rotativa.
                return CurrentRoom;
            }
            //verifica se realmente esta em sala, por que as vezes o player pode estar na sala, mas o jogo ainda nao ter comecado, entao tem que verificar se tem um jogo nessa sala.
            if (UserInfo.Member.RoomID >= 0)//verifica segundo no objeto
            {
                //atualiza a memoria rotativa, por que as vezes o player pode estar na sala, mas o jogo ainda nao ter comecado, entao tem que verificar se tem um jogo nessa sala.
                var room = GameServer.Instance.FindRoom(UserInfo.Member.RoomID);

                if (CurrentGame == null && room != null && room.CurrentGame != null)
                {
                    CurrentGame = room.CurrentGame;
                }

                return room;// encontrou a sala
            }

            return null;//sai
        }

        public void SetRoom(Room room, bool Invited = false)
        {
            if (room == null)
            {
                LeaveRoom(); 
            }
            else
            {
                EnterRoom(room, Invited);
            }
        }

        private void LeaveRoom()
        {
            CurrentGame = null;
            CurrentRoom = null;
            // remove da sala.
            UserInfo.Member.RoomID = -1;
            UserInfo.Place = 0; 
        }
         
        private void EnterRoom(Room room, bool Invited)
        {
            if (room == null)
                return;

            CurrentRoom = room;
            CurrentGame = room.CurrentGame;
            UserInfo.Member.RoomID = room.GetRoomId();
            UserInfo.Place = (sbyte)(Invited ? 70 : 10);
        }

        public Game GetGameRoom()
        {
            if (CurrentGame != null)//verifica na primeira variavel(Game)
                return CurrentGame;
             
            if (CurrentRoom != null && CurrentRoom.CurrentGame != null)//verifica na segunda variavel(Room)
            {
                if (CurrentGame == null)
                {
                    CurrentGame = CurrentRoom.CurrentGame;
                }
                return CurrentRoom.CurrentGame;
            }

            if (UserInfo.Member.RoomID >= 0)
            {
                var game = GameServer.Instance.FindRoom(UserInfo.Member.RoomID);
                if (game != null && game.CurrentGame != null)//verifcar se realmente tem um jogo nessa sala, por que as vezes o player pode estar na sala, mas o jogo ainda nao ter comecado, entao tem que verificar se tem um jogo nessa sala.
                {
                    CurrentGame = game.CurrentGame; 
                    return game.CurrentGame;
                } 
            }

            return null;
        }
         
        #endregion

        public WarehouseItemEx? CreateDefaultBall()
        {
            BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_COMET_TYPEID, qntd = 1 };
            stItem newItem = new stItem();

            // Inicializa a estrutura do item (ignora Level)
            ItemManager.initItemFromBuyItem(UserInfo, newItem, bi, false, 0, 0, 1);

            if (newItem._typeid == 0)
            {
                throw new exception($"[SQLDB::Error] Falha ao inicializar struct da Comet para UID: {UserInfo.UID}");
            }

            // Adiciona ao Banco de Dados/Memória
            int newId = ItemManager.addItem(newItem, this, 2 /* Padrão */, 0);
            
            SendNewItemPacket(newItem);//update inventory.

            return Inventory.FindWarehouseItemById(newId);
        }

        public WarehouseItemEx? CreateDefaultClubSet()
        {
            BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_CLUB_TYPEID, qntd = 1 };
            stItem newItem = new stItem();

            // Inicializa a estrutura do item (ignora Level)
            ItemManager.initItemFromBuyItem(UserInfo, newItem, bi, false, 0, 0, 1);

            if (newItem._typeid == 0)
            {
                throw new exception($"[SQLDB::Error] Falha ao inicializar struct da Club para UID: {UserInfo.UID}");
            }

            // Adiciona ao Banco de Dados/Memória
            int newId = ItemManager.addItem(newItem, this, 2 /* Padrão */, 0);

            SendNewItemPacket(newItem);//update inventory.

            return Inventory.FindWarehouseItemById(newId);
        }

        public CharacterInfo? CreateDefaultCharacter()
        {
            BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_CLUB_TYPEID, qntd = 1 };
            stItem newItem = new stItem();

            // Inicializa a estrutura do item (ignora Level)
            ItemManager.initItemFromBuyItem(UserInfo, newItem, bi, false, 0, 0, 1);

            if (newItem._typeid == 0)
            {
                throw new exception($"[SQLDB::Error] Falha ao inicializar struct da Character para UID: {UserInfo.UID}");
            }

            // Adiciona ao Banco de Dados/Memória
            int newId = ItemManager.addItem(newItem, this, 2 /* Padrão */, 0);

            SendNewItemPacket(newItem);//update inventory.

            return Inventory.FindCharacterById(newId);
        }

        private void SendNewItemPacket(stItem item)
        {
            using (Packet p = new Packet(0x216))
            {
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Quantidade de itens sendo adicionados

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);

                // Escreve os stats (Stats do Character ou do ClubSet)
                p.WriteBytes(item.stat.ToArray());

                // Quantidade ou Tempo restante
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);

                p.WriteZero(25); // Padding padrão do protocolo Pangya

                Send(p);
            }
        }


        public void addExp(int _exp, bool _upt_on_game = false)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addExp][Error] _exp is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    1, 0));
            }

            try
            {
                int ret = -1;

                if ((ret = UserInfo.addExp(_exp)) >= 0)
                {

                    if (ret > 0)
                    { // Player Upou de Level 
                        List<stItem> v_item = new List<stItem>();
                        stItem item = new stItem();
                        BuyItem bi = new BuyItem();

                        var level_prize = sIff.Instance.getLevelUpPrizeItem();

                        for (var i = (UserInfo.Member.GameLevel - ret + 1); i <= UserInfo.Member.GameLevel; ++i)
                        {

                            // Zera o vector de item que vai ser enviado por Level UP! para o mail box do player
                            v_item.Clear();

                            var it = level_prize.FirstOrDefault(c => c.Level == i);

                            if (it == null)
                            {
                                throw new exception("[player::addExp][ErrorSystem] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] addExp, mas nao encontrou o Level up prize[Level=" + Convert.ToString(i) + "] no IFF_STRUCT do server", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                    2, 0));
                            }

                            for (var ii = 0; ii < (it.reward.TypeID.Length); ++ii)
                            {

                                if (it.reward.TypeID[ii] != 0)
                                {
                                    bi = new BuyItem();
                                    item.clear();

                                    bi.id = -1;
                                    bi._typeid = it.reward.TypeID[ii];
                                    bi.qntd = it.reward.Quantity[ii];
                                    bi.time = (short)it.reward.Time[ii];

                                    ItemManager.initItemFromBuyItem(UserInfo,
                                        item, bi, false, 0, 0,
                                        1);

                                    if (item._typeid == 0)
                                    {
                                        throw new exception("[player::addExp][ErrorSystem] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] addExp, mas nao conseguiu inicializar o item[TYPEID=" + Convert.ToString(bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                            3, 0));
                                    }

                                    v_item.Add(item);
                                }
                            }

                            var msg = "Level UP! Prize.";

                            // Envia Prêmio de Level UP! para o Mail Box do player
                            MailManager.SendMailWithItem(0,
                                  UserInfo.UID, msg, v_item);
                        }

                        // Mostra msg que o player Upou de Level
                        var p = new Packet(0x10F);

                        p.WriteUInt32(0); // OK

                        p.WriteByte((byte)ret); // Qntd de Level(s) que ele upou
                        p.WriteByte((byte)UserInfo.Member.GameLevel); // Novo Level que o player ficou

                        Send(p);
                    }
                }

                // Att Level e Exp do player IN GAME
                if (_upt_on_game)
                { // Só att se for pegando do mail ou ticket report esses negocio, por que jogando vs/Tourney, nao precisa desse pacote
                    var p = new Packet(0x1D9);

                    p.WriteUInt32(UserInfo.Member.GameLevel);
                    p.WriteInt32(UserInfo.Statistics.exp);
                    this.Send(p);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::addExp][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.PLAYER_INFO)
                {
                    throw;
                }
            }
        }


        public void addCaddieExp(int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addCaddieExp][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar mais Experience[VALUE=" + Convert.ToString(_exp) + "] ao caddie equipado, mas Experience is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    300, 0));
            }

            if (Inventory.UserEquippedItem.CaddieEquiped == null)
            {
                throw new exception("[player::addCaddieExp][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar mais Experience[VALUE=" + Convert.ToString(_exp) + "] ao caddie equipado, mas ele nao esta com nenhum caddie equipado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    301, 0));
            }


            // Só add Exp se não estiver no ultimo Level do Caddie
            if (Inventory.UserEquippedItem.CaddieEquiped.level < LIMIT_LEVEL_CADDIE)
            {

                Inventory.UserEquippedItem.CaddieEquiped.exp += _exp;

                int exp_level = 0;

                while (Inventory.UserEquippedItem.CaddieEquiped.level < LIMIT_LEVEL_CADDIE && Inventory.UserEquippedItem.CaddieEquiped.exp >= (exp_level = (int)(520 + (160 * (Inventory.UserEquippedItem.CaddieEquiped.level)))))
                {

                    // Upou 1 Level
                    Inventory.UserEquippedItem.CaddieEquiped.level++;

                    Inventory.UserEquippedItem.CaddieEquiped.exp -= exp_level;
                }

                // UPDATE ON DB
                NormalManagerDB.Instance.add(1,
                     new CmdUpdateCaddieInfo(UserInfo.UID, Inventory.UserEquippedItem.CaddieEquiped),
                     SQLDBResponse, this);

                // LOG
            }
        }

        public void addMascotExp(int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addMascotExp][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar mais Experience[VALUE=" + Convert.ToString(_exp) + "] ao mascot equipado, mas Experience is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    400, 0));
            }

            if (Inventory.UserEquippedItem.MascotEquiped == null)
            {
                throw new exception("[player::addMascotExp][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar mais Experience[VALUE=" + Convert.ToString(_exp) + "] ao mascot equipado, mas ele nao esta com nenhum mascot equipado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    401, 0));
            }

            if (Inventory.UserEquippedItem.MascotEquiped._typeid == 0)
                return;

            // Progressão aritmética de segunda ordem

            // Só add Exp se não estiver no ultimo Level do Mascot
            if (Inventory.UserEquippedItem.MascotEquiped.level < LIMIT_LEVEL_MASCOT)
            {

                Inventory.UserEquippedItem.MascotEquiped.exp += _exp;

                int exp_level = 0;

                while (Inventory.UserEquippedItem.MascotEquiped.level < LIMIT_LEVEL_MASCOT && Inventory.UserEquippedItem.MascotEquiped.exp >= (exp_level = (int)(50 + ((20 + (20 + ((Inventory.UserEquippedItem.MascotEquiped.level) - 1) * 10)) * (Inventory.UserEquippedItem.MascotEquiped.level) / 2))))
                {

                    // Upou 1 Level
                    Inventory.UserEquippedItem.MascotEquiped.level++;

                    Inventory.UserEquippedItem.MascotEquiped.exp -= exp_level;
                }

                // UPDATE ON DB
                NormalManagerDB.Instance.add(2,
                      new CmdUpdateMascotInfo(UserInfo.UID, Inventory.UserEquippedItem.MascotEquiped),
                      SQLDBResponse, this);

            }
        }

        // Add Exp Estático
        public static void addExp(uint _uid, int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addExp][Error] _exp is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    1, 0));
            }

            PlayerInfo pi = null;
            try
            {

                int ret = -1;

                CmdPlayerInfo cmd_pi = new CmdPlayerInfo(_uid); // Waiter

                NormalManagerDB.Instance.add(0, cmd_pi);
                 
                if (cmd_pi.getException().getCodeError() != 0)
                {
                    throw cmd_pi.getException();
                }

                pi = new PlayerInfo();

                pi.Set(cmd_pi.getInfo());

                if ((ret = pi.addExp(_exp)) >= 0)
                {

                    if (ret > 0)
                    { // Player Upou de Level

                        List<stItem> v_item = new List<stItem>();
                        stItem item = new stItem();
                        BuyItem bi = new BuyItem();

                        var level_prize = sIff.Instance.getLevelUpPrizeItem();

                        for (var i = (pi.Member.GameLevel - ret + 1); i <= pi.Member.GameLevel; ++i)
                        {

                            // Zera o vector de item que vai ser enviado por Level UP! para o mail box do player
                            v_item.Clear();

                            var it = level_prize.FirstOrDefault(c => c.Level == i);

                            if (it == null)
                            {
                                throw new exception("[player::addExp][ErrorSystem] Normal[UID=" + Convert.ToString(pi.UID) + "] addExp, mas nao encontrou o Level up prize[Level=" + i + "] no IFF_STRUCT do server", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                    2, 0));
                            }

                            for (var ii = 0; ii < (it.reward.TypeID.Length); ++ii)
                            {

                                if (it.reward.TypeID[ii] != 0)
                                {
                                    bi = new BuyItem();
                                    item.clear();

                                    bi.id = -1;
                                    bi._typeid = it.reward.TypeID[ii];
                                    bi.qntd = it.reward.Quantity[ii];
                                    bi.time = (short)it.reward.Time[ii];

                                    ItemManager.initItemFromBuyItem(pi,
                                        item, bi, false, 0, 0,
                                        1/**/);

                                    if (item._typeid == 0)
                                    {
                                        throw new exception("[player::addExp][ErrorSystem] Normal[UID=" + Convert.ToString(pi.UID) + "] addExp, mas nao conseguiu inicializar o item[TYPEID=" + Convert.ToString(bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                            3, 0));
                                    }

                                    v_item.Add(item);
                                }
                            }

                            var msg = "Level UP! Prize.";

                            // Envia Prêmio de Level UP! para o Mail Box do player
                            MailManager.SendMailWithItem(0,
                                  pi.UID, msg, v_item);
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::addExp][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Clean
                if (pi != null)
                {

                    pi.Clear();

                    pi = null;
                }

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.PLAYER_INFO)
                {
                    throw;
                }
            }

            // Clean
            if (pi != null)
            {
                pi.Clear();
            }
        }

        public void addPang(ulong _pang)
        {

            UserInfo.addPang(_pang);

            // UPDATE ON GAME
            var p = new Packet((ushort)0xC8);

            p.WriteUInt64(UserInfo.Statistics.pang);
            p.WriteUInt64(_pang);

            Send(p);
        }

        public void consomePang(ulong _pang)
        {

            UserInfo.consomePang(_pang);

            // UPDATE ON GAME
            var p = new Packet((ushort)0xC8);

            p.WriteUInt64(UserInfo.Statistics.pang);
            p.WriteUInt64(_pang);

            Send(p);
        }

        public void saveCPLog(CPLog _cp_log)
        {
            ulong cp = _cp_log.getCookie();

            try
            {
                if (cp > 0)
                {
                    long log_id = -1;

                    var cmd_icpl = new CmdInsertCPLog(UserInfo.UID, _cp_log); // Waiter

                    NormalManagerDB.Instance.add(0, cmd_icpl);

                    if (cmd_icpl.getException().getCodeError() != 0)
                        throw cmd_icpl.getException();

                    if ((log_id = cmd_icpl.getId()) <= 0)
                        throw new exception($"[player::saveCPLog][Error] Normal[UID={UserInfo.UID}] nao conseguiu salvar o CPLog[{_cp_log.toString()}] do player. Bug",
                             ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1300, 0));

                    if ((_cp_log.getType() == CPLog.TYPE.BUY_SHOP || _cp_log.getType() == CPLog.TYPE.GIFT_SHOP)
                            && _cp_log.getItemCount() > 0)
                    {

                        // Tem item(ns), salva o log do(s) item(ns)
                        foreach (var el in _cp_log.getItens())
                        {
                            NormalManagerDB.Instance.add(3, new CmdInsertCPLogItem(UserInfo.UID, log_id, el), SQLDBResponse, this);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[player::saveCPLog][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void saveCPLog(uint _uid, CPLog _cp_log)
        {
            if (_uid == 0)
                throw new exception($"[player::saveCPLog(static)][Error] _uid is invalid({_uid})",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1301, 0));

            ulong cp = _cp_log.getCookie();

            try
            {
                if (cp > 0)
                {
                    long log_id = -1;

                    CmdInsertCPLog cmd_icpl = new CmdInsertCPLog(_uid, _cp_log, true); // Waiter

                    NormalManagerDB.Instance.add(0, cmd_icpl);

                    if (cmd_icpl.getException().getCodeError() != 0)
                        throw cmd_icpl.getException();

                    if ((log_id = cmd_icpl.getId()) <= 0)
                        throw new exception($"[player::saveCPLog(static)][Error] Normal[UID={_uid}] nao conseguiu salvar o CPLog[{_cp_log.toString()}] do player. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1300, 0));

                    if ((_cp_log.getType() == CPLog.TYPE.BUY_SHOP || _cp_log.getType() == CPLog.TYPE.GIFT_SHOP)
                            && _cp_log.getItemCount() > 0)
                    {

                        // Tem item(ns), salva o log do(s) item(ns)
                        foreach (var el in _cp_log.getItens())
                        {
                            NormalManagerDB.Instance.add(3, new CmdInsertCPLogItem(_uid, log_id, el), SQLDBResponse, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[player::saveCPLog(static)][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void addCookie(ulong _cookie)
        {

            UserInfo.addCookie(_cookie);

            // UPDATE ON GAME
            var p = new Packet((ushort)0x96);

            p.WriteUInt64(UserInfo.Cookie);

            Send(p);
        }

        public void consomeCookie(ulong _cookie)
        {

            UserInfo.consomeCookie(_cookie);

            // UPDATE ON GAME
            var p = new Packet((ushort)0x96);

            p.WriteUInt64(UserInfo.Cookie);

            Send(p);
        }

        public void addMoeda(ulong _pang, ulong _cookie)
        {

            addPang(_pang);
            addCookie(_cookie);
        }

        public void consomeMoeda(ulong _pang, ulong _cookie)
        {
            consomePang(_pang);
            consomeCookie(_cookie);
        }

        public bool CheckCharacterEquipedAuxPart(CharacterInfo ci)
        {

            bool upt_on_db = false;

            // Check AuxPart Equiped
            for (var i = 0; i < (ci.auxparts.Length); ++i)
            {

                if (ci.auxparts[i] != 0)
                {

                    // Esse AuxPartNumber é o 0x0 anel que consome(só mão direita), 0x1 mão direita, 0x21 mão esquerda
                    if (sIff.Instance.getItemGroupIdentify(ci.auxparts[i]) == IFF_GROUP.AUX_PART)
                    {

                        var aux = sIff.Instance.findAuxPart(ci.auxparts[i]);
                        var pAux = Inventory.FindWarehouseItemByTypeid(ci.auxparts[i]);

                        if (aux != null
                            && aux.Active
                            && pAux != null)
                        {

                            if (aux.Level.GoodLevel((byte)UserInfo.Level))
                            {

                                if (aux.slot[0] == 0 || pAux.c[0] > 0)
                                {
                                }
                                else
                                {

                                    // Desequipa
                                    ci.auxparts[i] = 0;

                                    upt_on_db = true;
                                }

                            }
                            else
                            {

                                // Desequipa
                                ci.auxparts[i] = 0;

                                upt_on_db = true;
                            }

                        }
                        else
                        {

                            // Desequipa
                            ci.auxparts[i] = 0;

                            upt_on_db = true;
                        }

                    }
                    else
                    {

                        // Desequipa
                        ci.auxparts[i] = 0;

                        upt_on_db = true;
                    }

                }
                else
                {
                }
            }

            return upt_on_db;
        }

        public bool CheckCharacterEquipedCutin(CharacterInfo ci)
        {

            bool upt_on_db = false;


            for (var i = 0; i < (ci.cut_in.Length); ++i)
            {

                if (ci.cut_in[i] != 0)
                {

                    var pCutin = Inventory.FindWarehouseItemById((int)ci.cut_in[i]);

                    if (pCutin == null)
                    {

                        // Zera (Desequipa)
                        ci.cut_in[i] = 0;

                        upt_on_db = true;

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquipedCutin][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Not Have Cutin[ID=" + Convert.ToString(ci.cut_in[i]) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var cutin = sIff.Instance.findSkin(pCutin._typeid);

                        if (cutin != null && !cutin.Level.GoodLevel((byte)UserInfo.Level))
                        {

                            // Zera (Desequipa)
                            ci.cut_in[i] = 0;

                            upt_on_db = true;

                            // Não tem o Level necessário para equipar esse Cutin
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquipedCutin][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Cutin[TYPEID=" + Convert.ToString(pCutin._typeid) + " ID=" + Convert.ToString(pCutin.id) + ", SLOT=" + Convert.ToString(i) + "]  Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(cutin.Level.is_max) + ", Lv=" + Convert.ToString((ushort)cutin.Level.level) + "] para equipar esse item. Hacker ou bug..", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (cutin == null)
                        {

                            // Zera (Desequipa)
                            ci.cut_in[i] = 0;

                            upt_on_db = true;

                            // Não tem esse Cutin no IFF_STRUCT do server desequipa ele
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquipedCutin][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Not Have Cutin[TYPEID=" + Convert.ToString(pCutin._typeid) + " ID=" + Convert.ToString(pCutin.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public void CheckCharacterAllItemEquiped(CharacterInfo ci)
        {

            var ret = CheckCharacterEquipedPart(ci);

            ret |= CheckCharacterEquipedAuxPart(ci);

            ret |= CheckCharacterEquipedCutin(ci);

            // Atualiza os parts equipados do player no banco de dados, que tinha parts errados
            if (ret)
            {
                NormalManagerDB.Instance.add(5,
                      new CmdUpdateCharacterAllPartEquiped(UserInfo.UID, ci),
                      SQLDBResponse, this);
            }
        }

        public bool CheckCharacterEquipedPart(CharacterInfo ci)
        {

            uint def_part = 0;

            // Angel Part of character 3% quit Rate para equipar o Normal angel wings
            var angel_wings_typeid = Global.angel_wings.FirstOrDefault(el => sIff.Instance.getItemCharIdentify(el) == (ci._typeid & 0x000000FF));

            int angel_wings_part_num = (Global.angel_wings.Any(el => el == angel_wings_typeid) ? -1 : (int)sIff.Instance.getItemCharPartNumber(angel_wings_typeid));

            bool upt_on_db = false;

            // Checks Parts Equiped
            for (var i = 0u; i < 24; ++i)
            {

                if (ci.parts_typeid[i] != 0)
                {

                    if (sIff.Instance.getItemGroupIdentify(ci.parts_typeid[i]) == IFF_GROUP.PART && (sIff.Instance.getItemCharPartNumber(ci.parts_typeid[i]) == i || (ci.parts_typeid[i] & 0x08000400/*def part*/) == 0x8000400))
                    {

                        var part = sIff.Instance.findPart(ci.parts_typeid[i]);

                        if (part != null && part.Active)
                        {

                            if (ci.parts_id[i] == 0)
                            {

                                def_part = ((sIff.Instance.getItemCharPartNumber(ci.parts_typeid[i]) | (uint)(ci._typeid << 5)) << 13) | 0x8000400;

                                if ((ci.parts_typeid[i] & def_part) == def_part)
                                {
                                }
                                else
                                {

                                    // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                    ci.unequipPart(part);

                                    upt_on_db = true;
                                }
                            }
                            else
                            {

                                var parts = Inventory.FindWarehouseItemById((int)ci.parts_id[i]);

                                if (parts != null)
                                {

                                    var slot = part.position_mask.getSlot((int)i);

                                    if (slot == false)
                                    {

                                        // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                        ci.unequipPart(part);

                                        upt_on_db = true;
                                    }
                                    else if (slot)
                                    {

                                        if (part.Level.GoodLevel((byte)UserInfo.Level))
                                        {

                                            if (angel_wings_part_num == -1 || parts._typeid != angel_wings_typeid || UserInfo.Statistics.getQuitRate() < 3.0f)
                                            {
                                            }
                                            else
                                            {

                                                // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                                ci.unequipPart(part);

                                                upt_on_db = true;
                                            }
                                        }
                                        else
                                        {

                                            // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                            ci.unequipPart(part);

                                            upt_on_db = true;
                                        }
                                    }
                                }
                                else
                                {

                                    // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                    ci.unequipPart(part);

                                    upt_on_db = true;
                                }
                            }
                        }
                        else
                        {

                            // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                            if (part != null)
                                ci.unequipPart(part);
                            else
                            {
                                part = sIff.Instance.findPart(_typeid: (def_part = ((i | (uint)(ci._typeid << 5)) << 13) | 0x8000400));
                                ci.parts_typeid[i] = (part != null) ? def_part : 0;
                                ci.parts_id[i] = 0;
                            }

                            upt_on_db = true;
                        }
                    }
                    else
                    {

                        var part = sIff.Instance.findPart(ci.parts_typeid[i]);

                        // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                        if (part != null)
                            ci.unequipPart(part);
                        else
                        {

                            part = sIff.Instance.findPart((def_part = ((i | (uint)(ci._typeid << 5)) << 13) | 0x8000400));
                            ci.parts_typeid[i] = (part != null) ? def_part : 0;
                            ci.parts_id[i] = 0;
                        }

                        upt_on_db = true;
                    }
                }
            }

            return upt_on_db;
        }

        private bool CheckCharacterEquiped(CharacterInfo ci)
        {
            for (int i = 0; i < 24; i++)
            {
                if (ci.parts_typeid[i] == 0)
                {
                    ci.parts_id[i] = 0;
                }
                else
                {
                    if (ci.parts_id[i] == 0)
                    {
                        ci.parts_typeid[i] = 0;
                    }
                }
            }
            return true;
        }


        public bool CheckSkinEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            uint tmp_typeid = 0;
            uint tmp_id = 0;

            for (var i = 0; i < (_ue.skin_typeid.Length); ++i)
            {

                if (_ue.skin_typeid[i] != 0)
                {

                    var pSkin = Inventory.FindWarehouseItemByTypeid(_ue.skin_typeid[i]);

                    if (pSkin == null)
                    {

                        // Guarda para usar no Log
                        tmp_typeid = _ue.skin_typeid[i];
                        tmp_id = _ue.skin_id[i];

                        // Zera (Desequipa)
                        _ue.skin_id[i] = 0;
                        _ue.skin_typeid[i] = 0;

                        upt_on_db = true;

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckSkinEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Skin[TYPEID=" + Convert.ToString(tmp_typeid) + ", ID=" + Convert.ToString(tmp_id) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var skin = sIff.Instance.findSkin(pSkin._typeid);

                        // Aqui tem que verificar as condições dos Title, uns só com 3% de quit Rate, % de acerto de pangya e etc

                        if (skin != null && !skin.Level.GoodLevel((byte)UserInfo.Level))
                        {

                            // Zera (Desequipa)
                            _ue.skin_id[i] = 0;
                            _ue.skin_typeid[i] = 0;

                            upt_on_db = true;

                            // Não tem o Level necessário para equipar esse Skin
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckSkinEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + " ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "]  Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(skin.Level.is_max) + ", Lv=" + Convert.ToString((ushort)skin.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (skin != null && sIff.Instance.IsTitle(pSkin._typeid))
                        {
                            //verifica se ele pode ter, silver, golden, bronze, e etc. sao titulos especiais, que precisam ter um, Check
                            //eu vou explicar melhor em outra parte
                            // Verifica se o TitleSkin tem condição e atualiza se tiver
                            uint title_num = sIff.Instance.getItemTitleNum(pSkin._typeid);

                            var Check_title = getTitleCallBack(title_num);

                            // Check_title == null, TitleSkin não tem condição
                            if (Check_title != null && !Check_title.Exec())
                            {

                                // Zera (Desequipa)
                                _ue.skin_id[i] = 0;
                                _ue.skin_typeid[i] = 0;

                                upt_on_db = true;

                                // Não passa na condição do TitleSkin, desequipa ele
                                _smp.LogManager.Instance.push(new AppMessage("[Player::CheckSkinEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + " ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "] nao passou na condition TITLE[NUM=" + Convert.ToString(title_num) + "], para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                        else if (skin == null)
                        {

                            // Zera (Desequipa)
                            _ue.skin_id[i] = 0;
                            _ue.skin_typeid[i] = 0;

                            upt_on_db = true;

                            // Não tem o Skin no IFF_STRUCT do server desequipa ele
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckSkinEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + ", ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public bool CheckPosterEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_typeid = 0;

            for (var i = 0; i < (_ue.poster.Length); ++i)
            {

                if (_ue.poster[i] != 0)
                {

                    var pPoster = Inventory.FindMyRoomItemByTypeid(_ue.poster[i]);

                    if (pPoster == null)
                    {

                        // Guarda para enviar no log
                        tmp_typeid = (int)_ue.poster[i];

                        // Zera (Desequipa)
                        _ue.poster[i] = 0;

                        upt_on_db = true;

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckPosterEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Poster[TYPEID=" + Convert.ToString(tmp_typeid) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var poster = sIff.Instance.findFurniture(pPoster._typeid);

                        if (poster != null && !poster.Level.GoodLevel((byte)UserInfo.Level))
                        {

                            // Zera (Desequipa)
                            _ue.poster[i] = 0;

                            upt_on_db = true;

                            // Não tem o Level necessário para equipar esse Poster
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckPosterEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Poster[TYPEID=" + Convert.ToString(pPoster._typeid) + " ID=" + Convert.ToString(pPoster.id) + ", SLOT=" + Convert.ToString(i) + "]  Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(poster.Level.is_max) + ", Lv=" + Convert.ToString((ushort)poster.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (poster == null)
                        {

                            // Zera (Desequipa)
                            _ue.poster[i] = 0;

                            upt_on_db = true;

                            // Não tem esse Poster no IFF_STRUCT do Server desequipa ele
                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckPosterEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Poster[TYPEID=" + Convert.ToString(pPoster._typeid) + ", ID=" + Convert.ToString(pPoster.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public bool CheckCharacterEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.character_id != 0)
            {

                if (Inventory.FindCharacterById(_ue.character_id) == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.character_id;

                    try
                    {

                        // Equipa Character Padrão
                        equipDefaultCharacter(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Character[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                } //else Character não tem tempo ou Level para equipar, então não precisa verificar o character se o player tiver ele

            }
            else
            {

                try
                {

                    // Equipa Character Padrão
                    equipDefaultCharacter(_ue);

                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não tem nenhum character Normal ou padrão equipado, equipado o character padrão
                _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCharacterEquiped][Error][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao tem um character equipado. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }

        public bool CheckCaddieEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.caddie_id != 0)
            {

                var pCaddie = Inventory.FindCaddieById(_ue.caddie_id);
                if (pCaddie == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.caddie_id;

                    // Zera (Desequipa)
                    _ue.caddie_id = 0;
                    Inventory.UserEquippedItem.CaddieEquiped = null;

                    upt_on_db = true;

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCaddieEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Caddie[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var caddie = sIff.Instance.findCaddie(pCaddie._typeid);

                    if (caddie != null && !caddie.Level.GoodLevel((byte)UserInfo.Level))
                    {

                        // Zera (Desequipa)
                        _ue.caddie_id = 0;
                        Inventory.UserEquippedItem.CaddieEquiped = null;

                        upt_on_db = true;

                        // Não tem o Level necessário para equipar esse caddie
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCaddieEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Caddie[TYPEID=" + Convert.ToString(pCaddie._typeid) + " ID=" + Convert.ToString(pCaddie.id) + " Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(caddie.Level.is_max) + ", Lv=" + Convert.ToString((ushort)caddie.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (caddie != null && pCaddie.rent_flag == 2 && (DateTime.Now >= pCaddie.end_date.ConvertTime())) // tempo acabou
                    {
                        // Desequipa
                        _ue.caddie_id = 0;
                        Inventory.UserEquippedItem.CaddieEquiped = null;

                        upt_on_db = true;

                        // Log do caddie expirado
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Player::CheckCaddieEquiped][Error] Normal[UID={UserInfo.UID}] Caddie[TYPEID={pCaddie._typeid} ID={pCaddie.id}, END_DATE={pCaddie.end_date.ConvertTime()}] expirou e não pode ser equipado. Hacker ou bug.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (caddie == null)
                    {

                        // Zera (Desequipa)
                        _ue.caddie_id = 0;
                        Inventory.UserEquippedItem.CaddieEquiped = null;

                        upt_on_db = true;

                        // Não tem esse caddie no IFF_STRUCT do server, desequipa ele
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckCaddieEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Caddie[TYPEID=" + Convert.ToString(pCaddie._typeid) + ", ID=" + Convert.ToString(pCaddie.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            return upt_on_db;
        }


        public bool CheckMascotEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;
            if (_ue.mascot_id != 0)
            {

                var pMascot = Inventory.FindMascotById(_ue.mascot_id);
                if (pMascot == null)
                {

                    // Guarda para usar no Log
                    tmp_id = _ue.mascot_id;

                    // Zera (Desequipa)
                    _ue.mascot_id = 0;
                    Inventory.UserEquippedItem.MascotEquiped = null;

                    upt_on_db = true;

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckMascotEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Mascot[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var mascot = sIff.Instance.findMascot(pMascot._typeid);

                    if (mascot != null && !mascot.Level.GoodLevel((byte)UserInfo.Level))
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        Inventory.UserEquippedItem.MascotEquiped = null;

                        upt_on_db = true;

                        // Não tem o Level necessário para equipar esse mascot, desequipa
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckMascotEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + " ID=" + Convert.ToString(pMascot.id) + " Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(mascot.Level.is_max) + ", Lv=" + Convert.ToString((ushort)mascot.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (mascot != null && pMascot.tipo == 1 && (DateTime.Now >= pMascot.data.ConvertTime()))
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        Inventory.UserEquippedItem.MascotEquiped = null;

                        upt_on_db = true;

                        // O tempo do mascot acabou, desequipa
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckMascotEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + " ID=" + Convert.ToString(pMascot.id) + ", END_DATE=" + (pMascot.data.ConvertTime()) + "] acabou o tempo do mascot, nao pode equipar esse mascot. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (mascot == null)
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        Inventory.UserEquippedItem.MascotEquiped = null;

                        upt_on_db = true;

                        // Não tem esse mascot no IFF_STRUCT do server, desequipa
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckMascotEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + ", ID=" + Convert.ToString(pMascot.id) + "] in IFF_STRCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            return upt_on_db;
        }


        public bool CheckClubSetEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.clubset_id != 0)
            {

                var pClubSet = Inventory.FindWarehouseItemById(_ue.clubset_id);

                if (pClubSet == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.clubset_id;

                    try
                    {

                        // Equipa ClubSet Padrão
                        equipDefaultClubSet(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have ClubSet[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var clubset = sIff.Instance.findClubSet(pClubSet._typeid);

                    if (clubset != null && !clubset.Level.GoodLevel((byte)UserInfo.Level))
                    {

                        try
                        {

                            // Equipa ClubSet Padrão
                            equipDefaultClubSet(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem o Level necessário para equipar esse clubset, equipa o clubset padrão
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] ClubSet[TYPEID=" + Convert.ToString(pClubSet._typeid) + " ID=" + Convert.ToString(pClubSet.id) + " Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(clubset.Level.is_max) + ", Lv=" + Convert.ToString((ushort)clubset.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (clubset == null)
                    {


                        try
                        {

                            // Equipa ClubSet Padrão
                            equipDefaultClubSet(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem esse clubset no IFF_STRUCT do server, equipa clubset padrão
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have ClubSet[TYPEID=" + Convert.ToString(pClubSet._typeid) + ", ID=" + Convert.ToString(pClubSet.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {

                try
                {

                    // Equipa ClubSet Padrão
                    equipDefaultClubSet(_ue);

                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não está com um clubset Normal ou padrão equipado, equipa o clubset padrão
                _smp.LogManager.Instance.push(new AppMessage("[Player::CheckClubSetEquiped][Error][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao esta com um ClubSet equipado. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }

        public bool CheckBallEquiped(UserEquip _ue)
        { 
            bool upt_on_db = false;
            if (_ue.ball_typeid != 0)
            {

                var pBall = Inventory.FindWarehouseItemByTypeid(_ue.ball_typeid);

                if (pBall == null)
                {
                    try
                    {

                        // Equipa Ball padrão
                        equipDefaultBall(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;
                }
                else
                {

                    var ball = sIff.Instance.findBall(pBall._typeid);

                    if (ball != null && !ball.Level.GoodLevel((byte)UserInfo.Level))
                    {

                        try
                        {

                            // Equipa Ball padrão
                            equipDefaultBall(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem o Level necessário para equipar a bola
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Ball[TYPEID=" + Convert.ToString(pBall._typeid) + " ID=" + Convert.ToString(pBall.id) + " Normal[Lv=" + Convert.ToString(UserInfo.Level) + "] nao tem o Level[is_max=" + Convert.ToString(ball.Level.is_max) + ", Lv=" + Convert.ToString((ushort)ball.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (ball == null)
                    {

                        try
                        {

                            // Equipa Ball padrão
                            equipDefaultBall(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem essa bola no IFF_STRUCT do server, equipa a bola padrão
                        _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] Not Have Ball[TYPEID=" + Convert.ToString(pBall._typeid) + ", ID=" + Convert.ToString(pBall.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {

                try
                {

                    // Equipa Ball padrão
                    equipDefaultBall(_ue);

                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não está com nenhuma bola Normal ou padrão equipada, equipa a bola padrão
                _smp.LogManager.Instance.push(new AppMessage("[Player::CheckBallEquiped][Error][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao esta com uma Ball equipada. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }
         
        public void CheckAllItemEquiped(UserEquip _ue)
        {

            if (CheckSkinEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateSkinEquiped(UserInfo.UID, _ue),
                      SQLDBResponse, this);
            }

            if (CheckPosterEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdatePosterEquiped(UserInfo.UID, _ue),
                      SQLDBResponse, this);
            }

            if (CheckCharacterEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateCharacterEquiped(UserInfo.UID, (int)_ue.character_id),
                      SQLDBResponse, this);
            }

            if (CheckCaddieEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateCaddieEquiped(UserInfo.UID, (int)_ue.caddie_id),
                      SQLDBResponse, this);
            }

            if (CheckMascotEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateMascotEquiped(UserInfo.UID, (int)_ue.mascot_id),
                      SQLDBResponse, this);
            }

            if (Inventory.CheckItemEquiped(_ue.item_slot))
            {
                NormalManagerDB.Instance.add(0, new CmdUpdateItemSlot(UserInfo.UID, _ue.item_slot), SQLDBResponse, this);
            }

            if (CheckClubSetEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateClubsetEquiped(UserInfo.UID, (int)_ue.clubset_id),
                      SQLDBResponse, this);
            }

            if (CheckBallEquiped(_ue))
            {
                NormalManagerDB.Instance.add(0,
                      new CmdUpdateBallEquiped(UserInfo.UID, _ue.ball_typeid),
                      SQLDBResponse, this);
            }
        }

        public void equipDefaultCharacter(UserEquip _ue)
        {

            // Valor padrão caso o adicionar Character de error
            var tmp_id = _ue.character_id;

            _ue.character_id = 0;
            Inventory.UserEquippedItem.CharacterEquiped = null;

            if (Inventory.Characters.Count() > 0)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultCharacter][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar o Character[ID=" + Convert.ToString(tmp_id) + "] para comecar o jogo, colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                Inventory.UserEquippedItem.CharacterEquiped = Inventory.Characters.First().Value;
                _ue.character_id = Inventory.UserEquippedItem.CharacterEquiped.id;

            }
            else
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultCharacter][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar o Character[ID=" + Convert.ToString(tmp_id) + "] para comecar o jogo, ele nao tem nenhum character. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = (uint)(sIff.Instance.CHARACTER << 26); // Nuri
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(UserInfo,
                    item, bi, false, 0, 0,
                    1);

                if (item._typeid != 0)
                {

                    // Add Item já atualiza o Character equipado
                    if ((item_id = ItemManager.addItem(item, this, 2, 0)) == RetAddItem.ERROR)

                    {
                        throw new exception("[player::equipDefaultCharacter][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu adicionar o Character[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2500, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultCharacter][Log][Warning][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu inicializar o Character[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2500, 1));
                }
            }
        }

        public void equipDefaultClubSet(UserEquip _ue)
        {

            // Guarda para usar no Log
            var tmp_id = _ue.clubset_id;

            // Valor padrão caso de erro no add ClubSet Padrão
            _ue.clubset_id = 0;
            Inventory.UserEquippedItem.Club_WI = null;
            Inventory.UserEquippedItem.ClubEquiped = new ClubSetInfo();

            _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultClubSet][Error] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar o Clubset[ID=" + Convert.ToString(tmp_id) + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
            var pWi = Inventory.FindWarehouseItemByTypeid(DEFAULT_CLUB_TYPEID);

            if (pWi != null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultClubSet][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar o ClubSet[ID=" + Convert.ToString(tmp_id) + "], mas acabou o tempo do ClubSet[ID=" + Convert.ToString(tmp_id) + @"], colocando o ClubSet Padrao""CV1"" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                // que no original fica no warehouse msm, eu só confundi quando fiz
                Inventory.UserEquippedItem.ClubEquiped.setValues(pWi.id, pWi._typeid, pWi.c);

                var cs = sIff.Instance.findClubSet(pWi._typeid);

                if (cs != null)
                {
                    for (var j = 0; j < (Inventory.UserEquippedItem.ClubEquiped.enchant_c.Length); ++j)
                    {
                        Inventory.UserEquippedItem.ClubEquiped.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                    }
                }

                Inventory.UserEquippedItem.Club_WI = pWi;
                _ue.clubset_id = pWi.id;

            }
            else
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultClubSet][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar o ClubSet[ID=" + Convert.ToString(tmp_id) + "], mas acabou o tempo do ClubSet[ID=" + Convert.ToString(tmp_id) + @"], ele nao tem o ClubSet Padrao""CV1"". Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = DEFAULT_CLUB_TYPEID;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(UserInfo,
                   item, bi, false, 0, 0,
                    1);

                if (item._typeid != 0)
                {
                    // Add Item já atualiza o Character equipado
                    if ((item_id = ItemManager.addItem(item, this, 2, 0)) != RetAddItem.ERROR)
                    {

                        // Equipa o ClubSet CV1
                        pWi = Inventory.FindWarehouseItemById(item_id);

                        if (pWi != null)
                        {

                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                            // que no original fica no warehouse msm, eu só confundi quando fiz
                            Inventory.UserEquippedItem.ClubEquiped.setValues(pWi.id, pWi._typeid, pWi.c);

                            var cs = sIff.Instance.findClubSet(pWi._typeid);

                            if (cs != null)
                            {
                                for (var j = 0; j < (Inventory.UserEquippedItem.ClubEquiped.enchant_c.Length); ++j)
                                {
                                    Inventory.UserEquippedItem.ClubEquiped.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                }
                            }

                            Inventory.UserEquippedItem.Club_WI = pWi;
                            Inventory.UserEquipment.clubset_id = pWi.id;

                            // Update ON DB
                            NormalManagerDB.Instance.add(0,
                                 new CmdUpdateClubsetEquiped(UserInfo.UID, item_id),
                                 SQLDBResponse, this);

                        }
                        else
                        {
                            throw new exception("[player::equipDefaultClubSet][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + @"] nao conseguiu achar o ClubSet""CV1""[ID=" + Convert.ToString(item.id) + "] padrao que acabou de adicionar para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                2501, 3));
                        }

                    }
                    else
                    {
                        throw new exception("[player::equipDefaultClubSet][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                            2501, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultClubSet][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2501, 1));
                }
            }
        }

        public void equipDefaultBall(UserEquip _ue)
        {

            // Verifica se o player é um premium user
            try
            {

                if (UserInfo.UserCapabilities.UserPremium)
                {

                    // Equipa a Ball Premium User Padrão
                    equipDefaultBallPremiumUser(_ue);

                    return; // Equipou a bola premium user com sucesso
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Guarda para usar no Log
            var tmp_typeid = _ue.ball_typeid;

            // Valor padrão caso de erro no adicionar a Ball padrão
            _ue.ball_typeid = DEFAULT_COMET_TYPEID;
            Inventory.UserEquippedItem.Ball_WI = null;

            var pWi = Inventory.FindWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

            if (pWi != null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBall][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                Inventory.UserEquippedItem.Ball_WI = pWi;
                _ue.ball_typeid = pWi._typeid;

            }
            else
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBall][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, ele nao tem a Ball Padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = DEFAULT_COMET_TYPEID;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(UserInfo,
                    item, bi, false, 0);

                if (item._typeid != 0)
                {

                    // Add Item já atualiza o Character equipado
                    if ((item_id = ItemManager.addItem(item, this, 2, 0)) != RetAddItem.ERROR)
                    {

                        // Equipa a Ball padrao
                        pWi = Inventory.FindWarehouseItemById((int)item_id);

                        if (pWi != null)
                        {

                            Inventory.UserEquippedItem.Ball_WI = pWi;
                            Inventory.UserEquipment.ball_typeid = pWi._typeid;

                            // Update ON DB
                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateBallEquiped(UserInfo.UID, (uint)item_id),
                                  SQLDBResponse, this);

                        }
                        else
                        {
                            throw new exception("[player::equipDefaultBall][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu achar a Ball[ID=" + Convert.ToString(item.id) + "] padrao que acabou de adicionar para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                2502, 3));
                        }

                    }
                    else
                    {
                        throw new exception("[player::equipDefaultBall][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu adicionar a Ball[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                            2502, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultBall][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] nao conseguiu inicializar a Ball[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2502, 1));
                }

            }
        }

        public void equipDefaultBallPremiumUser(UserEquip _ue)
        {

            // Guarda para usar no Log
            var tmp_typeid = _ue.ball_typeid;

            // Valor padrão caso de erro no adicionar a Ball Premium User padrão
            _ue.ball_typeid = sPremiumSystem.Instance.getPremiumBallByTicket(Inventory.PremiumTicket._typeid);
            Inventory.UserEquippedItem.Ball_WI = null;

            var pWi = Inventory.FindWarehouseItemByTypeid(_ue.ball_typeid);

            if (pWi != null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBallPremiumUser][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou verificar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, colocando a Ball Premium User Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                Inventory.UserEquippedItem.Ball_WI = pWi;
                _ue.ball_typeid = pWi._typeid;

            }
            else
            {

                _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBallPremiumUser][Log][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, ele nao tem a Ball Premium User Padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Add Premium Ball
                stItem item = sPremiumSystem.Instance.addPremiumBall(this);

                if (item._typeid != 0u)
                {
                    // Update ON DB
                    NormalManagerDB.Instance.add(0,
                          new CmdUpdateBallEquiped(UserInfo.UID, item._typeid),
                          SQLDBResponse, this);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[player::equipDefaultBallPremiumUser][ERROR][Warning] Normal[UID=" + Convert.ToString(UserInfo.UID) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, mas nao conseguiu adicionar a ball premium. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
        public List<CharacterInfo> isAuxPartEquiped(uint _typeid)
        {

            List<CharacterInfo> v_ci = new List<CharacterInfo>();

            Inventory.Characters.ToList().ForEach(_el =>
            {
                if (_el.Value.isAuxPartEquiped(_typeid))
                {
                    v_ci.Add(_el.Value);
                }
            });

            return v_ci;
        }

        public CharacterInfo isPartEquiped(uint _typeid)
        {
            var it = Inventory.Characters.FirstOrDefault(_el =>
            {
                return _el.Value.isPartEquiped(_typeid);
            });

            return it.Value;
        }

        public void setMemberInfo(PlayerMemberInfo MemberInfo)
        {
            UserInfo.Member = MemberInfo;
            UserInfo.UserCapabilities = MemberInfo.Capability;
            UserInfo.Member.OID = ConnectionID;
            //PlayerUserStatistics.mi.State.Visible = true;
            //PlayerUserStatistics.mi.State.Whisper = PlayerUserStatistics.Whisper;
            //PlayerUserStatistics.mi.State.Channel = !PlayerUserStatistics.Whisper.IsTrue();//passar true?
        }

        private void InitTitleCallbacks()
        {
            // Limpa se já existir algo
            if (BonusTitle == null)
                BonusTitle = new Dictionary<uint, TitleMapCallBack>();
            else
                BonusTitle.Clear();

            // No C#, passamos 'this' (a própria instância do Player) como o argumento do callback
            BonusTitle.Add(0x15, new TitleMapCallBack(BetterHitPangyaBronze, this));
            BonusTitle.Add(0x16, new TitleMapCallBack(BetterFairwayBronze, this));
            BonusTitle.Add(0x17, new TitleMapCallBack(BetterPuttBronze, this));
            BonusTitle.Add(0x18, new TitleMapCallBack(MasterCourse, this));
            BonusTitle.Add(0x19, new TitleMapCallBack(AtiradorDeOuro, this));
            BonusTitle.Add(0x1a, new TitleMapCallBack(AtiradorDeSilver, this));
            BonusTitle.Add(0x1b, new TitleMapCallBack(AtiradorDeBronze, this));
            BonusTitle.Add(0x1C, new TitleMapCallBack(BetterQuitRateBronze, this));

            // Silver
            BonusTitle.Add(0x32, new TitleMapCallBack(BetterHitPangyaSilver, this));
            BonusTitle.Add(0x33, new TitleMapCallBack(BetterFairwaySilver, this));
            BonusTitle.Add(0x34, new TitleMapCallBack(BetterPuttSilver, this));
            BonusTitle.Add(0x35, new TitleMapCallBack(BetterQuitRateSilver, this));

            // Records -> GP RECORD
            BonusTitle.Add(0x45, new TitleMapCallBack(NaturalRecord420, this));
            BonusTitle.Add(0x46, new TitleMapCallBack(NaturalRecord390, this));
            BonusTitle.Add(0x47, new TitleMapCallBack(NaturalRecord350, this));
            BonusTitle.Add(0x48, new TitleMapCallBack(NaturalRecord300, this));
            BonusTitle.Add(0x49, new TitleMapCallBack(NaturalRecord200, this));
            BonusTitle.Add(0x4a, new TitleMapCallBack(NaturalRecord80, this));

            // Gold
            BonusTitle.Add(0x7B, new TitleMapCallBack(BetterQuitRateGold, this));
            BonusTitle.Add(0x7C, new TitleMapCallBack(BetterPuttGold, this));
            BonusTitle.Add(0x7D, new TitleMapCallBack(BetterFairwayGold, this));
            BonusTitle.Add(0x7E, new TitleMapCallBack(BetterHitPangyaGold, this));

            // High Records -> GP RECORD
            BonusTitle.Add(0x17C, new TitleMapCallBack(NaturalRecord470, this));
            BonusTitle.Add(0x17D, new TitleMapCallBack(NaturalRecord540, this));
        }


        public TitleMapCallBack getTitleCallBack(uint _id)
        {
            if (BonusTitle.TryGetValue(_id, out TitleMapCallBack callback))
            {
                return callback;
            }

            return null;
        }


        private bool MasterCourse(object arg) => CheckStats(arg, p => p.UserInfo.isMasterCourse());
        //BRONZE
        private bool BetterHitPangyaBronze(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPangyaShotRate() >= 70.0f);
        private bool BetterFairwayBronze(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getFairwayRate() >= 70.0f);
        private bool BetterPuttBronze(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPuttRate() >= 80.0f);
        private bool BetterQuitRateBronze(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getQuitRate() <= 3.0f);
        //SILVER
        private bool BetterHitPangyaSilver(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPangyaShotRate() >= 77.0f);
        private bool BetterFairwaySilver(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getFairwayRate() >= 72.0f);
        private bool BetterPuttSilver(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPuttRate() >= 90.0f);
        private bool BetterQuitRateSilver(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getQuitRate() <= 2.0f);
        // Gold
        private bool BetterHitPangyaGold(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPangyaShotRate() >= 85.0f);
        private bool BetterFairwayGold(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getFairwayRate() >= 90.0f);
        private bool BetterPuttGold(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getPuttRate() >= 95.0f);
        private bool BetterQuitRateGold(object arg) => CheckStats(arg, p => p.UserInfo.Statistics.getQuitRate() <= 1.0f);

        // Medalhas da Temporada (Atirador)
        private bool AtiradorDeOuro(object arg) => CheckStats(arg, p => p.Inventory.CurrentTrophy.getSumGold() >= 10);
        private bool AtiradorDeSilver(object arg) => CheckStats(arg, p => p.Inventory.CurrentTrophy.getSumSilver() >= 10);
        private bool AtiradorDeBronze(object arg) => CheckStats(arg, p => p.Inventory.CurrentTrophy.getSumBronze() >= 10);

        // Records Naturais (Grand Prix JP)
        private bool NaturalRecord80(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -80);//dar pra fazer melhor, mais por enquanto, vou manter esse modelo de codigo..
        private bool NaturalRecord200(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -200);
        private bool NaturalRecord300(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -300);
        private bool NaturalRecord350(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -350);
        private bool NaturalRecord390(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -390);
        private bool NaturalRecord420(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -420);
        private bool NaturalRecord470(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -470);
        private bool NaturalRecord540(object arg) => CheckStats(arg, p => p.UserInfo.getSumRecordGrandPrix() <= -540);

        // Helper para evitar repetição de código (o antigo BEGIN/END macro)
        private bool CheckStats(object arg, Func<Player, bool> condition)
        {
            if (arg is Player player && condition(player))
                return true;
            return false;
        }


        public static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {
            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[player:SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // isso aqui depois pode mudar para o Item_manager, que vou tirar de ser uma classe static e usar ela como objeto(instancia)
            var _session = (Player)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Caddie Info
                    {
                        break;
                    }
                case 2: // Update Mascot Info
                    {
                        break;
                    }
                case 3: // Insert CPLog Item
                    {
                        var cmd_icpli = (CmdInsertCPLogItem)(_pangya_db);

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        public void SendChatNotice(string msg)
        {
            using (var p = new Packet(0x40))   // Msg to Chat of player
            {
                p.WriteByte(7);  // Notice

                p.WriteString(UserInfo.NickName);

                p.WriteString(msg);

                Send(p);
            }
        }
    }
}
