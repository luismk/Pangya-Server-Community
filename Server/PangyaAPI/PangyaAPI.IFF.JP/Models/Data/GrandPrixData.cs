using System;
using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct GrandPrixData.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class GrandPrixData
    {
        public enum GP_ABA : byte
        {
            UNKNOWN = byte.MaxValue,
            ROOKIE = 0,
            BEGINNER = 1,
            JUNIOR = 2,
            EVENT = 3
        }
        [field: MarshalAs(UnmanagedType.Bool, SizeConst = 4)]
        public bool Active { get; set; } = true;//0 start position
        public uint ID { get; set; }
        public uint TypeID_Link { get; set; }
        public uint TypeGP { get; set; }
        public ushort TimeHole { get; set; }
        [field: MarshalAs(unmanagedType: UnmanagedType.ByValArray, SizeConst = 66)]//is 64, 2 short unknown
        public byte[] NameInBytes { get; set; }
        public string Name { get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", ""); set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(66, '\0')); }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public Ticket ticket { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        public string Event_Icon { get; set; }//[39 + 1];
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 3)]
        public Flag flag { get; set; }
        public uint rule { get; set; }

        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 9)]
        public CourseInfo course_info { get; set; }
        public byte MinLevel { get; set; }
        public byte MaxLevel { get; set; }
        public byte Unknown0 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] condition { get; set; }


        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 12)]
        public BOT bot { get; set; }
        public uint _class { get; set; }
        public uint pang { get; set; }

        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 40)]
        public Reward reward { get; set; }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME Open { get; set; } //240 init
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME Start { get; set; }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME End { get; set; }
        public uint Unknown1 { get; set; }
        public uint Clear_GP_TypeID { get; set; }
        public uint Lock_YN { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 516)]
        byte[] InfoBytes { get; set; }//8 start position
        public string Info { get => Encoding.GetEncoding("Shift_JIS").GetString(InfoBytes).Replace("\0", ""); set => InfoBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(516, '\0')); }
        public GrandPrixData()

        {
            Active = false;
            ID = 0;
            TypeID_Link = 0;
            TypeGP = 0; // You need to assign a value to this property based on its Type
            TimeHole = 0;
            Name = string.Empty;
            ticket = new Ticket();
            Event_Icon = string.Empty;
            flag = new Flag();
            rule = 0;
            course_info = new CourseInfo();
            MinLevel = 0;
            MaxLevel = 0;
            Unknown0 = 0;
            condition = new uint[2];
            bot = new BOT();
            _class = 0;
            pang = 0;
            reward = new Reward
            {
                _typeid = new uint[5],
                qntd = new uint[5],
                time = new uint[5]
            };
            Open = new SYSTEMTIME(); // You need to assign a value to this property based on its Type
            Start = new SYSTEMTIME(); // You need to assign a value to this property based on its Type
            End = new SYSTEMTIME(); // You need to assign a value to this property based on its Type
            Unknown1 = 0;
            Clear_GP_TypeID = 0;
            Lock_YN = 0;
            Info = string.Empty;
        }
        public void FixBug()
        {
            var _Open = Open; // You need to assign a value to this property based on its Type
            var _Start = Start; // You need to assign a value to this property based on its Type
            var _End = End; // You need to assign a value to this property based on its Type

            Open.TimeGP = _Open.CheckAndReset();
            Start.TimeGP = _Start.CheckAndReset();
            End.TimeGP = _End.CheckAndReset();
        }

        public bool IsGPEvent()
        {
            return TypeGP == 3 || Open.Minute > 0 && Start.Year > 0 && End.Year > 0;
        }
        //tempo ja acabou
        public bool Check()
        {
            var a = Open.Hour > 0 || Open.Minute > 0;
            var b = Start.Hour > 0 || Start.Minute > 0;
            var c = End.Hour > 0 || End.Minute > 0;
            return a || b & c;
        }
        public GrandPrixData CreateEvent()
        {

            ID = 51947265;
            TypeID_Link = 51947264;
            TypeGP = 3;
            TimeHole = 0;
            Name = "Evento de Iniciante";
            ticket = new Ticket
            {
                _typeid = 436208228,
                qntd = 3
            };
            //event_common01 primeiro, 

            Event_Icon = "2017whiteday_event";//sao 3
            flag = new Flag
            {
                Natural_Mode = true,
                Shot_Mode = false,
                Hole_cup_x2 = 1
            };
            course_info = new CourseInfo
            {
                Course = 0,
                Modo = 0,
                Qntd_hole = 18
            };
            MinLevel = 0;
            MaxLevel = 0;
            Unknown0 = 0;
            condition = new uint[2] { 59, 180 };
            bot = new BOT
            {
                ScoreBotMax = -21,
                ScoreBotMed = 2,
                ScoreBotMin = 8
            };
            _class = 2;
            pang = 600;
            reward = new Reward
            {
                _typeid = new uint[5] { 436207632, 335544470, 436208243, 0, 0 },
                qntd = new uint[5] { 600, 20, 3, 0, 0 },
                time = new uint[5]
            };
            rule = 436208271;
            Open = new SYSTEMTIME
            {
                Hour = 0,
                Minute = 15
            };
            Start = new SYSTEMTIME
            {
                Hour = 0,
                Minute = 25
            };
            End = new SYSTEMTIME
            {
                Hour = 1,
                Minute = 15
            };
            Unknown1 = 0;
            Clear_GP_TypeID = 0;
            Lock_YN = 0;
            Info = "Grande Premio Evento [GameRaze]";
            return this;
        }

        public uint CreateNewId(uint eventType, uint classAb, uint configType, uint sequenceId)
        {
            // Validação dos parâmetros
            if (eventType > 3)
                throw new ArgumentOutOfRangeException(nameof(eventType), "eventType deve estar entre 0 e 3.");
            if (classAb < 1 || classAb > 255)
                throw new ArgumentOutOfRangeException(nameof(classAb), "classAb deve estar entre 0 e 255.");
            if (configType > 255)
                throw new ArgumentOutOfRangeException(nameof(configType), "configType deve estar entre 0 e 255.");
            if (sequenceId > 15)
                throw new ArgumentOutOfRangeException(nameof(sequenceId), "sequenceId deve estar entre 0 e 15.");

            // Criação do ID
            uint id = 0;

            // Adiciona a parte (1) - evento e tipo (4 bits totais)
            id |= (eventType & 0x3) << 24;  // 2 bits para evento e 2 bits para tipo

            // Adiciona a parte (2) - classe "Aba" (8 bits)
            id |= (classAb & 0xFF) << 16;

            // Adiciona a parte (3) - tipo de configuração (8 bits)
            id |= (configType & 0xFF) << 8;

            // Adiciona a parte (4) - sequência ou horário (4 bits)
            id |= (sequenceId & 0xF);

            return id;
        }
        public uint getGrandPrixAba()
        {
            return (uint)((ID & 0x00FFFFFF) >> 19);
        }

        public uint getGrandPrixType()
        {
            return (uint)((ID & 0x0000FF00) >> 8);
        }

        public bool isGrandPrixEvent()
        {
            return (uint)((ID & 0x3000000) >> 24) == 3u;
        }

        public bool isGrandPrixNormal()
        {
            return (uint)((ID & 0x3000000) >> 24) == 0;
        }

        public uint getGrandPrixAba(uint _typeid)
        {
            return (uint)((_typeid & 0x00FFFFFF) >> 19);
        }

        public uint getGrandPrixType(uint _typeid)
        {
            return (uint)((_typeid & 0x0000FF00) >> 8);
        }

        public bool isGrandPrixEvent(uint _typeid)
        {
            return (uint)((_typeid & 0x3000000) >> 24) == 3u;
        }

        public bool isGrandPrixNormal(uint _typeid)
        {
            return (uint)((_typeid & 0x3000000) >> 24) == 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Ticket
        {
            public uint _typeid { get; set; }
            public uint qntd { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Flag
        {
            [field: MarshalAs(UnmanagedType.U1, SizeConst = 1)]
            public bool Natural_Mode { get; set; }
            [field: MarshalAs(UnmanagedType.U1, SizeConst = 1)]
            public bool Shot_Mode { get; set; }
            public byte Hole_cup_x2 { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class CourseInfo
        {
            public uint Course { get; set; }
            public uint Modo { get; set; }
            public byte Qntd_hole { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BOT
        {
            public int ScoreBotMax { get; set; }
            public int ScoreBotMed { get; set; }
            public int ScoreBotMin { get; set; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Reward
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] _typeid { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] qntd { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] time { get; set; }
            public int GetQuantity()
            {
                int count = 0;

                for (int i = 0; i < qntd.Length; i++)
                {
                    if (qntd[i] > 0)
                    {
                        count++;
                    }
                }
                return count;
            }
            public bool SetQuantity(int idx, uint qtd)
            {
                qntd[idx] = qtd;

                return qntd[idx] > 0;
            }
        }
    }
    #endregion
}
