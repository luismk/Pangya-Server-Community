using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;

using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Feature
{
    public class MemorialSystem
    {
        private void initialize()
        {

            try
            {
                // Carrega as Coin e os Itens
                var coins = sIff.Instance.getMemorialShopCoinItem().ToArray();
                var rares = sIff.Instance.getMemorialShopRareItem().ToArray();

                ctx_coin c = new ctx_coin();
                ctx_coin_item_ex ci = new ctx_coin_item_ex();

                try
                {
                    foreach (var el in coins)
                    {
                        c = new ctx_coin();
                        c.tipo = (MEMORIAL_COIN_TYPE)el.type;
                        c._typeid = el.ID;
                        c.probabilidade = el.Probabilities;

                        foreach (var el2 in rares)
                        {
                            if (!el.gacha_range.empty() && !el.gacha_range.isBetweenGacha(el2.gacha.Number))
                                continue;

                            if (el.emptyFilter())
                            {
                                ci = new ctx_coin_item_ex
                                {
                                    tipo = (int)el2.RareType,
                                    _typeid = el2.ID,
                                    probabilidade = el2.Probabilities,
                                    gacha_number = (int)el2.gacha.Number,
                                    qntd = 1
                                };
                                //if (el2.RareType == MemorialRareType.Super_Rare2)
                                //{
                                //    _smp.LogManager.Instance.push(new AppMessage($"[MemorialSystem::Test] Raro->{(int)el2.RareType}, ID->{el2.ID} ", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                //}

                                c.item.Add(ci);
                            }
                            else
                            {
                                for (var i = 0; i < el2.filter.Length; ++i)
                                {
                                    if (el.hasFilter(el2.filter[i]))
                                    {
                                        ci = new ctx_coin_item_ex();
                                        ci.tipo = (int)el2.RareType;
                                        ci._typeid = el2.ID;
                                        ci.probabilidade = el2.Probabilities;
                                        ci.gacha_number = (int)el2.gacha.Number;
                                        ci.qntd = 1;
                                        //if (el2.RareType == MemorialRareType.Super_Rare2)
                                        //{
                                        //    _smp.LogManager.Instance.push(new AppMessage($"[MemorialSystem::Test] Raro->{(int)el2.RareType}, ID->{el2.ID} ", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        //}
                                        c.item.Add(ci);

                                        break;
                                    }
                                }
                            }
                        }

                        if (!m_coin.ContainsKey(c._typeid))
                            m_coin.Add(c._typeid, c);
                    } // Fim do loop de Coin Item
                }
                catch (exception e)
                {
                    throw e;
                }
            }
            catch (exception e)
            {
                throw e;
            }

            // Add os Itens Padr�es, para quando n�o ganha o rare item
            var cmd_mnii = new CmdMemorialNormalItemInfo();  

            NormalManagerDB.Instance.add(0, cmd_mnii);

            if (cmd_mnii.getException().getCodeError() != 0)
                throw cmd_mnii.getException();

            m_consolo_premio = cmd_mnii.getInfo();

            // Levels
            var cmd_mli = new CmdMemorialLevelInfo();

            NormalManagerDB.Instance.add(0, cmd_mli);

            if (cmd_mli.getException().getCodeError() != 0)
                throw cmd_mli.getException();


            m_level = cmd_mli.getInfo();


            m_load = (m_coin.Any() && m_level.Any() && m_level.Count == MEMORIAL_LEVEL_MAX + 1 && m_consolo_premio.Any());
        }


        public bool isLoad()
        {
            // + 1 no MEMORIAL_LEVEL_MAX por que � do 0 a 24, da 25 Levels
            bool isLoad = m_load && m_coin.Any() && m_level.Any() && m_level.Count == MEMORIAL_LEVEL_MAX + 1 && m_consolo_premio.Any();

            return isLoad;
        }


        public void load()
        {
            if (isLoad())
                clear();

            initialize();
        }


        public ctx_coin findCoin(uint _typeid)
        {
            var it = m_coin.Find(_typeid);

            if (it.Any())
            {
                return m_coin.GetValue(_typeid);
            }
            return null;
        }


        protected void clear()
        {

            if (m_coin.Any())
            {
                m_coin.Clear();
            }

            if (m_level.Any())
            {
                m_level.Clear();
            }

            if (m_consolo_premio.Any())
            {
                m_consolo_premio.Clear();
            }

            m_load = false;
        }


        protected uint calculeMemorialLevel(uint _achievement_pontos)
        {

            if (_achievement_pontos == 0)
            {
                return 0u; // Level 0
            }

            var level = ((_achievement_pontos - 1) / 300);

            return level > MEMORIAL_LEVEL_MAX ? (uint)MEMORIAL_LEVEL_MAX : level;
        }


        public List<ctx_coin_item_ex> Test(ctx_coin _ctx_c)
        {
            if (!isLoad())
                throw new exception("[MemorialSystem::drawCoin][Error] Memorial System not loaded, call load first.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 2, 0));

            if (_ctx_c._typeid == 0)
                throw new exception("[MemorialSystem::drawCoin][Error] coin _typeid invalid (zero).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 3, 0));

            if (_ctx_c.item.Count == 0)
                throw new exception("[MemorialSystem::drawCoin][Error] coin is empty.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 4, 0));

            List<ctx_coin_item_ex> v_item = new List<ctx_coin_item_ex>();
            LotterySystem lottery = new LotterySystem();

            uint level = 1; // Memorial Level (você pode ajustar essa lógica se quiser)

            // Adiciona os itens raros com multiplicador reduzido
            float rareMultiplier = 0.5f;

            foreach (var el in _ctx_c.item)
            {
                bool shouldAdd = false;

                switch (_ctx_c.tipo)
                {
                    case MEMORIAL_COIN_TYPE.MCT_NORMAL:
                        shouldAdd = el.gacha_number < 0 || (uint)el.gacha_number <= m_level[level].gacha_number;
                        break;
                    case MEMORIAL_COIN_TYPE.MCT_PREMIUM:
                        shouldAdd = el.gacha_number < 0 || (uint)el.gacha_number <= m_level[MEMORIAL_LEVEL_MAX - 1].gacha_number;
                        break;
                    case MEMORIAL_COIN_TYPE.MCT_SPECIAL:
                        shouldAdd = true;
                        break;
                    default:
                        throw new exception("[MemorialSystem::drawCoin][Error] Unknown coin type.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 6, 0));
                }

                if (shouldAdd)
                {
                    uint weight = _ctx_c.tipo == MEMORIAL_COIN_TYPE.MCT_SPECIAL ? (uint)el.probabilidade : (uint)(el.probabilidade * rareMultiplier);
                    lottery.Add(weight, el);
                }
            }

            // Pega limite de probabilidade e ajusta com o Rate
            ulong limit_prob = lottery.getLimitProbilidade();

            // Pega quantidade de itens comuns disponíveis para este Type de coin
            var count_item = m_consolo_premio.Values.Count(el => el.tipo == (_ctx_c.tipo == MEMORIAL_COIN_TYPE.MCT_PREMIUM ? 1 : 0));

            // Calcula Rate memorial (exemplo seu)
            var rate_memorial = (float)GameServer.Instance.getInfo().Rate.MemorialShop / 100.0f;

            if (_ctx_c.probabilidade > 0)
            {
                rate_memorial += (_ctx_c.probabilidade * 4.0f / 100.0f);
            }

            // Corrige divisão para float!
            limit_prob = (ulong)(limit_prob * (4.0f / rate_memorial));

            if (count_item > 0)
                count_item = (int)(limit_prob / (ulong)count_item);

            var rnd = new Random();
            uint commonMultiplier = 10; // aumenta chance dos comuns (antes não estava usando)

            // Adiciona itens comuns na roleta com peso aumentado
            foreach (var el in m_consolo_premio.Values)
            {
                if (el.tipo == (_ctx_c.tipo == MEMORIAL_COIN_TYPE.MCT_PREMIUM ? 1 : 0))
                {
                    // Peso aleatório + multiplicador para garantir chance maior
                    uint peso = (uint)(rnd.Next(5, count_item > 5 ? count_item : 10) * commonMultiplier);
                    lottery.Add(peso, el);
                }
            }

            // Sorteia N itens
            int count = 10;
            while (count > 0)
            {
                var lc = lottery.SpinRoleta(true);
                if (lc == null || lc.Value == null)
                    throw new exception("[MemorialSystem::drawCoin][Error] Falha ao sortear item.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 5, 0));

                if (lc.Value is ctx_coin_set_item csi)
                {
                    foreach (var item in csi.item)
                        v_item.Add(item);
                }
                else if (lc.Value is ctx_coin_item_ex ci)
                {
                    v_item.Add(ci);
                }
                else
                {
                    throw new exception("[MemorialSystem::drawCoin][Error] Item sorteado desconhecido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 6, 1));
                }

                count--;
            }

            return v_item;
        }


        public List<ctx_coin_item_ex> drawCoin(Player _session, ctx_coin _ctx_c)
        {
            if (!isLoad())
            {
                throw new exception("[MemorialSystem::" + "drawCoin" + "][Error] Memorial System not loadded, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM,
                    2, 0));
            }

            if (_ctx_c._typeid == 0)
            {
                throw new exception("[MemorialSystem::" + "drawCoin" + "][Error] coin _typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM,
                    3, 0));
            }

            if (_ctx_c.item.Count == 0)
            {
                throw new exception("[MemorialSystem::" + "drawCoin" + "][Error] coin is empty.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM,
                    4, 0));
            }

            List<ctx_coin_item_ex> v_item = new List<ctx_coin_item_ex>();


            LotterySystem lottery = new LotterySystem();

            ctx_coin_item_ex ci = null;
            ctx_coin_set_item csi = null;

            // Calcula Memorial Level Pelos Achievement Pontos
            uint level = calculeMemorialLevel(_session.UserInfo.Achievements.getPontos());

            foreach (var el in _ctx_c.item)
            {
                bool shouldAdd = false;

                switch (_ctx_c.tipo)
                {
                    case MEMORIAL_COIN_TYPE.MCT_NORMAL:
                        shouldAdd = true;//el.gacha_number < 0 || (uint)el.gacha_number <= m_level[Level].gacha_number;
                        break;
                    case MEMORIAL_COIN_TYPE.MCT_PREMIUM:
                        shouldAdd = true;//el.gacha_number < 0 || (uint)el.gacha_number <= m_level[MEMORIAL_LEVEL_MAX - 1].gacha_number;
                        break;
                    case MEMORIAL_COIN_TYPE.MCT_SPECIAL:
                        shouldAdd = true;
                        break;
                    case MEMORIAL_COIN_TYPE.MCT_CHARACTER:
                        shouldAdd = true;
                        break;
                    default:
                        throw new exception("[MemorialSystem::drawCoin][Error] Unknown coin type.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 6, 0));
                }

                if (shouldAdd)
                {
                    lottery.Add(el.probabilidade, el);
                }
            }

            // Pega limite de probabilidade e ajusta com o Rate
            ulong limit_prob = lottery.getLimitProbilidade();

            // Pega quantidade de itens comuns disponíveis para este Type de coin
            var count_item = m_consolo_premio.Values.Count(el => el.tipo == (_ctx_c.tipo == MEMORIAL_COIN_TYPE.MCT_PREMIUM ? 1 : 0));

            // Calcula Rate memorial
            var rate_memorial = GameServer.Instance.getInfo().Rate.MemorialShop / 100.0f;

            if (_ctx_c.probabilidade > 0)
            {
                rate_memorial += (_ctx_c.probabilidade * 4.0f / 100.0f);
            }

            // Corrige divisão para float!
            limit_prob = (ulong)(limit_prob * (4.0f / rate_memorial));

            if (count_item > 0)
                count_item = (int)(limit_prob / (ulong)count_item);

            // Adiciona itens comuns na roleta com peso aumentado
            foreach (var el in m_consolo_premio.Values)
            {
                if (el.tipo == (_ctx_c.tipo == MEMORIAL_COIN_TYPE.MCT_PREMIUM ? 1 : 0))//são dois tipos, o premium vem set item, eo Normal, vem somente 1 item
                {
                    // Peso aleatório + multiplicador para garantir chance maior
                    lottery.Add((uint)count_item, el);
                }
            }

            LotterySystem.LotteryCtx lc = null;
            uint count = 1; // Qntd de pr�mios sorteados

            while (count > 0)
            {

                lc = lottery.SpinRoleta(true);

                if (lc == null || lc.Value == null)
                {
                    continue; // Tenta novamente
                }

                bool is_set = lc.Value is ctx_coin_set_item ? true : false;

                if (is_set)
                {
                    csi = (ctx_coin_set_item)lc.Value;

                    foreach (var el in csi.item)
                    {
                        // Contianua que o player j� tem esse item, e n�o pode ter duplicatas dele 
                        if ((!sIff.Instance.IsCanOverlapped(el._typeid, true) || sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.CAD_ITEM) && !_session.Inventory.ownerItem(el._typeid))
                            continue;

                        v_item.Add(el);

                    }
                }
                else
                {
                    ci = (ctx_coin_item_ex)lc.Value;

                    if ((!sIff.Instance.IsCanOverlapped(ci._typeid, true) ||
                         sIff.Instance.getItemGroupIdentify(ci._typeid) == IFF_GROUP.CAD_ITEM)
                        && _session.Inventory.ownerItem(ci._typeid))
                    {
                        continue; // Item não elegível, tenta novamente
                    }

                    v_item.Add(ci);
                }

                count = 0; // item válido sorteado
            }

            if (v_item.Count == 0)
            {
                throw new exception("[MemorialSystem::drawCoin][Error] não conseguiu sortear item elegível.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MEMORIAL_SYSTEM, 5, 2));
            }

            return v_item;
        }


        private MultiMap<uint, ctx_coin> m_coin = new MultiMap<uint, ctx_coin>();
        private Dictionary<uint, ctx_memorial_level> m_level = new Dictionary<uint, ctx_memorial_level>();
        private Dictionary<uint, ctx_coin_set_item> m_consolo_premio = new Dictionary<uint, ctx_coin_set_item>();

        uint MEMORIAL_LEVEL_MAX = 24;  
        private bool m_load = false;
    }


    public class sMemorialSystem : Singleton<MemorialSystem>
    { }
}
