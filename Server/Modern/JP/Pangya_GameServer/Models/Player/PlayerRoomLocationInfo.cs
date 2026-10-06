namespace Pangya_GameServer.Models;

public class PlayerRoomLocationInfo
{ 
    public static PlayerRoomLocationInfo operator +(PlayerRoomLocationInfo a, PlayerRoomLocationInfo _add_location)
    {
        return new PlayerRoomLocationInfo()
        {
            X = a.X += _add_location.X,
            Z = a.Z += _add_location.Z,
            Y = a.Y += _add_location.Y
        };
    }
    public static PlayerRoomLocationInfo operator -(PlayerRoomLocationInfo a, PlayerRoomLocationInfo _add_location)
    {
        return new PlayerRoomLocationInfo()
        {
            X = a.X -= _add_location.X,
            Z = a.Z -= _add_location.Z,
            Y = a.Y -= _add_location.Y
        };
    }

    public float X { get; set; }
    public float Z { get; set; }
    public float Y { get; set; }

    public byte[] ToArray()
    {
        using var p = new Packet();
        p.WriteFloat(X);
        p.WriteFloat(Y);
        p.WriteFloat(Z);
        return p.GetBytes;
    }

    public PlayerRoomLocationInfo ToRead(Packet _r)
    {
        X = _r.ReadFloat();
        Y = _r.ReadFloat();
        Z = _r.ReadFloat();
        return this;
    }
}