using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Models
{
    // Player info
    public class player_info
    {
        public player_info()
        {
            clear();
        }

        public virtual void clear()
        {
            uid = 0;
            tipo = 0;
            level = 0;
            id = "";
            nickname = "";
            pass = "";
        }
        public uint uid { get; set; }
        public uint tipo { get; set; }
        public ushort level { get; set; }
        public string id { get; set; } = new string(new char[22]);
        public string nickname { get; set; } = new string(new char[22]);
        public string pass { get; set; } = new string(new char[40]);
    }

    public class CommandInfo
    {
        public CommandInfo(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        { 
        }
        public string toString()
        {
            return "IDX=" + Convert.ToString(idx) + ", ID=" + Convert.ToString(id) + ", ARG1=" + Convert.ToString(arg[0]) + ", ARG2=" + Convert.ToString(arg[1]) + ", ARG3=" + Convert.ToString(arg[2]) + ", ARG4=" + Convert.ToString(arg[3]) + ", ARG5=" + Convert.ToString(arg[4]) + ", TARGET=" + Convert.ToString(target) + ", FLAG=" + Convert.ToString(flag) + ", VALID=" + Convert.ToString((ushort)valid) + ", RESERVEDATE=" + Convert.ToString(reserveDate);
        }
        public uint idx { get; set; }
        public uint id { get; set; }
        public uint[] arg { get; set; } = new uint[5];
        public uint target { get; set; }
        public ushort flag { get; set; }
        public byte valid = 1;
        public DateTime reserveDate { get; set; } = DateTime.Now;
    }

    public enum COMMAND_ID : uint
    {
        BROADCAST_NOTICE,
        BROADCAST_TICKER,
        BROADCAST_CUBE_WIN,
        SHUTDOWN,
        NEW_ITEM_NOTICE,
        NEW_RATE,
        ADM_KICK_FROM_WEBSITE,
        RELOAD_SYSTEM
    }

    public class TickerInfo
    {
        public TickerInfo(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {

            if (!(nick.Length == 0))
            {
                nick = "";
            }

            if (!(msg.Length == 0))
            {
                msg = ""; 
            }
        }
        public bool isValid()
        {
            return (!(msg.Length == 0) && !(nick.Length == 0));
        }
        public string nick { get; set; } = "";
        public string msg { get; set; } = "";
    }

}
