// Refatoração completa da classe Lottery com correções de aleatoriedade e estrutura de roleta
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
namespace Pangya_GameServer.UTIL
{ 
    public class Lottery : System.IDisposable
    {
        public class LotteryCtx
        {
            public void clear()
            {
                prob = 0; // Probabilidade
                Value = new object();
                offset = new ulong[2]; // 0 start, 1 end
                active = 1; // 0 ou 1 ativo
            }
            public uint prob = 0; // Probabilidade
            public object Value = new object();
            public ulong[] offset = new ulong[2]; // 0 start, 1 end
            public byte active = 0; // 0 ou 1 ativo
        }

        public Lottery(ulong _value_rand = 0)//pode utilizar um valor randmico para inicializar o sistema de loteria
        {
            if (_value_rand > 0)
                rnd = new Random((int)_value_rand);
            else
                rnd = new Random();

            this.m_prob_limit = 0;

            initialize();
        }

        public void Dispose()
        {

            Clear();
            clear_roleta();

            if (m_rand_values.Count > 0)
            {
                m_rand_values.Clear();
            }
        }

        public void Clear()
        { // Clear Ctx

            if (m_ctx.Count > 0)
            {
                m_ctx.Clear();
            }
        }

        public void push(LotteryCtx _lc)
        {
            m_ctx.Add(_lc);
        }

        public void push(uint _prob, object _value)
        {

            LotteryCtx lc = new LotteryCtx();

            lc.active = 1;
            lc.prob = _prob;
            lc.Value = _value;

            push(lc);
        }


        public void Push(uint _prob, object _value)
        {

            LotteryCtx lc = new LotteryCtx();

            lc.active = 1;
            lc.prob = _prob;
            lc.Value = _value;

            push(lc);
        }
        public ulong getLimitProbilidade()
        {

            // Preenche roleta, para poder pegar o limite da probabilidade
            fill_roleta();

            return m_prob_limit;
        }

        // Retorna a quantidade de itens que tem para sortear
        public uint getCountItem()
        {
            return (uint)m_ctx.Count;
        }

        // Deleta o Item Sorteado, para não sair ele de novo, se for passado true
        public LotteryCtx spinRoleta(bool _remove_item_draw = false)
        {

            try
            {

                LotteryCtx lc = null;

                // Preencha a Roleta
                fill_roleta();

                ulong lucky = 0Ul;

                shuffle_values_rand();

                lucky = (m_rand_values[rnd.Next(0, 4)] * (ulong)rnd.Next()) % (m_prob_limit == 0 ? 1 : m_prob_limit + 1);

                // equivalente ao equal_range + fallback
                if (!TryLowerBound(m_roleta, lucky, out lc, out KeyValuePair<ulong, LotteryCtx> bound))
                    return null;

                if (_remove_item_draw && lc != null)
                    remove_draw_item(lc); 

                return lc; 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Lottery::spinRoleta][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        protected void initialize()
        {

            // 5 Rands Values
            for (var i = 0; i < 5u; ++i)
            {
                m_rand_values.Add((ulong)rnd.Next());
            }
              
            shuffle_values_rand();
        }

        protected void fill_roleta()
        {

            if (m_ctx.Count == 0)
            {
                throw new exception("[Lottery::fill_roleta][Error] nao tem lottery ctx, por favor popule o lottery primeiro.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOTTERY,
                    1, 0));
            }

            // Limpa Roleta
            clear_roleta();
            var rngStrong = CreateStrongRandom();
            Shuffle(m_ctx, rngStrong);

            m_prob_limit = 0Ul;

            // Preenche Roleta
            foreach (var el in m_ctx)
            {
                if (el.active == 1)
                {
                    el.offset[0] = (m_prob_limit == 0 ? m_prob_limit : m_prob_limit + 1);
                    el.offset[1] = m_prob_limit += (el.prob <= 0) ? 100 : el.prob;
                    m_roleta[el.offset[0]] = el;
                    m_roleta[el.offset[1]] = el;
                }
            }
        }

        protected void clear_roleta()
        {

            if (m_roleta.Count > 0)
            {
                m_roleta.Clear();
            }
        }

        protected void remove_draw_item(LotteryCtx _lc)
        {

            if (_lc != null)
            {
                _lc.active = 0;
            }
        }

        protected void shuffle_values_rand()
        {
            // === Shuffle 1: equivalente ao mt19937_64 ===
            var rngStrong = CreateStrongRandom();
            Shuffle(m_rand_values, rngStrong); 

            // === Shuffle 2: equivalente ao default_random_engine ===
            var rngFast = new Random(rnd.Next());
            Shuffle(m_rand_values, rngFast); 
        }
        private static Random CreateStrongRandom()
        {
            byte[] buffer = new byte[8];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            ulong seed64 = BitConverter.ToUInt64(buffer, 0);
            int seed32 = unchecked((int)(seed64 ^ (seed64 >> 32)));

            return new Random(seed32);
        }

        protected static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool TryLowerBound(
               SortedDictionary<ulong, LotteryCtx> dict,
               ulong key,
               out LotteryCtx value, out KeyValuePair<ulong, LotteryCtx> bound)
        {
            foreach (var kv in dict)
            {
                if (kv.Key >= key)
                {
                    value = kv.Value;
                    bound = kv;
                    return true;
                }
            }

            value = null;
            bound = new KeyValuePair<ulong, LotteryCtx>();
            return false;
        }

        private SortedDictionary<ulong, LotteryCtx> m_roleta = new SortedDictionary<ulong, LotteryCtx>();

        private List<LotteryCtx> m_ctx = new List<LotteryCtx>();
        private List<ulong> m_rand_values = new List<ulong>();

        private ulong m_prob_limit = new ulong();
        Random rnd;
    }
}
