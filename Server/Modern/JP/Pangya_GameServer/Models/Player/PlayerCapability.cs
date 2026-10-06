using Pangya_GameServer.Flags;

namespace Pangya_GameServer.Models
{
    /// <summary>
    /// Representa as capacidades, privilégios, cargos (como Game Master) e status especiais 
    /// de uma conta de jogador no servidor Pangya através de uma máscara de bits de 32 bits (<see cref="int"/>).
    /// </summary>
    public class PlayerCapability
    {
        /// <summary>
        /// Obtém ou define o valor bruto combinado de todas as flags de capacidade.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PlayerCapability"/> com um valor inicial opcional.
        /// </summary>
        /// <param Name="ul">Valor inicial da máscara de bits de capacidade (padrão é 0).</param>
        public PlayerCapability(int ul = 0)
        {
            Value = ul;
        }

        /// <summary>
        /// Obtém ou define se o jogador possui um estado normal (sem flags de privilégio ativas, valor igual a 0).
        /// </summary>
        public bool Normal
        {
            get => (Value == 0);
            set
            {
                if (value) Value = 0; // Reseta todas as flags para zero
                else Value &= ~0; // Mantém ou desativa conforme a lógica original
            }
        }

        /// <summary>
        /// Bit 0 (0x01): Indica se o jogador está jogando via IA (Computer/Bot) ou HoleMode simulado.
        /// </summary>
        public bool IA
        {
            get => (Value & (uint)CapabilityFlags.COMPUTER) != 0;
            set
            {
                if (value) Value |= 1; // Ativa o bit 0
                else Value &= ~1; // Desativa o bit 0
            }
        }

        /// <summary>
        /// Bit 1 (0x02): Flag relacionada a recursos de Galeria / Galeria combinada 
        /// (Geralmente associada a pacotes especiais como GM + PC Bang + Premium).
        /// </summary>
        public bool Gallery
        {
            get => (Value & (int)CapabilityFlags.GALLERY) != 0;
            set
            {
                if (value) Value |= 2; // Ativa o bit 1
                else Value &= ~2; // Desativa o bit 1
            }
        }

        /// <summary>
        /// Bit 2 (0x04): Indica se o jogador possui privilégios de Administrador / Game Master (GM).
        /// </summary>
        public bool IsGameMaster
        {
            get => (Value & (int)CapabilityFlags.GAME_MASTER) != 0;
            set
            {
                if (value) Value |= 4; // Ativa o bit 2
                else Value &= ~4; // Desativa o bit 2
            }
        }

        /// <summary>
        /// Bit 3 (0x08): Indica se o jogador possui acesso administrativo pelo site (GM Edit Site).
        /// </summary>
        public bool AdminSite
        {
            get => (Value & (int)CapabilityFlags.GM_EDIT_SITE) != 0;
            set
            {
                if (value) Value |= 8; // Ativa o bit 3
                else Value &= ~8; // Desativa o bit 3
            }
        }

        /// <summary>
        /// Bits combinados (Valor 14 / 0x0E): Indica se o jogador está em HoleMode espectador (Observer Mode).
        /// </summary>
        public bool ObserverMode
        {
            get => (Value & 14) == 14;
            set
            {
                if (value) Value |= 14;
                else Value &= ~14;
            }
        }

        /// <summary>
        /// Flag de privilégio total (God Mode / Super Administrador).
        /// </summary>
        public bool God
        {
            get => (Value & (int)CapabilityFlags.GOD) != 0;
            set
            {
                if (value) Value |= (int)CapabilityFlags.GOD;
                else Value &= ~(int)CapabilityFlags.GOD;
            }
        }

        /// <summary>
        /// Bit 4 (0x10): Indica se o Game Master está bloqueado de doar/entregar itens (GM Block Item Give).
        /// </summary>
        public bool IsGameMasterBlockItemGive
        {
            get => (Value & 16) != 0;
            set
            {
                if (value) Value |= 16;
                else Value &= ~16;
            }
        }

        /// <summary>
        /// Bit 6 (0x40): Indica se o jogador tem permissão para gerenciar ou interagir com o Sistema de Eventos de Usuário.
        /// </summary>
        public bool UserEventSystem
        {
            get => (Value & 64) != 0;
            set
            {
                if (value) Value |= 64;
                else Value &= ~64;
            }
        }

        /// <summary>
        /// Bit 7 (0x80): Indica uma subcategoria ou estado padrão de Game Master (GM Normal).
        /// </summary>
        public bool IsGameMasterNormal
        {
            get => (Value & 128) != 0;
            set
            {
                if (value) Value |= 128;
                else Value &= ~128;
            }
        }

        /// <summary>
        /// Bit 8 (0x100 / 256): Indica se o Game Master está com o envio de presentes bloqueado (Block Gift Shop).
        /// </summary>
        public bool IsGameMasterBlockGiftShop
        {
            get => (Value & (int)CapabilityFlags.BLOCK_GIFT_SHOP) != 0;
            set
            {
                if (value) Value |= 256;
                else Value &= ~256;
            }
        }

        /// <summary>
        /// Bit 9 (0x200 / 512): Indica se a conta possui acesso de testador beta ou login em servidor de testes (Login Test Server).
        /// </summary>
        public bool BetaUser
        {
            get => (Value & (int)CapabilityFlags.LOGIN_TEST_SERVER) != 0;
            set
            {
                if (value) Value |= 512;
                else Value &= ~512;
            }
        }

        /// <summary>
        /// Bit 10 (0x400 / 1024): Indica se o jogador possui a FlagRoom de Manto/Capa (Mantle) ou privilégio visual correspondente.
        /// </summary>
        public bool Mantle
        {
            get => (Value & (int)CapabilityFlags.MANTLE) != 0;
            set
            {
                if (value) Value |= 1024;
                else Value &= ~1024;
            }
        }

        /// <summary>
        /// Bit 11 (0x800 / 2048): Flag reservada ou desconhecida 3.
        /// </summary>
        public bool unknown3
        {
            get => (Value & 2048) != 0;
            set
            {
                if (value) Value |= 2048;
                else Value &= ~2048;
            }
        }

        /// <summary>
        /// Bit 14 (0x4000 / 16384): Indica se o jogador possui status de usuário Premium ativo.
        /// </summary>
        public bool UserPremium
        {
            get => (Value & (int)CapabilityFlags.PREMIUM_USER) != 0;
            set
            {
                if (value) Value |= 16384;
                else Value &= ~16384;
            }
        }

        /// <summary>
        /// Bit 15 (0x8000 / 32768): Indica se o jogador possui título especial de Game Master (Title GM).
        /// </summary>
        public bool IsGameMasterTitle
        {
            get => (Value & (int)CapabilityFlags.TITLE_GM) != 0;
            set
            {
                if (value) Value |= 32768;
                else Value &= ~32768;
            }
        }
    }
}