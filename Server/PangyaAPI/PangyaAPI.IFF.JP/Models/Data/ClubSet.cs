using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities.Models;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct ClubSet.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ClubSet : IFFCommon
    {


        public ClubSet()
        {
            Stats = new IFFStats();
            SlotStats = new IFFSlotStats();
            work_shop = new WorkShop();
        }

        public ClubSet(ref PangyaBinaryReader read, uint strLen)
        {
            LoadFile(ref read, strLen);
        }

        public void LoadFile(ref PangyaBinaryReader reader, uint strLen)
        {
            Load(ref reader, strLen);
            Clubs = reader.Read<SubClubs>();
            Stats = reader.Read<IFFStats>();
            SlotStats = reader.Read<IFFSlotStats>();
            work_shop = reader.Read<WorkShop>();

            ulUnknown = reader.ReadUInt32();
            text_pangya = reader.ReadUInt32();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
        public class SubClubs
        {
            public uint Wood { get; set; }
            public uint Iron { get; set; }
            public uint Wedge { get; set; }
            public uint Putter { get; set; }
        }

        [field: MarshalAs(UnmanagedType.Struct)]
        public SubClubs Clubs { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public IFFStats Stats { get; set; }

        [field: MarshalAs(UnmanagedType.Struct)]
        public IFFSlotStats SlotStats { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public class WorkShop
        {
            public int tipo;                           // -1 não pode up rank e nem level, 0 pode tudo
            public uint rank_s_stat;           // para o stat do rank S bonus
            public uint total_recovery;        // recovery points
            public float rate;                         // Rate que vai pegar por hole jogados
            public uint tipo_rank_s;           // power, spin, control end special para EXP
            public uint flag_transformar;      // Que pode Transformar nas taqueiras especiais, pode ser short e aqui em baixo ter outra flag
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public WorkShop work_shop;
        public uint ulUnknown;     // Pode ser do WorkShop, mas ainda não sei 
        public uint text_pangya { get; set; }
    }
    #endregion

}
