using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Models
{
    public enum eMISSION_TYPE : byte
    {
        MT_NO_TYPE,
        MT_CO_OP,
        MT_FREE_FOR_ALL
    }

    public class mission_approach
    {

        public mission_approach(uint _ul = 0u)
        {
            this.nick = "";
            clear();
        }
        public mission_approach(byte _numero,
            byte _box_qntd,
            eMISSION_TYPE _tipo,
            int _condition1,
            int _condition2,
            string _nick)
        {
            this.numero = _numero;
            this.box_qntd = _box_qntd;
            this.tipo = _tipo;
            this.condition = new int[] { _condition1, _condition2 };
            this.nick = _nick;
        }
        public virtual void clear()
        {

            numero = (byte)0u;
            box_qntd = 0;
            tipo = eMISSION_TYPE.MT_NO_TYPE;

            condition[0] = 0;
            condition[1] = 0;

            if (!nick.empty())
            {
                nick = "";
            }

        }
        public void toPacket(PangyaBinaryWriter _packet)
        { 
            _packet.WriteByte(numero);
            _packet.WriteByte(box_qntd);
            _packet.WriteByte((byte)tipo);
            _packet.WriteInt32(condition);
            _packet.WritePStr(nick);
        }

        public byte numero; // N�mero da miss�o
        public byte box_qntd; // Quantidade de box que a miss�o d�
        public eMISSION_TYPE tipo = new eMISSION_TYPE(); // Tipo da miss�o = Co-op, free-for-all; Players Reward (one player, all players, Everybody who clears)
        public int[] condition = new int[2]; // Condi��es da miss�o, 1 e 2
        public string nick = ""; // Nick name do player se a miss�o tiver uma condi��o para o player
    }

    public class mission_approach_ex : mission_approach
    {
        public mission_approach_ex(uint _ul = 0u) : base(_ul)
        {
            this.is_player_uid = false;
        }
        public override void clear()
        {

            base.clear();

            is_player_uid = false;
        }

        public bool is_player_uid; // Tem que colocar o player uid e o nick do player
    }

    public class mission_approach_dados
    {
        public class uMissionFlag
        {
            public uint flag;

            // máscaras
            private const int PLAYERS_SHIFT = 0;
            private const uint PLAYERS_MASK = 0x1F; // 5 bits

            private const int COND1_SHIFT = 5;
            private const uint COND1_MASK = 0x1FFF; // 13 bits

            private const int COND2_SHIFT = 18;
            private const uint COND2_MASK = 0x1FFF; // 13 bits

            public uint players
            {
                get => (flag >> PLAYERS_SHIFT) & PLAYERS_MASK;
                set
                {
                    flag &= ~(PLAYERS_MASK << PLAYERS_SHIFT);
                    flag |= (value & PLAYERS_MASK) << PLAYERS_SHIFT;
                }
            }

            public uint condition1
            {
                get => (flag >> COND1_SHIFT) & COND1_MASK;
                set
                {
                    flag &= ~(COND1_MASK << COND1_SHIFT);
                    flag |= (value & COND1_MASK) << COND1_SHIFT;
                }
            }

            public uint condition2
            {
                get => (flag >> COND2_SHIFT) & COND2_MASK;
                set
                {
                    flag &= ~(COND2_MASK << COND2_SHIFT);
                    flag |= (value & COND2_MASK) << COND2_SHIFT;
                }
            }
        }


        public uint numero = new uint();
        public uint box = new uint();
        public eMISSION_TYPE tipo = new eMISSION_TYPE();
        public uint reward_tipo = new uint();
        public uMissionFlag flag = new uMissionFlag();
    }

    public class approach_dados
    {
        public enum eSTATUS : byte
        {
            IN_GAME, // Est� no jogo
            LEFT_GAME // Deixou o jogo
        }

        public approach_dados(uint _ul = 0u)
        {
            clear();
        }

        public virtual void clear()
        {
            status = eSTATUS.IN_GAME;
            position = (sbyte)~0;
            distance = (uint)~0u;
            box = 0;
            rank_box = 0;
            time = 0;
        }

        public virtual void setLeftGame()
        { 
            status = eSTATUS.LEFT_GAME;
            position = (sbyte)~0;
            distance = (uint)~0u;
            box = 0;
            rank_box = 0;
            time = 0;
        }

        public eSTATUS status = new eSTATUS();
        public int oid = new int();
        public uint uid = new uint();
        public sbyte position; // Posi��o do player, -1 nenhuma
        public uint box = new uint(); // N�mero de box que o player ganhou
        public uint distance = new uint(); // Dist�ncia que o player ficou do hole, -1 se fez chip-in, timeout, OB ou Water hazard
        public uint time = new uint(); // Tempo da tacada do player, -1 se ele fez chip-in, timeout, OB ou Water hazard
        public ushort rank_box; // Box por Top Rank e no final do Approach
    }

    public class approach_dados_ex : approach_dados
    {
        public class uState
        {
            public uState(sbyte _uc = 0)
            {
                ucState = _uc;
            }

            public sbyte ucState;

            public byte chip_in
            {
                get => (byte)((ucState & (1 << 0)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        ucState |= (1 << 0);
                    else
                        ucState &= (sbyte)~(1 << 0);
                }
            }

            public byte giveup
            {
                get => (byte)((ucState & (1 << 1)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        ucState |= (1 << 1);
                    else
                        ucState &= (sbyte)~(1 << 1);
                }
            }

            public byte ob_or_water_hazard
            {
                get => (byte)((ucState & (1 << 2)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        ucState |= (1 << 2);
                    else
                        ucState &= (sbyte)~(1 << 2);
                }
            }

            public byte timeout
            {
                get => (byte)((ucState & (1 << 3)) != 0 ? 1 : 0);
                set
                {
                    if (value != 0)
                        ucState |= (1 << 3);
                    else
                        ucState &= (sbyte)~(1 << 3);
                }
            }
        }

        public enum eSTATE_QUIT : byte
        {
            SQ_IN_GAME,
            SQ_QUIT_START, // Quitou mas tem que mostrar no score board do hole
            SQ_QUIT_ENDED // Quitou e j� foi mostrado no score board do hole que ele quitou
        }

        public approach_dados_ex(uint _ul = 0u) : base(_ul)
        {
            this.total_distance = 0;
            this.total_time = 0;
            this.total_box = 0;
            this.state = new uState();
            this.state_quit = eSTATE_QUIT.SQ_IN_GAME;
        }

        public override void clear()
        {

            base.clear();

            total_distance = 0;
            total_time = 0;
            total_box = 0;
            state.ucState = 0;
            state_quit = eSTATE_QUIT.SQ_IN_GAME;
        }
        public void toPacket(PangyaBinaryWriter _packet)
        {

            // status � o primeiro addr dos dados da fun��o                 
            _packet.WriteByte(status);
            _packet.WriteInt32(oid);
            _packet.WriteUInt32(uid);
            _packet.WriteSByte(position);
            _packet.WriteUInt32(box);
            if (state.ucState != 0)//igual a zero tacada normal sem certa ou fazer algo
            {
                _packet.WriteUInt32(uint.MaxValue);
                _packet.WriteUInt32(0);
            }
            else
            {
                _packet.WriteUInt32(distance);
                _packet.WriteUInt32(time);
            }

            _packet.WriteUInt16(rank_box);
        }

        public override void setLeftGame()
        {

            base.setLeftGame();

            state_quit = eSTATE_QUIT.SQ_QUIT_START;

        }

        public uint total_distance = new uint();
        public uint total_time = new uint();
        public uint total_box = new uint();
        public uState state = new uState();
        public eSTATE_QUIT state_quit = new eSTATE_QUIT();
    }

    // Polimorfirsmo da struct PlayerGameInfo
    public class PlayerApproachInfo : PlayerGameInfo
    {
        public PlayerApproachInfo()
        {
            this.m_app_dados = new approach_dados_ex();
        }

        public override void clear()
        {

            // Clear base
            base.clear();

            // clear app dados
            m_app_dados.clear();
        }

        public approach_dados_ex m_app_dados = new approach_dados_ex(); // Approach dados do player
    }

}
