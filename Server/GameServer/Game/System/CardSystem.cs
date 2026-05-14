using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public class CardSystem
    {

        
        private List<Card> m_card = new List<Card>(); // Todos os Card
        
        private Dictionary<uint, CardPack> m_card_pack = new Dictionary<uint, CardPack>(); // Todos os Card Pack
        
        private Dictionary<uint, CardPack> m_box_card_pack = new Dictionary<uint, CardPack>(); // Todos os Box Card Pack

        
        private bool m_load; // Load CardSystem 
        public CardSystem()
        {
            this.m_load = false;
            // Inicializa
            //initialize();
        }


        // Load
        
        public void load()
        {

            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        
        public bool isLoad()
        {
            return (m_load && m_card_pack.Count > 0 && m_box_card_pack.Count > 0);
        }

        // finders
        
        public CardPack findCardPack(uint _typeid)
        {

            if (!isLoad())
            {
                throw new exception("[CardSystem::findCardPack][Error] Card System nao esta carregado, carregue ele primeiro antes de procurar um Card Pack.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    5, 0));
            }

            if (_typeid == 0)
            {
                throw new exception("[CardSystem::findCardPack][Error] _typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                1, 0));
            }

            return m_card_pack.FirstOrDefault(c => c.Key == _typeid).Value;
        }

        
        public CardPack findBoxCardPack(uint _typeid)
        {

            if (!isLoad())
            {
                throw new exception("[CardSystem::findBoxCardPack][Error] Card System nao esta carregado, carregue ele primeiro antes de procurar um Box Card Pack.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    5, 0));
            }

            if (_typeid == 0)
            {
                throw new exception("[CardSystem::findBoxCardPack][Error] _typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    1, 0));
            }
            return m_box_card_pack.FirstOrDefault(c => c.Key == _typeid).Value;
        }

        
        public Card findCard(uint _typeid)
        {

            if (!isLoad())
            {
                throw new exception("[CardSystem::findCard][Error] Card System nao esta carregado, carregue ele primeiro antes de procurar um Card.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    5, 0));
            }

            if (_typeid == 0)
            {
                throw new exception("[CardSystem::findCard][Error] _typeid is invalid(zeror)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    1, 0));
            }

            return m_card.FirstOrDefault(c => c._typeid == _typeid); 
        }

        
        public List<Card> draws(CardPack _cp)
        {
            if (_cp == null)
            {
                throw new exception("[CardSystem::findCardPack][Error] CardPack is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    1, 0));
            }

            if (_cp._typeid == 0
                || _cp.num == 0
                || _cp.card.Count == 0)
            {
                throw new exception("[CardSystem::findCardPack][Error] CardPack[TYPEID=" + Convert.ToString(_cp._typeid) + ", NUM=" + Convert.ToString(_cp.num) + ", card(s)=" + Convert.ToString(_cp.card.Count) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    1, 0));
            }

            List<Card> v_card = new List<Card>();

            Lottery lottery = new Lottery();

            foreach (var el in _cp.card)
            {
                lottery.Push((uint)(el.prob * (double)((el.tipo > CARD_TYPE.T_SECRET ? 10.0f : _cp.rate.value[(int)el.tipo] / 100.0f))), el);
            }

            for (var i = 0; i < _cp.num; ++i)
            {
                var lc = lottery.spinRoleta(true);

                if (lc == null)
                {
                    throw new exception("[CardSystem::draws][ErrorSystem] nao conseguiu sortear um card", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                        2, 0));
                }

                if (((Card)lc.Value) == null)
                {
                    throw new exception("[CardSystem::draws][ErrorSystem] valor retornado do sorteio é invalido(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                        3, 0));
                }

                v_card.Add((Card)lc.Value);
            }

            return v_card;
        }

        
        public Card drawsLoloCardCompose(LoloCardComposeEx _lcc)
        {

            Card card = new Card();


            Lottery lottery = new Lottery();

            uint prob = 0;

            for (var i = 0; i < (_lcc._typeid.Length); ++i)
            {
                prob += (uint)((_lcc.tipo + 1) * 20);
            }

            for (var i = 1; i <= 5; ++i)
            {
                var it = m_card_pack.Values.FirstOrDefault(c => c.volume == i);


                if (it != null)
                {
                    foreach (var el in it.card)
                    {
                        lottery.Push((el.tipo > CARD_TYPE.T_NORMAL ? el.prob + prob : el.prob), el);
                    }
                }
            }

            var lc = lottery.spinRoleta();

            if (lc == null)
            {
                throw new exception("[CardSystem::drawsLoloCardCompose][ErrorSystem] nao conseguiu sortear um card", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    2, 0));
            }

            if ((Card)lc.Value == null)
            {
                throw new exception("[CardSystem::drawsLoloCardCompose][ErrorSystem] valor retornado do sorteio é invalido(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                    3, 0));
            }

            card = (Card)lc.Value;

            return card;
        }

        
        protected void initialize()
        {
            // Load Card from IFF_STRUCT
            var card = sIff.getInstance().getCard();

            foreach (var el in card)
            {
                switch (sIff.getInstance().getItemSubGroupIdentify22(el.ID))
                {
                    case 0: // Character
                    case 1: // Caddie
                    case 2: // Special
                    case 5: // NPC
                        m_card.Add(new Card() { _typeid = el.ID, prob = 0, tipo = (CARD_TYPE)(el.Rarity) });
                        break;
                    case 4: // Box Card Pack
                        {
                            m_box_card_pack[el.ID] = new CardPack(el.ID, 3, (byte)el.Volumn);
                            break;
                        }
                    case 3: // Card Pack
                            // N�o usa esses, por que � card pack os dois
                        break;
                    default:
                        throw new exception("[CardSystem::initialize][Error] Card Group Type Is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CARD_SYSTEM,
                            4, 0));
                }
            }

            // Load Card Pack Map
            CmdCardPack cmd_cp = new CmdCardPack(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_cp, null,
                null);

            if (cmd_cp.getException().getCodeError() != 0)
                throw cmd_cp.getException();

            m_card_pack = cmd_cp.getCardPack();

            // Load Box Card Pack
            foreach (var el in m_box_card_pack.Values)
            {
                var it = m_card_pack.Values.FirstOrDefault(el2 => el2.volume == el.volume);

                if (it != null)
                {
                    el.card = new List<Card>(it.card);
                }
            }
            // Carregado com sucesso
            m_load = true;
            if (m_card.Count == 0 || m_card_pack.Count == 0 || m_box_card_pack.Count == 0)
                _smp.message_pool.getInstance().push(new message("[CardSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

        }

        
        protected void clear()
        {

            //Monitor.Exit(m_cs);

            if (m_card.Count > 0)
            {
                m_card.Clear();
            }

            if (m_card_pack.Count > 0)
            {
                m_card_pack.Clear();
            }

            m_load = false; 
        }
    }
    public class sCardSystem : Singleton<CardSystem>
    {
    }
}
