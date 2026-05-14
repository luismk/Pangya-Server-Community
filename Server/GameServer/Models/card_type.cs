using System.Collections.Generic;
using System.Runtime.InteropServices;
using PangyaAPI.Network.PangyaPacket;

namespace Pangya_GameServer.Models
{
    public enum CARD_TYPE : uint
    {
        T_NORMAL,
        T_RARE,
        T_SUPER_RARE,
        T_SECRET
    }

    public class Card
    {
        public void clear()
        {
        }
        public uint _typeid = new uint();
        public uint prob = new uint(); // Probabilidade
        public CARD_TYPE tipo; // tipo, Normal, Rare, Super Rare, Secreto
    }

    public class CardPack
    {
        public CardPack(uint _ul = 0u)
        {
            clear();
        }
        public CardPack(uint __typeid,
            uint _num, byte _volume)
        {
            this._typeid = __typeid;
            this.num = _num;
            this.volume = _volume;
        }
        public void clear()
        {
            rate = new Rate();
            card = new List<Card>();
            if (card.Count > 0)
            {
                card.Clear();
            }

            _typeid = 0;
            num = 0;
            volume = 0;
        }
        public class Rate
        {
            public void clear()
            {
                value = new ushort[4];
            }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] value = new ushort[4]; // Normal, Rare, Super Rare, Secret
        }
        public uint _typeid = new uint();
        public uint num = new uint(); // Número de card(s) que esse pack dá
        public byte volume; // Volume do Card Pack, Vol 1, 2, 3, 4, 5 etc
        public Rate rate = new Rate(); // Rate, N, R, SR, SC
        public List<Card> card = new List<Card>(); // Cards
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LoloCardCompose
    {
        public void clear()
        {
            _typeid = new uint[3];
        }
        public LoloCardCompose()
        {
            clear();
        }
        public ulong pang = new ulong();
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] _typeid = new uint[3];

        public LoloCardCompose ToRead(packet r)
        {
            pang = r.ReadUInt64();

            _typeid = new uint[3];
            for (int i = 0; i < 3; i++)
                _typeid[i] = r.ReadUInt32();

            return this;
        }

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LoloCardComposeEx : LoloCardCompose
    {
        public byte tipo = 0;
    }

}
