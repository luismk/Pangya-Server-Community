/// create and converted by LUIS MK

using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.PangyaEnums;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
namespace Pangya_GameServer.Models
{
    /// <summary>
    /// define 
    /// </summary>
    public static class DefineConstants
    {
        // A cada 100 players adiciona 1 ganhador para ser sorteado
        public const int NUMBER_OF_PLAYER_TO_WINNER = 100;
        public const int OPENNED_SPINNING_CUBE_TYPEID = 0x1A000161;
        public const uint CARD_ABBOT_ELEMENTAL_SHARD = 0x7C800026U;
        public const int KEY_OF_SPINNING_CUBE_TYPEID = 0x1A00015C;
        public const int PAPEL_BOX_TYPEID = 0x1A000208;
        public const double PI = 3.1415926535897931;
        public const int MAX_REWARD_PER_ROUND = 3;
        public const float SCALE_PANGYA = 3.2f;
        public const float DIVIDE_SCALE_PANGYA = 1.0f / 3.2f;

        public const float DIVIDE_SLOPE_CURVE_SCALE = 0.00875f;
        public const float SCALE_SLOPE_CURVE = 1.0f / 0.00875f;

        public const double GRAVITY_SCALE_PANGYA = 34.295295715332;

        public const float DESVIO_SCALE_PANGYA_TO_YARD = DIVIDE_SCALE_PANGYA / 1.5f;

        public const float STEP_TIME = 0.02f;

        public const float EFECT_MAGNUS = 0.00008f;

        public const float ROTATION_SPIN_FACTOR = 3.0f;
        public const float ROTATION_CURVE_FACTOR = 0.75f;

        public const float SPIN_DECAI_FACTOR = 0.1f;

        public const float WIND_SPIKE_FACTOR = 0.01f;

        public const uint BASE_POWER_CLUB = 15; // 15 * 2 = 30, 200 + 230, power base

        public const double POWER_SPIN_PW_FACTORY = 0.0698131695389748;
        public const double POWER_CURVE_PW_FACRORY = 0.349065847694874;

        public const float ACUMULATION_SPIN_FACTOR = 25.132742f;
        public const float ACUMULATION_CURVE_FACTOR = 12.566371f;

        public const float BALL_ROTATION_SPIN_COBRA = 2.5f;
        public const float BALL_ROTATION_SPIN_SPIKE = 3.1f;

        public const double ROUND_ZERO = 0.00001;
        public const int LIMIT_LEVEL_CADDIE = 3;
        public const int LIMIT_LEVEL_MASCOT = 9;

        public const byte LIMIT_DEGREE = 255;
        public const ulong EXPIRES_CACHE_TIME = 3 * 1000Ul; // 3 Segundos
        public const uint NUM_OF_EMAIL_PER_PAGE = 20u; // 20 Emails por p�gina
        public const uint LIMIT_OF_UNREAD_EMAIL = 300u; // 300 Emails n�o lidos que pode enviar para o player         
        public const uint UPDATE_TIME_INTERVALE_HOUR = 24u;
        public const long STDA_10_MICRO_PER_MICRO = 10;
        public const long STDA_10_MICRO_PER_MILLI = STDA_10_MICRO_PER_MICRO * 1000;
        public const long STDA_10_MICRO_PER_SEC = STDA_10_MICRO_PER_MILLI * 1000;
        public const long STDA_10_MICRO_PER_MIN = STDA_10_MICRO_PER_SEC * 60;
        public const long STDA_10_MICRO_PER_HOUR = STDA_10_MICRO_PER_MIN * 60;
        public const long STDA_10_MICRO_PER_DAY = STDA_10_MICRO_PER_HOUR * 24;

        public const byte DEFAULT_CHANNEL = byte.MaxValue; // channel invalid
        public const ushort DEFAULT_ROOM_ID = ushort.MaxValue; // room invalid

        public const uint CLEAR_10_DAILY_QUEST_TYPEID = 0x78800001; // Quest 10 clear daily quest
        public const uint ASSIST_ITEM_TYPEID = 0x1BE00016;
        public const uint GRAND_PRIX_TICKET = 0x1A000264;
        public const uint LIMIT_GRAND_PRIX_TICKET = 50; // Limit de Grand Prix Ticket que o player pode ter, chegou nesse limit não drop mais ele do hole
        public const uint MULLIGAN_ROSE_TYPEID = 0x1800000E;
        public const uint DEFAULT_COMET_TYPEID = 0x14000000;
        public const uint AIR_KNIGHT_SET = 0x10000000;
        public const uint CLUB_PATCHER_TYPEID = 0x1A00018F;
        public const uint MILAGE_POINT_TYPEID = 436208295;
        public const uint TIKI_POINT_TYPEID = 436208294;
        public const uint SPECIAL_SHUFFLE_COURSE_TICKET_TYPEID = 0x1A0000F7;
        public const uint PANG_POUCH_TYPEID = 0x1A000010;
        public const uint EXP_POUCH_TYPEID = 0x1A00015D;
        public const uint CP_POUCH_TYPEID = 0x1A000160;
        public const uint DECREASE_COMBO_VALUE = 3; // No JP é 10, no USA era 3
        public const float MEDIDA_PARA_YARDS = 0.3125f; // Usava 0.31251 Medida uinterna do pangya que no visual é o Yards
        // Icon Player Good(angel), Quiter 1 e 2 
        public const float GOOD_PLAYER_ICON = 3.0f;
        public const float QUITER_ICON_1 = 20.0f;
        public const float QUITER_ICON_2 = 30.0f;


        // Corta com toma, e corta com safety
        public static readonly uint[] active_item_cant_have_2_inventory = { 402653229u, 402653231u };

        public static readonly uint[] cadie_cauldron_Hermes_item_typeid = { 0x08010032u, 0x0804e058u, 0x0808e025u, 0x080ce041u, 0x0810a030u, 0x0814e05eu, 0x0818a060u, 0x081ce02fu, 0x0820e02fu };
        public static readonly uint[] cadie_cauldron_Jester_item_typeid = { 0x08000848u, 0x08040863u, 0x0808082bu, 0x080c002cu, 0x08100033u, 0x0814003eu, 0x0818005eu, 0x081c002bu, 0x08200018u, 0x0824000eu, 0x0828001du, 0x08380004u, 0x0830000cu, 0x082c0004u };
        public static readonly uint[] cadie_cauldron_Twilight_item_typeid = { 0x0801a812u, 0x08050811u, 0x0809081du, 0x080d481cu, 0x0811201bu, 0x08162810u, 0x08196013u, 0x081da817u, 0x0821a80cu };
        public const uint TICKET_BOT_TYPEID = 0x1A000401u;
        public const uint TICKET_BOT_TYPEID2 = 436207927;

        public const uint TROFEL_GM_EVENT_TYPEID = 0x2D0A3B00;

        public const byte cadie_cauldron_Hermes_random_id = 2;
        public const byte cadie_cauldron_Jester_random_id = 3;
        public const byte cadie_cauldron_Twilight_random_id = 4;
        public const int MS_NUM_MAPS = 22; // TH, US, KR is 20
        public const int STDA_INVITE_TIME_MILLISECONDS = 5000;	// 5 segundos em millisegundos

        public const int TREASURE_HUNTER_TIME_UPDATE = 30 * 60;       // 30 minutos
        public const int TREASURE_HUNTER_LIMIT_POINT_COURSE = 1000;     // 1000 limite de pontos do course
        public const int TREASURE_HUNTER_INCREASE_POINT = 50;           // 50 pontos que soma a cada 10 minutos para todos course pontos
        public const int TREASURE_HUNTER_BOX_PER_POINT = 100;           // 100 pontos por uma box
                                                                        // !@ tempor�rio
        public const uint PREMIUM_TICKET_TYPEID = 437256194u;
        // !@ tempor�rio
        public const uint PREMIUM_2_TICKET_TYPEID = 437256195u;
        // !@ tempor�rio
        public const uint PREMIUM_BOX_TYPEID = 436208314u;  //Box Gift (Premium)								 		  
                                                            // !@ tempor�rio
        public const uint PREMIUM_CLUBSET_TYPEID = 268439553u; // ClubSet VIP (Premium)-(original não tem)
                                                               // !@ tempor�rio
        public const uint PREMIUM_BALL_TYPEID = 335544536u; //Ball Premium temporario 
                                                            // !@ tempor�rio
        public const uint PREMIUM_MASCOT_TYPEID = 1073741900u; // Lolo (Premium)
                                                               // !@ tempor�rio
        public const uint PREMIUM_TITLE_TYPEID = 964690582u; //Title Premium Temporario
                                                             // !@ tempor�rio
        public const uint PREMIUM_2_TITLE_TYPEID = 964690583u; //ainda não fiz
                                                               // !@ tempor�rio
        public const uint PREMIUM_2_BALL_TYPEID = 335544553u; // Sakura (Premium)-(original não tem)
                                                              // !@ tempor�rio
        public const uint PREMIUM_2_CLUBSET_TYPEID = 268447744u; // Songoku Voice ClubSet (VIP) -(original não tem)
                                                                 // !@ tempor�rio
        public const uint PREMIUM_3_CLUBSET_TYPEID = 268455939u; //Paradox Voice ClubSet (VIP)-(original não tem)
                                                                 // !@ tempor�rio
        public const uint PREMIUM_2_AUTO_CALIPER_TYPEID = 436207680u;	//Auto Callipers (NAO SEI COMO FUNCIONA)   

        public const uint TICKET_REPORT_SCROLL_TYPEID = 0x1A000042u;
        public const uint TICKET_REPORT_TYPEID = 0x1A000041u;

        public const string STDA_END_LINE = "\r\n";

        public const uint TIME_BOOSTER_TYPEID = 0x1A000011;
        public const uint COIN_TYPEID = 0x1A000010;
        public const uint SPINNING_CUBE_TYPEID = 0x1A00015B;
        public const uint KURAFAITO_RING_CLUBMASTERY = 0x70210009;
        public const uint AUTO_COMMAND_TYPEID = 0x1A00019F;
        public const uint AUTO_CALIPER_TYPEID = 0x1A000040;
        public const uint POWER_MILK_TYPEID = 0x18000025;
        public const uint CHIP_IN_PRACTICE_TICKET_TYPEID = 0x1A00017Eu;

        public static readonly uint[] silent_wind_item = { 0x18000006, 0x1800002C, 0x1800002D, 0x1800002F };

        public static readonly uint[] safety_item = { 0x18000028, 0x1800002D };

        public static readonly uint[] passive_item = { 0x1A00000A, 0x1A00000B, 0x1A00000D, 0x1A00000E, 0x1A00000F/*1 Por Game*/, 0x1A000013, 0x1A000014/*1 Por Game*/
															, 0x1A00002F, 0x1A000035, 0x1A000084, 0x1A000085, 0x1A000086, 0x1A000090, 0x1A000099, 0x1A0000AD, 0x1A0000FC/*Double Exp*/,
                                        0x1A000001/*2*/ , 0x1A000002/*2*/ , 0x1A0000AE/*2*/  , 0x1A000005/*x4*/ , 0x1A0003B7/*x4*/, 0x1A0001D7/*0,5*/, 0x1A0001D8/*0,5*/
																		, 0x1A00025A/*0,4*/, 0x1A000007/*0,2*/, 0x1A000008/*0,2*/, 0x1A000009/*0,2*/ , 0x1A00000C/*0,2*/ /*Double Pang*/,
                                        0x1A000040/*Auto Caliper*/ , 0x1A000011/*Time Booster*/ , 0x1A00019F/*Auto Commander*/ , 0x1A0001A0/*Vector Sign*/ , 0x1A000136/*Fairy's Tears*/ ,
                                        0x1A000338/*Banana Club Mastery Boost*/ /*Help On Game*/ };

        // Consome 1 por jogo
        // Pirulito x2 de Exp boost que só consome 1 por jogo, os outros consome por hole
        public static readonly uint[] passive_item_exp_1perGame = { 0x1A00000F/*1 Por Game*/, 0x1A000014/*1 Por Game*/ };

        // Exp Boost todos são x2
        public static readonly uint[] passive_item_exp = { 0x1A00000A, 0x1A00000B, 0x1A00000D, 0x1A00000E, 0x1A00000F/*1 Por Game*/, 0x1A000013, 0x1A000014/*1 Por Game*/
										  , 0x1A00002F, 0x1A000035, 0x1A000084, 0x1A000085, 0x1A000086, 0x1A000090, 0x1A000099, 0x1A0000AD, 0x1A0000FC,/*Double Exp*/ };

        // Pang Boost X2
        public static readonly uint[] passive_item_pang_x2 = { 0x1A000001/*2*/ , 0x1A000002/*2*/ , 0x1A0000AE/*2*/ };

        // Pang Boost X4
        public static readonly uint[] passive_item_pang_x4 = { 0x1A000005/*x4*/, 0x1A0003B7/*x4*/, };

        // Pang Boost X1.5
        public static readonly uint[] passive_item_pang_x1_5 = { 0x1A0001D7/*0,5*/, 0x1A0001D8/*0,5*/ };

        // Pang Boost X1.4
        public static readonly uint[] passive_item_pang_x1_4 = { 0x1A00025A/*0,4*/ };

        // Pang Boost X1.2
        public static readonly uint[] passive_item_pang_x1_2 = { 0x1A000007/*0,2*/, 0x1A000008/*0,2*/, 0x1A000009/*0,2*/, 0x1A00000C/*0,2*/ };

        public static readonly uint[] passive_item_pang = { 0x1A000001/*2*/, 0x1A000002/*2*/, 0x1A0000AE/*2*/,
                                             0x1A000005/*x4*/, 0x1A0003B7/*x4*/,
                                             0x1A0001D7/*0,5*/, 0x1A0001D8/*0,5*/,
                                             0x1A00025A/*0,4*/,
                                             0x1A000007/*0,2*/, 0x1A000008/*0,2*/, 0x1A000009/*0,2*/, 0x1A00000C/*0,2*/ /*Double Pang*/ };

        // Banana que da x2 Club Mastry, consome 1 por Hole [BANANA_CLUB_MASTERY_BOOST]
        public static readonly uint[] passive_item_club_boost = { 0x1A000338 };

        public const int DL_LIMIT_ITEM_PER_PAGE = 20;
        // Artefact of EXP

        // Artefact of Rain Rate

        // Artefact Frozen Flame
        public const uint ART_LUMINESCENT_CORAL = 0x1A0001AAu; // 2%
        public const uint ART_TROPICAL_TREE = 0x1A0001ACu; // 4%
        public const uint ART_TWIN_LUNAR_MIRROR = 0x1A0001AEu; // 6%
        public const uint ART_MACHINA_WRENCH = 0x1A0001B0u; // 8%
        public const uint ART_SILVIA_MANUAL = 0x1A0001B2u; // 10%
        public const uint ART_SCROLL_OF_FOUR_GODS = 0x1A0001C0u; // 5%
        public const uint ART_ZEPHYR_TOTEM = 0x1A0001C2u; // 10%
        public const uint ART_DRAGON_ORB = 0x1A0001F8u; // 20%
        public const uint ART_FROZEN_FLAME = 0x1A0001FAu; // Mantém os itens Active Equipados, ou sejá não consome eles

        public static readonly uint[] devil_wings = { 0x08016801, 0x08058801, 0x08098801, 0x080dc801, 0x08118801, 0x08160801, 0x08190801, 0x081e2801, 0x08214801, 0x08254801, 0x082d480d, 0x08314801, 0x0839c801 };
        public static readonly uint[] obsidian_wings = { 0x0801680c, 0x0805880c, 0x0809880c, 0x080dc80c, 0x0811880c, 0x0816080c, 0x0819080c, 0x081e280c, 0x0821480c, 0x0825480c, 0x0829480c, 0x082d4806, 0x0831480a, 0x0839c80a };
        public static readonly uint[] corrupt_wings = { 0x08016810, 0x08058810, 0x08098810, 0x080dc810, 0x08118810, 0x08160810, 0x08190810, 0x081e2810, 0x08214810, 0x08254810, 0x08294812, 0x082d4803, 0x0831480d, 0x0839c80d };
        public static readonly uint[] hasegawa_chirain = { 0x8190809, 0x8254808 }; // Item de manter chuva
        public static readonly uint[] hat_spooky_halloween = { 0x0801880b, 0x0805a832, 0x0809a835, 0x080d084c, 0x0811a831, 0x0815a062, 0x0818e05c, 0x081d8837, 0x08212059, 0x08252026 };
        public static readonly uint[] hat_lua_sol = { 0x8018803, 0x805A828, 0x809A827, 0x80D083F, 0x811A823, 0x815A855, 0x818E050, 0x81D8825, 0x821204A, 0x8252015 }; // Dá 20% de Exp e Pang
        public static readonly uint[] hat_birthday = { 0x08000885, 0x0805a81c, 0x08080832, 0x080d0836, 0x08100038, 0x0815a047, 0x0818e048, 0x081d881e, 0x0821203c, 0x08252013, 0x0829200e, 0x082d6000 };
        // Arts
        public const uint ORCHID_BLOSSOM_ART = 0x1A0001A4;
        public const uint PENNE_ABACUS_ART = 0x1A0001A6;
        public const uint TITAN_WINDMILL_ART = 0x1A0001A8;

