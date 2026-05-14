namespace PangyaAPI.IFF.JP.Models.Flags
{
    public enum ShopFlagDay : byte
    {
        None = 0,
        Day = 1,
        Week = 7,
        TwoWeeks = 15,
        Month = 30,
        Year = 109
    }

    public enum CARD_GROUP : uint
    {
        CARD_N_NURI = 0x0,
        CARD_R_NURI = 0x1,
        CARD_SR_NURI = 0x2,
        CARD_SC_NURI = 0x3,
        CARD_N_HANA = 0x4,
        CARD_R_HANA = 0x5,
        CARD_SR_HANA = 0x6,
        CARD_SC_HANA = 0x7,
        CARD_N_AZER = 0x8,
        CARD_R_AZER = 0x9,
        CARD_SR_AZER = 0xA,
        CARD_SC_AZER = 0xB,
        CARD_N_KOOH = 0xC,
        CARD_R_KOOH = 0xD,
        CARD_SR_KOOH = 0xE,
        CARD_SC_KOOH = 0xF,
        CARD_N_CESILLIA = 0x10,
        CARD_R_CESILLIA = 0x11,
        CARD_SR_CESILLIA = 0x12,
        CARD_SC_CESILLIA = 0x13,
        CARD_N_MAX = 0x14,
        CARD_R_MAX = 0x15,
        CARD_SR_MAX = 0x16,
        CARD_SC_MAX = 0x17,
        CARD_N_ARIN = 0x18,
        CARD_R_ARIN = 0x19,
        CARD_SR_ARIN = 0x1A,
        CARD_SC_ARIN = 0x1B,
        CARD_N_KAZ = 0x1C,
        CARD_R_KAZ = 0x1D,
        CARD_SR_KAZ = 0x1E,
        CARD_SC_KAZ = 0x1F,
        CARD_N_BONG = 0x0,
        CARD_R_BONG = 0x1,
        CARD_SR_BONG = 0x2,
        CARD_SC_BONG = 0x3,
        CARD_N_PIPIN = 0x4,
        CARD_R_PIPIN = 0x5,
        CARD_SR_PIPIN = 0x6,
        CARD_SC_PIPIN = 0x7,
        CARD_R_BBOO = 0x8,
        CARD_SR_BBOO = 0x9,
        CARD_SC_BBOO = 0xA,
        CARD_N_BBOO = 0xB,
        CARD_N_DOL = 0xC,
        CARD_R_DOL = 0xD,
        CARD_SR_DOL = 0xE,
        CARD_SC_DOL = 0xF,
        CARD_N_LOLO = 0x10,
        CARD_R_LOLO = 0x11,
        CARD_SR_LOLO = 0x12,
        CARD_SC_LOLO = 0x13,
        CARD_N_QUMA = 0x14,
        CARD_R_QUMA = 0x15,
        CARD_SR_QUMA = 0x16,
        CARD_SC_QUMA = 0x17,
        CARD_SR_CADI = 0x18,
        CARD_SC_CADI = 0x19,
        CARD_BASIC_TRAN = 0x0,
        CARD_SWING_TRAN = 0x1,
        CARD_CAMPING_TRAN = 0x2,
        CARD_HEART_BLACKMOZZY = 0x3,
        CARD_TRUE_BLACKMOZZY = 0x4,
        CARD_SOUL_BLACKMOZZY = 0x5,
        CARD_HEART_WHITEMOZZY = 0x6,
        CARD_TRUE_WHITEMOZZY = 0x7,
        CARD_SOUL_WHITEMOZZY = 0x8,
        CARD_SMALL_PANG = 0x9,
        CARD_MIDDLE_PANG = 0xA,
        CARD_BIG_PANG = 0xB,
        CARD_CHEER_PIPIN = 0xC,
        CARD_CHEER_PIPIN2 = 0xD,
        CARD_MAGIC_CADIE = 0xE,
        CARD_MAGIC_CADIE2 = 0xF,
        CARD_BLESS_TIKI = 0x10,
        CARD_BLESS_TIKI2 = 0x11,
        CARD_SKILL_DOL = 0x12,
        CARD_SKILL_DOL2 = 0x13,
        CARD_CHEER_QUMA = 0x14,
        CARD_CHEER_QUMA2 = 0x15,
        CARD_HELP_CADDI = 0x16,
        CARD_POWER = 0x17,
        CARD_BAG_BILL = 0x18,
        CARD_BAG_BILL2 = 0x19,
        CARD_CARDPACK_1TH = 0x0,
        CARD_TICKET_GOLD = 0x1,
        CARD_TICKET_SILVER = 0x2,
        CARD_TICKET_BRONZE = 0x3,
        CARD_CARDBOX_1TH = 0x0
    }

    public enum PART_TYPE : int
    {
        TOP,
        BOTTOM,
        HEAD,
        GLOVES,
        SHOES,
        ACCESSORY_OR_BASE,
        SUB_LEG,
        UCC_CREATE,
        UCC_DRAW_ONLY = 8, //UCC_BLANK
        UCC_COPY_ONLY,//UCC_COPY
    }

    public enum CARD_SUB_TYPE : byte
    {
        T_CHARACTER,
        T_CADDIE,
        T_SPECIAL,
        T_PACK,
        T_BOX_PACK,
        T_NPC,
    }

    public enum SET_ITEM_SUB_TYPE : byte
    {
        COMMOON,
        CHARACTER_SET,//CHARACTER
        CHARACTER_SET_NEW,//PARTS
        MASCOT,
        CLUB_SET,//CLUBSET
        BALL,//BALL
        CHARACTER_SET_DUP_AND_ITEM_PASSIVE_AND_ACTIVE,//ITEM
        CADDIE,//CADDIE
        CARD,
        AUXPART,    // Anel
    }

    public enum IFF_GROUP : uint
    {
        CHARACTER = 1,      // 4
        PART,               // 8
        CLUB,               // 12
        CLUBSET,            // 16
        BALL,               // 20
        ITEM,               // 24
        CADDIE,             // 28
        CAD_ITEM,           // 32
        SET_ITEM,           // 36
        COURSE,             // 40
        MATCH,              // 44 Trofel
        TITLE = 12,
        ENCHANT = 13,       // 52
        SKIN,               // 56
        HAIR_STYLE,         // 60
        MASCOT,             // 64
        CHILDITEM = 17,
        FURNITURE = 18,     // 72
        OFFLINESHOP = 19,
        ACHIEVEMENT,        // 76
        COUNTER_ITEM = 27,  // 108
        AUX_PART,           // 112
        QUEST_STUFF,        // 116
        QUEST_ITEM,         // 120
        CARD,               // 124
    }

    public enum TypeSetFlag : ushort
    {
        UNKNOWN_0,
        CHARACTER_SET,
        CHARACTER_SET_NEW,
        UNKNOWN_3,
        CLUB_SET,
        BALL,
        CHARACTER_SET_DUP_AND_ITEM_PASSIVE_AND_ACTIVE,
        UNKNOWN_7,
        CARD,
        AUXPART,	// Anel
    }
    public enum eEFFECT : uint
    {
        ANIMATION = 1,
        UNKNOWN_V2,
        CUTIN,
        PIXEL,
        BASE,
        ONE_ALL_STATS,
        WIND_DECREASE,
        PATINHA
    }

    public enum eEFFECT_TYPE : uint
    {
        UNKNOWN_V1 = 1,
        GAME = 2,
        ROOM = 4,
        LOUNGE = 8
    }
    public enum TypeCard
    {
        Pack1,
        Pack2,
        Pack3,
        Pack4,
        Rare,
        All
    }

    public enum AbilityEffect : uint
    {
        NONE,
        PIXEL,                          // Pixel o valor em rate
        PIXEL_BY_WIND_NO_ITEM,          // Pixel dependendo do vento o valor em rate, se usar item ou ps cancela o efeito
        PIXEL_OVER_WIND_NO_ITEM,        // Pixel acima de um vento o valor em rate, se usar item ou ps cancela o efeito
        PIXEL_BY_WIND,                  // Pixel dependendo do vento o valor em rate
        PIXEL_2,                        // Pixel o valor em rate
        PIXEL_WITH_WEAK_WIND,           // Pixel quando o vento é fraco o valor em rate
        POWER_GAUGE_TO_START_HOLE,      // Power Gauge no começo do hole para cada hole o valor em rate
        POWER_GAUGE_MORE_ONE,           // Power Gauge da uma barra a+ 33 Units, o valor em rate
        POWER_GUAGE_TO_START_GAME,      // Power Gauge no começo do jogo o valor em rate
        PAWS_NOT_ACCUMULATE,            // Patinha não acumula com outro efeito de patinha, probabilidade está em rate
        SWITCH_TWO_EFFECT,              // Item com 2 efeitos não simutâneos, qual efeito está em rate, 0 Yards, 1 Power Gauge
        EARCUFF_DIRECTION_WIND,         // Muda a direção do vento, a probabilidade quem escolhe é o pangya
        COMBINE_ITEM_EFFECT,            // Combinação de itens, em rate tem o Index da combinação em (IFF)SetEffectTable
        SAFETY_CLIENT_RANDOM,           // Safety a probabilidade o cliente que decide
        PIXEL_RANDOM,                   // Pixel aleatório o valor está em rate, a probabilidade o cliente que decide
        WIND_1M_RANDOM,                 // Wind 1m aleatório a probabilidade está em rate
        PIXEL_BY_WIND_MIDDLE_DOUBLE,    // Pixel dependendo do vento, vento médio dá o dobro, o valor em rate
        GROUND_100_PERCENT_RONDOM,      // Terreno 100% aleatório, a probabilidade está em rate
        ASSIST_MIRACLE_SIGN,            // Assist Olho Mágico
        VECTOR_SIGN,                    // Mostra uma seta na bola, dependendo do vento, tipo trajetória do assist
        ASSIST_TRAJECTORY_SHOT,         // Assist Trajectory Shot
        PAWS_ACCUMULATE,                // Patinha acumula com outro efeito de patinha, a probabilidade está em rate
        POWER_GAUGE_FREE,               // Power Gauge, ganha 1 Power Gauge de graça para usar na tacada
        SAFETY_RANDOM,                  // Safety aleatório a probabilidade está em rate
        ONE_IN_ALL_STATS,               // [UNKNOWN] mas vou deixar o (Combine Itens) ONE IN ALL STATS, dá 1 para todos os stats, power, cltr, accuracy, spin e curve
        POWER_GAUGE_BY_MISS_SHOT,       // Power Gauge mesmo que erre pangya ou use item de Power Gauge ele ainda dá Power Gauge
        PIXEL_BY_WIND_2,                // Pixel dependendo do vento o valor está em rate
        PIXEL_WITH_RAIN,                // Pixel quando estiver chovendo(recovery) o valor está em rate
        NO_RAIN_EFFECT,                 // Sem efeito dá chuva no terreno
        PUTT_MORE_10Y_RANDOM,           // +10y no Putt aleatório a probabilidade está em rate
        UNKNOWN_31,
        MIRACLE_SIGN_RANDOM,            // Olho Mágico aleatório a probabilidade está em rate
        UNKNOWN_33,
        DECREASE_1M_OF_WIND,			// Diminui 1m do vento
    }
    public enum CardTypeFlag
    {
        Normal = 0x0,
        Caddie = 0x40,
        NPC = 0x41,
        Special = 0x80
    }

    public enum CharacterType : byte
    {
        NURI = 0,
        HANA = 1,
        AZER = 2,
        CECILIA = 3,
        MAX = 4,
        KOOH = 5,
        ARIN = 6,
        KAZ = 7,
        LUCIA = 8,
        NELL = 9,
        SPIKA = 10,
        NURI_R = 11,
        HANA_R = 12,
        AZER_R = 13,
        CECILIA_R = 14
    }

    public enum SubType
    {
        UNKNOWN_0,
        CHARACTER_SET,
        CHARACTER_SET_NEW,
        UNKNOWN_3,
        CLUB_SET,
        BALL,
        CHARACTER_SET_DUP_AND_ITEM_PASSIVE_AND_ACTIVE,
        UNKNOWN_7,
        CARD_PACK = 71,
        AUXPART
    }
    public enum _ItemType : uint
    {
        Normal = 0,
        Box = 1,
        Unknown = 2,
        Artifact = 4,
        Artifact_Mana = 10,
        MemorialCoin = 16,
        GM_Item = 128,
        Un252 = 252
    }

    public enum CadieBoxSetor : uint
    {
        Unknown = uint.MaxValue,
        Beginner = 0,
        Intermediary = 1,
        Advance = 2,
        Special = 3,
        Event = 4
    }
    public enum CadieBoxEnum : uint
    {
        MASCOT = 4294967295,
        PART = 4294967294,
        NURI = 0,
        HANA = 1,
        AZER = 2,
        CESILLIA = 3,
        MAX = 4,
        KOOH = 5,
        ARIN = 6,
        KAZ = 7,
        LUCIA = 8,
        NELL = 9,
        SPIKA = 10,
        NURI_R = 11,
        HANA_R = 12,
        AZER_R = 13,
        CESILLIA_R = 14
    }
    public enum ItemTypeEnum : int
    {
        Active = 0,
        consumable = 1,
        All = -1,
        Passive = 128,
        GM = 252,
    }


    public enum ItemLevelEnum : byte
    {
        ROOKIE_F = 0x00,
        ROOKIE_E = 0x01,
        ROOKIE_D = 0x02,
        ROOKIE_C = 0x03,
        ROOKIE_B = 0x04,
        ROOKIE_A = 0x05,
        BEGINNER_E = 0x06,
        BEGINNER_D = 0x07,
        BEGINNER_C = 0x08,
        BEGINNER_B = 0x09,
        BEGINNER_A = 0x0A,
        JUNIOR_E = 0x0B,
        JUNIOR_D = 0x0C,
        JUNIOR_C = 0x0D,
        JUNIOR_B = 0x0E,
        JUNIOR_A = 0x0F,
        SENIOR_E = 0x10,
        SENIOR_D = 0x11,
        SENIOR_C = 0x12,
        SENIOR_B = 0x13,
        SENIOR_A = 0x14,
        AMATEUR_E = 0x15,
        AMATEUR_D = 0x16,
        AMATEUR_C = 0x17,
        AMATEUR_B = 0x18,
        AMATEUR_A = 0x19,
        SEMI_PRO_E = 0x1A,
        SEMI_PRO_D = 0x1B,
        SEMI_PRO_C = 0x1C,
        SEMI_PRO_B = 0x1D,
        SEMI_PRO_A = 0x1E,
        PRO_E = 0x1F,
        PRO_D = 0x20,
        PRO_C = 0x21,
        PRO_B = 0x22,
        PRO_A = 0x23,
        NATIONAL_PRO_E = 0x24,
        NATIONAL_PRO_D = 0x25,
        NATIONAL_PRO_C = 0x26,
        NATIONAL_PRO_B = 0x27,
        NATIONAL_PRO_A = 0x28,
        WORLD_PRO_E = 0x29,
        WORLD_PRO_D = 0x2A,
        WORLD_PRO_C = 0x2B,
        WORLD_PRO_B = 0x2C,
        WORLD_PRO_A = 0x2D,
        MASTER_E = 0x2E,
        MASTER_D = 0x2F,
        MASTER_C = 0x30,
        MASTER_B = 0x31,
        MASTER_A = 0x32,
        TOP_MASTER_E = 0x33,
        TOP_MASTER_D = 0x34,
        TOP_MASTER_C = 0x35,
        TOP_MASTER_B = 0x36,
        TOP_MASTER_A = 0x37,
        WORLD_MASTER_E = 0x38,
        WORLD_MASTER_D = 0x39,
        WORLD_MASTER_C = 0x3A,
        WORLD_MASTER_B = 0x3B,
        WORLD_MASTER_A = 0x3C,
        LEGEND_E = 0x3D,
        LEGEND_D = 0x3E,
        LEGEND_C = 0x3F,
        LEGEND_B = 0x40,
        LEGEND_A = 0x41,
        INFINITY_LEGEND_E = 0x42,
        INFINITY_LEGEND_D = 0x43,
        INFINITY_LEGEND_C = 0x44,
        INFINITY_LEGEND_B = 0x45,
        INFINITY_LEGEND_A = 0x46,
        INFINITY_LEGEND_F = 0x8F
    }

    /// <summary>
    /// This flag is handling buying conditions
    /// </summary>
    public enum ShopFlagEnum : byte
    {
        Display = 85,
        /// <summary>
        /// Unknown value
        /// </summary>
        Only_Display = 128,

        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown03 = 3,

        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown64 = 64,

        /// <summary>
        /// CP
        /// </summary>
        Cookies_0 = 33,

        /// <summary>
        /// Unknown value
        /// </summary>
        Pang = 32,

        Active = 37,
        PersonalShop_Active = 18, //oou 19?
        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown16 = 16,

        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown8 = 8,

        Tradeable = 7,

        Unknown5 = 5,
        /// <summary>
        /// This shop item is a coupon
        /// </summary>
        Coupon = 4,

        /// <summary>
        /// This shop item is not giftable
        /// </summary>
        NonGiftable1 = 69,
         
        /// <summary>
        /// This shop item is giftable
        /// </summary>
        Giftable = 0x01,

        /// <summary>
        /// No special buying conditions
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Displays a "Special" banner on a shop item
        /// </summary>
        BannerSpecial = 3,

        /// <summary>
        /// Displays a "Hot" banner on a shop item
        /// </summary>
        BannerHot = 64,
        Combine97 = 97,
        Combine96 = 96,
        Combine98 = 99,
        Combine99 = 99,

        /// <summary>
        /// Displays a "New" banner on a shop item
        /// </summary>
        NonGiftable = 2
    }

    public enum MoneyFlagEnum : byte
    {
        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown1 = 128,

        /// <summary>
        /// Displays a "Special" banner on a shop item
        /// </summary>
        BannerSpecial = 64,

        /// <summary>
        /// Displays a "Hot" banner on a shop item
        /// </summary>
        BannerHot = 32,

        /// <summary>
        /// Displays a "New" banner on a shop item
        /// </summary>
        BannerNew = 0x02,

        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown2 = 0x08,

        /// <summary>
        /// Unknown value
        /// </summary>
        Unknown3 = 0x03,

        /// <summary>
        /// Item is for display only
        /// </summary>
        DisplayOnly = 0x04,

        /// <summary>
        /// This shop item is active
        /// </summary>
        Active = 0x01,
        /// <summary>
        /// No special shop display condition
        /// </summary>
        None = 0x00
    }

    /// <summary>
    /// use HairColor.iff(Type for character)
    /// </summary>
    public enum HairColorFlag : byte
    {
        Nuri = 0,
        Hana = 1,
        Azer = 2,
        Cecilia = 3,
        Max = 4,
        Kooh = 5,
        Arin = 6,
        Kaz = 7,
        Lucia = 8,
        Nell = 9,
        Spika = 10,
        NR = 11,
        HR = 12,
        CR = 14
    }

    /// <summary>
    /// This flag is handling different card effects
    /// </summary>
    public enum CardEffectFlag : ushort
    {
        /// <summary>
        /// No card effect
        /// </summary>
        None = 0,

        /// <summary>
        /// This card grants an experience bonus
        /// </summary>
        Experience = 1,

        /// <summary>
        /// This card grants a percentual pang increase
        /// </summary>
        PercentPang = 2,

        /// <summary>
        /// This card grants a percentual experience increase
        /// </summary>
        PercentExperience = 3,

        /// <summary>
        /// This card adds a fixed pang bonus
        /// </summary>
        Pang = 4,

        /// <summary>
        /// This card increases the Power statistic
        /// </summary>
        Power = 5,

        /// <summary>
        /// This card increases the Control statistic
        /// </summary>
        Control = 6,

        /// <summary>
        /// This card increases the Accuracy statistic
        /// </summary>
        Accuracy = 7,

        /// <summary>
        /// This card increases the Spin statistic
        /// </summary>
        Spin = 8,

        /// <summary>
        /// This card increases the Curve statistic
        /// </summary>
        Curve = 9,

        /// <summary>
        /// This card increases the Power shot gauge at the beginning of a match
        /// </summary>
        StartingGauge = 10,

        /// <summary>
        /// TODO: Figure out what this effect does again
        /// </summary>
        Inventory = 11
    }

    // 0 = ITEM COMUM
    // 1 = ITEM NORMAL ASA FECHADA
    // 2 = ITEM NORMAL ASA ABERTA
    // 3 = ITEM RARO ASA FECHADA
    // 4 = ITEM RARO ASA ABERTA
    public enum MemorialRareType : uint
    {
        Default = uint.MaxValue,
        Normal = 0,
        Comum = 1,
        Rare = 2,
        Super_Rare = 3,
        Super_Rare2 = 4
    }
    public enum FilterCoinType : uint
    {
        NORMAL,
        PREMIUM,
        SPECIAL,
        CHARACTER
    }
    public enum FilterType : uint
    {
        NORMAL = 0,
        SPRING = 1,
        SUMMER,
        FALL,
        WINTER,
        CLUBSET = 5,
        SETITEM,
        EAR,
        WING,
        LUVA,
        RING_R,
        RING_L,
        CADDIE,
        MASCOT,
        SUMMER_HOLYDAY,
        XMAS,
        HALLOWEEN,
        MAN = 17,
        WOMAN = 18,
        NURI,
        HANA,
        AZER,
        CECI,
        MAX,
        KOOH,
        ARIN,
        KAZ,
        LUCIA,
        NELL,
        SPIKA,
        NURI_R,
        HANA_R,
        AZER_R,
        CECI_R,
    }

    public enum Effect_Type
    {
        NONE,
        PIXEL,                          // Pixel o valor em rate
        PIXEL_BY_WIND_NO_ITEM,          // Pixel dependendo do vento o valor em rate, se usar item ou ps cancela o efeito
        PIXEL_OVER_WIND_NO_ITEM,        // Pixel acima de um vento o valor em rate, se usar item ou ps cancela o efeito
        PIXEL_BY_WIND,                  // Pixel dependendo do vento o valor em rate
        PIXEL_2,                        // Pixel o valor em rate
        PIXEL_WITH_WEAK_WIND,           // Pixel quando o vento é fraco o valor em rate
        POWER_GAUGE_TO_START_HOLE,      // Power Gauge no começo do hole para cada hole o valor em rate
        POWER_GAUGE_MORE_ONE,           // Power Gauge da uma barra a+ 33 Units, o valor em rate
        POWER_GUAGE_TO_START_GAME,      // Power Gauge no começo do jogo o valor em rate
        PAWS_NOT_ACCUMULATE,            // Patinha não acumula com outro efeito de patinha, probabilidade está em rate
        SWITCH_TWO_EFFECT,              // Item com 2 efeitos não simutâneos, qual efeito está em rate, 0 Yards, 1 Power Gauge
        EARCUFF_DIRECTION_WIND,         // Muda a direção do vento, a probabilidade quem escolhe é o pangya
        COMBINE_ITEM_EFFECT,            // Combinação de itens, em rate tem o Index da combinação em (IFF)SetEffectTable
        SAFETY_CLIENT_RANDOM,           // Safety a probabilidade o cliente que decide
        PIXEL_RANDOM,                   // Pixel aleatório o valor está em rate, a probabilidade o cliente que decide
        WIND_1M_RANDOM,                 // Wind 1m aleatório a probabilidade está em rate
        PIXEL_BY_WIND_MIDDLE_DOUBLE,    // Pixel dependendo do vento, vento médio dá o dobro, o valor em rate
        GROUND_100_PERCENT_RONDOM,      // Terreno 100% aleatório, a probabilidade está em rate
        ASSIST_MIRACLE_SIGN,            // Assist Olho Mágico
        VECTOR_SIGN,                    // Mostra uma seta na bola, dependendo do vento, tipo trajetória do assist
        ASSIST_TRAJECTORY_SHOT,         // Assist Trajectory Shot
        PAWS_ACCUMULATE,                // Patinha acumula com outro efeito de patinha, a probabilidade está em rate
        POWER_GAUGE_FREE,               // Power Gauge, ganha 1 Power Gauge de graça para usar na tacada
        SAFETY_RANDOM,                  // Safety aleatório a probabilidade está em rate
        ONE_IN_ALL_STATS,               // [UNKNOWN] mas vou deixar o (Combine Itens) ONE IN ALL STATS, dá 1 para todos os stats, power, cltr, accuracy, spin e curve
        POWER_GAUGE_BY_MISS_SHOT,       // Power Gauge mesmo que erre pangya ou use item de Power Gauge ele ainda dá Power Gauge
        PIXEL_BY_WIND_2,                // Pixel dependendo do vento o valor está em rate
        PIXEL_WITH_RAIN,                // Pixel quando estiver chovendo(recovery) o valor está em rate
        NO_RAIN_EFFECT,                 // Sem efeito dá chuva no terreno
        PUTT_MORE_10Y_RANDOM,           // +10y no Putt aleatório a probabilidade está em rate
        UNKNOWN_31,
        MIRACLE_SIGN_RANDOM,            // Olho Mágico aleatório a probabilidade está em rate
        UNKNOWN_33,
        DECREASE_1M_OF_WIND,            // Diminui 1m do vento
    }
    public enum IFF_REGION
    {
        Default = -1,
        Usa = 0,
        Japan = 1,
        Korea = 2,
        Thaiwan = 3
    }
}
