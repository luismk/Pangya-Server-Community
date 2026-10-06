using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Security.Cryptography;
namespace Pangya_GameServer.Engine
{
    public class LotterySystem : IDisposable
    {
        private SortedDictionary<ulong, LotteryCtx> Rollet = new SortedDictionary<ulong, LotteryCtx>();
        private List<LotteryCtx> Items = new List<LotteryCtx>();
        private List<ulong> Numbers = new List<ulong>();
        private ulong LimitProb = new ulong();

        public LotterySystem()//pode utilizar um valor randmico para inicializar o sistema de loteria
        {
            this.LimitProb = 0;
            Initialize();
        }

        public void Dispose()
        {
            Clear();
            ClearRoleta();

            if (Numbers.Count > 0)
            {
                Numbers.Clear();
            }
        }

        public void Clear()
        { // Clear Ctx

            if (Items.Count > 0)
            {
                this.LimitProb = 0;
                Items.Clear();
            }
        }

        public void Add(LotteryCtx _lc)
        {
            Items.Add(_lc);
        }

        public void Add(uint _prob, object _value)
        {
            Add(new LotteryCtx()
            {
                active = 1,
                prob = _prob,
                Value = _value
            });
        }

        public ulong getLimitProbilidade()
        {

            // Preenche roleta, para poder pegar o limite da probabilidade
            FillRoleta();

            return LimitProb;
        }

        // Retorna a quantidade de itens que tem para sortear
        public uint getCountItem()
        {
            return (uint)Items.Count;
        }

        // Deleta o Item Sorteado, para não sair ele de novo, se for passado true
        public LotteryCtx SpinRoleta(bool _remove_item_draw = false)
        {

            try
            {

                LotteryCtx lc = null;

                // Preencha a Roleta
                FillRoleta();

                ulong lucky = 0Ul;

                ShuffleRandomize();

                lucky = (Numbers[Random.Shared.Next(0, 4)] * (ulong)Random.Shared.Next()) % (LimitProb == 0 ? 1 : LimitProb + 1);

                // equivalente ao equal_range + fallback
                if (!TryLowerBound(Rollet, lucky, out lc, out KeyValuePair<ulong, LotteryCtx> bound))
                    return null;

                if (_remove_item_draw && lc != null)
                    RemoveItemWin(lc);

                return lc;
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Lottery::spinRoleta][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        private void Initialize()
        { 
            // 5 Rands Values
            for (var i = 0; i < 5; ++i)
            {
                Numbers.Add((ulong)Random.Shared.NextInt64());
            }

            ShuffleRandomize();
        }

        private void FillRoleta()
        {

            if (Items.Count == 0)
            {
                throw new exception("[Lottery::fill_roleta][Error] nao tem lottery ctx, por favor popule o lottery primeiro.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOTTERY,
                    1, 0));
            }

            // Limpa Roleta
            ClearRoleta(); 
            Shuffle(Items, CreateStrongRandom());

            LimitProb = 0Ul;

            // Preenche Roleta
            foreach (var el in Items)
            {
                if (el.active == 1)
                {
                    el.offset[0] = (LimitProb == 0 ? LimitProb : LimitProb + 1);
                    el.offset[1] = LimitProb += (el.prob <= 0) ? 100 : el.prob;
                    Rollet[el.offset[0]] = el;
                    Rollet[el.offset[1]] = el;
                }
            }
        }

        private void ClearRoleta()
        {
            if (Rollet.Count > 0)
            {
                Rollet.Clear();
            }
        }

        private void RemoveItemWin(LotteryCtx _lc)
        {
            if (_lc != null)
            {
                _lc.active = 0;
            }
        }

        private void ShuffleRandomize()
        {
            // === Shuffle 1: equivalente ao mt19937_64 ===
            Shuffle(Numbers, CreateStrongRandom());

            // === Shuffle 2: equivalente ao default_random_engine === 
            Shuffle(Numbers, Random.Shared);
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

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool TryLowerBound(SortedDictionary<ulong, LotteryCtx> dict,  ulong key, out LotteryCtx value, out KeyValuePair<ulong, LotteryCtx> bound)
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

        public class LotteryCtx
        {
            public LotteryCtx()
            {
                prob = 0; // Probabilidade
                Value = new object();
                offset = new ulong[2]; // 0 start, 1 end
                active = 1; // 0 ou 1 ativo
            }

            public uint prob = 0; // Probabilidade
            public object Value;
            public ulong[] offset; // 0 start, 1 end
            public byte active = 0; // 0 ou 1 ativo
        }

    }
}
