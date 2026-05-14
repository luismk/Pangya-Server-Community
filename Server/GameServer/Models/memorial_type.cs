using System.Collections.Generic;

namespace Pangya_GameServer.Models
{
    public enum MEMORIAL_COIN_TYPE : uint
    {
        MCT_NORMAL,
        MCT_PREMIUM,
        MCT_SPECIAL,
        MCT_CHARACTER
    }

    public class ctx_coin_item
    {
        public ctx_coin_item(uint _ul = 0u)
        {
            clear();
        }
        public ctx_coin_item(int _tipo,
            uint __typeid,
            uint _qntd)
        {
            this.tipo = _tipo;
            this._typeid = __typeid;
            this.qntd = _qntd;
        }
        public void clear()
        {
            this.tipo = 0;
            this._typeid = 0;
            this.qntd = 0;
        }
        public int tipo = new int();
        public uint _typeid = new uint();
        public uint qntd = new uint();
    }

    public class ctx_coin_item_ex : ctx_coin_item
    {
        public ctx_coin_item_ex(uint _ul = 0) : base(_ul)
        {
            clear();
        }

        public ctx_coin_item_ex(int _tipo,
           uint __typeid,
           uint _qntd,
           uint _probabilidade,
           int _gachar_number) : base(_tipo,
               __typeid, _qntd)
        {
            this.probabilidade = _probabilidade;
            this.gacha_number = _gachar_number;
        }

        public uint probabilidade = new uint();
        public int gacha_number = new int();
    }

    public class ctx_coin
    {
        public ctx_coin(uint _ul = 0u) { }
        public MEMORIAL_COIN_TYPE tipo;
        public uint _typeid = new uint();
        public uint probabilidade = new uint();
        public List<ctx_coin_item_ex> item = new List<ctx_coin_item_ex>();
    }

    public class ctx_memorial_level
    {
        public uint level = new uint(); // Level
        public uint gacha_number = new uint(); // número máximo do gacha
    }

    public class ctx_coin_set_item
    {
        public ctx_coin_set_item(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {

            _typeid = 0;
            tipo = 0;
            flag = -100;

            if (item.Count > 0)
            {
                item.Clear();
            }
        }
        public int flag = new int();
        public uint _typeid = new uint();
        public byte tipo; // Tipo 0 e 1, 1 Premium e 0 todos os outros
        public List<ctx_coin_item_ex> item = new List<ctx_coin_item_ex>();
    }
}

