
namespace Pangya_GameServer.Models;

// Player Papel Shop Info 
public class PlayerPapelShopInfo
{
    public ushort RemainCount { get; set; }
    public ushort CurrentCount { get; set; }
    public ushort LimitCount { get; set; }
    public PlayerPapelShopInfo()
    {
        RemainCount = ushort.MaxValue;
        CurrentCount = ushort.MaxValue;
        LimitCount = ushort.MaxValue;
    }

    public byte[] ToArray()
    {
        using var p = new Packet();
        p.WriteUInt16(RemainCount);
        p.WriteUInt16(CurrentCount);
        p.WriteUInt16(LimitCount);
        return p.GetBytes;
    }
}
