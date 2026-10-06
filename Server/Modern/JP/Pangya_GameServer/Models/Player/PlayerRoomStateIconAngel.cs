namespace Pangya_GameServer.Models
{
    public class PlayerRoomStateIconAngel
    { /// <summary> Gets or sets the raw byte bitmask value for room flags. </summary>
        public byte Value { get; set; } = 0;
         
        /// <summary> Flushes the bitmask back to zero. </summary>
        public void Clear() => Value = 0;

        /// <summary> Gets or sets the AngelicWings FlagRoom (Bit 0). </summary>
        public bool AngelicWings
        {
            get => (Value & 0x01) != 0;
            set { if (value) Value |= 0x01; else Value &= unchecked((byte)~0x01); }
        }

        /// <summary> Gets or sets the AngelicWingsEffect FlagRoom (Bit 1). </summary>
        public bool AngelicWingsEffect
        {
            get => ((Value >> 1) & 0x01) != 0;
            set { if (value) Value |= (1 << 1); else Value &= unchecked((byte)~(1 << 1)); }
        }

        /// <summary> Gets or sets the WingsReserved field (Bits 2-7, 6 bits total). </summary>
        public byte WingsReserved
        {
            get => (byte)((Value >> 2) & 0x3F);
            set => Value = (byte)((Value & ~(0x3F << 2)) | ((value & 0x3F) << 2));
        }
    }
}