/// create and converted by LUIS MK

namespace Pangya_GameServer.Models
{
    public class SpecialModeFlag
    {
        public SpecialModeFlag(uint _ul = 0)
        {
            Value = _ul;
        }

        public SpecialModeFlag()
        {
            Value = 0;
        }

        public uint Value { get; set; }

        public bool IsNaturalMode
        {
            get => (Value & 0x1u) == 1;
            set => Value = (Value & ~0x1u) | (value ? 1u : 0u);
        }

        public bool IsShotMode
        {
            get => ((Value >> 1) & 0x1) == 1;
            set => Value = (Value & ~0x2u) | ((value ? 1u : 0u) << 1);
        }

        public override string ToString()
        {
            return $"NaturalAndShortGame {{ Value = {Value}, NaturalMode = {IsNaturalMode}, ShortGame = {IsShotMode} }}";
        }
    }
}
