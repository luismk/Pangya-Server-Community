using System;
using System.Collections.Generic;
using System.Text;

namespace PangyaAPI.Network.Models
{

    public class PlayerStateBlockFlag
    { 
        public int TimeBlock;

        public ulong Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }

        public bool BlockByTime
        {
            get
            {
                return (Value & 1) == 1;
            }
            set
            {
                Value = (value ? (Value | 1) : (Value & 0xFFFFFFFFFFFFFFFEuL));
            }
        }

        public bool BlockForever
        {
            get
            {
                return (Value & 2) == 2;
            }
            set
            {
                Value = (value ? (Value | 2) : (Value & 0xFFFFFFFFFFFFFFFDuL));
            }
        }

        public bool BlockInLounger
        {
            get
            {
                return (Value & 4) == 4;
            }
            set
            {
                Value = (value ? (Value | 4) : (Value & 0xFFFFFFFFFFFFFFFBuL));
            }
        }

        public bool BlockInShopLounger
        {
            get
            {
                return (Value & 8) == 8;
            }
            set
            {
                Value = (value ? (Value | 8) : (Value & 0xFFFFFFFFFFFFFFF7uL));
            }
        }

        public bool BlockInGiftShop
        {
            get
            {
                return (Value & 0x10) == 16;
            }
            set
            {
                Value = (value ? (Value | 0x10) : (Value & 0xFFFFFFFFFFFFFFEFuL));
            }
        }

        public bool BlockInPapelShop
        {
            get
            {
                return (Value & 0x20) == 32;
            }
            set
            {
                Value = (value ? (Value | 0x20) : (Value & 0xFFFFFFFFFFFFFFDFuL));
            }
        }

        public bool BlockInScratchy
        {
            get
            {
                return (Value & 0x40) == 64;
            }
            set
            {
                Value = (value ? (Value | 0x40) : (Value & 0xFFFFFFFFFFFFFFBFuL));
            }
        }

        public bool BlockInNoticeTicker
        {
            get
            {
                return (Value & 0x80) == 128;
            }
            set
            {
                Value = (value ? (Value | 0x80) : (Value & 0xFFFFFFFFFFFFFF7FuL));
            }
        }

        public bool BlockInMemorialShop
        {
            get
            {
                return (Value & 0x100) == 256;
            }
            set
            {
                Value = (value ? (Value | 0x100) : (Value & 0xFFFFFFFFFFFFFEFFuL));
            }
        }

        public bool BlockInIPAll
        {
            get
            {
                return (Value & 0x200) == 512;
            }
            set
            {
                Value = (value ? (Value | 0x200) : (Value & 0xFFFFFFFFFFFFFDFFuL));
            }
        }

        public bool BlockInAddressMac
        {
            get
            {
                return (Value & 0x400) == 1024;
            }
            set
            {
                Value = (value ? (Value | 0x400) : (Value & 0xFFFFFFFFFFFFFBFFuL));
            }
        }

        public PlayerStateBlockFlag(ulong _ul = 0)
        {
            Value = _ul;
        }
    }

}
