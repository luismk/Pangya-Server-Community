using System;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Models
{
    public enum SCRATCH_CARD_TYPE : uint
    {
        SCT_COMMUN = 0, // tipo = 0(normal)
        SCT_COOKIE = 1, // tipo =1(cookie)
        SCT_RARE = 2  // tipo =2(rare ou super rare)
    }

    public class ctx_scratch_card_item
    {
        public uint _typeid;
        public uint probabilidade;
        public uint qntd;
        public int numero;                // Número que o papel shop já está
        public SCRATCH_CARD_TYPE tipo;
        public bool active;               // Active 0 ou 1

        public ctx_scratch_card_item()
        {
            clear();
        }

        public void clear()
        {
            _typeid = 0;
            probabilidade = 0;
            qntd = 0;
            numero = 0;
            tipo = SCRATCH_CARD_TYPE.SCT_COMMUN;
            active = false;
        }
    }

    public class ctx_scratch_card_rate
    {
        public string Nome;
        public SCRATCH_CARD_TYPE Tipo;
        public uint Prob;

        public ctx_scratch_card_rate(uint _ul = 0u)
        {
            clear();
        }

        public void clear()
        {
            Nome = string.Empty;
            Tipo = SCRATCH_CARD_TYPE.SCT_COMMUN;
            Prob = 0;
        }
    }

    public class ctx_scratch_card
    {
        public uint numero;                // Atual Número do Papel Shop
        public bool limitted_per_day;      // Limitado por dia, tem uma quantidade que pode jogar  // 0 ou 1
        public DateTime update_date;       // Date de atualização do dia do papel shop

        public ctx_scratch_card()
        {
            clear();
        }

        public void clear()
        {
            numero = 0;
            limitted_per_day = false;
            update_date = DateTime.MinValue;
        }

        public string toString()
        {
            return "NUMERO=" + numero + ", LIMITTED_PER_DAY=" + (limitted_per_day ? 1 : 0)
                + ", UPDATE_DATE=" + (update_date == DateTime.MinValue ? "0" : update_date.ToString("o"));
        }
    }

    public class ctx_scratch_card_item_win
    {
        public ctx_scratch_card_item ctx_psi = new ctx_scratch_card_item();
        public uint qntd;            // Qntd do item que foi sorteado
        public object item;          // void* item; stItem placeholder

        public ctx_scratch_card_item_win(uint _ul = 0u)
        {
            clear();
        }

        public void clear()
        {
            ctx_psi.clear();
            qntd = 0;
            item = null;
        }
    }

    public class ctx_scratch_card_coupon
    {
        public uint _typeid;
        public bool active;   // 0 ou 1

        public ctx_scratch_card_coupon()
        {
            clear();
        }

        public void clear()
        {
            _typeid = 0;
            active = false;
        }
    }
}
