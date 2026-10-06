namespace Pangya_GameServer.Models;

public class PlayerRoomInfoShop
{
    public uint State { get; set; }
    public string Name { get; set; }
    public PlayerRoomInfoShop()
    {
        State = 0;
        Name = "";
    }

    public byte[] ToArray()
    {
        using Packet p = new();
        p.WriteUInt32(State);
        p.WriteString(Name, 64);
        return p.GetBytes;
    }   
}
