/// create and converted by LUIS MK
namespace Pangya_GameServer.Models
{
    /// <summary>
    /// Representa as flags de estado de um jogador dentro de uma sala no servidor Pangya.
    /// Utiliza uma máscara de bits de 16 bits (<ushort>), onde cada bit individual 
    /// controla uma propriedade, status ou permissão específica do jogador na sala.
    /// </summary>
    public class PlayerRoomStateFlag
    {
        /// <summary>
        /// Obtém ou define o valor bruto combinado de todas as flags de estado da sala.
        /// </summary>
        public ushort Value { get; set; } = 0;

        /// <summary>
        /// Bit 0 (0x0001): Indica se o jogador está em um time específico (Time 1).
        /// </summary>
        public byte Team
        {
            get => (byte)((Value & (1 << 0)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 0);
                else
                    Value &= unchecked((ushort)~(1 << 0));
            }
        }

        /// <summary>
        /// Bit 1 (0x0002): Indica se o jogador está em um segundo time ou estado de time alternativo (Time 2).
        /// </summary>
        public byte Team2
        {
            get => (byte)((Value & (1 << 1)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 1);
                else
                    Value &= unchecked((ushort)~(1 << 1));
            }
        }

        /// <summary>
        /// Bit 2 (0x0004): Indica se o jogador está ausente, dormindo ou em HoleMode ocioso (Sleep).
        /// </summary>
        public byte Sleep
        {
            get => (byte)((Value & (1 << 2)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 2);
                else
                    Value &= unchecked((ushort)~(1 << 2));
            }
        }

        /// <summary>
        /// Bit 3 (0x0008): Indica se o jogador é o dono/líder da sala (Master). 
        /// Concede permissões para iniciar o jogo e expulsar outros jogadores.
        /// </summary>
        public byte Master
        {
            get => (byte)((Value & (1 << 3)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 3);
                else
                    Value &= unchecked((ushort)~(1 << 3));
            }
        }

        /// <summary>
        /// Bit 4 (0x0010): Sub-líder ou estado secundário de privilégio de dono da sala (Master 2).
        /// </summary>
        public byte Master2
        {
            get => (byte)((Value & (1 << 4)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 4);
                else
                    Value &= unchecked((ushort)~(1 << 4));
            }
        }

        /// <summary>
        /// Bit 5 (0x0020): Gender do usuario.
        /// </summary>
        public byte Gender
        {
            get => (byte)((Value & (1 << 5)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 5);
                else
                    Value &= unchecked((ushort)~(1 << 5));
            }
        }

        /// <summary>
        /// Bit 6 (0x0040): Flag relacionada à penalidade ou taxa de desistência de 10% (Quit 10%).
        /// </summary>
        public byte Quit10Porcent
        {
            get => (byte)((Value & (1 << 6)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 6);
                else
                    Value &= unchecked((ushort)~(1 << 6));
            }
        }

        /// <summary>
        /// Bit 7 (0x0080): Flag relacionada à penalidade ou taxa de desistência de 20% (Quit 20%).
        /// </summary>
        public byte Quit20Porcent
        {
            get => (byte)((Value & (1 << 7)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 7);
                else
                    Value &= unchecked((ushort)~(1 << 7));
            }
        }

        /// <summary>
        /// Bit 8 (0x0100): Indica se o jogador está utilizando asas ou efeito visual de asas na sala (Wings).
        /// </summary>
        public byte Wings
        {
            get => (byte)((Value & (1 << 8)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 8);
                else
                    Value &= unchecked((ushort)~(1 << 8));
            }
        }

        /// <summary>
        /// Bit 9 (0x0200): Indica se o jogador está pronto para iniciar a partida (Ready).
        /// </summary>
        public byte Ready
        {
            get => (byte)((Value & (1 << 9)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 9);
                else
                    Value &= unchecked((ushort)~(1 << 9));
            }
        }

        /// <summary>
        /// Bit 10 (0x0400): Bit desconhecido ou reservado 11.
        /// </summary>
        public byte unknown_bit11
        {
            get => (byte)((Value & (1 << 10)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 10);
                else
                    Value &= unchecked((ushort)~(1 << 10));
            }
        }

        /// <summary>
        /// Bit 11 (0x0800): Bit desconhecido ou reservado 12.
        /// </summary>
        public byte unknown_bit12
        {
            get => (byte)((Value & (1 << 11)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 11);
                else
                    Value &= unchecked((ushort)~(1 << 11));
            }
        }

        /// <summary>
        /// Bit 12 (0x1000): Bit desconhecido ou reservado 13.
        /// </summary>
        public byte unknown_bit13
        {
            get => (byte)((Value & (1 << 12)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 12);
                else
                    Value &= unchecked((ushort)~(1 << 12));
            }
        }

        /// <summary>
        /// Bit 13 (0x2000): Bit desconhecido ou reservado 14.
        /// </summary>
        public byte unknown_bit14
        {
            get => (byte)((Value & (1 << 13)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 13);
                else
                    Value &= unchecked((ushort)~(1 << 13));
            }
        }

        /// <summary>
        /// Bit 14 (0x4000): Bit desconhecido ou reservado 15.
        /// </summary>
        public byte unknown_bit15
        {
            get => (byte)((Value & (1 << 14)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 14);
                else
                    Value &= unchecked((ushort)~(1 << 14));
            }
        }

        /// <summary>
        /// Bit 15 (0x8000): Bit desconhecido ou reservado 16.
        /// </summary>
        public byte unknown_bit16
        {
            get => (byte)((Value & (1 << 15)) != 0 ? 1 : 0);
            set
            {
                if (value != 0)
                    Value |= (1 << 15);
                else
                    Value &= unchecked((ushort)~(1 << 15));
            }
        }
    }
}