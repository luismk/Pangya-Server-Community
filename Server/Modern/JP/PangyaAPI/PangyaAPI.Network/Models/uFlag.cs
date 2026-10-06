namespace PangyaAPI.Network.Models
{
    /// <summary>
    /// Representa as flags de controle e bloqueio de funcionalidades do servidor Pangya.
    /// Utiliza uma máscara de bits (bitwise) em um inteiro de 64 bits (<ulong>), onde cada bit 
    /// ativado ou desativado habilita ou restringe um recurso específico no jogo.
    /// </summary>
    public class ServerFlag
    {
        /// <summary>
        /// Obtém ou define o valor bruto combinado de todas as flags de recursos do servidor.
        /// </summary>
        public ulong Value { get; set; }



        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ServerFlag"/> com um valor inicial opcional.
        /// </summary>
        /// <param name="_ull">Valor inicial opcional das flags em formato de 64 bits (padrão é 0).</param>
        public ServerFlag(ulong _ull = 0)
        {
            Value = _ull;
        }

        /// <summary>
        /// Bit 0 (0x1): Flag desconhecida ou reservada 0.
        /// </summary>
        public bool Unknown0
        {
            get => (Value & 1) != 0;
            set => Value = value ? (Value | 1) : (Value & 0xFFFFFFFFFFFFFFFEuL);
        }

        /// <summary>
        /// Bit 1 (0x2): Controla o acesso geral a todos os modos de jogo (All Game).
        /// </summary>
        public bool AllGame
        {
            get => (Value & 2) != 0;
            set => Value = value ? (Value | 2) : (Value & 0xFFFFFFFFFFFFFFFDuL);
        }

        /// <summary>
        /// Bit 2 (0x4): Controla as compras na loja e o envio de presentes (Buy Shop and Gift).
        /// </summary>
        public bool BuyShopAndGift
        {
            get => (Value & 4) != 0;
            set => Value = value ? (Value | 4) : (Value & 0xFFFFFFFFFFFFFFFBuL);
        }

        /// <summary>
        /// Bit 3 (0x8): Controla especificamente o sistema de envio de presentes da loja (Gift Shop).
        /// </summary>
        public bool GiftShop
        {
            get => (Value & 8) != 0;
            set => Value = value ? (Value | 8) : (Value & 0xFFFFFFFFFFFFFFF7uL);
        }

        /// <summary>
        /// Bit 4 (0x10): Controla o acesso ao Papel Shop.
        /// </summary>
        public bool PapelShop
        {
            get => (Value & 0x10) != 0;
            set => Value = value ? (Value | 0x10) : (Value & 0xFFFFFFFFFFFFFFEFuL);
        }

        /// <summary>
        /// Bit 5 (0x20): Controla a lojinha pessoal dos jogadores (Personal Shop / Loja Pessoal) no ChatRoom.
        /// </summary>
        public bool PersonalShop
        {
            get => (Value & 0x20) != 0;
            set => Value = value ? (Value | 0x20) : (Value & 0xFFFFFFFFFFFFFFDFuL);
        }

        /// <summary>
        /// Bit 6 (0x40): Controla o modo de jogo Stroke (Versus Mode).
        /// </summary>
        public bool Stroke
        {
            get => (Value & 0x40) != 0;
            set => Value = value ? (Value | 0x40) : (Value & 0xFFFFFFFFFFFFFFBFuL);
        }

        /// <summary>
        /// Bit 7 (0x80): Controla o modo de partida padrão (Match / Match Mode).
        /// </summary>
        public bool Match
        {
            get => (Value & 0x80) != 0;
            set => Value = value ? (Value | 0x80) : (Value & 0xFFFFFFFFFFFFFF7FuL);
        }

        /// <summary>
        /// Bit 8 (0x100): Controla o modo Torneio (Tourney).
        /// </summary>
        public bool Tourney
        {
            get => (Value & 0x100) != 0;
            set => Value = value ? (Value | 0x100) : (Value & 0xFFFFFFFFFFFFFEFFuL);
        }

        /// <summary>
        /// Bit 9 (0x200): Controla o modo Torneio em Equipe (Team Tourney).
        /// </summary>
        public bool TeamTourney
        {
            get => (Value & 0x200) != 0;
            set => Value = value ? (Value | 0x200) : (Value & 0xFFFFFFFFFFFFFDFFuL);
        }

        /// <summary>
        /// Bit 10 (0x400): Controla as Batalhas de Guilda (Guild Battle).
        /// </summary>
        public bool GuildBattle
        {
            get => (Value & 0x400) != 0;
            set => Value = value ? (Value | 0x400) : (Value & 0xFFFFFFFFFFFFFBFFuL);
        }

        /// <summary>
        /// Bit 11 (0x800): Controla o modo Pang Battle.
        /// </summary>
        public bool PangBattle
        {
            get => (Value & 0x800) != 0;
            set => Value = value ? (Value | 0x800) : (Value & 0xFFFFFFFFFFFFF7FFuL);
        }

        /// <summary>
        /// Bit 12 (0x1000): Controla o modo de jogo Approach (Aproximação).
        /// </summary>
        public bool Approach
        {
            get => (Value & 0x1000) != 0;
            set => Value = value ? (Value | 0x1000) : (Value & 0xFFFFFFFFFFFFEFFFuL);
        }

        /// <summary>
        /// Bit 13 (0x2000): Controla o acesso ao Lounge (Praça/Chat).
        /// </summary>
        public bool Lounge
        {
            get => (Value & 0x2000) != 0;
            set => Value = value ? (Value | 0x2000) : (Value & 0xFFFFFFFFFFFFDFFFuL);
        }

        /// <summary>
        /// Bit 14 (0x4000): Controla o sistema de Raspadinha (Scratchy).
        /// </summary>
        public bool Scratchy
        {
            get => (Value & 0x4000) != 0;
            set => Value = value ? (Value | 0x4000) : (Value & 0xFFFFFFFFFFFFBFFFuL);
        }

        /// <summary>
        /// Bit 15 (0x8000): Flag desconhecida ou reservada 1.
        /// </summary>
        public bool Unknown1
        {
            get => (Value & 0x8000) != 0;
            set => Value = value ? (Value | 0x8000) : (Value & 0xFFFFFFFFFFFF7FFFuL);
        }

        /// <summary>
        /// Bit 16 (0x10000): Controla o serviço de Rankings (Rank Service).
        /// </summary>
        public bool RankService
        {
            get => (Value & 0x10000) != 0;
            set => Value = value ? (Value | 0x10000) : (Value & 0xFFFFFFFFFFFEFFFFuL);
        }

        /// <summary>
        /// Bit 17 (0x20000): Controla o sistema de Ticker (mensagens globais na tela).
        /// </summary>
        public bool TIcker
        {
            get => (Value & 0x20000) != 0;
            set => Value = value ? (Value | 0x20000) : (Value & 0xFFFFFFFFFFFDFFFFuL);
        }

        /// <summary>
        /// Bit 18 (0x40000): Controla o sistema de Correio / Caixa de Mensagens (MailBox).
        /// </summary>
        public bool MailBox
        {
            get => (Value & 0x40000) != 0;
            set => Value = value ? (Value | 0x40000) : (Value & 0xFFFFFFFFFFFBFFFFuL);
        }

        /// <summary>
        /// Bit 19 (0x80000): Controla o evento Grand Zodiac.
        /// </summary>
        public bool GrandZodiac
        {
            get => (Value & 0x80000) != 0;
            set => Value = value ? (Value | 0x80000) : (Value & 0xFFFFFFFFFFF7FFFFuL);
        }

        /// <summary>
        /// Bit 20 (0x100000): Controla o modo de Treino (Practice).
        /// </summary>
        public bool Practice
        {
            get => (Value & 0x100000) != 0;
            set => Value = value ? (Value | 0x100000) : (Value & 0xFFFFFFFFFFEFFFFFuL);
        }

        /// <summary>
        /// Bit 21 (0x200000): Controla o evento Grand Prix.
        /// </summary>
        public bool GrandPrix
        {
            get => (Value & 0x200000) != 0;
            set => Value = value ? (Value | 0x200000) : (Value & 0xFFFFFFFFFFDFFFFFuL);
        }

        /// <summary>
        /// Bits 22-23 (0xC00000): Flags desconhecidas ou reservadas 2.
        /// </summary>
        public bool Unknown2
        {
            get => (Value & 0xC00000) != 0;
            set => Value = value ? (Value | 0xC00000) : (Value & 0xFFFFFFFFFF3FFFFFuL);
        }

        /// <summary>
        /// Bit 24 (0x1000000): Controla o sistema de Guildas (Guild).
        /// </summary>
        public bool Guild
        {
            get => (Value & 0x1000000) != 0;
            set => Value = value ? (Value | 0x1000000) : (Value & 0xFFFFFFFFFEFFFFFFuL);
        }

        /// <summary>
        /// Bit 25 (0x2000000): Controla o modo Special Shuffler Course.
        /// </summary>
        public bool SpecialShufflerCourse
        {
            get => (Value & 0x2000000) != 0;
            set => Value = value ? (Value | 0x2000000) : (Value & 0xFFFFFFFFFDFFFFFFuL);
        }

        /// <summary>
        /// Bits 26-27 (0xC000000): Flags desconhecidas ou reservadas 3.
        /// </summary>
        public bool Unknown3
        {
            get => (Value & 0xC000000) != 0;
            set => Value = value ? (Value | 0xC000000) : (Value & 0xFFFFFFFFF3FFFFFFuL);
        }

        /// <summary>
        /// Bit 28 (0x10000000): Controla o acesso ao Memorial Shop.
        /// </summary>
        public bool MemorialShop
        {
            get => (Value & 0x10000000) != 0;
            set => Value = value ? (Value | 0x10000000) : (Value & 0xFFFFFFFFEFFFFFFFuL);
        }

        /// <summary>
        /// Bit 29 (0x20000000): Controla o modo Short Game.
        /// </summary>
        public bool ShortGame
        {
            get => (Value & 0x20000000) != 0;
            set => Value = value ? (Value | 0x20000000) : (Value & 0xFFFFFFFFDFFFFFFFuL);
        }

        /// <summary>
        /// Bit 30 (0x40000000): Controla o sistema de Maestria de Personagem (Character Mastery).
        /// </summary>
        public bool CharacterMastery
        {
            get => (Value & 0x40000000) != 0;
            set => Value = value ? (Value | 0x40000000) : (Value & 0xFFFFFFFFBFFFFFFFuL);
        }

        /// <summary>
        /// Bit 31 (0x80000000): Flag desconhecida ou reservada 4.
        /// </summary>
        public bool Unknown4
        {
            get => (Value & 0x80000000u) != 0;
            set => Value = value ? (Value | 0x80000000u) : (Value & 0xFFFFFFFF7FFFFFFFuL);
        }

        /// <summary>
        /// Bit 32 (0x100000000): Controla o sistema de Compound/Card do Lolo (Lolo Compound Card).
        /// </summary>
        public bool LoloCopoundCard
        {
            get => (Value & 0x100000000L) != 0;
            set => Value = value ? (Value | 0x100000000L) : (Value & 0xFFFFFFFEFFFFFFFFuL);
        }

        /// <summary>
        /// Bit 33 (0x200000000): Controla a reciclagem da Cadie (Cadie Recycle / Magic Box).
        /// </summary>
        public bool CadieRecycle
        {
            get => (Value & 0x200000000L) != 0;
            set => Value = value ? (Value | 0x200000000L) : (Value & 0xFFFFFFFDFFFFFFFFuL);
        }

        /// <summary>
        /// Bit 34 (0x400000000): Controla o sistema da Loja do Tiki (Legacy Tiki Shop).
        /// </summary>
        public bool LegacyTikiShop
        {
            get => (Value & 0x400000000L) != 0;
            set => Value = value ? (Value | 0x400000000L) : (Value & 0xFFFFFFFBFFFFFFFFuL);
        }
    }
}