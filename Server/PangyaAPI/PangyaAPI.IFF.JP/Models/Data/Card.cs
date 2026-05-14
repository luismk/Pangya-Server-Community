using System;
using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct Card.iff
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Card : IFFCommon
    {
        public byte Rarity { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        public string MPet { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] c = new ushort[5];
        public ushort Effect { get; set; }
        public UInt16 EffectValue { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string AdditionalTexture1 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string AdditionalTexture2 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string AdditionalTexture3 { get; set; }
        public ushort EffectTime { get; set; }
        public ushort Volumn { get; set; }
        public UInt32 Position { get; set; }
        public UInt32 flag1 { get; set; }     // !@flag que guarda alguns valores de de N, R, SR, SC e etc
        public UInt32 flag2 { get; set; }       // flag que guarda alguns valores de de N, R, SR, SC e etc
        public Card()
        { }

        public Card(PangyaBinaryReader reader)
        {
            Load(ref reader, 40);
            Rarity = reader.ReadByte();
            MPet = reader.ReadPStr(40);
            // Stats = reader.Read<IFFStats>();
            Effect = reader.ReadUInt16();
            EffectValue = reader.ReadUInt16();
            AdditionalTexture1 = reader.ReadPStr(40);
            AdditionalTexture2 = reader.ReadPStr(40);
            AdditionalTexture3 = reader.ReadPStr(40);
            EffectTime = reader.ReadUInt16();
            Volumn = reader.ReadUInt16();
            Position = reader.ReadUInt32();
            flag1 = reader.ReadUInt32();
            flag2 = reader.ReadUInt32();
        }

        public string GetTypeEffect()
        {
            System.Diagnostics.Debug.WriteLine($"MPet = {MPet}, Name = {Name}, Effect = {Effect}, Rare = {Rarity}");
            switch (Effect)
            {
                case 0:
                    {
                        return "None Effect";
                    }
                case 1:
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return "Inc. Pang (R)";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return "Inc. Pang (N)";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return "% Item Sucess(Special)";
                    }
                    if (Rarity == 2 && flag1 == 10 && flag2 == 1)
                    {
                        return "-" + EffectValue + " Yard";
                    }
                    if (Rarity == 2 && flag1 == 3 && flag2 == 1)
                    {
                        return "Inst. EXP (" + EffectValue + ")";
                    }
                    if (Rarity == 3 && flag1 == 3 && flag2 == 0)
                    {
                        return "Temp. Card EXP";
                    }
                    if (Rarity == 3 && flag1 == 14 && flag2 == 1)
                    {
                        return "-" + EffectValue + " Yard";
                    }
                    if (Rarity == 2 && flag1 == 10 && flag2 == 1)
                    {
                        return "-" + EffectValue + " Yard";
                    }

                    if (Rarity == 3 && flag1 == 14 && flag2 == 1)
                    {
                        return "-" + EffectValue + " Yard";
                    }
                    if (Rarity == 2 && flag1 == 8 && flag2 == 1)
                    {
                        return "% Item Sucess(High)";
                    }
                    if (Rarity == 1 && flag1 == 6 && flag2 == 1)
                    {
                        return "% Item Sucess(Medium)";
                    }

                    if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                    {
                        return "% Item Sucess(Low)";
                    }
                    return "Unknown Effect Name";
                case 2:
                    if (Rarity == 2 && flag1 == 3 && flag2 == 0)
                    {
                        return "Temp. " + EffectValue + "% Pangs";
                    }
                    if (Rarity == 1 && flag1 == 3 && flag2 == 0)
                    {
                        return "Temp. " + EffectValue + "% Pang";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return "Temp. " + EffectValue + "% Pang";
                    }
                    if (Rarity == 0 && flag1 == 2 && flag2 == 1)
                    {
                        return "Yard +" + EffectValue;
                    }
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return "Inc. EXP (R)";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        if (EffectTime > 0)
                        {
                            return "Temp. Inc. Pang (N)";
                        }
                        return "Inc. EXP (R)";
                    }
                    if (Rarity == 2 && flag1 == 11 && flag2 == 1)
                    {
                        return "Inc. EXP (SC)";
                    }
                    if (Rarity == 2 && flag1 == 10 && flag2 == 1)
                    {
                        return "Inc. EXP (SR)";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return "Inc. EXP (SC)";
                    }
                    return "Unknown Effect Name";
                case 3:
                    if (Rarity == 1 && flag1 == 7 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return "Temp. EXP";
                    }
                    if (Rarity == 1 && flag1 == 3 && flag2 == 0)
                    {
                        return "Temp. EXP";
                    }
                    if (Rarity == 2 && flag1 == 3 && flag2 == 0)
                    {
                        return "Temp. EXP";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return "Inc. EXP (N)";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    return "Unknown Effect Name";
                case 4:
                    if (Rarity == 1 && flag1 == 7 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 10 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 3 && flag2 == 0)
                    {
                        return $"Inst. Pangs ({EffectValue})";
                    }
                    if (Rarity == 1 && flag1 == 3 && flag2 == 0)
                    {
                        return $"Inst. Pangs ({EffectValue})";
                    }
                    if (Rarity == 0 && flag1 == 1 && flag2 == 0)
                    {
                        return $"Inst. Pangs ({EffectValue})";
                    }
                    return "Unknown Effect Name";
                case 5:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Temp. Power +" + EffectValue;
                        }
                        if (Rarity == 0 && flag1 == 7 && flag2 == 0)
                        {
                            return "Control +" + EffectValue;
                        }
                        if (Rarity == 1 && flag1 == 7 && flag2 == 0)
                        {
                            return "Yard +" + EffectValue;
                        }
                        if (Rarity == 2 && flag1 == 10 && flag2 == 1)
                        {
                            return "Yard +" + EffectValue;
                        }
                        if (Rarity == 3 && flag1 == 0 && flag2 == 1)
                        {
                            return "Yard +" + EffectValue;
                        }
                        return "Unknown Effect Name";
                    }
                case 6:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Control +" + EffectValue;
                        }
                        if (Rarity == 1 && flag1 == 10 && flag2 == 0)
                        {
                            return "Temp. Control +" + EffectValue;
                        }
                        if (Rarity == 2 && flag1 == 3 && flag2 == 0)
                        {
                            return "Temp. Control +" + EffectValue;
                        }
                        return "Unknown Effect Name";
                    }
                case 7:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Temp. Impact  +" + EffectValue;
                        }
                        if (Rarity == 2 && flag1 == 12 && flag2 == 0)
                        {
                            return "Control +" + EffectValue;
                        }
                        if (Rarity == 72 && flag1 == 15 && flag2 == 0)
                        {
                            return "Zone Impact +" + EffectValue + " Pixel";
                        }
                        return "Unknown Effect Name";
                    }
                case 8:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Spin +" + EffectValue;
                        }
                        if (Rarity == 3 && flag1 == 15 && flag2 == 0)
                        {
                            return "Control +" + EffectValue;
                        }
                        return "Unknown Effect Name";
                    }
                case 9:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Temp. Curve  +" + EffectValue;
                        }
                        return "Unknown Effect Name";
                    }
                case 10:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Combo Gauge";
                        }
                        if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Combo Gauge";
                        }
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Initial Power Gauge";
                        }
                        return "Unknown Effect Name";
                    }
                case 11:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Item Slot  +" + EffectValue;
                        }
                        if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                        {
                            return EffectValue + "% Pang Bonus";
                        }
                        return "Zone Impact Medium " + EffectValue + " Pixel";
                    }
                case 12:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Zone Impact " + EffectValue + " Pixel";
                        }
                        if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                        {
                            return EffectValue + "% Pang Bonus";
                        }
                        if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. Zone Impact " + EffectValue + " Pixel";
                        }
                        return "Zone Impact High";
                    }
                case 17:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Inst. Pangs (" + EffectValue + ")";
                        }
                        if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                        {
                            return EffectValue + "% Pang Bonus";
                        }
                        return "Zone Impact High";
                    }
                case 18:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Tempory Treasure Point";
                        }
                        if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                        {
                            return EffectValue + "% Pang Bonus";
                        }
                        return "Zone Impact High";
                    }
                case 34:
                    if (Rarity == 1 && flag1 == 5 && flag2 == 1)
                    {
                        return EffectValue + "% Club Mastery";
                    }
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return "50% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    return "Unknown Effect Name";
                case 28:
                    if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                    {
                        return "Temp. Combo Gauge";
                    }
                    if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                    {
                        return "Temp. Combo Gauge";
                    }
                    if (Rarity == 1 && flag1 == 3 && flag2 == 0)
                    {
                        return "Combo Gauge Bonus (R)";
                    }
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    return "Unknown Effect Name";
                case 27:
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return "Combo Gauge Bonus (N)";
                    }
                    if (Rarity == 1 && flag1 == 7 && flag2 == 1)
                    {
                        return "50% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 0 && flag1 == 3 && flag2 == 0)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 3 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 2 && flag1 == 11 && flag2 == 1)
                    {
                        return EffectValue + "% Pang Bonus";
                    }
                    if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                    {
                        return "Temp. Yard +" + EffectValue;
                    }
                    return "Unknown Effect Name";
                case 20: return "Blue Lagoon [Bonus]";
                case 21: return "Blue Walter [Bonus]";
                case 22: return "Shining Sand [Bonus]";
                case 23: return "Deep Inferno [Bonus]";
                case 24: return "Silva Cannon [Bonus]";
                case 25:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Easten Valley [Bonus]";
                        }
                        return "Unknown Effect Name";
                    }
                case 26:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Lost Seaway [Bonus]";
                        }
                        return "Unknown Effect Name";
                    }
                case 29:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Ice Inferno [Bonus]";
                        }
                        return "Unknown Effect Name";
                    }
                case 30:
                    {
                        if (Rarity == 0 && flag1 == 1 && flag2 == 1)
                        {
                            return "Wiz City [Bonus]";
                        }
                        return "Unknown Effect Name";
                    }
                case 19:
                    {
                        if (Rarity == 1 && flag1 == 3 && flag2 == 1)
                        {
                            return "Temp. " + EffectValue + "% Obtian Rain";
                        }
                        return "Zone Impact Medium " + EffectValue + " Pixel";
                    }
                case 31:
                    {
                        if (Rarity == 1 && flag1 == 5 && flag2 == 1)
                        {
                            return EffectValue + "% Obtian Rain";
                        }
                        return "Zone Impact Medium " + EffectValue + " Pixel";
                    }
                case 32:
                    {
                        if (Rarity == 1 && flag1 == 5 && flag2 == 1)
                        {
                            return "Mulligan Rose Effect";
                        }
                        return "Zone Impact Medium " + EffectValue + " Pixel";
                    }
                default:
                    break;
            }
            if (Rarity == 2 && flag1 == 2 && flag2 == 1)
            {
                return " -2 Yard [Penality]";
            }
            if (Rarity == 3 && flag1 == 3 && flag2 == 1)
            {
                return " -1 Yard [Penality]";
            }
            if (Rarity == 0 && flag1 == 2 && flag2 == 1)
            {
                return "Control +" + EffectValue;
            }
            if (Rarity == 0 && flag1 == 2 && flag2 == 1)
            {
                return "Control +" + EffectValue;
            }
            if (Rarity == 1 && flag1 == 2 && flag2 == 1)
            {
                return "Control +" + EffectValue;
            }

            return "Unknown Effect Name";
        }
    }
    #endregion
}
