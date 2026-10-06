using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.Network.Models
{
    public class ServerProperty
    { 
        public uint Value { get; set; }

        public bool Normal => Value == 0;

        public bool Special
        {
            get
            {
                return (Value & 1) != 0;
            }
            set
            {
                Value = (value ? (Value | 1) : (Value & 0xFFFFFFFEu));
            }
        }

        public bool SmallPlay
        {
            get
            {
                return (Value & 2) != 0;
            }
            set
            {
                Value = (value ? (Value | 2) : (Value & 0xFFFFFFFDu));
            }
        }

        public bool Ladder
        {
            get
            {
                return (Value & 4) != 0;
            }
            set
            {
                Value = (value ? (Value | 4) : (Value & 0xFFFFFFFBu));
            }
        }

        public bool Adult
        {
            get
            {
                return (Value & 8) != 0;
            }
            set
            {
                Value = (value ? (Value | 8) : (Value & 0xFFFFFFF7u));
            }
        }

        public bool Mantle
        {
            get
            {
                return (Value & 0x10) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x10) : (Value & 0xFFFFFFEFu));
            }
        }

        public bool Skins
        {
            get
            {
                return (Value & 0x20) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x20) : (Value & 0xFFFFFFDFu));
            }
        }

        public bool OnlyRookies
        {
            get
            {
                return (Value & 0x40) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x40) : (Value & 0xFFFFFFBFu));
            }
        }

        public bool NaturalMode
        {
            get
            {
                return (Value & 0x80) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x80) : (Value & 0xFFFFFF7Fu));
            }
        }

        public bool ChampionShip
        {
            get
            {
                return (Value & 0x100) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x100) : (Value & 0xFFFFFEFFu));
            }
        }

        public bool Blue
        {
            get
            {
                return (Value & 0x200) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x200) : (Value & 0xFFFFFDFFu));
            }
        }

        public bool Green
        {
            get
            {
                return (Value & 0x400) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x400) : (Value & 0xFFFFFBFFu));
            }
        }

        public bool GrandPrixMode
        {
            get
            {
                return (Value & 0x800) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x800) : (Value & 0xFFFFF7FFu));
            }
        }

        public bool Relay
        {
            get
            {
                return (Value & 0x1000) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x1000) : (Value & 0xFFFFEFFFu));
            }
        }

        public bool OnlyRookieBeginner
        {
            get
            {
                return (Value & 0x80000000u) != 0;
            }
            set
            {
                Value = (value ? (Value | 0x80000000u) : (Value & 0x7FFFFFFF));
            }
        }

        public ServerProperty(uint _ul = 0)
        {
            Value = _ul;
        }
    }

}
