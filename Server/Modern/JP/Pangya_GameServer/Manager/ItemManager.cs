using Pangya_GameServer.Feature;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Models;
using PangyaAPI.IFF.Regions.JP.Models.Generic;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using snmdb;
using System.Runtime.CompilerServices;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Manager
{
    /// <summary>
    /// Manipulation Add, Check, Remove Items
    /// </summary>
    public class ItemManager
    {
        public ItemManager()
        {
        }


        private static void LogTransferItemSuccess(string log, Player _s_snd, Player _s_rcv, PersonalShopItem _psi, [CallerMemberName] string func = "")
        {
#if DEBUG
            type_msg MSG_ACTIVE_DEBUG = type_msg.CL_FILE_LOG_AND_CONSOLE;
#else
    type_msg MSG_ACTIVE_DEBUG = type_msg.CL_ONLY_FILE_LOG;
#endif

            _smp.LogManager.Instance.push(new AppMessage($"[ItemManager::{func}][Sucess] Player_s[UID=" + (_s_snd.Inventory.uid) + "] " + log + "[Typeid="
            + (_psi.item._typeid) + ", ID=" + (_psi.item.id) + ", QNTD=" + (_psi.item.qntd) + ", PANG(qntd*unit)="
            + (_psi.item.pang * (ulong)_psi.item.qntd) + "] transferiu para o Player_r[UID=" + (_s_rcv.Inventory.uid) + "]", MSG_ACTIVE_DEBUG));

        }

        private static void LogItemManager(string log, Player _session, stItem _item, [CallerMemberName] string func = "")
        {
#if DEBUG
            type_msg MSG_ACTIVE_DEBUG = type_msg.CL_FILE_LOG_AND_CONSOLE;
#else
    type_msg MSG_ACTIVE_DEBUG = type_msg.CL_ONLY_FILE_LOG;
#endif

            _smp.LogManager.Instance.push(new AppMessage(
                $"[ItemManager::{func}][Sucess] Normal[UID={_session.Inventory.uid}] {log} [Typeid={_item._typeid}, ID={_item.id}, QNTD={(_item.STDA_C_ITEM_QNTD > 0 && _item.qntd <= 0xFFu ? _item.STDA_C_ITEM_QNTD : _item.qntd)}]",
                MSG_ACTIVE_DEBUG));
        }


        static void InitCommonBuyItem(ref stItem _item, BuyItem _bi, IFFCommon item)
        {
            _item.id = _bi.id;
            _item._typeid = _bi._typeid;
            _item.qntd = (int)_bi.qntd;
            _item.date.date.sysDate[0] = new SystemTime(item.date.Start.Year, item.date.Start.Month, item.date.Start.Day, item.date.Start.Hour, item.date.Start.Minute, item.date.Start.Second, item.date.Start.MilliSecond);//check start later
            _item.date.date.sysDate[1] = new SystemTime(item.date.End.Year, item.date.End.Month, item.date.End.Day, item.date.End.Hour, item.date.End.Minute, item.date.End.Second, item.date.End.MilliSecond);//check End later
            _item.date.active = Convert.ToUInt32(item.date.active);//check State date later
            _item.price = item.Shop.Price;
            _item.desconto = item.Shop.DiscountPrice;
            _item.is_cash = (byte)(item.Shop.flag_shop.IsCash ? 1 : 0);
            _item.type = 2; /*aqui é o valor padrão, mas outros iff pode mexer nele depois*/
        }

        static void BeginInitBuyItem(PlayerInfo player, ref stItem item, BuyItem buyItem, bool isGift, bool skipLevelCheck, IFFCommon iffInfo)
        {
            if (iffInfo == null || iffInfo.ID == 0)
            {
                iffInfo = sIff.Instance.findCommomItem(buyItem._typeid);
            }

            if (iffInfo != null)
            {
                CheckItemLevel(player, ref item, isGift, skipLevelCheck, iffInfo);
                CheckIsGift(player, ref item, isGift, skipLevelCheck, iffInfo);
                InitCommonBuyItem(ref item, buyItem, iffInfo);
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[ItemManager::BeginInitBuyItem][Error] UID={player.UID}, TID={buyItem._typeid}] ITEM NOT FOUND",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        static void CheckItemLevel(PlayerInfo _pi, ref stItem _item, bool _gift_opt, bool _chk_lvl, IFFCommon item)
        {
            if (!_gift_opt && !_chk_lvl && !item.Level.GoodLevel((byte)_pi.Level))
            {
                _smp.LogManager.Instance.push(new AppMessage(
            $"[Log] Normal[UID={_pi.UID}] nao tem o Level[value={item.Level}] necessario para comprar esse item[TYPEID={item.ID}]",
            type_msg.CL_FILE_LOG_AND_CONSOLE));
                _item._typeid = 0;
                return;
            }
        }

        static void CheckIsGift(PlayerInfo _pi, ref stItem _item, bool _gift_opt, bool _chk_lvl, IFFCommon item)
        {
            if (_gift_opt && !sIff.Instance.IsGiftItem(item.ID))
            {
                _smp.LogManager.Instance.push(new AppMessage(
                            $"[Log] Normal[UID={_pi.UID}] tentou presentear um item que não pode ser presenteado.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                
                _item._typeid = 0;
                return;
            }
        }

        static int TranslateFlagTimeToSeconds(int flagTime, int value)
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

        static int TranslateFlagTimeToHour(int flagTime, int value)
        {
            return TranslateFlagTimeToSeconds(flagTime, value) / 3600;
        }

        // Gets
        public static List<stItem> GetItemOfSetItem(Player _session, uint _typeid, bool _shop, int _chk_level)
        {

            if (!isSetItem(_typeid))
            {
                throw new exception("[ItemManager::getItemOfSetItem][Error] item[TYPEID=" + Convert.ToString(_typeid) + "] not is a valid SetItem. Player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    1, 0));
            }

            List<stItem> v_item = new List<stItem>();
            stItem item = new stItem();
            BuyItem bi = new BuyItem();

            SetItem set_item = sIff.Instance.findSetItem(_typeid);

            if (set_item == null)
            {
                throw new exception("[ItemManager::getItemOfSetItem][Error] item[TYPEID=" + Convert.ToString(_typeid) + "] nao foi encontrado. Player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                    initItemFromBuyItem(_session.UserInfo,
                        item, bi, _shop, 0, 0,
                        _chk_level);

                    if (item._typeid != 0)
                    {
                        v_item.Add(new stItem(item));
                    }
                    else
                    {
                        throw new exception("[ItemManager::getItemOfSetItem][Error] erro ao inicializar item[TYPEID=" + Convert.ToString(set_item.packege.item_typeid[i]) + "]. Player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

            switch (sIff.Instance.getItemGroupIdentify(_bi._typeid))
            {
                case IFF_GROUP.CHARACTER:
                    {
                        var item = sIff.Instance.findCharacter(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);


                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var item = sIff.Instance.findPart(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_option == 1 && item.valor_rental > 0)
                        {
                            _item.price = item.valor_rental;
                            _item.is_cash = 0; // Pang, por que é rental
                            _item.STDA_C_ITEM_TIME = 7; // 7 dias    // no original é no C[3] o tempos
                            _item.flag = 0x60; // dias Rental(acho)
                                               // time Type 6 rental, 4, 2,
                            _item.flag_time = 6;
                        }
                        else if (_bi.time > 0)
                        { // Roupa de tempo do CadieCauldron

                            if (item.Shop.flag_shop.time_shop.active)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][WARNIG] Normal[UID=" + Convert.ToString(_pi.UID) + "] inicializou Part[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo[VALUE=" + Convert.ToString(_bi.time) + "], mas no IFF_STRUCT do server ele nao é um item por tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                        var item = sIff.Instance.findClubSet(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

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
                        var item = sIff.Instance.findBall(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

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
                        var item = sIff.Instance.findItem(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //antes esse codigo, estava abaixo da verificacao, por isso estava
                        //vindo zerado, o objeto, precisa ser carregado, se nao dar bug
                        // Copia C[] do IFF::Item para o _item
                        for (int i = 0; i < _item.c.Length; i++)
                        {
                            _item.c[i] = (short)item.Stats.getSlot[i];
                        }


                        // essa regra é para os itens que é 1 item, mas tem mais quantidade que vai ser add, 1 item Normal, mas ele vem 10, fica no STDA_C_ITEM_QNTD
                        if (_item.qntd > 0 && _item.STDA_C_ITEM_QNTD != 0 && (_item.STDA_C_ITEM_QNTD == 1 || _item.STDA_C_ITEM_QNTD == _item.qntd))
                            _item.qntd /= (int)_item.STDA_C_ITEM_QNTD;


                        // Tem preço de tempo
                        var empty_price = sIff.Instance.EMPTY_ARRAY_PRICE(_item.c);

                        if (_bi.time > 0
                            && !empty_price
                            && sIff.Instance.getEnchantSlotStat(_item._typeid) == 0x21)
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
                                    _smp.LogManager.Instance.push(new AppMessage("[Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "]. Queria colocar mais[request=" + Convert.ToString(_bi.time) + "] que 365 dia na compra do Premium Ticket. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }

                        }
                        else if (_bi.time == 0 && !empty_price && sIff.Instance.getEnchantSlotStat(_item._typeid) == 0x21 && _bi.qntd > 0)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                                    _smp.LogManager.Instance.push(new AppMessage("[Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "]. Queria colocar mais[request=" + Convert.ToString(_bi.time) + "] que 365 dia na compra do Premium Ticket. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                        if (sIff.Instance.IsItemEquipable(_bi._typeid))
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
                        var item = sIff.Instance.findCaddie(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

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
                        var item = sIff.Instance.findCaddieItem(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.Instance.EMPTY_ARRAY_PRICE(item.price);

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

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] inicializou Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                                    _item.price = sIff.Instance.SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 2;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)(_bi.time * 24); // Horas

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Error] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Caddie Item[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    {
                        var item = sIff.Instance.findSetItem(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        var item = sIff.Instance.findSkin(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // ESSE AQUI É ONDE COMEÇA OS TEMPO, [1] É DO 1 A 365 DIAS, [7] É DO 7 A 365 DIAS
                        //item->Shop.flag_shop.time_shop.uc_time_start //---- AS SKINS É DO 7

                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.Instance.EMPTY_ARRAY_PRICE(item.price);

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
                                    _item.price = sIff.Instance.SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.flag_time = 4;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time > 0 && (item.Shop.flag_shop.time_shop.active || item.Shop.flag_shop.time_shop.dia > 0))
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] inicializou Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                                    _item.price = sIff.Instance.SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.flag_time = 4;
                            _item.flag = 0x20;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Error] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Skin[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.HAIR_STYLE:
                    {
                        var item = sIff.Instance.findHairStyle(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var item = sIff.Instance.findMascot(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        // Dias
                        // Aqui não precisa ver se tem time_limit e time_start, so tem que verificar se tem o item->price[0~4]
                        var empty_price = sIff.Instance.EMPTY_ARRAY_PRICE(item.price);

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
                                && !sIff.Instance.IsBuyItem(item.ID)
                                && !sIff.Instance.IsGiftItem(item.ID))
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] inicializou Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] com tempo no jogo e no IFF_STRUCT, mas ele nao tem os precos de tempo no IFF_STRUCT. Box ou Comando GM", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Warning] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                                    _item.price = sIff.Instance.SUM_ARRAY_PRICE_ULONG(item.price);
                                    break;
                            }

                            _item.STDA_C_ITEM_QNTD = 1; // item->Shop.flag_shop.time_shop.uc_time_start;
                            _item.flag_time = 4;
                            _item.flag = 0x40;
                            _item.STDA_C_ITEM_TIME = (short)_bi.time; // Dias

                        }
                        else if (_bi.time == 0 && !empty_price && _bi.qntd == 0)
                        {

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::initItemFromBuyItem][Error] Normal[UID=" + Convert.ToString(_pi.UID) + "] tentou inicializar Mascot[TYPEID=" + Convert.ToString(_bi._typeid) + "] sem tempo e sem quantidade no jogo e no IFF_STRUCT ele tem tempo. Hacker ou Command GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _item._typeid = 0;

                            return;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var item = sIff.Instance.findFurniture(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD == 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.AUX_PART:
                    {
                        var item = sIff.Instance.findAuxPart(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD == 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var item = sIff.Instance.findCard(_bi._typeid);

                        BeginInitBuyItem(_pi, ref _item, _bi, _gift_opt.IsTrue(), _chk_lvl.IsTrue(), (item != null && item.ID == 0) ? null : item);

                        if (_item.STDA_C_ITEM_QNTD <= 0)
                        {
                            _item.STDA_C_ITEM_QNTD = (short)_item.qntd;
                        }

                        //END_INIT_BUYITEM; nao é nada, so um log simples
                        break;
                    }
                default: // Não tem esse item para vender no ShopRoom
                    _smp.LogManager.Instance.push(new AppMessage("Normal[UID=" + Convert.ToString(_pi.UID) + "] Tentou comprar um item que nao tem no ShopRoom para vender. typeid: " + Convert.ToString(_bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    break;
            }
        }

        public static void initItemFromEmailItem(PlayerInfo _pi, stItem _item, EmailInfo.ItemGift _ei_item)
        {

            BuyItem item = new BuyItem();

            item.id = (int)_ei_item.id;
            item._typeid = _ei_item._typeid;
            item.qntd = _ei_item.qntd;
            item.time = (short)(_ei_item.flag_time == 2 ? _ei_item.tempo_qntd / 24 : _ei_item.tempo_qntd);

            _item.flag_time = _ei_item.flag_time;

            // Aqui tem que criar o proprio dele por que tem o Type do tempo[dias, horas, minutos, segundos] e etc
            initItemFromBuyItem(_pi,
                _item, item, false, 0, 0, 1);
        }

        // Check is have setitem in email
        public static void CheckSetItemOnEmail(Player _session, EmailInfo _ei)
        {

            if (_ei.itens.Count == 0 && _ei.id <= 0)
            {
                throw new exception("[ItemManager::CheckSetItemOnEmail][Error] email not have item for check", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                    20, 0));
            }

            for (var i = 0; i < _ei.itens.Count; ++i)
            {
                if (isSetItem(_ei.itens[i]._typeid))
                {
                    var v_item = GetItemOfSetItem(_session,
                        _ei.itens[i]._typeid, false, 1);

                    if (v_item.Any())
                    {

                        foreach (var el in v_item)
                            _ei.itens.Add(new EmailInfo.ItemGift(-1, el._typeid, el.flag_time, (uint)el.qntd, (ushort)el.STDA_C_ITEM_TIME, 0, 0, 0/*type GM*/, 0, "", 0));

                        _ei.itens.RemoveAt(i--);
                    }
                }
            }
        }

        // Add Itens

        public static int addItem(stItem _item, uint _uid, /*era byte*/ byte _gift_flag, byte _purchase, bool _dup = false)
        {
            int ret_id = RetAddItem.ERROR;

            try
            {

                // Block Memória para o UID, para garantir que não vai adicionar itens simuntaneamente
                BlockMemoryManager.blockUID(_uid);

                // Error Grave lança uma excessa
                if (_uid == 0)
                {
                    throw new exception("[ItemManager::addItem][Error] UID invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        8, 0));
                }

                if (_item._typeid == 0)
                {
                    throw new exception("[ItemManager::addItem][Error] item invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        9, 0));
                }

                switch (sIff.Instance.getItemGroupIdentify(_item._typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        {

                            if ( ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] add um character[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                            NormalManagerDB.Instance.add(0,
                                  cmd_ac);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ce = cmd_ac.getInfo();
                            _item.id = (int)ce.id;

                            if (ce.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o character[TYPEID=" + Convert.ToString(ce._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            ret_id = RetAddItem.SUCCESS;//ce.Login;

                            break;
                        }
                    case IFF_GROUP.CADDIE:
                        {

                            if ( ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add um caddie[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possi.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                            NormalManagerDB.Instance.add(2,
                                  cmd_ac);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _item.id = (int)ci.id;

                            if (ci.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o caddie[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;

                            break;
                        }
                    case IFF_GROUP.CAD_ITEM:
                        {
                            uint cad_typeid = (uint)((Convert.ToUInt32(sIff.Instance.CADDIE) << 26) | sIff.Instance.getCaddieIdentify(_item._typeid));

                            var ci =  _ownerCaddieItem(_uid, _item._typeid);

                            if (!(ci.id > 0))
                            {
                                throw new exception("[itme_manager::addItem][Log] Normal[UID=" + Convert.ToString(_uid) + "] tentou comprar um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + "] sem o caddie[TYPEID=" + Convert.ToString(cad_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    14, 0));
                            }

                            long end_date = 0;

                            if (ci.parts_typeid == _item._typeid)
                            { // Já tem o parts caddie, atualiza o tempo

                                // Adiciona o Tempo em Unix Time Stamp
                                end_date = UtilTime.SystemTimeToUnix(ci.end_parts_date) + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME); //(((_item.flag_time == 2) ? _item.STDA_C_ITEM_TIME : _item.STDA_C_ITEM_TIME * 24) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                // update Parts End Date Unix
                                ci.updatePartsEndDate();

                                _item.STDA_C_ITEM_TIME = (short)((_item.flag_time == 2) ? ci.parts_end_date_unix : ci.parts_end_date_unix * 24);

                                _item.id = ci.id;

                                ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;


                            }
                            else
                            {

                                // Não tem o caddie parts ainda, add
                                ci.parts_typeid = _item._typeid;

                                end_date = UtilTime.GetLocalTimeAsUnix() + ((ci.parts_end_date_unix = (short)TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME)) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                _item.id = ci.id;

                                ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;

                            }

                            var str_end_date = UtilTime.FormatDate(ci.end_parts_date);

                            // Atualiza no para os 2 aqui
                            NormalManagerDB.Instance.add(5,
                                  new CmdUpdateCaddieItem(_uid,
                                      str_end_date, ci),
                                  SQLDBResponse,
                                  null);

                            break;
                        }
                    case IFF_GROUP.MASCOT:
                        {
                            var mascot = sIff.Instance.findMascot(_item._typeid);

                            if (mascot == null)
                            {
                                throw new exception("[ItemManager::addItem][Erorr] mascot[TYPEID=" + Convert.ToString(_item._typeid) + "] nao foi encontrado no IFF_STRUCT do server, para o Normal[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            var pMi =  _ownerMascot(_uid, _item._typeid);

                            if ((pMi.id > 0))
                            { // Player já tem o mascot, só add mais tempo à ele

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {

                                    var unix_time = UtilTime.SystemTimeToUnix(pMi.data);

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local time for verify server on local time
                                    pMi.data = (UtilTime.UnixUTCToTzLocalTime(unix_time));

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;

                                    // System Time Struct is Local Time 
                                    _item.date.date.sysDate[0].CreateTime();

                                    _item.date.date.sysDate[1] = pMi.data;

                                    var str_date = UtilTime._formatDate(pMi.data);

                                    // Cmd update time mascot db
                                    NormalManagerDB.Instance.add(6,
                                          new CmdUpdateMascotTime(_uid,
                                              pMi.id, str_date),
                                          SQLDBResponse,
                                          null);
                                }

                                _item.id = pMi.id;

                                ret_id = RetAddItem.SUCCESS;//_item.Login;

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
                                    mi.message = "PangYa JP";
                                }

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {
                                    mi.tipo = 1; // Mascot de Tempo
                                }

                                var cmd_am = new CmdAddMascot(_uid, // Waiter
                                    mi, _item.STDA_C_ITEM_TIME,
                                    _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_am);

                                if (cmd_am.getException().getCodeError() != 0)
                                {
                                    throw cmd_am.getException();
                                }

                                mi = cmd_am.getInfo();
                                _item.id = (int)mi.id;

                                if (mi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Mascot[TYPEID=" + Convert.ToString(mi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                if (mascot.Shop.flag_shop.time_shop.active && _item.STDA_C_ITEM_TIME > 0)
                                {

                                    var unix_time = UtilTime.GetSystemTimeAsUnix();

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local time for verify server on local time
                                    mi.data = UtilTime.UnixUTCToTzLocalTime(unix_time);

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;

                                    // System Time Struct is Local Time
                                    UtilTime.GetLocalTime(out DateTime time);

                                    _item.date.date.sysDate[0].CreateTime(time);

                                    _item.date.date.sysDate[1] = mi.data;
                                }

                                ret_id = RetAddItem.SUCCESS;//mi.Login;

                            }

                            break;
                        }
                    case IFF_GROUP.BALL:
                        {
                            var pWi =  _ownerBall(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            { // já tem atualiza quantidade

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.SUCCESS;//pWi.Login;

                                NormalManagerDB.Instance.add(7,
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

                                NormalManagerDB.Instance.add(0,
                                      cmd_ab);


                                if (cmd_ab.getException().getCodeError() != 0)
                                {
                                    throw cmd_ab.getException();
                                }

                                wi = cmd_ab.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Ball[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = (int)wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.SUCCESS;//wi.Login;
                            }

                        }
                        break;
                    case IFF_GROUP.CLUBSET:
                        {

                            var clubset = sIff.Instance.findClubSet(_item._typeid);

                            if (clubset == null)
                            {
                                throw new exception("[ItemManager::addItem][Error] clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] set nao foi encontrado no IFF_STRUCT do server, para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pWi =  _ownerItem(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            { // Já tem, verifica se é de tempo e atualizar, se não dá error


                                // Add Mais tempo no Club Set
                                if (_item.STDA_C_ITEM_TIME > 0
                                    && ((pWi.flag & 0x20) != 0 || (pWi.flag & 0x40) != 0 || (pWi.flag & 0x60) != 0)
                                    && pWi.end_date_unix_local > 0)
                                {

                                    _item.date.active = 1;

                                    // update ano (Horas) que o item ainda tem
                                    pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                    // Só atualiza o Apply date se não tiver
                                    if (pWi.apply_date_unix_local == 0u)
                                    {

                                        pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                        // Convert to UTC to send client
                                        pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                    }

                                    pWi.end_date_unix_local = (uint)(UtilTime.GetLocalTimeAsUnix() + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));

                                    // Atualiza o tempo do ClubSet do player
                                    NormalManagerDB.Instance.add(20,
                                          new CmdUpdateClubSetTime(_uid, pWi),
                                          SQLDBResponse,
                                          null);

                                    _item.STDA_C_ITEM_QNTD = 1;
                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = _item.qntd;

                                    _item.id = pWi.id;
                                    ret_id = RetAddItem.SUCCESS;//pWi.Login;


                                }
                                else
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                if (_gift_flag == 1 && wi.id > 0)
                                {
                                    CmdGetGiftClubSet cmd_ggcs = new CmdGetGiftClubSet(_uid, // Waiter
                                        wi);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ggcs);

                                    if (cmd_ggcs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ggcs.getException();
                                    }

                                    wi = cmd_ggcs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu pegar o presente de ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + "] para o Normal[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            13, 0));
                                    }

                                }
                                else
                                {
                                    CmdAddClubSet cmd_acs = new CmdAddClubSet(_uid, // Waiter
                                        wi, _purchase, 0);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_acs);

                                    if (cmd_acs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_acs.getException();
                                    }

                                    wi = cmd_acs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                ret_id = RetAddItem.SUCCESS;//wi.Login;

                            }

                        }

                        break;
                    case IFF_GROUP.CARD:
                        {
                            var pCi =  _ownerCard(_uid, _item._typeid);

                            if ((pCi.id > 0))
                            { // Já tem o item atualiza quantidade

                                _item.stat.qntd_ant = pCi.qntd;

                                pCi.qntd += _item.qntd;

                                _item.stat.qntd_dep = pCi.qntd;

                                _item.id = pCi.id; ret_id = RetAddItem.SUCCESS;//pCi.Login;

                                NormalManagerDB.Instance.add(8,
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

                                NormalManagerDB.Instance.add(0,
                                      cmd_ac);

                                if (cmd_ac.getException().getCodeError() != 0)
                                {
                                    throw cmd_ac.getException();
                                }

                                ci = cmd_ac.getInfo();
                                _item.id = ci.id;

                                if (ci.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;

                            }

                            break;
                        }
                    case IFF_GROUP.FURNITURE:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            var furniture = sIff.Instance.findFurniture(_item._typeid);

                            if (furniture == null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que nao existe no IFF_STRUCT do server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            if ( ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja tem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            MyRoomItem mri = new MyRoomItem();

                            mri._typeid = _item._typeid;
                            mri.id = _item.id;

                            mri.location.SetLoc(furniture.location);

                            CmdAddFurniture cmd_af = new CmdAddFurniture(_uid, // Waiter
                                mri);

                            NormalManagerDB.Instance.add(0,
                                  cmd_af);

                            if (cmd_af.getException().getCodeError() != 0)
                            {
                                throw cmd_af.getException();
                            }

                            mri = cmd_af.getInfo();
                            _item.id = mri.id;

                            if (mri.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Furniture[TYPEID=" + Convert.ToString(mri._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.STDA_C_ITEM_QNTD;

                            ret_id = RetAddItem.SUCCESS;

                            break;
                        }
                    case IFF_GROUP.AUX_PART:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            //auto auxPart = sIff::Instance.FindAuxPart(_item._typeid);

                            var pWi =  _ownerAuxPart(_uid, _item._typeid);

                            if ((pWi.id > 0))
                            {

                                if (!sIff.Instance.IsCanOverlapped(pWi._typeid))
                                {
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.SUCCESS;//pWi.Login;

                                NormalManagerDB.Instance.add(9,
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_uid, // Waiter
                                    wi, _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ai);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o AuxPart[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.SUCCESS;//wi.Login;

                            }

                            break;
                        }
                    case IFF_GROUP.ITEM:
                        {
                            // CHECK FOR POUCH [PANG OR EXP]
                            if (_item._typeid == PANG_POUCH_TYPEID)
                            {
                                // Pang Pouch para o player 
                                var _session = GameServer.Instance.FindPlayer(_uid);
                                if (_session != null)
                                {
                                   _session.addPang((uint)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD)); 
                                }
                                else
                                {
                                     PlayerInfo.addPang(_uid,(uint)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                }
                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + Convert.ToString(_uid) + "] Adicionou Pang Pouch. item[TYPEID=" + Convert.ToString(_item._typeid) + "] Qntd[value=" + Convert.ToString((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH; 
                            }
                            else if (_item._typeid == EXP_POUCH_TYPEID)
                            {

                                // Exp Pouch para o player
                                var _session = GameServer.Instance.FindPlayer(_uid);
                                if (_session != null)
                                {
                                    _session.addExp(((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                }
                                else
                                {
                                    Player.addExp(_uid, ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                }

                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + Convert.ToString(_uid) + "] Adicionou Exp Pouch. item[TYPEID=" + Convert.ToString(_item._typeid) + "] Qntd[value=" + Convert.ToString((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH; 
                            }
                            else if (_item._typeid == CP_POUCH_TYPEID)
                            {

                                // Log Ganhos de CP
                                CPLog cp_log = new();

                                cp_log.setType(CPLog.TYPE.CP_POUCH);

                                cp_log.setCookie((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD)); 
                                var _session = GameServer.Instance.FindPlayer(_uid);
                                if (_session != null)
                                {
                                    _session.addCookie((ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                    _session.saveCPLog(cp_log);
                                }
                                else
                                {
                                    // Log de Ganhos de CP
                                    Player.saveCPLog(_uid, cp_log); 
                                    // Cookie Point(CP) Pouch para o player
                                    PlayerInfo.addCookie(_uid, (ulong)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD));
                                } 

                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + Convert.ToString(_uid) + "] Adicionou CP Pouch. item[TYPEID=" + Convert.ToString(_item._typeid) + "] Qntd[value=" + Convert.ToString((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                            }
                            // Fim check pouch

                            var pWi =  _ownerItem(_uid, _item._typeid);

                            // Ticket Report Sempre Add 1 Novo
                            if ((pWi.id > 0) && _item._typeid != TICKET_REPORT_SCROLL_TYPEID)
                            {

                                if (sPremiumSystem.Instance.isPremium(pWi._typeid) && pWi.ano > 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add Item[TYPEID=" + Convert.ToString(_item._typeid) + "] 'Premium Ticket' que ele ja possui, com tempo, tem que esperar acabar o tempo", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        15, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.SUCCESS;//pWi.Login;

                                NormalManagerDB.Instance.add(9,
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_uid, // Waiter
                                    wi, _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ai);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.SUCCESS;//wi.Login;
                            }

                            break;
                        }
                    case IFF_GROUP.SKIN:
                        {

                            if ( ownerItem(_uid, _item._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add Skin[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                            NormalManagerDB.Instance.add(0,
                                  cmd_as);

                            if (cmd_as.getException().getCodeError() != 0)
                            {
                                throw cmd_as.getException();
                            }

                            wi = cmd_as.getInfo();
                            _item.id = wi.id;

                            if (wi.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Skin[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.qntd;

                            ret_id = RetAddItem.SUCCESS;//wi.Login;

                            break;
                        }
                    case IFF_GROUP.PART:
                        {
                            if ( ownerItem(_uid, _item._typeid) && !sIff.Instance.IsCanOverlapped(_item._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] tentou add Part[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                            wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                            if (_gift_flag == 1 && wi.id > 0)
                            {
                                CmdGetGiftPart cmd_ggp = new CmdGetGiftPart(_uid, // Waiter
                                    wi, _item.type_iff);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ggp);

                                if (cmd_ggp.getException().getCodeError() != 0)
                                {
                                    throw cmd_ggp.getException();
                                }

                                wi = cmd_ggp.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu pegar o presente de Part[TYPEID=" + Convert.ToString(_item._typeid) + "] para o Normal[UID=" + Convert.ToString(_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        13, 0));
                                }

                            }
                            else
                            {
                                CmdAddPart cmd_ap = new CmdAddPart(_uid, // Waiter
                                    wi, _purchase, 0,
                                    _item.type_iff);

                                NormalManagerDB.Instance.add(3,
                                      cmd_ap);

                                if (cmd_ap.getException().getCodeError() != 0)
                                {
                                    throw cmd_ap.getException();
                                }

                                wi = cmd_ap.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Parts[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

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

                            ret_id = RetAddItem.SUCCESS;//wi.Login;

                            break;
                        }
                    case IFF_GROUP.HAIR_STYLE:
                        {
                            var hair = sIff.Instance.findHairStyle(_item._typeid);

                            if (hair != null)
                            {
                                var ce =  _ownerHairStyle(_uid, _item._typeid);

                                if ((ce.id > 0))
                                { // Tem o Character

                                    ce.default_hair = hair.Color;

                                    NormalManagerDB.Instance.add(4,
                                          new CmdAddCharacterHairStyle(_uid,
                                              ce, _purchase, 0),
                                          SQLDBResponse,
                                          null);

                                    ret_id = RetAddItem.SUCCESS;//ce.Login;

                                }
                                else
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] nao tem esse character.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        16, 0));
                            }

                            else
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_uid) + "] hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + "] nao tem no IFF_STRUCT.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    17, 0));
                            }

                            break;
                        }
                    case IFF_GROUP.MATCH: // Troféu
                        {
                            var type_trofel = sIff.Instance.getItemSubGroupIdentify24(_item._typeid);

                            // Troféu Espacial
                            if (type_trofel == 1 || type_trofel == 2)
                            {

                                var tsi =  _ownerTrofelEspecial(_uid, _item._typeid);

                                if (tsi.id > 0)
                                { // Já tem item add quantidade do Troféu especial

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    _item.id = tsi.id;
                                    ret_id = RetAddItem.SUCCESS;//tsi.Login;

                                    NormalManagerDB.Instance.add(18,
                                          new CmdUpdateTrofelEspecialQntd(_uid,
                                              tsi.id, tsi.qntd,
                                              CmdUpdateTrofelEspecialQntd.eTYPE.ESPECIAL),
                                          SQLDBResponse,
                                          null);

                                }
                                else
                                {

                                    TrophySpecialInfo ts = new TrophySpecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.ESPECIAL);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ts);


                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Trofel Especial[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    ret_id = RetAddItem.SUCCESS;//ts.Login;
                                }

                            }
                            else if (type_trofel == 3)
                            { // Grand Prix

                                var tsi =  _ownerTrofelEspecial(_uid, _item._typeid);

                                if (tsi.id > 0)
                                { // Já tem item add quantidade do Troféu Grand Prix

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    _item.id = tsi.id;
                                    ret_id = RetAddItem.SUCCESS;//tsi.Login;
                                    NormalManagerDB.Instance.add(18,
                                          new CmdUpdateTrofelEspecialQntd(_uid,
                                              tsi.id, tsi.qntd,
                                              CmdUpdateTrofelEspecialQntd.eTYPE.GRAND_PRIX),
                                          SQLDBResponse,
                                          null);

                                }
                                else
                                {

                                    TrophySpecialInfo ts = new TrophySpecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.GRAND_PRIX
                                        );

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ts);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Trofel Grand Prix[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    ret_id = RetAddItem.SUCCESS;//ts.Login;
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

                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::addItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_uid);

                ret_id = RetAddItem.ERROR;
            }
            return ret_id;
        }

        public static RetAddItem addItem(List<stItem> _v_item, uint _uid,/*era byte*/ byte _gift_flag, byte _purchase, bool _dup = false)
        {

            RetAddItem rai = new RetAddItem();
            int type = RetAddItem.INIT_VALUE;
            var list = _v_item;
            for (int i = 0; i < list.Count; i++)
            {
                var it = _v_item[i];

                if ((type =  addItem(it,

                    _uid, _gift_flag, _purchase,
                    _dup)) <= 0)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::addItem][Log] Normal[UID=" + Convert.ToString(_uid) + "] tentou adicionar o item[TYPEID=" + Convert.ToString(it._typeid) + ", ID=" + Convert.ToString(it.id) + "], " + ((type == RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH) ? "mas era Pang, Experience ou CP pouch" : "mas nao conseguiu.Bug"), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    rai.fails.Add(it);

                    _v_item.Remove(it);

                    if (rai.type == RetAddItem.INIT_VALUE)
                    {
                        rai.type = type;
                    }
                    else if (type == RetAddItem.ERROR)
                    {

                        if (rai.type == RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        }
                        else if (rai.type == RetAddItem.SUCCESS)
                        {
                            rai.type = RetAddItem.SUCCESS_WITH_ERROR;
                        }
                        else if (rai.type != RetAddItem.SUCCESS_WITH_ERROR && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR)
                        {
                            rai.type = type;
                        }

                    }
                    else if (type == RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        if (rai.type == RetAddItem.ERROR || rai.type == RetAddItem.SUCCESS_WITH_ERROR)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        }
                        else if (rai.type == RetAddItem.SUCCESS)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                        }
                    }

                }
                else
                {

                    // Incrementa(????)
                    ++i;

                    if (rai.type == RetAddItem.INIT_VALUE)
                    {
                        rai.type = RetAddItem.SUCCESS;
                    }
                    else if (rai.type == RetAddItem.ERROR)
                    {
                        rai.type = RetAddItem.SUCCESS_WITH_ERROR;
                    }
                }
            }

            return rai;
        }

        public static int addItem(stItem _item, Player _session, /*era byte*/ byte _gift_flag, byte _purchase, bool _dup = false)
        {

            int ret_id = RetAddItem.ERROR;

            try
            {

                // Block Memória para o UID, para garantir que não vai adicionar itens simuntaneamente
                BlockMemoryManager.blockUID(_session.Inventory.uid);
                  
                if (_item._typeid == 0)
                {
                    throw new exception("[ItemManager::addItem][Error] item invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        9, 0));
                }

                switch (sIff.Instance.getItemGroupIdentify(_item._typeid))
                {
                    case IFF_GROUP.CHARACTER:
                        {
                            var pCi = _session.Inventory.FindCharacterByTypeid(_item._typeid);

                            if (pCi != null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] add um character[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            CharacterInfo ce = new CharacterInfo();
                            ce.id = _item.id;
                            ce._typeid = _item._typeid;

                            ce.initComboDef();

                            // Aqui tem que add em uma fila e manda pra query e depois ver se foi concluida, enquanto verifica outras coisas[
                            // E ESSA CLASSE NÃO PODE SER STATIC, POR QUE TEM QUE GUARDA UNS VALORE NECESSÁRIOS

                            // Add no banco de dados
                            var cmd_ac = new CmdAddCharacter(_session.Inventory.uid, // Waitable
                                ce, _purchase, 0);

                            NormalManagerDB.Instance.add(0,
                                  cmd_ac);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ce = cmd_ac.getInfo();
                            _item.id = ce.id;

                            if (ce.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o character[TYPEID=" + Convert.ToString(ce._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            // Add List Item ON Server
                            var it_char = ce;
                            _session.Inventory.Characters.Add(ce.id, ce);

                            // Atualiza character equipado
                            _session.Inventory.UserEquipment.character_id = it_char.id;
                            _session.Inventory.UserEquippedItem.CharacterEquiped = it_char;

                            NormalManagerDB.Instance.add(17,
                                  new CmdUpdateCharacterEquiped(_session.Inventory.uid, _session.Inventory.UserEquipment.character_id),
                                  SQLDBResponse,
                                  null);

                            ret_id = RetAddItem.SUCCESS;//ce.Login;

                            break;
                        }
                    case IFF_GROUP.CADDIE:
                        {
                            var pCi = _session.Inventory.FindCaddieByTypeid(_item._typeid);

                            if (pCi != null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add um caddie[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possi.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                            CmdAddCaddie cmd_ac = new CmdAddCaddie(_session.Inventory.uid, // Waitable
                                ci, _purchase, 0);

                            NormalManagerDB.Instance.add(2,
                                  cmd_ac);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _item.id = ci.id;

                            if (ci.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o caddie[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }


                            _item.STDA_C_ITEM_QNTD = 1;
                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = 1;

                            // Add List Item ON Server
                            _session.Inventory.Caddies.Add(ci.id, ci);

                            ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;

                            break;
                        }
                    case IFF_GROUP.CAD_ITEM:
                        {
                            uint cad_typeid = (uint)((Convert.ToUInt32(sIff.Instance.CADDIE << 26)) | sIff.Instance.getCaddieIdentify(_item._typeid));

                            var ci = _session.Inventory.FindCaddieByTypeid(cad_typeid);

                            if (ci == null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou comprar um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + "] sem o caddie[TYPEID=" + Convert.ToString(cad_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    14, 0));
                            }

                            long end_date = 0;

                            if (ci.parts_typeid == _item._typeid)
                            { // Já tem o parts caddie, atualiza o tempo

                                end_date = UtilTime.SystemTimeToUnix(ci.end_parts_date) + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME); //(((_item.flag_time == 2) ? _item.STDA_C_ITEM_TIME : _item.STDA_C_ITEM_TIME * 24) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                // Update Parts End Date Unix
                                ci.updatePartsEndDate();

                                // Update Time To Send To Client
                                _item.STDA_C_ITEM_TIME = (short)((_item.flag_time == 2) ? ci.parts_end_date_unix : ci.parts_end_date_unix * 24);

                                _item.id = ci.id;

                                ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;

                            }
                            else
                            {

                                // Não tem o caddie parts ainda, add
                                ci.parts_typeid = _item._typeid;

                                end_date = UtilTime.GetLocalTimeAsUnix() + ((ci.parts_end_date_unix = (short)(TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME))) * 60 * 60);

                                // Converte para System Time novamente
                                ci.end_parts_date = (UtilTime.UnixToSystemTime(end_date));

                                _item.id = ci.id;

                                ret_id = RetAddItem.SUCCESS;//CharacterInfo.Login;
                            }

                            // Verifica se o Caddie já tem um item update do parts do caddie, por que se tiver, 
                            // ele vai desequipar esse parts novo por que o player não relogou quando acabou o tempo do caddie parts
                            var v_it = _session.Inventory.FindUpdateItemById(ci.id);

                            if (!v_it.Any() == false)
                            {

                                foreach (var el in v_it)
                                {
                                    if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                    {
                                        // Tira esse Update Item do map
                                        _session.Inventory.UpdateItems.Remove(el.Key);
                                    }
                                }

                            }
                            // ---- fim do verifica se o caddie tem parts no update item ----

                            var str_end_date = UtilTime.formatDateLocal(end_date);

                            // Atualiza no para os 2 aqui
                            NormalManagerDB.Instance.add(5,
                                  new CmdUpdateCaddieItem(_session.Inventory.uid,
                                      str_end_date, ci),
                                  SQLDBResponse,
                                  null);

                            break;
                        }
                    case IFF_GROUP.MASCOT:
                        {
                            var mascot = sIff.Instance.findMascot(_item._typeid);

                            if (mascot == null)
                            {
                                throw new exception("[ItemManager::addItem][Erorr] mascot[TYPEID=" + Convert.ToString(_item._typeid) + "] nao foi encontrado no IFF_STRUCT do server, para o Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            var pMi = _session.Inventory.FindMascotByTypeid(_item._typeid);

                            if (pMi != null)
                            { // Player já tem o mascot, só add mais tempo à ele

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {

                                    //long unix_time = UtilTime.SystemTimeToUnix(pMi->data);
                                    long unix_time = UtilTime.TzLocalTimeToUnixUTC(pMi.data);

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME);

                                    // Local Time for verify on server on local time
                                    pMi.data = (UtilTime.UnixUTCToTzLocalTime(unix_time));

                                    _item.stat.qntd_dep = (int)unix_time;

                                    _item.date.active = 1;
                                    // System Time Struct is Local Time 
                                    _item.date.date.sysDate[0].CreateTime();
                                    _item.date.date.sysDate[1] = pMi.data;

                                    var str_date = UtilTime._formatDate(pMi.data);

                                    // Cmd update time mascot db
                                    NormalManagerDB.Instance.add(6,
                                          new CmdUpdateMascotTime(_session.Inventory.uid,
                                              pMi.id, str_date),
                                          SQLDBResponse,
                                          null);
                                }

                                _item.id = pMi.id;

                                ret_id = RetAddItem.SUCCESS;//_item.Login;

                                // Verifica se o Mascot está no item update, por que se tiver, 
                                // ele vai desequipar esse mascot por que o player não relogou quando acabou o tempo do Mascot
                                var v_it = _session.Inventory.FindUpdateItemById(pMi.id);

                                if (!v_it.Any() == false)
                                {

                                    foreach (var el in v_it)
                                    {
                                        if (el.Value.type == UpdateItem.UI_TYPE.MASCOT)
                                        {
                                            // Tira esse Update Item do map
                                            _session.Inventory.UpdateItems.Remove(el.Key);
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
                                    mi.message = "PangYa JP!";
                                }

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {
                                    mi.tipo = 1; // Mascot de Tempo
                                }

                                CmdAddMascot cmd_am = new CmdAddMascot(_session.Inventory.uid, // Waiter
                                    mi, _item.STDA_C_ITEM_TIME,
                                    _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_am);

                                if (cmd_am.getException().getCodeError() != 0)
                                {
                                    throw cmd_am.getException();
                                }

                                mi = cmd_am.getInfo();
                                _item.id = mi.id;

                                if (mi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Mascot[TYPEID=" + Convert.ToString(mi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.STDA_C_ITEM_QNTD = 1;
                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                if (mascot.Shop.flag_shop.time_shop.active & _item.STDA_C_ITEM_TIME > 0)
                                {

                                    long unix_time = UtilTime.GetSystemTimeAsUnix();

                                    _item.stat.qntd_ant = (int)unix_time;

                                    unix_time += TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME);

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
                                _session.Inventory.Mascots.Add(mi.id, mi);

                                ret_id = RetAddItem.SUCCESS;//mi.Login;
                            }

                            break;
                        }
                    case IFF_GROUP.BALL:
                        {
                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            { // já tem atualiza quantidade

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                ret_id = RetAddItem.SUCCESS;//pWi.Login;
                                _item.id = pWi.id;
                                NormalManagerDB.Instance.add(7,
                                      new CmdUpdateBallQntd(_session.Inventory.uid,
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

                                CmdAddBall cmd_ab = new CmdAddBall(_session.Inventory.uid, // Waiter
                                    wi, _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ab);

                                if (cmd_ab.getException().getCodeError() != 0)
                                {
                                    throw cmd_ab.getException();
                                }

                                wi = cmd_ab.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Ball[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                // Add List Item ON Server
                                _session.Inventory.WarehouseItems.Add(wi.id, wi);

                                ret_id = RetAddItem.SUCCESS;//wi.Login;
                                                              //  ITEM_MANAGER_LOG("Adicionou Ball", _session, _item);

                            }

                            break;
                        }
                    case IFF_GROUP.CLUBSET:
                        {

                            var clubset = sIff.Instance.findClubSet(_item._typeid);

                            if (clubset == null)
                            {
                                throw new exception("[ItemManager::addItem][Error] clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] set nao foi encontrado no IFF_STRUCT do server, para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            {

                                // Add Mais tempo no Club Set
                                if (_item.STDA_C_ITEM_TIME > 0
                                    && ((pWi.flag & 0x20) != 0 || (pWi.flag & 0x40) != 0 || (pWi.flag & 0x60) != 0)
                                    && pWi.end_date_unix_local > 0)
                                {

                                    _item.date.active = 1;

                                    // update ano (Horas) que o item ainda tem
                                    pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                    // Só atualiza o Apply date se não tiver
                                    if (pWi.apply_date_unix_local == 0u)
                                    {

                                        pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                        // Convert to UTC to send client
                                        pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                    }

                                    pWi.end_date_unix_local = (uint)(UtilTime.GetLocalTimeAsUnix() + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));
                                    // Atualiza o tempo do ClubSet do player
                                    NormalManagerDB.Instance.add(20,
                                          new CmdUpdateClubSetTime(_session.Inventory.uid, pWi),
                                          SQLDBResponse,
                                          null);

                                    _item.STDA_C_ITEM_QNTD = 1;
                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = _item.qntd;
                                    ret_id = RetAddItem.SUCCESS;//pWi.Login;
                                    _item.id = pWi.id;
                                    // Verifica se o ClubSet está no item update, por que se tiver, 
                                    // ele vai desequipar esse clubset por que o player não relogou quando acabou o tempo do clubset
                                    var v_it = _session.Inventory.FindUpdateItemById(pWi.id);

                                    if (!v_it.Any() == false)
                                    {

                                        foreach (var el in v_it)
                                        {
                                            if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                                            {
                                                // Tira esse Update Item do map
                                                _session.Inventory.UpdateItems.Remove(el.Key);
                                            }
                                        }

                                    }
                                    // ---- fim do verifica se o ClubSet está no update item ----
                                    //  ITEM_MANAGER_LOG("Atualizou Tempo do CluSet", _session, _item);

                                }
                                else
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add clubset[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para Horas, quanto tempo falta em horas

                                if (_gift_flag == 1 && wi.id > 0)
                                {
                                    CmdGetGiftClubSet cmd_ggcs = new CmdGetGiftClubSet(_session.Inventory.uid, // Waiter
                                        wi);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ggcs);

                                    if (cmd_ggcs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ggcs.getException();
                                    }

                                    wi = cmd_ggcs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu pegar o presente de ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + "] para o Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            13, 0));
                                    }

                                }
                                else
                                {
                                    CmdAddClubSet cmd_acs = new CmdAddClubSet(_session.Inventory.uid, // Waiter
                                        wi, _purchase, 0);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_acs);

                                    if (cmd_acs.getException().getCodeError() != 0)
                                    {
                                        throw cmd_acs.getException();
                                    }

                                    wi = cmd_acs.getInfo();
                                    _item.id = wi.id;

                                    if (wi.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {

                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

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
                                _session.Inventory.WarehouseItems.Add(wi.id, wi);

                                ret_id = RetAddItem.SUCCESS;//wi.Login;

                            }

                            break;
                        }
                    case IFF_GROUP.CARD:
                        {
                            var pCi = _session.Inventory.FindCardByTypeid(_item._typeid);

                            if (pCi != null)
                            { // Já tem item add quantidade ao card

                                _item.stat.qntd_ant = pCi.qntd;

                                pCi.qntd += _item.qntd;

                                _item.stat.qntd_dep = pCi.qntd;

                                _item.id = pCi.id;

                                ret_id = RetAddItem.SUCCESS;

                                NormalManagerDB.Instance.add(8,
                                      new CmdUpdateCardQntd(_session.Inventory.uid,
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

                                CmdAddCard cmd_ac = new CmdAddCard(_session.Inventory.uid, ci, _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ac);

                                if (cmd_ac.getException().getCodeError() != 0)
                                    throw cmd_ac.getException();

                                ci = cmd_ac.getInfo();
                                _item.id = ci.id;

                                if (ci.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = _item.qntd;

                                // Add List Item ON Server
                                _session.Inventory.Cards.Add(ci.id, ci);

                                ret_id = RetAddItem.SUCCESS;
                                //  ITEM_MANAGER_LOG("Adicionou Card", _session, _item);
                            }

                            break;
                        }
                    case IFF_GROUP.FURNITURE:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            var furniture = sIff.Instance.findFurniture(_item._typeid);

                            if (furniture == null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que nao existe no IFF_STRUCT do server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    12, 0));
                            }

                            var pFi = _session.Inventory.FindMyRoomItemByTypeid(_item._typeid);

                            if (pFi != null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja tem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    10, 0));
                            }

                            MyRoomItem mri = new MyRoomItem();

                            mri._typeid = _item._typeid;
                            mri.id = _item.id;

                            mri.location.SetLoc(furniture.location);

                            CmdAddFurniture cmd_af = new(_session.Inventory.uid, // Waiter
                                mri);

                            NormalManagerDB.Instance.add(0,
                                  cmd_af);

                            if (cmd_af.getException().getCodeError() != 0)
                            {
                                throw cmd_af.getException();
                            }

                            mri = cmd_af.getInfo();
                            _item.id = mri.id;

                            if (mri.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Furniture[TYPEID=" + Convert.ToString(mri._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.STDA_C_ITEM_QNTD;

                            // Add List Item ON Server
                            _session.Inventory.MyRoomItems.Add(mri);

                            ret_id = RetAddItem.SUCCESS;

                            //  ITEM_MANAGER_LOG("Adicionou Furniture", _session, _item);

                            break;
                        }
                    case IFF_GROUP.AUX_PART:
                        {
                            // Tem que fazer esse aqui, por que pode vim por Set Item ou MailBox
                            //auto auxPart = sIff::Instance.FindAuxPart(_item._typeid);

                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            { // Já tem item add quantidade do AuxPart

                                if (!sIff.Instance.IsCanOverlapped(pWi._typeid))
                                {
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        10, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.SUCCESS;//pWi.Login;

                                NormalManagerDB.Instance.add(9,
                                      new CmdUpdateItemQntd(_session.Inventory.uid,
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                CmdAddItem cmd_ai = new CmdAddItem(_session.Inventory.uid, // Waiter
                                    wi, _purchase, 0);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ai);

                                if (cmd_ai.getException().getCodeError() != 0)
                                {
                                    throw cmd_ai.getException();
                                }

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o AuxPart[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        11, 0));
                                }

                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time                     
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                // Add List Item ON Server
                                _session.Inventory.WarehouseItems.Add(wi.id, wi);

                                ret_id = RetAddItem.SUCCESS;//wi.Login;

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

                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + (_session.Inventory.uid) + "] Adicionou Pang Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.Inventory.uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

                            }
                            else if (_item._typeid == EXP_POUCH_TYPEID/*Exp Pouch*/)
                            {

                                // Exp Pouch para o player
                                _session.addExp((int)((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD), true/*Send packet for update Level and Experience in game*/);

                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + (_session.Inventory.uid) + "] Adicionou Exp Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.Inventory.uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;

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

                                _smp.LogManager.Instance.push(new AppMessage("[Pangya Shop][Log] Normal[UID=" + (_session.Inventory.uid) + "] Adicionou CP Pouch. item[TYPEID="
                                        + (_item._typeid) + "] Qntd[value=" + ((_item.qntd > 0xFFu) ? _item.qntd : _item.STDA_C_ITEM_QNTD) + "]", type_msg.CL_ONLY_FILE_LOG));

                                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                                BlockMemoryManager.unblockUID(_session.Inventory.uid);

                                return RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                            }
                            // Fim check pouch

                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            // Ticket Report Sempre Add 1 Novo
                            if (pWi != null && _item._typeid != TICKET_REPORT_SCROLL_TYPEID)
                            {   // Já tem item add quantidade do Item

                                if (sPremiumSystem.Instance.isPremium(pWi._typeid))
                                {

                                    var st = UtilTime.UnixToSystemTime(pWi.end_date_unix_local);

                                    if (UtilTime.GetLocalTimeDiffDESC(st) > 0 || _session.UserInfo.UserCapabilities.UserPremium)
                                        throw new exception("[ItemManager::addItem][Error] Normal[UID=" + (_session.Inventory.uid) + "] tentou add Item[TYPEID="
                                                + (_item._typeid) + "] 'Premium Ticket' que ele ja possui com tempo, tem que esperar acabar o tempo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 15, 0));
                                }

                                _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                                pWi.STDA_C_ITEM_QNTD += _item.STDA_C_ITEM_QNTD;

                                _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                                _item.id = pWi.id;
                                ret_id = RetAddItem.SUCCESS;//pWi.Login;

                                // Verifica se é Premium Ticket
                                if (sPremiumSystem.Instance.isPremium(pWi._typeid))
                                { // Renova o Premium Ticket por mais 30(tempo, coloquei mais opções) dias

                                    if (_item.STDA_C_ITEM_TIME > 0)
                                    {
                                        _item.date.active = 1;

                                        // Conver
                                        pWi.STDA_C_ITEM_TIME = (short)(_item.STDA_C_ITEM_TIME / 24); // converte de novo para Dias para salvar no banco de dados

                                        // Atualiza o tempo do Premium Ticket do player
                                        NormalManagerDB.Instance.add(19, new CmdUpdatePremiumTicketTime(_session.Inventory.uid, pWi), SQLDBResponse, null);

                                        // update ano (Horas) que o item ainda tem
                                        pWi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                                        // Só atualiza o Apply date se não tiver
                                        if (pWi.apply_date_unix_local == 0u)
                                        {

                                            pWi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                            // Convert to UTC to send client
                                            pWi.apply_date = UtilTime.TzLocalUnixToUnixUTC(pWi.apply_date_unix_local);
                                        }

                                        pWi.end_date_unix_local = (uint)((uint)UtilTime.GetLocalTimeAsUnix() + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                        // Convert to UTC to send client
                                        pWi.end_date = UtilTime.TzLocalUnixToUnixUTC(pWi.end_date_unix_local);

                                        // System Time Struct is Local Time 
                                        _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(pWi.apply_date_unix_local));
                                        _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(pWi.end_date_unix_local));

                                    }

                                    if (sPremiumSystem.Instance.isPremium(_item._typeid))
                                    {

                                        _smp.LogManager.Instance.push(new AppMessage("[ItemManager::addItem][Log] Normal[UID=" + (_session.Inventory.uid) + "] renovou premium ticket[TYPEID="
                                                + (pWi._typeid) + "] por " + (_item.STDA_C_ITEM_TIME / 24u) + " Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        sPremiumSystem.Instance.addPremiumUser(_session, pWi, (uint)(_item.STDA_C_ITEM_TIME / 24)/*Dias*/);
                                    }

                                    // Verifica se o Premium Ticket está no item update, por que se tiver
                                    var v_it = _session.Inventory.FindUpdateItemById(pWi.id);

                                    if (!v_it.Any() == false)
                                    {
                                        foreach (var el in v_it)
                                        {
                                            if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                                            {
                                                // Tira esse Update Item do map
                                                _session.Inventory.UpdateItems.Remove(el.Key);
                                            }
                                        }
                                    }
                                    // ---- fim do verifica se o Premium Ticket está no update item ----

                                }
                                else // Atualiza a quantidade do item Normal
                                    NormalManagerDB.Instance.add(9, new CmdUpdateItemQntd(_session.Inventory.uid, pWi.id, pWi.STDA_C_ITEM_QNTD), SQLDBResponse, null);

                                // Atualiza Gacha Coupon
                                if (_item._typeid == 0x1A000080/*Coupon Gacha*/)
                                {
                                    _session.Inventory.CouponGacha.normal_ticket += _item.STDA_C_ITEM_QNTD;

                                    _session.Send(Handle_PACKET_RESPONSE.pacote102(_session.UserInfo, _session.Inventory.CouponGacha));
                                }

                            }
                            else
                            {

                                if (sPremiumSystem.Instance.isPremium(_item._typeid))
                                {

                                    pWi = (_session.Inventory.PremiumTicket._typeid == 0) ? null : _session.Inventory.FindWarehouseItemByTypeid(_session.Inventory.PremiumTicket._typeid);

                                    if (pWi != null)
                                    {

                                        var st = UtilTime.UnixToSystemTime(pWi.end_date_unix_local);

                                        if (UtilTime.GetLocalTimeDiffDESC(st) > 0 || _session.UserInfo.UserCapabilities.UserPremium)
                                            throw new exception("[ItemManager::addItem][Error] Normal[UID=" + (_session.Inventory.uid) + "] tentou add Item[TYPEID="
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

                                wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1; // Aqui tem que colocar para minutos ou segundos(acho)

                                var cmd_ai = new CmdAddItem(_session.Inventory.uid, wi, _purchase, 0/*_gift_flag*/);   // Waiter

                                NormalManagerDB.Instance.add(0, cmd_ai);

                                if (cmd_ai.getException().getCodeError() != 0)
                                    throw cmd_ai.getException();

                                wi = cmd_ai.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Item[TYPEID=" + (wi._typeid) + "] para o player: "
                                            + (_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 11, 0));


                                if (_item.STDA_C_ITEM_TIME > 0)
                                {
                                    _item.date.active = 1;

                                    wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                    // Convert to UTC to send client
                                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                    wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                    // Convert to UTC to send client
                                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                    // System Time Struct is Local Time
                                    _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                    _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                                }

                                if (_item._typeid == 0x1A000080/*Coupon Gacha*/)
                                {
                                    _session.Inventory.CouponGacha.normal_ticket += _item.STDA_C_ITEM_QNTD;

                                    _session.Send(Handle_PACKET_RESPONSE.pacote102(_session.UserInfo, _session.Inventory.CouponGacha));
                                }

                                _item.stat.qntd_ant = 0;
                                _item.stat.qntd_dep = wi.STDA_C_ITEM_QNTD;

                                _session.Inventory.WarehouseItems.Add(wi.id, wi);

                                if (sPremiumSystem.Instance.isPremium(_item._typeid))
                                {

                                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::addItem][Log] Normal[UID=" + (_session.Inventory.uid) + "] comprou premium ticket[TYPEID="
                                            + (wi._typeid) + "] por " + (_item.STDA_C_ITEM_TIME / 24) + " Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    sPremiumSystem.Instance.addPremiumUser(_session, wi, (uint)(_item.STDA_C_ITEM_TIME / 24));
                                }

                                ret_id = RetAddItem.SUCCESS;//wi.Login;

                            }

                            break;
                        }
                    case IFF_GROUP.SKIN:
                        {
                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null)
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add Skin[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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
                            CmdAddSkin cmd_as = new CmdAddSkin(_session.Inventory.uid, // Waiter
                                wi, _purchase, 0);

                            NormalManagerDB.Instance.add(0,
                                  cmd_as);

                            if (cmd_as.getException().getCodeError() != 0)
                            {
                                throw cmd_as.getException();
                            }

                            wi = cmd_as.getInfo();
                            _item.id = wi.id;

                            if (wi.id <= 0)
                            {
                                throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Skin[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    11, 0));
                            }

                            if (_item.STDA_C_ITEM_TIME > 0)
                            {
                                _item.date.active = 1;

                                wi.apply_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix();

                                // Convert to UTC to send client
                                wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

                                // Convert to UTC to send client
                                wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);

                                // System Time Struct is Local Time
                                _item.date.date.sysDate[0] = (UtilTime.UnixToSystemTime(wi.apply_date_unix_local));
                                _item.date.date.sysDate[1] = (UtilTime.UnixToSystemTime(wi.end_date_unix_local));
                            }

                            _item.stat.qntd_ant = 0;
                            _item.stat.qntd_dep = _item.qntd;

                            // Add List Item ON Server
                            _session.Inventory.WarehouseItems.Add(wi.id, wi);

                            ret_id = RetAddItem.SUCCESS;//wi.Login;
                            break;
                        }
                    case IFF_GROUP.PART:
                        {
                            var pWi = _session.Inventory.FindWarehouseItemByTypeid(_item._typeid);

                            if (pWi != null && !sIff.Instance.IsCanOverlapped(pWi._typeid))
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou add Part[TYPEID=" + Convert.ToString(_item._typeid) + "] que ele ja possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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
                            wi.ano = (_item.STDA_C_ITEM_TIME > 0) ? TranslateFlagTimeToHour(_item.flag_time, _item.STDA_C_ITEM_TIME) : -1;

                            if (_gift_flag == 1 && wi.id > 0)
                            {
                                CmdGetGiftPart cmd_ggp = new CmdGetGiftPart(_session.Inventory.uid, // Waiter
                                    wi, _item.type_iff);

                                NormalManagerDB.Instance.add(0,
                                      cmd_ggp);
                                if (cmd_ggp.getException().getCodeError() != 0)
                                {
                                    throw cmd_ggp.getException();
                                }

                                wi = cmd_ggp.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu pegar o presente de Part[TYPEID=" + Convert.ToString(_item._typeid) + "] para o Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        13, 0));
                                }

                            }
                            else
                            {
                                CmdAddPart cmd_ap = new CmdAddPart(_session.Inventory.uid, // Waiter
                                    wi, _purchase, 0,
                                    _item.type_iff);

                                NormalManagerDB.Instance.add(3,
                                      cmd_ap);

                                if (cmd_ap.getException().getCodeError() != 0)
                                {
                                    throw cmd_ap.getException();
                                }

                                wi = cmd_ap.getInfo();
                                _item.id = wi.id;

                                if (wi.id <= 0)
                                {
                                    throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Parts[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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

                                wi.end_date_unix_local = (uint)(wi.apply_date_unix_local + TranslateFlagTimeToSeconds(_item.flag_time, _item.STDA_C_ITEM_TIME));

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
                            _session.Inventory.WarehouseItems.Add(wi.id, wi);

                            ret_id = RetAddItem.SUCCESS;//wi.Login;
                            break;
                        }
                    case IFF_GROUP.HAIR_STYLE:
                        {
                            var hair = sIff.Instance.findHairStyle(_item._typeid);

                            if (hair != null)
                            {
                                uint char_typeid = (uint)((sIff.Instance.CHARACTER << 26) | hair.Character);

                                CharacterInfo ce = _session.Inventory.FindCharacterByTypeid(char_typeid);

                                if (ce != null)
                                {

                                    ce.default_hair = hair.Color;

                                    NormalManagerDB.Instance.add(4,
                                          new CmdAddCharacterHairStyle(_session.Inventory.uid,
                                              ce, _purchase, 0),
                                          SQLDBResponse,
                                          _session);

                                    ret_id = RetAddItem.SUCCESS;//ce.Login;


                                }
                                else
                                    throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem esse character[TYPEID=" + Convert.ToString(char_typeid) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                        16, 0));
                            }

                            else
                            {
                                throw new exception("[ItemManager::addItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + "] nao tem no IFF_STRUCT.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                    17, 0));
                            }

                            break;
                        }
                    case IFF_GROUP.MATCH: // Troféu
                        {
                            var type_trofel = sIff.Instance.getItemSubGroupIdentify24(_item._typeid);

                            // Troféu Espacial
                            if (type_trofel == 1 || type_trofel == 2)
                            {

                                var tsi = _session.Inventory.FindTrofelEspecialByTypeid(_item._typeid);

                                if (tsi != null)
                                { // Já tem item add quantidade do Troféu Especial

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;
                                    ret_id = RetAddItem.SUCCESS;//tsi.Login;
                                    _item.id = tsi.id;

                                    NormalManagerDB.Instance.add(18,
                                          new CmdUpdateTrofelEspecialQntd(_session.Inventory.uid,
                                              tsi.id, tsi.qntd,
                                              CmdUpdateTrofelEspecialQntd.eTYPE.ESPECIAL),
                                          SQLDBResponse,
                                          null);

                                }
                                else
                                {

                                    TrophySpecialInfo ts = new TrophySpecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_session.Inventory.uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.ESPECIAL);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ts);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Trofel Especial[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    // Add List Item ON Server
                                    _session.Inventory.CurrentSpecialTrophies.Add(ts);

                                    ret_id = RetAddItem.SUCCESS;//ts.Login;
                                }

                            }
                            else if (type_trofel == 3)
                            { // Grand Prix

                                var tsi = _session.Inventory.FindTrofelGrandPrixByTypeid(_item._typeid);

                                if (tsi != null)
                                { // Já tem item add quantidade do Troféu Grand Prix

                                    _item.stat.qntd_ant = tsi.qntd;

                                    tsi.qntd += _item.STDA_C_ITEM_QNTD;

                                    _item.stat.qntd_dep = tsi.qntd;

                                    ret_id = RetAddItem.SUCCESS;//tsi.Login;
                                    _item.id = tsi.id;
                                    NormalManagerDB.Instance.add(18,
                                          new CmdUpdateTrofelEspecialQntd(_session.Inventory.uid,
                                              tsi.id, tsi.qntd,
                                              CmdUpdateTrofelEspecialQntd.eTYPE.GRAND_PRIX),
                                          SQLDBResponse,
                                          null);

                                }
                                else
                                {

                                    TrophySpecialInfo ts = new TrophySpecialInfo();
                                    ts.id = _item.id;
                                    ts._typeid = _item._typeid;
                                    ts.qntd = _item.STDA_C_ITEM_QNTD;

                                    CmdAddTrofelEspecial cmd_ts = new CmdAddTrofelEspecial(_session.Inventory.uid, // Waiter
                                        ts,
                                        CmdAddTrofelEspecial.eTYPE.GRAND_PRIX);

                                    NormalManagerDB.Instance.add(0,
                                          cmd_ts);

                                    if (cmd_ts.getException().getCodeError() != 0)
                                    {
                                        throw cmd_ts.getException();
                                    }

                                    ts = cmd_ts.getInfo();
                                    _item.id = ts.id;

                                    if (ts.id <= 0)
                                    {
                                        throw new exception("[ItemManager::addItem][Error] nao conseguiu adicionar o Trofel Grand Prix[TYPEID=" + Convert.ToString(ts._typeid) + "] para o player: " + Convert.ToString(_session.Inventory.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                            11, 0));
                                    }

                                    _item.stat.qntd_ant = 0;
                                    _item.stat.qntd_dep = ts.qntd;

                                    // Add List Item ON Server
                                    _session.Inventory.CurrentGrandPrixTrophies.Add(ts);

                                    ret_id = RetAddItem.SUCCESS;//ts.Login;
                                }
                            }

                            break;
                        } // End iff::MATCH
                } // End Switch

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_session.Inventory.uid);
            }

            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::addItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Block memória para o UID, previne de add mais de um item simuntaneamente, para não gerar valores errados
                BlockMemoryManager.unblockUID(_session.Inventory.uid);

                ret_id = RetAddItem.ERROR;
            }

            return ret_id;
        }

        public static RetAddItem addItem(List<stItem> _v_item, Player _session, byte _gift_flag, byte _purchase, bool _dup = false)
        {

            RetAddItem rai = new();
            int type = RetAddItem.INIT_VALUE;

            var list = _v_item;
            for (int i = 0; i < list.Count; /*incrementa só no else*/)
            {
                var it = _v_item[i];

                if ((type =  addItem(it, _session, _gift_flag, _purchase, _dup)) <= 0)
                { 
                    rai.fails.Add(it);

                    _v_item.Remove(it);

                    if (rai.type == RetAddItem.INIT_VALUE)
                    {
                        rai.type = type;
                    }
                    else if (type == RetAddItem.ERROR)
                    {

                        if (rai.type == RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        }
                        else if (rai.type == RetAddItem.SUCCESS)
                        {
                            rai.type = RetAddItem.SUCCESS_WITH_ERROR;
                        }
                        else if (rai.type != RetAddItem.SUCCESS_WITH_ERROR && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR)
                        {
                            rai.type = type;
                        }

                    }
                    else if (type == RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        if (rai.type == RetAddItem.ERROR || rai.type == RetAddItem.SUCCESS_WITH_ERROR)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR;
                        }
                        else if (rai.type == RetAddItem.SUCCESS)
                        {
                            rai.type = RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH;
                        }
                    }

                }
                else
                {
                    ++i;

                    if (rai.type == RetAddItem.INIT_VALUE)
                    {
                        rai.type = RetAddItem.SUCCESS;
                    }
                    else if (rai.type == RetAddItem.ERROR)
                    {
                        rai.type = RetAddItem.SUCCESS_WITH_ERROR;
                    }
                }
            }

            return rai;
        }

        public static int giveItem(stItem _item, Player _session,  /*era byte*/ byte _gift_flag)
        { 

            int ret_id = -1;

            switch (sIff.Instance.getItemGroupIdentify(_item._typeid))
            {
                case IFF_GROUP.CHARACTER:
                    // Não pode deletar Character
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um Character[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CADDIE:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um Caddie[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CAD_ITEM:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um CaddieItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.Inventory.FindCardById(_item.id);

                        if (pCi == null)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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
                            NormalManagerDB.Instance.add(12, // Delete Card
                                  new CmdDeleteCard(_session.Inventory.uid, pCi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_ci, second.Login, == , pCi->Login);
                            var it = _session.Inventory.FindCaddieById(pCi.id);

                            if (it != null)
                            {
                                _session.Inventory.Caddies.Remove(it.id);
                            }
                        }
                        else
                        {
                            NormalManagerDB.Instance.add(8, // Update
                                  new CmdUpdateCardQntd(_session.Inventory.uid,
                                      pCi.id, pCi.qntd),
                                  SQLDBResponse,
                                  null);
                        }

                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        _item.stat.qntd_ant = 1;
                        _item.stat.qntd_dep = 0;
                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        NormalManagerDB.Instance.add(13, // Gift ClubSet
                              new CmdGiftClubSet(_session.Inventory.uid, pWi.id),
                              SQLDBResponse,
                              null);

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                        if (it != null)
                        {
                            _session.Inventory.WarehouseItems.Remove(it.id);
                        }

                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        if (_item.STDA_C_ITEM_QNTD > pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem quantidade[value=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", req=" + Convert.ToString(_item.STDA_C_ITEM_QNTD) + "] suficiente para o Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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
                            NormalManagerDB.Instance.add(11, // Delete Ball
                                   (Pangya_DB)new CmdDeleteBall(_session.Inventory.uid, pWi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                            var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                            if (it != null)
                            {
                                _session.Inventory.WarehouseItems.Remove(it.id);
                            }
                        }
                        else
                        {
                            NormalManagerDB.Instance.add(7, // Update
                                  new CmdUpdateBallQntd(_session.Inventory.uid,
                                      pWi.id, pWi.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);
                        }

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.HAIR_STYLE:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um HairStyle[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.ITEM:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        if (_item.STDA_C_ITEM_QNTD > pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem quantidade[value=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", req=" + Convert.ToString(_item.STDA_C_ITEM_QNTD) + "] suficiente para o item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
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
                            NormalManagerDB.Instance.add(10, // Delete Item
                                  new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                            var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                            if (it != null)
                            {
                                _session.Inventory.WarehouseItems.Remove(it.id);
                            }
                        }
                        else
                        {
                            NormalManagerDB.Instance.add(9, // Update
                                  new CmdUpdateItemQntd(_session.Inventory.uid,
                                      pWi.id, pWi.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);
                        }

                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um SetItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.SKIN:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um Skin[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.MASCOT:
                    throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode presentear um Mascot[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] ja comprado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                        20, 0));
                case IFF_GROUP.PART:
                    {

                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            throw new exception("[ItemManager::giveItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] para ser presenteado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER,
                                21, 0));
                        }

                        _item.stat.qntd_ant = 1;
                        _item.stat.qntd_dep = 0;
                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        NormalManagerDB.Instance.add(14, // Gift Part
                              new CmdGiftPart(_session.Inventory.uid, pWi.id),
                              SQLDBResponse,
                              null);

                        // Return value, avoid memory leaks
                        ret_id = pWi.id;

                        var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                        if (it != null)
                        {
                            _session.Inventory.WarehouseItems.Remove(it.id);
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
                if ( giveItem(list[i],
                    _session, _gift_flag) <= 0)
                {
                    _v_item.Remove(list[i]);
                }
            }

            return (int)i;
        }

        public static int removeItem(stItem _item, Player _session)
        { 
            int ret_id = -1;

            switch (sIff.Instance.getItemGroupIdentify(_item._typeid))
            {
                case IFF_GROUP.AUX_PART: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= (short)_item.qntd)
                        { // Exclui o Item[AxuPart]

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            _item.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * ushort.MaxValue);

                            pWi.STDA_C_ITEM_QNTD = 0;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            NormalManagerDB.Instance.add(0,
                                  new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                            var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                            if (it != null)
                            {
                                _session.Inventory.WarehouseItems.Remove(it.id);
                            }

                            ret_id = _item.id;

                            // Se deletou a AuxPart que estava equipada 
                            // Desequipa o AuxPart
                            var v_ci = _session.isAuxPartEquiped(_item._typeid);

                            if (!v_ci.Any() == false)
                            {

                                foreach (var el in v_ci)
                                {

                                    if (el != null)
                                    {

                                        // Desequipa o AuxPart
                                        el.unequipAuxPart(_item._typeid);

                                        // Update ON DB
                                        NormalManagerDB.Instance.add(0,
                                              new CmdUpdateCharacterAllPartEquiped(_session.Inventory.uid, el),
                                              SQLDBResponse,
                                              null);

#if DEBUG
                                        _smp.message_pool.getInstance.push(new message("[ItemManager::removeItem][Log] PLAYER[UID=" + Convert.ToString(_session.Inventory.uid) + "] desequipou o AuxPart[TYPEID=" + Convert.ToString(_item._typeid) + "] do Character[TYPEID=" + Convert.ToString(el._typeid) + ", ID=" + Convert.ToString(el.id) + "] por que ele foi deletado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                                        // Update ON GAME
                                        var p = new Packet(0x6B);

                                        p.WriteByte(4); // 4 Sucesso
                                        p.WriteByte(0); // Character All Parts

                                        p.WriteBytes(el.ToArray());

                                        _session.Send(p);
                                    }
                                }
                            }

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)_item.qntd;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateItemQntd(_session.Inventory.uid,
                                      pWi.id, pWi.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);

                            ret_id = pWi.id;

                        }

                        break;
                    }
                case IFF_GROUP.BALL: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Ball[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                            NormalManagerDB.Instance.add(0,
                                  new CmdDeleteBall(_session.Inventory.uid, pWi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                            var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                            if (it != null)
                            {
                                _session.Inventory.WarehouseItems.Remove(it.id);
                            }

                            ret_id = _item.id;

                            // Se deletou a bola que estava equipada 
                            // troca para a bola padrão
                            if (_session.Inventory.UserEquipment.ball_typeid == _item._typeid)
                            {

                                WarehouseItemEx pBall = null;
                                var p = new Packet();

                                if ((pBall = _session.Inventory.FindWarehouseItemByTypeid(0x14000000)) == null)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error][Warning] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem a Comet padrao para substituir a bola[TYPEID=" + Convert.ToString(_item._typeid) + "] deletada. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                                else
                                { // Substitui

                                    // Update ON SERVER
                                    _session.Inventory.UserEquipment.ball_typeid = DEFAULT_COMET_TYPEID; // Comet Padrão

                                    _session.Inventory.UserEquippedItem.Ball_WI = pBall;

                                    // Update ON DB
                                    NormalManagerDB.Instance.add(0,
                                          new CmdUpdateBallEquiped(_session.Inventory.uid, _session.Inventory.UserEquipment.ball_typeid),
                                          SQLDBResponse,
                                          null);

#if DEBUG
                                    _smp.message_pool.getInstance.push(new message("[ItemManager::removeItem][Log] PLAYER[UID=" + Convert.ToString(_session.Inventory.uid) + "] substitui a bola[TYPEID=" + Convert.ToString(_item._typeid) + "] deletada pela COMET PADRAO[TYPEID=" + Convert.ToString(DEFAULT_COMET_TYPEID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                                    // Update ON GAME   
                                    _session.Send(Handle_PACKET_RESPONSE.pacote04B(_session, 2));
                                }
                            }

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)(_item.qntd);

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateBallQntd(_session.Inventory.uid,
                                      pWi.id, pWi.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);

                            ret_id = pWi.id;

                        }

                        break;
                    }
                case IFF_GROUP.CADDIE:
                    {
                        var pCi = _session.Inventory.FindCaddieById(_item.id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Caddie[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                               (Pangya_DB)new CmdDeleteCaddie(_session.Inventory.uid, pCi.id),
                              SQLDBResponse,
                              null);

                        //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_ci, second.Login, == , pCi->Login);
                        var it = _session.Inventory.FindCaddieById(pCi.id);

                        if (it != null)
                        {
                            _session.Inventory.Caddies.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.CAD_ITEM:
                    // por hora nao exclui esse por que ainda nao vi nenhum que exclui caddie item
                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um caddie item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.Inventory.FindCardById(_item.id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Card[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pCi.qntd <= _item.qntd)
                        { // Exclui Item[Card]


                            _item.stat.qntd_ant = pCi.qntd;

                            _item.stat.qntd_dep = 0;

                            _item.STDA_C_ITEM_QNTD = (short)(pCi.qntd * -1);

                            pCi.qntd = 0;

                            NormalManagerDB.Instance.add(0,
                                   (Pangya_DB)new CmdDeleteCard(_session.Inventory.uid, pCi.id),
                                  SQLDBResponse,
                                  null);

                            //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_card_info, Login, == , pCi->Login);
                            var it = _session.Inventory.FindCardById(pCi.id);

                            if (it != null)
                            {
                                _session.Inventory.Cards.Remove(it.id);
                            }

                            ret_id = _item.id;

                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = (int)pCi.qntd;

                            pCi.qntd -= _item.qntd;

                            _item.stat.qntd_dep = (int)pCi.qntd;

                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateCardQntd(_session.Inventory.uid,
                                      pCi.id, pCi.qntd),
                                  SQLDBResponse,
                                  null);

                            ret_id = pCi.id;

                        }

                        break;
                    }
                case IFF_GROUP.CHARACTER:
                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um character[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.CLUBSET: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um ClubSet[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                              new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                              SQLDBResponse,
                              null);

                        //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                        var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                        if (it != null)
                        {
                            _session.Inventory.WarehouseItems.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var pFi = _session.Inventory.FindMyRoomItemById(_item.id);

                        if (pFi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Furniture[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                              new CmdDeleteFurniture(_session.Inventory.uid, pFi.id),
                              SQLDBResponse,
                              null);

                        //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_mri, Login, == , pFi->Login);
                        var it = _session.Inventory.FindMyRoomItemById(pFi.id);

                        if (it != null)
                        {
                            _session.Inventory.MyRoomItems.Remove(it);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.HAIR_STYLE:
                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um hairstyle[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.ITEM: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= (short)_item.qntd)
                        { // Exclui o Item[Item]

                            _item.stat.qntd_ant = pWi.STDA_C_ITEM_QNTD;

                            _item.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * -1);

                            pWi.STDA_C_ITEM_QNTD = 0;

                            _item.stat.qntd_dep = pWi.STDA_C_ITEM_QNTD;

                            NormalManagerDB.Instance.add(0,
                                  new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                                  SQLDBResponse,
                                  null);

                            var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                            if (it != null)
                            {
                                _session.Inventory.WarehouseItems.Remove(it.id);
                            }

                            ret_id = _item.id;

                            //  ITEM_MANAGER_LOG("Deletou Item", _session, _item);
                        }
                        else
                        { // Att quantidade do Item

                            _item.stat.qntd_ant = (int)pWi.STDA_C_ITEM_QNTD;

                            pWi.STDA_C_ITEM_QNTD -= (short)(_item.qntd);

                            _item.stat.qntd_dep = (int)pWi.STDA_C_ITEM_QNTD;

                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateItemQntd(_session.Inventory.uid,
                                      pWi.id, pWi.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);

                            ret_id = pWi.id;
                        }

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var pMi = _session.Inventory.FindMascotById(_item.id);

                        if (pMi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Mascot[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                              new CmdDeleteMascot(_session.Inventory.uid, pMi.id),
                              SQLDBResponse,
                              null);

                        
                        var it = _session.Inventory.FindMascotById(pMi.id);

                        if (it != null)
                        {
                            _session.Inventory.Mascots.Remove(it.id);
                        }

                        ret_id = _item.id;

                        break;
                    }
                case IFF_GROUP.PART: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Part[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                              new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                              SQLDBResponse,
                              null);

                        //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                        var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                        if (it != null)
                        {
                            _session.Inventory.WarehouseItems.Remove(it.id);
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
                            NormalManagerDB.Instance.add(0,
                                  new CmdUpdateCharacterAllPartEquiped(_session.Inventory.uid, ci),
                                  SQLDBResponse,
                                  null);

                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Log] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] desequipou o Part[TYPEID=" + Convert.ToString(_item._typeid) + "] por que ele foi deletado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Update ON GAME
                            var p = new Packet(0x6B);

                            p.WriteByte(4); // 4 Sucesso
                            p.WriteByte(0); // Character All Parts

                            p.WriteBytes(ci.ToArray());
                            _session.Send(p);
                        }
                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um SetItem[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] mas nao e permitido. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                case IFF_GROUP.SKIN: // Warehouse
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_item.id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::removeItem][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou remover um Skin[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        _item.stat.qntd_ant = 1;

                        _item.STDA_C_ITEM_QNTD = short.MaxValue;

                        _item.stat.qntd_dep = 0;

                        NormalManagerDB.Instance.add(0,
                              new CmdDeleteItem(_session.Inventory.uid, pWi.id),
                              SQLDBResponse,
                              null);

                        //auto it = VECTOR_FIND_ITEM(_session.PlayerUserStatistics.v_wi, Login, == , pWi->Login);
                        var it = _session.Inventory.FindWarehouseItemById(pWi.id);

                        if (it != null)
                        {
                            _session.Inventory.WarehouseItems.Remove(it.id);
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
            object ret_wi = null;

            switch (sIff.Instance.getItemGroupIdentify(_psi.item._typeid))
            {
                case IFF_GROUP.ITEM:
                    {
                        var pWi_s = new WarehouseItemEx(_s_snd.Inventory.FindWarehouseItemById(_psi.item.id));

                        if (pWi_s == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] nao tem o item[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        if (pWi_s.STDA_C_ITEM_QNTD < (int)_psi.item.qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] nao tem quantidade de item[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + ", QNTD=" + Convert.ToString(_psi.item.qntd) + "] para transferir para o Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pWi_r = _s_rcv.Inventory.FindWarehouseItemByTypeid(_psi.item._typeid);

                        if (pWi_r != null)
                        { // Ele já tem atualiza

                            pWi_r.STDA_C_ITEM_QNTD += (short)_psi.item.qntd;

                            ret_wi = new WarehouseItemEx(pWi_r);

                            NormalManagerDB.Instance.add(9,
                                  new CmdUpdateItemQntd(_s_rcv.Inventory.uid,
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

                            CmdAddItem cmd_ai = new CmdAddItem(_s_rcv.Inventory.uid, // Waiter
                                wi, 0, 0);

                            NormalManagerDB.Instance.add(0,
                                  cmd_ai);

                            if (cmd_ai.getException().getCodeError() != 0)
                            {
                                throw cmd_ai.getException();
                            }

                            wi = new WarehouseItemEx(cmd_ai.getInfo());
                            if (wi.id <= 0)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Log] nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(wi._typeid) + "] para o player: " + Convert.ToString(_s_rcv.Inventory.uid), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                return null;
                            }

                            _s_rcv.Inventory.WarehouseItems.Add(wi.id, wi);

                            ret_wi = new WarehouseItemEx(pWi_r);

                        }

                        // Att Item do player que vendeu
                        pWi_s.STDA_C_ITEM_QNTD -= (short)_psi.item.qntd;
                        if (pWi_s.STDA_C_ITEM_QNTD == 0)
                        {
                            NormalManagerDB.Instance.add(10, // Delete Item
                                  new CmdDeleteItem(_s_snd.Inventory.uid, pWi_s.id),
                                  SQLDBResponse,
                                  null);

                           
                            var it = _s_snd.Inventory.FindWarehouseItemById(pWi_s.id);

                            if (it != null)
                            {
                                _s_snd.Inventory.WarehouseItems.Remove(it.id);
                            }

                        }
                        else
                        {
                            NormalManagerDB.Instance.add(9, // Update
                                  new CmdUpdateItemQntd(_s_snd.Inventory.uid,
                                      pWi_s.id,
                                      pWi_s.STDA_C_ITEM_QNTD),
                                  SQLDBResponse,
                                  null);
                        }
                        LogTransferItemSuccess("Transferiu Item", _s_snd, _s_rcv, _psi);
                        // Depois que que fazer um CmdPersonalShop Venda Log
                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var pWi_s = _s_snd.Inventory.FindWarehouseItemById(_psi.item.id);

                        if (pWi_s == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pWi_r = _s_rcv.Inventory.FindWarehouseItemByTypeid(_psi.item._typeid);

                        if (pWi_r != null && !sIff.Instance.IsCanOverlapped(pWi_r._typeid))
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "] tentou comprar o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que ele ja possui do Normal[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }
                        else
                        {

                            var part = sIff.Instance.findPart(_psi.item._typeid);

                            if (part == null)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "] tentou comprar o Part[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao esta no IFF_STRUCT do server, do Normal[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                return null;
                            }

                            // CmdTransferPart
                            NormalManagerDB.Instance.add(16,
                                  new CmdTransferPart(_s_snd.Inventory.uid,
                                      _s_rcv.Inventory.uid, pWi_s.id,
                                      (byte)part.type_item),
                                  SQLDBResponse,
                                  null);

                            // Add para o player que comprou
                            ret_wi = pWi_s;
                            _s_rcv.Inventory.WarehouseItems.Add(pWi_s.id, pWi_s);

                            // Deleta do player que vendeu
                           
                            var it = _s_snd.Inventory.FindWarehouseItemById(pWi_s.id);

                            if (it != null)
                            {
                                _s_snd.Inventory.WarehouseItems.Remove(it.id);
                            }

                        }
                        LogTransferItemSuccess("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        // Vai ter mas só quando eu liberar no cliente
                        var @base = sIff.Instance.findCommomItem(_psi.item._typeid);

                        if (@base == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "] tentou comprar ClubSet[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao existe no IFF_STRUCT do server, do Normal[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Log][Warning] Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "] tentou comprar ClubSet[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] no Personal Shop[Owner UID=" + Convert.ToString(_s_snd.Inventory.uid) + "], mas eu ainda nao liberei " + (@base.Shop.flag_shop.can_send_mail_and_personal_shop ? "" : "no cliente e") + " no server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        LogTransferItemSuccess("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        // Vai ter mas só quando eu liberar no cliente
                        var @base = sIff.Instance.findCommomItem(_psi.item._typeid);

                        if (@base == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "] tentou comprar Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] que nao existe no IFF_STRUCT do server, do Normal[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pCi_s = _s_snd.Inventory.FindCardById(_psi.item.id);

                        if (pCi_s == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + "] para enviar para transferir para o player recv[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        if (pCi_s.qntd < (int)_psi.item.qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player send[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] nao tem quantidade do Card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", ID=" + Convert.ToString(_psi.item.id) + ", QNTD=" + Convert.ToString(_psi.item.qntd) + "] para transferir para o Normal[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return null;
                        }

                        var pCi_r = _s_rcv.Inventory.FindCardByTypeid(_psi.item._typeid);

                        if (pCi_r != null)
                        { // Ele já tem atualiza

                            pCi_r.qntd += (ushort)_psi.item.qntd;

                            _psi_r.item.id = pCi_r.id;

                            ret_wi = pCi_r;

                            NormalManagerDB.Instance.add(9,
                                  new CmdUpdateCardQntd(_s_rcv.Inventory.uid,
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

                            CmdAddCard cmd_ac = new CmdAddCard(_s_rcv.Inventory.uid, // Waiter
                                ci, 0, 0);

                            NormalManagerDB.Instance.add(0,
                                  cmd_ac);

                            if (cmd_ac.getException().getCodeError() != 0)
                            {
                                throw cmd_ac.getException();
                            }

                            ci = cmd_ac.getInfo();
                            _psi_r.item.id = ci.id;

                            if (ci.id <= 0)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Log] nao conseguiu adicionar o Card[TYPEID=" + Convert.ToString(ci._typeid) + "] para o player: " + Convert.ToString(_s_rcv.Inventory.uid), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                return null;
                            }
                            ret_wi = ci;
                            _s_rcv.Inventory.Cards.Add(ci.id, ci);
                        }

                        // Att Item do player que vendeu
                        pCi_s.qntd -= (_psi.item.qntd);

                        if (pCi_s.qntd == 0)
                        {
                            NormalManagerDB.Instance.add(10, // Delete Item
                                  new CmdDeleteCard(_s_snd.Inventory.uid, pCi_s.id),
                                  SQLDBResponse,
                                  null);

                           
                            var it = _s_snd.Inventory.FindCardById(pCi_s.id);

                            if (it != null)
                            {
                                _s_snd.Inventory.Cards.Remove(it.id);
                            }

                        }
                        else
                        {
                            NormalManagerDB.Instance.add(9, // Update
                                  new CmdUpdateCardQntd(_s_snd.Inventory.uid,
                                      pCi_s.id, pCi_s.qntd),
                                  SQLDBResponse,
                                  null);
                        }
                        LogTransferItemSuccess("Transferiu Item", _s_snd, _s_rcv, _psi);
                        break;
                    }
                default: // Não suporta todos os outros, [não é permitido vender no Personal Shop]
                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::transferItem][Error] player_rcv[UID=" + Convert.ToString(_s_rcv.Inventory.uid) + "], player_snd[UID=" + Convert.ToString(_s_snd.Inventory.uid) + "] Esse Item[TYPEID=" + Convert.ToString(_psi.item._typeid) + "] nao pode ser vendido no Personal Shop", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
            }

            if (ret_wi != null)
                NormalManagerDB.Instance.add(15, new CmdPersonalShopLog(_s_snd.Inventory.uid, _s_rcv.Inventory.uid, _psi, _psi_r.item.id), ItemManager.SQLDBResponse, null);


            return ret_wi;
        }

        public static int exchangeCadieMagicBox(Player _session, uint _typeid, int _id, uint _qntd)
        { 
            int ret_id = -1;

            switch (sIff.Instance.getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CADDIE:
                    {
                        var pCi = _session.Inventory.FindCaddieById(_id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de caddie é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var caddie = sIff.Instance.findCaddie(_typeid);

                        if (caddie == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (caddie.valor_mensal > 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que é por tempo, isso nao é permitido pelo server", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var pMi = _session.Inventory.FindMascotById(_id);

                        if (pMi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de mascot é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var mascot = sIff.Instance.findMascot(_typeid);

                        if (mascot == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (mascot.Shop.flag_shop.time_shop.dia > 0 && mascot.Shop.flag_shop.time_shop.active)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao é permitido[de tempo] trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // passa

                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var pCi = _session.Inventory.FindCardById(_id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pCi.qntd < (int)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Card no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pCi.qntd) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var card = sIff.Instance.findCard(_typeid);

                        if (card == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // passa

                        break;
                    }
                // Warehouse Item
                case IFF_GROUP.AUX_PART:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar AuxPart no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var auxPart = sIff.Instance.findAuxPart(_typeid);

                        if (auxPart == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.BALL:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Ball no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var ball = sIff.Instance.findBall(_typeid);

                        if (ball == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.ITEM:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Item no CadiCauldron mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var item = sIff.Instance.findItem(_typeid);

                        if (item == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.CLUBSET:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o ClubSet no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de clubset é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var clubset = sIff.Instance.findClubSet(_typeid);

                        if (clubset == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.PART:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Part no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de part é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var part = sIff.Instance.findPart(_typeid);

                        if (part == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    }
                case IFF_GROUP.SKIN:
                    {
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Skin no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        if (_qntd != 1u)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de ItemSkin é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        var skin = sIff.Instance.findSkin(_typeid);

                        if (skin == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return -1;
                        }

                        ret_id = 1; // Passa

                        break;
                    } // End Warehouse Item
                default:
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeCadieMagicBox][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao pode no CadieCauldron. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    } // End Default
            } // End Switch

            return ret_id;
        }

        public static List<stItem> exchangeTikiShop(Player _session, uint _typeid, int _id, uint _qntd)
        { 

            List<stItem> v_item = new List<stItem>();

            switch (sIff.Instance.getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CADDIE:
                    {

                        if (_qntd > 1)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem duplicata de Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pCi = _session.Inventory.FindCaddieById(_id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var caddie = sIff.Instance.findCaddie(_typeid);

                        if (caddie == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (caddie.valor_mensal > 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um caddie[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que é por tempo, isso nao é permitido pelo server", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem duplicata de Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pMi = _session.Inventory.FindMascotById(_id);

                        if (pMi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_qntd != 1)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] quantidade[value=" + Convert.ToString(_qntd) + "] de mascot é errado, nao pode mais que 1. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var mascot = sIff.Instance.findMascot(_typeid);

                        if (mascot == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (mascot.Shop.flag_shop.time_shop.dia > 0 && mascot.Shop.flag_shop.time_shop.active)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um Mascot[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao é permitido[de tempo] trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        var pCi = _session.Inventory.FindCardById(_id);

                        if (pCi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pCi.qntd < (int)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Card no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pCi.qntd) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var card = sIff.Instance.findCard(_typeid);

                        if (card == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Card[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar AuxPart no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar AuxPart no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var auxPart = sIff.Instance.findAuxPart(_typeid);

                        if (auxPart == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_session.Inventory.isAuxPartEquiped(_typeid))
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode trocar o AuxPart[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] equipado no Tiki Shop. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Ball no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Ball no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var ball = sIff.Instance.findBall(_typeid);

                        if (ball == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Ball[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Item no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (pWi.STDA_C_ITEM_QNTD < (short)_qntd)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Item no Tiki Shop mais nao tem quantidade[have=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", request=" + Convert.ToString(_qntd) + "] de item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "]. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var iff_item = sIff.Instance.findItem(_typeid);

                        if (iff_item == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem duplicata de ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o ClubSet no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar ClubSet no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var clubset = sIff.Instance.findClubSet(_typeid);

                        if (clubset == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o ClubSet[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        var part = sIff.Instance.findPart(_typeid);

                        if (part == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Part[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if (_qntd > 0)
                        {
                            var pWi_all = _session.Inventory.FindAllPartNotEquiped(_typeid);

                            if (pWi_all.Count() < _qntd)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Part no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] quantidade[resq=" + Convert.ToString(_qntd) + ", value=" + Convert.ToString(pWi_all.Count()) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                return new List<stItem>();
                            }

                            if (_qntd == 1)
                            {
                                var pWi = _session.Inventory.FindWarehouseItemById(_id);

                                if (pWi == null)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    return new List<stItem>();
                                }

                                if ((pWi.flag & 96) == 96
                                    || (pWi.flag & 0x20) == 0x20
                                    || (pWi.flag & 0x40) == 0x40)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao pode trocar Part Rental[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no Tiki Shop. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem duplicata de Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var pWi = _session.Inventory.FindWarehouseItemById(_id);

                        if (pWi == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Skin no Warehouse Item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] para trocar. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        if ((pWi.flag & 0x20) == 0x20
                            || (pWi.flag & 0x40) == 0x40
                            || (pWi.flag & 0x60) == 0x60)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar Skin no Tiki Shop mais nao pode trocar item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] de tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            return new List<stItem>();
                        }

                        var skin = sIff.Instance.findSkin(_typeid);

                        if (skin == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem o Skin[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] no IFF_STRUCT do server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        _smp.LogManager.Instance.push(new AppMessage("[ItemManager::exchangeTikiShop][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] tentou trocar um item[TYPEID=" + Convert.ToString(_typeid) + ", ID=" + Convert.ToString(_id) + "] que nao pode no CadieCauldron. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    } // End Default
            } // End Switch

            return v_item;
        }
         
        public static bool isSetItem(uint _typeid)
        {
            return sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.SET_ITEM;
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
            if (sIff.Instance.getItemGroupIdentify(_typeid) != IFF_GROUP.SET_ITEM)
            {

                var cmd_dli = new CmdFindDolfiniLockerItem(_uid, // Waiter
                    _typeid);

                NormalManagerDB.Instance.add(0,
                      cmd_dli);

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

            switch (sIff.Instance.getItemGroupIdentify(_typeid))
            {
                case IFF_GROUP.CHARACTER:
                    {
                        var cmd_fc = new CmdFindCharacter(_uid, // Waiter
                            _typeid);

                        NormalManagerDB.Instance.add(0,
                              cmd_fc);

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

                        NormalManagerDB.Instance.add(0,
                              cmd_fc);

                        if (cmd_fc.getException().getCodeError() != 0)
                        {
                            throw cmd_fc.getException();
                        }

                        ret = cmd_fc.hasFound();

                        break;
                    }
                case IFF_GROUP.MASCOT:
                    {
                        var mi =  _ownerMascot(_uid, _typeid);

                        ret = (mi.id > 0); //cmd_fm.hasFound();

                        break;
                    }
                case IFF_GROUP.CARD:
                    {
                        var card =  _ownerCard(_uid, _typeid);

                        ret = (card.id > 0); //cmd_fc.hasFound();

                        break;
                    }
                case IFF_GROUP.FURNITURE:
                    {
                        var cmd_ff = new CmdFindFurniture(_uid, // Waiter
                            _typeid);

                        NormalManagerDB.Instance.add(0,
                              cmd_ff);

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
                        var aux_part =  _ownerAuxPart(_uid, _typeid);

                        ret = (aux_part.id > 0); //cmd_fwi.hasFound();

                        break;
                    }
                case IFF_GROUP.SET_ITEM:
                    ret =  ownerSetItem(_uid, _typeid);
                    break;
                case IFF_GROUP.HAIR_STYLE:
                    ret =  ownerHairStyle(_uid, _typeid);
                    break;
                case IFF_GROUP.CAD_ITEM: // Esse aqui verifica se já tem, mas não que não pode ter mais. mas sim para aumentar o tempo
                    ret =  ownerCaddieItem(_uid, _typeid);
                    break;
                case IFF_GROUP.MATCH:
                    {
                        var tsi =  _ownerTrofelEspecial(_uid, _typeid);

                        ret = (tsi.id > 0);

                        break;
                    } // End iff::MATCH
            } // End Switch

            // Player não tem o item no warehouse e nem no Dolfini Locker, Verifica no Mail Box dele
            if (!ret)
            {

                // Procura no Mail Box do player
                ret =  ownerMailBoxItem(_uid, _typeid);
            }

            return ret;
        }

        public static bool ownerSetItem(uint _uid, uint _typeid)
        {

            var set = sIff.Instance.findSetItem(_typeid);

            if (set != null)
            {
                for (var i = 0; i < (set.packege.item_typeid.Length); ++i)
                {
                    // Eleminar a verificação do character que ele só inclui se o player não tiver ele
                    // se ele tiver não faz diferença não anula o verificação do set
                    if (set.packege.item_typeid[i] != 0 && sIff.Instance.getItemGroupIdentify(set.packege.item_typeid[i]) != IFF_GROUP.CHARACTER)
                    {
                        if ( ownerItem(_uid, set.packege.item_typeid[i])) // se tiver 1 item que seja não pode ganhar o set se não vai duplicar os itens, que ele tem
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

            var caddie =  _ownerCaddieItem(_uid, _typeid);

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
                (uint)(sIff.Instance.CADDIE << 26) | sIff.Instance.getCaddieIdentify(_typeid));

            NormalManagerDB.Instance.add(0,
                  cmd_fc);

            if (cmd_fc.getException().getCodeError() != 0)
            {
                throw cmd_fc.getException();
            }

            return cmd_fc.getInfo();
        }

        public static CharacterInfo _ownerHairStyle(uint _uid, uint _typeid)
        {

            var hair = sIff.Instance.findHairStyle(_typeid);

            if (hair != null)
            {

                var cmd_fc = new CmdFindCharacter(_uid, // Waiter
                    (uint)(sIff.Instance.CHARACTER << 26) | hair.Character);

                NormalManagerDB.Instance.add(0,
                      cmd_fc);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fm);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fwi);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fc);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fwi);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fwi);

            if (cmd_fwi.getException().getCodeError() != 0)
            {
                throw cmd_fwi.getException();
            }

            return cmd_fwi.getInfo();
        }

        public static TrophySpecialInfo _ownerTrofelEspecial(uint _uid, uint _typeid)
        {

            var type_trofel = sIff.Instance.getItemSubGroupIdentify24(_typeid);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fts);

            if (cmd_fts.getException().getCodeError() != 0)
            {
                throw cmd_fts.getException();
            }

            return cmd_fts.getInfo();
        }

        public static bool ownerHairStyle(uint _uid, uint _typeid)
        {

            var hair = sIff.Instance.findHairStyle(_typeid);

            if (hair != null)
            {
                var character =  _ownerHairStyle(_uid, _typeid);

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

            NormalManagerDB.Instance.add(0,
                  cmd_fmbi);

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

        public static bool IsBetween(SystemTime start, SystemTime end, SystemTime current)
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
                throw new Exception("[ItemManager::betweenTimeSystem][Error] Item nao e um item de tempo.");


            SystemTime st = new SystemTime();//gerar o datetime now
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
            stItem.stDate date = new stItem.stDate(_date);

            return betweenTimeSystem(ref date);
        }

        private static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[ItemManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

        public static uint CalculateSafeExchangeCount(Player session, CadieExchangeItem cost, uint clientRequested, uint hardLimit)
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

            uint playerHas =  getItemQuantity(
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
                    $"[CHEAT] UID={session.Inventory.uid} RequestCount ={clientRequested}, RequestMax={maxPossible} quantidade inválida",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 903, 0xDEAD0004)
                );
            }

            if (clientRequested > hardLimit)
            {
                _smp.LogManager.Instance.push(
                    new AppMessage(
                        $"[WARN] UID={session.Inventory.uid} limitado de {clientRequested} para {hardLimit}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    )
                );

                return hardLimit;
            }

            return clientRequested;
        }

        public static uint getItemQuantity(Player session, uint typeid, int itemId)
        {
            var type = sIff.Instance.getItemGroupIdentify(typeid);
            switch (type)
            {
                case IFF_GROUP.CARD:
                    {
                        var item = session.Inventory.FindCardById(itemId);
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
                            var item = session.Inventory.FindWarehouseItemById(itemId);

                            // Se o item não existe no warehouse, retorna 0
                            if (item == null) return 0;

                            // Se for PART, e o player possui o item, retornamos 1 (count fixo)
                            // ou usamos a quantidade do banco se ela for maior que 0.
                            return (uint)((item != null) ? (uint)item.STDA_C_ITEM_QNTD == 0 ? 1 : item.STDA_C_ITEM_QNTD : 0);
                        }
                        else
                        {
                            if (type == IFF_GROUP.CADDIE)
                            {
                                var item = session.Inventory.FindCaddieById(itemId);

                                // Se o item não existe no warehouse, retorna 0
                                if (item == null) return 0;

                                // Se for PART, e o player possui o item, retornamos 1 (count fixo)
                                // ou usamos a quantidade do banco se ela for maior que 0.
                                return 1;
                            }
                        }

                        // Para outros itens (Consumíveis), retorna a quantidade real
                        var warehouseItem = session.Inventory.FindWarehouseItemByTypeidAndId(typeid, itemId);
                        return (uint)((warehouseItem != null) ? (uint)warehouseItem.STDA_C_ITEM_QNTD == 0 ? 1 : warehouseItem.STDA_C_ITEM_QNTD : 0);
                    }
                default:
                    return 0;
            }
        }
    }
}