namespace PangyaAPI.Network.Models;

public class ServerInfo
{
    public string Name { get; set; } = "";
    public int UID { get; set; } 
    public int MaxUsers { get; set; } 
    public int CurrentUsers { get; set; } 
    public string IpAddress { get; set; } = ""; 
    public int Port { get; set; } 
    public int AngelicWingsCount { get; set; } 
    public short MapEvent { get; set; } 
    public short AppRate { get; set; } 
    public short ScratchRate { get; set; } 
    public short ServerIcon { get; set; }  
    public ServerType Type { get; set; }
    public string BuildVersion { get; set; } = "";
    public string ClientVersion { get; set; } = "";
    public uint VersionPacket { get; set; }
    public ServerProperty Property { get; set; } = new();
    public ServerEventFlag EventFlag { get; set; } = new();
    public ServerRateInfo Rate { get; set; } = new();
    public ServerFlag Flag { get; set; } = new();

    public ServerInfo()
    {
        Property = new ServerProperty(); 
        EventFlag = new ServerEventFlag(0);
        Rate = new ServerRateInfo();
        Flag = new ServerFlag(); 
    }

    public byte[] ToArray()
    {
        using Packet p = new();
        p.WriteString(Name, 28);
        p.WriteInt32(983);
        p.WriteZero(8);
        p.WriteInt32(UID);
        p.WriteInt32(MaxUsers);
        p.WriteInt32(CurrentUsers);
        p.WriteString(IpAddress, 18);
        p.WriteInt32(Port);
        p.WriteUInt32(Property.Value);
        p.WriteInt32(AngelicWingsCount);
        p.WriteUInt16(EventFlag.Value);
        p.WriteInt16(MapEvent);
        p.WriteInt16(AppRate);
        p.WriteInt16(ScratchRate);
        p.WriteInt16(ServerIcon);
        return p.GetBytes;
    }

    public ServerInfo ToRead(Packet p)
    {
        Name = p.ReadString(40);
        UID = p.ReadInt32();
        MaxUsers = p.ReadInt32();
        CurrentUsers = p.ReadInt32();
        IpAddress = p.ReadString(18);
        Port = p.ReadInt32();
        Property.Value = p.ReadUInt32();
        AngelicWingsCount = p.ReadInt32();
        EventFlag.Value = p.ReadUInt16();
        MapEvent = p.ReadInt16();
        AppRate = p.ReadInt16();
        ScratchRate = p.ReadInt16();
        ServerIcon = p.ReadInt16();
        return this;
    }
}
