using System.Collections.Generic;
using Pangya_GameServer.Models;

using PangyaAPI.Utilities;

namespace Pangya_GameServer.Game
{
    public class Dupla
    {
        public enum eSTATE : byte
        {
            IN_GAME,
            OUT_GAME,
            OVER_TIME // Tempo acabou antes de player acabar o jogo
        }
        public Dupla(byte _numero,
            Player _p1, Player _p2)
        {
            this.numero = _numero;
            this.p = new List<Player>();
            p.Add(_p1);
            p.Add(_p2);
            hole = new byte[2]; // Número do hole que o player está no jogo: [0] e [1]
            pang_win = new uint[2]; // Pang: [0] e [1]
            pang = new ulong[2]; // Total de pangs ganho no jogo: [0] e [1]
            dados = new Dados[][] { Tools.InitializeWithDefaultInstances<Dados>(18), Tools.InitializeWithDefaultInstances<Dados>(18) }; // Dados dos holes, 18 holes: [0] e [1] 
        }

        public int sumScoreP1()
        {
            int sum = 0;

            for (var i = 0; i < 18; ++i)
            {
                sum += dados[0][i].score;
            }

            return sum;
        }

        public int sumScoreP2()
        {

            int sum = 0;

            for (var i = 0; i < 18; ++i)
            {
                sum += dados[1][i].score;
            }

            return sum;
        }

        public byte numero; // Número da dupla
        public List<Player> p = new List<Player>(); // Players: [0] e [1]
        public byte[] hole = new byte[2]; // Número do hole que o player está no jogo: [0] e [1]
        public uint[] pang_win = new uint[2]; // Pang: [0] e [1]
        public ulong[] pang = new ulong[2]; // Total de pangs ganho no jogo: [0] e [1]
        public eSTATE[] state = new eSTATE[2]; // Estado no jogo: [0] e [1]
        public Dados[][] dados = { Tools.InitializeWithDefaultInstances<Dados>(18), Tools.InitializeWithDefaultInstances<Dados>(18) }; // Dados dos holes, 18 holes: [0] e [1]

    }
}
