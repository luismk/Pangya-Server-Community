using System.Collections.Generic;

namespace Pangya_GameServer.Models
{
    public enum BOX_TYPE_RARETY : byte
    {
        R_NORMAL,
        R_RARE,
        R_SUPER_RARE
    }

    public enum BOX_TYPE_OPEN : byte
    {
        O_SEND_MAIL,
        O_SEND_MYROOM
    }

    public enum BOX_TYPE : byte
    {
        NORMAL,
        ALL_RARE_OR_LUCKY_REWARD
    }

    public class ctx_box_item
    {
        public void clear()
        {
        }
        public uint _typeid = new uint();
        public int numero = new int();
        public int qntd = new int();
        public uint probabilidade = new uint();
        public BOX_TYPE_RARETY raridade;
        public byte duplicar = 1; // 0 ou 1, 1 pode duplicar item
        public byte active = 1; // 0 ou 1
    }

    public class ctx_box
    {
        public ctx_box(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
        }
        public BOX_TYPE_OPEN tipo_open;
        public BOX_TYPE tipo;
        public int numero = new int();
        public int id = new int();
        public uint _typeid = new uint();
        public uint opened_typeid = new uint(); // Typeid da Box aberta se tiver
        public string msg = ""; // Msg da box
        public List<ctx_box_item> item = new List<ctx_box_item>(); // Todos os itens da box
    }
}
