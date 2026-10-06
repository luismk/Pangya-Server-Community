namespace PangyaAPI.Network.Models;

public class ServerEventFlag
{


    public ServerEventFlag(ushort ul = 0)
    {
        Value = ul;
    }
    public ushort Value { get; set; }

    public bool PangPlus
    {
        get
        {
            return (Value & 2) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 2) : (Value & -3));
        }
    }

    public bool ExperienceDouble
    {
        get
        {
            return (Value & 4) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 4) : (Value & -5));
        }
    }

    public bool ReduceQuitRate
    {
        get
        {
            return (Value & 8) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 8) : (Value & -9));
        }
    }

    public bool ExperiencePlus
    {
        get
        {
            return (Value & 0x10) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x10) : (Value & -17));
        }
    }

    public bool unknown_0
    {
        get
        {
            return (Value & 0x20) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x20) : (Value & -33));
        }
    }

    public bool unknown_1
    {
        get
        {
            return (Value & 0x40) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x40) : (Value & -65));
        }
    }

    public bool Unknown_2
    {
        get
        {
            return (Value & 0x100) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x100) : (Value & -257));
        }
    }


    public bool ClubMasteryPlus
    {
        get
        {
            return (Value & 0x80) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x80) : (Value & -129));
        }
    }

    public bool Unknown_3
    {
        get
        {
            return (Value & 0x200) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x200) : (Value & -513));
        }
    }

    public bool Unknown_4
    {
        get
        {
            return (Value & 0x400) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x400) : (Value & -1025));
        }
    }

    public bool Unknown_5
    {
        get
        {
            return (Value & 0x800) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x800) : (Value & -2049));
        }
    }

    public bool Unknown_6
    {
        get
        {
            return (Value & 0x1000) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x1000) : (Value & -4097));
        }
    }

    public bool Unknown_7
    {
        get
        {
            return (Value & 0x2000) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x2000) : (Value & -8193));
        }
    }

    public bool Unknown_8
    {
        get
        {
            return (Value & 0x4000) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x4000) : (Value & -16385));
        }
    }

    public bool Unknown_9
    {
        get
        {
            return (Value & 0x8000) != 0;
        }
        set
        {
            Value = (ushort)(value ? (Value | 0x8000) : (Value & -32769));
        }
    }
}
