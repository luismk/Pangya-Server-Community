namespace Pangya_GameServer.Models;

public class PlayerItemSpecialBoost
{
    public ushort Value { get; set; }
    public byte PangMastery
    {
        get => (byte)(Value & 1);
        set
        {
            if (value != 0)
                Value |= 1;
            else
                Value &= 0xFFFE; // ~(1 << 0)
        }
    }

    public byte PangNitro
    {
        get => (byte)((Value >> 1) & 1);
        set
        {
            if (value != 0)
                Value |= 1 << 1;
            else
                Value &= 0xFFFD; // ~(1 << 1)
        }
    }

    public PlayerItemSpecialBoost()
    {
        Value = 0;
    }
}
