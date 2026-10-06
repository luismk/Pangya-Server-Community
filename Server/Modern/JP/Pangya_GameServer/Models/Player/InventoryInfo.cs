using Pangya_GameServer.Feature;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network.Models;
using snmdb;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Models
{
    /// <summary>
    /// Lida com Inventorio do jogador, como por exemplo: Itens Equipados, Itens no Inventorio, Troféus, MyRoom, etc...
    /// </summary>
    public class InventoryInfo
    {
        public uint uid { get; set; }

        // Managers & Systems
        /// <summary>
        /// Barra de selecao de itens
        /// </summary>
        public UserEquip UserEquipment { get; set; }
        /// <summary>
        /// Lida com personagens do jogador
        /// </summary>
        public CharacterManager Characters { get; set; }
        /// <summary>
        /// Lida com Caddies do jogador
        /// </summary>
        public CaddieManager Caddies { get; set; }
        /// <summary>
        /// Linda com mascotes do jogador
        /// </summary>
        public MascotManager Mascots { get; set; }
        /// <summary>
        /// Lida com Items(Inventorio), que são os itens que o jogador tem, mas não estão equipados, como por exemplo: Tacos, Partes, AuxPartes, etc...
        /// </summary>
        public ItemWarehouseManager WarehouseItems { get; set; }
        /// <summary>
        /// Lida com Card do jogador, que são os itens que dão bônus para o personagem, como por exemplo: Aumento de EXP, Aumento de Pang, etc... Mas não são equipados como os Cards Especiais, que dão bônus para o personagem e são equipados.
        /// </summary>
        public CardManager Cards { get; set; }
        /// <summary>
        /// Lida com Cards Equipados no personagem ou mesmo temporario
        /// </summary>
        public CardEquipManager CardEquipment { get; set; }
        /// <summary>
        /// Lida com item de buffer, que dao aumento de Experience e outras coisas.
        /// </summary>
        public List<ItemBuffEx> ItemBuffs { get; set; }
        /// <summary>
        /// Lida com Atualizacao ou expiracao dos itens
        /// </summary>
        public Dictionary<int, UpdateItem> UpdateItems { get; set; }
        /// <summary>
        /// Lida com cupom do gacha do jogador, voce pode jogar no gacha.
        /// </summary>
        public CouponGacha CouponGacha { get; set; }
        /// <summary>
        /// Class Special para guardar as informacoes antes de iniciar a partida
        /// </summary>
        public UserEquipedItem UserEquippedItem { get; set; }
        // Trophies Normal & Special.
        /// <summary>
        /// Trofeus ganhos/ganhou na sessao atual, ou seja, os trofeus que ele tem no momento, e os trofeus que ele ganhou na ultima
        /// </summary>
        public TrophyInfo CurrentTrophy { get; set; }
        /// <summary>
        /// trofeus ganhos na sessao passada
        /// </summary>
        public TrophyInfo RemainingTrophy { get; set; }
        /// <summary>
        /// Trofeus especial ganhos/ganhou na sessao atual, ou seja, os trofeus que ele tem no momento, e os trofeus que ele ganhou na ultima
        /// </summary>
        public List<TrophySpecialInfo> CurrentSpecialTrophies { get; set; }
        /// <summary>
        /// trofeus especiais ganhos na sessao passada
        /// </summary>
        public List<TrophySpecialInfo> RemainingSpecialTrophies { get; set; }
        /// <summary>
        /// Trofeus especiais do grand prix ganhos/ganhou na sessao atual, ou seja, os trofeus que ele tem no momento, e os trofeus que ele ganhou na ultima
        /// </summary>
        public List<TrophySpecialInfo> CurrentGrandPrixTrophies { get; set; }   // Trofel Grand Prix
        /// <summary>
        /// trofeus especiais do grand prix ganhos na sessao passada
        /// </summary>
        public List<TrophySpecialInfo> RemainingGrandPrixTrophies { get; set; } // Trofel Grand Prix

        // MyRoom
        /// <summary>
        /// Item como tv, post, e outros
        /// </summary>
        public List<MyRoomItem> MyRoomItems { get; set; }      // MyRoomItem 
        /// <summary>
        /// configuracao, X, Y, Z dos objetos do MyRomItems
        /// </summary>
        public MyRoomConfig MyRoomConfig { get; set; }
        /// <summary>
        /// Maleta guarda items, pangs
        /// </summary>
        public DolfiniLocker DolfineLocker { get; set; }
        // Stats
        public int ToTalClubSetCount { get; private set; }
        public int TotalPartsCount { get; private set; }
        // Workshop
        public ClubSetWorkshopLasUpLevel WorkshopLastUpLevel { get; set; }
        public ClubSetWorkshopTransformClubSet WorkshopTransform { get; set; }
        /// <summary>
        /// Informacoes sobre o Player Premium
        /// </summary>
        public PremiumTicket PremiumTicket { get; set; }
        public InventoryInfo()
        { 
            // Objetos Simples e Classes de Dados
            CouponGacha = new CouponGacha();
            UserEquippedItem = new UserEquipedItem();
            WorkshopLastUpLevel = new ClubSetWorkshopLasUpLevel();
            WorkshopTransform = new ClubSetWorkshopTransformClubSet();
            PremiumTicket = new PremiumTicket();
            CurrentTrophy = new TrophyInfo();
            RemainingTrophy = new TrophyInfo();
            UserEquipment = new UserEquip();
            MyRoomConfig = new MyRoomConfig();
            DolfineLocker = new DolfiniLocker();
            Characters = new CharacterManager();
            Caddies = new CaddieManager();
            Mascots = new MascotManager();
            WarehouseItems = new ItemWarehouseManager();
            Cards = new CardManager();
            CardEquipment = new CardEquipManager();
            ItemBuffs = new List<ItemBuffEx>();
            CurrentSpecialTrophies = new List<TrophySpecialInfo>();
            RemainingSpecialTrophies = new List<TrophySpecialInfo>();
            CurrentGrandPrixTrophies = new List<TrophySpecialInfo>();
            RemainingGrandPrixTrophies = new List<TrophySpecialInfo>();
            MyRoomItems = new List<MyRoomItem>();
            UpdateItems = new Dictionary<int, UpdateItem>();
        }

        public void Load(uint _uid)
        {
            uid = _uid;
            try
            {
                // Carregamento de dados básicos e sistemas
                CouponGacha = CommandDB.LoadCouponGacha(uid);
                UserEquipment = CommandDB.LoadUserEquip(uid);
                Characters = CommandDB.LoadCharacter(uid);
                Caddies = CommandDB.LoadCaddie(uid);
                WarehouseItems = CommandDB.LoadWarehouse(uid);
                Mascots = CommandDB.LoadMascot(uid);
                CardEquipment = CommandDB.LoadCardEquip(uid);
                ItemBuffs = CommandDB.LoadItemBuff(uid);
                PremiumTicket = CommandDB.LoadPremium(uid);
                Cards = CommandDB.LoadCard(uid);
                // MyRoom
                MyRoomConfig = CommandDB.LoadMyRoomConfig(uid);
                MyRoomItems = CommandDB.LoadMyRoomItem(uid);
                DolfineLocker = CommandDB.LoadDolfineInfo(uid);

                // Troféus (Normal e Especial)
                CurrentTrophy = CommandDB.LoadTrophy(uid, CmdTrofelInfo.TYPE_SEASON.CURRENT);
                RemainingTrophy = CommandDB.LoadTrophy(uid, CmdTrofelInfo.TYPE_SEASON.FOUR);

                CurrentSpecialTrophies = CommandDB.LoadTrophySpecial(uid, CmdTrophySpecial.TYPE_SEASON.CURRENT, CmdTrophySpecial.TYPE.NORMAL);
                CurrentGrandPrixTrophies = CommandDB.LoadTrophySpecial(uid, CmdTrophySpecial.TYPE_SEASON.CURRENT, CmdTrophySpecial.TYPE.GRAND_PRIX);

                // Update
                UpdateItems ??= [];
            }
            catch (Exception ex)
            {
                var msg = $"[Inventory::Load][Fatal] Erro ao carregar dados do UID: {uid}. Detalhes: {ex.Message}";
                _smp.LogManager.Instance.push(new AppMessage(msg, type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        public void SyncCharacter(int _id)
        {
            if (_id <= 0)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[InventoryInfo::SyncCharacter] Tentativa de atualizar Player[UID={uid}] com Character zerado! Bloqueado.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // 1. Verifica se o personagem existe no mapa de personagens do player
            var EquipChar = this.Characters[_id];

            // 3. Se for o personagem atual, sincroniza a referência principal 
            if (UserEquipment.character_id != _id)//atualiza o character e o Login
            {
                UserEquipment.character_id = _id;
                if (EquipChar != UserEquippedItem.CharacterEquiped)//so equipa se for diferente, isso evita a tropelar a memoria, rotativa.
                    UserEquippedItem.CharacterEquiped = EquipChar;

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[InventoryInfo::SyncCharacter][Sucess] Normal[UID: {uid}, CID: {_id}] UserEquip MAIN Update",
                    type_msg.CL_ONLY_CONSOLE));
            }
        }


        public void SyncCharacter(int _id, CharacterInfo newChar)
        {
            // 1. Validação de integridade (Sanity Check)
            if (newChar == null || newChar.id == 0 || newChar._typeid == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[InventoryInfo::SyncCharacter][Error] Player[UID={uid}] Character zerado ou nulo! Sync abortado.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // 2. Verifica se o personagem pertence ao inventário do player
            if (!Characters.ContainsKey(_id))
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[InventoryInfo::SyncCharacter][Warning] Player[UID={uid}] tentou sincronizar CID[{_id}] que não possui!",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // É importante atualizar sempre para garantir que novos atributos/roupas sejam aplicados
            this.Characters[_id] = newChar;

            // Se o ID que estamos sincronizando for o ID que o player está usando agora
            if (UserEquipment.character_id == _id)
            {
                // Atualiza a referência do objeto equipado para o novo objeto sincronizado
                UserEquippedItem.CharacterEquiped = newChar;

                _smp.LogManager.Instance.push(new AppMessage($"[InventoryInfo::SyncCharacter][Success] Normal[UID: {uid}, CID: {_id}] UPDATE AND SYNC.", type_msg.CL_ONLY_CONSOLE));
            }
        }
        #region FIND ITEM 

        public CharacterInfo FindCharacterById(int _id)
        {
            return this.Characters.findCharacterById(_id);
        }

        public CharacterInfo FindCharacterByTypeid(uint _typeid)
        {
            return this.Characters.findCharacterByTypeid(_typeid);
        }

        public MascotInfoEx FindMascotById(int _id)
        {
            return Mascots.findMascotById(_id);
        }

        public MascotInfoEx FindMascotByTypeid(uint _typeid)
        {
            return Mascots.findMascotByTypeid(_typeid);
        }

        public MyRoomItem FindMyRoomItemById(int _id)
        {
            return MyRoomItems.FirstOrDefault(el => el.id == _id);
        }

        public MyRoomItem FindMyRoomItemByTypeid(uint _typeid)
        {
            return MyRoomItems.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrophySpecialInfo FindTrofelEspecialById(int _id)
        {
            return CurrentSpecialTrophies.FirstOrDefault(el => el.id == _id);
        }

        public TrophySpecialInfo FindTrofelEspecialByTypeid(uint _typeid)
        {
            return CurrentSpecialTrophies.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrophySpecialInfo FindTrofelEspecialByTypeidAndId(uint _typeid, int _id)
        {
            return CurrentSpecialTrophies.FirstOrDefault(el => el.id == _id && el._typeid == _typeid);
        }

        public TrophySpecialInfo FindTrofelGrandPrixById(int _id)
        {
            return CurrentGrandPrixTrophies.FirstOrDefault(el => el.id == _id);
        }

        public TrophySpecialInfo FindTrofelGrandPrixByTypeid(uint _typeid)
        {
            return CurrentGrandPrixTrophies.FirstOrDefault(el => el._typeid == _typeid);
        }

        public TrophySpecialInfo FindTrofelGrandPrixByTypeidAndId(uint _typeid, int _id)
        {
            return CurrentGrandPrixTrophies.FirstOrDefault(el => el.id == _id && el._typeid == _typeid);
        }

        public WarehouseItemEx FindWarehouseItemById(int _id)
        {
            return WarehouseItems.findWarehouseItemById(_id);
        }

        public WarehouseItemEx FindWarehouseItemByTypeid(uint _typeid)
        {
            return WarehouseItems.findWarehouseItemByTypeid(_typeid);
        }

        public WarehouseItemEx FindWarehouseItemByTypeidAndId(uint _typeid, int _id)
        {
            return WarehouseItems.findWarehouseItemByTypeidAndId(_typeid, _id);
        }



        public CaddieInfoEx FindCaddieById(int _id)
        {
            return Caddies.findCaddieById(_id);
        }

        public CaddieInfoEx FindCaddieByTypeid(uint _typeid)
        {
            return Caddies.findCaddieByTypeid(_typeid);
        }

        public CardInfo FindCardById(int _id)
        {
            return Cards.findCardById(_id);
        }

        public CardInfo FindCardByTypeid(uint _typeid)
        {
            return Cards.findCardByTypeid(_typeid);
        }

        public CardEquipInfoEx FindCardEquipedById(int _id, int _char_typeid, int _slot)
        {
            return CardEquipment.FirstOrDefault(_element =>
            {
                return (_element.id == _id && ((_char_typeid == 0 && _slot == 0)
              || (_element.parts_typeid == _char_typeid && _element.slot == _slot)));
            });
        }

        public CardEquipInfoEx FindCardEquipedByTypeid(uint _typeid, int _char_typeid = 0, int _slot = 0, int _tipo = 0, int _efeito = 0)
        {
            return CardEquipment.FirstOrDefault(_element =>
            {
                return ((_element._typeid == _typeid || (_element.tipo == _tipo && _element.efeito == _efeito))
            && ((_char_typeid == 0 && _slot == 0) || (_element.parts_typeid == _char_typeid && _element.slot == _slot)));

            });
        }


        public Dictionary<int, UpdateItem> FindUpdateItemById(int id)
        {
            return UpdateItems
                .Where(it => it.Value.id == id)
                .ToDictionary(it => it.Key, it => it.Value);
        }

        public List<WarehouseItemEx> FindAllPartNotEquiped(uint _typeid)
        {

            List<WarehouseItemEx> v_item = new List<WarehouseItemEx>();

            foreach (var el in WarehouseItems)
            {

                var item = el.Value;

                bool hasPartEquipped = Characters.Any(el2 => el2.Value.isPartEquiped(item._typeid, item.id));

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

        public ItemBuffEx FindItemBuff(uint _typeid, uint _tipo = 0)
        {
            var it = ItemBuffs.FirstOrDefault(el =>
            {
                return (el._typeid == _typeid || el.tipo == _tipo);
            });

            return it;
        }

        #endregion

        public bool ItemExist(uint _typeid)
        {
            if (FindWarehouseItemByTypeid(_typeid) != null)
                return true;

            if (FindCardByTypeid(_typeid) != null)
                return true;

            if (FindCharacterByTypeid(_typeid) != null)
                return true;

            if (FindMascotByTypeid(_typeid) != null)
                return true;

            if (FindCardByTypeid(_typeid) != null)
                return true;

            return false;
        }

        public int getCharacterMaxSlot(byte stats, byte level)
        {
            CharacterInfo.Stats _stats = (CharacterInfo.Stats)stats;
            // pega o número máximo de slot de power do character equipado
            int value = 0;

            if (UserEquippedItem.CharacterEquiped == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getCharacterMaxSlotPower][Error][Warning] Normal[UID=" + (uid)
                        + "] nao tem nenhum character equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            var value_part = UserEquippedItem.CharacterEquiped.getSlotOfStatsFromCharEquipedPartItem(stats);
            var value_auxpart = UserEquippedItem.CharacterEquiped.getSlotOfStatsFromCharEquipedAuxPart(stats);
            var value_set_effect_table = UserEquippedItem.CharacterEquiped.getSlotOfStatsFromSetEffectTable(stats);
            var value_card = UserEquippedItem.CharacterEquiped.getSlotOfStatsFromCharEquipedCard(stats);//so obtem

            if (value_part == -1 || value_card == -1 || value_auxpart == -1 || value_set_effect_table == -1)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getCharacterMaxSlotPower][Error][Warning] Normal[UID="
                        + uid + "], value of slots stat[value=" + _stats + "] is invalid. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

            // Level + POWER, cada Level da +1 de POWER
            if (_stats == CharacterInfo.Stats.S_POWER)
            {
                value += ((level - 1) / 5);//base arrendondada
            }

            var mastery = sIff.Instance.findCharacterMastery(UserEquippedItem.CharacterEquiped._typeid);

            if (mastery.Count == 0)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getSlotPower][Error][Warning] Normal[UID=" + (uid)
                        + "] tentou pegar os slots stat[value=" + _stats + "] do Character[TYPEID=" + (UserEquippedItem.CharacterEquiped._typeid) + ", ID="
                        + (UserEquippedItem.CharacterEquiped.id) + "], mas nao tem o Character Mastery no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            if (mastery.Count() < UserEquippedItem.CharacterEquiped.mastery)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getSlotPower][Error][Warning] Normal[UID=" + (uid)
                        + "] tentou pegar os slots stat[value=" + _stats + "] do Character[TYPEID=" + (UserEquippedItem.CharacterEquiped._typeid)
                        + ", ID=" + (UserEquippedItem.CharacterEquiped.id) + "], mas o CharacterMastery[value=" + (UserEquippedItem.CharacterEquiped.mastery)
                        + ", vector_Count=" + (mastery.Count()) + "] do player e invalido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            var count = UserEquippedItem.CharacterEquiped.mastery;
            var bonus = 0;
            var pcl = UserEquippedItem.CharacterEquiped.getSlotOfStatsFromCharEquiped(stats);
            var rest = 0;
            for (var i = 0; i < count; ++i)
                if ((mastery[i].stats - 1) == stats)
                    bonus++;

            if (bonus > 0)
            {
                rest = value;
                value += bonus;
            }
            _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getCharacterMaxSlotPower][Warning] Normal[UID="
                    + uid + "], Stat[value=" + _stats + "], Slot[value=" + value + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            return value;
        }

        public int getClubSetMaxSlot(byte _stats)
        {

            int value = 0;

            if (UserEquippedItem.Club_WI == null || UserEquippedItem.Club_WI._typeid != UserEquippedItem.ClubEquiped._typeid)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getClubSetMaxSlotPower][Error][Warning] Normal[UID=" + (uid)
                        + "] nao tem o clubset equipado ou o ClubSet Info nao esta inicializado para o clubset equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            var clubset = sIff.Instance.findClubSet(UserEquippedItem.Club_WI._typeid);

            if (clubset == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[PlayerInfo::getClubSetMaxSlotPower][Error][Warning] Normal[UID=" + (uid)
                        + "] nao tem o ClubSet[TYPEID=" + (UserEquippedItem.Club_WI._typeid) + "] no IFF_STRUCT do server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return -1;
            }

            value = (clubset.SlotStats.getSlot[(byte)_stats] - clubset.Stats.getSlot[(byte)_stats]) + UserEquippedItem.Club_WI.clubset_workshop.c[(byte)_stats];

            return value;
        }

        public int getStatSlot(byte statIndex, byte level)
        {
            int total_slot = 0;

            // 1. Caddie
            if (UserEquippedItem.CaddieEquiped != null)
            {
                var cad = sIff.Instance.findCaddie(UserEquippedItem.CaddieEquiped._typeid);
                if (cad != null)
                    total_slot += cad.Stats.getSlot[statIndex];
            }

            // 2. Mascot
            if (UserEquippedItem.MascotEquiped != null)
            {
                var mascot = sIff.Instance.findMascot(UserEquippedItem.MascotEquiped._typeid);
                if (mascot != null)
                {
                    // Nota: Se a struct do Mascot não usar array, mapeie aqui:
                    if (statIndex == 0) total_slot += (int)mascot.Power;
                    else if (statIndex == 1) total_slot += (int)mascot.Control;
                    else if (statIndex == 2) total_slot += (int)mascot.Impact;
                    else if (statIndex == 3) total_slot += (int)mascot.Spin;
                    else if (statIndex == 4) total_slot += (int)mascot.Curve;
                }
            }

            // 3. Character & Parts
            if (UserEquippedItem.CharacterEquiped != null)
            {
                var character = sIff.Instance.findCharacter(UserEquippedItem.CharacterEquiped._typeid);
                if (character != null)
                {
                    // Base do Character no IFF (PCL)
                    total_slot += character.PCL[statIndex];

                    // Cálculo de Limite do Personagem (Usando o que descobrimos do IFF)
                    int maxCharValue = getCharacterMaxSlot(statIndex, level);
                    int currentUpgrade = UserEquippedItem.CharacterEquiped.pcl[statIndex];

                    if (maxCharValue != -1 && currentUpgrade > maxCharValue)
                        total_slot += maxCharValue;
                    else
                        total_slot += currentUpgrade;
                }
            }

            // 4. Cards Especiais
            foreach (var it in CardEquipment)
            {
                byte targetEffect = (byte)(5 + statIndex);
                if (it.parts_id == 0 && it.parts_typeid == 0 &&
                    sIff.Instance.getItemSubGroupIdentify22(it._typeid) == 2 && it.efeito == targetEffect)
                {
                    total_slot += (int)it.efeito_qntd;
                }
            }

            // 5. ClubSet (Tacos)
            if (UserEquippedItem.Club_WI != null && UserEquippedItem.Club_WI._typeid == UserEquippedItem.ClubEquiped._typeid)
            {
                var clubset = sIff.Instance.findClubSet(UserEquippedItem.Club_WI._typeid);
                if (clubset != null)
                    total_slot += clubset.SlotStats.getSlot[statIndex];

                // Cálculo de Limite do ClubSet
                int maxClubValue = getClubSetMaxSlot(statIndex);
                int currentClubUpgrade = UserEquippedItem.ClubEquiped.slot_c[statIndex];

                if (maxClubValue != -1 && currentClubUpgrade > maxClubValue)
                    total_slot += maxClubValue;
                else
                    total_slot += currentClubUpgrade;
            }

            return total_slot;
        }

        #region CHECK ITENS

        public bool CheckItemEquiped(uint[] item_slot)
        {
            bool upt_on_db = false;
            uint tmp_typeid = 0;
            WarehouseItemEx pWi = null;

            Dictionary<uint, uint> mp_count_same_item = new Dictionary<uint, uint>();

            for (int i = 0; i < item_slot.Length; ++i)
            {
                if (item_slot[i] != 0)
                {
                    if (!sIff.Instance.ItemEquipavel(item_slot[i]))
                    {
                        item_slot[i] = 0;

                        upt_on_db = true;

                        _smp.LogManager.Instance.push(new AppMessage("[Inventory::checkItemEquiped][Error] Normal[UID=" + uid +
                            "] Not Equipable Item[TYPEID=" + tmp_typeid + ", SLOT=" + i + "], but it is equiped. Hacker ou Bug",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if ((pWi = FindWarehouseItemByTypeid(item_slot[i])) == null)
                    {
                        item_slot[i] = 0;

                        upt_on_db = true;

                        _smp.LogManager.Instance.push(new AppMessage("[Inventory::checkItemEquiped][Error] Normal[UID=" + uid +
                            "] Not Have Item[TYPEID=" + tmp_typeid + ", SLOT=" + i + "], but it is equiped. Hacker ou Bug",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        if (mp_count_same_item.TryGetValue(pWi._typeid, out uint count))
                        {
                            if (active_item_cant_have_2_inventory.Contains(pWi._typeid))
                            {
                                item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.LogManager.Instance.push(new AppMessage("[Inventory::checkItemEquiped][Error] Normal[UID=" + uid +
                                    "] Nao pode equipar 2 Ex:[Corta com (Toma ou Safety)] Item[TYPEID=" + pWi._typeid +
                                    ", ID=" + pWi.id + "] no  Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else if (pWi.STDA_C_ITEM_QNTD < (int)(count + 1))
                            {
                                item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.LogManager.Instance.push(new AppMessage("[Inventory::checkItemEquiped][Error] Normal[UID=" + uid +
                                    "] Nao tem quantidade do Item[TYPEID=" + pWi._typeid + ", ID=" + pWi.id +
                                    "] para equipar ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                mp_count_same_item[pWi._typeid] = count + 1;
                            }
                        }
                        else
                        {
                            if (pWi.STDA_C_ITEM_QNTD < 1)
                            {
                                item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.LogManager.Instance.push(new AppMessage("[Inventory::checkItemEquiped][Error] Normal[UID=" + uid +
                                    "] Nao tem quantidade do Item[TYPEID=" + pWi._typeid + ", ID=" + pWi.id +
                                    "] para equipar ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                mp_count_same_item.Add(pWi._typeid, 1);
                            }
                        }
                    }
                }
            }

            return upt_on_db;
        }

        /// <summary>
        /// check para verificar se tem itens do Type b
        /// </summary>
        /// <returns></returns>
        public PlayerItemSpecialBoost CheckHaveItemBoost()
        {

            PlayerItemSpecialBoost ib = new();

            //Pang
            foreach (var _el in WarehouseItems.ToArray())
            {
                // Pang Boost X2
                // Verifica a quantidade do item para gastar menos processo se ele não tiver a quantidade necessária para ativar a PCBangMascot
                if (_el.Value.STDA_C_ITEM_QNTD > 0 && passive_item_pang_x2.Any(c => c == _el.Value._typeid))
                    ib.PangMastery = 1;

                // Pang Boost X4
                // Verifica a quantidade do item para gastar menos processo se ele não tiver a quantidade necessária para ativar a PCBangMascot
                if (_el.Value.STDA_C_ITEM_QNTD > 0 && passive_item_pang_x4.Any(c => c == _el.Value._typeid))
                    ib.PangNitro = 1;

                // Tenta não consumir mais processo, quando já estiver as duas PCBangMascot setada.
                // Tentando verificar outros itens que possa ter ainda no map
                if (ib.PangMastery == 1 && ib.PangNitro == 1)
                    break;
            }

            return ib;
        }


        public bool isAuxPartEquiped(uint _typeid)
        {
            var it = Characters.FirstOrDefault(el =>
            {
                return el.Value.isAuxPartEquiped(_typeid);
            });

            return it.Key != 0;
        }

        public bool isPartEquiped(uint _typeid, int _id)
        {
            var it = Characters.FirstOrDefault(el =>
            {
                return el.Value.isPartEquiped(_typeid, _id);
            });

            return it.Key != 0;
        }

        public bool ownerCaddieItem(uint _typeid)
        {
            var cad = FindCaddieByTypeid((7 << 26) | sIff.Instance.getCaddieIdentify(_typeid));

            // Se não tiver o caddie não pode ter o caddie item(parts caddie)
            // Verificar se tem o caddie, o caddie item não precisa
            if (cad == null /*|| cad.parts_typeid != _typeid*/)
                return true;

            return false;
        }

        public bool ownerHairStyle(uint _typeid)
        {
            var hair = sIff.Instance.findHairStyle(_typeid);

            if (hair != null)
            {
                var character = FindCharacterByTypeid((uint)((sIff.Instance.CHARACTER << 26) | hair.Character));

                if (character != null && character.default_hair == hair.Color)
                    return true;
            }

            return false;
        }

        public bool ownerItem(uint _typeid, int option = 0)
        {
            bool ret = false;

            // Verifica se ele tem no Dolfini Locker
            if (DolfineLocker.ownerItem(_typeid))
                return true;

            switch ((IFF_GROUP)sIff.Instance.getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CHARACTER:
                    if (FindCharacterByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.CADDIE:
                    if (FindCaddieByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.MASCOT:
                    if (FindMascotByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.CARD:
                    if (FindCardByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.FURNITURE:
                    if (FindMyRoomItemByTypeid(_typeid) != null)
                        ret = true;
                    break;
                case IFF_GROUP.BALL:
                case IFF_GROUP.AUX_PART:
                case IFF_GROUP.CLUBSET:
                case IFF_GROUP.ITEM:
                case IFF_GROUP.PART:
                case IFF_GROUP.SKIN:
                    if (FindWarehouseItemByTypeid(_typeid) != null)
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

            NormalManagerDB.Instance.add(0, cmd_fmbi, null, null);

            if (cmd_fmbi.getException().getCodeError() != 0)
                throw cmd_fmbi.getException();

            if (cmd_fmbi.hasFound())
                return true;

            return false;
        }

        public bool ownerSetItem(uint _typeid)
        {
            var set = sIff.Instance.findSetItem(_typeid);

            if (set != null)
            {
                for (var i = 0; i < set.packege.item_typeid.Length; ++i)
                {
                    // Eleminar a verificação do character que ele só inclui se o player não tiver ele
                    // se ele tiver não faz diferença não anula o verificação do set
                    if (set.packege.item_typeid[i] != 0 && sIff.Instance.getItemGroupIdentify(set.packege.item_typeid[i]) != IFF_GROUP.CHARACTER)
                        if (ownerItem(set.packege.item_typeid[i])) // se tiver 1 item que seja não pode ganhar o set se não vai duplicar os itens, que ele tem
                            return true;
                }
            }

            return false;
        }
        #endregion

        #region UPDATE ITEMS

        public void updateTrofelInfo(uint _trofel_typeid, byte _trofel_rank)
        {

            if (_trofel_typeid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + uid + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que é invalido(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

            if (_trofel_typeid == TROFEL_GM_EVENT_TYPEID/*GM Event*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + uid + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é Normal, é um TrophyID de evento GM. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 201, 0));

            uint type = sIff.Instance.getMatchTypeIdentity(_trofel_typeid);

            // Verifica se é o 2C e se o Tipo do Trofel é menor ou igual a 12, que é o Pro 7 o ultimo
            if (sIff.Instance.getItemSubGroupIdentify24(_trofel_typeid) != 0 && type > 12/*Pro 7*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + uid + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é Normal, é um outro TrophyID. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 202, 0));

            if (_trofel_rank == 0u || _trofel_rank > 3)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + uid + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] RankPosition é invalido. Bug,", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 203, 0));

            // Update Trofel Info Atual (season atual)
            CurrentTrophy.update(type, _trofel_rank);

            NormalManagerDB.Instance.add(4, new CmdUpdateNormalTrofel(uid, CurrentTrophy));
        }

        // Update Trofel Info Estático
        public static void updateTrofelInfo(uint _uid, uint _trofel_typeid, byte _trofel_rank)
        {

            if (_uid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + (_uid) + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "], mas UID is invalid(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 204, 0));

            if (_trofel_typeid == 0u)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + (_uid) + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que é invalido(zero). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 200, 0));

            if (_trofel_typeid == TROFEL_GM_EVENT_TYPEID/*GM Event*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + (_uid) + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é Normal, é um TrophyID de evento GM. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 201, 0));

            var type = sIff.Instance.getMatchTypeIdentity(_trofel_typeid);

            // Verifica se é o 2C e se o Tipo do Trofel é menor ou igual a 12, que é o Pro 7 o ultimo
            if (sIff.Instance.getItemSubGroupIdentify24(_trofel_typeid) != 0 && type > 12/*Pro 7*/)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + (_uid) + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] que nao é Normal, é um outro TrophyID. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 202, 0));

            if (_trofel_rank == 0u || _trofel_rank > 3)
                throw new exception("[PlayerInfo::updateTrofelInfo][Error] Normal[UID=" + (_uid) + "] tentou atualizar um TrophyID[TYPEID="
                        + (_trofel_typeid) + ", RANK=" + (_trofel_rank) + "] RankPosition é invalido. Bug,", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER_INFO, 203, 0));

            var cmd_ti = new CmdTrofelInfo(_uid, CmdTrofelInfo.TYPE_SEASON.CURRENT); // Waiter

            NormalManagerDB.Instance.add(0, cmd_ti);

            if (cmd_ti.getException().getCodeError() != 0)
                throw cmd_ti.getException();

            var ti = cmd_ti.getInfo();

            // Update Trofel Info Atual (season atual)
            ti.update(type, _trofel_rank);

            NormalManagerDB.Instance.add(4, new CmdUpdateNormalTrofel(_uid, ti));
        }

        #endregion

        #region HELPERS
        /// <summary>
        /// Character(460 bytes), Caddie(25 bytes), ClubSet(28 bytes), Mascot(62 bytes), Total Size 628 
        /// </summary>
        /// <returns>Equiped Item(628 array of byte)</returns>
        public byte[] GetUserEquipedItem()
        {
            return UserEquippedItem.ToArray();
        }

        /// <summary>
        /// Size = 116 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetUserEquipInfo()
        {
            return UserEquipment.ToArray();
        }

        /// <summary>
        /// Size = 78 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetTrophyInfo()
        {
            return CurrentTrophy.ToArray();
        }

        public void SetPremiumSys()
        {
            Player player = GameServer.Instance.FindPlayer(uid);
            if (player != null && IsPremium())
            {
                sPremiumSystem.Instance.updatePremiumUser(player);
            }
        }

        public bool IsPremium()
        {
            return sPremiumSystem.Instance.isPremium(PremiumTicket._typeid) && PremiumTicket.id != 0 && PremiumTicket.unix_sec_date > 0;
        }

        public void SetDefaultCharacter(Player player)
        {
            try
            {
                // 1. Tenta recuperar o personagem já equipado
                var currentId = UserEquipment.character_id;
                var charInfo = FindCharacterById(currentId);

                if (charInfo != null && charInfo._typeid != 0)
                {
                    UserEquippedItem.CharacterEquiped = charInfo;
                    return; // Personagem válido encontrado, encerra aqui.
                }

                // 2. Fallback: Se não encontrou, vamos dar o personagem padrão ao jogador
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SQLDB::Warning] Player[UID={uid}] não possui o personagem equipado (ID: {currentId}). Adicionando personagem padrão.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Prepara o item para adição
                BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_CHAR_TYPEID, qntd = 1 };
                stItem newItem = new stItem();

                // Inicializa a estrutura do item
                ItemManager.initItemFromBuyItem(player.UserInfo, newItem, bi, false, 0, 0, 1);

                if (newItem._typeid == 0)
                {
                    throw new exception($"[SQLDB::Error] Falha ao inicializar personagem padrão para UID: {uid}");
                }

                // 3. Adiciona ao Banco de Dados/Memória
                int newId = ItemManager.addItem(newItem, player, 2 /* Padrão */, 0);

                if (newId == (int)RetAddItem.ERROR)
                {
                    throw new exception($"[SQLDB::Error] Falha ao persistir personagem padrão no banco para UID: {uid}");
                }

                // 4. Localiza o personagem recém-criado no Warehouse
                newItem.id = newId;
                var addedChar = FindCharacterById(newId);

                if (addedChar != null && addedChar._typeid == DEFAULT_CHAR_TYPEID)
                {
                    UserEquippedItem.CharacterEquiped = addedChar;
                    UserEquipment.character_id = addedChar.id; // Atualiza o ID equipado na sessão

                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[SQLDB::Info] Personagem padrão atribuído com sucesso ao Player[UID={uid}].",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    throw new exception($"[SQLDB::Error] Player[UID={uid}] adicionou o personagem, mas não foi possível localizá-lo após a inserção.");
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SetDefaultCharacter::Critical] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        public void SetDefaultClub(Player player)
        {
            try
            {
                // 1. Tenta recuperar o ClubSet já equipado
                var currentClub = FindWarehouseItemById(UserEquipment.clubset_id);

                if (currentClub != null && UserEquipment.clubset_id != 0)
                {
                    EquipClubSetAction(currentClub);
                    return;
                }

                // 2. Fallback 1: Tenta encontrar o ClubSet Padrão no Warehouse (Inventário)
                var defaultClub = FindWarehouseItemByTypeid(DEFAULT_CLUB_TYPEID);

                if (defaultClub != null)
                {
                    EquipClubSetAction(defaultClub);
                    return;
                }

                // 3. Fallback 2: O player não tem o ClubSet Padrão. Vamos criar um novo (Air Knight).
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SQLDB::Warning] Player[UID={uid}] não possui ClubSet padrão. Criando novo...",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_CLUB_TYPEID, qntd = 1 };
                stItem newItem = new stItem();

                ItemManager.initItemFromBuyItem(player.UserInfo, newItem, bi, false, 0, 0, 1);

                int newId = ItemManager.addItem(newItem, player, 2 /* Padrão */, 0);

                if (newId != (int)RetAddItem.ERROR)
                {
                    var addedClub = FindWarehouseItemById(newId);
                    if (addedClub != null)
                    {
                        EquipClubSetAction(addedClub);
                        return;
                    }
                }

                // Se chegou aqui, falhou em todas as tentativas
                throw new exception($"[SQLDB::Error] Não foi possível atribuir ClubSet para o Player[UID={uid}].");
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SetDefaultClub::Critical] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        public void SetDefaultCaddie(Player player)
        {
            // Check Caddie Times
            PlayerManager.CheckCaddie(player);

            // Att Caddie Equipado que não tem nenhum caddie o player
            if (UserEquipment.caddie_id == 0)
            {
                UserEquippedItem.CaddieEquiped = null;
                return;
            }
            else
            {
                // É um Map, então depois usa o find com a Key, qui é mais rápido que rodar ele em um loop 
                if (Caddies.TryGetValue(UserEquipment.caddie_id, out CaddieInfoEx caddieInfo))
                    UserEquippedItem.CaddieEquiped = caddieInfo;
            }
        }

        public void SetDefaultMascot(Player player)
        {
            // Check Mascot Times
            PlayerManager.CheckMascot(player);

            // Att Mascot Equipado que não tem nenhum mascot o player
            if (UserEquipment.mascot_id == 0 || Mascots.Count() <= 0)
                UserEquipment.mascot_id = 0;
            else
            {
                // Mascot Info

                // É um Map, então depois usa o find com a Key, qui é mais rápido que rodar ele em um loop
                var it = Mascots.Values.FirstOrDefault(c => c.id == UserEquipment.mascot_id);

                if (it != null)
                    UserEquippedItem.MascotEquiped = it;
            }
        }

        public void SetDefaultComet(Player player)
        {
            try
            {
                // 1. Tenta recuperar a bola atualmente equipada no Warehouse
                var currentBall = FindWarehouseItemByTypeid(UserEquipment.ball_typeid);

                if (UserEquipment.ball_typeid != 0 && currentBall != null)
                {
                    UserEquippedItem.Ball_WI = currentBall;
                    return; // Sucesso, encerra o método
                }

                // 2. Fallback 1: Tenta encontrar a bola padrão (DEFAULT_COMET_TYPEID) no Warehouse
                var defaultBall = FindWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                if (defaultBall != null)
                {
                    UserEquipment.ball_typeid = DEFAULT_COMET_TYPEID;
                    UserEquippedItem.Ball_WI = defaultBall;
                    return;
                }

                // 3. Fallback 2: O player não tem a bola padrão no inventário. Vamos criá-la.
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SQLDB::Warning] Player[UID={uid}] sem Comet padrão (ID: {DEFAULT_COMET_TYPEID}). Adicionando...",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem { id = -1, _typeid = DEFAULT_COMET_TYPEID, qntd = 1 };
                stItem newItem = new stItem();

                // Inicializa a estrutura do item (ignora Level)
                ItemManager.initItemFromBuyItem(player.UserInfo, newItem, bi, false, 0, 0, 1);

                if (newItem._typeid == 0)
                {
                    throw new exception($"[SQLDB::Error] Falha ao inicializar struct da Comet para UID: {uid}");
                }

                // Adiciona ao Banco de Dados/Memória
                int newId = ItemManager.addItem(newItem, player, 2 /* Padrão */, 0);

                if (newId != (int)RetAddItem.ERROR)
                {
                    // Busca o item recém-adicionado para garantir a integridade
                    var addedItem = FindWarehouseItemById(newId);
                    if (addedItem != null)
                    {
                        UserEquipment.ball_typeid = DEFAULT_COMET_TYPEID;
                        UserEquippedItem.Ball_WI = addedItem;
                        return;
                    }
                }

                // Se falhou em tudo, lança erro crítico
                throw new exception($"[SQLDB::Error] Player[UID={uid}] não conseguiu receber a Comet padrão. Bug crítico.");
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[SetDefaultComet::Critical] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        /// <summary>
        /// Sub-método para evitar repetição da lógica de sincronização de stats e slots do IFF
        /// </summary>
        public void EquipClubSetAction(WarehouseItem item)
        {
            UserEquipment.clubset_id = item.id;
            UserEquippedItem.Club_WI = item;

            // Sincroniza valores base e enchant (Workshop)
            UserEquippedItem.ClubEquiped.setValues(item.id, item._typeid, item.c);

            var clubIff = sIff.Instance.findClubSet(item._typeid);
            if (clubIff != null)
            {
                // Calcula Stats: Base do IFF + Workshop/Enchant
                for (var i = 0; i < 5; ++i)
                {
                    UserEquippedItem.ClubEquiped.enchant_c[i] = (short)(clubIff.SlotStats.getSlot[i] + item.clubset_workshop.c[i]);
                }
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Club::Error] ClubSet[TypeID={item._typeid}] não encontrado no IFF Struct.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool IsClubSetValid(int clubsetId)
        {
            // 1. O ID é válido?
            if (clubsetId <= 0) return false;

            // 2. O item existe no inventário do player?
            var club = FindWarehouseItemById(clubsetId);
            if (club == null) return false;

            // 3. O item é realmente um ClubSet (evita injetar ID de outro item)
            if (sIff.Instance.getItemGroupIdentify(club._typeid) != IFF_GROUP.CLUBSET)
                return false;

            // 4. Verificação de Tempo (O item expirou?)
            // No Pangya, itens de tempo ficam no map mp_ui (Update Item)
            if (UpdateItems.ContainsKey(club.id))
            {
                var updateItem = UpdateItems[club.id];

                // Se for um item de tempo e o tempo atual passou do fim
                if (updateItem.type == UpdateItem.UI_TYPE.WAREHOUSE)
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            uid = 0;
            // Objetos Simples e Classes de Dados
            CouponGacha = null;
            UserEquippedItem = null;
            WorkshopLastUpLevel = null;
            WorkshopTransform = null;
            PremiumTicket = null;
            CurrentTrophy = null;
            RemainingTrophy= null;// = newTrophyInfo();
            UserEquipment= null;// = newUserEquip();
            MyRoomConfig= null;// = newMyRoomConfig();
            DolfineLocker= null;// = newDolfiniLocker();
            Characters.Clear();// = newCharacterManager();
            Caddies.Clear();// = newCaddieManager();
            Mascots.Clear();// = newMascotManager();
            WarehouseItems.Clear();// = newItemWarehouseManager();
            Cards.Clear();// = newCardManager();
            CardEquipment.Clear();// = newCardEquipManager();
            ItemBuffs.Clear();// = newList<ItemBuffEx>();
            CurrentSpecialTrophies.Clear();// = newList<TrophySpecialInfo>();
            RemainingSpecialTrophies.Clear();// = newList<TrophySpecialInfo>();
            CurrentGrandPrixTrophies.Clear();// = newList<TrophySpecialInfo>();
            RemainingGrandPrixTrophies.Clear();// = newList<TrophySpecialInfo>();
            MyRoomItems.Clear();// = newList<MyRoomItem>();
            UpdateItems.Clear();// = newDictionary<int, UpdateItem>();
        }

        #endregion
    }
}
