using System;

namespace Pangya_GameServer.PangyaEnums
{
    [Flags]
    public enum DisplayStateFlags : uint
    {
        OverDrive = 1 << 0,
        Bit2_Unknown = 1 << 1,
        SuperPangya = 1 << 2,
        SpecialShot = 1 << 3,
        BeamImpact = 1 << 4,
        ChipIn17To199 = 1 << 5,
        ChipIn200Plus = 1 << 6,
        LongPutt = 1 << 7,
        AcertoHole = 1 << 8,
        ApproachShot = 1 << 9,
        ChipInWithSpecialShot = 1 << 10,
        Bit12_Unknown = 1 << 11,
        HappyBonus = 1 << 12,
        ClearBonus = 1 << 13,
        AztecBonus = 1 << 14,
        RecoveryBonus = 1 << 15,
        ChipInWithoutSpecialShot = 1 << 16,
        BoundBonus = 1 << 17,
        Bit19_Unknown = 1 << 18,
        Bit20_Unknown = 1 << 19,
        MascotBonusWithPangya = 1 << 20,
        MascotBonusWithoutPangya = 1 << 21,
        SpecialBonusWithPangya = 1 << 22,
        SpecialBonusWithoutPangya = 1 << 23,
        Bit25_Unknown = 1 << 24,
        Bit26_Unknown = 1 << 25,
        DevilBonus = 1 << 26,
        Bit28to32_UnknownMask = 0xF8000000 // bits 27–31
    }
}
