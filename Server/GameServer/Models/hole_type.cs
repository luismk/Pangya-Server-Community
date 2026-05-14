using System;
using System.Runtime.InteropServices;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Models
{
    public class stHoleWind
    {
        public stHoleWind(uint _ul = 0)
        {
            this.degree = new stDegree(0);
            clear();
        }
        public stHoleWind(byte _wind, ushort _degree)
        {
            this.wind = _wind;
            this.degree = new stDegree(_degree);
        }
        public void clear()
        {
        }
        public byte wind;
        public class stDegree
        {
            public stDegree() : this(0)
            { }
            public stDegree(ushort _degree)
            {
                this.degree = _degree;
                this.min_degree = 0;
                min_degree = (ushort)(((degree - 20) < 0) != false ? (ushort)(LIMIT_DEGREE + (degree) - 20) : (degree) - 20);
            }
            public void setDegree(ushort _degree)
            {
                degree = _degree;

                min_degree = (ushort)(((degree - 20) < 0) != false ? (ushort)(LIMIT_DEGREE + (degree) - 20) : (degree) - 20);
            }
            public ushort getDegree()
            {
                return degree;
            }
            public ushort getShuffleDegree()
            {

                degree = (ushort)((min_degree + (new Random().Next() % LIMIT_RANGE)) % LIMIT_DEGREE);
                return degree;
            }
            protected ushort degree;
            protected ushort min_degree;
            protected const byte LIMIT_RANGE = 40;
        }
        public stDegree degree = new stDegree();
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class stHolePar
    {
        public int par;
        public int[] range_score = new int[2];
        public int total_shot;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stXZLocation
    {
        public float x;
        public float z;
    }

    public class uCubeCoinFlag
    {
        public byte[] ucFlag = new byte[2];
        public byte type;
        public byte enable; // Ativa Coin e Cube no Hole
        public byte enable_cube; // Ativa Cube
        public byte enable_coin; // Ativa Coin
    }

    public class Cube
    {
        public enum eFLAG_LOCATION : uint
        {
            EDGE_GREEN,
            CARPET,
            AIR,
            GROUND
        }

        public enum eTYPE : uint
        {
            COIN,
            CUBE
        }

        public Cube(uint _ul = 0)
        {
            clear();
        }
        public Cube(uint _id,
            eTYPE _tipo,
            uint _flag_unknown,
            eFLAG_LOCATION _flag_location,
            float _x, float _y, float _z)
        {
            this.id = _id;
            this.tipo = (_tipo);
            this.flag_unknown = _flag_unknown;
            this.flag_location = (_flag_location);
            this.location = new Cube.stLocation(_x,
                _y, _z);
        }
        public void clear()
        {
        }
        public class stLocation
        {
            public float x;
            public float y;
            public float z;
            public stLocation() { }
            public stLocation(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        public stLocation location = new stLocation();
        public uint id = new uint();
        public eTYPE tipo;
        public uint flag_unknown = new uint();
        public eFLAG_LOCATION flag_location; // Borda Green = 0, Carpet = 1 , Ar = 2, Chão = 3
    }

    public class CubeEx : Cube
    {
        public CubeEx(uint _ul = 0) : base(_ul)
        {
            this.rate = 1u;
        }
        public CubeEx(uint _id,
            eTYPE _tipo,
            uint _flag_unknown,
            eFLAG_LOCATION _flag_location,
            float _x, float _y, float _z,
            uint _rate) : base(_id,
                _tipo, _flag_unknown,
                _flag_location, _x, _y, _z)
        {
            this.rate = _rate;
        }
        public new void clear()
        {
            base.clear();

            rate = 1u;
        }

        public uint rate = new uint(); // Aqui é quantas vezes o cube ou coin caiu no mesmo lugar
    }
}
