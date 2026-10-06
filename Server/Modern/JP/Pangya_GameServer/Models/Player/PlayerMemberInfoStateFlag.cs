
namespace Pangya_GameServer.Models;

public class PlayerMemberInfoStateFlag
{
    public byte Value { get; set; }

    // Bit 0 (0x01) - Valor: 1 ou 0
    public byte Channel
    {
        get => (byte)(Value & 0x01);
        set => Value = (byte)((value != 0) ? (Value | 0x01) : (Value & ~0x01));
    }

    // Bit 1 (0x02) - Valor: 2 ou 0
    public byte Visible
    {
        get => (byte)(Value & 0x02);
        set => Value = (byte)((value != 0) ? (Value | 0x02) : (Value & ~0x02));
    }

    // Bit 2 (0x04) - Valor: 4 ou 0
    public byte Whisper
    {
        get => (byte)(Value & 0x04);
        set => Value = (byte)((value != 0) ? (Value | 0x04) : (Value & ~0x04));
    }

    // Bit 3 (0x08) - Valor: 8 ou 0
    public byte Gender
    {
        get => (byte)(Value & 0x08);
        set => Value = (byte)((value != 0) ? (Value | 0x08) : (Value & ~0x08));
    }

    // Bit 4 (0x10) - Valor: 16 ou 0
    public byte Wings
    {
        get => (byte)(Value & 0x10);
        set => Value = (byte)((value != 0) ? (Value | 0x10) : (Value & ~0x10));
    }

    // Bit 5 (0x20) - Valor: 32 ou 0
    public byte AngelWings
    {
        get => (byte)(Value & 0x20);
        set => Value = (byte)((value != 0) ? (Value | 0x20) : (Value & ~0x20));
    }

    // Bit 6 (0x40) - Valor: 64 ou 0
    public byte Quit10Porcent
    {
        get => (byte)(Value & 0x40);
        set => Value = (byte)((value != 0) ? (Value | 0x40) : (Value & ~0x40));
    }

    // Bit 7 (0x80) - Valor: 128 ou 0
    public byte Quit20Porcent
    {
        get => (byte)(Value & 0x80);
        set => Value = (byte)((value != 0) ? (Value | 0x80) : (Value & ~0x80));
    }
     
    /// <summary>
    /// Retorna uma representação visual dos bits para Debug
    /// </summary>
    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"--- MemberInfoStateFlag (Raw: 0x{Value:X2}) ---");
        sb.AppendLine($"Binary:   {Convert.ToString(Value, 2).PadLeft(8, '0')}");
        sb.AppendLine($"Channel:  {(Channel != 0 ? "ON" : "OFF")} (1)");
        sb.AppendLine($"Visible:  {(Visible != 0 ? "ON" : "OFF")} (2)");
        sb.AppendLine($"Whisper:  {(Whisper != 0 ? "ON" : "OFF")} (4)");
        sb.AppendLine($"Sexo:     {(Gender != 0 ? "ON" : "OFF")} (8)");
        sb.AppendLine($"Azinha:   {(Wings != 0 ? "ON" : "OFF")} (16)");
        sb.AppendLine($"Angel:    {(AngelWings != 0 ? "ON" : "OFF")} (32)");
        sb.AppendLine($"Quiter 1: {(Quit10Porcent != 0 ? "ON" : "OFF")} (64)");
        sb.AppendLine($"Quiter 2: {(Quit20Porcent != 0 ? "ON" : "OFF")} (128)");
        sb.Append("------------------------------------");

        return sb.ToString();
    }
}
