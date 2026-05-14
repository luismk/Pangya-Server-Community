//Convertion By LuisMK
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Game.Manager
{
    /// <summary>
    /// Manipulation Add, Check, Remove Items
    /// </summary>
    public class ItemManager
    {
        public class RetAddItem
        {
            public const int T_INIT_VALUE = -5;
            public const int T_ERROR = -4;
            public const int TR_SUCCESS_WITH_ERROR = -3;
            public const int TR_SUCCES_PANG_AND_EXP_AND_CP_WITH_ERROR = -2;
            public const int TR_SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR = -1;
            public const int T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH = 0;
            public const int T_SUCCESS = 1;

            public RetAddItem()
            {
                clear();
            }

            public void clear()
            {

                if (!fails.Any())
                {
                    fails.Clear();
                }
                type = T_INIT_VALUE;
            }
            public List<stItem> fails = new List<stItem>();
            public int type = 1;
        }
        public ItemManager()
        {
        }


        private static void TRANSFER_ITEM_SUCESS_MSG_LOG(string log, Player _s_snd, Player _s_rcv, PersonalShopItem _psi, [CallerMemberName] string func = "")
        {
#if DEBUG
            type_msg MSG_ACTIVE_DEBUG = type_msg.CL_FILE_LOG_AND_CONSOLE;
#else
    type_msg MSG_ACTIVE_DEBUG = type_msg.CL_ONLY_FILE_LOG;
#endif

            _smp.message_pool.getInstance().push(new message($"[ItemManager::{func}][Sucess] Player_s[UID=" + (_s_snd.m_pi.uid) + "] " + log + "[Typeid="
            + (_psi.item._typeid) + ", ID=" + (_psi.item.id) + ", QNTD=" + (_psi.item.qntd) + ", PANG(qntd*unit)="
            + (_psi.item.pang * (ulong)_psi.item.qntd) + "] transferiu para o Player_r[UID=" + (_s_rcv.m_pi.uid) + "]", MSG_ACTIVE_DEBUG));

        }

        private static void ITEM_MANAGER_LOG(string log, Player _session, stItem _item, [CallerMemberName] string func = "")
        {
#if DEBUG
            type_msg MSG_ACTIVE_DEBUG = type_msg.CL_FILE_LOG_AND_CONSOLE;
#else
    type_msg MSG_ACTIVE_DEBUG = type_msg.CL_ONLY_FILE_LOG;
#endif

            _smp.message_pool.getInstance().push(new message(
                $"[ItemManager::{func}][Sucess] PLAYER[UID={_session.m_pi.uid}] {log} [Typeid={_item._typeid}, ID={_item.id}, QNTD={(_item.STDA_C_ITEM_QNTD > 0 && _item.qntd <= 0xFFu ? _item.STDA_C_ITEM_QNTD : _item.qntd)}]",
                MSG_ACTIVE_DEBUG));
        }


        static void INIT_COMMOM_BUYITEM(ref stItem _item, BuyItem _bi, IFFCommon item)
        {
            _item.id = _bi.id;
            _item._typeid = _bi._typeid;
            _item.date.date.sysDate[0] = new SYSTEMTIME(item.date.Start.Year, item.date.Start.Month, item.date.Start.Day, item.date.Start.Hour, item.date.Start.Minute, item.date.Start.Second, item.date.Start.MilliSecond);//check start later
            _item.date.date.sysDate[1] = new SYSTEMTIME(item.date.End.Year, item.date.End.Month, item.date.End.Day, item.date.End.Hour, item.date.End.Minute, item.date.End.Second, item.date.End.MilliSecond);//check End later
            _item.date.active = Convert.ToUInt32(item.date.active);//check active date later
            _item.price = item.Shop.Price;
            _item.desconto = item.Shop.DiscountPrice;
            _item.qntd = (int)(int)_bi.qntd;
            _item.is_cash = (byte)(item.Shop.flag_shop.IsCash ? 1 : 0);
            _item.type = 2; /*aqui é o valor padrão, mas outros iff pode mexer nele depois*/
        }

        static void BEGIN_INIT_BUYITEM(PlayerInfo _pi, ref stItem _item, BuyItem _bi, bool _gift_opt, bool _chk_lvl, IFFCommon item)
        {
            if (item != null)
            {
                if (item.ID == 0)
                {
                    item = sIff.getInstance().findCommomItem(_bi._typeid);
                }
                CHECK_LEVEL_ITEM(_pi, ref _item, _gift_opt, _chk_lvl, item);
                CHECK_IS_GIFT(_pi, ref _item, _gift_opt, _chk_lvl, item);
                INIT_COMMOM_BUYITEM(ref _item, _bi, item);
            }
            else
                _smp.message_pool.getInstance().push(new message($"[ItemManager::BEGIN_INIT_BUYITEM][Error] UID={_pi.uid}, TID={_bi._typeid}] ITEM NOT FOUND", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        static void CHECK_LEVEL_ITEM(PlayerInfo _pi, ref stItem _item, bool _gift_opt, bool _chk_lvl, IFFCommon item)
        {
            if (!_gift_opt && !_chk_lvl && !item.Level.GoodLevel((byte)_pi.level))
            {
                _smp.message_pool.getInstance().push(new message("[Log] PLAYER[UID=" + (_pi.uid)

                        + "] nao tem o level[value=" + (item.Level) + "] necessario para comprar esse item[TYPEID=" + (item.ID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                _item._typeid = 0;
                return;
            }
        }

        static void CHECK_IS_GIFT(PlayerInfo _pi, ref stItem _item, bool _gift_opt, bool _chk_lvl, IFFCommon item)
        {
            if (_gift_opt && !sIff.getInstance().IsGiftItem(item.ID))
            {
                _smp.message_pool.getInstance().push(new message("[Log] PLAYER[UID=" + (_pi.uid) + "] tentou presentear um item que não pode ser presenteado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                _item._typeid = 0;
                return;
            }
        }

        static int STDA_TRANSLATE_FLAG_TIME(int flagTime, int value)
        {
            if (flagTime == 0x20 || flagTime == 2 || flagTime == 3)
                return value * 60 * 60; // Hour
            else if (flagTime == 0x10 || flagTime == 0x40 || flagTime == 0x60 || flagTime == 1 || flagTime == 4 || flagTime == 6)
                return value * 24 * 60 * 60; // Day
            else if (flagTime == 0x50 || flagTime == 5)
                return value * 30 * 24 * 60 * 60; // 30 Days
            else
                return 0;
        }

        static int STDA_TRANSLATE_FLAG_TIME_TO_HOUR(int flagTime, int value)
        {
            return STDA_TRANSLATE_FLAG_TIME(flagTime, value) / 3600;
        }

        // Gets
        public static List<stItem> getItemOfSetItem(Player _session, uint _typeid, bool _shop, int _chk_level)
        {

            if (!isSetItem(_typeid))
            {
                throw new exception("[item_manager::getItemOfSetItem][Error] item[TYPEID=" + Convert.ToString(_typeid) + "] not is a valid SetItem. Player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    1, 0));
            }

            List<stItem> v_item = new List<stItem>();
            stItem item = new stItem();
            BuyItem bi = new BuyItem();

            SetItem set_item = sIff.getInstance().findSetItem(_typeid);

            if (set_item == null)
            {
                throw new exception("[item_manager::getItemOfSetItem][Error] item[TYPEID=" + Convert.ToString(_typeid) + "] nao foi encontrado. Player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    2, 0));
            }

            for (var i = 0; i < set_item.packege.Total; ++i)
            {
                if (set_item.packege.item_typeid[i] != 0)
                {
                    bi.clear();
                    item.clear();
                    bi.id = -1;
                    bi._typeid = set_item.packege.item_typeid[i];
                    bi.qntd = set_item.packege.item_qntd[i];

                    initItemFromBuyItem(_session.m_pi,
                        item, bi, _shop, 0, 0,
                        _chk_level);

                    if (item._typeid != 0)
                    {
                        v_item.Add(new stItem(item));
                    }
                    else
                    {
                        throw new exception("[item_manager::getItemOfSetItem][Error] erro ao inicializar item[TYPEID=" + Convert.ToString(set_item.packege.item_typeid[i]) + "]. Player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                            25, 0));
                    }
                }
            }

            return new List<stItem>(v_item);
        }

        // Init Item From Buy --Gift opt-- Item
        public static void initItemFromBuyItem(PlayerInfo _pi, stItem _item, BuyItem _bi, bool _shop, int _option, int _gift_opt = 0, int _chk_lvl = 0)
        {

            // Limpa o _item
            _item.clear();

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_bi._typeid))
            {
                case IFF_GROUP.CHARACTER:
                    {
                        var item = sIff.getInstance().findCharacter(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);


                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var item = sIff.getInstance().findPart(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_option == 1 && item.valor_rental > 0)
                        {
                            _item.price = item.valor_rental;
                            _item.is_cash = 0; // Pang, por que é rental
                            _item.STDA_C_ITEM_TIME = 7; // 7 dias    // no original é no C[3] o tempos
                            _item.flag = 0x60; // dias Rental(acho)
                                               // time tipo 6 rental, 4, 2,
                            _item.flag_time = 6;
                        }
                        else if (_bi.time > 0)
                        { // Roupa de tempo do CadieCauldron

                            if (item.Shop.flag_shop.time_shop.active)
                            {
                                _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][WARNIG] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] inicializou Part[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo[VALUE=" + Convert.ToString(_bi.time) + "], mas no IFF_STRUCT do server ele nao é um item por tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                            _item.STDA_C_ITEM_TIME = (short)_bi.time;
                            _item.flag = 0x20;
                            _item.flag_time = 2;
                        }

                        // UCC
                        if (item.IsUCC())
                        {
                            _item.flag = 5;
                        }

                        _item.type_iff = (byte)item.type_item;



                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        var item = sIff.getInstance().findClubSet(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_bi.time > 0)
                        {

                            _item.qntd = (int)1;
                            _item.flag = 0x20;
                            _item.flag_time = 2;
                            _item.STDA_C_ITEM_TIME = (short)((ushort)(_bi.time * 24));
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var item = sIff.getInstance().findBall(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        for (int i = 0; i < _item.c.Length; i++)
                        {
                            _item.c[i] = (short)item.Stats.getSlot[i];
                        }


                        if (_chk_lvl != 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }
                        else if (_item.qntd != item.Stats.getSlot[0])
                        {

                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;

                            _item.qntd = (int)1;

                        }
                        else
                        {

                            if (_chk_lvl == 0
                                && _item.qntd > 0
                                && item.Stats.getSlot[0] != 0)
                            {
                                _item.qntd /= item.Stats.getSlot[0];
                            }

                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.ITEM:
                    {
                        var item = sIff.getInstance().findItem(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //antes esse codigo, estava abaixo da verificacao, por isso estava
                        //vindo zerado, o objeto, precisa ser carregado, se nao dar bug
                        // Copia C[] do IFF::Item para o _item
                        for (int i = 0; i < _item.c.Length; i++)
                        {
                            _item.c[i] = (short)item.Stats.getSlot[i];
                        }


                        // essa regra é para os itens que é 1 item, mas tem mais quantidade que vai ser add, 1 item normal, mas ele vem 10, fica no STDA_C_ITEM_QNTD
                        if (_item.qntd > 0 && _item.STDA_C_ITEM_QNTD != 0 && (_item.STDA_C_ITEM_QNTD == 1 || _item.STDA_C_ITEM_QNTD == _item.qntd))
                            _item.qntd /= (int)_item.STDA_C_ITEM_QNTD;


                        // Tem preço de tempo
                        var empty_price = sIff.getInstance().EMPTY_ARRAY_PRICE(_item.c);

                        if (_bi.time > 0
                            && !empty_price
                            && sIff.getInstance().getEnchantSlotStat(_item._typeid) == 0x21)
                        {

                            if (item.Shop.flag_shop.time_shop.active && item.Shop.flag_shop.time_shop.dia > 0)
                            {

                                switch (_bi.time)
                                {
                                    case 1:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[0] : _bi.pang == _item.c[0])
                                        {
                                            _item.price = (uint)_item.c[0];
                                        }
                                        break;
                                    case 7:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[1] : _bi.pang == _item.c[1])
                                        {
                                            _item.price = (uint)_item.c[1];
                                        }
                                        break;
                                    case 15:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[2] : _bi.pang == _item.c[2])
                                        {
                                            _item.price = (uint)_item.c[2];
                                        }
                                        break;
                                    case 30:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[3] : _bi.pang == _item.c[3])
                                        {
                                            _item.price = (uint)_item.c[3];
                                        }
                                        break;
                                    case 365:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[4] : _bi.pang == _item.c[4])
                                        {
                                            _item.price = (uint)_item.c[4];
                                        }
                                        break;
                                    default:
                                        _bi.time = 1; // 1 dia, Coloca o menor
                                        break;
                                }

                                _item.qntd = 1; //item->Shop.flag_shop.time_shop.uc_time_start;
                                _item.flag = 0x20;
                                _item.flag_time = 2;
                                _item.STDA_C_ITEM_TIME = (_bi.time * 24); // Premium Ticket

                                if (_bi.time > 365)
                                {
                                    _smp.message_pool.getInstance().push(new message("[Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "]. Queria colocar mais[request=" + Convert.ToString(_bi.time) + "] que 365 dia na compra do Premium Ticket. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }

                        }
                        else if (_bi.time == 0 && !empty_price && sIff.getInstance().getEnchantSlotStat(_item._typeid) == 0x21 && _bi.qntd > 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _bi.time = (short)(_bi.qntd > 365 ? 365 : _bi.qntd);

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            if (item.Shop.flag_shop.time_shop.active && item.Shop.flag_shop.time_shop.dia > 0)
                            {

                                switch (_bi.time)
                                {
                                    case 1:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[0] : _bi.pang == _item.c[0])
                                        {
                                            _item.price = (uint)_item.c[0];
                                        }
                                        break;
                                    case 7:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[1] : _bi.pang == _item.c[1])
                                        {
                                            _item.price = (uint)_item.c[1];
                                        }
                                        break;
                                    case 15:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[2] : _bi.pang == _item.c[2])
                                        {
                                            _item.price = (uint)_item.c[2];
                                        }
                                        break;
                                    case 30:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[3] : _bi.pang == _item.c[3])
                                        {
                                            _item.price = (uint)_item.c[3];
                                        }
                                        break;
                                    case 365:
                                        if (_item.is_cash == 1 ? _bi.cookie == _item.c[4] : _bi.pang == _item.c[4])
                                        {
                                            _item.price = (uint)_item.c[4];
                                        }
                                        break;
                                    default:
                                        _bi.time = 1; // 1 dia, coloca o menor
                                        break;
                                }

                                _item.qntd = (int)1; //item->Shop.flag_shop.time_shop.uc_time_start;
                                _item.flag = 0x20;
                                _item.flag_time = 2;
                                _item.STDA_C_ITEM_TIME = (short)(_bi.time * 24); // Premium Ticket

                                if (_bi.time > 365)
                                {
                                    _smp.message_pool.getInstance().push(new message("[Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "]. Queria colocar mais[request=" + Convert.ToString(_bi.time) + "] que 365 dia na compra do Premium Ticket. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }
                        }

                        if ((item.ID == 0x1A000081 || item.ID == 0x1A000082 || item.ID == 0x1A0003B3))
                        {
                            _item._typeid = 0x1A000080; // Typeid do Coupon Gacha Single, por que os outros é item pack, em um item
                        }

                        if (_item.STDA_C_ITEM_QNTD <= 0 || _item.STDA_C_ITEM_QNTD < (int)_item.qntd)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        if ((!_shop || _item.qntd == 0) && _item.STDA_C_ITEM_QNTD != _bi.qntd)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_bi.qntd;
                            _item.qntd = (int)_bi.qntd;
                        }

                        if (sIff.getInstance().IsItemEquipable(_bi._typeid))
                        { // Equiável
                        }
                        else
                        { // Passivo

                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.CADDIE:
                    {
                        var item = sIff.getInstance().findCaddie(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (item.valor_mensal > 0)
                        {
                            _item.date_reserve = 30; // 30 dias
                            for (int i = 0; i < _item.c.Length; i++)
                            {
                                _item.c[i] = (short)item.Stats.getSlot[i];
                            }
                            _item.flag = 0x20; // Time de dias( acho que seja o 0x20, não lembro mais)
                            _item.flag_time = 2;

                            _item.STDA_C_ITEM_TIME = (short)_item.date_reserve; // Caddie depois que add, só colocar o time novamente
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.CAD_ITEM:
                    {
                        var item = sIff.getInstance().findCaddieItem(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.getInstance().EMPTY_ARRAY_PRICE(item.price);

                        if (_bi.time > 0 && !empty_price)
                        {

                            switch (_bi.time)
                            { // Dias
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 2;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)(_bi.time * 24); // Horas

                        }
                        else if (_bi.time > 0 && (item.Shop.flag_shop.time_shop.active || item.Shop.flag_shop.time_shop.dia > 0))
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] inicializou Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd > 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _bi.time = (short)(_bi.qntd > 30 ? 30 : _bi.qntd);

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            switch (_bi.time)
                            { // Dias
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                                default: // Não passou a quantidade de dias certo manda a soma de todos os preços que tem no iff
                                    _item.price = sIff.getInstance().SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 2;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)(_bi.time * 24); // Horas

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Error] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    {
                        var item = sIff.getInstance().findSetItem(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        var item = sIff.getInstance().findSkin(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // ESSE AQUI É ONDE COMEÇA OS TEMPO, [1] É DO 1 A 365 DIAS, [7] É DO 7 A 365 DIAS
                        //item->Shop.flag_shop.time_shop.uc_time_start //---- AS SKINS É DO 7

                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.getInstance().EMPTY_ARRAY_PRICE(item.price);

                        if (_bi.time > 0 && !empty_price)
                        {

                            switch (_bi.time)
                            { // Dias
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                                case 365:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[4] : _bi.pang == item.price[4])
                                    {
                                        _item.price = item.price[4];
                                    }
                                    break;
                                default: // Não passou a quantidade de dias certo manda a soma de todos os preços que tem no iff
                                    _item.price = sIff.getInstance().SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.flag_time = 4;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time > 0 && (item.Shop.flag_shop.time_shop.active || item.Shop.flag_shop.time_shop.dia > 0))
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] inicializou Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd > 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _bi.time = (short)(_bi.qntd > 365 ? 365 : _bi.qntd);

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            switch (_bi.time)
                            { // Dias
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                                case 365:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[4] : _bi.pang == item.price[4])
                                    {
                                        _item.price = item.price[4];
                                    }
                                    break;
                                default: // Não passou a quantidade de dias certo manda a soma de todos os preços que tem no iff
                                    _item.price = sIff.getInstance().SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.flag_time = 4;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Error] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.HAIR_STYLE:
                    {
                        var item = sIff.getInstance().findHairStyle(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var item = sIff.getInstance().findMascot(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // Dias
                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.getInstance().EMPTY_ARRAY_PRICE(item.price);

                        if (_bi.time > 0 && !empty_price)
                        {

                            switch (_bi.time)
                            {
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                                case 360:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[4] : _bi.pang == item.price[4])
                                    {
                                        _item.price = item.price[4];
                                    }
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time > 0 && (item.Shop.flag_shop.time_shop.active || item.Shop.flag_shop.time_shop.dia > 0))
                        {

                            if (_shop
                                && !sIff.getInstance().IsBuyItem(item.ID)
                                && !sIff.getInstance().IsGiftItem(item.ID))
                            {
                                _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] inicializou Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd > 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Warning] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _bi.time = (short)(_bi.qntd > 365 ? 365 : _bi.qntd);

                            // Qntd tem que ser 1 por que o item é por tempo
                            if (_bi.qntd > 1)
                            {
                                _item.qntd = (int)(_bi.qntd = 1);
                            }

                            switch (_bi.time)
                            {
                                case 1:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[0] : _bi.pang == item.price[0])
                                    {
                                        _item.price = item.price[0];
                                    }
                                    break;
                                case 7:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[1] : _bi.pang == item.price[1])
                                    {
                                        _item.price = item.price[1];
                                    }
                                    break;
                                case 15:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[2] : _bi.pang == item.price[2])
                                    {
                                        _item.price = item.price[2];
                                    }
                                    break;
                                case 30:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[3] : _bi.pang == item.price[3])
                                    {
                                        _item.price = item.price[3];
                                    }
                                    break;
                                case 365:
                                    if (_item.is_cash == 1 ? _bi.cookie == item.price[4] : _bi.pang == item.price[4])
                                    {
                                        _item.price = item.price[4];
                                    }
                                    break;
                                default: // Não passou a quantidade de dias certo manda a soma de todos os preços que tem no iff
                                    _item.price = sIff.getInstance().SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[item_manager::initItemFromBuyItem][Error] PLAYER[UID=" + Convert.ToString(_pi.uid) + "] tentou inicializar Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var item = sIff.getInstance().findFurniture(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD == 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.AUX_PART:
                    {
                        var item = sIff.getInstance().findAuxPart(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD == 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var item = sIff.getInstance().findCard(_bi._typeid);

                        BEGIN_INIT_BUYITEM(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD <= 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                default: // Não tem esse item para vender no shop
                    _smp.message_pool.getInstance().push(new message("PLAYER[UID=" + Convert.ToString(_pi.uid) + "] Tentou comprar um item que nao tem no shop para vender. typeid: " + Convert.ToString(_bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    break;
            }
        }

        public static void initItemFromEmailItem(PlayerInfo _pi, stItem _item, EmailInfo.item _ei_item)
        {

            BuyItem item = new BuyItem();

            item.id = (int)_ei_item.id;
            item._typeid = _ei_item._typeid;
            item.qntd = _ei_item.qntd;
            item.time = (short)(_ei_item.flag_time == 2 ? _ei_item.tempo_qntd / 24 : _ei_item.tempo_qntd);

            _item.flag_time = _ei_item.flag_time;

            // Aqui tem que criar o proprio dele por que tem o tipo do tempo[dias, horas, minutos, segundos] e etc
            initItemFromBuyItem(_pi,
                _item, item, false, 0, 0, 1);
        }

        // Check is have setitem in email
        public static void checkSetItemOnEmail(Player _session, EmailInfo _ei)
        {

            if (_ei.itens.empty() && _ei.id <= 0)
            {
                throw new exception("[item_manager::checkSetItemOnEmail][Error] email not have item for check", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    20, 0));
            }

            for (var i = 0; i < _ei.itens.Count; ++i)
            {
                if (isSetItem(_ei.itens[i]._typeid))
                {
                    var v_item = getItemOfSetItem(_session,
                        _ei.itens[i]._typeid, false, 1);

                    if (!v_item.empty())
                    {

                        foreach (var el in v_item)
                            _ei.itens.Add(new EmailInfo.item(-1, el._typeid, el.flag_time, (uint)el.qntd, (ushort)el.STDA_C_ITEM_TIME, 0, 0, 0/*type GM*/, 0, "", 0));

                        _ei.itens.RemoveAt(i--);
                    }
                }
            }
        }

        // Add Itens

        public static int addItem(stItem _item, uint _uid, /*era byte*/ byte _gift_flag, byte _purchase, bool _dup = false)
        {
            int ret_id = RetAddItem.T_ERROR;

            try
            {

                // Block Memória para o UID, para garantir que não vai adicionar itens simuntaneamente
                BlockMemoryManager.blockUID(_uid);

                // Error Grave lança uma excessa
                if (_uid == 0)
                {
                    throw new exception("[item_manager::addItem][Error] uid invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        8, 0));
                }

                if (_item._typeid == 0)
                {
                    throw new exception("[item_manager::addItem][Error] item invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        9, 0));
                }

                switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_item._typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        {

                            if (ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] add um character[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            CharacterInfo ce = new CharacterInfo();
                            ce.id = (int)_item.id;
                            ce._typeid = _item._typeid;

                            ce.initComboDef();

                            // Aqui tem que add em uma fila e manda pra query e depois ver se foi concluida, enquanto verifica outras coisas[
                            // E ESSA CLASSE NÃO PODE SER STATIC, POR QUE TEM QUE GUARDA UNS VALORE NECESSÁRIOS

                            // Add no banco de dados
                            var cmd_ac = new CmdAddCharacter(_uid, // Waitable
                                ce, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_ac, null, null);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ce = cmd_ac.getInfo();
                            _item.id = (int)ce.id;

                            if (ce.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o character[TYPEID=" + Convert.ToString(ce._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            ret_id = RetAddItem.T_SUCCESS;//ce.id;

                            break;
                        }
                    case IFF_GROUP.CADDIE:
                        {

                            if (ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add um caddie[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possi.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            CaddieInfoEx ci = new CaddieInfoEx();
                            ci.id = _item.id;
                            ci._typeid = _item._typeid;
                            ci.check_end = 1; // Yes;
                            ci.rent_flag = 1; // 1 Normal sem ferias(tempo), 2 com ferias(tempo)

                            if (_item.date_reserve > 0)
                            {
                                ci.rent_flag = 2;
                                ci.end_date_unix = (ushort)_item.date_reserve; //(_item.type == 0x20) ? _item.STDA_C_ITEM_TIME : (_item.type == 0x40) ? _item.STDA_C_ITEM_TIME * 60 * 60 : _item.STDA_C_ITEM_TIME;
                            }

                            CmdAddCaddie cmd_ac = new CmdAddCaddie(_uid, // Waitable
                                ci, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(2,
                                cmd_ac, null, null);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _item.id = (int)ci.id;

                            if (ci.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o caddie[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            ret_id = RetAddItem.T_SUCCESS;//ci.id;

                            break;
                        }
                    case IFF_GROUP.CAD_ITEM:
                        {
                            uint cad_typeid = (uint)((Convert.ToUInt32(sIff.getInstance().CADDIE) << 26) | sIff.getInstance().getCaddieIdentify(_item._typeid));

                            var ci = _ownerCaddieItem(_uid, _item._typeid);

                            if (!(ci.id > 0))
                            {
                                throw new exception("[itme_manager::addItem][Log] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou comprar um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + "] sem o caddie[TYPEID=" + Convert.ToString(cad_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    14, 0));
                            }

                            long end_date = 0;

                            if (ci.parts_typeid == _item._typeid)
                            { // Já tem o parts caddie, atualiza o tempo

                                // Adiciona o Tempo em Unix Time Stamp
                                end_date = UtilTime.SystemTimeToUnix(ci.end_parts_date) + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME); //(((_item.flag_time == 2) ? _item.STDA_C_ITEM_TIME : _item.STDA_C_ITEM_TIME * 24) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                // update Parts End Date Unix
                                ci.updatePartsEndDate();

                                _item.STDA_C_ITEM_TIME = (short)((_item.flag_time == 2) ? ci.parts_end_date_unix : ci.parts_end_date_unix * 24);

                                _item.id = ci.id;

                                ret_id = RetAddItem.T_SUCCESS;//ci.id;


                            }
                            else
                            {

                                // Não tem o caddie parts ainda, add
                                ci.parts_typeid = _item._typeid;

                                end_date = UtilTime.GetLocalTimeAsUnix() + ((ci.parts_end_date_unix = (short)STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME)) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                _item.id = ci.id;

                                ret_id = RetAddItem.T_SUCCESS;//ci.id;

                            }

                            var str_end_date = UtilTime.FormatDate(ci.end_parts_date);

                            // Atualiza no para os 2 aqui
                            snmdb.NormalManagerDB.getInstance().add(5,
                                new CmdUpdateCaddieItem(_uid,
                                    str_end_date, ci),
                                SQLDBResponse,
                                null);

                            break;
                        }
                    case IFF_GROUP.MASCOT:
                        {
                            var mascot = sIff.getInstance().findMascot(_item._typeid);

                            if (mascot == null)
                            {
                                throw new exception("[item_manager::addItem][Erorr] mascot[TYPEID=" + Convert.ToString(_item._typeid) + "] nao foi encontrado no IFF_STRUCT do server, para o PLAYER[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            var pMi = _ownerMascot(_uid, _item._typeid);

                            if ((pMi.id > 0))
                            { // Player já tem o mascot, só add mais tempo à ele

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {

                                    var unix_time = UtilTime.SystemTimeToUnix(pMi.data);

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local time for verify server on local time
                                    pMi.data = (UtilTime.UnixUTCToTzLocalTime(unix_time));

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;

                                    // System Time Struct is Local Time 
                                    _item.date.date.sysDate[0].CreateTime();

                                    _item.date.date.sysDate[1] = pMi.data;

                                    var str_date = UtilTime._formatDate(pMi.data);

                                    // Cmd update time mascot db
                                    snmdb.NormalManagerDB.getInstance().add(6,
                                        new CmdUpdateMascotTime(_uid,
                                            pMi.id, str_date),
                                        SQLDBResponse,
                                        null);
                                }

                                _item.id = pMi.id;

                                ret_id = RetAddItem.T_SUCCESS;//_item.id;

                            }
                            else
                            {
                                MascotInfoEx mi = new MascotInfoEx();
                                mi.id = _item.id;
                                mi._typeid = _item._typeid;
                                mi.is_cash = _item.is_cash;
                                mi.price = _item.price;
                                mi.tipo = 0; // Padrão, é os mascot que não tem tempo
                                mi.message = "";
                                if (mascot.msg.active)
                                {
                                    mi.message = "PangYa SuperSS";
                                }

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {
                                    mi.tipo = 1; // Mascot de Tempo
                                }

                                var cmd_am = new CmdAddMascot(_uid, // Waiter
                                    mi, _item.STDA_C_ITEM_TIME,
                                    _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_am, null, null);

                                if (cmd_am.getException().getCodeError() != 0)
                                {
                                    throw cmd_am.getException();
                                }

                                mi = cmd_am.getInfo();
                                _item.id = (int)mi.id;

                                if (mi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Mascot[TYPEID=" + Convert.ToString(mi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {

                                    var unix_time = UtilTime.GetSystemTimeAsUnix();

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local time for verify server on local time
                                    mi.data = UtilTime.UnixUTCToTzLocalTime(unix_time);

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;

                                    // System Time Struct is Local Time
                                    UtilTime.GetLocalTime(out DateTime time);

                                    _item.date.date.sysDate[0].CreateTime(time);

                                    _item.date.date.sysDate[1] = mi.data;
                                }

                                ret_id = RetAddItem.T_SUCCESS;//mi.id;

                            }

                            break;
                        }
                    case IFF_GROUP.BALL:
                        {
                            var pWi = _ownerBall(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            { // já tem atualiza quantidade

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;

                                snmdb.NormalManagerDB.getInstance().add(7,
                                    new CmdUpdateBallQntd(_uid,
                                        pWi.id, pWi.STDA_C_ITEM_QNTD),
                                    SQLDBResponse,
                                    null);
                            }
                            else
                            { // não tem, add

                                WarehouseItemEx wi = new WarehouseItemEx();
                                wi.id = _item.id;
                                wi._typeid = _item._typeid;

                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;
                                wi.c = new stItem(_item).c;


                                wi.ano = -1;

                                CmdAddBall cmd_ab = new CmdAddBall(_uid, // Waiter
                                    wi, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ab, null, null);


                                if (cmd_ab.getException().getCodeError() != 0)
                                {
                                    throw cmd_ab.getException();
                                }

                                wi = cmd_ab.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Ball[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = (int)wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;
                            }

                        }
                        break;
                    case IFF_GROUP.CLUBSET:
                        {

                            var clubset = sIff.getInstance().findClubSet(_item._typeid);

                            if (clubset == null)
                            {
                                throw new exception("[item_manager::addItem][Error] clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] set nao foi encontrado no IFF_STRUCT do server, para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pWi = _ownerItem(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            { // Já tem, verifica se é de tempo e atualizar, se não dá error


                                // Add Mais tempo no Club Set
                                if (_item.STDA_C_ITEM_TIME > 0
                                    && ((pWi.flag & 0x20) != 0 || (pWi.flag & 0x40) != 0 || (pWi.flag & 0x60) != 0)
                                    && pWi.end_date_unix_local > 0)
                                {

                                    _item.date.active = 1;

                                    // update ano (Horas) que o item ainda tem
                                    pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                    // Só atualiza o Apply date se não tiver
                                    if (pWi.apply_date_unix_local == 0u)
                                    {

                                        pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                        // Convert to UTC to send client
                                        pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                    }

                                    pWi.end_date_unix_local = (uint)(UtilTime.GetLocalTimeAsUnix() + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));

                                    // Atualiza o tempo do ClubSet do player
                                    snmdb.NormalManagerDB.getInstance().add(20,
                                        new CmdUpdateClubSetTime(_uid, pWi),
                                        SQLDBResponse,
                                        null);

                                    _item.STDA_C_ITEM_QNTD = 1;
                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = _item.qntd;

                                    _item.id = pWi.id;
                                    ret_id = RetAddItem.T_SUCCESS;//pWi.id;


                                }
                                else
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));

                            }
                            else
                            {

                                WarehouseItemEx wi = new WarehouseItemEx();

                                wi.id = _item.id;
                                wi._typeid = _item._typeid;
                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME = _item.STDA_C_ITEM_TIME;
                                }

                                wi.clubset_workshop.level = clubset.work_shop.tipo; // Cv 1 e etc

                                if (wi.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                                }

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                if (_gift_flag == 1 && wi.id > 0)
                                {
                                    CmdGetGiftClubSet cmd_ggcs = new CmdGetGiftClubSet(_uid, // Waiter
                                        wi);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ggcs, null, null);

                                    if (cmd_ggcs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ggcs.getException();
                                    }

                                    wi = cmd_ggcs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu pegar o presente de ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + "] para o PLAYER[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            13, 0));
                                    }

                                }
                                else
                                {
                                    CmdAddClubSet cmd_acs = new CmdAddClubSet(_uid, // Waiter
                                        wi, _purchase, 0);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_acs, null, null);

                                    if (cmd_acs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_acs.getException();
                                    }

                                    wi = cmd_acs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    // Tenho que mexer nessas flags direitinho, por que aqui só está a de 0x60 e 0x20
                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            }

                        }

                        break;
                    case IFF_GROUP.CARD:
                        {
                            var pCi = _ownerCard(_uid, _item._typeid);

                            if ((pCi.id > 0))
                            { // Já tem o item atualiza quantidade

                                _item.stat.qntd_ant = pCi.qntd;

                                pCi.qntd += _item.qntd;

                                _item.stat.qntd_dep = pCi.qntd;

                                _item.id = pCi.id; ret_id = RetAddItem.T_SUCCESS;//pCi.id;

                                snmdb.NormalManagerDB.getInstance().add(8,
                                    new CmdUpdateCardQntd(_uid,
                                        pCi.id, pCi.qntd),
                                    SQLDBResponse,
                                    null);

                            }
                            else
                            {

                                CardInfo ci = new CardInfo();

                                ci.id = _item.id;
                                ci._typeid = _item._typeid;
                                ci.qntd = _item.qntd;
                                ci.type = 1;

                                CmdAddCard cmd_ac = new CmdAddCard(_uid, // Waiter
                                    ci, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ac, null, null);

                                if (cmd_ac.getException().getCodeError() != 0)
                                {
                                    throw cmd_ac.getException();
                                }

                                ci = cmd_ac.getInfo();
                                _item.id = ci.id;

                                if (ci.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                ret_id = RetAddItem.T_SUCCESS;//ci.id;

                            }

                            break;
                        }
                    case IFF_GROUP.FURNITURE:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            var furniture = sIff.getInstance().findFurniture(_item._typeid);

                            if (furniture == null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que nao existe no IFF_STRUCT do server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            if (ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja tem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            MyRoomItem mri = new MyRoomItem();

                            mri._typeid = _item._typeid;
                            mri.id = _item.id;

                            mri.location.SetLoc(furniture.location);

                            CmdAddFurniture cmd_af = new CmdAddFurniture(_uid, // Waiter
                                mri);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_af, null, null);

                            if (cmd_af.getException().getCodeError() != 0)
                            {
                                throw cmd_af.getException();
                            }

                            mri = cmd_af.getInfo();
                            _item.id = mri.id;

                            if (mri.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Furniture[TYPEID=" + Convert.ToString(mri._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.STDA_C_ITEM_QNTD;

                            ret_id = RetAddItem.T_SUCCESS;//mri.id;

                            break;
                        }
                    case IFF_GROUP.AUX_PART:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            //auto auxPart = sIff::getInstance().findAuxPart(_item._typeid);

                            var pWi = _ownerAuxPart(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            {

                                if (!sIff.getInstance().IsCanOverlapped(pWi._typeid))
                                {
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;

                                snmdb.NormalManagerDB.getInstance().add(9,
                                    new CmdUpdateItemQntd(_uid,
                                        pWi.id, pWi.STDA_C_ITEM_QNTD),
                                    SQLDBResponse,
                                    null);
                            }
                            else
                            {

                                WarehouseItemEx wi = new WarehouseItemEx();
                                wi.id = _item.id;
                                wi._typeid = _item._typeid;

                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;
                                wi.c = new stItem(_item).c;

                                if (wi.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                                }

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_uid, // Waiter
                                    wi, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ai, null, null);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o AuxPart[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            }

                            break;
                        }
                    case IFF_GROUP.ITEM:
                        {
                            // CHECK FOR POUCH [PANG OR EXP]
                            if (_item._typeid == PANG_POUCH_TYPEID)
                            {

                                // Pang Pouch para o player
                                PlayerInfo.addPang(_uid, (uint)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                 
                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

                            }
                            else if (_item._typeid == EXP_POUCH_TYPEID)
                            {

                                // Exp Pouch para o player
                                Player.addExp(_uid, (int)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                _smp.message_pool.getInstance().push(new message("[Pangya Shop][Log] PLAYER[UID=" + Convert.ToString(_uid) + "] Adicionou Exp Pouch. item[TYPEID=" + Convert.ToString(_item._typeid) + "] Qntd[value=" + Convert.ToString((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

                            }
                            else if (_item._typeid == CP_POUCH_TYPEID)
                            {

                                // Log Ganhos de CP
                                CPLog cp_log = new CPLog();

                                cp_log.setType(CPLog.TYPE.CP_POUCH);

                                cp_log.setCookie((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                // Cookie Point(CP) Pouch para o player
                                PlayerInfo.addCookie(_uid, (ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                // Log de Ganhos de CP
                                Player.saveCPLog(_uid, cp_log);

                                _smp.message_pool.getInstance().push(new message("[Pangya Shop][Log] PLAYER[UID=" + Convert.ToString(_uid) + "] Adicionou CP Pouch. item[TYPEID=" + Convert.ToString(_item._typeid) + "] Qntd[value=" + Convert.ToString((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                            }
                            // Fim check pouch

                            var pWi = _ownerItem(_uid, _item._typeid);

                            // Ticket Report Sempre Add 1 Novo
                            if ((pWi.id > 0) && _item._typeid != TICKET_REPORT_SCROLL_TYPEID)
                            {

                                if (sPremiumSystem.getInstance().isPremium(pWi._typeid) && pWi.ano > 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add Item[TYPEID=" + Convert.ToString(_item._typeid) + "] 'Premium Ticket' que ele ja possui, com tempo, tem que esperar acabar o tempo", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        15, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;

                                snmdb.NormalManagerDB.getInstance().add(9,
                                    new CmdUpdateItemQntd(_uid,
                                        pWi.id, pWi.STDA_C_ITEM_QNTD),
                                    SQLDBResponse,
                                    null);

                            }
                            else
                            {

                                WarehouseItemEx wi = new WarehouseItemEx();
                                wi.id = _item.id;
                                wi._typeid = _item._typeid;

                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;

                                if (wi.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                                }

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_uid, // Waiter
                                    wi, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ai, null, null);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;
                            }

                            break;
                        }
                    case IFF_GROUP.SKIN:
                        {

                            if (ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add Skin[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            WarehouseItemEx wi = new WarehouseItemEx();
                            wi.id = _item.id;
                            wi._typeid = _item._typeid;
                            wi.type = (sbyte)_item.type;
                            wi.flag = (sbyte)_item.flag;
                            wi.c = new stItem(_item).c;

                            CmdAddSkin cmd_as = new CmdAddSkin(_uid, // Waiter
                                wi, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_as, null, null);

                            if (cmd_as.getException().getCodeError() != 0)
                            {
                                throw cmd_as.getException();
                            }

                            wi = cmd_as.getInfo();
                            _item.id = wi.id;

                            if (wi.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Skin[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.qntd;

                            ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            break;
                        }
                    case IFF_GROUP.PART:
                        {
                            if (ownerItem(_uid, _item._typeid) && !sIff.getInstance().IsCanOverlapped(_item._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou add Part[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            // Ainda falta as parte por tempo, aquelas do couldron de cor dourada
                            WarehouseItemEx wi = new WarehouseItemEx();
                            wi.id = _item.id;
                            wi._typeid = _item._typeid;

                            if (_item.IsUCC())
                            {
                                if ((!string.IsNullOrEmpty(_item.ucc.IDX)))
                                {
                                    wi.ucc.idx = _item.ucc.IDX;
                                }
                                wi.ucc.seq = (short)_item.ucc.seq;
                                wi.ucc.status = (byte)_item.ucc.status;
                            }

                            wi.type = (sbyte)_item.type;
                            wi.flag = (sbyte)_item.flag;
                            wi.c = new stItem(_item).c;

                            wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                            if (_gift_flag == 1 && wi.id > 0)
                            {
                                CmdGetGiftPart cmd_ggp = new CmdGetGiftPart(_uid, // Waiter
                                    wi, _item.type_iff);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ggp, null, null);

                                if (cmd_ggp.getException().getCodeError() != 0)
                                {
                                    throw cmd_ggp.getException();
                                }

                                wi = cmd_ggp.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu pegar o presente de Part[TYPEID=" + Convert.ToString(_item._typeid) + "] para o PLAYER[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        13, 0));
                                }

                            }
                            else
                            {
                                CmdAddPart cmd_ap = new CmdAddPart(_uid, // Waiter
                                    wi, _purchase, 0,
                                    _item.type_iff);

                                snmdb.NormalManagerDB.getInstance().add(3,
                                    cmd_ap, null, null);

                                if (cmd_ap.getException().getCodeError() != 0)
                                {
                                    throw cmd_ap.getException();
                                }

                                wi = cmd_ap.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Parts[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                            }

                            if (_item.IsUCC())
                            {
                                if (!string.IsNullOrEmpty(wi.ucc.idx))
                                {
                                    _item.ucc.IDX = wi.ucc.idx;
                                }
                                _item.ucc.seq = (uint)wi.ucc.seq;
                                _item.ucc.status = wi.ucc.status;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));

                                // Qntd depois em segundos
                                _item.stat.qntd_dep = (int)wi.end_date;

                                if (_item.flag_time == 2)
                                {
                                    _item.STDA_C_ITEM_TIME *= 24; // Horas
                                }
                            }

                            ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            break;
                        }
                    case IFF_GROUP.HAIR_STYLE:
                        {
                            var hair = sIff.getInstance().findHairStyle(_item._typeid);

                            if (hair != null)
                            {
                                var ce = _ownerHairStyle(_uid, _item._typeid);

                                if ((ce.id > 0))
                                { // Tem o Character

                                    ce.default_hair = hair.Color;

                                    snmdb.NormalManagerDB.getInstance().add(4,
                                        new CmdAddCharacterHairStyle(_uid,
                                            ce, _purchase, 0),
                                        SQLDBResponse,
                                        null);

                                    ret_id = RetAddItem.T_SUCCESS;//ce.id;

                                }
                                else
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] nao tem esse character.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        16, 0));
                            }

                            else
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_uid) + "] hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + "] nao tem no IFF_STRUCT.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    17, 0));
                            }

                            break;
                        }
                    case IFF_GROUP.MATCH: // Troféu
                        {
                            var type_trofel = sIff.getInstance().getItemSubGroupIdentify24(_item._typeid);

                            // Troféu Espacial
                            if (type_trofel == 1 || type_trofel == 2)
                            {

                                var tsi = _ownerTrofelEspecial(_uid, _item._typeid);

                                if (tsi.id > 0)
                                { // Já tem item add quantidade do Troféu especial

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    _item.id = tsi.id;
                                    ret_id = RetAddItem.T_SUCCESS;//tsi.id;

                                    snmdb.NormalManagerDB.getInstance().add(18,
                                        new CmdUpdateTrofelEspecialQntd(_uid,
                                            tsi.id, tsi.qntd,
                                            CmdUpdateTrofelEspecialQntd.eTYPE.ESPECIAL),
                                        SQLDBResponse,
                                        null);

                                }
                                else
                                {

                                    TrofelEspecialInfo ts = new TrofelEspecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.ESPECIAL);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ts, null, null);


                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Trofel Especial[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    ret_id = RetAddItem.T_SUCCESS;//ts.id;
                                }

                            }
                            else if (type_trofel == 3)
                            { // Grand Prix

                                var tsi = _ownerTrofelEspecial(_uid, _item._typeid);

                                if (tsi.id > 0)
                                { // Já tem item add quantidade do Troféu Grand Prix

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    _item.id = tsi.id;
                                    ret_id = RetAddItem.T_SUCCESS;//tsi.id;
                                    snmdb.NormalManagerDB.getInstance().add(18,
                                        new CmdUpdateTrofelEspecialQntd(_uid,
                                            tsi.id, tsi.qntd,
                                            CmdUpdateTrofelEspecialQntd.eTYPE.GRAND_PRIX),
                                        SQLDBResponse,
                                        null);

                                }
                                else
                                {

                                    TrofelEspecialInfo ts = new TrofelEspecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.GRAND_PRIX
                                        );

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ts, null, null);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Trofel Grand Prix[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    ret_id = RetAddItem.T_SUCCESS;//ts.id;
                                }
                            }

                            break;
                        } // End iff::MATCH
                } // End Switch

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_uid);

            }

            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[item_manager::addItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_uid);

                ret_id = RetAddItem.T_ERROR;
            }
            return ret_id;
        }

        public static RetAddItem addItem(List<stItem> _v_item, uint _uid, byte _gift_flag, byte _purchase, bool _dup = false)
        {
            RetAddItem rai = new RetAddItem();
            int type = RetAddItem.T_INIT_VALUE;

            Player _session = sgs.gs.getInstance().findPlayer(_uid) ?? null;

            // Percorremos de trás para frente para poder remover itens com segurança
            for (int i = _v_item.Count - 1; i >= 0; i--)
            {
                var it = _v_item[i];
                if (_session != null) 
                type = addItem(it,  _session, _gift_flag, _purchase, _dup);
else
                    type = addItem(it, _uid, _gift_flag, _purchase, _dup);

                if (type <= 0)
                {
                    // LOG DE ERRO
                    _smp.message_pool.getInstance().push(new message("[item_manager::addItem][Log] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou adicionar o item[TYPEID=" + Convert.ToString(it._typeid) + ", ID=" + Convert.ToString(it.id) + "], " + ((type == RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH) ? "mas era pang, exp ou CP pouch" : "mas nao conseguiu.Bug"), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    rai.fails.Add(it);
                    _v_item.RemoveAt(i);  

                    if (rai.type == RetAddItem.T_INIT_VALUE)
                    {
                        rai.type = type;
                    }
                    else if (type == RetAddItem.T_ERROR)
                    {
                        if (rai.type == RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                            rai.type = RetAddItem.TR_SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        else if (rai.type == RetAddItem.T_SUCCESS)
                            rai.type = RetAddItem.TR_SUCCESS_WITH_ERROR;
                        else if (rai.type != RetAddItem.TR_SUCCESS_WITH_ERROR && rai.type != RetAddItem.TR_SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR)
                            rai.type = type;
                    }
                    else if (type == RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        if (rai.type == RetAddItem.T_ERROR || rai.type == RetAddItem.TR_SUCCESS_WITH_ERROR)
                            rai.type = RetAddItem.TR_SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        else if (rai.type == RetAddItem.T_SUCCESS)
                            rai.type = RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                    }
                }
                else
                { 
                    if (rai.type == RetAddItem.T_INIT_VALUE)
                    {
                        rai.type = RetAddItem.T_SUCCESS;
                    }
                    else if (rai.type == RetAddItem.T_ERROR)
                    {
                        rai.type = RetAddItem.TR_SUCCESS_WITH_ERROR;
                    }
                }
            }

            return rai;
        }

        public static int addItem(stItem _item, Player _session, /*era byte*/ byte _gift_flag, byte _purchase, bool _dup = false)
        {

            int ret_id = RetAddItem.T_ERROR;

            try
            {

                // Block Memória para o UID, para garantir que não vai adicionar itens simuntaneamente
                BlockMemoryManager.blockUID(_session.m_pi.uid);
 
                if (_item._typeid == 0)
                {
                    throw new exception("[item_manager::addItem][Error] item invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        9, 0));
                }

                switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_item._typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        {
                            var pCi = _session.m_pi.findCharacterByTypeid(_item._typeid);

                            if (pCi != null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] add um character[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            CharacterInfo ce = new CharacterInfo();
                            ce.id = _item.id;
                            ce._typeid = _item._typeid;

                            ce.initComboDef();

                            // Aqui tem que add em uma fila e manda pra query e depois ver se foi concluida, enquanto verifica outras coisas[
                            // E ESSA CLASSE NÃO PODE SER STATIC, POR QUE TEM QUE GUARDA UNS VALORE NECESSÁRIOS

                            // Add no banco de dados
                            var cmd_ac = new CmdAddCharacter(_session.m_pi.uid, // Waitable
                                ce, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_ac, null, null);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ce = cmd_ac.getInfo();
                            _item.id = ce.id;

                            if (ce.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o character[TYPEID=" + Convert.ToString(ce._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            // Add List Item ON Server
                            var it_char = ce;
                            _session.m_pi.mp_ce.Add(ce.id, ce);

                            // Atualiza character equipado
                            _session.m_pi.ue.character_id = it_char.id;
                            _session.m_pi.ei.char_info = it_char;

                            snmdb.NormalManagerDB.getInstance().add(17,
                                new CmdUpdateCharacterEquiped(_session.m_pi.uid, _session.m_pi.ue.character_id),
                                SQLDBResponse,
                                null);

                            ret_id = RetAddItem.T_SUCCESS;//ce.id;

                            break;
                        }
                    case IFF_GROUP.CADDIE:
                        {
                            var pCi = _session.m_pi.findCaddieByTypeid(_item._typeid);

                            if (pCi != null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add um caddie[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possi.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            CaddieInfoEx ci = new CaddieInfoEx();
                            ci.id = _item.id;
                            ci._typeid = _item._typeid;
                            ci.check_end = 1; // Yes;
                            ci.rent_flag = 1; // 1 Normal sem ferias(tempo), 2 com ferias(tempo)

                            if (_item.date_reserve > 0)
                            {
                                ci.rent_flag = 2;
                                ci.end_date_unix = _item.date_reserve; //(_item.type == 0x20) ? _item.STDA_C_ITEM_TIME : (_item.type == 0x40) ? _item.STDA_C_ITEM_TIME * 60 * 60 : _item.STDA_C_ITEM_TIME;
                            }

                            CmdAddCaddie cmd_ac = new CmdAddCaddie(_session.m_pi.uid, // Waitable
                                ci, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(2,
                                cmd_ac, null, null);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _item.id = ci.id;

                            if (ci.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o caddie[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }


                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            // Add List Item ON Server
                            _session.m_pi.mp_ci.Add(ci.id, ci);

                            ret_id = RetAddItem.T_SUCCESS;//ci.id;

                            break;
                        }
                    case IFF_GROUP.CAD_ITEM:
                        {
                            uint cad_typeid = (uint)((Convert.ToUInt32(sIff.getInstance().CADDIE << 26)) | sIff.getInstance().getCaddieIdentify(_item._typeid));

                            var ci = _session.m_pi.findCaddieByTypeid(cad_typeid);

                            if (ci == null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + "] sem o caddie[TYPEID=" + Convert.ToString(cad_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    14, 0));
                            }

                            long end_date = 0;

                            if (ci.parts_typeid == _item._typeid)
                            { // Já tem o parts caddie, atualiza o tempo

                                end_date = UtilTime.SystemTimeToUnix(ci.end_parts_date) + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME); //(((_item.flag_time == 2) ? _item.STDA_C_ITEM_TIME : _item.STDA_C_ITEM_TIME * 24) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                // Update Parts End Date Unix
                                ci.updatePartsEndDate();

                                // Update Time To Send To Client
                                _item.STDA_C_ITEM_TIME = (short)((_item.flag_time == 2) ? ci.parts_end_date_unix : ci.parts_end_date_unix * 24);

                                _item.id = ci.id;

                                ret_id = RetAddItem.T_SUCCESS;//ci.id;

                            }
                            else
                            {

                                // Não tem o caddie parts ainda, add
                                ci.parts_typeid = _item._typeid;

                                end_date = UtilTime.GetLocalTimeAsUnix() + ((ci.parts_end_date_unix = (short)(STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME))) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                _item.id = ci.id;

                                ret_id = RetAddItem.T_SUCCESS;//ci.id;
                            }

                            // Verifica se o Caddie já tem um item update do parts do caddie, por que se tiver, 
                            // ele vai desequipar esse parts novo por que o player não relogou quando acabou o tempo do caddie parts
                            var v_it = _session.m_pi.findUpdateItemByTypeidAndId(ci._typeid, ci.id);

                            if (!v_it.empty())
                            {

                                foreach (var el in v_it)
                                {
                                    if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                    {
                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }
                                }

                            }
                            // ---- fim do verifica se o caddie tem parts no update item ----

                            var str_end_date = UtilTime.formatDateLocal(end_date);

                            // Atualiza no para os 2 aqui
                            snmdb.NormalManagerDB.getInstance().add(5,
                                new CmdUpdateCaddieItem(_session.m_pi.uid,
                                    str_end_date, ci),
                                SQLDBResponse,
                                null);

                            break;
                        }
                    case IFF_GROUP.MASCOT:
                        {
                            var mascot = sIff.getInstance().findMascot(_item._typeid);

                            if (mascot == null)
                            {
                                throw new exception("[item_manager::addItem][Erorr] mascot[TYPEID=" + Convert.ToString(_item._typeid) + "] nao foi encontrado no IFF_STRUCT do server, para o PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            var pMi = _session.m_pi.findMascotByTypeid(_item._typeid);

                            if (pMi != null)
                            { // Player já tem o mascot, só add mais tempo à ele

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {

                                    //long unix_time = UtilTime.SystemTimeToUnix(pMi->data);
                                    long unix_time = UtilTime.TzLocalTimeToUnixUTC(pMi.data);

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local Time for verify on server on local time
                                    pMi.data = (UtilTime.UnixUTCToTzLocalTime(unix_time));

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;
                                    // System Time Struct is Local Time 
                                    _item.date.date.sysDate[0].CreateTime();
                                    _item.date.date.sysDate[1] = pMi.data;

                                    var str_date = UtilTime._formatDate(pMi.data);

                                    // Cmd update time mascot db
                                    snmdb.NormalManagerDB.getInstance().add(6,
                                        new CmdUpdateMascotTime(_session.m_pi.uid,
                                            pMi.id, str_date),
                                        SQLDBResponse,
                                        null);
                                }

                                _item.id = pMi.id;

                                ret_id = RetAddItem.T_SUCCESS;//_item.id;

                                // Verifica se o Mascot está no item update, por que se tiver, 
                                // ele vai desequipar esse mascot por que o player não relogou quando acabou o tempo do Mascot
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pMi._typeid, pMi.id);

                                if (!v_it.empty())
                                {

                                    foreach (var el in v_it)
                                    {
                                        if (el.Value.type == UpdateItem.UI_TYPE.MASCOT)
                                        {
                                            // Tira esse Update Item do map
                                            _session.m_pi.mp_ui.Remove(el.Key);
                                        }
                                    }

                                }
                                // ---- fim do verifica se o Mascot está no update item ----

                            }
                            else
                            {

                                MascotInfoEx mi = new MascotInfoEx();
                                mi.id = _item.id;
                                mi._typeid = _item._typeid;
                                mi.is_cash = _item.is_cash;
                                mi.price = _item.price;
                                mi.tipo = 0; // Padrão, é os mascot que não tem tempo
                                mi.message = "";
                                if (mascot.msg.active)
                                {
                                    mi.message = "PangYa SuperSS!";
                                }

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {
                                    mi.tipo = 1; // Mascot de Tempo
                                }

                                CmdAddMascot cmd_am = new CmdAddMascot(_session.m_pi.uid, // Waiter
                                    mi, _item.STDA_C_ITEM_TIME,
                                    _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_am, null, null);

                                if (cmd_am.getException().getCodeError() != 0)
                                {
                                    throw cmd_am.getException();
                                }

                                mi = cmd_am.getInfo();
                                _item.id = mi.id;

                                if (mi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Mascot[TYPEID=" + Convert.ToString(mi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {

                                    long unix_time = UtilTime.GetSystemTimeAsUnix();

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local time for verify server on local time
                                    mi.data = (UtilTime.UnixUTCToTzLocalTime(unix_time));

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0].CreateTime();
                                    _item.date.date.sysDate[1] = mi.data;

                                }
                                else // O Mascot não é por tempo, mas precisa do end data, por que é a mesma da data que ele foi adicionado no banco de dados, precisa na hora de atualizar o mascot
                                {
                                    mi.data.CreateTime();
                                }

                                // Add List Item ON Server
                                _session.m_pi.mp_mi.Add(mi.id, mi);

                                ret_id = RetAddItem.T_SUCCESS;//mi.id;
                            }

                            break;
                        }
                    case IFF_GROUP.BALL:
                        {
                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            { // já tem atualiza quantidade

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;
                                _item.id = pWi.id;
                                snmdb.NormalManagerDB.getInstance().add(7,
                                    new CmdUpdateBallQntd(_session.m_pi.uid,
                                        pWi.id, pWi.STDA_C_ITEM_QNTD),
                                    SQLDBResponse,
                                    null);

                                //  ITEM_MANAGER_LOG("Atualizou Ball", _session, _item);

                            }
                            else
                            { // não tem, add

                                WarehouseItemEx wi = new WarehouseItemEx();
                                wi.id = _item.id;
                                wi._typeid = _item._typeid;

                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;
                                wi.c = new stItem(_item).c;

                                wi.ano = -1;

                                CmdAddBall cmd_ab = new CmdAddBall(_session.m_pi.uid, // Waiter
                                    wi, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ab, null, null);

                                if (cmd_ab.getException().getCodeError() != 0)
                                {
                                    throw cmd_ab.getException();
                                }

                                wi = cmd_ab.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Ball[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                // Add List Item ON Server
                                _session.m_pi.mp_wi.Add(wi.id, wi);

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;
                                                              //  ITEM_MANAGER_LOG("Adicionou Ball", _session, _item);

                            }

                            break;
                        }
                    case IFF_GROUP.CLUBSET:
                        {

                            var clubset = sIff.getInstance().findClubSet(_item._typeid);

                            if (clubset == null)
                            {
                                throw new exception("[item_manager::addItem][Error] clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] set nao foi encontrado no IFF_STRUCT do server, para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            {

                                // Add Mais tempo no Club Set
                                if (_item.STDA_C_ITEM_TIME > 0
                                    && ((pWi.flag & 0x20) != 0 || (pWi.flag & 0x40) != 0 || (pWi.flag & 0x60) != 0)
                                    && pWi.end_date_unix_local > 0)
                                {

                                    _item.date.active = 1;

                                    // update ano (Horas) que o item ainda tem
                                    pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                    // Só atualiza o Apply date se não tiver
                                    if (pWi.apply_date_unix_local == 0u)
                                    {

                                        pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                        // Convert to UTC to send client
                                        pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                    }

                                    pWi.end_date_unix_local = (uint)(UtilTime.GetLocalTimeAsUnix() + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));
                                    // Atualiza o tempo do ClubSet do player
                                    snmdb.NormalManagerDB.getInstance().add(20,
                                        new CmdUpdateClubSetTime(_session.m_pi.uid, pWi),
                                        SQLDBResponse,
                                        null);

                                    _item.STDA_C_ITEM_QNTD = 1;
                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = _item.qntd;
                                    ret_id = RetAddItem.T_SUCCESS;//pWi.id;
                                    _item.id = pWi.id;
                                    // Verifica se o ClubSet está no item update, por que se tiver, 
                                    // ele vai desequipar esse clubset por que o player não relogou quando acabou o tempo do clubset
                                    var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pWi._typeid, pWi.id);

                                    if (!v_it.empty())
                                    {

                                        foreach (var el in v_it)
                                        {
                                            if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                                            {
                                                // Tira esse Update Item do map
                                                _session.m_pi.mp_ui.Remove(el.Key);
                                            }
                                        }

                                    }
                                    // ---- fim do verifica se o ClubSet está no update item ----
                                    //  ITEM_MANAGER_LOG("Atualizou Tempo do CluSet", _session, _item);

                                }
                                else
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));
                            }
                            else
                            {

                                WarehouseItemEx wi = new WarehouseItemEx();

                                wi.id = _item.id;
                                wi._typeid = _item._typeid;
                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;

                                wi.clubset_workshop.level = clubset.work_shop.tipo; // Cv 1 e etc

                                // Tempo
                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME = _item.STDA_C_ITEM_TIME;
                                }

                                if (wi.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                                }

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para Horas, quanto tempo falta em horas

                                if (_gift_flag == 1 && wi.id > 0)
                                {
                                    CmdGetGiftClubSet cmd_ggcs = new CmdGetGiftClubSet(_session.m_pi.uid, // Waiter
                                        wi);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ggcs, null, null);

                                    if (cmd_ggcs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ggcs.getException();
                                    }

                                    wi = cmd_ggcs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu pegar o presente de ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + "] para o PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            13, 0));
                                    }

                                }
                                else
                                {
                                    CmdAddClubSet cmd_acs = new CmdAddClubSet(_session.m_pi.uid, // Waiter
                                        wi, _purchase, 0);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_acs, null, null);

                                    if (cmd_acs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_acs.getException();
                                    }

                                    wi = cmd_acs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {

                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time                                           
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                // Add List Item ON Server
                                _session.m_pi.mp_wi.Add(wi.id, wi);

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            }

                            break;
                        }
                    case IFF_GROUP.CARD:
                        {
                            var pCi = _session.m_pi.findCardByTypeid(_item._typeid);

                            if (pCi != null)
                            { // Já tem item add quantidade ao card

                                _item.stat.qntd_ant = pCi.qntd;

                                pCi.qntd += _item.qntd;

                                _item.stat.qntd_dep = pCi.qntd;

                                _item.id = pCi.id;

                                ret_id = RetAddItem.T_SUCCESS;

                                snmdb.NormalManagerDB.getInstance().add(8,
                                    new CmdUpdateCardQntd(_session.m_pi.uid,
                                        pCi.id, pCi.qntd),
                                    SQLDBResponse,
                                    null);

                                //  ITEM_MANAGER_LOG("Atualizou Card", _session, _item);

                            }
                            else
                            {

                                CardInfo ci = new CardInfo
                                {
                                    id = _item.id,
                                    _typeid = _item._typeid,
                                    qntd = _item.qntd,
                                    type = 1
                                };

                                CmdAddCard cmd_ac = new CmdAddCard(_session.m_pi.uid, ci, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ac, null, null);

                                if (cmd_ac.getException().getCodeError() != 0)
                                    throw cmd_ac.getException();

                                ci = cmd_ac.getInfo();
                                _item.id = ci.id;

                                if (ci.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                // Add List Item ON Server
                                _session.m_pi.v_card_info.Add(ci.id, ci);

                                ret_id = RetAddItem.T_SUCCESS;
                                //  ITEM_MANAGER_LOG("Adicionou Card", _session, _item);
                            }

                            break;
                        }
                    case IFF_GROUP.FURNITURE:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            var furniture = sIff.getInstance().findFurniture(_item._typeid);

                            if (furniture == null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que nao existe no IFF_STRUCT do server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pFi = _session.m_pi.findMyRoomItemByTypeid(_item._typeid);

                            if (pFi != null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja tem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            MyRoomItem mri = new MyRoomItem();

                            mri._typeid = _item._typeid;
                            mri.id = _item.id;

                            mri.location.SetLoc(furniture.location);

                            CmdAddFurniture cmd_af = new CmdAddFurniture(_session.m_pi.uid, // Waiter
                                mri);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_af, null, null);

                            if (cmd_af.getException().getCodeError() != 0)
                            {
                                throw cmd_af.getException();
                            }

                            mri = cmd_af.getInfo();
                            _item.id = mri.id;

                            if (mri.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Furniture[TYPEID=" + Convert.ToString(mri._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.STDA_C_ITEM_QNTD;

                            // Add List Item ON Server
                            _session.m_pi.v_mri.Add(mri);

                            ret_id = RetAddItem.T_SUCCESS;//mri.id;

                            //  ITEM_MANAGER_LOG("Adicionou Furniture", _session, _item);

                            break;
                        }
                    case IFF_GROUP.AUX_PART:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            //auto auxPart = sIff::getInstance().findAuxPart(_item._typeid);

                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            { // Já tem item add quantidade do AuxPart

                                if (!sIff.getInstance().IsCanOverlapped(pWi._typeid))
                                {
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;

                                snmdb.NormalManagerDB.getInstance().add(9,
                                    new CmdUpdateItemQntd(_session.m_pi.uid,
                                        pWi.id, pWi.STDA_C_ITEM_QNTD),
                                    SQLDBResponse,
                                    null);

                                //  ITEM_MANAGER_LOG("Atualizou AuxPart", _session, _item);

                            }
                            else
                            {

                                WarehouseItemEx wi = new WarehouseItemEx();
                                wi.id = _item.id;
                                wi._typeid = _item._typeid;

                                wi.type = (sbyte)_item.type;
                                wi.flag = (sbyte)_item.flag;
                                wi.c = new stItem(_item).c;
                                if (wi.STDA_C_ITEM_TIME > 0)
                                {
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                                }

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_session.m_pi.uid, // Waiter
                                    wi, _purchase, 0);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ai, null, null);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o AuxPart[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time                     
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                // Add List Item ON Server
                                _session.m_pi.mp_wi.Add(wi.id, wi);

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;

                                //  ITEM_MANAGER_LOG("Adicionou AuxPart", _session, _item);

                            }

                            break;
                        }
                    case IFF_GROUP.ITEM:
                        {

                            // CHECK FOR POUCH [PANG, EXP OR CP]
                            if (_item._typeid == PANG_POUCH_TYPEID/*Pang Pouch*/)
                            {

                                // Pang Pouch para o player
                                _session.addPang((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                _smp.message_pool.getInstance().push(new message("[Pangya Shop][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] Adicionou Pang Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.m_pi.uid);

                                return RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

                            }
                            else if (_item._typeid == EXP_POUCH_TYPEID/*Exp Pouch*/)
                            {

                                // Exp Pouch para o player
                                _session.addExp((int)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD), true/*Send packet for update level and exp in game*/);

                                _smp.message_pool.getInstance().push(new message("[Pangya Shop][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] Adicionou Exp Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.m_pi.uid);

                                return RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

                            }
                            else if (_item._typeid == CP_POUCH_TYPEID/*Cookie Point Pouch*/)
                            {

                                // Log Ganhos de CP
                                CPLog cp_log = new CPLog();

                                cp_log.setType(CPLog.TYPE.CP_POUCH);

                                cp_log.setCookie((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                // Cookie Point(CP) Pouch para o player
                                _session.addCookie((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));

                                // Log de Ganhos de CP
                                _session.saveCPLog(cp_log);

                                _smp.message_pool.getInstance().push(new message("[Pangya Shop][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] Adicionou CP Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.m_pi.uid);

                                return RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                            }
                            // Fim check pouch

                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            // Ticket Report Sempre Add 1 Novo
                            if (pWi != null && _item._typeid != TICKET_REPORT_SCROLL_TYPEID)
                            {   // Já tem item add quantidade do Item

                                if (sPremiumSystem.getInstance().isPremium(pWi._typeid))
                                {

                                    var st = UtilTime.UnixToSystemTime(pWi.end_date_unix_local);

                                    if (UtilTime.GetLocalTimeDiffDESC(st) > 0 || _session.m_pi.m_cap.premium_user)
                                        throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou add Item[TYPEID="
                                                + (_item._typeid) + "] 'Premium Ticket' que ele ja possui com tempo, tem que esperar acabar o tempo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 15, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.T_SUCCESS;//pWi.id;

                                // Verifica se é Premium Ticket
                                if (sPremiumSystem.getInstance().isPremium(pWi._typeid))
                                { // Renova o Premium Ticket por mais 30(tempo, coloquei mais opções) dias

                                    if (_item.STDA_C_ITEM_TIME > 0)
                                    {
                                        _item.date.active = 1;

                                        // Conver
                                        pWi.STDA_C_ITEM_TIME = (short)(_item.STDA_C_ITEM_TIME / 24); // converte de novo para Dias para salvar no banco de dados

                                        // Atualiza o tempo do Premium Ticket do player
                                        snmdb.NormalManagerDB.getInstance().add(19, new CmdUpdatePremiumTicketTime(_session.m_pi.uid, pWi), SQLDBResponse, null);

                                        // update ano (Horas) que o item ainda tem
                                        pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                        // Só atualiza o Apply date se não tiver
                                        if (pWi.apply_date_unix_local == 0u)
                                        {

                                            pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                            // Convert to UTC to send client
                                            pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                        }

                                        pWi.end_date_unix_local = (uint)((uint)UtilTime.GetLocalTimeAsUnix() + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                        // Convert to UTC to send client
                                        pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                        // System Time Struct is Local Time 
                                        _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                        _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));

                                    }

                                    if (sPremiumSystem.getInstance().isPremium(_item._typeid))
                                    {

                                        _smp.message_pool.getInstance().push(new message("[item_manager::addItem][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] renovou premium ticket[TYPEID="
                                                + (pWi._typeid) + "] por " + (_item.STDA_C_ITEM_TIME / 24u) + " Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        sPremiumSystem.getInstance().addPremiumUser(_session, pWi, (uint)(_item.STDA_C_ITEM_TIME / 24)/*Dias*/);
                                    }

                                    // Verifica se o Premium Ticket está no item update, por que se tiver
                                    var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pWi._typeid, pWi.id);

                                    if (!v_it.empty())
                                    {
                                        foreach (var el in v_it)
                                        {
                                            if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                                            {
                                                // Tira esse Update Item do map
                                                _session.m_pi.mp_ui.Remove(el.Key);
                                            }
                                        }
                                    }
                                    // ---- fim do verifica se o Premium Ticket está no update item ----

                                }
                                else // Atualiza a quantidade do item normal
                                    snmdb.NormalManagerDB.getInstance().add(9, new CmdUpdateItemQntd(_session.m_pi.uid, pWi.id, pWi.STDA_C_ITEM_QNTD), SQLDBResponse, null);

                                // Atualiza Gacha Coupon
                                if (_item._typeid == 0x1A000080/*Coupon Gacha*/)
                                {
                                    _session.m_pi.cg.normal_ticket += _item.STDA_C_ITEM_QNTD;

                                    packet_func.session_send(packet_func.pacote102(_session.m_pi),
                                                                        _session, 1);
                                }

                            }
                            else
                            {

                                if (sPremiumSystem.getInstance().isPremium(_item._typeid))
                                {

                                    pWi = (_session.m_pi.pt._typeid == 0) ? null : _session.m_pi.findWarehouseItemByTypeid(_session.m_pi.pt._typeid);

                                    if (pWi != null)
                                    {

                                        var st = UtilTime.UnixToSystemTime(pWi.end_date_unix_local);

                                        if (UtilTime.GetLocalTimeDiffDESC(st) > 0 || _session.m_pi.m_cap.premium_user)
                                            throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou add Item[TYPEID="
                                                    + (_item._typeid) + "] 'Premium Ticket' que ele ja possui outro Premium Ticket[TYPEID="
                                                    + (pWi._typeid) + "] com tempo, tem que esperar acabar o tempo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 15, 0));
                                    }
                                }

                                WarehouseItemEx wi = new WarehouseItemEx
                                {
                                    id = _item.id,
                                    _typeid = _item._typeid,
                                    type = (sbyte)_item.type,
                                    flag = (sbyte)_item.flag,
                                    c = new stItem(_item).c,
                                };

                                if (wi.STDA_C_ITEM_TIME > 0)
                                    wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                var cmd_ai = new CmdAddItem(_session.m_pi.uid, wi, _purchase, 0/*_gift_flag*/);   // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0, cmd_ai, null, null);

                                if (cmd_ai.getException().getCodeError() != 0)
                                    throw cmd_ai.getException();

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Item[TYPEID=" + (wi._typeid) + "] para o player: "
                                            + (_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 11, 0));


                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                if (_item._typeid == 0x1A000080/*Coupon Gacha*/)
                                {
                                    _session.m_pi.cg.normal_ticket += _item.STDA_C_ITEM_QNTD;

                                    packet_func.session_send(packet_func.pacote102(_session.m_pi),
                                     _session, 1);
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                _session.m_pi.mp_wi.Add(wi.id, wi);

                                if (sPremiumSystem.getInstance().isPremium(_item._typeid))
                                {

                                    _smp.message_pool.getInstance().push(new message("[item_manager::addItem][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] comprou premium ticket[TYPEID="
                                            + (wi._typeid) + "] por " + (_item.STDA_C_ITEM_TIME / 24) + " Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    sPremiumSystem.getInstance().addPremiumUser(_session, wi, (uint)(_item.STDA_C_ITEM_TIME / 24));
                                }

                                ret_id = RetAddItem.T_SUCCESS;//wi.id;

                            }

                            break;
                        }
                    case IFF_GROUP.SKIN:
                        {
                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add Skin[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            WarehouseItemEx wi = new WarehouseItemEx
                            {
                                id = _item.id,
                                _typeid = _item._typeid,
                                type = (sbyte)_item.type,
                                flag = (sbyte)_item.flag,
                                c = new stItem(_item).c,
                            };
                            CmdAddSkin cmd_as = new CmdAddSkin(_session.m_pi.uid, // Waiter
                                wi, _purchase, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_as, null, null);

                            if (cmd_as.getException().getCodeError() != 0)
                            {
                                throw cmd_as.getException();
                            }

                            wi = cmd_as.getInfo();
                            _item.id = wi.id;

                            if (wi.id <= 0)
                            {
                                throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Skin[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.qntd;

                            // Add List Item ON Server
                            _session.m_pi.mp_wi.Add(wi.id, wi);

                            ret_id = RetAddItem.T_SUCCESS;//wi.id;
                            break;
                        }
                    case IFF_GROUP.PART:
                        {
                            var pWi = _session.m_pi.findWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null && !sIff.getInstance().IsCanOverlapped(pWi._typeid))
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou add Part[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            // Ainda falta as parte por tempo, aquelas do couldron de cor dourada, Já Fiz
                            WarehouseItemEx wi = new WarehouseItemEx
                            {
                                id = _item.id,
                                _typeid = _item._typeid
                            };

                            if (_item.IsUCC())
                            {
                                if ((!string.IsNullOrEmpty(_item.ucc.IDX)))
                                {
                                    wi.ucc.idx = _item.ucc.IDX;
                                }
                                wi.ucc.seq = (short)_item.ucc.seq;
                                wi.ucc.status = (byte)_item.ucc.status;
                            }

                            wi.type = (sbyte)_item.type;
                            wi.flag = (sbyte)_item.flag;
                            wi.c = new stItem(_item).c;
                            wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? STDA_TRANSLATE_FLAG_TIME_TO_HOUR(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                            if (_gift_flag == 1 && wi.id > 0)
                            {
                                CmdGetGiftPart cmd_ggp = new CmdGetGiftPart(_session.m_pi.uid, // Waiter
                                    wi, _item.type_iff);

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ggp, null, null);
                                if (cmd_ggp.getException().getCodeError() != 0)
                                {
                                    throw cmd_ggp.getException();
                                }

                                wi = cmd_ggp.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu pegar o presente de Part[TYPEID=" + Convert.ToString(_item._typeid) + "] para o PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        13, 0));
                                }

                            }
                            else
                            {
                                CmdAddPart cmd_ap = new CmdAddPart(_session.m_pi.uid, // Waiter
                                    wi, _purchase, 0,
                                    _item.type_iff);

                                snmdb.NormalManagerDB.getInstance().add(3,
                                    cmd_ap, null, null);

                                if (cmd_ap.getException().getCodeError() != 0)
                                {
                                    throw cmd_ap.getException();
                                }

                                wi = cmd_ap.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Parts[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                            }

                            if (_item.IsUCC())
                            {
                                if (!string.IsNullOrEmpty(wi.ucc.idx))
                                {
                                    _item.ucc.IDX = wi.ucc.idx;
                                }
                                _item.ucc.seq = (uint)wi.ucc.seq;
                                _item.ucc.status = wi.ucc.status;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + STDA_TRANSLATE_FLAG_TIME(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));

                                // Qntd depois em segundos
                                _item.stat.qntd_dep = (int)wi.end_date;

                                if (_item.flag_time == 2)
                                {
                                    _item.STDA_C_ITEM_TIME *= 24; // Horas
                                }
                            }

                            // Add List Item ON Server
                            _session.m_pi.mp_wi.Add(wi.id, wi);

                            ret_id = RetAddItem.T_SUCCESS;//wi.id;
                            break;
                        }
                    case IFF_GROUP.HAIR_STYLE:
                        {
                            var hair = sIff.getInstance().findHairStyle(_item._typeid);

                            if (hair != null)
                            {
                                uint char_typeid = (uint)((sIff.getInstance().CHARACTER << 26) | hair.Character);

                                CharacterInfo ce = _session.m_pi.findCharacterByTypeid(char_typeid);

                                if (ce != null)
                                {

                                    ce.default_hair = hair.Color;

                                    snmdb.NormalManagerDB.getInstance().add(4,
                                        new CmdAddCharacterHairStyle(_session.m_pi.uid,
                                            ce, _purchase, 0),
                                        SQLDBResponse,
                                        _session);

                                    ret_id = RetAddItem.T_SUCCESS;//ce.id;


                                }
                                else
                                    throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem esse character[TYPEID=" + Convert.ToString(char_typeid) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        16, 0));
                            }

                            else
                            {
                                throw new exception("[item_manager::addItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + "] nao tem no IFF_STRUCT.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    17, 0));
                            }

                            break;
                        }
                    case IFF_GROUP.MATCH: // Troféu
                        {
                            var type_trofel = sIff.getInstance().getItemSubGroupIdentify24(_item._typeid);

                            // Troféu Espacial
                            if (type_trofel == 1 || type_trofel == 2)
                            {

                                var tsi = _session.m_pi.findTrofelEspecialByTypeid(_item._typeid);

                                if (tsi != null)
                                { // Já tem item add quantidade do Troféu Especial

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;
                                    ret_id = RetAddItem.T_SUCCESS;//tsi.id;
                                    _item.id = tsi.id;

                                    snmdb.NormalManagerDB.getInstance().add(18,
                                        new CmdUpdateTrofelEspecialQntd(_session.m_pi.uid,
                                            tsi.id, tsi.qntd,
                                            CmdUpdateTrofelEspecialQntd.eTYPE.ESPECIAL),
                                        SQLDBResponse,
                                        null);

                                }
                                else
                                {

                                    TrofelEspecialInfo ts = new TrofelEspecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_session.m_pi.uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.ESPECIAL);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ts, null, null);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Trofel Especial[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    // Add List Item ON Server
                                    _session.m_pi.v_tsi_current_season.Add(ts);

                                    ret_id = RetAddItem.T_SUCCESS;//ts.id;
                                }

                            }
                            else if (type_trofel == 3)
                            { // Grand Prix

                                var tsi = _session.m_pi.findTrofelGrandPrixByTypeid(_item._typeid);

                                if (tsi != null)
                                { // Já tem item add quantidade do Troféu Grand Prix

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    ret_id = RetAddItem.T_SUCCESS;//tsi.id;
                                    _item.id = tsi.id;
                                    snmdb.NormalManagerDB.getInstance().add(18,
                                        new CmdUpdateTrofelEspecialQntd(_session.m_pi.uid,
                                            tsi.id, tsi.qntd,
                                            CmdUpdateTrofelEspecialQntd.eTYPE.GRAND_PRIX),
                                        SQLDBResponse,
                                        null);

                                }
                                else
                                {

                                    TrofelEspecialInfo ts = new TrofelEspecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_session.m_pi.uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.GRAND_PRIX);

                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        cmd_ts, null, null);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[item_manager::addItem][Error] nao conseguiu adicionar o Trofel Grand Prix[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    // Add List Item ON Server
                                    _session.m_pi.v_tgp_current_season.Add(ts);

                                    ret_id = RetAddItem.T_SUCCESS;//ts.id;
                                }
                            }

                            break;
                        } // End iff::MATCH
                } // End Switch

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_session.m_pi.uid);
            }

            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[item_manager::addItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_session.m_pi.uid);

                ret_id = RetAddItem.T_ERROR;
            }

            return ret_id;
        }

         
        public static int giveItem(stItem _item, Player _session,  /*era byte*/ byte _gift_flag)
        {

            if (!_session.getState())
            {
                throw new exception("[item_manager::giveItem][Error] session nao esta conectada.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    0, 8));
            }

            int ret_id = -1;

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_item._typeid))
            {
                case IFF_GROUP.CHARACTER:
                    // Não pode deletar Character
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um Character[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CADDIE:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um Caddie[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CAD_ITEM:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um CaddieItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.m_pi.findCardById(_item.id);

                        if (pCi == null)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        _item.stat.qntd_ant = pCi.qntd;

                        pCi.qntd -= _item.STDA_C_ITEM_QNTD;

                        _item.stat.qntd_dep = pCi.qntd;

                        _item.STDA_C_ITEM_QNTD *= short.MaxValue; // deixa negativo para tirar no pangya

                        // Return value, avoid memory leaks
                        ret_id = pCi.id;

                        if (pCi.qntd == 0)
                        {
                            snmdb.NormalManagerDB.getInstance().add(12, // Delete Card
                                new CmdDeleteCard(_session.m_pi.uid, pCi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_ci, second.id, == , pCi->id);
                            var it = _session.m_pi.findCaddieById(pCi.id);

                            if (it.id != _session.m_pi.mp_ci.end().Key)
                            {
                                _session.m_pi.mp_ci.Remove(it.id);
                            }
                        }
                        else
                        {
                            snmdb.NormalManagerDB.getInstance().add(8, // Update
                                new CmdUpdateCardQntd(_session.m_pi.uid,
                                    pCi.id, pCi.qntd),
                                SQLDBResponse,
                                null);
                        }

                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        _item.stat.qntd_ant = 1;
                        _item.stat.qntd_dep = 0;
                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        snmdb.NormalManagerDB.getInstance().add(13, // Gift ClubSet
                            new CmdGiftClubSet(_session.m_pi.uid, pWi.id),
                            SQLDBResponse,
                            null);

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        var it = _session.m_pi.findWarehouseItemById(pWi.id);

                        if (it.id != 0)
                        {
                            _session.m_pi.mp_wi.Remove(it.id);
                        }

                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        if (_item.STDA_C_ITEM_QNTD > pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem quantidade[value=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", req=" + Convert.ToString(_item.STDA_C_ITEM_QNTD) + "] suficiente para o Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                22, 0));
                        }

                        _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                        pWi.STDA_C_ITEM_QNTD -= _item.STDA_C_ITEM_QNTD;

                        _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                        _item.STDA_C_ITEM_QNTD *= short.MaxValue; // deixa negativo para tirar no pangya

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        if (pWi.STDA_C_ITEM_QNTD == 0)
                        {
                            snmdb.NormalManagerDB.getInstance().add(11, // Delete Ball
                                 (Pangya_DB)new CmdDeleteBall(_session.m_pi.uid, pWi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                            var it = _session.m_pi.findWarehouseItemById(pWi.id);

                            if (it.id != 0)
                            {
                                _session.m_pi.mp_wi.Remove(it.id);
                            }
                        }
                        else
                        {
                            snmdb.NormalManagerDB.getInstance().add(7, // Update
                                new CmdUpdateBallQntd(_session.m_pi.uid,
                                    pWi.id, pWi.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);
                        }

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.HAIR_STYLE:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um HairStyle[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.ITEM:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        if (_item.STDA_C_ITEM_QNTD > pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem quantidade[value=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", req=" + Convert.ToString(_item.STDA_C_ITEM_QNTD) + "] suficiente para o item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                22, 0));
                        }

                        _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                        pWi.STDA_C_ITEM_QNTD -= _item.STDA_C_ITEM_QNTD;

                        _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                        _item.STDA_C_ITEM_QNTD *= short.MaxValue; // deixa negativo para tirar no pangya

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        if (pWi.STDA_C_ITEM_QNTD == 0)
                        {
                            snmdb.NormalManagerDB.getInstance().add(10, // Delete Item
                                new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                            var it = _session.m_pi.findWarehouseItemById(pWi.id);

                            if (it.id != 0)
                            {
                                _session.m_pi.mp_wi.Remove(it.id);
                            }
                        }
                        else
                        {
                            snmdb.NormalManagerDB.getInstance().add(9, // Update
                                new CmdUpdateItemQntd(_session.m_pi.uid,
                                    pWi.id, pWi.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);
                        }

                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um SetItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.SKIN:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um Skin[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.MASCOT:
                    throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode presentear um Mascot[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.PART:
                    {

                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[item_manager::giveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        _item.stat.qntd_ant = 1;
                        _item.stat.qntd_dep = 0;
                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        snmdb.NormalManagerDB.getInstance().add(14, // Gift Part
                            new CmdGiftPart(_session.m_pi.uid, pWi.id),
                            SQLDBResponse,
                            null);

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        var it = _session.m_pi.findWarehouseItemById(pWi.id);

                        if (it.id != 0)
                        {
                            _session.m_pi.mp_wi.Remove(it.id);
                        }

                        break;
                    } // Case iff::PART End
            } // Switch End

            return ret_id;
        }

        public static int giveItem(List<stItem> _v_item, Player _session,  /*era byte*/ byte _gift_flag)
        {

            int i;
            var list = _v_item;
            for (i = 0; i < list.Count(); ++i)
            {
                if (giveItem(list[i],
                    _session, _gift_flag) <= 0)
                {
                    _v_item.Remove(list[i]);
                }
            }

            return (int)i;
        }

        public static int removeItem(stItem _item, Player _session)
        {

            if (!_session.getState())
            {
                throw new exception("[item_manager::removeItem][Error] session nao esta conectada.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    0, 8));
            }

            int ret_id = -1;

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_item._typeid))
            {
                case IFF_GROUP.AUX_PART: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= (short)_item.qntd)
                        { // Exclui o Item[AxuPart]

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            _item.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * ushort.MaxValue);

                            pWi.STDA_C_ITEM_QNTD = 0;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                            var it = _session.m_pi.findWarehouseItemById(pWi.id);

                            if (it.id != 0)
                            {
                                _session.m_pi.mp_wi.Remove(it.id);
                            }

                            ret_id = _item.id;

                            // Se deletou a AuxPart que estava equipada 
                            // Desequipa o AuxPart
                            var v_ci = _session.isAuxPartEquiped(_item._typeid);

                            if (!v_ci.empty())
                            {

                                foreach (var el in v_ci)
                                {

                                    if (el != null)
                                    {

                                        // Desequipa o AuxPart
                                        el.unequipAuxPart(_item._typeid);

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateCharacterAllPartEquiped(_session.m_pi.uid, el),
                                            SQLDBResponse,
                                            null);

#if DEBUG
                                        _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] desequipou o AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] do Character[TYPEID=" + Convert.ToString(el._typeid) + ", ID=" + Convert.ToString(el.id) + "] por que ele foi deletado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                                        // Update ON GAME
                                        var p = new PangyaBinaryWriter((ushort)0x6B);

                                        p.WriteByte(4); // 4 Sucesso
                                        p.WriteByte(0); // Character All Parts

                                        p.WriteBytes(el.ToArray());

                                        packet_func.session_send(p,
                                            _session, 1);
                                    }
                                }
                            }

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)_item.qntd;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateItemQntd(_session.m_pi.uid,
                                    pWi.id, pWi.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);

                            ret_id = pWi.id;

                        }

                        break;
                    }
                case IFF_GROUP.BALL: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= (short)_item.qntd)
                        { // Exclui o Item[Ball]

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            _item.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * ushort.MaxValue);

                            pWi.STDA_C_ITEM_QNTD = 0;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            // Passa o typeid do Warehouse para o _item para garantir, se não tiver colocado o typeid, na estrutura
                            _item._typeid = pWi._typeid;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdDeleteBall(_session.m_pi.uid, pWi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                            var it = _session.m_pi.findWarehouseItemById(pWi.id);

                            if (it.id != 0)
                            {
                                _session.m_pi.mp_wi.Remove(it.id);
                            }

                            ret_id = _item.id;

                            // Se deletou a bola que estava equipada 
                            // troca para a bola padrão
                            if (_session.m_pi.ue.ball_typeid == _item._typeid)
                            {

                                WarehouseItemEx pBall = null;
                                var p = new PangyaBinaryWriter();

                                if ((pBall = _session.m_pi.findWarehouseItemByTypeid(0x14000000)) == null)
                                {
                                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem a Comet padrao para substituir a bola[TYPEID=" + Convert.ToString(_item._typeid) + "] deletada. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                                else
                                { // Substitui

                                    // Update ON SERVER
                                    _session.m_pi.ue.ball_typeid = DEFAULT_COMET_TYPEID; // Comet Padrão

                                    _session.m_pi.ei.comet = pBall;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateBallEquiped(_session.m_pi.uid, _session.m_pi.ue.ball_typeid),
                                        SQLDBResponse,
                                        null);

#if DEBUG
                                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] substitui a bola[TYPEID=" + Convert.ToString(_item._typeid) + "] deletada pela COMET PADRAO[TYPEID=" + Convert.ToString(DEFAULT_COMET_TYPEID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                                    // Update ON GAME  
                                    packet_func.session_send(packet_func.pacote04B(
                                        _session, 2),
                                        _session, 1);
                                }
                            }

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)(_item.qntd);

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateBallQntd(_session.m_pi.uid,
                                    pWi.id, pWi.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);

                            ret_id = pWi.id;

                        }

                        break;
                    }
                case IFF_GROUP.CADDIE:
                    {
                        var pCi = _session.m_pi.findCaddieById(_item.id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Caddie[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                             (Pangya_DB)new CmdDeleteCaddie(_session.m_pi.uid, pCi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_ci, second.id, == , pCi->id);
                        var it = _session.m_pi.findCaddieById(pCi.id);

                        if (it.id != _session.m_pi.mp_ci.end().Key)
                        {
                            _session.m_pi.mp_ci.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.CAD_ITEM:
                    // por hora nao exclui esse por que ainda nao vi nenhum que exclui caddie item
                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.m_pi.findCardById(_item.id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Card[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pCi.qntd <= _item.qntd)
                        { // Exclui Item[Card]


                            _item.stat.qntd_ant = pCi.qntd;

                            _item.stat.qntd_dep = 0;

                            _item.STDA_C_ITEM_QNTD = (short)(pCi.qntd * -1);

                            pCi.qntd = 0;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                 (Pangya_DB)new CmdDeleteCard(_session.m_pi.uid, pCi.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_card_info, id, == , pCi->id);
                            var it = _session.m_pi.findCardById(pCi.id);

                            if (it.id != _session.m_pi.v_card_info.end().Key)
                            {
                                _session.m_pi.v_card_info.Remove(it.id);
                            }

                            ret_id = _item.id;

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = (int)pCi.qntd;

                            pCi.qntd -= _item.qntd;

                            _item.stat.qntd_dep = (int)pCi.qntd;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateCardQntd(_session.m_pi.uid,
                                    pCi.id, pCi.qntd),
                                SQLDBResponse,
                                null);

                            ret_id = pCi.id;

                        }

                        break;
                    }
                case IFF_GROUP.CHARACTER:
                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um character[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.CLUBSET: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                        var it = _session.m_pi.findWarehouseItemById(pWi.id);

                        if (it.id != 0)
                        {
                            _session.m_pi.mp_wi.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var pFi = _session.m_pi.findMyRoomItemById(_item.id);

                        if (pFi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdDeleteFurniture(_session.m_pi.uid, pFi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_mri, id, == , pFi->id);
                        var it = _session.m_pi.findMyRoomItemById(pFi.id);

                        if (it != _session.m_pi.v_mri.end())
                        {
                            _session.m_pi.v_mri.Remove(it);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.HAIR_STYLE:
                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.ITEM: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= (short)_item.qntd)
                        { // Exclui o Item[Item]

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            _item.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * -1);

                            pWi.STDA_C_ITEM_QNTD = 0;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                                SQLDBResponse,
                                null);

                            var it = _session.m_pi.findWarehouseItemById(pWi.id);

                            if (it.id != 0)
                            {
                                _session.m_pi.mp_wi.Remove(it.id);
                            }

                            ret_id = _item.id;

                            //  ITEM_MANAGER_LOG("Deletou Item", _session, _item);
                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = (int)pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)(_item.qntd);

                            _item.stat.qntd_dep = (int)pWi.STDA_C_ITEM_QNTD;

                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateItemQntd(_session.m_pi.uid,
                                    pWi.id, pWi.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);

                            ret_id = pWi.id;
                        }

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var pMi = _session.m_pi.findMascotById(_item.id);

                        if (pMi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Mascot[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdDeleteMascot(_session.m_pi.uid, pMi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_mi, id, == , pMi->id);
                        var it = _session.m_pi.findMascotById(pMi.id);

                        if (it.id != _session.m_pi.mp_mi.end().Key)
                        {
                            _session.m_pi.mp_mi.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.PART: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Part[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                        var it = _session.m_pi.findWarehouseItemById(pWi.id);

                        if (it.id != 0)
                        {
                            _session.m_pi.mp_wi.Remove(it.id);
                        }

                        ret_id = _item.id;

                        // Se deletou a Part que estava equipada 
                        // Desequipa o Part
                        var ci = _session.isPartEquiped(_item._typeid);

                        if (ci != null)
                        {

                            // Desequipa o Part
                            ci.unequipPart(_item._typeid);

                            // Update ON DB
                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateCharacterAllPartEquiped(_session.m_pi.uid, ci),
                                SQLDBResponse,
                                null);

                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] desequipou o Part[TYPEID=" + Convert.ToString(_item._typeid) + "] por que ele foi deletado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Update ON GAME
                            var p = new PangyaBinaryWriter((ushort)0x6B);

                            p.WriteByte(4); // 4 Sucesso
                            p.WriteByte(0); // Character All Parts

                            p.WriteBytes(ci.ToArray());

                            packet_func.session_send(p,
                                _session, 1);
                        }
                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um SetItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.SKIN: // Warehouse
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::removeItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou remover um Skin[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdDeleteItem(_session.m_pi.uid, pWi.id),
                            SQLDBResponse,
                            null);

                        //auto it = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, == , pWi->id);
                        var it = _session.m_pi.findWarehouseItemById(pWi.id);

                        if (it.id != 0)
                        {
                            _session.m_pi.mp_wi.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                default:
                    break;
            }

            return ret_id;
        }

        public static int removeItem(List<stItem> _v_item, Player _session)
        {
            int i = 0;
            var list = _v_item;
            for (; i < list.Count(); ++i)
            {
                if (removeItem(list[i], _session) <= 0)
                {
                    _v_item.Remove(list[i]);
                }
            }

            return (int)i;
        }

        public static int removeItem(List<stItemEx> _v_item, Player _session)
        {

            int i = 0;
            var list = _v_item;
            for (i = 0; i < list.Count(); ++i)
            {
                if (removeItem(list[i], _session) <= 0)
                {
                    _v_item.Remove(list[i]);
                }
            }
            return i;
        }

        public static object transferItem(Player _s_snd, Player _s_rcv, PersonalShopItem _psi, PersonalShopItem _psi_r)
        {


            if (!_s_rcv.getState() || !_s_snd.getState())
            {
                throw new exception("[item_manger::transferItem][Error] player _s_rcv or player _s_snd is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    8, 0));
            }

            object ret_wi = null;

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_psi.item._typeid))
            {
                case IFF_GROUP.ITEM:
                    {
                        var pWi_s = new WarehouseItemEx(_s_snd.m_pi.findWarehouseItemById(_psi.item.id));

                        if (pWi_s == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] nao tem o item[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        if (pWi_s.STDA_C_ITEM_QNTD < (int)_psi.item.qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] nao tem quantidade de item[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + ", QNTD=" + Convert.ToString(_psi.item.qntd) + "] para transferir para o PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pWi_r = _s_rcv.m_pi.findWarehouseItemByTypeid(_psi.item._typeid);

                        if (pWi_r != null)
                        { // Ele já tem atualiza

                            pWi_r.STDA_C_ITEM_QNTD += (short)_psi.item.qntd;

                            ret_wi = new WarehouseItemEx(pWi_r);

                            snmdb.NormalManagerDB.getInstance().add(9,
                                new CmdUpdateItemQntd(_s_rcv.m_pi.uid,
                                    pWi_r.id,
                                    pWi_r.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);

                        }
                        else
                        { // Cria um novo item para ele

                            WarehouseItemEx wi = new WarehouseItemEx
                            {
                                id = _psi.item.id,
                                _typeid = _psi.item._typeid,
                                type = pWi_s.type,
                                flag = 5, // Personal Shop 
                                STDA_C_ITEM_QNTD = (short)_psi.item.qntd,
                                ano = -1// Aqui tem que colocar para minutos ou segundos(acho)
                            };

                            if (wi.STDA_C_ITEM_TIME > 0)
                            {
                                wi.STDA_C_ITEM_TIME /= 24; // converte de novo para Dias para salvar no banco de dados
                            }

                            CmdAddItem cmd_ai = new CmdAddItem(_s_rcv.m_pi.uid, // Waiter
                                wi, 0, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_ai, null, null);

                            if (cmd_ai.getException().getCodeError() != 0)
                            {
                                throw cmd_ai.getException();
                            }

                            wi = new WarehouseItemEx(cmd_ai.getInfo());
                            if (wi.id <= 0)
                            {
                                _smp.message_pool.getInstance().push(new message("[Log] nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_s_rcv.m_pi.uid), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                return null;
                            }

                            _s_rcv.m_pi.mp_wi.insert(wi.id, wi);
                            ret_wi = new WarehouseItemEx(pWi_r);

                        }

                        // Att Item do player que vendeu
                        pWi_s.STDA_C_ITEM_QNTD -= (short)_psi.item.qntd;
                        if (pWi_s.STDA_C_ITEM_QNTD == 0)
                        {
                            snmdb.NormalManagerDB.getInstance().add(10, // Delete Item
                                new CmdDeleteItem(_s_snd.m_pi.uid, pWi_s.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_s_snd.m_pi.v_wi, id, == , pWi_s->id);
                            var it = _s_snd.m_pi.findWarehouseItemById(pWi_s.id);

                            if (it.id != _s_snd.m_pi.mp_wi.end().Key)
                            {
                                _s_snd.m_pi.mp_wi.Remove(it.id);
                            }

                        }
                        else
                        {
                            snmdb.NormalManagerDB.getInstance().add(9, // Update
                                new CmdUpdateItemQntd(_s_snd.m_pi.uid,
                                    pWi_s.id,
                                    pWi_s.STDA_C_ITEM_QNTD),
                                SQLDBResponse,
                                null);
                        }
                        TRANSFER_ITEM_SUCESS_MSG_LOG("Transferiu Item", _s_snd, _s_rcv, _psi);
                        // Depois que que fazer um CmdPersonalShop Venda Log
                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var pWi_s = _s_snd.m_pi.findWarehouseItemById(_psi.item.id);

                        if (pWi_s == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pWi_r = _s_rcv.m_pi.findWarehouseItemByTypeid(_psi.item._typeid);

                        if (pWi_r != null && !sIff.getInstance().IsCanOverlapped(pWi_r._typeid))
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "] tentou comprar o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que ele ja possui do PLAYER[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }
                        else
                        {

                            var part = sIff.getInstance().findPart(_psi.item._typeid);

                            if (part == null)
                            {
                                _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "] tentou comprar o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao esta no IFF_STRUCT do server, do PLAYER[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                return null;
                            }

                            // CmdTransferPart
                            snmdb.NormalManagerDB.getInstance().add(16,
                                new CmdTransferPart(_s_snd.m_pi.uid,
                                    _s_rcv.m_pi.uid, pWi_s.id,
                                    (byte)part.type_item),
                                SQLDBResponse,
                                null);

                            // Add para o player que comprou
                            ret_wi = pWi_s;
                            _s_rcv.m_pi.mp_wi.Add(pWi_s.id, pWi_s);

                            // Deleta do player que vendeu
                            //auto it = VECTOR_FIND_ITEM(_s_snd.m_pi.v_wi, id, == , pWi_s->id);
                            var it = _s_snd.m_pi.findWarehouseItemById(pWi_s.id);

                            if (it.id != _s_snd.m_pi.mp_wi.end().Key)
                            {
                                _s_snd.m_pi.mp_wi.Remove(it.id);
                            }

                        }
                        TRANSFER_ITEM_SUCESS_MSG_LOG("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        // Vai ter mas só quando eu liberar no cliente
                        var @base = sIff.getInstance().findCommomItem(_psi.item._typeid);

                        if (@base == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "] tentou comprar ClubSet[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao existe no IFF_STRUCT do server, do PLAYER[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Log][Warning] PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "] tentou comprar ClubSet[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] no Personal Shop[Owner UID=" + Convert.ToString(_s_snd.m_pi.uid) + "], mas eu ainda nao liberei " + (@base.Shop.flag_shop.can_send_mail_and_personal_shop ? "" : "no cliente e") + " no server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        TRANSFER_ITEM_SUCESS_MSG_LOG("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        // Vai ter mas só quando eu liberar no cliente
                        var @base = sIff.getInstance().findCommomItem(_psi.item._typeid);

                        if (@base == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "] tentou comprar Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao existe no IFF_STRUCT do server, do PLAYER[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pCi_s = _s_snd.m_pi.findCardById(_psi.item.id);

                        if (pCi_s == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        if (pCi_s.qntd < (int)_psi.item.qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] nao tem quantidade do Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + ", QNTD=" + Convert.ToString(_psi.item.qntd) + "] para transferir para o PLAYER[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pCi_r = _s_rcv.m_pi.findCardByTypeid(_psi.item._typeid);

                        if (pCi_r != null)
                        { // Ele já tem atualiza

                            pCi_r.qntd += (ushort)_psi.item.qntd;

                            _psi_r.item.id = pCi_r.id;

                            ret_wi = pCi_r;

                            snmdb.NormalManagerDB.getInstance().add(9,
                                new CmdUpdateCardQntd(_s_rcv.m_pi.uid,
                                    pCi_r.id, pCi_r.qntd),
                                SQLDBResponse,
                                null);

                        }
                        else
                        { // Cria um novo item para ele

                            CardInfo ci = new CardInfo();
                            ci.id = _psi.item.id;
                            ci._typeid = _psi.item._typeid;

                            ci.type = pCi_s.type;

                            ci.qntd = (ushort)_psi.item.qntd;

                            CmdAddCard cmd_ac = new CmdAddCard(_s_rcv.m_pi.uid, // Waiter
                                ci, 0, 0);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_ac, null, null);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _psi_r.item.id = ci.id;

                            if (ci.id <= 0)
                            {
                                _smp.message_pool.getInstance().push(new message("[Log] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_s_rcv.m_pi.uid), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                return null;
                            }
                            ret_wi = ci;
                            _s_rcv.m_pi.v_card_info.Add(ci.id, ci);
                        }

                        // Att Item do player que vendeu
                        pCi_s.qntd -= (_psi.item.qntd);

                        if (pCi_s.qntd == 0)
                        {
                            snmdb.NormalManagerDB.getInstance().add(10, // Delete Item
                                new CmdDeleteCard(_s_snd.m_pi.uid, pCi_s.id),
                                SQLDBResponse,
                                null);

                            //auto it = VECTOR_FIND_ITEM(_s_snd.m_pi.v_wi, id, == , pWi_s->id);
                            var it = _s_snd.m_pi.findCardById(pCi_s.id);

                            if (it.id != _s_snd.m_pi.v_card_info.end().Key)
                            {
                                _s_snd.m_pi.v_card_info.Remove(it.id);
                            }

                        }
                        else
                        {
                            snmdb.NormalManagerDB.getInstance().add(9, // Update
                                new CmdUpdateCardQntd(_s_snd.m_pi.uid,
                                    pCi_s.id, pCi_s.qntd),
                                SQLDBResponse,
                                null);
                        }
                        TRANSFER_ITEM_SUCESS_MSG_LOG("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                default: // Não suporta todos os outros, [não é permitido vender no Personal Shop]
                    _smp.message_pool.getInstance().push(new message("[item_manager::transferItem][Error] player_rcv[UID=" + Convert.ToString(_s_rcv.m_pi.uid) + "], player_snd[UID=" + Convert.ToString(_s_snd.m_pi.uid) + "] Esse Item[TYPEID=" + Convert.ToString(_psi.item._typeid) + "] nao pode ser vendido no Personal Shop", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
            }

            if (ret_wi != null)
                snmdb.NormalManagerDB.getInstance().add(15, new CmdPersonalShopLog(_s_snd.m_pi.uid, _s_rcv.m_pi.uid, _psi, _psi_r.item.id), ItemManager.SQLDBResponse, null);


            return ret_wi;
        }

        public static int exchangeCadieMagicBox(Player _session, uint _typeid, int _id, uint _qntd)
        {

            if (!_session.getState())
            {
                throw new exception("[item_manager::exchangeCadieMagicBox][Error] player _session is not connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    8, 0));
            }

            int ret_id = -1;

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CADDIE:
                    {
                        var pCi = _session.m_pi.findCaddieById(_id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de caddie é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var caddie = sIff.getInstance().findCaddie(_typeid);

                        if (caddie == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (caddie.valor_mensal > 0)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que é por tempo, isso nao é permitido pelo server", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var pMi = _session.m_pi.findMascotById(_id);

                        if (pMi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de mascot é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var mascot = sIff.getInstance().findMascot(_typeid);

                        if (mascot == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (mascot.Shop.flag_shop.time_shop.dia > 0 && mascot.Shop.flag_shop.time_shop.active)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao é permitido[de tempo] trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // passa

                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.m_pi.findCardById(_id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pCi.qntd < (int)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Card no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pCi.qntd) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var card = sIff.getInstance().findCard(_typeid);

                        if (card == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // passa

                        break;
                    }
                // Warehouse Item
                case IFF_GROUP.AUX_PART:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar AuxPart no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var auxPart = sIff.getInstance().findAuxPart(_typeid);

                        if (auxPart == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Ball no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var ball = sIff.getInstance().findBall(_typeid);

                        if (ball == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.ITEM:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Item no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var item = sIff.getInstance().findItem(_typeid);

                        if (item == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o ClubSet no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de clubset é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var clubset = sIff.getInstance().findClubSet(_typeid);

                        if (clubset == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Part no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de part é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var part = sIff.getInstance().findPart(_typeid);

                        if (part == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Skin no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de skin é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var skin = sIff.getInstance().findSkin(_typeid);

                        if (skin == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    } // End Warehouse Item
                default:
                    {
                        _smp.message_pool.getInstance().push(new message("[item_manager::exchangeCadieMagicBox][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao pode no CadieCauldron. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    } // End Default
            } // End Switch

            return ret_id;
        }

        public static List<stItem> exchangeTikiShop(Player _session, uint _typeid, int _id, uint _qntd)
        {

            if (!_session.getState())
            {
                throw new exception("[item_manager::exchangeTikiShop][Error] player _session is not connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    8, 0));
            }

            List<stItem> v_item = new List<stItem>();

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CADDIE:
                    {

                        if (_qntd > 1)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem duplicata de Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pCi = _session.m_pi.findCaddieById(_id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var caddie = sIff.getInstance().findCaddie(_typeid);

                        if (caddie == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (caddie.valor_mensal > 0)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que é por tempo, isso nao é permitido pelo server", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pCi.id;
                        item._typeid = pCi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));
                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {

                        if (_qntd > 1)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem duplicata de Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pMi = _session.m_pi.findMascotById(_id);

                        if (pMi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_qntd != 1)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de mascot é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var mascot = sIff.getInstance().findMascot(_typeid);

                        if (mascot == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (mascot.Shop.flag_shop.time_shop.dia > 0 && mascot.Shop.flag_shop.time_shop.active)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao é permitido[de tempo] trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pMi.id;
                        item._typeid = pMi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.m_pi.findCardById(_id);

                        if (pCi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pCi.qntd < (int)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Card no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pCi.qntd) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var card = sIff.getInstance().findCard(_typeid);

                        if (card == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pCi.id;
                        item._typeid = pCi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                // Warehouse Item
                case IFF_GROUP.AUX_PART:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar AuxPart no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar AuxPart no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var auxPart = sIff.getInstance().findAuxPart(_typeid);

                        if (auxPart == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_session.m_pi.isAuxPartEquiped(_typeid))
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode trocar o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] equipado no Tiki Shop. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Ball no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Ball no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var ball = sIff.getInstance().findBall(_typeid);

                        if (ball == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                case IFF_GROUP.ITEM:
                    {
                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Item no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Item no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var iff_item = sIff.getInstance().findItem(_typeid);

                        if (iff_item == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {

                        if (_qntd > 1)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem duplicata de ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o ClubSet no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar ClubSet no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var clubset = sIff.getInstance().findClubSet(_typeid);

                        if (clubset == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var part = sIff.getInstance().findPart(_typeid);

                        if (part == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_qntd > 0)
                        {
                            var pWi_all = _session.m_pi.findAllPartNotEquiped(_typeid);

                            if (pWi_all.Count() < _qntd)
                            {
                                _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Part no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[resq=" + Convert.ToString(_qntd) + ", value=" + Convert.ToString(pWi_all.Count()) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                return new List<stItem>();
                            }

                            if (_qntd == 1)
                            {
                                var pWi = _session.m_pi.findWarehouseItemById(_id);

                                if (pWi == null)
                                {
                                    _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    return new List<stItem>();
                                }

                                if ((pWi.flag & 96) == 96
                                    || (pWi.flag & 0x20) == 0x20
                                    || (pWi.flag & 0x40) == 0x40)
                                {
                                    _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode trocar Part Rental[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no Tiki Shop. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    return new List<stItem>();
                                }

                                var item = new stItem();

                                item.type = 2;
                                item.id = pWi.id;
                                item._typeid = pWi._typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                                v_item.Add(new stItem(item));
                            }
                            else
                            {
                                for (var i = 0; i < _qntd; ++i)
                                {
                                    var item = new stItem();

                                    item.type = 2;
                                    item.id = pWi_all[i].id;
                                    item._typeid = pWi_all[i]._typeid;
                                    item.qntd = 1;
                                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                                    v_item.Add(new stItem(item));
                                }
                            }
                        }

                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        if (_qntd > 1)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem duplicata de Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pWi = _session.m_pi.findWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Skin no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar Skin no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var skin = sIff.getInstance().findSkin(_typeid);

                        if (skin == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem o Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = (int)_qntd;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));

                        break;
                    } // End Warehouse Item
                default:
                    {
                        _smp.message_pool.getInstance().push(new message("[item_manager::exchangeTikiShop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar um item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao pode no CadieCauldron. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    } // End Default
            } // End Switch

            return v_item;
        }

        public static void openTicketReportScroll(Player _session, int _ticket_scroll_item_id, int _ticket_scroll_id, bool _upt_on_game = false)
        {
            if (_session == null || !_session.getState()) return;

            var p = new PangyaBinaryWriter();

            try
            {
                if (_ticket_scroll_item_id < 0 || _ticket_scroll_id < 0)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] ITEM_ID ou ID inválido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2500, 0));
                }

                var pWi = _session.m_pi.findWarehouseItemById(_ticket_scroll_item_id);
                if (pWi == null)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] Player não tem o item.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2501, 0));
                }

                // Validação C1/C2 (Bitmask)
                uint expectedId = (uint)(pWi.c[1] * 0x800) | (uint)(ushort)pWi.c[2];
                if (expectedId != (uint)_ticket_scroll_id)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] ID do Ticket não bate. Esperado: " + expectedId,
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2502, 0));
                }

                // Busca dados no Banco
                CmdTicketReportDadosInfo cmd_trdi = new CmdTicketReportDadosInfo(_ticket_scroll_id);
                snmdb.NormalManagerDB.getInstance().add(0, cmd_trdi, null, null);

                if (cmd_trdi.getException().getCodeError() != 0)
                    throw cmd_trdi.getException();

                var trsi = cmd_trdi.getInfo();
                if (trsi == null) throw new Exception("Dados do ticket retornaram nulos.");

                // 1. Calcular EXP ANTES de remover o item
                var playerStat = trsi.v_players.FirstOrDefault(_el => _el.uid == _session.m_pi.uid);
                int expToGain = (playerStat != null && playerStat.exp > 0) ? (int)playerStat.exp : 0;

                // 2. Remover o Item
                stItem itemRem = new stItem();
                itemRem.type = 2;
                itemRem.id = pWi.id;
                itemRem._typeid = pWi._typeid;
                itemRem.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * -1);

                if (removeItem(itemRem, _session) <= 0)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] Falha ao deletar item.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2503, 0));
                }

                // 3. Checar se o item estava expirado (UpdateItem)
                var ui_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_ticket_scroll_item_id, UpdateItem.UI_TYPE.WAREHOUSE);

                // CORREÇÃO DO CRASH: Checar se ui_it não é nulo antes do Count
                if (ui_it != null && ui_it.Count > 0)
                {
                    // Se expirou, remove do mapa de updates
                    _session.m_pi.mp_ui.Remove(ui_it.First().Key);

                    // Se expirou, damos a exp e paramos por aqui com erro de expiração
                    if (expToGain > 0) _session.addExp(expToGain, _upt_on_game);

                    throw new exception("Item expirado, mas EXP concedida.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2504, 0));
                }

                // 4. Fluxo Normal (Sucesso)
                _smp.message_pool.getInstance().push(new message("[item_manager::openTicketReportScroll][Log] Player " + _session.m_pi.uid + " abriu ticket com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia pacote de resposta do Ticket
                p.init_plain(0x11A);
                p.WriteUInt32((uint)trsi.v_players.Count());
                p.WriteTime(trsi.date);
                foreach (var el in trsi.v_players)
                    p.WriteBytes(el.ToArray());

                packet_func.session_send(p, _session, 1);

                // 5. Adiciona EXP por ÚLTIMO (para o visual do Pangya não bugar)
                if (expToGain > 0)
                {
                    _session.addExp(expToGain, _upt_on_game);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[item_manager::openTicketReportScroll][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta de Erro para o Cliente (Packet 0x11A com erro)
                p.init_plain(0x11A);
                p.WriteInt32(-1); // Código de Erro
                p.WriteZeroByte(16); // Data vazia
                packet_func.session_send(p, _session, 1);
            }
        }

        public static bool isSetItem(uint _typeid)
        {
            return sIff.getInstance().getItemGroupIdentify(_typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.SET_ITEM;
        }

        public static bool isTimeItem(stItem.stDate _date)
        {
            return (_date.active == 1 || _date.date.sysDate[0].Year != 0 || _date.date.sysDate[1].Year != 0);
        }

        public static bool isTimeItem(stItem.stDate.stDateSys _date)
        {
            return (_date.sysDate[0].Year != 0 || _date.sysDate[1].Year != 0);
        }

        public static bool ownerItem(uint _uid, uint _typeid)
        {

            bool ret = false;

            // Procura primeiro no Dolfini Locker o item, se for diferente de SetItem
            if (sIff.getInstance().getItemGroupIdentify(_typeid) != IFF_GROUP.SET_ITEM)
            {

                var cmd_dli = new CmdFindDolfiniLockerItem(_uid, // Waiter
                    _typeid);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_dli, null, null);

                if (cmd_dli.getException().getCodeError() != 0)
                {
                    throw cmd_dli.getException();
                }

                if (cmd_dli.hasFound())
                {
                    return true;
                }
            }
            // Find de busca em dolfini locker

            switch ((IFF_GROUP)sIff.getInstance().getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CHARACTER:
                    {
                        var cmd_fc = new CmdFindCharacter(_uid, // Waiter
                            _typeid);

                        snmdb.NormalManagerDB.getInstance().add(0,
                            cmd_fc, null, null);

                        if (cmd_fc.getException().getCodeError() != 0)
                        {
                            throw cmd_fc.getException();
                        }

                        ret = cmd_fc.hasFound();

                        break;
                    }
                case IFF_GROUP.CADDIE:
                    {
                        var cmd_fc = new CmdFindCaddie(_uid, // Waiter
                            _typeid);

                        snmdb.NormalManagerDB.getInstance().add(0,
                            cmd_fc, null, null);

                        if (cmd_fc.getException().getCodeError() != 0)
                        {
                            throw cmd_fc.getException();
                        }

                        ret = cmd_fc.hasFound();

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var mi = _ownerMascot(_uid, _typeid);

                        ret = (mi.id > 0); //cmd_fm.hasFound();

                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var card = _ownerCard(_uid, _typeid);

                        ret = (card.id > 0); //cmd_fc.hasFound();

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var cmd_ff = new CmdFindFurniture(_uid, // Waiter
                            _typeid);

                        snmdb.NormalManagerDB.getInstance().add(0,
                            cmd_ff, null, null);

                        if (cmd_ff.getException().getCodeError() != 0)
                        {
                            throw cmd_ff.getException();
                        }

                        ret = cmd_ff.hasFound();

                        break;
                    }
                case IFF_GROUP.BALL:
                case IFF_GROUP.AUX_PART:
                case IFF_GROUP.CLUBSET:
                case IFF_GROUP.ITEM:
                case IFF_GROUP.PART:
                case IFF_GROUP.SKIN:
                    {
                        var aux_part = _ownerAuxPart(_uid, _typeid);

                        ret = (aux_part.id > 0); //cmd_fwi.hasFound();

                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    ret = ownerSetItem(_uid, _typeid);
                    break;
                case IFF_GROUP.HAIR_STYLE:
                    ret = ownerHairStyle(_uid, _typeid);
                    break;
                case IFF_GROUP.CAD_ITEM: // Esse aqui verifica se já tem, mas não que não pode ter mais. mas sim para aumentar o tempo
                    ret = ownerCaddieItem(_uid, _typeid);
                    break;
                case IFF_GROUP.MATCH:
                    {
                        var tsi = _ownerTrofelEspecial(_uid, _typeid);

                        ret = (tsi.id > 0);

                        break;
                    } // End iff::MATCH
            } // End Switch

            // Player não tem o item no warehouse e nem no Dolfini Locker, Verifica no Mail Box dele
            if (!ret)
            {

                // Procura no Mail Box do player
                ret = ownerMailBoxItem(_uid, _typeid);
            }

            return ret;
        }

        public static bool ownerSetItem(uint _uid, uint _typeid)
        {

            var set = sIff.getInstance().findSetItem(_typeid);

            if (set != null)
            {
                for (var i = 0; i < (set.packege.item_typeid.Length); ++i)
                {
                    // Eleminar a verificação do character que ele só inclui se o player não tiver ele
                    // se ele tiver não faz diferença não anula o verificação do set
                    if (set.packege.item_typeid[i] != 0 && sIff.getInstance().getItemGroupIdentify(set.packege.item_typeid[i]) != IFF_GROUP.CHARACTER)
                    {
                        if (ownerItem(_uid, set.packege.item_typeid[i])) // se tiver 1 item que seja não pode ganhar o set se não vai duplicar os itens, que ele tem
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool ownerCaddieItem(uint _uid, uint _typeid)
        {

            var caddie = _ownerCaddieItem(_uid, _typeid);

            if (!(caddie.id > 0)) // Não tem o caddie então não pode ter o caddie item
            {
                return true;
            }

            // Pode enviar o mesmo


            return false;
        }

        public static CaddieInfoEx _ownerCaddieItem(uint _uid, uint _typeid)
        {

            var cmd_fc = new CmdFindCaddie(_uid, // Waiter
                (uint)(sIff.getInstance().CADDIE << 26) | sIff.getInstance().getCaddieIdentify(_typeid));

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fc, null, null);

            if (cmd_fc.getException().getCodeError() != 0)
            {
                throw cmd_fc.getException();
            }

            return cmd_fc.getInfo();
        }

        public static CharacterInfo _ownerHairStyle(uint _uid, uint _typeid)
        {

            var hair = sIff.getInstance().findHairStyle(_typeid);

            if (hair != null)
            {

                var cmd_fc = new CmdFindCharacter(_uid, // Waiter
                    (uint)(sIff.getInstance().CHARACTER << 26) | hair.Character);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_fc, null, null);

                if (cmd_fc.getException().getCodeError() != 0)
                {
                    throw cmd_fc.getException();
                }

                return cmd_fc.getInfo();
            }

            return new CharacterInfo();
        }

        public static MascotInfoEx _ownerMascot(uint _uid, uint _typeid)
        {

            var cmd_fm = new CmdFindMascot(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fm, null, null);

            if (cmd_fm.getException().getCodeError() != 0)
            {
                throw cmd_fm.getException();
            }

            return cmd_fm.getInfo();
        }

        public static WarehouseItemEx _ownerBall(uint _uid, uint _typeid)
        {

            var cmd_fwi = new CmdFindWarehouseItem(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fwi, null, null);

            if (cmd_fwi.getException().getCodeError() != 0)
            {
                throw cmd_fwi.getException();
            }

            return cmd_fwi.getInfo();
        }

        public static CardInfo _ownerCard(uint _uid, uint _typeid)
        {

            var cmd_fc = new CmdFindCard(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fc, null, null);

            if (cmd_fc.getException().getCodeError() != 0)
            {
                throw cmd_fc.getException();
            }

            return cmd_fc.getInfo();
        }

        public static WarehouseItemEx _ownerAuxPart(uint _uid, uint _typeid)
        {

            CmdFindWarehouseItem cmd_fwi = new CmdFindWarehouseItem(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fwi, null, null);

            if (cmd_fwi.getException().getCodeError() != 0)
            {
                throw cmd_fwi.getException();
            }

            return cmd_fwi.getInfo();
        }

        public static WarehouseItemEx _ownerItem(uint _uid, uint _typeid)
        {

            CmdFindWarehouseItem cmd_fwi = new CmdFindWarehouseItem(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fwi, null, null);

            if (cmd_fwi.getException().getCodeError() != 0)
            {
                throw cmd_fwi.getException();
            }

            return cmd_fwi.getInfo();
        }

        public static TrofelEspecialInfo _ownerTrofelEspecial(uint _uid, uint _typeid)
        {

            var type_trofel = sIff.getInstance().getItemSubGroupIdentify24(_typeid);

            CmdFindTrofelEspecial.eTYPE type = CmdFindTrofelEspecial.eTYPE.ESPECIAL;

            if (type_trofel == 1 || type_trofel == 2)
            {
                type = CmdFindTrofelEspecial.eTYPE.ESPECIAL;
            }
            else if (type_trofel == 3)
            {
                type = CmdFindTrofelEspecial.eTYPE.GRAND_PRIX;
            }

            CmdFindTrofelEspecial cmd_fts = new CmdFindTrofelEspecial(_uid, // Waiter
                _typeid, type);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fts, null, null);

            if (cmd_fts.getException().getCodeError() != 0)
            {
                throw cmd_fts.getException();
            }

            return cmd_fts.getInfo();
        }

        public static bool ownerHairStyle(uint _uid, uint _typeid)
        {

            var hair = sIff.getInstance().findHairStyle(_typeid);

            if (hair != null)
            {
                var character = _ownerHairStyle(_uid, _typeid);

                if (!(character.id > 0)) // Não tem o Character
                {
                    return true;
                }

                if (character.default_hair == hair.Color)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ownerMailBoxItem(uint _uid, uint _typeid)
        {

            CmdFindMailBoxItem cmd_fmbi = new CmdFindMailBoxItem(_uid, // Waiter
                _typeid);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fmbi, null, null);

            if (cmd_fmbi.getException().getCodeError() != 0)
            {
                throw cmd_fmbi.getException();
            }

            if (cmd_fmbi.hasFound())
            {
                return true;
            }

            return false;
        }

        public static bool IsBetween(SYSTEMTIME start, SYSTEMTIME end, SYSTEMTIME current)
        {
            // Converte SYSTEMTIME para DateTime (ou null se Year for 0)
            DateTime dtStart = (start.Year > 0) ? UtilTime.ToDateTime(start) : DateTime.MaxValue;
            DateTime dtEnd = (end.Year > 0) ? UtilTime.ToDateTime(end) : DateTime.MaxValue;
            DateTime dtCurrent = UtilTime.ToDateTime(current);

            if (dtStart != DateTime.MaxValue)
            {
                if (!(dtEnd != DateTime.MaxValue))
                    return dtCurrent >= dtStart; // Só tem início (a2 <= a4)

                // Tem ambos (a2 <= a4 <= a3)
                return dtCurrent >= dtStart && dtCurrent <= dtEnd;
            }
            else if (dtEnd != DateTime.MaxValue)
            {
                return dtCurrent <= dtEnd; // Só tem fim (a4 <= a3)
            }

            return true; // Ambos nulos (Igual ao return 1 do assembly)
        }
        public static bool betweenTimeSystem(ref stItem.stDate _date)
        {

            if (!isTimeItem(_date))
                throw new Exception("[item_manager::betweenTimeSystem][Error] Item nao e um item de tempo.");


            SYSTEMTIME st = new SYSTEMTIME();//gerar o datetime now
            UtilTime.GetLocalTime(ref st);

            return IsBetween(_date.date.sysDate[0], _date.date.sysDate[1], st);
        }

        public static bool betweenTimeSystem(IFFDate _date)
        {
            var date = new stItem.stDate();
            date.active = (uint)(_date.active ? 1 : 0);
            date.date.sysDate[0].Year = _date.Start.Year;
            date.date.sysDate[0].Day = _date.Start.Day;
            date.date.sysDate[0].Hour = _date.Start.Hour;
            date.date.sysDate[0].Minute = _date.Start.Minute;
            date.date.sysDate[0].MilliSecond = _date.Start.MilliSecond;
            date.date.sysDate[0].Second = _date.Start.Second;
            date.date.sysDate[0].Month = _date.Start.Month;
            date.date.sysDate[0].DayOfWeek = _date.Start.DayOfWeek;

            date.date.sysDate[1].Year = _date.End.Year;
            date.date.sysDate[1].Day = _date.End.Day;
            date.date.sysDate[1].Hour = _date.End.Hour;
            date.date.sysDate[1].Minute = _date.End.Minute;
            date.date.sysDate[1].MilliSecond = _date.End.MilliSecond;
            date.date.sysDate[1].Second = _date.End.Second;
            date.date.sysDate[1].Month = _date.End.Month;
            date.date.sysDate[1].DayOfWeek = _date.End.DayOfWeek;
            return betweenTimeSystem(ref date);
        }

        public static bool betweenTimeSystem(stItem.stDate.stDateSys _date)
        {
            stItem.stDate date = new stItem.stDate(0, _date);

            return betweenTimeSystem(ref date);
        }

        private static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[item_manager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // isso aqui depois pode mudar para o Item_manager, que vou tirar de ser uma classe static e usar ela como objeto(instancia)
            //auto _session = reinterpret_cast< player* >(_arg);

            switch (_msg_id)
            {
                case 1: // Add Character
                    {
                        // Não usa mais
                        break;
                    }
                case 2: // add Caddie
                    {
                        // Não usa mais
                        break;
                    }
                case 3: // add Warehouse
                    {
                        // Não usa mais
                        break;
                    }
                case 4: // Add HairStyle
                    {
                        // Esse aqui só tem att o cabelo no character do player
                        break;
                    }
                case 5: // Caddie Item
                    {
                        // Esse aqui só att o parts typeid do caddie e o tempo no DB, não precisa esperar a resposta do DB já que ele não retorna nada
                        break;
                    }
                case 6: // Update Mascot Time
                    {
                        // Não usa mais
                        break;
                    }
                case 7: // Update Ball Quantidade
                    {
                        // Não usa mais
                        break;
                    }
                case 8: // Update Card Quantidade
                    {
                        // Não usa mais
                        break;
                    }
                case 9: // Update Item Quantidade
                    {
                        // Não usa mais
                        break;
                    }
                case 10: // Delete Item
                    {
                        // Não usa mais
                        break;
                    }
                case 11: // Delete Ball
                    {
                        // Não usa mais
                        break;
                    }
                case 12: // Delete Card
                    {
                        // Não usa mais
                        break;
                    }
                case 13: // Gift ClubSet
                    {
                        // Não usa mais
                        break;
                    }
                case 14: // Gift Part
                    {
                        // Não usa mais
                        break;
                    }
                case 15: // Personal Shop Log
                    {
                        // Não usa por que não retorna nada é um INSERT
                        break;
                    }
                case 16: // Personal Shop Transfer Part
                    {
                        // Não usa por que não retorna nada é um UPDATE
                        break;
                    }
                case 17: // Update Character Equipped
                    {
                        // Não usa por que não retorna nada é um UPDATE
                        break;
                    }
                case 18: // Update Trofel Especial e Grand Prix
                    {
                        // Não usa por que não retorna nada é um UPDATE
                        break;
                    }
                case 19: // Update Premium Ticket Time
                    {
                        // Não usa por que não retorna nada é um UPDATE
                        break;
                    }
                case 20: // Update Clubset Time
                    {
                        // Não uda por que não retorna nada é um UPDATE
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        public static uint CalculateSafeExchangeCount(
        Player session,
        CadieExchangeItem cost,
        uint clientRequested,
        uint hardLimit
    )
        {
            if (clientRequested == 0)
                throw new exception(
                    $"[CHEAT] clientRequested = 0",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 900, 0xDEAD0001)
                );

            if (cost.QtyPerExchange == 0)
                throw new exception(
                    $"[Error] QtyPerExchange = 0 (server config inválido)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0xDEAD0002)
                );

            uint playerHas = getItemQuantity(
                session,
                cost._typeid,
                cost.id
            );

            uint maxPossible = playerHas / cost.QtyPerExchange;

            if (maxPossible == 0)
                throw new exception(
                    $"[Error] player não possui itens suficientes",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 902, 0xDEAD0003)
                );

            if (clientRequested > maxPossible)
            {
                throw new exception(
                    $"[CHEAT] UID={session.m_pi.uid} RequestCount ={clientRequested}, RequestMax={maxPossible} quantidade inválida",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 903, 0xDEAD0004)
                );
            }

            if (clientRequested > hardLimit)
            {
                _smp.message_pool.getInstance().push(
                    new message(
                        $"[WARN] UID={session.m_pi.uid} limitado de {clientRequested} para {hardLimit}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    )
                );

                return hardLimit;
            }

            return clientRequested;
        }

        public static uint getItemQuantity(Player session, uint typeid, int itemId)
        {
            var type = (IFF_GROUP)sIff.getInstance().getItemGroupIdentify(typeid);
            switch (type)
            {
                case IFF_GROUP.CARD:
                    {
                        var item = session.m_pi.findCardById(itemId);
                        if (item == null)
                        {
                            return 0;
                        }
                        return (uint)item.qntd;
                    }
                case IFF_GROUP.CADDIE:
                case IFF_GROUP.PART:
                case IFF_GROUP.AUX_PART:
                case IFF_GROUP.CLUBSET:
                case IFF_GROUP.ITEM:
                    {
                        if (type == IFF_GROUP.PART || type == IFF_GROUP.CLUBSET)
                        {
                            var item = session.m_pi.findWarehouseItemById(itemId);

                            // Se o item não existe no warehouse, retorna 0
                            if (item == null) return 0; 

                            return (uint)((item != null) ? (uint)item.STDA_C_ITEM_QNTD == 0 ? 1 : item.STDA_C_ITEM_QNTD : 0);
                        }
                        else
                        {
                            if (type == IFF_GROUP.CADDIE)
                            {
                                var item = session.m_pi.findCaddieById(itemId);

                                // Se o item não existe no warehouse, retorna 0
                                if (item == null) return 0;
                                 
                                return 1;
                            }
                        }

                        // Para outros itens (Consumíveis), retorna a quantidade real
                        var warehouseItem = session.m_pi.findWarehouseItemById(itemId);
                        return (uint)((warehouseItem != null) ? (uint)warehouseItem.STDA_C_ITEM_QNTD == 0? 1: warehouseItem.STDA_C_ITEM_QNTD : 0);
                    }
                default:
                    return 0;
            }
        }
    }
}