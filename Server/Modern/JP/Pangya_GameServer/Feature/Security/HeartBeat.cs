using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Feature.Security
{
    public class HeartBeat
    {
        public uint UID { get; private set; }
        public uint LastestCheckTick { get; private set; }
        public uint ValidCheckTick { get; private set; }
        public int Chance { get; private set; }

        public HeartBeat(uint uid)
        {
            this.UID = uid;
            this.LastestCheckTick = 0;
            this.ValidCheckTick = 5000; // Padrão S4
            this.Chance = 0;
        }

        public void Recv()
        {
            this.LastestCheckTick = (uint)Environment.TickCount;
        }

        public void Reset()
        {
            this.LastestCheckTick = (uint)Environment.TickCount;
            this.Chance = 0;
            this.ValidCheckTick = 5000;
        }

        public byte Check()
        {
            uint tickCount = (uint)Environment.TickCount;

            uint diff = tickCount - this.LastestCheckTick;

            if (this.ValidCheckTick >= diff)
            {
                return 0;
            }

            this.Chance++;
            if (this.Chance < 3)
            {
                return 0;
            }

            _smp.LogManager.Instance.push(new AppMessage(
                $"[E_HEARTBIT] [{this.ValidCheckTick} < {diff}] [{tickCount}, {this.LastestCheckTick}] UID: {this.UID}",
                type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Penalidade e Reset de verificação
            this.ValidCheckTick = 60000;
            this.LastestCheckTick = (uint)Environment.TickCount;

            return 1;
        }
    }
}