        // Game Rules
        public const uint ONLY_1M_RULE = 0x1A000265;
        public const uint SUPER_WIND_RULE = 0x1A000266;
        public const uint HOLE_CUP_MAGNET_RULE = 0x1A000269;
        public const uint NO_TURNING_BACK_RULE = 0x1A00026A;
        public const uint WIND_3M_A_5M_RULE = 0x1A00028F;
        public const uint WIND_7M_A_9M_RULE = 0x1A000290;
        public const uint ART_RAINBOW_MAGIC_HAT = 0x1A0001BE;
        public const uint ART_WICKED_BROOMSTICK = 0x1A0001B4;
        public const uint ART_TEORITE_ORE = 0x1A0001B6;
        public const uint ART_REDNOSE_WIZBERRY = 0x1A0001B8;
        public const uint ART_MAGANI_FLOWER = 0x1A0001BA;
        public const uint ART_ROGER_K_STEERING_WHEEL = 0x1A0001BC;
        public const uint SSC_TICKET = 0x1A0000F7;
        // Motion Item da Treasure Hunter Point Também
        public static readonly uint[] motion_item = { 0x08026800, 0x08026801, 0x08026802, 0x08064800, 0x08064801, 0x08064802, 0x08064803, 0x080A2800, 0x080A2801, 0x080A2802, 0x080E4800, 0x080E4801, 0x080E4802, 0x08122800, 0x08122801, 0x08122802, 0x0816E801, 0x0816E802, 0x0816E803, 0x0816E805, 0x816E806, 0x081A4800, 0x081A4801, 0x081EA800, 0x08228800, 0x08228801, 0x08228802, 0x08228803, 0x08268800, 0x082A6800, 0x082E4800, 0x082E4801, 0x08320800, 0x08320801, 0x08320802, 0x083A4800, 0x083A4801, 0x083A4802 };
        public const float ALTURA_MIN_TO_CUBE_SPAWN = 60.0f; // 60 (SCALE_PANGYA)
        public const uint LIMIT_LOCATION_COIN_CUBE_PER_HOLE_PAR_3 = 30;
        public const uint LIMIT_LOCATION_COIN_CUBE_PER_HOLE_PAR_4 = 100;
        public const uint LIMIT_LOCATION_COIN_CUBE_PER_HOLE_PAR_5 = 150;
        public const uint UPDATE_TIME_INTERVAL_HOUR = 24u;
        public const string BOT_GM_EVENT_NAME = "Bot GM Event";//title room gm bot
        public const string MESSAGE_BOT_GM_EVENT_START_PART1 = @"Bot GM Event comecou, sala criada no canal """;
        public const string MESSAGE_BOT_GM_EVENT_START_PART2 = @""", o jogo comeca em ";
        public const string MESSAGE_BOT_GM_EVENT_START_PART3 = " minutos. Os premios sao ";


        public static int[] ExpByLevel = { 30, 40, 50, 60, 70, 140,					// ROOKIE
												   105, 125, 145, 165, 330,					// BEGINNER
												   248, 278, 308, 338, 675,					// JUNIOR
												   506, 546, 586, 626, 1253,					// SENIOR
												   1002, 1052, 1102, 1152, 2304,				// AMADOR
												   1843, 1903, 1963, 2023, 4046,				// SEMI PRO
												   3237, 3307, 3377, 3447, 6894,				// PRO
												   5515, 5595, 5675, 5755, 11511,				// NACIONAL
												   8058, 8148, 8238, 8328, 16655,				// WORLD PRO
												   8328, 8428, 8528, 8628, 17255,				// MESTRE
												   9490, 9690, 9890, 10090, 20181,			// TOP_MASTER
												   20181, 20481, 20781, 21081, 42161,			// WORLD_MASTER
												   37945, 68301, 122942, 221296, 442592,		// LEGEND
												   663887, 995831, 1493747, 2240620, 0 };// INFINIT_LEGEND

        ///* MAKE TROFEL ROOM GAME
        // Argument:
        //	soma "Total Level"
        //	num_player "Numero de Jogadores
        // Return trofel Typeid or 0
        public static uint STDA_MAKE_TROFEL(uint soma, int numPlayer)
        {
            if (numPlayer != 0)
            {
                uint match = (uint)sIff.getInstance().MATCH;  // Supondo singleton com propriedade MATCH
                uint roundSoma = STDA_ROUND_SOMA_LEVEL(soma);

                return (match << 26) | ((roundSoma / (uint)numPlayer / 5) << 16);
            }
            else
            {
                return 0;
            }
        }

        public static bool CHECK_PASSIVE_ITEM(uint _typeid)
        {
            var res = (sIff.getInstance().getItemGroupIdentify((_typeid)) == IFF_GROUP.ITEM && sIff.getInstance().getItemSubGroupIdentify24((_typeid)) > 1/*Passive Item*/);

            return res;
        }
        ///* ROUND SOMA LEVEL ROOM GAME
        // soma "Total Level"
        // Return ROUND soma 
        public static uint STDA_ROUND_SOMA_LEVEL(uint _soma)
        {
            // Implementação fictícia: talvez faça um arredondamento ou um ajuste
            return ((_soma) + (5 - ((_soma) % 5)) - 5);
        }
        ///* TRANSFORMA O VALOR de porcentagem de exibição 100% para 1,0
        public static float TRANSF_SERVER_RATE_VALUE(int value)
        {
            return (value <= 0) ? 1.0f : value / 100f;
        }

        public static ulong enumToBitValue(Enum value)
        {
            return 1UL << Convert.ToInt32(value);
        }

        public static eTYPE_DISTANCE calculeTypeDistance(float distance)
        {
            eTYPE_DISTANCE type = eTYPE_DISTANCE.BIGGER_OR_EQUAL_58;

            if (distance >= 58f)
                return eTYPE_DISTANCE.BIGGER_OR_EQUAL_58;
            else if (distance < 10f)
                return eTYPE_DISTANCE.LESS_10;
            else if (distance < 15f)
                return eTYPE_DISTANCE.LESS_15;
            else if (distance < 28f)
                return eTYPE_DISTANCE.LESS_28;
            else if (distance < 58f)
                return eTYPE_DISTANCE.LESS_58;

            return type;
        }

        public static eTYPE_DISTANCE calculeTypeDistanceByPosition(Vector3D vec1, Vector3D vec2)
        {
            return calculeTypeDistance(vec1.distanceXZTo(vec2) * DIVIDE_SCALE_PANGYA);
        }

        public static float getPowerShotFactory(byte ps)
        {
            float powerShotFactory = 0f;

            switch ((ePOWER_SHOT_FACTORY)ps)
            {
                case ePOWER_SHOT_FACTORY.ONE_POWER_SHOT:
                    powerShotFactory = 10f;
                    break;
                case ePOWER_SHOT_FACTORY.TWO_POWER_SHOT:
                    powerShotFactory = 20f;
                    break;
                case ePOWER_SHOT_FACTORY.ITEM_15_POWER_SHOT:
                    powerShotFactory = 15f;
                    break;
            }

            return powerShotFactory;
        }

        public static float getPowerByDegreeAndSpin(float degree, float spin)
        {
            return (float)(0.5f + (0.5f * (degree + (spin * POWER_SPIN_PW_FACTORY))) / ((56f / 180f) * PI));
        }
    }

    //---------------------Broadcast Types---------------------//
    public enum eBROADCAST_TYPES : byte
    {
        BT_HIDE_BROADCAST, // Pangya JP ficava mandado esse tipo com "<BroadCastReservedNoticesIdx>[531, 532],[549,550]</BroadCastReservedNoticesIdx>" dos que vi
        BT_SPINNING_CUBE_RARE,
        BT_SPINNING_CUBE_WIN_PANG_POUCH,
        BT_GOLDEN_TIME_START_OF_DAY = 11, // Habilitou o Golden Time Event, ou é a primeira do dia programado
        BT_GOLDEN_TIME_START_ROUND,
        BT_GOLDEN_TIME_ROUND_MORE_PEOPLE, // Tem muita pessoas jogando ou em sala lounge
        BT_GOLDEN_TIME_ROUND_REWARD_PLAYER,
        BT_GOLDEN_TIME_FINISH_ROUND,
        BT_GOLDEN_TIME_FINISH_OF_DAY, // Finaliza o dia do Golden Time Event e fala a data do próximo programado
        BT_GOLDEN_TIME_FINISH, // Termina o Evento Golden Time não tem outro evento programado
        BT_GOLDEN_TIME_ROUND_NOT_HAVE_WINNERS, // Não teve ganhadores no round
        BT_MESSAGE_PLAIN = 20, // Aqui ele mostra uma message normal, como se fosse o do GM broadcast
        BT_GRAND_ZODIAC_EVENT_START_TIME
    }
    //--------------------------End----------------------------//



    public class stIdentifyKey
    {

        public uint _typeid;
        public int id;
        public stIdentifyKey(uint __typeid, int _id)
        {
            _typeid = (__typeid);
            id = (_id);
        }
        public static bool operator <(stIdentifyKey MyIntLeft, stIdentifyKey _ik)
        {

            // Classifica pelo ID, depois o typeid
            if (MyIntLeft.id != _ik.id)
                return MyIntLeft.id < _ik.id;
            else
                return MyIntLeft._typeid < _ik._typeid;
        }

        public static bool operator >(stIdentifyKey MyIntLeft, stIdentifyKey _ik)
        {

            // Classifica pelo ID, depois o typeid
            if (MyIntLeft.id != _ik.id)
                return MyIntLeft.id < _ik.id;
            else
                return MyIntLeft._typeid < _ik._typeid;
        }

    }


    public class stLocation
    {
        public stLocation()
        {

        }
        public stLocation(float x, float z, float r)
        {
            this.x = x;
            this.z = z;
            this.r = r;
        }

        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float r { get; set; }    // Face

        public static stLocation operator +(stLocation _old_location, stLocation _add_location)
        {
            return new stLocation()
            {
                x = _old_location.x + _add_location.x,
                y = _old_location.y + _add_location.y,
                z = _old_location.z + _add_location.z,
                r = _old_location.r + _add_location.r
            };
        }
        public static stLocation operator -(stLocation _old_location, stLocation _add_location)
        {
            return new stLocation()
            {
                x = _old_location.x - _add_location.x,
                y = _old_location.y - _add_location.y,
                z = _old_location.z - _add_location.z,
                r = _old_location.r - _add_location.r
            };
        }

    }

    /*
         Skin[Title] map Call back function to trate Condition 
        */
    public class stTitleMapCallback
    {

        // Function Callback type

        // Constructor
        public stTitleMapCallback(uint _ul = 0)
        {
            // Construtor sem callback ou argumento
        }

        // Construtor com callback e argumento
        public stTitleMapCallback(Func<object, int> _callback, object _arg)
        {
            call_back = _callback;
            arg = _arg;
        }

        public uint exec()
        {
            if (call_back != null)
            {
                int result = call_back.Invoke(arg); // Chama o callback e pega o resultado (int)
                return (uint)result; // Retorna o valor como uint
            }
            else
            {
                // Exemplo de mensagem de erro
                _smp.message_pool.getInstance().push(new message("[PlayerInfo::stTitleMapCallBack::exec][Error] call_back is null.", 0));
                return 0;
            }
        }
        Func<object, int> call_back;
        object arg;
    }


    public class player_info
    {
        public uint uid { get; set; }
        public BlockFlag block_flag { get; set; }
        public short level { get; set; }
        public string id { get; set; }
        public string nickname { get; set; }
        public string pass { get; set; }
        public byte m_state_logged;
        public player_info()
        {
            block_flag = new BlockFlag();
            id = "";
            nickname = "";
            pass = "";
        }

        public void set_info(player_info info)
        {
            uid = info.uid;
            level = info.level;
            block_flag = info.block_flag;
            nickname = info.nickname;
            pass = info.pass;
        }
    }
    public class stSyncUpdateDB
    {
        public enum eSTATE_UPDATE : byte
        {
            NONE,
            REQUEST_UPDATE,
            UPDATE_CONFIRMED,
            ERROR_UPDATE
        }

        private eSTATE_UPDATE m_state; 
        private readonly AutoResetEvent m_cv = new AutoResetEvent(false);

        public stSyncUpdateDB()
        {
            m_state = eSTATE_UPDATE.NONE;
        }

        public void requestUpdateOnDB()
        {
            int timeoutCount = 3; // 30 segundos check, para enviar nova requisição
            int timeoutMs = 10000; // 10 segundos de espera
            if (m_state == eSTATE_UPDATE.REQUEST_UPDATE)
            {
                while (m_state == eSTATE_UPDATE.REQUEST_UPDATE && timeoutCount > 0)
                {
                    bool signaled = m_cv.WaitOne(timeoutMs);

                    if (!signaled)
                    {
                        timeoutCount--;
                    }
                }

                if (timeoutCount == 0)
                {
                    // Log de advertência se o tempo expirar
                    Console.WriteLine("[SyncUpdateDB::RequestUpdateOnDB][Warning] 30 segundos consumidos, mudança de estado forçada.");
                }
            }

            // Atualiza o estado para REQUEST_UPDATE
            m_state = eSTATE_UPDATE.REQUEST_UPDATE;
        }

        public void confirmUpdadeOnDB()
        {
            if (m_state != eSTATE_UPDATE.REQUEST_UPDATE)
            {
                throw new Exception("[SyncUpdateDB::ConfirmUpdateOnDB][Error] m_state está errado, não é REQUEST_UPDATE.");
            }

            // Atualiza o estado para UPDATE_CONFIRMED
            m_state = eSTATE_UPDATE.UPDATE_CONFIRMED;

            // Acorda as threads que estão esperando a condition variable
            m_cv.Set();
        }

        public void errorUpdateOnDB()
        {
            if (m_state != eSTATE_UPDATE.REQUEST_UPDATE)
            {
                throw new Exception("[SyncUpdateDB::ErrorUpdateOnDB][Error] m_state está errado, não é REQUEST_UPDATE.");
            }

            // Atualiza o estado para ERROR_UPDATE
            m_state = eSTATE_UPDATE.ERROR_UPDATE;

            // Acorda as threads que estão esperando a condition variable
            m_cv.Set();
        }
    }

    // Player Location para atualização do no banco de dados
    public class stPlayerLocationDB : stSyncUpdateDB
    {
        public stPlayerLocationDB(uint _ul = 0u)
        {
            clear();
        }

        ~stPlayerLocationDB()
        {
            clear();
        }

        void clear()
        {
            channel = 255;
            lobby = 255;
            room = 255;
            place = new PlayerPlace();
        }
        public byte channel;
        public byte lobby;
        public ushort room;
        public PlayerPlace place;
    }
    public class ctx_UCCWebKey
    {
        public byte opt;
        public uint uid;
        public byte seq;
        public int item_id;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UCC_Load_Ctx
    {
        public uint _typeid;
        public int id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string ucc_idx;
    }
    public class ChatPenaltyManager
    {

        private const int ResetIntervalMs = 10 * 60 * 1000; // 10 minutos
        private static readonly Dictionary<int, int> OffenseToMinutes = new Dictionary<int, int>()
        {
            [1] = 1,
            [2] = 5,
            [3] = 15,
            [4] = 60
        };

        private int _offenseCount = 0;
        private int _lastOffenseTick = 0;

        private int _resetOffenseTick = 0;
        public bool IsBlocked { get; private set; } = false;
        public int BlockExpireTick { get; private set; } = 0;
        public void RegisterOffense(uint uid, string reason)
        {
            int now = Environment.TickCount;

            // Verifica se o tempo total passou desde a última ofensa
            if (now > _resetOffenseTick)
                _offenseCount = 0;

            _offenseCount++;
            _lastOffenseTick = now;

            // Tempo de bloqueio baseado nas reincidências
            int blockMinutes = OffenseToMinutes.TryGetValue(_offenseCount, out var min) ? min : 60;

            // Marca o tempo que o bloqueio vai acabar
            BlockExpireTick = now + blockMinutes * 60_000;
            IsBlocked = true;

            // Define o novo tempo de expiração da reincidência (ex: 5min após essa ofensa)
            _resetOffenseTick = now + ResetIntervalMs;

            _smp.message_pool.getInstance().push(new message(
                $"[ChatBlock]: UID={uid} bloqueado por {blockMinutes} minuto(s) (motivo: {reason}, reincidências: {_offenseCount})",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void ClearBlock()
        {
            IsBlocked = false;
            BlockExpireTick = 0;
            _offenseCount = 0;
        }

        public bool IsStillBlocked()
        {
            if (!IsBlocked)
                return false;

            if (Environment.TickCount >= BlockExpireTick)
            {
                ClearBlock();
                return false;
            }

            return true;
        }

        public int GetOffenseCount() => _offenseCount;
    }

    // MemberInfo dados principais do player, tem id, nick, guild, level, exp, e etc)
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 297)]
    public class MemberInfo
    {
        public MemberInfo()
        {
            Clear();
        }

        public void Clear()
        {
            rank = new uint[3];
            id_bytes = new byte[22];
            nick_name_bytes = new byte[22];
            guild_name_bytes = new byte[17];
            guild_mark_img = "";
            sComment = "";
            channeling_flag = 0;
            point_point_event = 0;
            gallery_uid = 0;
            nick_NT_bytes = new byte[128];
            capability = new uCapability();
            state_flag = new uMemberInfoStateFlag();
            papel_shop = new PlayerPapelShopInfo();
            oid = -1;
            flag_block = new BlockFlag();
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        private byte[] id_bytes;
        public string id
        {
            get => id_bytes.GetString();
            set => id_bytes.SetString(value);
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        private byte[] nick_name_bytes;
        public string nick_name
        {
            get => nick_name_bytes.GetString();
            set => nick_name_bytes.SetString(value);
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] guild_name_bytes;
        public string guild_name
        {
            get => guild_name_bytes.GetString();
            set => guild_name_bytes.SetString(value);
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string guild_mark_img;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
        public string sComment;
        public uint school;
        [field: MarshalAs(UnmanagedType.Struct)]
        public uCapability capability;
        public uint gallery_uid;//gallery_uid
        public int oid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] rank;
        public uint guild_uid;
        public uint guild_mark_img_no; // só tem no JP
        [field: MarshalAs(UnmanagedType.Struct)]
        public uMemberInfoStateFlag state_flag;
        public ushort flag_login_time;
        [field: MarshalAs(UnmanagedType.Struct)]
        public PlayerPapelShopInfo papel_shop;
        public uint point_point_event { get; set; }         // S4 TH
        public BlockFlag flag_block { get; set; }      //é 32 bytes é time_block, mas no Fresh UP JP o type block do pacote principal é de 64, então não tem mais o time block
        public uint channeling_flag { get; set; }			// S4 TH
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] nick_NT_bytes;
        public string sDisplayID
        {
            get => nick_NT_bytes.GetString();
            set => nick_NT_bytes.SetString(value);
        }

        /// <summary>
        /// Size = 297 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteStr(id, 22);
                p.WriteStr(nick_name, 22);
                p.WriteStr(guild_name, 17);
                p.WriteStr(guild_mark_img, 12);
                p.WriteStr(sComment, 35);
                p.WriteUInt32(school);          // ainda não o que é aqui direito
                p.WriteInt32(capability.ulCapability);
                p.WriteUInt32(gallery_uid);
                p.WriteInt32(oid);
                p.WriteUInt32(rank);             //Ranking [0] = Total, [1] = Diário, [2] = Guilda
                p.WriteUInt32(guild_uid);
                p.WriteUInt32(guild_mark_img_no);   // só tem no JP
                p.WriteByte(state_flag.ucByte);
                p.WriteUInt16(flag_login_time);     // 1 é primeira vez que logou, 2 já não é mais a primeira vez que fez login no server
                p.WriteBytes(papel_shop.ToArray());
                p.WriteUInt32(point_point_event);         // S4 TH
                p.WriteUInt64(flag_block.m_id_state.ull_IDState);    //é 32 bytes é time_block, mas no Fresh UP JP o type block do pacote principal é de 64, então não tem mais o time block
                p.WriteUInt32(channeling_flag);         // S4 TH
                p.WriteStr(sDisplayID, 128);   //
                Debug.Assert(!(p.GetSize != 297), "MemberInfo::Build() is Error");
                return p.GetBytes;
            }
        }
    }
    // MemberInfoEx extendido tem o m_uid, limite papel shop e tutorial,
    // so os que nao manda para o pangya no pacote MemberInfo
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MemberInfoEx : MemberInfo
    {
        public MemberInfoEx()
        {
            Clear();
            papel_shop_last_update = new SYSTEMTIME();
            papel_shop_last_update.CreateTime();
            sala_numero = DefineConstants.DEFAULT_ROOM_ID;
        }
        public uint uid { get; set; }
        public uint guild_point { get; set; }
        public long guild_pang { get; set; }
        public ushort sala_numero { get; set; }
        public byte sexo;
        public byte level;
        public byte do_tutorial;
        public byte event_1;
        public byte event_2;
        public uint manner_flag;
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME papel_shop_last_update;
        public byte[] ToArrayEx()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(sala_numero);   //
                p.WriteBytes(message: ToArray());   //
                Debug.Assert(!(p.GetSize != 299), "MemberInfo::BuildEx() is Error");
                return p.GetBytes;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public class uCapability
    {
        private int _ulCapability;

        // Propriedade ulCapability com get e set utilizando operações bitwise
        public int ulCapability
        {
            get => _ulCapability;
            set
            {
                _ulCapability = value;
            }
        }
        public bool PLAYER //normal
        {
            get => (ulCapability == 0); // 0x01
            set
            {
                if (value) _ulCapability = 0; // Ativar o bit
                else _ulCapability &= ~0; // Desativar o bit
            }
        }
        // Flags de bit diretamente nos setters
        public bool A_I_MODE
        {
            get => (ulCapability & (uint)CapabilityFlags.COMPUTER) != 0; // 0x01
            set
            {
                if (value) _ulCapability |= 1; // Ativar o bit
                else _ulCapability &= ~1; // Desativar o bit
            }
        }

        public bool gallery //TODO: # 2 n�o sei bem mas estava na type que o mlk falou que era GM + PC Bang + Premium 
        {
            get => (_ulCapability & (int)CapabilityFlags.GALLERY) != 0; // 0x02
            set
            {
                if (value) _ulCapability |= 2; // Ativar o bit
                else _ulCapability &= ~2; // Desativar o bit
            }
        }

        public bool game_master
        {
            get => (_ulCapability & (int)CapabilityFlags.GAME_MASTER) != 0; // 0x04
            set
            {
                if (value) _ulCapability |= 4; // Ativar o bit
                else _ulCapability &= ~4; // Desativar o bit
            }
        }

        public bool gm_edit_site
        {
            get => (_ulCapability & (int)CapabilityFlags.GM_EDIT_SITE) != 0; // 0x08
            set
            {
                if (value) _ulCapability |= 8; // Ativar o bit
                else _ulCapability &= ~8; // Desativar o bit
            }
        }

        public bool observer
        {
            get => (_ulCapability & 14) == 14; // 
            set
            {
                if (value) _ulCapability |= 14; // 
                else _ulCapability &= ~14; // 
            }
        }



        public bool God
        {
            get => (_ulCapability & (int)CapabilityFlags.GOD) != 0; // 0x40
            set
            {
                if (value) _ulCapability |= (int)CapabilityFlags.GOD; // Ativar o bit
                else _ulCapability &= ~(int)CapabilityFlags.GOD; // Desativar o bit
            }
        }

        public bool block_give_item_gm
        {
            get => (_ulCapability & 16) != 0; // 0x40
            set
            {
                if (value) _ulCapability |= 16; // Ativar o bit
                else _ulCapability &= ~16; // Desativar o bit
            }
        }

        public bool mod_system_event
        {
            get => (_ulCapability & 64) != 0; // 0x40
            set
            {
                if (value) _ulCapability |= 64; // Ativar o bit
                else _ulCapability &= ~64; // Desativar o bit
            }
        }

        public bool gm_normal
        {
            get => (_ulCapability & 128) != 0; // 0x80
            set
            {
                if (value) _ulCapability |= 128; // Ativar o bit
                else _ulCapability &= ~128; // Desativar o bit
            }
        }

        public bool block_gift_shop
        {
            get => (_ulCapability & (int)CapabilityFlags.BLOCK_GIFT_SHOP) != 0; // 0x100
            set
            {
                if (value) _ulCapability |= 256; // Ativar o bit
                else _ulCapability &= ~256; // Desativar o bit
            }
        }

        public bool login_test_server
        {
            get => (_ulCapability & (int)CapabilityFlags.LOGIN_TEST_SERVER) != 0; // 0x200
            set
            {
                if (value) _ulCapability |= 512; // Ativar o bit
                else _ulCapability &= ~512; // Desativar o bit
            }
        }

        public bool mantle
        {
            get => (_ulCapability & (int)CapabilityFlags.MANTLE) != 0; // 0x400
            set
            {
                if (value) _ulCapability |= 1024; // Ativar o bit
                else _ulCapability &= ~1024; // Desativar o bit
            }
        }

        public bool unknown3
        {
            get => (_ulCapability & 2048) != 0; // 0x800
            set
            {
                if (value) _ulCapability |= 2048; // Ativar o bit
                else _ulCapability &= ~2048; // Desativar o bit
            }
        }

        public bool premium_user
        {
            get => (_ulCapability & (int)CapabilityFlags.PREMIUM_USER) != 0; // 0x02
            set
            {
                if (value) _ulCapability |= 16384; // Ativar o bit
                else _ulCapability &= ~16384; // Desativar o bit
            }
        }

        public bool title_gm
        {
            get => (_ulCapability & (int)CapabilityFlags.TITLE_GM) != 0; // 0x1000
            set
            {
                if (value) _ulCapability |= 32768; // Ativar o bit
                else _ulCapability &= ~32768; // Desativar o bit
            }
        }

        // Construtores
        public uCapability()
        {
            _ulCapability = 0;
        }

        public uCapability(int ul)
        {
            _ulCapability = ul;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1)]
    public class uMemberInfoStateFlag
    {
        public byte ucByte { get; set; } 

            // Bit 0 (0x01) - Valor: 1 ou 0
            public byte channel
            {
                get => (byte)(ucByte & 0x01);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x01) : (ucByte & ~0x01));
            }

            // Bit 1 (0x02) - Valor: 2 ou 0
            public byte visible
            {
                get => (byte)(ucByte & 0x02);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x02) : (ucByte & ~0x02));
            }

            // Bit 2 (0x04) - Valor: 4 ou 0
            public byte whisper
            {
                get => (byte)(ucByte & 0x04);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x04) : (ucByte & ~0x04));
            }

            // Bit 3 (0x08) - Valor: 8 ou 0
            public byte sexo
            {
                get => (byte)(ucByte & 0x08);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x08) : (ucByte & ~0x08));
            }

            // Bit 4 (0x10) - Valor: 16 ou 0
            public byte azinha
            {
                get => (byte)(ucByte & 0x10);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x10) : (ucByte & ~0x10));
            }

            // Bit 5 (0x20) - Valor: 32 ou 0
            public byte icon_angel
            {
                get => (byte)(ucByte & 0x20);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x20) : (ucByte & ~0x20));
            }

            // Bit 6 (0x40) - Valor: 64 ou 0
            public byte quiter_1
            {
                get => (byte)(ucByte & 0x40);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x40) : (ucByte & ~0x40));
            }

            // Bit 7 (0x80) - Valor: 128 ou 0
            public byte quiter_2
            {
                get => (byte)(ucByte & 0x80);
                set => ucByte = (byte)((value != 0) ? (ucByte | 0x80) : (ucByte & ~0x80));
            }

            /// <summary>
            /// Zera todas as flags (Equivalente ao memset original)
            /// </summary>
            public void clear()
            {
                ucByte = 0;
            }

        /// <summary>
        /// Retorna uma representação visual dos bits para Debug
        /// </summary>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"--- MemberInfoStateFlag (Raw: 0x{ucByte:X2}) ---");
            sb.AppendLine($"Binary:   {Convert.ToString(ucByte, 2).PadLeft(8, '0')}");
            sb.AppendLine($"Channel:  {(channel != 0 ? "ON" : "OFF")} (1)");
            sb.AppendLine($"Visible:  {(visible != 0 ? "ON" : "OFF")} (2)");
            sb.AppendLine($"Whisper:  {(whisper != 0 ? "ON" : "OFF")} (4)");
            sb.AppendLine($"Sexo:     {(sexo != 0 ? "ON" : "OFF")} (8)");
            sb.AppendLine($"Azinha:   {(azinha != 0 ? "ON" : "OFF")} (16)");
            sb.AppendLine($"Angel:    {(icon_angel != 0 ? "ON" : "OFF")} (32)");
            sb.AppendLine($"Quiter 1: {(quiter_1 != 0 ? "ON" : "OFF")} (64)");
            sb.AppendLine($"Quiter 2: {(quiter_2 != 0 ? "ON" : "OFF")} (128)");
            sb.Append("------------------------------------");

            return sb.ToString();
        }
    }

    // Player Papel Shop Info
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6)]
    public class PlayerPapelShopInfo
    {
        public ushort remain_count { get; set; }
        public ushort current_count { get; set; }
        public ushort limit_count { get; set; }
        public PlayerPapelShopInfo()
        {
            remain_count = ushort.MaxValue;
            current_count = ushort.MaxValue;//0xFF 0xFF
            limit_count = ushort.MaxValue;
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt16(remain_count);
                p.WriteUInt16(current_count);
                p.WriteUInt16(limit_count);
                return p.GetBytes;
            }
        }
    }

    // Medal Win
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class uMedalWin
    {
        public byte ucMedal { get; set; }
        public _stMedal stMedal { get; set; }
        public uMedalWin()
        {
            stMedal = new _stMedal();
        }
        public class _stMedal
        {
            public byte lucky = 1;
            public byte speediest = 1;
            public byte best_drive = 1;
            public byte best_chipin = 1;
            public byte best_long_puttin = 1;
            public byte best_recovery = 0;


        }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {

                p.Write(ucMedal);   // só tem no JP
                p.Write(stMedal.lucky);   // só tem no JP
                p.Write(stMedal.speediest);//state_flag.ucByte(falta saber das flags)
                p.Write(stMedal.best_drive);     // 1 é primeira vez que logou, 2 já não é mais a primeira vez que fez login no server
                p.Write(stMedal.best_chipin);
                p.Write(stMedal.best_long_puttin);
                p.Write(stMedal.best_recovery);
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class UserInfo
    {
        public UserInfo()
        {
            clear();
        }

        public int tacada { get; set; }
        public int putt { get; set; }
        public int tempo { get; set; }
        public int tempo_tacada { get; set; }
        public float best_drive { get; set; }           // Max Distancia
        public int acerto_pangya { get; set; }
        public int timeout { get; set; }
        public int ob { get; set; }
        public int total_distancia { get; set; }
        public int hole { get; set; }
        public int hole_in { get; set; }        // Aqui é os holes que não foram concluídos Ex: Give up, ou no Match o outro player ganho sem precisar do player terminar o hole
        public int hio { get; set; }
        public short bunker { get; set; }
        public int fairway { get; set; }
        public int albatross { get; set; }
        public int mad_conduta { get; set; }    // Aqui é hole in, mas no info não tras ele por que ele já foi salvo no hole alí em cima
        public int putt_in { get; set; }
        public float best_long_putt { get; set; }
        public float best_chip_in { get; set; }
        public int exp { get; set; }
        public byte level { get; set; }
        public UInt64 pang { get; set; }
        public int media_score { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public sbyte[] best_score { get; set; }              // Best Score Por Estrela, mas acho que o pangya nao usa mais isso
        public byte event_flag { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int64[] best_pang { get; set; }          // Best Pang por Estrela, mas acho que o pangya nao usa mais isso
        public Int64 sum_pang { get; set; }             // A soma do pangs das 5 estrela acho
        public int jogado { get; set; }
        public int team_hole { get; set; }
        public int team_win { get; set; }
        public int team_game { get; set; }
        public int ladder_point { get; set; }               // Ladder é o Match acho, de tourneio não sei direito
        public int ladder_hole { get; set; }
        public int ladder_win { get; set; }
        public int ladder_lose { get; set; }
        public int ladder_draw { get; set; }
        public int combo { get; set; }
        public int all_combo { get; set; }
        public int quitado { get; set; }
        public long skin_pang { get; set; }         // Skin é o Pang Battle tem valor negativo ele """##### Ajeitei agora(ACHO)
        public int skin_win { get; set; }
        public int skin_lose { get; set; }
        public int skin_all_in_count { get; set; }
        public int skin_run_hole { get; set; }              // Correu desistiu (ACHO)
        public int skin_strike_point { get; set; }          // Antes era o nao_sei
        public int jogados_disconnect { get; set; }     // Antes era o jogos_nao_sei
        public short event_value { get; set; }
        public int disconnect { get; set; }             // Vou deixar aqui o disconect count (antes era skin_strike_point)
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 6)]
        public stMedal medal { get; set; }
        public int sys_school_serie { get; set; }           // Sistema antigo do pangya JP que era de Serie de escola, respondia as perguntas se passasse ia pra outra serie é da 1° a 5°
        public int game_count_season { get; set; }
        public short _16bit_nao_sei { get; set; }
        public float getMediaScore()
        {   // AVG SCORE

            // Verifica se é 0, por que não pode dividir 18 por 0 que dá excessão, 
            // por que não pode dividir nenhum número por 0
            if ((hole - hole_in) == 0)
                return 0;
            var result = (18.0f / (hole - hole_in)) * media_score + 72.0f;
            return result;
        }
        public float getPangyaShotRate()
        {

            // Previne divisão por 0
            if (tacada == 0)
                return 0;

            return ((float)acerto_pangya / tacada) * 100;
        }
        public float getFairwayRate()
        {

            // Previne divisão por 0
            if ((hole - hole_in) == 0)
                return 0;

            return ((float)fairway / (hole - hole_in)) * 100;
        }
        public float getPuttRate()
        {

            // Previne divisão por 0
            if (putt == 0)
                return 0;

            return ((float)putt_in / putt) * 100;
        }
        public float getOBRate()
        {

            // Previne divisão por 0
            if ((tacada + putt) == 0)
                return 0;

            return ((float)ob / (tacada + putt)) * 100;
        }
        public float getMatchWinRate()
        {

            // Previne divisão por 0
            if (team_game == 0)
                return 0;

            return ((float)team_win / team_game) * 100;
        }
        public float getShotTimeRate()
        {

            // Previne divisão por 0
            if ((tacada + putt) == 0)
                return 0;

            return ((float)tempo_tacada / (tacada + putt)) * 100;
        }

        public float getQuitRate()
        {

            // Previne divisão por 0
            if (jogado == 0)
                return 0;

            return quitado * 100 / jogado;
        }

        public void clear()
        {
            best_pang = new long[5];
            best_score = new sbyte[5];
            medal = new stMedal();
        }
        public void add(UserInfo _ui)
        {

            if (_ui.best_drive > best_drive)
                best_drive = _ui.best_drive;

            if (_ui.best_long_putt > best_long_putt)
                best_long_putt = _ui.best_long_putt;

            if (_ui.best_chip_in > best_chip_in)
                best_chip_in = _ui.best_chip_in;

            // Combo e Todal Combos
            if (_ui.combo < 0)
            {   // Negativo

                // tira só do combo, não de todos os combos que foram feitos
                if (combo <= DefineConstants.DECREASE_COMBO_VALUE)
                    combo = 0;
                else
                    combo += _ui.combo;

            }
            else
            {                   // Positivo

                combo += _ui.combo;

                // Só soma o all combo se combo > que all_combo
                if (combo > all_combo)
                    all_combo += _ui.combo;
            }

            // Event Angel ativado, quitado < 0
            if (_ui.quitado < 0)
            {

                // Se for 0 não subtrai
                if ((quitado + _ui.quitado) <= 0)
                    quitado = 0;
                else
                    quitado += _ui.quitado;

            }
            else // Normal soma o quit do player se ele quitou
                quitado += _ui.quitado;

            // Skin (Pang Battle)
            if ((skin_all_in_count + _ui.skin_all_in_count) >= 5)
            {

                skin_all_in_count = 0;
                skin_pang += 1000; // dá 1000 pangs por que ele jogou 5 Pang Battle

            }
            else
                skin_all_in_count += _ui.skin_all_in_count;

            tacada += _ui.tacada;
            putt += _ui.putt;
            tempo += _ui.tempo;
            tempo_tacada += _ui.tempo_tacada;
            acerto_pangya += _ui.acerto_pangya;
            timeout += _ui.timeout;
            ob += _ui.ob;
            total_distancia += _ui.total_distancia;
            hole += _ui.hole;
            hole_in += (_ui.hole - _ui.hole_in);
            hio += _ui.hio;
            bunker += _ui.bunker;
            fairway += _ui.fairway;
            albatross += _ui.albatross;
            putt_in += _ui.putt_in;
            media_score += _ui.media_score;
            best_score[0] += _ui.best_score[0];
            best_score[1] += _ui.best_score[1];
            best_score[2] += _ui.best_score[2];
            best_score[3] += _ui.best_score[3];
            best_score[4] += _ui.best_score[4];
            best_pang[0] += _ui.best_pang[0];
            best_pang[1] += _ui.best_pang[1];
            best_pang[2] += _ui.best_pang[2];
            best_pang[3] += _ui.best_pang[3];
            best_pang[4] += _ui.best_pang[4];
            sum_pang += _ui.sum_pang;
            event_flag += _ui.event_flag;
            jogado += _ui.jogado;
            team_game += _ui.team_game;
            team_win += _ui.team_win;
            team_hole += _ui.team_hole;
            ladder_point += _ui.ladder_point;
            ladder_hole += _ui.ladder_hole;
            ladder_win += _ui.ladder_win;
            ladder_lose += _ui.ladder_lose;
            ladder_draw += _ui.ladder_draw;
            skin_pang += _ui.skin_pang;
            skin_win += _ui.skin_win;
            skin_lose += _ui.skin_lose;
            skin_run_hole += _ui.skin_run_hole;
            skin_strike_point += _ui.skin_strike_point;
            disconnect += _ui.disconnect;
            jogados_disconnect += _ui.jogados_disconnect;
            event_value += _ui.event_value;
            sys_school_serie += _ui.sys_school_serie;
            game_count_season += _ui.game_count_season;

            // Medal
            medal.add(_ui.medal);
        }

        /// <summary>
        /// Size = 265 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(tacada);
                p.WriteInt32(putt);
                p.WriteInt32(tempo);
                p.WriteInt32(tempo_tacada);
                p.WriteSingle(best_drive);           // Max Distancia
                p.WriteInt32(acerto_pangya);
                p.WriteInt32(timeout);
                p.WriteInt32(ob);
                p.WriteInt32(total_distancia);
                p.WriteInt32(hole);
                p.WriteInt32(hole_in);        // Aqui é os holes que não foram concluídos Ex: Give up, ou no Match o outro player ganho sem precisar do player terminar o hole
                p.WriteInt32(hio);
                p.WriteInt16(bunker);
                p.WriteInt32(fairway);
                p.WriteInt32(albatross);
                p.WriteInt32(mad_conduta);    // Aqui é hole in, mas no info não tras ele por que ele já foi salvo no hole alí em cima
                p.WriteInt32(putt_in);
                p.WriteSingle(best_long_putt);
                p.WriteSingle(best_chip_in);
                p.WriteInt32(exp);
                p.WriteByte(level);
                p.WriteUInt64(pang);
                p.WriteInt32(media_score);
                p.WriteSBytes(best_score);              // Best Score Por Estrela, mas acho que o pangya nao usa mais isso
                p.WriteByte(event_flag);
                p.WriteInt64(best_pang);          // Best Pang por Estrela, mas acho que o pangya nao usa mais isso
                p.WriteInt64(sum_pang);             // A soma do pangs das 5 estrela acho
                p.WriteInt32(jogado);
                p.WriteInt32(team_hole);
                p.WriteInt32(team_win);
                p.WriteInt32(team_game);
                p.WriteInt32(ladder_point);               // Ladder é o Match acho, de tourneio não sei direito
                p.WriteInt32(ladder_hole);
                p.WriteInt32(ladder_win);
                p.WriteInt32(ladder_lose);
                p.WriteInt32(ladder_draw);
                p.WriteInt32(combo);
                p.WriteInt32(all_combo);
                p.WriteInt32(quitado);
                p.WriteInt64(skin_pang);         // Skin é o Pang Battle tem valor negativo ele """##### Ajeitei agora(ACHO)
                p.WriteInt32(skin_win);
                p.WriteInt32(skin_lose);
                p.WriteInt32(skin_all_in_count);
                p.WriteInt32(skin_run_hole);              // Correu desistiu (ACHO)
                p.WriteInt32(skin_strike_point);          // Antes era o nao_sei
                p.WriteInt32(jogados_disconnect);     // Antes era o jogos_nao_sei
                p.WriteInt16(event_value);
                p.WriteInt32(disconnect);             // Vou deixar aqui o disconect count (antes era skin_strike_point)
                p.WriteBytes(medal.ToArray());
                p.WriteInt32(sys_school_serie);           // Sistema antigo do pangya JP que era de Serie de escola, respondia as perguntas se passasse ia pra outra serie é da 1° a 5°
                p.WriteInt32(game_count_season);
                p.WriteInt16(_16bit_nao_sei);
                Debug.Assert(!(p.GetSize != 265), "UserInfo::Build() is Error");

                return p.GetBytes;
            }
        }


        public override string ToString()
        {
            return "Tacada: " + (tacada) + "  Putt: " + (putt) + "  Tempo: " + (tempo) + "  Tempo Tacada: " + (tempo_tacada)
                + "  Best drive: " + (best_drive) + "  Acerto pangya: " + (acerto_pangya) + "  timeout: " + (timeout)
                + "  OB: " + (ob) + "  Total distancia: " + (total_distancia) + "  hole: " + (hole)
                + "  Hole in: " + (hole_in) + "  HIO: " + (hio) + "  Bunker: " + (bunker) + "  Fairway: " + (fairway)
                + "  Albratross: " + (albatross) + "  Mad conduta: " + (mad_conduta) + "  Putt in: " + (putt_in)
                + "  Best long puttin: " + (best_long_putt) + "  Best chipin: " + (best_chip_in) + "  Exp: " + (exp)
                + "  Level: " + (level) + "  Pang: " + (pang) + "  Media score: " + (media_score)
                + "  Best score[" + (best_score[0]) + ", " + (best_score[1]) + ", " + (best_score[2])
                + ", " + (best_score[3]) + ", " + (best_score[4]) + "]  Event type: " + (event_flag)
                + "  Best pang[" + (best_pang[0]) + ", " + (best_pang[1]) + ", " + (best_pang[2]) + ", " + (best_pang[3])
                + ", " + (best_pang[4]) + "]  Soma pang: " + (sum_pang) + "  Jogado: " + (jogado) + "  Team Hole: " + (team_hole)
                + "  Team win: " + (team_win) + "  Team game: " + (team_game) + "  Ladder point: " + (ladder_point)
                + "  Ladder hole: " + (ladder_hole) + "  Ladder win: " + (ladder_win) + "  Ladder lose: " + (ladder_lose)
                + "  Ladder draw: " + (ladder_draw) + "  Combo: " + (combo) + "  All combo: " + (all_combo)
                + "  Quitado: " + (quitado) + "  Skin pang: " + (skin_pang) + "  Skin win: " + (skin_win)
                + "  Skin lose: " + (skin_lose) + "  Skin all in count: " + (skin_all_in_count) + "  Skin run hole: " + (skin_run_hole)
                + "  Disconnect(MY): " + (disconnect) + "  Jogados Disconnect(MY): " + (jogados_disconnect) + "  Event value: " + (event_value)
                + "  Skin Strike Point: " + (skin_strike_point) + "  Sistema School Serie: " + (sys_school_serie)
                + "  Game count season: " + (game_count_season) + "  _16bit nao sei: " + (_16bit_nao_sei);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class UserInfoEx : UserInfo
    {
        public UserInfoEx()
        { }
        public void add(UserInfoEx _ui, ulong _total_pang_win_game)
        {
            add(_ui);
            if (_total_pang_win_game > 0)
                total_pang_win_game += _total_pang_win_game;
        }

        public ulong total_pang_win_game { get; set; }

        public string[] ToStringDB()
        {
            return new string[] {
    best_drive.ToString(),
    best_chip_in.ToString(),
    best_long_putt.ToString(),
    combo.ToString(),
    all_combo.ToString(),
    tacada.ToString(),
    putt.ToString(),
    tempo.ToString(),
    tempo_tacada.ToString(),
    acerto_pangya.ToString(),
    timeout.ToString(),
    ob.ToString(),
    total_distancia.ToString(),
    hole.ToString(),
    hole_in.ToString(),
    hio.ToString(),
    bunker.ToString(),
    fairway.ToString(),
    albatross.ToString(),
    mad_conduta.ToString(),
    putt_in.ToString(),
    media_score.ToString(),
    best_score[0].ToString(),
    best_score[1].ToString(),
    best_score[2].ToString(),
    best_score[3].ToString(),
    best_score[4].ToString(),
    best_pang[0].ToString(),
    best_pang[1].ToString(),
    best_pang[2].ToString(),
    best_pang[3].ToString(),
    best_pang[4].ToString(),
    sum_pang.ToString(),
    event_flag.ToString(),
    jogado.ToString(),
    team_game.ToString(),
    team_win.ToString(),
    team_hole.ToString(),
    ladder_point.ToString(),
    ladder_hole.ToString(),
    ladder_win.ToString(),
    ladder_lose.ToString(),
    ladder_draw.ToString(),
    quitado.ToString(),
    skin_pang.ToString(),
    skin_win.ToString(),
    skin_lose.ToString(),
    skin_run_hole.ToString(),
    skin_all_in_count.ToString(),
    disconnect.ToString(),
    jogados_disconnect.ToString(),
    event_value.ToString(),
    skin_strike_point.ToString(),
    sys_school_serie.ToString(),
    game_count_season.ToString(),
    (total_pang_win_game).ToString(),
    medal.lucky.ToString(),
    medal.fast.ToString(),
    medal.best_drive.ToString(),
    medal.best_chipin.ToString(),
    medal.best_puttin.ToString(),
    medal.best_recovery.ToString(),
    _16bit_nao_sei.ToString(),
};
        }

        public UserInfoEx ToRead(packet p)
        {
            tacada = p.ReadInt32();
            putt = p.ReadInt32();
            tempo = p.ReadInt32();
            tempo_tacada = p.ReadInt32();
            best_drive = p.ReadSingle();
            acerto_pangya = p.ReadInt32();
            timeout = p.ReadInt32();
            ob = p.ReadInt32();
            total_distancia = p.ReadInt32();
            hole = p.ReadInt32();
            hole_in = p.ReadInt32();
            hio = p.ReadInt32();
            bunker = p.ReadInt16();
            fairway = p.ReadInt32();
            albatross = p.ReadInt32();
            mad_conduta = p.ReadInt32();
            putt_in = p.ReadInt32();
            best_long_putt = p.ReadSingle();
            best_chip_in = p.ReadSingle();
            exp = p.ReadInt32();
            level = p.ReadByte();
            pang = p.ReadUInt64();
            media_score = p.ReadInt32();
            best_score = p.ReadSBytes(5);
            event_flag = p.ReadByte();
            best_pang = new long[5];
            for (int i = 0; i < 5; i++)
                best_pang[i] = p.ReadInt64();
            sum_pang = p.ReadInt64();
            jogado = p.ReadInt32();
            team_hole = p.ReadInt32();
            team_win = p.ReadInt32();
            team_game = p.ReadInt32();
            ladder_point = p.ReadInt32();
            ladder_hole = p.ReadInt32();
            ladder_win = p.ReadInt32();
            ladder_lose = p.ReadInt32();
            ladder_draw = p.ReadInt32();
            combo = p.ReadInt32();
            all_combo = p.ReadInt32();
            quitado = p.ReadInt32();
            skin_pang = p.ReadInt64();
            skin_win = p.ReadInt32();
            skin_lose = p.ReadInt32();
            skin_all_in_count = p.ReadInt32();
            skin_run_hole = p.ReadInt32();
            skin_strike_point = p.ReadInt32();
            jogados_disconnect = p.ReadInt32();
            event_value = p.ReadInt16();
            disconnect = p.ReadInt32();
            medal = new stMedal
            {
                lucky = p.ReadInt32(),
                fast = p.ReadInt32(),
                best_drive = p.ReadInt32(),
                best_chipin = p.ReadInt32(),
                best_puttin = p.ReadInt32(),
                best_recovery = p.ReadInt32(),
            };
            sys_school_serie = p.ReadInt32();
            game_count_season = p.ReadInt32();
            _16bit_nao_sei = p.ReadInt16();
            return this;
        }
    }

    // Medal
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stMedal
    {
        public void add(stMedal _medal)
        {
            lucky += _medal.lucky;
            fast += _medal.fast;
            best_drive += _medal.best_drive;
            best_chipin += _medal.best_chipin;
            best_puttin += _medal.best_puttin;
            best_recovery += _medal.best_recovery;
        }

        public void add(uMedalWin _medal_win)
        {
            if (_medal_win.stMedal.lucky == 1)
                lucky++;
            else if (_medal_win.stMedal.speediest == 1)
                fast++;
            else if (_medal_win.stMedal.best_drive == 1)
                best_drive++;
            else if (_medal_win.stMedal.best_chipin == 1)
                best_chipin++;
            else if (_medal_win.stMedal.best_long_puttin == 1)
                best_puttin++;
            else if (_medal_win.stMedal.best_recovery == 1)
                best_recovery++;
        }

        public int lucky { get; set; }
        public int fast { get; set; }
        public int best_drive { get; set; }
        public int best_chipin { get; set; }
        public int best_puttin { get; set; }
        public int best_recovery { get; set; }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {

                p.Write(lucky);   // só tem no JP
                p.Write(fast);//state_flag.ucByte(falta saber das flags)
                p.Write(best_drive);     // 1 é primeira vez que logou, 2 já não é mais a primeira vez que fez login no server
                p.Write(best_chipin);
                p.Write(best_puttin);
                p.Write(best_recovery);
                return p.GetBytes;
            }
        }

    }

    public class CouponGacha
    {
        public int partial_ticket;
        public int normal_ticket;
    }


    // Counter Item Info
    public class CounterItemInfo
    {
        public CounterItemInfo() => clear();
        public CounterItemInfo(CounterItemInfo _ul)
        {
            this.active = _ul.active;
            this._typeid = _ul._typeid;
            this.id = _ul.id;
            this.value = _ul.value;
        }

        public virtual void clear()
        {
            this.active = 0;
            this._typeid = 0;
            this.id = 0;
            this.value = 0;
        }
        public bool isValid()
        {
            return (id > 0 && _typeid != 0);
        }
        public byte active;
        public uint _typeid = new uint();
        public int id = 0;
        public int value = 0;
    }

    // Quest Stuff Info
    public class QuestStuffInfo
    {
        public void clear()
        {
            id = 0;
            _typeid = new uint();
            counter_item_id = 0;
            clear_date_unix = new uint();
        }
        public bool isValid()
        {
            return (id > 0 && _typeid != 0);
        }
        public int id = 0;
        public uint _typeid = new uint();
        public int counter_item_id = 0;
        public uint clear_date_unix = new uint();
    }

    public class AchievementInfo
    {
        public enum ACHIEVEMENT_STATUS : byte
        {
            PENDENTING = 1,
            EXCLUEDED,
            ACTIVED,
            CONCLUEDED,
        }

        // Fields (Mantidos exatamente como no C++)
        public byte active;
        public uint _typeid;
        public int id;
        public int status; // Mantido como int para aceitar os valores 1, 2, 3, 4
        public Dictionary<int, CounterItemInfo> map_counter_item = new Dictionary<int, CounterItemInfo>();
        public List<QuestStuffInfo> v_qsi = new List<QuestStuffInfo>();

        public virtual void clear()
        {
            active = 0;
            _typeid = 0;
            id = 0;
            status = 0;

            v_qsi.Clear();
            map_counter_item.Clear();
        }

        public CounterItemInfo findCounterItemById(int _id)
        {
            if (_id < 0)
                throw new Exception("[AchievementInfo::findCounterItemById][Error] _id is invalid");

            return map_counter_item.TryGetValue(_id, out var cii) ? cii : null;
        }

        public CounterItemInfo findCounterItemByTypeId(uint _typeid)
        {
            if (_typeid == 0)
                throw new Exception("[AchievementInfo::findCounterItemByTypeid][Error] _typeid is invalid");

            return map_counter_item.Values.FirstOrDefault(item => item._typeid == _typeid);
        }

        public QuestStuffInfo findQuestStuffById(int _id)
        {
            if (_id < 0)
                throw new Exception("[AchievementInfo::findQuestStuffById][Error] _id is invalid");

            return v_qsi.FirstOrDefault(q => q.id == _id);
        }

        public QuestStuffInfo findQuestStuffByTypeId(uint _typeid)
        {
            if (_typeid == 0)
                throw new Exception("[AchievementInfo::findQuestStuffByTypeId][Error] _typeid is invalid");

            return v_qsi.FirstOrDefault(q => q._typeid == _typeid);
        }

        public uint addCounterByTypeId(uint _typeid, int _value)
        {
            if (_typeid == 0)
                throw new Exception("[AchievementInfo::addCounterByTypeId][Error] _typeid is invalid");

            uint count = 0;
            // Simula o std::map<int32_t, CounterItemInfo*> do C++
            var map_cii = new Dictionary<int, CounterItemInfo>();

            foreach (var el in v_qsi)
            {
                if (el.clear_date_unix == 0)
                {
                    CounterItemInfo cii = findCounterItemById(el.counter_item_id);
                    if (cii != null && cii._typeid == _typeid)
                        map_cii[cii.id] = cii;
                }
            }

            foreach (var it in map_cii)
            {
                it.Value.value += _value;
                count++;
            }

            return count;
        }

        public bool checkAllQuestClear()
        {
            if (v_qsi.Count == 0) return true;
            return v_qsi.All(el => el.clear_date_unix != 0);
        }
    }

    public class AchievementInfoEx : AchievementInfo
    {
        public uint quest_base_typeid;

        public AchievementInfoEx()
        {
            clear();
        }

        public override void clear()
        {
            base.clear();
            quest_base_typeid = 0;
        }

        // No C#, retornar o objeto QuestStuffInfo equivale a retornar o ponteiro/referência do item na lista
        public QuestStuffInfo getQuestBase()
        {
            if (quest_base_typeid == 0)
                return null;

            return v_qsi.FirstOrDefault(q => q._typeid == quest_base_typeid);
        }
    } 


    // Premium Ticket User
    public class PremiumTicket
    {
        public int id;
        public uint _typeid;
        public int unix_sec_date;
        public int unix_end_date;

        public void clear()
        {
            id = 0;
            _typeid = 0;
            unix_sec_date = 0;
            unix_end_date = 0;
        }
    }

    // Request Info
    public class RequestInfo
    {
        public uint uid;
        public byte season;
        public byte show;     // 12 pacotes enviados pode enviar o pacote089
    }
    // Itens Equipado do Player(nao é struct, é algo proprio)
    public class UserEquipedItem
    {
        public CharacterInfo char_info { get; set; }
        public CaddieInfoEx cad_info { get; set; }
        public MascotInfoEx mascot_info { get; set; }
        public ClubSetInfo csi { get; set; }
        public WarehouseItem clubset { get; set; }
        public WarehouseItem comet { get; set; }
        public UserEquipedItem()
        { clear(); }

        protected void clear() //init 
        {
            /// 628bytes
            char_info = new CharacterInfo();
            cad_info = new CaddieInfoEx();
            mascot_info = new MascotInfoEx();
            csi = new ClubSetInfo();
            /////
            comet = new WarehouseItem();
            clubset = new WarehouseItem();
        }


        /// <summary>
        /// Character(513 bytes), Caddie(25 bytes), ClubSet(28 bytes), Mascot(62 bytes), Total Size 628 
        /// </summary>
        /// <returns>Equiped Item(628 array of byte)</returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                //char info
                p.WriteBytes(char_info == null || char_info.id != 0 ? new byte[513] : char_info.ToArray());
                // caddie Info
                p.WriteBytes(cad_info == null || cad_info.id != 0 ? new byte[25] : cad_info.ToArray());
                // Club Set Info
                p.WriteBytes(csi == null || csi.id != 0 ? new byte[28] : csi.ToArray());
                // Mascot Info
                p.WriteBytes(mascot_info == null || mascot_info.id != 0 ? new byte[62] : mascot_info.ToArray());

                Debug.Assert((p.GetSize == 628), "EquipedItems::Build() is error");
                return p.GetBytes;
            }
        }

    }

    // Estado do Character no Lounge
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class StateCharacterLounge
    {
        public StateCharacterLounge()
            => clear();
        void clear()
        {
            camera_zoom = 1;
            scale_head = 1;
            walk_speed = 1;
            fUnknown = 1;
        }

        public float camera_zoom;  // Zoom da câmera
        public float scale_head;   // Tamanho da cabeça do character
        public float walk_speed;   // Velocidade que o player anda no lounge
        public float fUnknown;



        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteSingle(camera_zoom);  // Zoom da câmera
                p.WriteSingle(scale_head);   // Tamanho da cabeça do character
                p.WriteSingle(walk_speed);   // Velocidade que o player anda no lounge
                p.WriteSingle(fUnknown);
                return p.GetBytes;
            }
        }
    }

    // MyRoom Config
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MyRoomConfig
    {
        public short allow_enter;     // Se pode ou não entrar no My Room
        public byte public_lock;      // Se tem senha ou não
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string pass;//15]                  // Senha
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 90)]
        public byte[] ucUnknown90;//[90]  // Não o que é ainda
        public MyRoomConfig()
        {
            ucUnknown90 = new byte[90];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt16(allow_enter);
                p.WriteByte(public_lock);
                p.WriteStr(pass, 15);
                p.WriteBytes(ucUnknown90);
                return p.GetBytes;
            }
        }
    }

    // MyRoom Item
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MyRoomItem
    {
        public int id;
        public uint _typeid;
        public short number;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Location
        {
            public float x;
            public float y;
            public float z;
            public float r;

            public void SetLoc(PangyaAPI.IFF.JP.Models.Data.Furniture.Location location)
            {
                x = location.x;
                y = location.y;
                z = location.z;
                r = location.r; // face
            }
        }
        public MyRoomItem()
        {
            location = new Location();
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Location location;
        public byte equiped;     // Equipado ou não, 1 YES, 0 NO

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(id);
                p.Write(_typeid);
                p.Write(number);
                p.WriteSingle(location.x);
                p.WriteSingle(location.y);
                p.WriteSingle(location.z);
                p.WriteSingle(location.r);
                p.Write(equiped);
                return p.GetBytes;
            }
        }
    }

    // Dolfine Locker
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DolfiniLocker
    {
        public DolfiniLocker()
        {
            v_item = new List<DolfiniLockerItem>();
            pass = "";
        }
        void clear()
        {
            pang = 0;

            v_item.Clear();
        }
        public uint isLocker()
        {

            if (string.IsNullOrEmpty(pass))
                return 2;   // Senha não foi criada ainda
            else if (!locker && pass_check)
                return 76;// 1;	// Senha já foi verificada para essa session

            return 76;  // Senha ainda não foi verificada para essa session
        }
        public bool ownerItem(uint _typeid)
        {

            var it = v_item.Where(c => c.item._typeid == _typeid);


            return it.Any();
        }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string pass = "";//[7]
        public ulong pang;
        public bool locker;               // Essa opção tem que ser do gs para pedir para o player verificar a senha todas vez do locker
        public bool pass_check;  // 1 já foi verificado a senha nessa session, 0 ainda não foi verificada
        public List<DolfiniLockerItem> v_item;
    }
    // Dolfini Locker Item
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DolfiniLockerItem
    {
        public ulong index; // ID do item no dolfini Locker
        [field: MarshalAs(UnmanagedType.Struct)]
        public TradeItem item;

        public DolfiniLockerItem()
        {
            item = new TradeItem();
        }
        public DolfiniLockerItem(DolfiniLockerItem dolf)
        {
            index = dolf.index;
            item = new TradeItem(dolf.item);
        }

        public DolfiniLockerItem ToRead(packet r)
        {
            index = r.ReadUInt64();
            item = new TradeItem().ToRead(r);
            return this;
        }

    }

    // TradeItem
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TradeItem
    {
        public uint _typeid;
        public int id;
        public int qntd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] ucUnknown3 = new byte[3];
        public ulong pang;
        public uint upgrade_custo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] c = new ushort[5];
        public ushort usUnknown;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string sd_idx = "";
        public short sd_seq;
        public byte sd_status;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Card
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] character = new uint[4];

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] caddie = new uint[4];

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] NPC = new uint[4];

            public ushort character_slot_count;
            public ushort caddie_slot_count;
            public ushort NPC_slot_count;

            public void clear()
            {
                Array.Clear(character, 0, character.Length);
                Array.Clear(caddie, 0, caddie.Length);
                Array.Clear(NPC, 0, NPC.Length);
                character_slot_count = 0;
                caddie_slot_count = 0;
                NPC_slot_count = 0;
            }
        }

        [MarshalAs(UnmanagedType.Struct)]
        public Card card = new Card();

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        public string sd_name = "";

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string sd_copier_nick = "";

        public void clear()
        {
            _typeid = 0;
            id = -1;
            qntd = 0;
            Array.Clear(ucUnknown3, 0, ucUnknown3.Length);
            pang = 0;
            upgrade_custo = 0;
            Array.Clear(c, 0, c.Length);
            usUnknown = 0;
            sd_idx = "";
            sd_seq = 0;
            sd_status = 0;
            card.clear();
            sd_name = "";
            sd_copier_nick = "";
        }

        public TradeItem()
        {
            clear();
        }

        public TradeItem(TradeItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            clear();
            _typeid = item._typeid;
            id = item.id;
            qntd = item.qntd;

            Array.Copy(item.ucUnknown3, ucUnknown3, ucUnknown3.Length);

            pang = item.pang;
            upgrade_custo = item.upgrade_custo;

            Array.Copy(item.c, c, c.Length);

            usUnknown = item.usUnknown;
            sd_idx = string.Copy(item.sd_idx ?? "");
            sd_seq = item.sd_seq;
            sd_status = item.sd_status;

            // Card
            card = new Card
            {
                character = (uint[])item.card.character.Clone(),
                caddie = (uint[])item.card.caddie.Clone(),
                NPC = (uint[])item.card.NPC.Clone(),
                character_slot_count = item.card.character_slot_count,
                caddie_slot_count = item.card.caddie_slot_count,
                NPC_slot_count = item.card.NPC_slot_count
            };

            sd_name = item.sd_name;
            sd_copier_nick = item.sd_copier_nick;
        }


        /// <summary>
        /// return 168 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(_typeid);
                p.WriteInt32(id);
                p.WriteInt32(qntd);
                p.WriteBytes(ucUnknown3, 3);
                p.WriteUInt64(pang);
                p.WriteUInt32(upgrade_custo);
                p.WriteUInt16(c);//array[]
                p.WriteUInt16(usUnknown);
                p.WriteStr(sd_idx, 9);
                p.WriteInt16(sd_seq);
                p.WriteByte(sd_status);
                p.WriteUInt32(card.character);
                p.WriteUInt32(card.caddie);
                p.WriteUInt32(card.NPC);
                p.WriteUInt16(card.character_slot_count);
                p.WriteUInt16(card.caddie_slot_count);
                p.WriteUInt16(card.NPC_slot_count);
                p.WriteStr(sd_name, 41);
                p.WriteStr(sd_copier_nick, 22);
                return p.GetBytes;
            }
        }
        public TradeItem ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            qntd = r.ReadInt32();
            ucUnknown3 = r.ReadBytes(3);
            pang = r.ReadUInt64();
            upgrade_custo = r.ReadUInt32();

            c = new ushort[5];
            for (int i = 0; i < 5; i++)
                c[i] = r.ReadUInt16();

            usUnknown = r.ReadUInt16();
            sd_idx = r.ReadPStr(9);
            sd_seq = r.ReadInt16();
            sd_status = r.ReadByte();

            card = new Card();
            card.character = new uint[4];
            for (int i = 0; i < 4; i++)
                card.character[i] = r.ReadUInt32();

            card.caddie = new uint[4];
            for (int i = 0; i < 4; i++)
                card.caddie[i] = r.ReadUInt32();

            card.NPC = new uint[4];
            for (int i = 0; i < 4; i++)
                card.NPC[i] = r.ReadUInt32();

            card.character_slot_count = r.ReadUInt16();
            card.caddie_slot_count = r.ReadUInt16();
            card.NPC_slot_count = r.ReadUInt16();

            sd_name = r.ReadPStr(41);
            sd_copier_nick = r.ReadPStr(22);

            return this;
        }

    }


    // Item Generico

    public class stItem
    {
        public virtual void clear()
        {
            id = 0;
            _typeid = 0;
            type_iff = 0;
            type = 0;
            flag = 0;
            flag_time = 0;
            qntd = 0;
            name = "";
            icon = "";
            stat = new item_stat();
            ucc = new UCC();
            is_cash = 0;
            price = 0;
            desconto = 0;
            date = new stDate();
            date_reserve = 0;
        }

        public bool IsUCC()
        {
            return type_iff == (byte)PangyaAPI.IFF.JP.Models.Flags.PART_TYPE.UCC_DRAW_ONLY || type_iff == (byte)PangyaAPI.IFF.JP.Models.Flags.PART_TYPE.UCC_COPY_ONLY;
        }

        public int id = new int();
        public uint _typeid = new uint();

        public byte type_iff; // Tipo que está no iff structure, tipo no Part.iff, 1 parte de baixo da roupa, 3 luva, 8 e 9 UCC etc
        public byte type; // 2 Normal Item
        public byte flag; // 1 Padrão item fornecido pelo server, 5 UCC_BLANK
        public byte flag_time; // 6 rental(dia), 2 hora(acho), 4 minuto(acho)
        public int qntd = new int();
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string name = new string(new char[64]);
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        public string icon = new string(new char[41]);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class item_stat
        {
            public void clear()
            {
                qntd_ant = 0;
                qntd_dep = 0;
            }
            public int qntd_ant = new int();
            public int qntd_dep = new int();

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.Write(qntd_ant);
                    p.Write(qntd_dep);
                    return p.GetBytes;
                }
            }
        }

        [field: MarshalAs(UnmanagedType.Struct)]
        public item_stat stat = new item_stat();
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UCC
        {
            public void clear()
            {
                IDX = "";
                status = 0;
                seq = 0;
            }
            [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
            public string IDX; // UCC INDEX STRING
            public uint status = new uint();
            public uint seq = new uint();
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public UCC ucc = new UCC();
        public byte is_cash = 0;
        public uint price = new uint();
        public uint desconto = new uint();
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class stDate
        {
            public stDate()
            {
                clear();
            }
            public stDate(int index, stDateSys st)
            {
                clear();
                date.sysDate = st.sysDate;
            }
            public void clear()
            {
                active = 0;
                date.clear();
            }
            public uint active = 1; // 1 Actived, 0 Desatived
            public class stDateSys
            {
                public void clear()
                {
                    sysDate = new SYSTEMTIME[] { new SYSTEMTIME(), new SYSTEMTIME() };
                }
                public stDateSys()
                {
                    clear();
                }

                public stDateSys(SYSTEMTIME start, SYSTEMTIME end)
                {
                    clear();
                    sysDate[0].SetInfo(start);
                    sysDate[1].SetInfo(end);
                }
                [field: MarshalAs(UnmanagedType.ByValArray)]
                public SYSTEMTIME[] sysDate = new SYSTEMTIME[] { new SYSTEMTIME(), new SYSTEMTIME() }; // 0 Begin, 1 End
            }
            public stDateSys date = new stDateSys();
        }

        public stDate date = new stDate();
        public ushort date_reserve;

        public short[] c = new short[5];
        public stItem() { }
        public stItem(stItem _item)
        {
            id = _item.id;
            _typeid = _item._typeid;
            type_iff = _item.type_iff;
            type = _item.type;
            flag = _item.flag;
            flag_time = _item.flag_time;
            qntd = _item.qntd;
            name = _item.name;
            icon = _item.icon;

            stat = new item_stat
            {
                qntd_ant = _item.stat.qntd_ant,
                qntd_dep = _item.stat.qntd_dep
            };

            ucc = new UCC
            {
                IDX = _item.ucc.IDX,
                status = _item.ucc.status,
                seq = _item.ucc.seq
            };

            is_cash = _item.is_cash;
            price = _item.price;
            desconto = _item.desconto;

            date = new stDate
            {
                active = _item.date.active,
                date = new stDate.stDateSys
                {
                    sysDate = new SYSTEMTIME[2]
                    {
                _item.date.date.sysDate[0], // Se PangyaTime for struct, isso é seguro
                _item.date.date.sysDate[1]
                    }
                }
            };

            date_reserve = _item.date_reserve;

            c = new short[_item.c.Length];
            Array.Copy(_item.c, c, _item.c.Length);
        }
        public int STDA_C_ITEM_QNTD
        {
            get => c[0] == 32767 ? -1 : c[0];
            set => c[0] = (short)(value == -1 ? 32767 : value);
        }

        public short STDA_C_ITEM_TICKET_REPORT_ID_HIGH
        {
            get => c[1];
            set => c[1] = value;
        }

        public short STDA_C_ITEM_TICKET_REPORT_ID_LOW
        {
            get => c[2];
            set => c[2] = value;
        }

        public int STDA_C_ITEM_TIME
        {
            get => c[3] == 32767 ? -1 : c[3];
            set => c[3] = (short)(value == -1 ? 32767 : value);
        }

    }

    public class PlayerAssistent
    {
        public uint _typeid { get => DefineConstants.ASSIST_ITEM_TYPEID; }
        public int id { get; set; }
        public bool assist_flag { get; set; }
    }

    // stItem Extended
    public class stItemEx : stItem
    {
        public stItemEx(uint _ul = 0u)
        {
            clear();
        }
        public stItemEx(stItemEx _item)
        {
            id = _item.id;
            _typeid = _item._typeid;
            type_iff = _item.type_iff;
            type = _item.type;
            flag = _item.flag;
            flag_time = _item.flag_time;
            qntd = _item.qntd;
            name = string.Copy(_item.name);
            icon = string.Copy(_item.icon);

            stat = new item_stat
            {
                qntd_ant = _item.stat.qntd_ant,
                qntd_dep = _item.stat.qntd_dep
            };

            ucc = new UCC
            {
                IDX = _item.ucc.IDX,
                status = _item.ucc.status,
                seq = _item.ucc.seq
            };

            is_cash = _item.is_cash;
            price = _item.price;
            desconto = _item.desconto;

            date = new stDate
            {
                active = _item.date.active,
                date = new stDate.stDateSys
                {
                    sysDate = new SYSTEMTIME[2]
                    {
                _item.date.date.sysDate[0], // Se PangyaTime for struct, isso é seguro
                _item.date.date.sysDate[1]
                    }
                }
            };

            date_reserve = _item.date_reserve;

            c = new short[_item.c.Length];
            Array.Copy(_item.c, c, _item.c.Length);

            clubset_workshop = _item.clubset_workshop;

        }

        public stItemEx(stItem _item) : base(_item)
        {

        }

        public override void clear()
        {
            base.clear();//é ncessario chamar ele
            clubset_workshop = new ClubSetWorkshop();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ClubSetWorkshop
        {
            public void clear()
            {
            }
            public short[] c = new short[5];
            public uint mastery = new uint();
            public byte level;
            public uint rank = new uint();
            public uint recovery = new uint();
            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteInt16(c);
                    p.WriteUInt32(mastery);
                    p.WriteByte(level);
                    p.WriteUInt32(rank);
                    p.WriteUInt32(recovery);
                    return p.GetBytes;
                }
            }
        }
        public ClubSetWorkshop clubset_workshop = new ClubSetWorkshop();
    }

    /**** Base Item do pacote 0x216 Update Item No Game
	**/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stItem216
    {
        public byte type;
        public uint _typeid;
        public uint id;
        public uint flag_time;
        public uint qntd_ant;
        public uint qntd_dep;
        public uint qntd;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public short[] c; //5
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string ucc_idx; //9
        public byte seq;  // ou stats
        public uint card_typeid;
        public byte card_slot;
        public stItem216()
        {
            c = new short[5];
            ucc_idx = "";
        }
    }

    // Friend Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class FriendInfo
    {
        public uint uid;
        public byte sex;  // gender, genero, sexo, 0 masculino, 1 Feminino
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string id;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string nickname;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string apelido;
    }

    // Daily Quest Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DailyQuestInfo
    {
        public DailyQuestInfo()
        {
            clear();
        }
        public DailyQuestInfo(int _typeid_0, uint _typeid_1, uint _typeid_2, SYSTEMTIME _st)
        {
            date = _st;
            _typeid = new uint[3] { (uint)_typeid_1, (uint)_typeid_2, (uint)_typeid_2 };

        }

        public void clear()
        {
            date = new SYSTEMTIME();
            _typeid = new uint[3];
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public SYSTEMTIME date;            // System Time Windows
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] _typeid;// [3];    // array[3] Typeid da Quest
        public override string ToString()
        {
            return "QUEST_TYPEID_0=" + (_typeid[0]) + ", QUEST_TYPEID_1=" + (_typeid[1]) + ", QUEST_TYPEID_2="
                    + (_typeid[2]) + ", UPDATE_DATE=" + date.ConvertTime();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DailyQuestInfoUser
    {
        public uint now_date;      // Data que a quest está (current quest), do sistema de daily quest
        public uint accept_date;   // Data da última quest que foi aceita
        public uint current_date;  // Data que a quest está (current quest), do player
        public uint count;         // Número de quests do dia
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] _typeid;     // Máximo de 3 quests por dia

        public DailyQuestInfoUser()
        {
            _typeid = new uint[3];
        }

        public DailyQuestInfoUser(DailyQuestInfoUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            now_date = user.now_date;
            accept_date = user.accept_date;
            current_date = user.current_date;
            count = user.count;
            _typeid = (uint[])user._typeid.Clone(); // Clonar o array para evitar referências compartilhadas
        }

        public DailyQuestInfoUser(uint initialValue)
        {
            now_date = initialValue;
            accept_date = initialValue;
            current_date = initialValue;
            count = initialValue;
            _typeid = new uint[3];
        }
    }

    // Remove Daily Quest
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RemoveDailyQuestUser
    {
        public int id;
        public uint _typeid;
    }

    // Add DailyQuest
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AddDailyQuestUser
    {
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string name;// [64];
        public uint _typeid;
        public uint quest_typeid;
        public uint status;
    }

    // Player Canal Info

    // Player Canal Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayerLobbyInfo
    {
        public PlayerLobbyInfo()
        {
            clear();
        }
        public void clear()
        {
            sala_numero = ushort.MaxValue;
            capability = new uCapability();
            state_flag = new uStateFlag();
            guild_mark_img = "";
            sDisplayID_Bytes = new byte[128];// [18];                // Acho
            nickname_bytes = new byte[22];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                int capabilityValue = (flag_visible_gm == 0) ? 0 : capability.ulCapability;

                p.WriteUInt32(uid);
                p.WriteInt32(oid);
                p.WriteUInt16(sala_numero);
                p.WriteStr(nickname, 22);
                p.WriteByte(level);
                p.WriteInt32(capabilityValue);
                p.WriteUInt32(title);
                p.WriteUInt32(ladder_point);
                p.WriteByte(state_flag.ucByte);
                p.WriteInt32(guild_uid);
                p.WriteUInt32(guild_index_mark);
                p.WriteStr(guild_mark_img, 12);
                p.WriteInt16(flag_visible_gm);
                p.WriteUInt32(channeling_flag);
                p.WriteStr(sDisplayID, 128);             // S4 TH
                return p.GetBytes;
            }
        }

        public uint uid;
        public int oid;
        public ushort sala_numero;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        private byte[] nickname_bytes;
        public string nickname { get => nickname_bytes.GetString(); set => nickname_bytes.SetString(value); }
        public byte level;
        public uCapability capability;
        public uint title;
        public uint ladder_point;             // Acho que é o team point  
        [field: MarshalAs(UnmanagedType.Struct)]
        public uStateFlag state_flag;
        public int guild_uid;
        public uint guild_index_mark;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string guild_mark_img;
        public short flag_visible_gm;//th é vip                         
        public uint channeling_flag;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] sDisplayID_Bytes;
        public string sDisplayID { get => sDisplayID_Bytes.GetString(); set => sDisplayID_Bytes.SetString(value); }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class uStateFlag
        {
            public byte ucByte;
            public uStateFlag()
            {
                ucByte = 0;
            }
            public uStateFlag(byte op)
            {
                ucByte = op;
            }
            // Bit 0 - AFK
            public byte away
            {
                get => (byte)((ucByte >> 0) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 0)) | ((value & 1) << 0));
            }

            // Bit 1 - Gênero
            public byte sexo
            {
                get => (byte)((ucByte >> 1) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 1)) | ((value & 1) << 1));
            }

            // Bit 2 - Quit rate > 31% e < 41%
            public byte quiter_1
            {
                get => (byte)((ucByte >> 2) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 2)) | ((value & 1) << 2));
            }

            // Bit 3 - Quit rate > 41%
            public byte quiter_2
            {
                get => (byte)((ucByte >> 3) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 3)) | ((value & 1) << 3));
            }

            // Bit 4 - Quit rate < 3% (Azinha)
            public byte azinha
            {
                get => (byte)((ucByte >> 4) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 4)) | ((value & 1) << 4));
            }

            // Bit 5 - Angel Wings
            public byte icon_angel
            {
                get => (byte)((ucByte >> 5) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 5)) | ((value & 1) << 5));
            }

            // Bit 6 - Unknown
            public byte ucUnknown_bit7
            {
                get => (byte)((ucByte >> 6) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 6)) | ((value & 1) << 6));
            }

            // Bit 7 - Unknown
            public byte ucUnknown_bit8
            {
                get => (byte)((ucByte >> 7) & 1);
                set => ucByte = (byte)((ucByte & ~(1 << 7)) | ((value & 1) << 7));
            }
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]//348
    public class PlayerRoomInfo
    {
        public PlayerRoomInfo()
        {
            clear();
        }
        protected void clear()
        {
            state_action = new StateAction();
            place = new PlayerPlace(0x0A);
            ucUnknown3 = new byte[3];
            capability = new uCapability();
            state_flag = new StateFlag();
            skin = new uint[6];
            location = new stLocation();
            shop = new PersonShop();
            flag_item_boost = new uItemBoost();
            nick_NT_bytes = new byte[128];
            guild_mark_img = "";
            nickname = "";
            convidado = 0;
            guild_name = "";
        }
        public int oid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string nickname;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string guild_name;
        public byte position;
        [field: MarshalAs(UnmanagedType.Struct)]
        public uCapability capability;
        public uint title;
        public uint char_typeid;       // Character Typeid
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] skin;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class StateFlag
        {
            public ushort usFlag = 0;

            public StateFlag()
            {
                clear();
            }

            public void clear()
            {
                usFlag = 0;
            }
            public byte team
            {
                get => (byte)((usFlag & (1 << 0)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 0);
                    else
                        usFlag &= unchecked((ushort)~(1 << 0));
                }
            }

            public byte team2
            {
                get => (byte)((usFlag & (1 << 1)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 1);
                    else
                        usFlag &= unchecked((ushort)~(1 << 1));
                }
            }

            public byte away
            {
                get => (byte)((usFlag & (1 << 2)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 2);
                    else
                        usFlag &= unchecked((ushort)~(1 << 2));
                }
            }

            public byte master
            {
                get => (byte)((usFlag & (1 << 3)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 3);
                    else
                        usFlag &= unchecked((ushort)~(1 << 3));
                }
            }

            public byte master2
            {
                get => (byte)((usFlag & (1 << 4)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 4);
                    else
                        usFlag &= unchecked((ushort)~(1 << 4));
                }
            }

            public byte sexo
            {
                get => (byte)((usFlag & (1 << 5)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 5);
                    else
                        usFlag &= unchecked((ushort)~(1 << 5));
                }
            }

            public byte quiter_1
            {
                get => (byte)((usFlag & (1 << 6)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 6);
                    else
                        usFlag &= unchecked((ushort)~(1 << 6));
                }
            }

            public byte quiter_2
            {
                get => (byte)((usFlag & (1 << 7)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 7);
                    else
                        usFlag &= unchecked((ushort)~(1 << 7));
                }
            }

            public byte azinha
            {
                get => (byte)((usFlag & (1 << 8)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 8);
                    else
                        usFlag &= unchecked((ushort)~(1 << 8));
                }
            }

            public byte ready
            {
                get => (byte)((usFlag & (1 << 9)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 9);
                    else
                        usFlag &= unchecked((ushort)~(1 << 9));
                }
            }

            public byte unknown_bit11
            {
                get => (byte)((usFlag & (1 << 10)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 10);
                    else
                        usFlag &= unchecked((ushort)~(1 << 10));
                }
            }

            public byte unknown_bit12
            {
                get => (byte)((usFlag & (1 << 11)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 11);
                    else
                        usFlag &= unchecked((ushort)~(1 << 11));
                }
            }

            public byte unknown_bit13
            {
                get => (byte)((usFlag & (1 << 12)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 12);
                    else
                        usFlag &= unchecked((ushort)~(1 << 12));
                }
            }

            public byte unknown_bit14
            {
                get => (byte)((usFlag & (1 << 13)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 13);
                    else
                        usFlag &= unchecked((ushort)~(1 << 13));
                }
            }

            public byte unknown_bit15
            {
                get => (byte)((usFlag & (1 << 14)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 14);
                    else
                        usFlag &= unchecked((ushort)~(1 << 14));
                }
            }

            public byte unknown_bit16
            {
                get => (byte)((usFlag & (1 << 15)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        usFlag |= (1 << 15);
                    else
                        usFlag &= unchecked((ushort)~(1 << 15));
                }
            }
        }

        [field: MarshalAs(UnmanagedType.Struct)]
        public StateFlag state_flag;//2 bytes
        public byte level;
        public byte icon_angel;
        [field: MarshalAs(UnmanagedType.Struct)]
        public PlayerPlace place;         // Tem o valor 0x0A aqui quase sempre das vezes que vi esse pacote, Pode ser o Place(lugar que o player está) tipo Room = 10(hex:0x0A)
        public int guild_uid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string guild_mark_img;
        public uint guild_mark_index;
        public uint uid;
        [field: MarshalAs(UnmanagedType.Struct)]
        public StateAction state_action;
        //---------Action
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class StateAction
        {
            public uint state_lounge;//animate
            public short usUnknown_flg;//Unknown1	// Acho que seja uma type tbm
            public uint state;//Posture	// Acho que seja estado de "lugar" pelo que lembro
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class stLocation
        {
            // Corrigindo o operador de soma
            public static stLocation operator +(stLocation a, stLocation _add_location)
            {
                return new stLocation()
                {
                    x = a.x += _add_location.x,
                    z = a.z += _add_location.z,
                    r = a.r += _add_location.r
                };
            }
            public static stLocation operator -(stLocation a, stLocation _add_location)
            {
                return new stLocation()
                {
                    x = a.x -= _add_location.x,
                    z = a.z -= _add_location.z,
                    r = a.r -= _add_location.r
                };
            }

            public float x;
            public float z;
            public float r;

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteFloat(x);
                    p.WriteFloat(z);
                    p.WriteFloat(r);
                    return p.GetBytes;
                }
            }

            public stLocation ToRead(packet _r)
            {
                x = _r.ReadFloat();
                z = _r.ReadFloat();
                r = _r.ReadFloat();
                return this;
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public stLocation location;
        //----------
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class PersonShop//deve ter mais coisas aqui....
        {
            public uint active;
            [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string name;//64
            public PersonShop()
            {
                active = 0;
                name = "";
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public PersonShop shop;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class uItemBoost
        {
            public ushort ulItemBoost;
            public byte ucPangMastery
            {
                get => (byte)(ulItemBoost & 1);
                set
                {
                    if (value != 0)
                        ulItemBoost |= 1;
                    else
                        ulItemBoost &= 0xFFFE; // ~(1 << 0)
                }
            }

            public byte ucPangNitro
            {
                get => (byte)((ulItemBoost >> 1) & 1);
                set
                {
                    if (value != 0)
                        ulItemBoost |= 1 << 1;
                    else
                        ulItemBoost &= 0xFFFD; // ~(1 << 1)
                }
            }

            public uItemBoost()
            {
                ulItemBoost = 0;
            }
        }
        public uint mascot_typeid;
        [field: MarshalAs(UnmanagedType.Struct)]
        public uItemBoost flag_item_boost;// Boost EXP, Pang e etc(2 bytes)
        public uint channeling_flag;// Pode ser a type de teasure do player, ou de drop item
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private byte[] nick_NT_bytes;
        public string sDisplayID
        {
            get => nick_NT_bytes.GetString();
            set => nick_NT_bytes.SetString(value);
        }
        public byte convidado;   // Flag Convidado, [Não sei bem por que os que entra na sala normal tem valor igual aqui, já que é type de convidado waiting]
        public float avg_score;// Media score "media de holes feito pelo player"
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] ucUnknown3;// Não sei mas sempre é 0 depois do media score(66 no th)  

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(oid);
                p.WriteStr(nickname, 22);
                p.WriteStr(guild_name, 20);
                p.WriteByte(position);
                p.Write(capability.ulCapability);
                p.WriteUInt32(title);
                p.WriteUInt32(char_typeid);
                p.WriteUInt32(skin);//array 
                p.Write(state_flag.usFlag);
                p.WriteByte(level);
                p.WriteByte(icon_angel);//1 fica angel, se 0 fica outra coisa...
                p.WriteByte(place.ulPlace);// angel effect no s4, Tem o valor 0x0A aqui quase sempre das vezes que vi esse pacote, Pode ser o Place(lugar que o player está) tipo Room = 10(hex:0x0A)
                p.WriteInt32(guild_uid);
                p.WriteStr(guild_mark_img, 12); //[12]);
                p.WriteUInt32(guild_mark_index);
                p.WriteUInt32(uid);
                //---------Action
                p.Write(state_action.state_lounge);
                p.Write(state_action.usUnknown_flg);
                p.Write(state_action.state);
                p.Write(location.x);
                p.Write(location.z);
                p.Write(location.r);
                //------------------ SHOP 
                p.Write(shop.active);
                p.WriteStr(shop.name, 64);//aqui tem mais info, precisa testar o tamanho maximo do nome do shop.
                //----------------------
                p.WriteUInt32(mascot_typeid);
                p.Write(flag_item_boost.ulItemBoost);
                p.WriteUInt32(channeling_flag);// Pode ser a type de teasure do player, ou de drop item
                p.WriteStr(sDisplayID, 128);//[22] Acho que seja o ID na ntreev do player, a empresa que mantêm as contas, no JP era o gamepot
                p.WriteByte(convidado);   // Flag Convidado, [Não sei bem por que os que entra na sala normal tem valor igual aqui, já que é type de convidado waiting]
                p.WriteSingle(avg_score);// Media score "media de holes feito pelo player"
                p.WriteBytes(ucUnknown3);// Não sei mas sempre é 0 depois do media score(66 no th)
                Debug.Assert(!(p.GetSize != 348), "PlayerRoomInfo::ToArray() is error");
                return p.GetBytes;
            }
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayerPlace
    {
        public byte ulPlace; // armazena o estado das flags

        public PlayerPlace(byte ul = 0)
        {
            ulPlace = 0; // Começa sem flags ativas
        }

        public bool None
        {
            get => ulPlace == 0;
        }

        // Propriedade para o "MainLobby" (primeiro bit)
        public bool main_lobby /// ulPlace for igual a 1
        {
            get => ((PlaceFlags)ulPlace).HasFlag(PlaceFlags.MainLobby); // Verifica se o bit 1 está ativado
            set
            {
                if (value) ulPlace |= (byte)PlaceFlags.MainLobby; // Ativa o bit 0
                else ulPlace &= (byte)~PlaceFlags.MainLobby; // Desativa o bit 0
            }
        }

        // Propriedade para o "MiniGame" (segundo bit)
        public bool web_link_or_my_room
        {
            get => ((PlaceFlags)ulPlace).HasFlag(PlaceFlags.WebLinkOrMyRoom); // Verifica se o bit 1 está ativado
            set
            {
                if (value) ulPlace |= (byte)PlaceFlags.WebLinkOrMyRoom; // Ativa o bit 1
                else ulPlace &= (byte)~PlaceFlags.WebLinkOrMyRoom; // Desativa o bit 1
            }
        }

        // Propriedade para o "PlayRoom" (segundo e quarto bit)
        public bool game_play
        {
            get => ((PlaceFlags)ulPlace).HasFlag(PlaceFlags.GamePlay); // Verifica se o bit 1 está ativado
            set
            {
                if (value) ulPlace |= (byte)PlaceFlags.GamePlay; // Ativa os bits 1 e 3
                else ulPlace &= (byte)~PlaceFlags.GamePlay; // Desativa os bits 1 e 3
            }
        }
    }
    // Player Room Info Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]//861
    public class PlayerRoomInfoEx : PlayerRoomInfo
    {
        public PlayerRoomInfoEx()
        {
            clear();
            ci = new CharacterInfo();
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public CharacterInfo ci { get; set; }

        public byte[] ToArrayEx()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteBytes(ToArray());
                p.WriteBytes(ci.ToArray());
                Debug.Assert(!(p.GetSize != 861), "PlayerRoomInfoEx::BuildEx is error");
                return p.GetBytes;
            }
        }
    }


    // Treasure Hunter Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TreasureHunterInfo
    {
        public TreasureHunterInfo()
        { clear(); }
        public void clear()
        {
            course = 0;
            point = 0;
        }

        public sbyte course;
        public int point;


        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteSByte(course);
                p.WriteInt32(point);
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TreasureHunterItem
    {
        public TreasureHunterItem()
        { }

        public TreasureHunterItem(TreasureHunterItem item)
        {
            _typeid = item._typeid;
            qntd = item.qntd;
            probabilidade = item.probabilidade;
            flag = item.flag;
            active = item.active;
        }

        public uint _typeid;
        public uint qntd;
        public uint probabilidade;
        public byte flag;
        public byte active = 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CadieExchangeItem
    {
        public uint _typeid { get; set; }
        public int id { get; set; }

        public uint QtyPerExchange;

        public CadieExchangeItem ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            return this;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial class CardEquip
    {
        public uint char_typeid { get; set; }
        public int char_id { get; set; }
        public uint card_typeid { get; set; }
        public int card_id { get; set; }
        public uint char_card_slot { get; set; }

        public CardEquip ToRead(packet r)
        {
            char_typeid = r.ReadUInt32();
            char_id = r.ReadInt32();
            card_typeid = r.ReadUInt32();
            card_id = r.ReadInt32();
            char_card_slot = r.ReadUInt32();

            return this;
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CardRemove
    {
        public uint char_typeid { get; set; }
        public int char_id { get; set; }
        public uint removedor_typeid { get; set; }
        public int removedor_id { get; set; }
        public uint card_slot { get; set; }

        public CardRemove ToRead(packet r)
        {
            char_typeid = r.ReadUInt32();
            char_id = r.ReadInt32();
            removedor_typeid = r.ReadUInt32();
            removedor_id = r.ReadInt32();
            card_slot = r.ReadUInt32();

            return this;
        }

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TikiShopExchangeItem
    {
        public uint _typeid { get; set; }
        public int id { get; set; }
        public uint qntd { get; set; }

        public TikiShopExchangeItem ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            qntd = r.ReadUInt32();
            return this;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ClubSetWorkShopTransferMasteryPts
    {
        public uint UCIM_chip_typeid { get; set; } // UCIM chip typeid
        public int[] clubset { get; set; } = new int[2]; // src[0], dst[1] ID do ClubSet
        public uint qntd { get; set; } // Qntd de troca

        public ClubSetWorkShopTransferMasteryPts ToRead(packet r)
        {
            UCIM_chip_typeid = r.ReadUInt32();

            clubset = new int[2];
            clubset[0] = r.ReadInt32(); // src
            clubset[1] = r.ReadInt32(); // dst

            qntd = r.ReadUInt32();

            return this;
        }

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CWUpLevel
    {
        public uint item_typeid { get; set; }
        public ushort qntd { get; set; }
        public int clubset_id { get; set; }
        public CWUpLevel ToRead(packet r)
        {
            item_typeid = r.ReadUInt32();
            qntd = r.ReadUInt16();
            clubset_id = r.ReadInt32();

            return this;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ProbCardExtra
    {
        public byte active;
        public byte stat { get; set; } // PWR, CTRL, ACCRY, SPIN, CURVE
        public uint prob { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CWUpRank
    {
        public uint item_typeid { get; set; }
        public ushort qntd { get; set; }
        public int clubset_id { get; set; }
        public CWUpRank ToRead(packet r)
        {
            item_typeid = r.ReadUInt32();
            qntd = r.ReadUInt16();
            clubset_id = r.ReadInt32();

            return this;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stLegacyTikiShopExchangeItem
    {
        public uint _typeid { get; set; }
        public int id { get; set; }
        public int qntd { get; set; }
        public uint value_tp { get; set; }

        public stLegacyTikiShopExchangeItem ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            qntd = r.ReadInt32();
            value_tp = r.ReadUInt32();

            return this;
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stLegacyTikiShopExchangeTP
    {
        public uint _typeid { get; set; }
        public int qntd { get; set; }
        public uint tp { get; set; }

        public stLegacyTikiShopExchangeTP ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            qntd = r.ReadInt32();
            tp = r.ReadUInt32();

            return this;
        }

    }
    // SalaInfo
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RoomInfo
    {
        public enum ROOM_INFO_COURSE : byte
        {
            BLUE_LAGOON,
            BLUE_WATER,
            SEPIA_WIND,
            WIND_HILL,
            WIZ_WIZ,
            WEST_WIZ,
            BLUE_MOON,
            SILVIA_CANNON,
            ICE_CANNON,
            WHITE_WIZ,
            SHINNING_SAND,
            PINK_WIND,
            NEW_MAP,//not exist
            DEEP_INFERNO,
            ICE_SPA,
            LOST_SEAWAY,
            EASTERN_VALLEY,
            CHRONICLE_1_CHAOS,
            ICE_INFERNO,
            WIZ_CITY,
            ABBOT_MINE,
            MYSTIC_RUINS,
            GRAND_ZODIAC = 64,
            RANDOM = 127,
            UNK = 0x7F
        }

        public enum ROOM_INFO_TYPE : byte
        {
            STROKE,
            MATCH,
            LOUNGE,
            GAME_TYPE,
            TOURNEY,
            TOURNEY_TEAM,
            GUILD_BATTLE,
            PANG_BATTLE,
            GAME_TYPE_08,
            GAME_TYPE_09,//
            APPROCH,
            GRAND_ZODIAC_INT,// GM_EVENT = 0x0B,
            GAME_TYPE_12,
            GRAND_ZODIAC_ADV,
            GRAND_ZODIAC_PRACTICE,
            GAME_TYPE_15,
            GAME_TYPE_16,
            GAME_TYPE_17,
            SPECIAL_SHUFFLE_COURSE,
            PRACTICE,
            GRAND_PRIX,
        }
        public enum ROOM_INFO_MODO : byte
        {
            M_FRONT,
            M_BACK,
            M_RANDOM,
            M_SHUFFLE,
            M_REPEAT,
            M_SHUFFLE_COURSE,
        }
        public enum ROOM_INFO_CHANGE : uint
        {
            NAME,
            SENHA,
            TIPO,
            COURSE,
            QNTD_HOLE,
            MODO,
            TEMPO_VS,
            MAX_PLAYER,
            TEMPO_30S,
            STATE_FLAG,
            GALLERY_LIMIT,
            HOLE_REPEAT,
            FIXED_HOLE,
            ARTEFATO,
            NATURAL,
        }
        public RoomInfo()
        {
            clear();
        }
        public void clear()
        {
            name = "";
            senha_flag = 1;
            state = 1;
            flag = 0;
            max_player = 0;
            num_player = 0;
            key = new byte[16];
            gallery_limit = 30;
            qntd_hole = 0;
            tipo_show = 0;
            numero = ushort.MaxValue;
            modo = 0;
            course = ROOM_INFO_COURSE.BLUE_LAGOON;
            time_vs = 0;
            time_30s = 0;
            trofel = 0;
            state_flag = 0;
            guilds = new RoomGuildInfo();
            rate_pang = 0;
            rate_exp = 0;
            flag_gm = 0;
            master = 0;
            type_extend = 0;
            typeid_artefatic = 0;
            special_flag_mod = new SpecialModeFlag();
            grand_prix = new RoomGrandPrixInfo();
        }

        public Guid roomId = Guid.Empty;     // ID da sala
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public byte[] snome = new byte[64];
        public string name { get => snome.GetString(); set => snome.SetString(value); }// [64];
        public byte senha_flag;  // Sala sem senha = 1, Sala com senha = 0
        public byte state;       // Sala em espera = 1, Sala em Game = 0
        public byte flag;                 // Sala que pode entrar depois que começou = 1
        public byte max_player;
        public byte num_player;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]//talvez seja 16
        public byte[] key;
        public byte gallery_num;//s4
        public byte gallery_limit;                 // Modo Multiplayer do pangya acho, sempre 0x1E (dec: 30) no pangya
        public byte qntd_hole;
        public byte tipo_show;            // esse é o tipo que mostra no pacote, esse pode mudar dependendo do tipo real da sala, fica ou camp, ou VS ou especial, não coloca todos os tipos aqui
        public ushort numero;
        public byte modo;
        [field: MarshalAs(UnmanagedType.U1)]
        public ROOM_INFO_COURSE course;
        public uint time_vs;
        public uint time_30s;
        public uint trofel;
        public short state_flag;          // Quando é sala de 100 player o mais de gm event aqui é 0x100
        [field: MarshalAs(UnmanagedType.Struct)]
        public RoomGuildInfo guilds;
        public uint rate_pang;
        public uint rate_exp;
        public byte flag_gm;
        public int master;         // Tem valores negativos, por que a sala usa ele para grand prix e etc
        public byte type_extend;          // tipo extended, que fala o tipo da sala certinho
        public uint typeid_artefatic;          // Aqui usa pra GP efeitos especiais do GP 
        [field: MarshalAs(UnmanagedType.Struct)]
        public SpecialModeFlag special_flag_mod;       // Aqui usa para Short Game Também
        [field: MarshalAs(UnmanagedType.Struct)]
        public RoomGrandPrixInfo grand_prix;

        public ROOM_INFO_MODO getModo()
        {
            return (ROOM_INFO_MODO)modo;
        }
        public byte getMap()
        {
            return Convert.ToByte(course & RoomInfo.ROOM_INFO_COURSE.UNK);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("RoomInfo {");
            sb.AppendLine($"  nome = \"{name}\"");
            sb.AppendLine($"  senha_flag = {senha_flag} ({(senha_flag == 1 ? "Sem Senha" : "Com Senha")})");
            sb.AppendLine($"  state = {state} ({(state == 1 ? "Espera" : "Em Jogo")})");
            sb.AppendLine($"  type = {flag}");
            sb.AppendLine($"  max_users = {max_player}");
            sb.AppendLine($"  players = {num_player}");
            sb.AppendLine($"  key = {BitConverter.ToString(key)}");
            sb.AppendLine($"  gallery_num = {gallery_num}");
            sb.AppendLine($"  gallery_limit = {gallery_limit}");
            sb.AppendLine($"  qntd_hole = {qntd_hole}");
            sb.AppendLine($"  tipo_show = {tipo_show}");
            sb.AppendLine($"  numero = {numero}");
            sb.AppendLine($"  modo = {modo} ({getModo()})");
            sb.AppendLine($"  course = {(byte)course} ({course})");
            sb.AppendLine($"  time_vs = {time_vs}");
            sb.AppendLine($"  time_30s = {time_30s}");
            sb.AppendLine($"  trofel = {trofel}");
            sb.AppendLine($"  state_flag = {state_flag}");
            sb.AppendLine($"  guilds = {guilds}");
            sb.AppendLine($"  rate_pang = {rate_pang}");
            sb.AppendLine($"  rate_exp = {rate_exp}");
            sb.AppendLine($"  flag_gm = {flag_gm}");
            sb.AppendLine($"  master = {master}");
            sb.AppendLine($"  tipo_ex = {type_extend}");
            sb.AppendLine($"  artefato = {typeid_artefatic}");
            sb.AppendLine($"  natural = {special_flag_mod}");
            sb.AppendLine($"  grand_prix = {grand_prix}");
            sb.Append("}");

            return sb.ToString();
        }

    }

    //    // Sala Guild Info(tenho que olhar mais direito se esta correto)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RoomGuildInfo
    {
        public int guild_1_uid;
        public int guild_2_uid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string guild_1_mark;             // mark string o pangya JP não usa aqui fica 0
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string guild_2_mark;             // mark string o pangya JP não usa aqui fica 0
        public ushort guild_1_index_mark;
        public ushort guild_2_index_mark;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string guild_1_nome;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string guild_2_nome;
        public RoomGuildInfo()
        {
            clear();
        }

        public void clear(int type = 0)
        {
            if (type == 0)
            {
                guild_1_uid = 0;
                guild_1_index_mark = 0;
                guild_1_mark = "";
                guild_1_nome = "";

                guild_2_uid = 0;
                guild_2_index_mark = 0;
                guild_2_mark = "";
                guild_2_nome = "";
            }
            if (type == 1)
            {
                guild_1_uid = 0;
                guild_1_index_mark = 0;
                guild_1_mark = "";
                guild_1_nome = "";
            }
            if (type == 2)
            {
                guild_2_uid = 0;
                guild_2_index_mark = 0;
                guild_2_mark = "";
                guild_2_nome = "";
            }
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(guild_1_uid);
                p.Write(guild_2_uid);
                p.WriteStr(guild_1_mark, 12);
                p.WriteStr(guild_2_mark, 12);
                p.Write(guild_1_index_mark);
                p.Write(guild_2_index_mark);
                p.WriteStr(guild_1_nome, 20);
                p.WriteStr(guild_1_nome, 20);
                return p.GetBytes;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("RoomGuildInfo {");
            sb.AppendLine($"  guild_1_uid = {guild_1_uid}");
            sb.AppendLine($"  guild_2_uid = {guild_2_uid}");
            sb.AppendLine($"  guild_1_mark = \"{guild_1_mark}\"");
            sb.AppendLine($"  guild_2_mark = \"{guild_2_mark}\"");
            sb.AppendLine($"  guild_1_index_mark = {guild_1_index_mark}");
            sb.AppendLine($"  guild_2_index_mark = {guild_2_index_mark}");
            sb.AppendLine($"  guild_1_nome = \"{guild_1_nome}\"");
            sb.AppendLine($"  guild_2_nome = \"{guild_2_nome}\"");
            sb.Append("}");
            return sb.ToString();
        }

    }

    //    // Sala Grand Prix Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RoomGrandPrixInfo
    {
        public uint dados_typeid;
        public uint rank_typeid;
        public uint tempo;
        public uint active;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(dados_typeid);
                p.Write(rank_typeid);
                p.Write(tempo);
                p.Write(active);
                return p.GetBytes;
            }
        }

        public override string ToString()
        {
            return $"RoomGrandPrixInfo {{ dados_typeid = {dados_typeid}, rank_typeid = {rank_typeid}, tempo = {tempo}, active = {active} }}";
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SpecialModeFlag
    {
        public SpecialModeFlag(uint _ul = 0)
        {
            ulNaturalAndShortGame = _ul;
        }

        public SpecialModeFlag()
        {
            ulNaturalAndShortGame = 0;
        }

        public uint ulNaturalAndShortGame { get; set; }

        public bool natural
        {
            get => (ulNaturalAndShortGame & 0x1u) == 1;
            set => ulNaturalAndShortGame = (ulNaturalAndShortGame & ~0x1u) | (value ? 1u : 0u);
        }

        public bool short_game
        {
            get => ((ulNaturalAndShortGame >> 1) & 0x1) == 1;
            set => ulNaturalAndShortGame = (ulNaturalAndShortGame & ~0x2u) | ((value ? 1u : 0u) << 1);
        }

        public override string ToString()
        {
            return $"NaturalAndShortGame {{ ulNaturalAndShortGame = {ulNaturalAndShortGame}, natural = {natural}, short_game = {short_game} }}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RoomInfoEx : RoomInfo
    {
        public RoomInfoEx()
        {
            clear();
            senha = "";
            hole_repeat = 0;
            fixed_hole = 0;
            tipo = 0;
            state_afk = 0;
            channel_rookie = false;
            angel_event = false;
            special_flag_mod = new SpecialModeFlag();
        }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string senha;                     // Senha da sala
        public byte tipo;                 // Tipo real da sala
        public byte hole_repeat;          // Número do hole que vai ser repetido
        public uint fixed_hole;            // Aqui é 1 Para Hole(Pin"Beam") Fixo, e 0 para aleatório
        public byte state_afk;   // Estado afk da sala, usar para depois começar a sala, já que o pangya não mostra se a sala está afk
        public bool channel_rookie;   // Flag que guarda, se o channel é rookie ou não, onde a sala foi criada, vem da Flag do channel
        public bool angel_event;      // Flag que guarda se o Angel Event está ligado 
        public ROOM_INFO_TYPE getTipo()
        {
            return (ROOM_INFO_TYPE)tipo;
        }

        public bool IsShotGame()
        {
            return special_flag_mod.short_game;
        }

        public byte[] ToArray()
        {
            using (var bw = new PangyaBinaryWriter())
            {
                bw.WriteStr(name, 64);//nao tem nada
                bw.WriteByte(senha_flag);
                bw.WriteByte(state);
                bw.WriteByte(flag);
                bw.WriteByte(max_player);
                bw.WriteByte(num_player);
                bw.WriteBytes(key, 16);//seria a chave -> senha da sala encriptografada!
                bw.WriteByte(gallery_num);
                bw.WriteByte(gallery_limit);
                bw.WriteByte(qntd_hole);
                bw.WriteByte(tipo_show);
                bw.WriteUInt16(numero);
                bw.WriteByte(modo);
                bw.WriteByte(course);
                bw.WriteUInt32(time_vs);
                bw.WriteUInt32(time_30s);
                bw.WriteUInt32(trofel);
                bw.WriteInt16(state_flag);

                //----- GUILDS
                bw.WriteBytes(guilds.ToArray());
                //----

                bw.WriteUInt32(rate_pang);
                bw.WriteUInt32(rate_exp);

                bw.WriteByte(flag_gm);
                bw.WriteInt32(master);

                bw.WriteByte(type_extend);
                bw.WriteUInt32(typeid_artefatic);

                //----- UNaturalAndShortGame
                bw.Write(special_flag_mod.ulNaturalAndShortGame);

                //----- GrandPrix
                bw.WriteBytes(grand_prix.ToArray());
                return bw.GetBytes;
            }
        }


        public byte[] ToArrayEx()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteByte(tipo_show);
                p.WriteByte(getMap());
                p.WriteByte(qntd_hole);
                p.WriteByte(modo);
                if (hole_repeat > 0 || getModo() == ROOM_INFO_MODO.M_REPEAT)
                {
                    p.WriteByte(hole_repeat);
                    p.WriteUInt32(fixed_hole);
                }
                p.WriteUInt32(special_flag_mod.ulNaturalAndShortGame);
                p.WriteByte(max_player);
                p.WriteByte(gallery_limit);        // constante 30 de pangya
                p.WriteSByte(Convert.ToSByte(state_flag == 1 ? 0 : 1));
                p.WriteUInt32(time_vs);
                p.WriteUInt32(time_30s);
                p.WriteUInt32(trofel);
                p.WriteByte(senha_flag); // Senha Flag
                if (senha_flag == 0)
                {
                    p.WriteString(senha);
                }
                p.WriteString(name);
                return p.GetBytes;
            }
        }
    }

    /// <summary>
    /// Ideia: eric antonio
    /// github.com/eatoniobr
    /// reformulado e melhorado por LuisMK
    /// </summary>
    public class RoomInfoLog : RoomInfoEx
    {

        // -------------------------------
        // Flags e estados da sala
        // -------------------------------
        public uint Is_GM_Event;          // Evento GM ativo
        public bool Is_natural;           // Modo Natural ativo
        public bool Is_short_game;        // Short game ativo
        public uint Is_GP;                // Grand Prix ativo
        public byte Is_hole_repeat;       // Buraco repetido
        public bool m_bot_tourney;        // Indica se é torneio com bots

        // -------------------------------
        // Dados do jogador
        // -------------------------------
        public uint uid;                  // UID do jogador
        public int character;             // Personagem
        public int club;                  // Taco
        public int mascot;                // Mascote
        public int caddie;                // Caddie

        // -------------------------------
        // Progresso no jogo
        // -------------------------------
        public uint hole;                 // Buraco atual
        public int score;                 // Pontuação
        public int exp;                  // Experiência
        public ulong pang;                // Pang
        public ulong bonus_pang;          // Pang bônus
        public ulong tacada_num;          // Número de tacadas
        public ulong total_tacada_num;    // Total de tacadas
        public ulong specialshot;         // Special Shot
        public bool premium;              // Jogador premium
        public ulong giveup;              // Desistiu
        public uint timeout;              // Desconectou por tempo
        public bool enter_after_started;  // Entrou após o início
        public bool finish_game;          // Terminou o jogo
        public uint assist_flag;          // Flag de assistência
        public uint Win_trofeu;           // Ganhou troféu

        // -------------------------------
        // Estatísticas detalhadas de tacadas
        // -------------------------------
        public uint HitHio;               // Hole In One
        public uint HitAlba;              // Albatross
        public uint HitEagle;             // Eagle
        public uint HitBirdie;            // Birdie
        public uint HitPar;               // Par
        public uint HitBogey;             // Bogey
        public uint Hit_x2_Bogey;         // Double Bogey
        public uint Hit_x3_Bogey;         // Triple Bogey

        // -------------------------------
        // Construtores
        // -------------------------------
        public RoomInfoLog() => clear();

        // Construtores
        public RoomInfoLog(RoomInfoEx _ul) : this()
        {
            SetInfo(_ul);
        }


        // Configura as informações da sala
        public void SetInfo(RoomInfoEx info)
        {
            if (info == null) return;

            // --- Campos básicos ---
            roomId = info.roomId;
            name = info.name;
            senha = info.senha;
            senha_flag = info.senha_flag;
            state = info.state;
            flag = info.flag;
            flag_gm = info.flag_gm;
            type_extend = info.type_extend;
            tipo = info.tipo;
            tipo_show = info.tipo_show;
            numero = info.numero;
            modo = info.modo;
            course = info.course;
            qntd_hole = info.qntd_hole;
            gallery_limit = info.gallery_limit;
            time_vs = info.time_vs;
            time_30s = info.time_30s;
            trofel = info.trofel;
            state_flag = info.state_flag;
            max_player = info.max_player;
            num_player = info.num_player;
            master = info.master;
            type_extend = info.type_extend;

            // --- Arrays e structs ---
            key = info.key != null ? (byte[])info.key.Clone() : new byte[16];
            guilds = new RoomGuildInfo
            {
                guild_1_uid = info.guilds.guild_1_uid,
                guild_2_uid = info.guilds.guild_2_uid,
                guild_1_mark = info.guilds.guild_1_mark,
                guild_2_mark = info.guilds.guild_2_mark,
                guild_1_index_mark = info.guilds.guild_1_index_mark,
                guild_2_index_mark = info.guilds.guild_2_index_mark,
                guild_1_nome = info.guilds.guild_1_nome,
                guild_2_nome = info.guilds.guild_2_nome
            };

            special_flag_mod = new SpecialModeFlag(info.special_flag_mod.ulNaturalAndShortGame);
            grand_prix = new RoomGrandPrixInfo
            {
                dados_typeid = info.grand_prix.dados_typeid,
                rank_typeid = info.grand_prix.rank_typeid,
                tempo = info.grand_prix.tempo,
                active = info.grand_prix.active
            };

            // --- Taxas e eventos ---
            rate_pang = info.rate_pang;
            rate_exp = info.rate_exp;
            typeid_artefatic = info.typeid_artefatic;
            Is_GP = info.grand_prix.active;
            Is_GM_Event = info.flag_gm;
            Is_natural = info.special_flag_mod.natural;
            Is_short_game = info.special_flag_mod.short_game;

            // --- Controle de estado ---
            state_afk = info.state_afk;
            hole_repeat = info.hole_repeat;
            fixed_hole = info.fixed_hole;
            channel_rookie = info.channel_rookie;
            angel_event = info.angel_event;
            gallery_num = info.gallery_num;

            // --- Complementares ---
            Is_hole_repeat = info.hole_repeat;
        }


        // Configura as informações do jogador
        public void UpdateInfo(uint _uid, int _character, int _club, int _mascot, int _caddie,
            uint _hole = 0, int _score = 0, int _exp = 0, ulong _pang = 0,
            ulong _bonus_pang = 0, ulong _tacada_num = 0, ulong _total_tacada_num = 0, ulong _specialshot = 0,
            bool _premium = false, bool _giveup = false, uint _timeout = 0, uint _enter_after_started = 0,
            uint _finish_game = 0, uint _assist_flag = 0, uint _Win_trofeu = 0, bool _hithio = false,
            bool _hitalba = false, bool _hiteagle = false, bool _hitbirdie = false, bool _hitpar = false,
            bool _hitbogey = false, bool _hit_2_bogey = false, bool _hit_3_bogey = false, RoomInfoEx _ul = null)
        {

            uid = _uid;
            character = _character;
            club = _club;
            mascot = _mascot;
            caddie = _caddie;
            hole = _hole;
            score = _score;
            exp = _exp;
            pang = _pang;
            bonus_pang = _bonus_pang;
            tacada_num = _tacada_num;
            total_tacada_num = _total_tacada_num;
            specialshot = _specialshot;
            premium = _premium;
if (_giveup) giveup++; 
            timeout = _timeout;
            enter_after_started = _enter_after_started == 1;
            finish_game = _finish_game == 1;
            assist_flag = _assist_flag;
            Win_trofeu = _Win_trofeu;
             // Verificam se o hit aconteceu e incrementam +1 no contador da classe
if (_hithio) HitHio++;
if (_hitalba) HitAlba++;
if (_hiteagle) HitEagle++;
if (_hitbirdie) HitBirdie++;
if (_hitpar) HitPar++;
if (_hitbogey) HitBogey++;
if (_hit_2_bogey) Hit_x2_Bogey++;
if (_hit_3_bogey) Hit_x3_Bogey++;
            if (_ul != null)
                SetInfo(_ul);
        }




        // Atualiza informações
        public RoomInfoLog UpdateInfo(uint _uid, uint _character, uint _club, uint _mascot, uint _caddie, RoomInfoEx _ul, bool bot_tourney = false)
        {
            UpdateInfo(_uid, (int)_character, (int)_club, (int)_mascot, (int)_caddie);
            m_bot_tourney = bot_tourney;
            if (_ul != null)
                SetInfo(_ul);
            return this;
        }

        // Gera string com as informações
        public string ToString(bool isDb)
        {
            if (isDb)
            {
                return $"{name}, {num_player}, {max_player}, {type_extend}, {uid}, {roomId}, {character}, {caddie}, {mascot}, {club}, {tipo}, {modo}, {qntd_hole}, {course}, {hole}, {score}, {exp}, {pang}, {bonus_pang}, {tacada_num}, {total_tacada_num}, {giveup}, {timeout}, {enter_after_started}, {finish_game}, {assist_flag}, {Win_trofeu}, {master}, {Is_short_game}, {Is_natural}, {HitHio}, {HitAlba}, {HitEagle}, {HitBirdie}, {HitPar}, {HitBogey}, {Hit_x2_Bogey}, {Hit_x3_Bogey}";
            }
            else
            {
                return $"[UID: {uid}, CharID: {character}, Room Type: {tipo},  Room TypeEx: {type_extend}, Game Mode: {modo}, Number Holes: {qntd_hole}, Map: {course}, Actual Hole: {hole}, Record: {score}, Exp: {exp}, Pangs: {pang}, P. Bonus: {bonus_pang}, Number Shot: {tacada_num}, Total Shot: {total_tacada_num}, Giveup: {giveup}, Timeout: {timeout}, EnterAfter: {enter_after_started}, FinishGame: {finish_game}, AssistFlag: {assist_flag}, RoomOwner: {master}, GameShort: {Is_short_game}, Natural: {Is_natural}]";
            }
        }

        // Sobrescrita do ToString padrão
        public override string ToString() => ToString(false);
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RateValue
    {
        public uint pang;
        public uint exp;
        public uint clubset;
        public uint rain;
        public uint treasure;
        public byte persist_rain;
    }


    // Item Pangya Base Para Pacote216
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ItemPangyaBase
    {

        public byte tipo;
        public uint _typeid;
        public uint id;
        public uint tipo_unidade_add;
        public uint qntd_ant;
        public uint qntd_dep;
        public uint qntd;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] unknown;// [8];
        public short qntd_time;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ItemPangya : ItemPangyaBase
    {
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string sd_idx;// [9];
        public uint sd_status;
        public uint sd_seq;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] unknown2;//[5];
    }

    // BuyItem
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BuyItem
    {
        public void clear()
        {
            id = 0;
            _typeid = 0;
            time = 0;
            ItemType = 0;
            qntd = 0;
            pang = 0;
            cookie = 0;
        }
        public int id;
        public uint _typeid;
        public short time;
        public short ItemType;
        public uint qntd;
        public uint pang;
        public uint cookie;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] ucUnknown13;

        public BuyItem ToRead(packet r)
        {
            id = r.ReadInt32();
            _typeid = r.ReadUInt32();
            time = r.ReadInt16();
            ItemType = r.ReadInt16();
            qntd = r.ReadUInt32();
            pang = r.ReadUInt32();
            cookie = r.ReadUInt32();
            ucUnknown13 = r.ReadBytes(13);

            return this;
        }

    }

    // Email Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class EmailInfo
    {
        public EmailInfo()
        {
            clear();
        }

        public void clear()
        {
            id = -1;
            lida_yn = 0;
            from_id = "";
            msg = "";
            gift_date = "";
            itens = new List<item>();
        }
        public int id;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string from_id;//[22];
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string gift_date;//[20];
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string msg;//[100];
        public byte lida_yn;
        //trocar por string, fica mais facil, depois eu vejo!
        public DateTime RegDate => string.IsNullOrEmpty(gift_date) ? DateTime.Now : DateTime.Parse(gift_date);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class item
        {
            public int id;
            public uint _typeid;
            public byte flag_time;
            public uint qntd;
            public uint tempo_qntd;
            public ulong pang;
            public ulong cookie;
            public int gm_id;
            public uint flag_gift;
            [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
            public string ucc_img_mark;//[9];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] ucUnknown3;//[3];
            public short type;

            public item()
            {
                clear();
            }

            public void clear()
            {
                ucc_img_mark = "";
                ucUnknown3 = new byte[3];
            }

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteInt32(id);
                    p.WriteUInt32(_typeid);
                    p.WriteByte(flag_time);
                    p.WriteUInt32(qntd);
                    p.WriteUInt32(tempo_qntd);
                    p.WriteUInt64(pang);
                    p.WriteUInt64(cookie);
                    p.WriteInt32(gm_id);
                    p.WriteUInt32(flag_gift);
                    p.WriteStr(ucc_img_mark, 9);//[9];
                    p.WriteBytes(ucUnknown3, 3);//[3];
                    p.WriteInt16(type);
                    return p.GetBytes;
                }
            }

            public item ToRead(packet r)
            {
                id = r.ReadInt32();
                _typeid = r.ReadUInt32();
                flag_time = r.ReadByte();
                qntd = r.ReadUInt32();
                tempo_qntd = r.ReadUInt32();
                pang = r.ReadUInt64();
                cookie = r.ReadUInt64();
                gm_id = r.ReadInt32();
                flag_gift = r.ReadUInt32();
                ucc_img_mark = r.ReadPStr(9);
                ucUnknown3 = r.ReadBytes(3);
                type = r.ReadInt16();

                return this; // devolve a mesma instância preenchida
            }

            public item(int _id, uint typeid, byte _flag_time, uint _qntd, ushort _tempo_qntd, uint _pang, uint _cookie, int _gm_id, uint _flag_gift, string _ucc_img_mark, short _type)
            {
                id = _id;
                _typeid = typeid;
                flag_time = _flag_time;
                qntd = _qntd;
                pang = _pang;
                cookie = _cookie;
                gm_id = _gm_id;
                tempo_qntd = _tempo_qntd;
                ucc_img_mark = _ucc_img_mark;
                flag_gift = _flag_gift;
                type = _type;
            }
        }
        public List<item> itens;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WritePStr(string.IsNullOrEmpty(from_id) ? "@ADM" : from_id);
                p.WritePStr(RegDate.ToString("dd/MM/yyyy"));
                p.WritePStr(msg);
                p.WriteByte(lida_yn); // Flag que mostra o item, 1 mostra, 0 não mostra

                p.WriteInt32(itens.Count);
                if (itens.Count > 0)
                    for (var i = 0; i < itens.Count; ++i)
                        p.WriteBytes(itens[i].ToArray());
                else
                    p.WriteBytes(new byte[55]);
                return p.GetBytes;
            }
        }

    }

    // EmailInfoEx
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class EmailInfoEx : EmailInfo
    {
        public EmailInfoEx()
        {
            clear();
            visit_count = 0;
        }

        public EmailInfoEx(uint v)
        {
        }

        public uint visit_count;
    }

    //// Mail Box
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MailBox
    {
        public int id;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] from_id_bytes;// [30];
        public string from_id { get => from_id_bytes.GetString(); set => from_id_bytes.SetString(value); }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] msg_bytes;// [80];
        public string msg { get => msg_bytes.GetString(); set => msg_bytes.SetString(value); }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] unknown2;// [18];
        public uint visit_count;
        public byte lida_yn;
        public uint item_num;           // Número de itens que tem nesse anexado a esse email
        [field: MarshalAs(UnmanagedType.Struct)]
        public EmailInfo.item item;
        public MailBox()
        {
            from_id_bytes = new byte[30];
            msg_bytes = new byte[80];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteStr(from_id, 30);// [30]);
                p.WriteStr(msg, 80);// [80]);
                p.WriteBytes(unknown2, 18);// [18]);
                p.WriteUInt32(visit_count);
                p.WriteByte(lida_yn);
                p.WriteUInt32(item_num);
                // Sometimes mail don't contain any items, need to check if mail contains an item or not.
                if (item?.ToArray() is byte[] itemData)
                    p.WriteBytes(itemData);
                else
                    p.WriteBytes(new byte[55]);
                return p.GetBytes;
            }
        }
    }

    //// Ticket Report Scroll Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TicketReportScrollInfo
    {
        public TicketReportScrollInfo() { clear(); }
        public void clear()
        {
            id = -1;
            date = new SYSTEMTIME();

            v_players = new List<stPlayerDados>();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class stPlayerDados
        {
            public stPlayerDados()
            {
                clear();
            }
            public void clear()
            {
                ucUnknown_flg = 2;
            }

            public byte[] ToArray()
            {
                try
                {
                    using (var p = new PangyaBinaryWriter())
                    {
                        p.WriteUInt32(uid);
                        p.WriteUInt64(pang);
                        p.WriteUInt64(bonus_pang);
                        p.WriteUInt32(trofel_typeid);
                        p.WriteUInt32(exp);
                        p.WriteUInt32(mascot_typeid);
                        p.WriteByte(premium_user);
                        p.WriteByte(item_boost); // [Bit] 1 = Pang Mastery x2, 2 = Pang Nitro x4, 3 = (ACHO) Exp x2
                        p.WriteUInt32(level);
                        p.WriteSByte(score);
                        p.WriteBytes(medalha.ToArray());
                        p.WriteByte(trofel);
                        p.WriteStr(id, 22);
                        p.WriteStr(nickname, 22);
                        p.WriteUInt32(ulUnknown);
                        p.WriteUInt32(guild_uid);
                        p.WriteUInt32(mark_index);        // Guild, isso é do JP, que ele nao usa o EMBLEM NUMER
                        p.WriteStr(guild_mark_img, 12);
                        p.WriteUInt32(tipo);
                        p.WriteByte(state);
                        p.WriteByte(ucUnknown_flg);    // Não sei mas sempre peguei o valor 2
                        p.WriteTime(finish_time);
                        return p.GetBytes;
                    }
                }
                catch (Exception e)
                { 
                    throw e;
                }
            }
            public uint uid;
            public ulong pang;
            public ulong bonus_pang;
            public uint trofel_typeid;
            public uint exp;
            public uint mascot_typeid;
            public byte premium_user;
            public byte item_boost; // [Bit] 1 = Pang Mastery x2, 2 = Pang Nitro x4, 3 = (ACHO) Exp x2
            public uint level;
            public sbyte score;
            public uMedalWin medalha = new uMedalWin();
            public byte trofel;
            public string id;//[22];
            public string nickname;//[22];
            public uint ulUnknown;
            public uint guild_uid;
            public uint mark_index;        // Guild, isso é do JP, que ele nao usa o EMBLEM NUMER
            public string guild_mark_img;//[12];
            public uint tipo;
            public byte state;
            public byte ucUnknown_flg;    // Não sei mas sempre peguei o valor 2
            public SYSTEMTIME finish_time= new SYSTEMTIME();
        }
        public int id;
        public SYSTEMTIME date = new SYSTEMTIME();
        public List<stPlayerDados> v_players;
    }

    // Estrutura que Guarda as informações dos Convites do Canal
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class InviteChannelInfo
    {
        public ushort room_number;
        public uint invite_uid;
        public uint invited_uid;
        [field: MarshalAs(UnmanagedType.Struct)]
        public SYSTEMTIME time;

        public InviteChannelInfo()
        { time = new SYSTEMTIME(); }
    }

    // Command Info, os Comando do Auth Server
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CommandInfo
    {
        public CommandInfo()
        {
            arg = new int[5];
            reserveDate = new SYSTEMTIME();
        }
        public uint idx;
        public uint id;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] arg = new int[5];// [5];
        public uint target;
        public short flag;
        public byte valid;
        [field: MarshalAs(UnmanagedType.Struct)]
        public SYSTEMTIME reserveDate;
    }

    //// Update Item
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class UpdateItem
    {
        public enum UI_TYPE : byte
        {
            CADDIE,
            CADDIE_PARTS,
            MASCOT,
            WAREHOUSE,
        }
        public UI_TYPE type;
        public uint _typeid;
        public int id;
        public UpdateItem(UI_TYPE _type, uint typeid, int _id)
        {
            this.type = _type;
            this._typeid = typeid;
            this.id = _id;
        }
    }

    //// Grand Prix Clear
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GrandPrixClear
    {
        public uint _typeid;
        public uint position;
        public GrandPrixClear()
        { }

        public GrandPrixClear(uint typeid, int _position)
        {
            _typeid = typeid;
            position = (uint)_position;
        }
    }

    //// Guild Update Activity Info
    //// Guarda os dados das atualizações que os Clubs tem de alterações
    //// Como membro kickado, sair do club e aceito no club
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GuildUpdateActivityInfo
    {
        public enum TYPE_UPDATE : byte
        {
            TU_ACCEPTED_MEMBER,
            TU_EXITED_MEMBER,
            TU_KICKED_MEMBER,
        }

        public ulong index; // ID do update activity
        public uint club_uid;
        public uint owner_uid; // Quem fez a Ação
        public uint player_uid;
        [field: MarshalAs(unmanagedType: UnmanagedType.U1)]
        public TYPE_UPDATE type;
        [field: MarshalAs(unmanagedType: UnmanagedType.Struct)]
        public SYSTEMTIME reg_date;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ChangePlayerItemRoom
    {
        public enum TYPE_CHANGE : byte
        {
            TC_CADDIE = 1,
            TC_BALL,
            TC_CLUBSET,
            TC_CHARACTER,
            TC_MASCOT,
            TC_ITEM_EFFECT_LOUNGE,  // Hermes x2, Twilight, Jester x2
            TC_ALL,                 // CHARACTER, CADDIE, CLUBSET e BALL essa é a ordem
            TC_UNKNOWN = 255,
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class stItemEffectLounge
        {
            public enum TYPE_EFFECT : uint
            {
                TE_BIG_HEAD = 1,
                TE_FAST_WALK,
                TE_TWILIGHT,
            }
            public uint item_id;   // Aqui ele manda 0 o cliente, não sei por que, deveria mandar o id do item equipado
            public TYPE_EFFECT effect;

            public stItemEffectLounge ToRead(packet r)
            {
                item_id = r.ReadUInt32();
                effect = (TYPE_EFFECT)r.ReadUInt32();

                return this;
            }

        }
        [field: MarshalAs(unmanagedType: UnmanagedType.U1)]
        public TYPE_CHANGE type;                   // Type Change
        public int caddie;                // Caddie ID
        public uint ball;                  // Ball Typeid
        public int clubset;               // ClubSet ID
        public int character;         // Character ID
        public int mascot;                // Mascot ID
        [field: MarshalAs(UnmanagedType.Struct)]
        public stItemEffectLounge effect_lounge = new stItemEffectLounge();   // Item effect lounge
    }

    // Trofel Info      
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TrofelInfo
    {
        public TrofelInfo()
        {
            clear();
        }
        public void clear()
        {
            Array.Clear(ama_6_a_1, 0, ama_6_a_1.Length); // Limpa todos os valores de ama_6_a_1
            Array.Clear(pro_1_a_7, 0, pro_1_a_7.Length); // Limpa todos os valores de pro_1_a_7
        }
        public void update(uint _type, byte _rank)
        {

            // Maior que Pro 7
            if (_type > 12)
                throw new exception("[TrofelInfo::update][Error] _type[VALUE=" + (_type) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_GAME_ST, 200, 0));

            if (_rank == 0u || _rank > 3)
                throw new exception("[TrofelInfo::update][Error] _rank[VALUE=" + (_rank) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_GAME_ST, 201, 0)); ;

            if (_type < 6)
            { // AMA

                ama_6_a_1[_type, _rank - 1]++;

            }
            else
            { // >= 6 PRO

                pro_1_a_7[_type - 6, _rank - 1]++;
            }
        }

        public uint getSumGold()
        {
            uint gold_sum = 0;

            // Itera sobre as linhas da matriz ama_6_a_1
            for (int i = 0; i < ama_6_a_1.GetLength(0); i++)
            {
                gold_sum += (uint)ama_6_a_1[i, 0]; // Coluna 0 para o ouro
            }

            // Itera sobre as linhas da matriz pro_1_a_7
            for (int i = 0; i < pro_1_a_7.GetLength(0); i++)
            {
                gold_sum += (uint)pro_1_a_7[i, 0]; // Coluna 0 para o ouro
            }

            return gold_sum;
        }

        public uint getSumSilver()
        {
            uint silver_sum = 0;

            // Itera sobre as linhas da matriz ama_6_a_1
            for (int i = 0; i < ama_6_a_1.GetLength(0); i++)
            {
                silver_sum += (uint)ama_6_a_1[i, 1]; // Coluna 1 para a prata
            }

            // Itera sobre as linhas da matriz pro_1_a_7
            for (int i = 0; i < pro_1_a_7.GetLength(0); i++)
            {
                silver_sum += (uint)pro_1_a_7[i, 1]; // Coluna 1 para a prata
            }

            return silver_sum;
        }

        public uint getSumBronze()
        {
            uint bronze_sum = 0;

            // Itera sobre as linhas da matriz ama_6_a_1
            for (int i = 0; i < ama_6_a_1.GetLength(0); i++)
            {
                bronze_sum += (uint)ama_6_a_1[i, 2]; // Coluna 2 para o bronze
            }

            // Itera sobre as linhas da matriz pro_1_a_7
            for (int i = 0; i < pro_1_a_7.GetLength(0); i++)
            {
                bronze_sum += (uint)pro_1_a_7[i, 2]; // Coluna 2 para o bronze
            }

            return bronze_sum;
        }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public short[,] ama_6_a_1 = new short[6, 3];    // Ama 6~1, Ouro, Prata e Bronze
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 42)]
        public short[,] pro_1_a_7 = new short[7, 3];    // Pro 1~7, Ouro, Prate e Bronze
                                                        // 
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                // Gravar ama_6_a_1 (6 linhas x 3 colunas)
                for (int i = 0; i < 6; i++)  // 6 linhas
                {
                    for (int j = 0; j < 3; j++)  // 3 colunas (Ouro, Prata, Bronze)
                    {
                        p.WriteInt16(ama_6_a_1[i, j]);  // Escreve cada valor como short
                    }
                }

                // Gravar pro_1_a_7 (7 linhas x 3 colunas)
                for (int i = 0; i < 7; i++)  // 7 linhas
                {
                    for (int j = 0; j < 3; j++)  // 3 colunas (Ouro, Prata, Bronze)
                    {
                        p.WriteInt16(pro_1_a_7[i, j]);  // Escreve cada valor como short
                    }
                }
                Debug.Assert(!(p.GetSize != 78), "TrofelInfo::ToArray() is error");
                return p.GetBytes;
            }
        }
    }

    // Trofel Especial Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TrofelEspecialInfo
    {
        public int id;
        public uint _typeid;
        public int qntd;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteUInt32(_typeid);
                p.WriteInt32(qntd);
                return p.GetBytes;
            }
        }
    }

    // Item Equipados
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 116)]
    public class UserEquip
    {
        public UserEquip()
        {
            clear();
        }
        public void clear()
        {
            item_slot = new uint[10];
            skin_id = new uint[6];
            skin_typeid = new uint[6];
            poster = new uint[2];
        }

        public int caddie_id;
        public int character_id;
        public int clubset_id;
        public uint ball_typeid;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] item_slot;    // 10 Item slot
        /// <summary>
        /// [0] = BG, [1] = Frame, [2] = Sticker, [3] = Slot, [4] = Cutin, [5] = Title
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] skin_id;     // 6 skin id, tem o title, frame, stick e etc
        /// <summary>
        /// [0] = BG, [1] = Frame, [2] = Sticker, [3] = Slot, [4] = Cutin, [5] = Title
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] skin_typeid; // 6 skin typeid, tem o title, frame, stick e etc
        public int mascot_id;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] poster;     // Poster, tem 2 o poster A e poster B
        public uint m_title => skin_typeid[5];// Titulo Typeid 

        /// <summary>
        /// Size = 116 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(caddie_id);
                p.WriteInt32(character_id);
                p.WriteInt32(clubset_id);
                p.WriteUInt32(ball_typeid);

                p.WriteUInt32(item_slot);//[10];      // 10 Item slot
                p.WriteUInt32(skin_id);//[6];     // 6 skin id, tem o title, frame, stick e etc
                p.WriteUInt32(skin_typeid); // 6 skin typeid, tem o title, frame, stick e etc

                p.WriteInt32(mascot_id);
                p.WriteUInt32(poster);     // Poster, tem 2 o poster A e poster B

                Debug.Assert(!(p.GetSize != 116), "UserEquip::Build is Error");

                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MapStatistics
    {
        public MapStatistics(uint _ul = 0u)
        {
            clear();
        }
        public void clear(sbyte _course = 0)
        {
            best_score = 127;
            course = _course;
        }

        public bool isRecorded()
        {
            // Player fez record nesse Course
            return (best_score != 127);
        }

        public sbyte course;
        public uint tacada;
        public uint putt;
        public uint hole;
        public uint fairway;
        public uint hole_in;
        public uint putt_in;
        public int total_score;
        public sbyte best_score;
        public ulong best_pang;
        public uint character_typeid;
        public byte event_score;
        /// <summary>
        /// Size 43 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {

                p.WriteSByte(course);
                p.WriteUInt32(tacada);
                p.WriteUInt32(putt);
                p.WriteUInt32(hole);
                p.WriteUInt32(fairway);
                p.WriteUInt32(hole_in);
                p.WriteUInt32(putt_in);
                p.WriteInt32(total_score);
                p.WriteSByte(best_score);
                p.WriteUInt64(best_pang);
                p.WriteUInt32(character_typeid);
                p.WriteByte(event_score);
                Debug.Assert(!(p.GetSize != 43), "MapStatistics::ToArray() is Error");
                return p.GetBytes;
            }
        }
        public void CopyFrom(MapStatistics _cpy)
        {
            course = _cpy.course;
            tacada = _cpy.tacada;
            putt = _cpy.putt;
            hole = _cpy.hole;
            fairway = _cpy.fairway;
            hole_in = _cpy.hole_in;
            putt_in = _cpy.putt_in;
            total_score = _cpy.total_score;
            best_score = _cpy.best_score;
            best_pang = _cpy.best_pang;
            character_typeid = _cpy.character_typeid;
            event_score = _cpy.event_score;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // MapStatisticsEx esse tem o tipo que não vai no pacote que passa pro cliente
    public class MapStatisticsEx : MapStatistics
    {
        public MapStatisticsEx(uint _ul = 0) : base(_ul)
        {
            tipo = 0;
        }
        public MapStatisticsEx(MapStatisticsEx _cpy)
        {
            CopyFrom(_cpy);
        }
        public MapStatisticsEx(MapStatistics _cpy)
        {
            tipo = 0;
            CopyFrom(_cpy);
        }
        public byte tipo;             // Tipo, 0 Normal, 0x32 Natural, 0x33 Grand Prix
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // Caddie Info
    public class CaddieInfo
    {
        public int id;
        public uint _typeid;
        public uint parts_typeid;
        public byte level;
        public int exp;
        public byte rent_flag;
        public ushort end_date_unix;
        public short parts_end_date_unix;
        public byte purchase;
        public short check_end;
        public virtual void clear()
        {
            // Limpa o caddie Parts
            parts_typeid = 0;
            parts_end_date_unix = 0;
        }

        /// <summary>
        /// Size = 25 (0x19)
        /// </summary>
        /// <param name="is_login"></param>
        /// <returns></returns>
        public byte[] ToArray()
        {
            try
            {

                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteInt32(id);
                    p.WriteUInt32(_typeid);
                    p.WriteUInt32(parts_typeid);
                    p.WriteByte(level);
                    p.WriteInt32(exp);
                    p.WriteByte(rent_flag);
                    p.WriteUInt16(end_date_unix);
                    p.WriteInt16(parts_end_date_unix);
                    p.WriteByte(purchase);
                    p.WriteInt16(check_end);

                    return p.GetBytes;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    // Caddie Info Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CaddieInfoEx : CaddieInfo
    {
        public SYSTEMTIME end_date;
        public SYSTEMTIME end_parts_date;
        public bool need_update;         // Precisa Atulizar para o cliente

        public CaddieInfoEx()
        {
            end_date = new SYSTEMTIME();
            end_parts_date = new SYSTEMTIME();
        }
        public void updatePartsEndDate()
        {
            if (end_parts_date.IsEmpty)
            {
                parts_end_date_unix = 0;
                // Zera Parts_Typeid
                if (parts_typeid > 0)
                {
                    parts_typeid = 0;

                    need_update = true;   // Precisa Atulizar para o cliente
                }
                return;
            }
            DateTime now = DateTime.Now.Date;  // Só data, sem hora
            DateTime end = end_parts_date.ConvertTime().Date;

            int diffDays = (end - now).Days;
            // Não tem mais o parts _typeid acabou o tempo dela
            if (diffDays <= 0)
            {
                // Zera Parts_Typeid
                if (parts_typeid > 0)
                {
                    parts_typeid = 0;

                    need_update = true;   // Precisa Atulizar para o cliente
                }
            }
            else
                parts_end_date_unix = (short)diffDays;
        }
        public void updateEndDate()
        {

            if (end_date.IsEmpty)
            {
                end_date_unix = 0;
                return;
            }
            DateTime now = DateTime.Now.Date;  // Só data, sem hora
            DateTime end = end_date.ConvertTime().Date;

            int diffDays = (end - now).Days;

            if (diffDays <= 0)
                end_date_unix = 0;
            else
                end_date_unix = (ushort)diffDays;
        }

        //precisa ser o CaddieInfoEx mesmo, depois modifico
        public void Check() //chamar igual no orignal!
        {
            // Update Timestamp Unix of caddie and caddie Parts

            // End Date Unix Update 
            updateEndDate();

            // Parts End Date Unix Update

            updatePartsEndDate();
        }

        public override void clear()
        {
            base.clear();
            end_parts_date = new SYSTEMTIME();
        }

        public CaddieInfoEx getInfo()
        {
            Check();
            return this;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestMakeTutorial
    {

        public void clear()
        {
            this = new RequestMakeTutorial(); // zera todos os campos
        }
        public static RequestMakeTutorial Load(byte[] buffer)
        {
            if (buffer.Length < 6)
                throw new ArgumentException("Buffer must have at least 6 bytes.");

            RequestMakeTutorial req = new RequestMakeTutorial();

            req.uTipo = new u1
            {
                usTipo = BitConverter.ToUInt16(buffer, 0),
                stTipo = new u1.stTipo_t
                {
                    finish = buffer[0],
                    tipo = buffer[1]
                }
            };

            req.uValor = new u2
            {
                ulValor = BitConverter.ToUInt32(buffer, 2),
                stValor = new u2.st1_t
                {
                    rookie = new u2.st1_t.uByte(buffer[2]),
                    beginner = new u2.st1_t.uByte(buffer[3]),
                    advancer = new u2.st1_t.uByte(buffer[4])
                }
            };

            return req;
        }
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct u1
        {
            [FieldOffset(0)]
            public ushort usTipo;
            [FieldOffset(0)]
            public stTipo_t stTipo;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct stTipo_t//esse debaixo é o mesmo de cima, so que divido em bytes, ushort é 2 bytes,
                                  //entao cada byte alimenta uma posicao de dados diferente
            {
                public byte finish;  // 0 normal, 1 finish tutorial
                public byte tipo;    // 0 Rookie, 1 Beginner, 2 Advancer
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct u2
        {
            [FieldOffset(0)]
            public uint ulValor;

            [FieldOffset(0)]
            public st1_t stValor;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct st1_t//esse debaixo é o mesmo de cima, so que divido em bytes, uint é 4 bytes,
                               //entao cada byte alimenta uma posicao de dados diferente
            {
                public uByte rookie;
                public uByte beginner;
                public uByte advancer;
                // public uByte unknown_fill; // se necessário

                [StructLayout(LayoutKind.Explicit, Pack = 1)]
                public struct uByte
                {
                    [FieldOffset(0)]
                    public byte ucbyte;

                    [FieldOffset(0)]
                    public st2_t st8bit;
                    public uByte(byte value)
                    {
                        ucbyte = value;
                        st8bit = new st2_t { bits = value };
                    }
                    [StructLayout(LayoutKind.Sequential)]
                    public struct st2_t
                    {
                        public byte bits;

                        public bool _bit0 { get => (bits & (1 << 0)) != 0; set => bits = SetBit(bits, 0, value); }
                        public bool _bit1 { get => (bits & (1 << 1)) != 0; set => bits = SetBit(bits, 1, value); }
                        public bool _bit2 { get => (bits & (1 << 2)) != 0; set => bits = SetBit(bits, 2, value); }
                        public bool _bit3 { get => (bits & (1 << 3)) != 0; set => bits = SetBit(bits, 3, value); }
                        public bool _bit4 { get => (bits & (1 << 4)) != 0; set => bits = SetBit(bits, 4, value); }
                        public bool _bit5 { get => (bits & (1 << 5)) != 0; set => bits = SetBit(bits, 5, value); }
                        public bool _bit6 { get => (bits & (1 << 6)) != 0; set => bits = SetBit(bits, 6, value); }
                        public bool _bit7 { get => (bits & (1 << 7)) != 0; set => bits = SetBit(bits, 7, value); }

                        public byte whatBit()
                        {
                            if (_bit0) return 1;
                            else if (_bit1) return 2;
                            else if (_bit2) return 3;
                            else if (_bit3) return 4;
                            else if (_bit4) return 5;
                            else if (_bit5) return 6;
                            else if (_bit6) return 7;
                            else if (_bit7) return 8;
                            return 0;
                        }

                        private static byte SetBit(byte value, int bit, bool on)
                        {
                            if (on)
                                return (byte)(value | (1 << bit));
                            else
                                return (byte)(value & ~(1 << bit));
                        }
                    }
                }
            }
        }

        public u1 uTipo;
        public u2 uValor;
    }
    // Club Set Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ClubSetInfo
    {
        public int id;
        public uint _typeid;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public short[] slot_c = new short[5];// [5];        // Total de slot para upa do stats, força, controle, spin e etc
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public short[] enchant_c = new short[5];// [5];     // Enchant Club, Força, controle, spin e etc

        public void setValues(int _uid, uint id_type, short[] value)
        {
            slot_c = value;
            _typeid = id_type;
            id = _uid;
        }
        public ClubSetInfo()
        {
            slot_c = new short[5];
            enchant_c = new short[5];
        }
        /// <summary>
        /// Size = 28 bytes (0x)
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteUInt32(_typeid);

                p.WriteInt16(slot_c);// [5];        // Total de slot para upa do stats, força, controle, spin e etc
                p.WriteInt16(enchant_c);// [5];     // Enchant Club, Força, controle, spin e etc
                //if (p.GetSize == 28)
                //    Debug.WriteLine("GetClubData Size Okay");

                return p.GetBytes;
            }
        }
    }

    // Mascot Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MascotInfo
    {
        public int id;
        public uint _typeid;
        public byte level;
        public int exp;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        private byte[] message_bytes = new byte[30];
        public string message { get => message_bytes.GetString(); set => message_bytes.SetString(value); }
        public short tipo;
        [field: MarshalAs(UnmanagedType.Struct)]
        public SYSTEMTIME data = new SYSTEMTIME();
        public byte flag;
        public MascotInfo()
        {
            clear();
        }
        public void clear()
        {
            data = new SYSTEMTIME();
            message_bytes = new byte[30];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteUInt32(_typeid);
                p.WriteByte(level);
                p.WriteInt32(exp);
                p.WriteStr(message, 30);
                p.WriteInt16(tipo);
                p.WriteTime(data);
                p.WriteByte(flag);
                return p.GetBytes;
            }
        }

    }

    // Mascot Info Ex, tem o IsCash type nele
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MascotInfoEx : MascotInfo
    {
        public MascotInfoEx()
        {
            clear();
            PCBang = 0;
        }
        public bool checkUpdate()
        {

            if (data.IsEmpty)
                need_update = 1;

            return (need_update == 1);
        }

        public byte is_cash;
        public uint price;
        public byte need_update;
        public byte PCBang = 1;
    }

    // Item Warehouse
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 196)]
    public class WarehouseItem
    {
        public WarehouseItem()
        {
            clear();
        }

        public void clear()
        {
            c = new short[5];
            card = new Card()
            { caddie = new uint[4], character = new uint[4], NPC = new uint[4] };
            clubset_workshop = new ClubsetWorkshop() { c = new short[5] };
            ucc = new UCC();
        }
        public int id { get; set; }
        public uint _typeid { get; set; }
        public int ano { get; set; }            // acho que seja só tempo que o item ainda tem
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public short[] c { get; set; } = new short[5];     // Stats do item ctrl, força etc, se não usa isso o [0] é a quantidade
        public byte purchase { get; set; }
        public sbyte flag { get; set; }
        public long apply_date { get; set; }
        public long end_date { get; set; }
        public sbyte type { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UCC
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private byte[] name_bytes;
            public string name { get => name_bytes.GetString(); set => name_bytes.SetString(value); }
            public sbyte trade { get; set; }     // Aqui pode(acho) ser qntd de sd que foi vendida
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            private byte[] idx_bytes;
            public string idx { get => idx_bytes.GetString(); set => idx_bytes.SetString(value); }             // 8 
            public byte status { get; set; }
            public short seq { get; set; }          // aqui é a seq de sd que vendeu
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
            private byte[] copier_nick_bytes;
            public string copier_nick { get => copier_nick_bytes.GetString(); set => copier_nick_bytes.SetString(value); }
            public uint copier { get; set; }                // m_uid de quem fez a sd

            public UCC()
            {
                name_bytes = new byte[40];
                idx_bytes = new byte[9];
                copier_nick_bytes = new byte[22];
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public UCC ucc;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Card
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] character { get; set; } = new uint[4];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] caddie { get; set; } = new uint[4];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] NPC { get; set; } = new uint[4];
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Card card;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ClubsetWorkshop
        {
            public short flag { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public short[] c { get; set; } = new short[5];
            // Stats do item ctrl, força etc, se não usa isso o [0] é a quantidade			public uint  mastery {get; set;}
            public uint mastery { get; set; }
            public uint recovery_pts { get; set; }
            public int level { get; set; }
            public int rank { get; set; }          // UP eu chamo esse

            public int calcRank(ushort[] _c)
            {
                int total = (int)(c[0] + _c[0] + c[1] + _c[1] + c[2] + _c[2] + c[3] + _c[3] + c[4] + _c[4]);

                if (total >= 30 && total < 60)
                    return (int)((total - 30) / 5);

                return int.MaxValue;
            }

            public int calcLevel(ushort[] _c)
            {

                int total = (int)(c[0] + _c[0] + c[1] + _c[1] + c[2] + _c[2] + c[3] + _c[3] + c[4] + _c[4]);

                if (total >= 30 && total < 60)
                    return (total - 30) % 5;

                return int.MaxValue;
            }

            public static int s_calcRank(ushort[] _c)
            {

                uint total = (uint)(_c[0] + _c[1] + _c[2] + _c[3] + _c[4]);

                if (total >= 30 && total < 60)
                    return (int)(total - 30) / 5;

                return -1;
            }
            public static int s_calcLevel(ushort[] _c)
            {

                uint total = (uint)(_c[0] + _c[1] + _c[2] + _c[3] + _c[4]);

                if (total >= 30 && total < 60)
                    return (int)(total - 30) % 5;

                return -1;
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public ClubsetWorkshop clubset_workshop;
        public bool IsUCC()
        {
            return sIff.getInstance().getItemGroupIdentify(_typeid) == IFF_GROUP.PART &&
                         !string.IsNullOrEmpty(ucc.idx);
        }
        /// <summary>
        /// Size 196 Bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteUInt32(_typeid);
                p.WriteInt32(ano);
                p.WriteInt16(c);
                p.WriteByte(purchase);
                p.WriteSByte(flag);//sbyte
                p.WriteInt64(apply_date);
                p.WriteInt64(end_date);
                p.WriteSByte(type);//sbyte 
                p.WriteStr(ucc.name, 40);
                p.WriteSByte(ucc.trade);//sbyte
                p.WriteStr(ucc.idx, 9);
                p.WriteByte(ucc.status);//sbyte
                p.WriteInt16(ucc.seq);
                p.WriteStr(ucc.copier_nick, 22);
                p.WriteUInt32(ucc.copier);
                p.WriteUInt32(card.character);
                p.WriteUInt32(card.caddie);
                p.WriteUInt32(card.NPC);
                p.WriteInt16(clubset_workshop.flag);
                p.WriteInt16(clubset_workshop.c);
                p.WriteUInt32(clubset_workshop.mastery);
                p.WriteUInt32(clubset_workshop.recovery_pts);
                p.WriteInt32(clubset_workshop.level);
                p.WriteInt32(clubset_workshop.rank);
                Debug.Assert(!(p.GetSize != 196), "WareHouse::ToArray() is error");
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WarehouseItemEx : WarehouseItem
    {
        // Date to Calcule dates
        public uint apply_date_unix_local;
        public uint end_date_unix_local;
        public WarehouseItemEx()
        {
            clear();
        }

        public WarehouseItemEx(WarehouseItemEx pWi)
        {
            clear();

            id = pWi.id;
            _typeid = pWi._typeid;
            ano = pWi.ano;

            if (pWi.c != null)
                Array.Copy(pWi.c, c, c.Length);

            purchase = pWi.purchase;
            flag = pWi.flag;
            apply_date = pWi.apply_date;
            end_date = pWi.end_date;
            type = pWi.type;

            if (pWi.ucc != null)
            {
                ucc.name = pWi.ucc.name;
                ucc.trade = pWi.ucc.trade;
                ucc.idx = pWi.ucc.idx;
                ucc.status = pWi.ucc.status;
                ucc.seq = pWi.ucc.seq;
                ucc.copier_nick = pWi.ucc.copier_nick;
                ucc.copier = pWi.ucc.copier;
            }

            if (pWi.card != null)
            {
                Array.Copy(pWi.card.character, card.character, card.character.Length);
                Array.Copy(pWi.card.caddie, card.caddie, card.caddie.Length);
                Array.Copy(pWi.card.NPC, card.NPC, card.NPC.Length);
            }

            if (pWi.clubset_workshop != null)
            {
                clubset_workshop.flag = pWi.clubset_workshop.flag;

                if (pWi.clubset_workshop.c != null)
                    Array.Copy(pWi.clubset_workshop.c, clubset_workshop.c, clubset_workshop.c.Length);

                clubset_workshop.mastery = pWi.clubset_workshop.mastery;
                clubset_workshop.recovery_pts = pWi.clubset_workshop.recovery_pts;
                clubset_workshop.level = pWi.clubset_workshop.level;
                clubset_workshop.rank = pWi.clubset_workshop.rank;
            }

            // Campos exclusivos de WarehouseItemEx
            apply_date_unix_local = pWi.apply_date_unix_local;
            end_date_unix_local = pWi.end_date_unix_local;
        }
        public int STDA_C_ITEM_QNTD
        {
            get => c[0] == 32767 ? -1 : c[0];
            set => c[0] = (short)(value == -1 ? 32767 : value);
        }

        public short STDA_C_ITEM_TICKET_REPORT_ID_HIGH
        {
            get => c[1];
            set => c[1] = value;
        }

        public short STDA_C_ITEM_TICKET_REPORT_ID_LOW
        {
            get => c[2];
            set => c[2] = value;
        }

        public int STDA_C_ITEM_TIME
        {
            get => c[3] == 32767 ? -1 : c[3];
            set => c[3] = (short)(value == -1 ? 32767 : value);
        }
    }

    // ClubSet Workshop Last Up Level
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ClubSetWorkshopLasUpLevel
    {
        public int clubset_id;
        public uint stat;
    }

    // ClubSet WorkShop Transform ClubSet In Special ClubSet
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ClubSetWorkshopTransformClubSet
    {
        public int clubset_id;
        public uint stat;
        public uint transform_typeid;
    }
    // Personal Shop Item
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PersonalShopItem
    {
        public uint index;     // Index Sequência do item no shop
        [field: MarshalAs(UnmanagedType.Struct)]
        public TradeItem item;
        public PersonalShopItem()
        { clear(); }

        public PersonalShopItem(PersonalShopItem psi)
        {
            clear();
            index = psi.index;
            item = new TradeItem(psi.item);
        }

        public void clear()
        {
            item = new TradeItem();

        }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(index);
                p.WriteBytes(item.ToArray());
                return p.GetBytes;
            }
        }

        public PersonalShopItem ToRead(packet r)
        {
            index = r.ReadUInt32();
            item = new TradeItem().ToRead(r);
            return this;
        }

    }

    // Tutorial Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TutorialInfo
    {
        public uint getTutoAll()
        {
            return rookie | beginner | advancer;
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(rookie);
                p.WriteUInt32(beginner);
                p.WriteUInt32(advancer);
                return p.GetBytes;
            }
        }

        public uint rookie;
        public uint beginner;
        public uint advancer;
    }

    // Card Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CardInfo
    {
        public int id;
        public uint _typeid;
        public uint slot;
        public uint efeito;
        public uint efeito_qntd;
        public int qntd;
        public SYSTEMTIME use_date;
        public SYSTEMTIME end_date;
        public byte type;
        public byte use_yn;
        public CardInfo()
        {
            use_date = new SYSTEMTIME();
            end_date = new SYSTEMTIME();
        }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(id);
                p.WriteUInt32(_typeid);
                p.WriteUInt32(slot);
                p.WriteUInt32(efeito);
                p.WriteUInt32(efeito_qntd);
                p.WriteInt32(qntd);
                if (use_date.IsEmpty)
                {
                    p.WriteZero(16);
                }
                else
                {
                    p.WriteTime(use_date);
                }

                if (end_date.IsEmpty)
                {
                    p.WriteZero(16);
                }
                else
                {
                    p.WriteTime(end_date);
                }
                p.WriteByte(type);
                p.WriteByte(use_yn);
                return p.GetBytes;
            }
        }
    }

    // Card Equip Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CardEquipInfo
    {
        public uint id;
        public uint _typeid;
        public uint parts_typeid;
        public uint parts_id;
        public uint efeito;
        public uint efeito_qntd;
        public uint slot; 
        public SYSTEMTIME use_date = new SYSTEMTIME(); 
        public SYSTEMTIME end_date = new SYSTEMTIME();
        public uint tipo;
        public byte use_yn;
        public CardEquipInfo()
        {
            use_date = new SYSTEMTIME();
            end_date = new SYSTEMTIME();
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(id);//zerado
                p.WriteUInt32(_typeid);
                p.WriteUInt32(parts_typeid);
                p.WriteUInt32(parts_id);
                p.WriteUInt32(efeito);
                p.WriteUInt32(efeito_qntd);
                p.WriteUInt32(slot);
                p.WriteTime(use_date);
                p.WriteTime(end_date);
                p.WriteUInt32(tipo);
                p.WriteByte(use_yn);
                return p.GetBytes;
            }
        }
    }

    // Card Equip Info Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CardEquipInfoEx : CardEquipInfo
    {
        public CardEquipInfoEx()
        {
            use_date = new SYSTEMTIME();
            end_date = new SYSTEMTIME();
        }
        public int index = new int();
    }



    // Message Off
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MsgOffInfo
    {

        public uint from_uid;
        public short id;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public string nick;//[22];
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public string msg;//[64];
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public string date;// [17];
        public byte Un;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(from_uid);
                p.Write(id);
                p.WriteStr(nick, 22);//[22];
                p.WriteStr(msg, 64);//[64];
                p.WriteStr(date, 16);// [17];
                p.Write(Un);
                return p.GetBytes;
            }
        }
    }

    // Attendence Reward Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AttendanceRewardInfo
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class item
        {
            public uint _typeid;
            public uint qntd;

            public void clear()
            {
                _typeid = 0;
                qntd = 0;
            }
        }
        public byte login;
        [field: MarshalAs(UnmanagedType.Struct)]
        public item now;
        [field: MarshalAs(UnmanagedType.Struct)]
        public item after;
        public uint counter;
        public AttendanceRewardInfo()
        {
            clear();
        }
        public void clear()
        {
            now = new item();
            after = new item();
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteByte(login);
                p.WriteUInt32(now._typeid);
                p.WriteUInt32(now.qntd);
                p.WriteUInt32(after._typeid);
                p.WriteUInt32(after.qntd);
                p.WriteUInt32(counter);
                return p.GetBytes;
            }
        }
    }

    // Attendance Reward Info Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AttendanceRewardInfoEx : AttendanceRewardInfo
    {
        public AttendanceRewardInfoEx()
        {
            last_login = new SYSTEMTIME();
            base.clear();
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public SYSTEMTIME last_login;   // Data do ultimo login
    }

    // Attendance Reward Item Context
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AttendanceRewardItemCtx
    {
        public uint _typeid;
        public uint qntd;
        public byte tipo;
    }

    // Last Five Players Played with player
    public class Last5PlayersGame
    {
        public Last5PlayersGame()
        {
            // Inicializa o array com 5 elementos de LastPlayerGame
            this.players = new List<LastPlayerGame>();

            // Inicializa cada elemento do array
            for (int i = 0; i < 5; i++)
                players.Add(new LastPlayerGame());
        }
        public class LastPlayerGame
        {
            public uint sex;   // gender, genero, sexo, 0 masculino, 1 Feminino
            public string nick;
            public string id;
            public uint uid;

            public LastPlayerGame()
            {
                nick = "";
                id = "";
            }
            public bool Equals(LastPlayerGame obj)
            {
                return (uid == obj.uid
                            && id == obj.id);
            }

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteUInt32(sex);
                    p.WriteStr(nick, 22);
                    p.WriteStr(id, 22);
                    p.WriteUInt32(uid);
                    return p.GetBytes;
                }
            }
        }

        public void add(player_info _pi, uint _sex)
        {
            // Se já é o último, atualiza
            if (players.Count > 0 && players[players.Count - 1].uid == _pi.uid)
            {
                players[players.Count - 1].sex = _sex;
                players[players.Count - 1].nick = _pi.nickname;
                return;
            }

            // Adiciona novo no fim
            players.Add(new LastPlayerGame
            {
                id = _pi.id,
                uid = _pi.uid,
                sex = _sex,
                nick = _pi.nickname
            });

            // Remove duplicados (anteriores ao novo adicionado)
            for (int i = players.Count - 2; i >= 0; i--)
            {
                if (players[i].uid == _pi.uid)
                {
                    players.RemoveAt(i);
                    break; // só pode existir um duplicado, então break
                }
            }

            // Limita a 5
            while (players.Count > 5)
            {
                players.RemoveAt(0);
            }
        }

        public List<LastPlayerGame> players;  // Last Five Players Played with player
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PangyaTime_ItemBuff : SYSTEMTIME
    {
        public void setTime(uint time)
        {
            Second = (ushort)(time / 0xFFFF);
            MilliSecond = (ushort)(time % 0xFFFF);
        }
        public uint getTime()
        {
            return (uint)((Second * 0xFFFF) | MilliSecond);
        }

    }
    // Time 32, HighTime, LowTime
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class time32
    {
        public void setTime(int time)
        {
            high_time = (ushort)(time / 0xFFFF);
            low_time = (ushort)(time % 0xFFFF);
        }
        public uint getTime()
        {
            return Convert.ToUInt32((high_time * 0xFFFF) | low_time);
        }
        public ushort high_time;
        public ushort low_time;
    }

    // Item Buff (Exemple: Yam, Bola Arco-iris)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ItemBuff
    {
        public enum eTYPE : uint
        {
            NONE,
            YAM_AND_GOLD,
            RAINBOW,
            RED,
            GREEN,
            YELLOW,
        }
        public uint id;
        public uint _typeid;
        public uint parts_typeid;
        public uint parts_id;
        public uint efeito;
        public uint efeito_qntd;
        public uint slot;
        public SYSTEMTIME use_date = new SYSTEMTIME();
        public PangyaTime_ItemBuff tempo = new PangyaTime_ItemBuff();
        public uint tipo;
        public byte use_yn;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(id);
                p.WriteUInt32(_typeid);
                p.WriteUInt32(parts_typeid);
                p.WriteUInt32(parts_id);
                p.WriteUInt32(efeito);
                p.WriteUInt32(efeito_qntd);
                p.WriteUInt32(slot);
                p.WriteTime(use_date);
                p.WriteTime(tempo);
                p.WriteUInt32(tipo);
                p.WriteByte(use_yn);

                return p.GetBytes;
            }
        }
    }

    // Item Buff Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ItemBuffEx : ItemBuff
    {
        public long index;
        public SYSTEMTIME end_date = new SYSTEMTIME();
        public uint percent;       // Rate, tipo 2 é 0 por que é 100 
    }

    // Guild Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GuildInfo
    {

        public int uid;
        public byte leadder;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] name_Bytes;
        public string name
        {
            get => name_Bytes.GetString();
            set => name_Bytes.SetString(value);
        }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] mark_img_Bytes;
        public string mark_img
        {
            get => mark_img_Bytes.GetString();
            set => mark_img_Bytes.SetString(value);
        }
        public uint index_mark_emblem;
        public ulong ull_unknown;
        public ulong pang;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] _16unknown;
        public uint point;
        public GuildInfo()
        {
            clear();
        }

        public void clear()
        {
            name_Bytes = new byte[20];
            mark_img_Bytes = new byte[12];
            _16unknown = new byte[16];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(uid);
                p.Write(leadder);
                p.WriteStr(name, 20);
                p.WriteStr(mark_img, 12);
                p.Write(index_mark_emblem);
                p.Write(ull_unknown);
                p.Write(pang);
                p.WriteBytes(_16unknown, 16);
                p.Write(point);
                return p.GetBytes;
            }
        }
    }


    public class CPLog
    {
        public enum TYPE : byte
        {
            BUY_SHOP,
            GIFT_SHOP,
            TICKER,
            CP_POUCH,    // Player ganha CP pela CP(Cookie Point) Pouch
        }

        public struct stItem
        {

            public uint _typeid;
            public int qntd;
            public ulong price;

            public stItem(uint _ul = 0u)
            {
                _typeid = 0;
                qntd = 0;
                price = 0;
                clear();
            }

            public stItem(uint __typeid, int _qntd, ulong _cp)
            {
                _typeid = __typeid;
                qntd = _qntd;
                price = _cp;
            }

            public void clear()
            {
                _typeid = 0;
                qntd = 0;
                price = 0;
            }
        }

        protected TYPE m_type;
        protected int m_mail_id;
        protected ulong m_cookie;
        protected List<stItem> v_item;

        public CPLog(uint _ul = 0u)
        {
            v_item = new List<stItem>();
            clear();
        }

        ~CPLog() { }

        public void clear()
        {
            m_type = TYPE.BUY_SHOP;
            m_mail_id = -1;
            m_cookie = 0UL;

            if (v_item != null && v_item.Count > 0)
            {
                v_item.Clear();
                v_item.TrimExcess();
            }
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public int getMailId()
        {
            return m_mail_id;
        }

        public void setMailId(int _mail_id)
        {
            m_mail_id = _mail_id;
        }

        public ulong getCookie()
        {
            ulong total = m_cookie;

            v_item.ForEach(el =>
            {
                total += el.price;
            });

            return total;
        }

        public void setCookie(ulong _cp)
        {
            m_cookie = _cp;
        }

        public uint getItemCount()
        {
            return (uint)v_item.Count;
        }

        public List<stItem> getItens()
        {
            return v_item;
        }

        public void putItem(uint _typeid, int _qntd, ulong _cp)
        {
            v_item.Add(new stItem(_typeid, _qntd, _cp));
        }

        public void putItem(stItem _item)
        {
            v_item.Add(_item);
        }

        public string toString()
        {
            StringBuilder str = new StringBuilder();

            str.Append("TYPE=").Append((ushort)m_type)
                .Append(", mail_id=").Append(m_mail_id)
                .Append(", cookie=").Append(getCookie())
                .Append(", item(ns) quantidade=").Append(v_item.Count);

            foreach (var el in v_item)
            {
                str.Append(", {TYPEID=").Append(el._typeid)
                   .Append(", QNTD=").Append(el.qntd)
                   .Append(", PRICE=").Append(el.price)
                   .Append("}");
            }

            return str.ToString();
        }
    }
    // GuildInfoEx
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GuildInfoEx : GuildInfo
    {
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string mark_emblem;
        public GuildInfoEx()
        {
            mark_emblem = "guildmark";
            clear();
        }

    }
    // Location
    public class Location
    {
        public void clear()
        {
            x = 0;
            y = 0;
            z = 0;
            r = 0; // face
        }
        public double diffXZ(Location _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(z - _l.z, 2));
        }
        public double diffXZ(ShotEndLocationData.stLocation _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(z - _l.z, 2));
        }
        public static double diffXZ(Location _l1, Location _l2)
        {
            return Math.Sqrt(Math.Pow(_l1.x - _l2.x, 2) + Math.Pow(_l1.z - _l2.z, 2));
        }
        public double diff(Cube.stLocation _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(y - _l.y, 2) + Math.Pow(z - _l.z, 2));
        }
        public double diff(ShotEndLocationData.stLocation _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(y - _l.y, 2) + Math.Pow(z - _l.z, 2));
        }
        public double diff(Location _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(y - _l.y, 2) + Math.Pow(z - _l.z, 2));
        }
        public static double diff(Location _l1, Location _l2)
        {
            return Math.Sqrt(Math.Pow(_l1.x - _l2.x, 2) + Math.Pow(_l1.y - _l2.y, 2) + Math.Pow(_l1.z - _l2.z, 2));
        }
        public static double diff(Location _l1, Cube.stLocation _l2)
        {
            return Math.Sqrt(Math.Pow(_l1.x - _l2.x, 2) + Math.Pow(_l1.y - _l2.y, 2) + Math.Pow(_l1.z - _l2.z, 2));
        }
        public string toString()
        {
            return "X: " + Convert.ToString(x) + " Y: " + Convert.ToString(y) + " Z: " + Convert.ToString(z) + " R: " + Convert.ToString(r);
        }

        public double diffXZ(ShotSyncData.Location _l)
        {
            return Math.Sqrt(Math.Pow(x - _l.x, 2) + Math.Pow(y - _l.y, 2) + Math.Pow(z - _l.z, 2));
        }

        public float x;
        public float y;
        public float z;
        public float r; // face

        public Location(float _x, float _y, float _z, float _r)
        {
            x = _x;
            y = _y;
            z = _z;
            r = _r; // face
        }
        public Location()
        {

        }

        public static implicit operator Location(ShotSyncData.Location loc)
        {
            return new Location()
            {
                x = loc.x,
                y = loc.y,
                z = loc.z,
            };
        }
    }

    // Canal Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ChannelInfo
    {
        public ChannelInfo()
        {
            clear();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UFlag
        {
            public uint ulFlag { get; set; }

            public bool all
            {
                get => (ulFlag & 1) != 0;
                set => ulFlag = Convert.ToUInt32(value ? (ulFlag | 1) : (ulFlag & ~1));
            }

            public bool Channel30S
            {
                get => (ulFlag & 2) != 0;
                set => ulFlag = value ? (ulFlag | 2u) : (ulFlag & ~2u);
            }

            public bool Ongamenet
            {
                get => (ulFlag & 4) != 0;
                set => ulFlag = value ? (ulFlag | 4u) : (ulFlag & ~4u);
            }

            public bool NoItem
            {
                get => (ulFlag & 8) != 0;
                set => ulFlag = value ? (ulFlag | 8u) : (ulFlag & ~8u);
            }

            public bool Random
            {
                get => (ulFlag & 16) != 0;
                set => ulFlag = value ? (ulFlag | 16u) : (ulFlag & ~16u);
            }

            public bool Adult
            {
                get => (ulFlag & 32) != 0;
                set => ulFlag = value ? (ulFlag | 32u) : (ulFlag & ~32u);
            }

            public bool Ladder
            {
                get => (ulFlag & 128) != 0;
                set => ulFlag = value ? (ulFlag | 128u) : (ulFlag & ~128u);
            }

            public bool Channel30IN9
            {
                get => (ulFlag & 256) != 0;
                set => ulFlag = value ? (ulFlag | 256u) : (ulFlag & ~256u);
            }

            public bool LowLevel// junior_bellow
            {
                get => (ulFlag & 512) != 0;
                set => ulFlag = (uint)(value ? (ulFlag | 512) : (ulFlag & ~512));
            }

            public bool HighLevel //junior_above
            {
                get => (ulFlag & 1024) != 0;
                set => ulFlag = (uint)(value ? (ulFlag | 1024) : (ulFlag & ~1024));
            }

            public bool only_rookie
            {
                get => (ulFlag & 2048) != 0;
                set => ulFlag = (uint)(value ? (ulFlag | 2048) : (ulFlag & ~2048));
            }

            public bool beginner
            {
                get => (ulFlag & 4096) != 0;
                set => ulFlag = (uint)(value ? (ulFlag | 4096) : (ulFlag & ~4096));
            }

            public bool senior
            {
                get => (ulFlag & 8192) != 0;
                set => ulFlag = (uint)(value ? (ulFlag | 8192) : (ulFlag & ~8192));
            }

            public bool Skins //[EVENT] 
            {
                get => (ulFlag & 16384) != 0;
                set => ulFlag = value ? (ulFlag | 16384u) : (ulFlag & ~16384u);
            }
        }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string name { get; set; }
        public short max_user { get; set; }
        public short curr_user { get; set; }
        public byte id { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public UFlag type { get; set; }
        public uint property { get; set; }
        public uint min_level_allow { get; set; }
        public uint max_level_allow { get; set; }

        public void clear()
        {
            type = new UFlag();
        }
        public byte[] ToArray()
        {
            using (var Response = new PangyaBinaryWriter())
            {
                Response.WriteStr(name, 64);
                Response.WriteInt16(max_user);
                Response.WriteInt16(curr_user);
                Response.WriteByte(id); //Lobby ID
                Response.WriteUInt32(type.ulFlag); //ルーム制限あるね- channel type
                Response.WriteUInt32(16); //メンテナンス表記+ナチュラルマーク- property  
                Response.WriteUInt32(min_level_allow); //メンテナンス表記+なんか    
                Response.WriteUInt32(max_level_allow); //メンテナンス表記+Granplix
                return Response.GetBytes;
            }
        }
    }
    public class ClientVersion
    {
        public char[] region = new char[3];
        public char[] season = new char[3];
        public uint high;
        public uint low;
        public bool flag;

        public const bool REDUZI_VERSION = false;
        public const bool COMPLETE_VERSION = true;

        public ClientVersion()
        {
            Array.Clear(region, 0, region.Length);
            Array.Clear(season, 0, season.Length);
            high = 0;
            low = 0;
            flag = REDUZI_VERSION;
        }

        public ClientVersion(uint _high, uint _low)
        {
            Array.Clear(region, 0, region.Length);
            Array.Clear(season, 0, season.Length);
            high = _high;
            low = _low;
            flag = REDUZI_VERSION;
        }

        public ClientVersion(string _region, string _season, uint _high, uint _low)
        {
            if (_region == null || _season == null)
                throw new Exception("Error argument invalid, _region or _season is null. ClientVersion::ClientVersion()");

            Array.Clear(region, 0, region.Length);
            Array.Clear(season, 0, season.Length);

            if (_region.Length != 2 || _season.Length != 2)
                throw new Exception("Error _region or _season length != 2");

            region[0] = _region[0];
            region[1] = _region[1];

            season[0] = _season[0];
            season[1] = _season[1];

            high = _high;
            low = _low;
            flag = COMPLETE_VERSION;
        }

        public static ClientVersion MakeVersion(string _cv)
        {
            if (string.IsNullOrEmpty(_cv))
                throw new Exception("Error cv is empty, ClientVersion::make_version()");

            string[] tokens = _cv.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                throw new Exception("Error Invalid argument. ClientVersion::make_version()");

            if (tokens.Length < 2)
                throw new Exception("Error Not string token enough, ClientVersion::make_version()");

            try
            {
                if (tokens.Length == 2)
                {
                    return new ClientVersion(
                        Convert.ToUInt32(tokens[0]),
                        Convert.ToUInt32(tokens[1])
                    );
                }
                else if (tokens.Length == 4)
                {
                    if (tokens[0].Length != 2 || tokens[1].Length != 2)
                        throw new Exception("Error region or season token length != 2");

                    return new ClientVersion(
                        tokens[0],
                        tokens[1],
                        Convert.ToUInt32(tokens[2]),
                        Convert.ToUInt32(tokens[3])
                    );
                }
                else
                {
                    throw new Exception("Error unexpected token string. ClientVersion::make_version()");
                }
            }
            catch (FormatException e)
            {
                throw new Exception("Error invalid argument Convert.ToUInt32(), ClientVersion::make_version(). " + e.Message);
            }
            catch (OverflowException e)
            {
                throw new Exception("Error out of range Convert.ToUInt32(), ClientVersion::make_version(). " + e.Message);
            }
        }

        private string FixedValue(uint _value, uint _width)
        {
            return _value.ToString().PadLeft((int)_width, '0');
        }

        public override string ToString()
        {
            return new string(region) + "." + new string(season) + "." + FixedValue(high, 2) + "." + FixedValue(low, 2);
        }
    }
}
