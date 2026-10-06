 namespace Pangya_GameServer.Models;

public class PlayerLobbyInfo
{
    public PlayerLobbyInfo()
    {
        RoomID = -1;
    } 

    public byte[] ToArray()
    {
        using var p = new Packet();

        p.WriteUInt32(UID);
        p.WriteInt32(OID);
        p.WriteInt16(RoomID);
        p.WriteString(NickName, 22);
        p.WriteByte(GameLevel);
        p.WriteInt32(IsGM);
        p.WriteUInt32(TitleSkin);
        p.WriteUInt32(LadderPoints);
        p.WriteByte(State.Value);
        p.WriteInt32(GuildIndex);
        p.WriteUInt32(GuildMarkIndex);
        p.WriteString(GuildMarkImage, 12);
        p.WriteInt16(IsGMVisible);
        p.WriteUInt32(ChannelingFlag);
        p.WriteString(DisplayID, 128);             // S4 TH
        return p.GetBytes;
    }

    public uint UID { get; set; }
    public int OID { get; set; } = -1;
    public short RoomID { get; set; }
    public string NickName { get; set; } = "";
    public byte GameLevel { get; set; } 
    public PlayerCapability Capability { get; set; } = new();
    public uint TitleSkin { get; set; }
    public uint LadderPoints { get; set; }
    public PlayerLobbyStateFlag State { get; set; } = new();
    public int GuildIndex { get; set; }
    public uint GuildMarkIndex { get; set; }
    public string GuildMarkImage { get; set; } = "";
    public short IsGMVisible { get; set; } //th é vip                         
    public uint ChannelingFlag { get; set; }
    public string DisplayID { get; set; } = "";

    private int IsGM => (IsGMVisible == 0) ? 0 : Capability.Value;

}
