using System.Runtime.InteropServices;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct CutinInformation.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class CutinInformation
    {
        public uint active;
        public uint _typeid;         // NormalTypeid
        public uint rare_typeid;    // RareTypeid
        public uint rarity;         // Rarity
        [MarshalAs(UnmanagedType.Struct)]
        public uCondition tipo;     // Condition: 1 = PS1, 2 = PS2, 4 = Erro Pangya

        public uint sector;         // Img_Pos
        public uint character_id;   // CharIndex

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Img[] img;           // Char, Bg, Pattern, Text

        public uint tempo;          // Out_Ani

        public void Clear()
        {
            active = 0;
            _typeid = 0;
            rare_typeid = 0;
            rarity = 0;
            tipo.Clear();
            sector = 0;
            character_id = 0;

            if (img == null || img.Length != 4)
                img = new Img[4];

            for (int i = 0; i < img.Length; i++)
                img[i].Clear();

            tempo = 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct uCondition
        {
            [FieldOffset(0)]
            public uint ulCondition;

            [FieldOffset(0)]
            private BitField bits;

            public uCondition(uint ul = 0)
            {
                bits = new BitField();
                ulCondition = ul;
            }

            public void Clear() => ulCondition = 0;

            [StructLayout(LayoutKind.Sequential)]
            private struct BitField
            {
                public uint flags;

                public bool power_shot
                {
                    get => (flags & (1 << 0)) != 0;
                    set => flags = value ? flags | (1u << 0) : flags & ~(1u << 0);
                }

                public bool double_power_short
                {
                    get => (flags & (1 << 1)) != 0;
                    set => flags = value ? flags | (1u << 1) : flags & ~(1u << 1);
                }

                public bool power_shot_failed
                {
                    get => (flags & (1 << 2)) != 0;
                    set => flags = value ? flags | (1u << 2) : flags & ~(1u << 2);
                }

                public bool chipin
                {
                    get => (flags & (1 << 3)) != 0;
                    set => flags = value ? flags | (1u << 3) : flags & ~(1u << 3);
                }
            }

            public bool power_shot
            {
                get => (ulCondition & (1u << 0)) != 0;
                set => ulCondition = value ? ulCondition | (1u << 0) : ulCondition & ~(1u << 0);
            }

            public bool double_power_short
            {
                get => (ulCondition & (1u << 1)) != 0;
                set => ulCondition = value ? ulCondition | (1u << 1) : ulCondition & ~(1u << 1);
            }

            public bool power_shot_failed
            {
                get => (ulCondition & (1u << 2)) != 0;
                set => ulCondition = value ? ulCondition | (1u << 2) : ulCondition & ~(1u << 2);
            }

            public bool chipin
            {
                get => (ulCondition & (1u << 3)) != 0;
                set => ulCondition = value ? ulCondition | (1u << 3) : ulCondition & ~(1u << 3);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Img
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string sprite; // char[40]

            public uint tipo; // Ani

            public void Clear()
            {
                sprite = "";
                tipo = 0;
            }
        }
    }
    #endregion
}
