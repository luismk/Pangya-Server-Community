using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using PangyaAPI.IFF.JP.Models;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace PangyaAPI.IFF.JP.Extensions
{
    public class IFFHandle
    {
        public IFFFile<Part> Part { get; set; }
        public IFFFile<Item> Item { get; set; }
        public IFFFile<SetItem> Set_item { get; set; }
        public IFFFile<Mascot> Mascot { get; set; }
        public IFFFile<Achievement> Achievement { get; set; }
        public IFFFile<CounterItem> Counter_item { get; set; }
        public IFFFile<QuestStuff> Quest_stuff { get; set; }
        public IFFFile<QuestItem> Quest_item { get; set; }
        public IFFFile<AuxPart> Aux_part { get; set; }
        public IFFFile<Ball> Ball { get; set; }
        public IFFFile<Caddie> Caddie { get; set; }
        public IFFFile<CaddieItem> Caddie_item { get; set; }
        public IFFFile<Card> Card { get; set; }
        public IFFFile<Character> Character { get; set; }
        public IFFFile<Club> m_club { get; set; }
        public IFFFile<ClubSet> m_club_set { get; set; }
        public IFFFile<ClubSetWorkShopLevelUpProb> m_club_set_work_shop_level_up_prob { get; set; }
        public IFFFile<ClubSetWorkShopRankUpExp> m_club_set_work_shop_rank_exp { get; set; }
        public IFFFile<ClubSetWorkShopLevelUpLimit> m_club_set_work_shop_level_up_limit { get; set; }
        public IFFFile<Course> m_course { get; set; }
        public IFFFile<CutinInformation> m_cutin_infomation { get; set; }
        public IFFFile<Enchant> m_enchant { get; set; }
        public IFFFile<Furniture> m_furniture { get; set; }
        public IFFFile<HairStyle> m_hair_style { get; set; }
        public IFFFile<Match> m_match { get; set; }
        public IFFFile<Skin> m_skin { get; set; }
        public IFFFile<Ability> m_ability { get; set; }
        public IFFFile<Desc> m_desc { get; set; }
        public IFFFile<GrandPrixAIOptionalData> m_grand_prix_ai_optinal_data { get; set; }
        public IFFFile<GrandPrixConditionEquip> m_grand_prix_condition_equip { get; set; }
        public IFFFile<GrandPrixData> m_grand_prix_data { get; set; }
        public IFFFile<MemorialShopCoinItem> m_memorial_shop_coin_item { get; set; }
        public IFFFile<ArtifactManaInfo> m_artifact_mana_info { get; set; }
        public IFFFile<ErrorCodeInfo> m_error_code_info { get; set; }
        public IFFFile<HoleCupDropItem> m_hole_cup_drop_item { get; set; }
        public IFFFile<LevelUpPrizeItem> m_level_up_prize_item { get; set; }
        public IFFFile<NonVisibleItemTable> m_non_visible_item_table { get; set; }
        public IFFFile<PointShop> m_point_shop { get; set; }
        public IFFFile<ShopLimitItem> m_shop_limit_item { get; set; }
        public IFFFile<SpecialPrizeItem> m_special_prize_item { get; set; }
        public IFFFile<SubscriptionItemTable> m_subscription_item_table { get; set; }
        public IFFFile<SetEffectTable> m_set_effect_table { get; set; }
        public IFFFile<TikiPointTable> m_tiki_point_table { get; set; }
        public IFFFile<TikiRecipe> m_tiki_recipe { get; set; }
        public IFFFile<TikiSpecialTable> m_tiki_special_table { get; set; }
        public IFFFile<TimeLimitItem> m_time_limit_item { get; set; }
        public IFFFile<AddonPart> m_addon_part { get; set; }
        public IFFFile<CadieMagicBox> m_cadie_magic_box { get; set; }
        public IFFFile<CadieMagicBoxRandom> m_cadie_magic_box_random { get; set; }
        public IFFFile<CharacterMastery> m_character_mastery { get; set; }
        public IFFFile<GrandPrixRankReward> m_grand_prix_rank_reward { get; set; }
        public IFFFile<GrandPrixSpecialHole> m_grand_prix_special_hole { get; set; }
        public IFFFile<MemorialShopRareItem> m_memorial_shop_rare_item { get; set; }
        public IFFFile<CaddieVoiceTable> m_caddie_voice_table { get; set; }
        public IFFFile<FurnitureAbility> m_furniture_ability { get; set; }
        public IFFFile<TwinsItemTable> m_twins_item_table { get; set; }

        string PATH_PANGYA_IFF = "data/pangya_jp.iff";
        bool m_loaded;
        ZipFileEx Zip { get; set; }

        public int CHARACTER = (int)IFF_GROUP.CHARACTER;      // 4
        public int PART = (int)IFF_GROUP.PART;        // 8
        public int CLUB = (int)IFF_GROUP.CLUB;        // 12
        public int CLUBSET = (int)IFF_GROUP.CLUBSET;     // 16
        public int BALL = (int)IFF_GROUP.BALL;        // 20
        public int ITEM = (int)IFF_GROUP.ITEM;        // 24
        public int CADDIE = (int)IFF_GROUP.CADDIE;      // 28
        public int CAD_ITEM = (int)IFF_GROUP.CAD_ITEM;    // 32
        public int SET_ITEM = (int)IFF_GROUP.SET_ITEM;    // 36
        public int COURSE = (int)IFF_GROUP.COURSE;      // 40
        public int MATCH = (int)IFF_GROUP.MATCH;       // 44 Trofel
        public int TITLE = (int)IFF_GROUP.TITLE;
        public int ENCHANT = (int)IFF_GROUP.ENCHANT;       // 52
        public int SKIN = (int)IFF_GROUP.SKIN;        // 56
        public int HAIR_STYLE = (int)IFF_GROUP.HAIR_STYLE;  // 60
        public int MASCOT = (int)IFF_GROUP.MASCOT;      // 64
        public int CHILDITEM = (int)IFF_GROUP.CHILDITEM;
        public int FURNITURE = (int)IFF_GROUP.FURNITURE;     // 72
        public int OFFLINESHOP = (int)IFF_GROUP.OFFLINESHOP;
        public int ACHIEVEMENT = (int)IFF_GROUP.ACHIEVEMENT; // 76
        public int COUNTER_ITEM = (int)IFF_GROUP.COUNTER_ITEM;  // 108
        public int AUX_PART = (int)IFF_GROUP.AUX_PART;    // 112
        public int QUEST_STUFF = (int)IFF_GROUP.QUEST_STUFF; // 116
        public int QUEST_ITEM = (int)IFF_GROUP.QUEST_ITEM;  // 120
        public int CARD = (int)IFF_GROUP.CARD;        // 124
        public IFFHandle()
        {
            m_loaded = false;
        }
        ~IFFHandle()
        {
            m_loaded = false;
        }

        private IFFFile<T> MakeUnzipLoad<T>(string iffName) where T : new()
        {
            var mapIFF = new IFFFile<T>();

            try
            {
                if (!File.Exists(PATH_PANGYA_IFF))
                    throw new Exception($"[IFFHandle::MakeUnzipLoad][StError]: Falha ao ler arquivo: {PATH_PANGYA_IFF}");

                if (Zip == null)
                    Zip = new ZipFileEx(PATH_PANGYA_IFF);

                using (var zipArchive = ZipFile.OpenRead(PATH_PANGYA_IFF))
                {
                    mapIFF.Load(Zip.GetEntryBytes(iffName));
                    mapIFF.SetIffName(iffName);
                }

                return mapIFF;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IFFHandle::MakeUnzipLoad][StError]: {ex.Message}");
                return default;
            }
        }

        public void initilation()
        {
            try
            {
                if (m_loaded)
                    reset();

                m_point_shop = load_point_shop();
                m_addon_part = load_addon_part();
                m_error_code_info = load_error_code_info();
                m_club_set_work_shop_level_up_limit = load_club_set_work_shop_level_up_limit();
                m_club_set_work_shop_level_up_prob = load_club_set_work_shop_level_up_prob();
                m_club_set_work_shop_rank_exp = load_club_set_work_shop_rank_up_exp();
                Achievement = load_achievement();
                Item = load_item();
                Mascot = load_mascot();
                Aux_part = load_aux_part();
                Ball = load_ball();
                Caddie = load_caddie();
                Caddie_item = load_caddie_item();
                m_cadie_magic_box = load_cadie_magic_box();
                m_cadie_magic_box_random = load_cadie_magic_box_random();
                Card = load_card();
                Character = load_character();
                m_character_mastery = load_character_mastery();
                m_club = load_club();
                m_club_set = load_club_set();
                m_course = load_course();
                m_cutin_infomation = load_cutin_infomation();
                m_enchant = load_enchant();
                m_furniture = load_furniture();
                m_hair_style = load_hair_style();
                m_match = load_match();
                m_skin = load_skin();
                m_ability = load_ability();
                m_desc = load_desc();
                m_grand_prix_data = load_grand_prix_data();
                m_grand_prix_ai_optinal_data = load_grand_prix_ai_optional_data();
                m_grand_prix_condition_equip = load_grand_prix_condition_equip();
                m_grand_prix_rank_reward = load_grand_prix_rank_reward();  
                m_grand_prix_special_hole = load_grand_prix_special_hole();
                m_memorial_shop_coin_item = load_memorial_shop_coin_item();
                m_memorial_shop_rare_item = load_memorial_shop_rare_item();
                m_furniture_ability = load_furniture_ability();
                m_level_up_prize_item = load_level_up_prize_item();
                Counter_item = load_counter_item();
                m_set_effect_table = load_set_effect_table();
                m_tiki_point_table = load_tiki_point_table();
                m_tiki_recipe = load_tiki_recipe();
                m_tiki_special_table = load_tiki_special_table();
                Quest_item = load_quest_item();
                Quest_stuff = load_quest_stuff();
                Set_item = load_set_item();
                m_time_limit_item = load_time_limit_item();
               // m_twins_item_table = load_twins_item_table();
                Part = load_part();
                m_loaded = true;
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message($"[IFFHandle::Load][Error]: {ex.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw ex;
            }
        }

        private void reset()
        {
            // Lojas e Infos de Sistema
            m_point_shop.Clear();
            m_addon_part.Clear();
            m_error_code_info.Clear();

            // Workshop e Club Sets
            m_club_set_work_shop_level_up_limit.Clear();
            m_club_set_work_shop_level_up_prob.Clear();
            m_club_set_work_shop_rank_exp.Clear();

            // Itens Base e Conquistas
            Achievement.Clear();
            Item.Clear();
            Mascot.Clear();
            Aux_part.Clear();
            Ball.Clear();

            // Caddie e Magic Box
            Caddie.Clear();
            Caddie_item.Clear();
            m_cadie_magic_box.Clear();
            m_cadie_magic_box_random.Clear();

            // Customização e Personagem
            Card.Clear();
            Character.Clear();
            m_character_mastery.Clear();
            m_club.Clear();
            m_club_set.Clear();
            m_course.Clear();
            m_cutin_infomation.Clear();
            m_enchant.Clear();
            m_furniture.Clear();
            m_hair_style.Clear();

            // Mecânicas de Jogo e Atributos
            m_match.Clear();
            m_skin.Clear();
            m_ability.Clear();
            m_desc.Clear();

            // Grand Prix
            m_grand_prix_data.Clear();
            m_grand_prix_ai_optinal_data.Clear();
            m_grand_prix_condition_equip.Clear();
            m_grand_prix_rank_reward.Clear();
            m_grand_prix_special_hole.Clear();

            // Memorial Shop e Recompensas
            m_memorial_shop_coin_item.Clear();
            m_memorial_shop_rare_item.Clear();
            m_furniture_ability.Clear();
            m_level_up_prize_item.Clear();

            // Tiki Shop e Quests
            Counter_item.Clear();
            m_set_effect_table.Clear();
            m_tiki_point_table.Clear();
            m_tiki_recipe.Clear();
            m_tiki_special_table.Clear();
            Quest_item.Clear();
            Quest_stuff.Clear();
            Set_item.Clear();
            m_time_limit_item.Clear();
            Part.Clear();
            Zip.Dispose();//anula
            Zip = null;
            m_loaded = false;
        }

        public void reload()
        {
            reset();
            initilation();
        }

        public void reload(string data)
        {
            if (m_loaded)
                reset();
            PATH_PANGYA_IFF = data;
            m_loaded = false;
            Zip = new ZipFileEx(data);
            initilation();
        }
        private IFFFile<Achievement> load_achievement()
        {
            return MakeUnzipLoad<Achievement>("Achievement.iff");
        }

        private IFFFile<QuestItem> load_quest_item()
        {
            return MakeUnzipLoad<QuestItem>("QuestItem.iff");
        }

        private IFFFile<QuestStuff> load_quest_stuff()
        {
            return MakeUnzipLoad<QuestStuff>("QuestStuff.iff");
        }

        private IFFFile<CounterItem> load_counter_item()
        {
            return MakeUnzipLoad<CounterItem>("CounterItem.iff");
        }

        private IFFFile<Item> load_item()
        {
            return MakeUnzipLoad<Item>("Item.iff");
        }

        private IFFFile<Part> load_part()
        {
            return MakeUnzipLoad<Part>("Part.iff");
        }

        private IFFFile<AuxPart> load_aux_part()
        {
            return MakeUnzipLoad<AuxPart>("AuxPart.iff");
        }

        private IFFFile<Ball> load_ball()
        {
            return MakeUnzipLoad<Ball>("Ball.iff");
        }

        private IFFFile<Caddie> load_caddie()
        {
            return MakeUnzipLoad<Caddie>("Caddie.iff");
        }

        private IFFFile<CaddieItem> load_caddie_item()
        {
            return MakeUnzipLoad<CaddieItem>("CaddieItem.iff");
        }

        private IFFFile<CadieMagicBox> load_cadie_magic_box()
        {
            return MakeUnzipLoad<CadieMagicBox>("CadieMagicBox.iff");
        }

        private IFFFile<CadieMagicBoxRandom> load_cadie_magic_box_random()
        {
            return MakeUnzipLoad<CadieMagicBoxRandom>("CadieMagicBoxRandom.iff");
        }

        private IFFFile<Card> load_card()
        {
            return MakeUnzipLoad<Card>("Card.iff");
        }

        private IFFFile<Character> load_character()
        {
            return MakeUnzipLoad<Character>("Character.iff");
        }

        private IFFFile<CharacterMastery> load_character_mastery()
        {
            return MakeUnzipLoad<CharacterMastery>("CharacterMastery.iff");
        }

        private IFFFile<Club> load_club()
        {
            return MakeUnzipLoad<Club>("Club.iff");
        }

        private IFFFile<ClubSet> load_club_set()
        {
            return MakeUnzipLoad<ClubSet>("ClubSet.iff");
        }

        private IFFFile<ClubSetWorkShopLevelUpLimit> load_club_set_work_shop_level_up_limit()
        {
            return MakeUnzipLoad<ClubSetWorkShopLevelUpLimit>("ClubSetWorkShopLevelUpLimit.iff");
        }

        private IFFFile<ClubSetWorkShopLevelUpProb> load_club_set_work_shop_level_up_prob()
        {
            return MakeUnzipLoad<ClubSetWorkShopLevelUpProb>("ClubSetWorkShopLevelUpProb.iff");
        }

        private IFFFile<ClubSetWorkShopRankUpExp> load_club_set_work_shop_rank_up_exp()
        {
            return MakeUnzipLoad<ClubSetWorkShopRankUpExp>("ClubSetWorkShopRankUpExp.iff");
        }

        private IFFFile<Course> load_course()
        {
            return MakeUnzipLoad<Course>("Course.iff");
        }

        private IFFFile<CutinInformation> load_cutin_infomation()
        {
            return MakeUnzipLoad<CutinInformation>("CutinInfomation.iff");
        }

        private IFFFile<Enchant> load_enchant()
        {
            return MakeUnzipLoad<Enchant>("Enchant.iff");
        }

        private IFFFile<Furniture> load_furniture()
        {
            return MakeUnzipLoad<Furniture>("Furniture.iff");
        }

        private IFFFile<HairStyle> load_hair_style()
        {
            return MakeUnzipLoad<HairStyle>("HairStyle.iff");
        }

        private IFFFile<Match> load_match()
        {
            return MakeUnzipLoad<Match>("Match.iff");
        }

        private IFFFile<Skin> load_skin()
        {
            return MakeUnzipLoad<Skin>("Skin.iff");
        }

        private IFFFile<Ability> load_ability()
        {
            return MakeUnzipLoad<Ability>("Ability.iff");
        }

        private IFFFile<Desc> load_desc()
        {
            return MakeUnzipLoad<Desc>("Desc.iff");
        }

        private IFFFile<GrandPrixAIOptionalData> load_grand_prix_ai_optional_data()
        {
            return MakeUnzipLoad<GrandPrixAIOptionalData>("GrandPrixAIOptionalData.sff");
        }

        private IFFFile<GrandPrixConditionEquip> load_grand_prix_condition_equip()
        {
            return MakeUnzipLoad<GrandPrixConditionEquip>("GrandPrixConditionEquip.iff");
        }

        private IFFFile<GrandPrixData> load_grand_prix_data()
        {
            return MakeUnzipLoad<GrandPrixData>("GrandPrixData.iff");
        }

        private IFFFile<GrandPrixRankReward> load_grand_prix_rank_reward()
        {
            return MakeUnzipLoad<GrandPrixRankReward>("GrandPrixRankReward.iff");
        }

        private IFFFile<GrandPrixSpecialHole> load_grand_prix_special_hole()
        {
            return MakeUnzipLoad<GrandPrixSpecialHole>("GrandPrixSpecialHole.iff");
        }

        private IFFFile<MemorialShopCoinItem> load_memorial_shop_coin_item()
        {
            return MakeUnzipLoad<MemorialShopCoinItem>("MemorialShopCoinItem.sff");
        }

        private IFFFile<MemorialShopRareItem> load_memorial_shop_rare_item()
        {
            return MakeUnzipLoad<MemorialShopRareItem>("MemorialShopRareItem.iff");
        }
        private IFFFile<PointShop> load_point_shop()
        {
            return MakeUnzipLoad<PointShop>("PointShop.iff");
        }

        private IFFFile<AddonPart> load_addon_part()
        {
            return MakeUnzipLoad<AddonPart>("AddonPart.iff");
        }

        private IFFFile<ArtifactManaInfo> load_artifact_mana_info()
        {
            return MakeUnzipLoad<ArtifactManaInfo>("ArtifactManaInfo.iff");
        }

        private IFFFile<CaddieVoiceTable> load_caddie_voice_table()
        {
            return MakeUnzipLoad<CaddieVoiceTable>("CaddieVoiceTable.iff");
        }

        private IFFFile<ErrorCodeInfo> load_error_code_info()
        {
            return MakeUnzipLoad<ErrorCodeInfo>("ErrorCodeInfo.iff");
        }

        private IFFFile<FurnitureAbility> load_furniture_ability()
        {
            return MakeUnzipLoad<FurnitureAbility>("FurnitureAbility.iff");
        }

        private IFFFile<HoleCupDropItem> load_hole_cup_drop_item()
        {
            return MakeUnzipLoad<HoleCupDropItem>("HoleCupDropItem.iff");
        }

        private IFFFile<LevelUpPrizeItem> load_level_up_prize_item()
        {
            return MakeUnzipLoad<LevelUpPrizeItem>("LevelUpPrizeItem.iff");
        }

        private IFFFile<NonVisibleItemTable> load_non_visible_item_table()
        {
            return MakeUnzipLoad<NonVisibleItemTable>("NonVisibleItemTable.iff");
        }

        private IFFFile<ShopLimitItem> load_shop_limit_item()
        {
            return MakeUnzipLoad<ShopLimitItem>("ShopLimitItem.iff");
        }

        private IFFFile<SpecialPrizeItem> load_special_prize_item()
        {
            return MakeUnzipLoad<SpecialPrizeItem>("SpecialPrizeItem.iff");
        }

        private IFFFile<SubscriptionItemTable> load_subscription_item_table()
        {
            return MakeUnzipLoad<SubscriptionItemTable>("SubscriptionItemTable.iff");
        }

        private IFFFile<SetEffectTable> load_set_effect_table()
        {
            return MakeUnzipLoad<SetEffectTable>("SetEffectTable.iff");
        }

        private IFFFile<TikiPointTable> load_tiki_point_table()
        {
            return MakeUnzipLoad<TikiPointTable>("TikiPointTable.iff");
        }

        private IFFFile<TikiRecipe> load_tiki_recipe()
        {
            return MakeUnzipLoad<TikiRecipe>("TikiRecipe.iff");
        }

        private IFFFile<TikiSpecialTable> load_tiki_special_table()
        {
            return MakeUnzipLoad<TikiSpecialTable>("TikiSpecialTable.iff");
        }

        private IFFFile<TimeLimitItem> load_time_limit_item()
        {
            return MakeUnzipLoad<TimeLimitItem>("TimeLimitItem.iff");
        }

        private IFFFile<TwinsItemTable> load_twins_item_table()
        {
            return MakeUnzipLoad<TwinsItemTable>("TwinsItemTable.iff");
        }

        private IFFFile<SetItem> load_set_item()
        {
            return MakeUnzipLoad<SetItem>("SetItem.iff");
        }

        private IFFFile<Mascot> load_mascot()
        {
            return MakeUnzipLoad<Mascot>("Mascot.iff");
        }
         
        public PointShop findPointShop(uint _typeid)
        {
            return m_point_shop.FirstOrDefault(c => c.ID == _typeid);
        }
        public AuxPart findAuxPart(uint _typeid)
        {
            return Aux_part.FirstOrDefault(c => c.ID == _typeid);
        }

        public Ball findBall(uint _typeid)
        {
            return Ball.FirstOrDefault(c => c.ID == _typeid);
        }

        public Caddie findCaddie(uint _typeid)
        {
            return Caddie.FirstOrDefault(c => c.ID == _typeid);
        }

        public CaddieItem findCaddieItem(uint _typeid)
        {
            return Caddie_item.FirstOrDefault(c => c.ID == _typeid);
        }

        public CadieMagicBox findCadieMagicBox(uint _seq)
        {
            return m_cadie_magic_box.FirstOrDefault(e => e.seq == _seq);
        }

        public Card findCard(uint _typeid)
        {
            return Card.FirstOrDefault(c => c.ID == _typeid);
        }

        public Character findCharacter(uint _typeid)
        {
            return Character.FirstOrDefault(c => c.ID == _typeid);
        }

        public List<CharacterMastery> findCharacterMastery(uint _typeid)
        {
            return m_character_mastery.Where(c => c._typeid == _typeid).ToList();
        }

        public ClubSetWorkShopLevelUpProb findClubSetWorkShopLevelUpProb(int _tipo)
        {
            return m_club_set_work_shop_level_up_prob.FirstOrDefault(c => c.tipo == _tipo);
        }

        public List<ClubSetWorkShopLevelUpLimit> findClubSetWorkShopLevelUpLimit(int _tipo)
        {
            return m_club_set_work_shop_level_up_limit.Where(c => c.tipo == _tipo).ToList();
        }

        public ClubSetWorkShopRankUpExp findClubSetWorkShopRankExp(uint _tipo)
        {
            return m_club_set_work_shop_rank_exp.FirstOrDefault(c => c.tipo == _tipo);
        }

        public Club findClub(uint _typeid)
        {
            return m_club.FirstOrDefault(c => c.ID == _typeid);
        }

        public ClubSet findClubSet(uint _typeid)
        {
            return m_club_set.FirstOrDefault(c => c.ID == _typeid);

        }

        public Achievement findAchievement(uint _typeid)
        {
            return Achievement.FirstOrDefault(c => c.ID == _typeid);
        }


        // Find
        public Part findPart(uint _typeid)
        {
            return Part.FirstOrDefault(c => c.ID == _typeid);
        }

        public Item findItem(uint _typeid)
        {
            return Item.FirstOrDefault(c => c.ID == _typeid);
        }

        public Mascot findMascot(uint _typeid)
        {
            return Mascot.FirstOrDefault(c => c.ID == _typeid);
        }

        public QuestItem findQuestItem(uint _typeid)
        {
            return Quest_item.FirstOrDefault(c => c.ID == _typeid);
        }

        public QuestStuff findQuestStuff(uint _typeid)
        {
            return Quest_stuff.FirstOrDefault(c => c.ID == _typeid);
        }

        public SetItem findSetItem(uint _typeid)
        {
            return Set_item.FirstOrDefault(c => c.ID == _typeid);
        }


        public Course findCourse(uint _typeid)
        {
            return m_course.FirstOrDefault(c => c.ID == _typeid);
        }

        public CutinInformation findCutinInfomation(uint _typeid)
        {
            return m_cutin_infomation.FirstOrDefault(c => c._typeid == _typeid);
        }

        public Enchant findEnchant(uint _typeid)
        {
            return m_enchant.FirstOrDefault(_el => _el.TypeID == _typeid);
        }

        public Furniture findFurniture(uint _typeid)
        {
            return m_furniture.FirstOrDefault(_el => _el.ID == _typeid);
        }

        public HairStyle findHairStyle(uint _typeid)
        {
            return m_hair_style.FirstOrDefault(c => c.ID == _typeid);
        }

        public Match findMatch(uint _typeid)
        {
            return m_match.FirstOrDefault(c => c.ID == _typeid);
        }

        public Skin findSkin(uint _typeid)
        {
            return m_skin.FirstOrDefault(c => c.ID == _typeid);
        }

        public Ability findAbility(uint _typeid)
        {
            return m_ability.FirstOrDefault(c => c.ID == _typeid);
        }

        public Desc findDesc(uint _typeid)
        {
            return m_desc.FirstOrDefault(c => c.ID == _typeid);
        }

        public GrandPrixData findGrandPrixData(uint _typeid)
        {
            return m_grand_prix_data.FirstOrDefault(c => c.ID == _typeid);
        }
        public List<GrandPrixSpecialHole> findGrandPrixSpecialHole(uint _typeid)
        {
            return m_grand_prix_special_hole.Where(c => c.TypeID == _typeid).ToList();
        }


        public MemorialShopCoinItem findMemorialShopCoinItem(uint _typeid)
        {
            return m_memorial_shop_coin_item.FirstOrDefault(c => c.ID == _typeid);
        }

        public LevelUpPrizeItem findLevelUpPrizeItem(uint _level)
        {
            return m_level_up_prize_item.FirstOrDefault(c => c.Level == _level);
        }

        public SetEffectTable findSetEffectTable(uint _id)
        {
            return m_set_effect_table.FirstOrDefault(c => c.Index == _id);
        }

        public TikiPointTable findTikiPointTable(uint _id)
        {
            return m_tiki_point_table.FirstOrDefault(e => e.Index == _id);
        }

        public TikiRecipe findTikiRecipe(uint _id)
        {
            return m_tiki_recipe.FirstOrDefault(c => c.TypeID == _id);
        }

        public Dictionary<uint, CadieMagicBoxRandom> findCadieMagicBoxRandom(uint _id)
        {
            var cadie = m_cadie_magic_box_random.FirstOrDefault(c=> c.ID == _id);
            var dic = new Dictionary<uint, CadieMagicBoxRandom>();
            if (cadie != null)
            {
                dic.Add(_id, cadie);
                return dic;
            }
            return dic;
        }

        public MemorialShopRareItem findMemorialShopRareItem(uint _gacha_num)
        {
            return m_memorial_shop_rare_item.FirstOrDefault(e => e.gacha.Number == _gacha_num);
        }

        public CounterItem findCounterItem(uint _typeid)
        {
            return Counter_item.FirstOrDefault(c => c.ID == _typeid);
        }

        public TimeLimitItem findTimeLimitItem(uint _typeid)
        {
            return m_time_limit_item.FirstOrDefault(_el => _el._typeid == _typeid);
        }

        public bool ItemEquipavel(uint _typeid)
        {
            return ((_typeid & 0xFE000000) >> 25 & 3) == 0;
        }

        public bool IsBuyItem(uint _typeid)
        {

            var commom = findCommomItem(_typeid);

            if (commom != null)
                return (commom.Active && !commom.IsHide);

            return false;
        }


        public T findItem<T>(uint _typeid)
        {
            T commom = default;

            try
            {
                switch ((IFF_GROUP)getItemGroupIdentify(_typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        commom = (T)Activator.CreateInstance(findCharacter(_typeid).GetType());
                        break;
                    case IFF_GROUP.PART:
                        commom = (T)Activator.CreateInstance(findPart(_typeid).GetType());
                        break;
                    case IFF_GROUP.CLUB:
                        commom = (T)Activator.CreateInstance(findClub(_typeid).GetType());
                        break;
                    case IFF_GROUP.CLUBSET:
                        commom = (T)Activator.CreateInstance(findClubSet(_typeid).GetType());
                        break;
                    case IFF_GROUP.BALL:
                        commom = (T)Activator.CreateInstance(findBall(_typeid).GetType());
                        break;
                    case IFF_GROUP.ITEM:
                        commom = (T)Activator.CreateInstance(findItem(_typeid).GetType());
                        break;
                    case IFF_GROUP.CADDIE:
                        commom = (T)Activator.CreateInstance(findCaddie(_typeid).GetType());
                        break;
                    case IFF_GROUP.CAD_ITEM:
                        commom = (T)Activator.CreateInstance(findCaddieItem(_typeid).GetType());
                        break;
                    case IFF_GROUP.SET_ITEM:
                        commom = (T)Activator.CreateInstance(findSetItem(_typeid).GetType());
                        break;
                    case IFF_GROUP.COURSE:
                        commom = (T)Activator.CreateInstance(findCourse(_typeid).GetType());
                        break;
                    case IFF_GROUP.SKIN:
                        commom = (T)Activator.CreateInstance(findSkin(_typeid).GetType());
                        break;
                    case IFF_GROUP.HAIR_STYLE:
                        commom = (T)Activator.CreateInstance(findHairStyle(_typeid).GetType());
                        break;
                    case IFF_GROUP.MASCOT:
                        commom = (T)Activator.CreateInstance(findMascot(_typeid).GetType());
                        break;
                    case IFF_GROUP.FURNITURE:
                        commom = (T)Activator.CreateInstance(findFurniture(_typeid).GetType());
                        break;
                    case IFF_GROUP.ACHIEVEMENT:
                        commom = (T)Activator.CreateInstance(findAchievement(_typeid).GetType());
                        break;
                    case IFF_GROUP.COUNTER_ITEM:
                        commom = (T)Activator.CreateInstance(findCounterItem(_typeid).GetType());
                        break;
                    case IFF_GROUP.AUX_PART:
                        commom = (T)Activator.CreateInstance(findAuxPart(_typeid).GetType());
                        break;
                    case IFF_GROUP.QUEST_STUFF:
                        commom = (T)Activator.CreateInstance(findQuestStuff(_typeid).GetType());
                        break;
                    case IFF_GROUP.QUEST_ITEM:
                        commom = (T)Activator.CreateInstance(findQuestItem(_typeid).GetType());
                        break;
                    case IFF_GROUP.CARD:
                        commom = (T)Activator.CreateInstance(findCard(_typeid).GetType());
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return commom;
        }
        public IFFCommon findCommomItem(uint _typeid)
        {
            IFFCommon commom = null;

            try
            {
                switch ((IFF_GROUP)getItemGroupIdentify(_typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        commom = Character.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.PART:
                        commom = Part.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.CLUB:
                        commom = m_club.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.CLUBSET:
                        commom = m_club_set.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.BALL:
                        commom = Ball.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.ITEM:
                        commom = Item.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.CADDIE:
                        commom = Caddie.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.CAD_ITEM:
                        commom = Caddie_item.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.SET_ITEM:
                        commom = Set_item.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.COURSE:
                        commom = m_course.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.SKIN:
                        commom = m_skin.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.HAIR_STYLE:
                        commom = m_hair_style.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.MASCOT:
                        commom = Mascot.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.FURNITURE:
                        commom = m_furniture.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.ACHIEVEMENT:
                        commom = Achievement.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.COUNTER_ITEM:
                        //  commom = findCounterItem(_typeid);
                        break;
                    case IFF_GROUP.AUX_PART:
                        commom = Aux_part.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.QUEST_STUFF:
                        commom = Quest_stuff.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.QUEST_ITEM:
                        commom = Quest_item.GetItemCommon(_typeid);
                        break;
                    case IFF_GROUP.CARD:
                        commom = Card.GetItemCommon(_typeid);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return commom;
        }
        public string GetItemName(uint typeid)
        {
            var common = findCommomItem(typeid);
            if (common != null)
            {
                return common.Name;
            }
            return "null";
        }


        public bool ItemDef(uint _typeid, uint typeid)
        {
            var Character = (CharacterType)_typeid;

            switch (Character)
            {
                case CharacterType.NURI:
                    _typeid = 67108864;
                    break;
                case CharacterType.HANA:
                    _typeid = 67108865;
                    break;
                case CharacterType.AZER:
                    _typeid = 67108866;
                    break;
                case CharacterType.CECILIA:
                    _typeid = 67108867;
                    break;
                case CharacterType.MAX:
                    _typeid = 67108868;
                    break;
                case CharacterType.KOOH:
                    _typeid = 67108869;
                    break;
                case CharacterType.ARIN:
                    _typeid = 67108870;
                    break;
                case CharacterType.KAZ:
                    _typeid = 67108871;
                    break;
                case CharacterType.LUCIA:
                    _typeid = 67108872;
                    break;
                case CharacterType.NELL:
                    _typeid = 67108873;
                    break;
                case CharacterType.SPIKA:
                    typeid = 67108874;
                    break;
                case CharacterType.NURI_R:
                    _typeid = 67108875;
                    break;
                case CharacterType.HANA_R:
                    _typeid = 67108876;
                    break;
                case CharacterType.AZER_R:
                    _typeid = 67108877;
                    break;
                case CharacterType.CECILIA_R:
                    _typeid = 67108878;
                    break;
                default:
                    break;
            }
            bool check_ = false;
            for (int i = 0; i < 24; ++i)
            {
#pragma warning disable CS0675 // Bit a bit ou operador usado em um operando de assinatura estendida
                var part_typeid = (((_typeid << 5/*CharIdentify*/) | i) << 13/*PartNum*/) | 0x8000400;
#pragma warning restore CS0675 // Bit a bit ou operador usado em um operando de assinatura estendida

                var item = findPart((uint)part_typeid);
                if (item.ID > 0)
                {
                    part_typeid = findPart((uint)part_typeid).ID;
                    check_ = part_typeid == typeid;
                    if (check_)
                    {
                        break;
                    }
                }
            }
            return check_;
        }

        public bool IsGiftItem(uint _typeid)
        {
            var commom = findCommomItem(_typeid);

            // É saleable ou giftable nunca os 2 juntos por que é a flag composta Somente Purchase(compra)
            // então faço o xor nas 2 flag se der o valor de 1 é por que ela é um item que pode presentear
            // Ex: 1 + 1 = 2 Não é
            // Ex: 1 + 0 = 1 OK
            // Ex: 0 + 1 = 1 OK
            // Ex: 0 + 0 = 0 Não é
            if (commom != null)
                return (commom.Active && commom.Shop.flag_shop.IsCash
                    && (commom.Shop.flag_shop.is_saleable ^ commom.Shop.flag_shop.IsGift));

            return false;
        }

        public bool IsOnlyDisplay(uint _typeid)
        {
            var commom = findCommomItem(_typeid);

            if (commom != null)
                return (commom.Active && commom.Shop.flag_shop.IsDisplay);

            return false;
        }

        public bool IsOnlyPurchase(uint _typeid)
        {
            var commom = findCommomItem(_typeid);

            if (commom != null)
                return (commom.Active && commom.Shop.flag_shop.is_saleable
                    && commom.Shop.flag_shop.IsGift);

            return false;
        }

        public bool IsOnlyGift(uint _typeid)
        {
            var commom = findCommomItem(_typeid);

            if (commom != null)
                return commom.IsOnlyGift();

            return false;
        }

        public IFF_GROUP getItemGroupIdentify(uint _typeid)
        {
            return (IFF_GROUP)((_typeid & 0xFC000000) >> 26);
        }

        public uint getItemSubGroupIdentify24(uint _typeid)
        {
            return (uint)((_typeid & ~0xFC000000) >> 24);       // aqui é >> 24, mas deixei 25 por causa do item equipável e o passivo, mas posso mudar depois isso
        }

        public uint getItemSubGroupIdentify22(uint _typeid)
        {
            return (uint)((_typeid & ~0xFC000000) >> 22);       // esse retorno os grupos divididos em 0x40 0x80 0xC0, 0x100, 0x140
        }

        public uint getItemSubGroupIdentify21(uint _typeid)
        {
            return (uint)((_typeid & ~0xFC000000) >> 21);       // esse retorno os grupos divididos em 0x20 0x40 0x60, 0x80, 0xA0, 0xC0, 0xE0, 0x100
        }

        public uint setItemSubGroupIdentify21(uint group, uint nextId)
        {
            // Verifica se o valor do grupo está dentro do intervalo válido
            if (group > 0x3F) // 0x3F é 63 em decimal, o limite para 6 bits
            {
                throw new ArgumentOutOfRangeException(nameof(group), "O valor do grupo está fora do intervalo permitido.");
            }

            // Define a baseTypeId, mantendo o valor fixo dos bits mais significativos
            uint baseTypeId = 622829568 & 0xFC000000;

            // Incrementa o próximo identificador específico
            uint specificId = nextId++;

            // Cria o novo Index combinando o identificador específico e o grupo
            uint newTypeId = baseTypeId | ((specificId & 0xFFFFF) | ((group << 21) & 0x03FFFFE0));

            return newTypeId;
        }

        public uint getItemCharIdentify(uint _typeid)
        {
            return (uint)((_typeid & 0x03FF0000) >> 18);
        }

        public uint getItemCharPartNumber(uint _typeid)
        {
            return (uint)((_typeid & 0x0003FF00) >> 13);
        }

        public uint getItemCharTypeNumber(uint _typeid)
        {
            return (uint)((_typeid & 0x00001FFF) >> 8);
        }

        public uint getItemIdentify(uint _typeid)
        {
            return (uint)(_typeid & 0x000000FF);
        }

        public uint getItemTitleNum(uint _typeid)
        {
            var restul = (_typeid & 0x3FFFFF);
            return restul;
        }


        public uint getItemSkin(uint _typeid)
        {
            var restul = (_typeid & 0x3C00000u);
            if (restul == 0)
            {
                return 0;
            }

            if (restul == 4194304)
            {
                return 1;
            }

            if (restul == 8388608)
            {
                return 2;
            }

            if (restul == 12582912)
            {
                return 3;
            }

            if (restul == 20971520)
            {
                return 4;
            }

            if (restul == 25165824)
            {
                restul = 5;
            }
            return restul;
        }


        public uint getMatchTypeIdentity(uint _typeid)
        {
            return (uint)((_typeid & ~0xFC000000) >> 16);
        }

        public uint getCaddieItemType(uint _typeid)
        {
            return (uint)((_typeid & 0x0000FF00) >> 13);
        }

        public uint getCaddieIdentify(uint _typeid)
        {
            return (uint)(((_typeid & 0x0FFF0000) >> 21)/*Caddie Base*/ + ((_typeid & 0x000F0000) >> 16)/*Caddie Type, N, R, S e etc*/);
        }

        // Acho que eu fiz para usar no enchant de up stat de taqueira e character
        public uint getEnchantSlotStat(uint _typeid)
        {
            return (uint)((_typeid & 0x03FF0000) >> 20);
        }

        public uint setItemAuxPartNumber(byte type, uint group, uint nextId)
        {
            // Define uma baseTypeId para o grupo, mantendo bits mais significativos
            uint baseTypeId = 0;
            if (type == 0)
                baseTypeId = 1879113857 & 0xFFFF0000;
            else
                baseTypeId = 1881210884 & 0xFFFF0000;
            // Incrementa o próximo identificador específico
            uint specificId = nextId;

            // Cria o novo Index combinando o identificador específico e o grupo
            // Adiciona os bits de `group` e `specificId` ao baseTypeId
            uint newTypeId = baseTypeId | (specificId & 0x0000FFFF) | ((group << 16) & 0x00FF0000);

            return newTypeId;
        }

        public uint getItemAuxPartNumber(uint _typeid)
        {
            return (uint)((_typeid & 0x0003FF00) >> 16);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte GetAuxType(uint ID)
        {
            byte result;

            result = (byte)System.Math.Round((ID & 0x001F0000) / System.Math.Pow(2.0, 16.0));

            return result;
        }


        public CardTypeFlag GetCardType(uint TypeID)
        {
            // Aplicar a máscara e dividir pelo divisor
            double value = (TypeID & 0x00FF0000) / 65536.0;

            // Converter para o tipo CardTypeFlag
            return (CardTypeFlag)(int)(value + 0.5);
        }


        public bool CardCheckPosition(uint TypeID, uint Slot)
        {
            bool result = true;

            switch (Slot)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    {
                        if (!(GetCardType(TypeID) == CardTypeFlag.Normal))
                        {
                            result = false;
                        }

                    }
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                    {
                        if (!(GetCardType(TypeID) == CardTypeFlag.Caddie))
                        {
                            result = false;
                        }

                    }
                    break;
                case 9:
                case 10:
                    {
                        if (!(GetCardType(TypeID) == CardTypeFlag.NPC))
                        {
                            result = false;
                        }
                    }
                    break;
                default:
                    if (!(GetCardType(TypeID) == CardTypeFlag.Special))
                    {
                        result = false;
                    }
                    break;
            }

            return result;
        }

        public uint getGrandPrixAba(uint _typeid)
        {
            return (uint)((_typeid & 0x00FFFFFF) >> 19);
        }

        public GrandPrixData.GP_ABA getGrandPrixAbaType(uint _typeid)
        {
            return (GrandPrixData.GP_ABA)((_typeid & 0x00FFFFFF) >> 19);
        }

        public uint getGrandPrixType(uint _typeid)
        {
            return (uint)((_typeid & 0x0000FF00) >> 8);
        }

        public bool isGrandPrixEvent(uint _typeid)
        {
            return (uint)((_typeid & 0x3000000) >> 24) == 3u;
        }

        public bool isGrandPrixNormal(uint _typeid)
        {
            return (uint)((_typeid & 0x3000000) >> 24) == 0;
        }

        public bool IsExist(uint _typeid)
        {
            if (_typeid <= 0)
                return false;

            return findCommomItem(_typeid) != null;
        }
        public bool ExistIcon(uint _typeid)
        {
            var _base = findCommomItem(_typeid);
            if (_base == null)
            {
                return false;
            }
            return !string.IsNullOrEmpty(_base.ShopIcon);
        }



        public bool IsSelfDesign(uint TypeId)
        {
            switch (TypeId)
            {
                case 134258720:
                case 134242351:
                case 134258721:
                case 134242355:
                case 134496433:
                case 134496434:
                case 134512665:
                case 134496344:
                case 134512666:
                case 134496345:
                case 134783001:
                case 134758439:
                case 134783002:
                case 134758443:
                case 135020720:
                case 135020721:
                case 135045144:
                case 135020604:
                case 135045145:
                case 135020607:
                case 135299109:
                case 135282744:
                case 135299110:
                case 135282745:
                case 135545021:
                case 135545022:
                case 135569438:
                case 135544912:
                case 135569439:
                case 135544915:
                case 135807173:
                case 135807174:
                case 135823379:
                case 135807066:
                case 135823380:
                case 135807067:
                case 136093719:
                case 136069163:
                case 136093720:
                case 136069166:
                case 136331407:
                case 136331408:
                case 136355843:
                case 136331271:
                case 136355844:
                case 136331272:
                case 136593549:
                case 136593550:
                case 136617986:
                case 136593410:
                case 136617987:
                case 136593411:
                case 136880144:
                case 136855586:
                case 136880145:
                case 136855587:
                case 136855588:
                case 136855589:
                case 137379868:
                case 137379869:
                case 137404426:
                case 137379865:
                case 137404427:
                case 137379866:
                case 137904143:
                case 137904144:
                case 137928708:
                case 137904140:
                case 137928709:
                case 137904141:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsCanOverlapped(uint _typeid, bool is_memorial = false)
        {

            switch ((IFF_GROUP)getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CHARACTER:
                case IFF_GROUP.COURSE:
                case IFF_GROUP.MATCH:
                case IFF_GROUP.ENCHANT:
                case IFF_GROUP.HAIR_STYLE:
                case IFF_GROUP.ACHIEVEMENT:
                case IFF_GROUP.QUEST_STUFF:
                case IFF_GROUP.QUEST_ITEM:
                default:
                    return false;

                case IFF_GROUP.CLUBSET:
                    {
                        var cadItem = findClubSet(_typeid);

                        if (cadItem != null && cadItem.Shop.flag_shop.time_shop.active)
                            return true;    // Caddie item pode, se for de tempo para aumentar o tempo dele

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var cadItem = findFurniture(_typeid);

                        if (cadItem != null && cadItem.Shop.flag_shop.time_shop.active)
                            return true;    // Caddie item pode, se for de tempo para aumentar o tempo dele

                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        var cadItem = findSkin(_typeid);

                        if (cadItem != null && cadItem.Shop.flag_shop.time_shop.active)
                            return true;    // Caddie item pode, se for de tempo para aumentar o tempo dele

                        break;
                    }
                case IFF_GROUP.CAD_ITEM:
                    {
                        var cadItem = findCaddieItem(_typeid);

                        if (cadItem != null && cadItem.Shop.flag_shop.time_shop.active)
                            return true;    // Caddie item pode, se for de tempo para aumentar o tempo dele

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var mascot = findMascot(_typeid);

                        if (mascot != null && mascot.Shop.flag_shop.time_shop.active)
                            return true;

                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var part = findPart(_typeid);

                        // Libera os parts para Duplicatas se ele estiver liberado para vender no personal shop
                        if (part != null && (part.type_item == PART_TYPE.UCC_DRAW_ONLY || part.type_item == PART_TYPE.UCC_COPY_ONLY
                            || part.Shop.flag_shop.IsDuplication || part.Shop.flag_shop.can_send_mail_and_personal_shop))
                            return true;

                        break;
                    }
                case IFF_GROUP.ITEM:  // Libera todos item para dub se tiver abilitado no shop 
                case IFF_GROUP.BALL:
                case IFF_GROUP.CARD:
                    return true;
                case IFF_GROUP.CADDIE:
                    if (_typeid == 0x1C000001 || _typeid == 0x1C000002 || _typeid == 0x1C000003 || _typeid == 0x1C000007)
                        return true;
                    break;
                case IFF_GROUP.SET_ITEM:
                    {
                        var iff = findSetItem(_typeid);
                        if (iff == null) return false;

                        var tipo_set_item = (SET_ITEM_SUB_TYPE)getItemSubGroupIdentify21(_typeid);

                        // Cards e Comets (Balls) sempre podem duplicar no Memorial
                        if (tipo_set_item == SET_ITEM_SUB_TYPE.CHARACTER_SET_DUP_AND_ITEM_PASSIVE_AND_ACTIVE || tipo_set_item == SET_ITEM_SUB_TYPE.CARD || tipo_set_item == SET_ITEM_SUB_TYPE.BALL)
                            return true;

                        // Se for um Set de Personagem (Roupas/Acessórios) no Memorial
                        if (is_memorial && (tipo_set_item == SET_ITEM_SUB_TYPE.CHARACTER_SET || tipo_set_item == SET_ITEM_SUB_TYPE.CHARACTER_SET_NEW))
                        {
                            for (int i = 0; i < iff.packege.item_qntd.Length; i++)
                            {
                                uint innerId = iff.packege.item_typeid[i];
                                if (innerId == 0) continue;

                                // Se houver uma PART (Roupa) dentro do pacote que o player já possui
                                if (getItemGroupIdentify(innerId) == IFF_GROUP.PART)
                                {
                                    // Se o player já tem a peça e ela NÃO é duplicável (não é UCC/Tempo)
                                    if (!IsCanOverlapped(innerId, true))
                                    {
                                        // No Memorial, se uma peça do set for repetida, barramos o set todo
                                        // para evitar que o player ganhe 2 bonés iguais.
                                        return false;
                                    }
                                }
                            }
                        }
                        return true;
                    }
                case IFF_GROUP.AUX_PART:
                    {
                        var auxPart = findAuxPart(_typeid);

                        if (auxPart != null && auxPart.getQnt())//corrigido....
                            return Convert.ToBoolean(_typeid & ~0x1F0000);

                        break;
                    }   // Fim AuxPart
            }   // Fim Case

            return false;
        }
        public bool IsItemEquipable(uint _typeid)
        {
            var item = findItem(_typeid);

            if (item != null)
                return (getItemSubGroupIdentify24(_typeid) >> 1) == 0;  // Equiável, aqui depois tenho que mudar se mudar lá em cima, para (func() >> 1) == 0

            return false;
        }

        public bool IsTitle(uint _typeid)
        {

            if ((IFF_GROUP)getItemGroupIdentify(_typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.SKIN)
            {
                if ((_typeid & 0x3C00000u) != 0x1800000u)
                    return false;   // Não é um title

                return true;
            }

            return false;   // Não é uma skin(bg, frame, sticker, slot, cutin, title)
        }

        public IFFFile<Achievement> getAchievement()
        {
            return Achievement;
        }

        public IFFFile<QuestItem> getQuestItem()
        {
            return Quest_item;
        }
        public IFFFile<Item> getItem()
        {
            return Item;
        }

        public IFFFile<Card> getCard()
        {
            return Card;
        }

        public IFFFile<Skin> getSkin()
        {
            return m_skin;
        }

        public IFFFile<AuxPart> getAuxPart()
        {
            return Aux_part;
        }

        public IFFFile<Ball> getBall()
        {
            return Ball;
        }

        public IFFFile<Character> getCharacter()
        {
            return Character;
        }

        public IFFFile<Caddie> getCaddie()
        {
            return Caddie;
        }

        public IFFFile<CaddieItem> getCaddieItem()
        {
            return Caddie_item;
        }

        public IFFFile<CadieMagicBox> getCadieMagicBox()
        {
            return m_cadie_magic_box;
        }

        public IFFFile<ClubSet> getClubSet()
        {
            return m_club_set;
        }

        public IFFFile<HairStyle> getHairStyle()
        {
            return m_hair_style;
        }

        public IFFFile<Part> getPart()
        {
            return Part;
        }

        public IFFFile<Mascot> getMascot()
        {
            return Mascot;
        }

        public IFFFile<SetItem> getSetItem()
        {
            return Set_item;
        }

        public IFFFile<Desc> getDesc()
        {
            return m_desc;
        }

        public IFFFile<LevelUpPrizeItem> getLevelUpPrizeItem()
        {
            return m_level_up_prize_item;
        }

        public IFFFile<MemorialShopCoinItem> getMemorialShopCoinItem()
        {
            return m_memorial_shop_coin_item;
        }

        public IFFFile<MemorialShopRareItem> getMemorialShopRareItem()
        {
            return m_memorial_shop_rare_item;
        }

        public IFFFile<Course> getCourse()
        {
            return m_course;
        }

        public IFFFile<GrandPrixData> getGrandPrixData()
        {
            return m_grand_prix_data;
        }

        public IFFFile<Ability> getAbility()
        {
            return m_ability;
        }

        public IFFFile<SetEffectTable> getSetEffectTable()
        {
            return m_set_effect_table;
        }

        public IFFFile<QuestStuff> getQuestStuff()
        {
            return Quest_stuff;
        }

        public IFFFile<Club> getClub()
        {
            return m_club;
        }

        public IFFFile<Enchant> getEnchant()
        {
            return m_enchant;
        }

        public IFFFile<Furniture> getFurniture()
        {
            return m_furniture;
        }

        public IFFFile<Match> getMatch()
        {
            return m_match;
        }

        public IFFFile<TikiPointTable> getTikiPointTable()
        {
            return m_tiki_point_table;
        }

        public IFFFile<TikiRecipe> getTikiRecipe()
        {
            return m_tiki_recipe;
        }

        public IFFFile<TikiSpecialTable> getTikiSpecialTable()
        {
            return m_tiki_special_table;
        }

        public IFFFile<CadieMagicBoxRandom> getCadieMagicBoxRandom()
        {
            return m_cadie_magic_box_random;
        }

        public IFFFile<GrandPrixRankReward> getGrandPrixRankReward()
        {
            return m_grand_prix_rank_reward;
        }

        public IFFFile<GrandPrixSpecialHole> getGrandPrixSpecialHole()
        {
            return m_grand_prix_special_hole;
        }

        public IFFFile<FurnitureAbility> getFurnitureAbility()
        {
            return m_furniture_ability;
        }
        public IFFFile<CounterItem> getCounterItem()
        {
            return Counter_item;
        }

        public List<ClubSet> findClubSetOriginal(uint _typeid)
        {

            List<ClubSet> v_clubset = new List<ClubSet>();
            ClubSet clubset = null;

            // Invalid Typeid
            if (_typeid == 0)
                return v_clubset;

            if ((clubset = findClubSet(_typeid)) != null)
            {

                foreach (var el in m_club_set)
                {

                    // Text pangya é o logo da taqueira, como as especiais tem seu proprio logo
                    // então o número do logo vai ser a taqueira base das taqueira que transforma
                    if (el.text_pangya == clubset.text_pangya)
                        v_clubset.Add(el);
                }
            }

            return v_clubset;
        }

        public void Log(string v)
        {
            File.WriteAllText($"IF_{DateTime.Now.Millisecond}.Log", v);
        }

        public bool isLoad()
        {
            return m_loaded;
        }

        public bool EMPTY_ARRAY_PRICE<T>(T[] price) where T : struct, IComparable
        {
            return !price.Any(el => !el.Equals(default(T)));
        }


        public uint SUM_ARRAY_PRICE_ULONG<T>(T[] price) where T : struct, IComparable
        {
            uint sum = 0;

            foreach (var el in price)
            {
                sum += Convert.ToUInt32(el);
            }

            return sum;
        }

        public SortedDictionary<uint, TimeLimitItem> getTimeLimitItem()
        {
            var list = new SortedDictionary<uint, TimeLimitItem>();
            foreach (var item in m_time_limit_item)
            {
                list.Add(item._typeid, item);
            }
            return list;
        }

        public SetEffectTable findFirstItemInSetEffectTable(uint _typeid)
        {
            if (!isLoad())
            {
                _smp.message_pool.getInstance().push(new message("[iff::FindFirstItemInSetEffectTable][Error] IFF não carregado", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return null;
            }
			
            try
            { 
				return m_set_effect_table.FirstOrDefault(set => set.item.ID.Any(id => id == _typeid));
            }
           catch (Exception e)
    {
        _smp.message_pool.getInstance().push(new message("[iff::findFirstItemInSetEffectTable][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
        return null;
    }
        }

        public List<SetEffectTable> findAllItemInSetEffectTable(uint _typeid)
        {

            if (!isLoad())
            {
                _smp.message_pool.getInstance().push(new message("[iff::FindAllItemInSetEffectTable][Error] IFF não carregado", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return new List<SetEffectTable>();
            }

            List<SetEffectTable> ret = new List<SetEffectTable>();

            try
            {
                foreach (var _el in m_set_effect_table)
                {
                    for (int i = 0; i < _el.item.ID.Length; i++)
                    {
                        if (_el.item.ID[i] == _typeid)
                        { ret.Add(_el); }
                    }
                }
            }
            catch (Exception)
            {
                // Log opcional:
                // MessagePool.Instance.Push(new Message("[iff::FindAllItemInSetEffectTable][ErrorSystem] " + e.Message, CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public List<GrandPrixRankReward> findGrandPrixRankReward(uint ID) => m_grand_prix_rank_reward?.Where(c => c.ID == ID).ToList() ?? new List<GrandPrixRankReward>();

        public List<GrandPrixAIOptionalData> getGrandPrixAIOptionalData()
        {
            return m_grand_prix_ai_optinal_data;
        }

        public GrandPrixConditionEquip findGrandPrixConditionEquip(uint _typeid)
        {
            return m_grand_prix_condition_equip.FirstOrDefault(c => c._typeid == _typeid);

        }

        public Dictionary<string, List<IFFCommon>> GetAllGalleryItems()
        {
            var allItems = new Dictionary<string, List<IFFCommon>>();

            // Função auxiliar para mapear, definir o tipo e adicionar à lista principal
            // Use 'IFF_GROUP' ou a string de filtro que você quer.
            void AddItems<T>(IEnumerable<T> items, string itemType) where T : IFFCommon
            {
                if (!allItems.ContainsKey(itemType))
                {
                    allItems.Add(itemType, new List<IFFCommon>());
                }

                // 2. Adiciona todos os itens desse tipo à lista existente
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.ShopIcon))//icones vazios nao entram na galeria
                    {
                        allItems[itemType].Add(item);
                    }
                }
            }
            //somente esses é necessario
            AddItems(Character.AsEnumerable(), "character");
            AddItems(Part.AsEnumerable(), "part");
            AddItems(m_club_set.AsEnumerable(), "clubset");
            AddItems(Ball.AsEnumerable(), "ball");
            AddItems(Item.AsEnumerable(), "item");
            AddItems(Caddie.AsEnumerable(), "caddie");
            AddItems(Caddie_item.AsEnumerable(), "caditem");
            AddItems(Set_item.AsEnumerable(), "setitem");
            AddItems(m_skin.AsEnumerable(), "skin");
            AddItems(m_hair_style.AsEnumerable(), "hair");
            AddItems(Mascot.AsEnumerable(), "mascot");
            AddItems(Card.AsEnumerable(), "card");
            return allItems;
        }
    }
    public class sIff : Singleton<IFFHandle>
    {
    }
}
