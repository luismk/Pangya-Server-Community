namespace Pangya_GameServer.Models
{
    // Dados dos holes no jogo
    public class Dados
    {
        public int score;
        public int tacada;
        public bool finish = true;
    }

    // Guild Match register
    public class GuildMatch
    {
        public uint[] uid = new uint[2];        // Guild UID: [0] e [1]
        public uint[] point = new uint[2];      // Guild Point: [0] e [1]
        public uint[] pang = new uint[2];       // Guild Pang: [0] e [1]
    }

    // Guild Points
    public class GuildPoints
    {
        public enum eGUILD_WIN : byte
        {
            WIN,
            LOSE,
            DRAW,
        }
        public void clear()
        {
        }
        public uint uid;
        public ulong point;
        public ulong pang;
        public eGUILD_WIN win;
    }

    // Guild Member Points
    public class GuildMemberPoints
    {
        public int guild_uid = 0;
        public uint member_uid = 0;
        public int point = 0;
        public uint pang = 0;
    }

}
