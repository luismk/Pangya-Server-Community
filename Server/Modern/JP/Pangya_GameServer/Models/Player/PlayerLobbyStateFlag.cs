 namespace Pangya_GameServer.Models;

public class PlayerLobbyStateFlag
{
    public byte Value;
    public PlayerLobbyStateFlag()
    {
        Value = 0;
    } 
    // Bit 0 - AFK
    public byte Sleep
    {
        get => (byte)((Value >> 0) & 1);
        set => Value = (byte)((Value & ~(1 << 0)) | ((value & 1) << 0));
    }

    // Bit 1 - Gênero
    public byte Gender
    {
        get => (byte)((Value >> 1) & 1);
        set => Value = (byte)((Value & ~(1 << 1)) | ((value & 1) << 1));
    }

    // Bit 2 - Quit Rate > 31% e < 41%
    public byte QuiterLow
    {
        get => (byte)((Value >> 2) & 1);
        set => Value = (byte)((Value & ~(1 << 2)) | ((value & 1) << 2));
    }

    // Bit 3 - Quit Rate > 41%
    public byte QuiterHight
    {
        get => (byte)((Value >> 3) & 1);
        set => Value = (byte)((Value & ~(1 << 3)) | ((value & 1) << 3));
    }

    // Bit 4 - Quit Rate < 3% (Azinha)
    public byte NoQuiterWings
    {
        get => (byte)((Value >> 4) & 1);
        set => Value = (byte)((Value & ~(1 << 4)) | ((value & 1) << 4));
    }

    // Bit 5 - Angel Wings
    public byte AngelWings
    {
        get => (byte)((Value >> 5) & 1);
        set => Value = (byte)((Value & ~(1 << 5)) | ((value & 1) << 5));
    }

    // Bit 6 - Unknown
    public byte ucUnknown_bit7
    {
        get => (byte)((Value >> 6) & 1);
        set => Value = (byte)((Value & ~(1 << 6)) | ((value & 1) << 6));
    }

    // Bit 7 - Unknown
    public byte ucUnknown_bit8
    {
        get => (byte)((Value >> 7) & 1);
        set => Value = (byte)((Value & ~(1 << 7)) | ((value & 1) << 7));
    }
}