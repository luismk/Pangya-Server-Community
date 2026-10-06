namespace Pangya_GameServer.Models;

public class PlayerRoomInfoAction
{
    public uint Animation { get; set; }//animate
    public short SubRoomID { get; set; }//sub Login, talvez seja da loja....
    public uint Posture { get; set; }//Posture	// Acho que seja estado de "lugar" pelo que lembro

    public byte[] ToArray()
    {
        using Packet p = new();
        p.WriteUInt32(Animation);
        p.WriteInt16(SubRoomID);
        p.WriteUInt32(Posture);
        return p.GetBytes;
    }
}
