using System;

namespace PangyaAPI.Network.Models
{
    /// <summary>
    /// Representa as informações básicas e essenciais do perfil de um jogador no servidor Pangya.
    /// Serve como classe base para estruturas de dados mais completas de sessão e personagem.
    /// </summary>
    public class PlayerInfoBase
    {
        /// <summary>
        /// Obtém ou define o identificador único global do jogador no banco de dados (User ID / UID).
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Obtém ou define a capacidade/permissão do jogador no servidor. 
        /// Ex: 0 = Jogador normal, 4 = Master player / Administrador.
        /// </summary>
        public uint Capability { get; set; }

        /// <summary>
        /// Obtém ou define a data e hora em que a conta foi registrada ou entrou no sistema.
        /// </summary>
        public DateTime EntryRegister { get; set; }

        /// <summary>
        /// Obtém ou define o token de validação da loja de cookies (Shop Token) do jogador.
        /// </summary>
        public string ShopToken { get; set; }

        /// <summary>
        /// Obtém ou define as flags de bloqueio individuais do jogador (<see cref="BlockFlag"/>), 
        /// controlando restrições específicas aplicadas à conta.
        /// </summary>
        public PlayerBlockFlag BlockFlag { get; set; }

        /// <summary>
        /// Obtém ou define o índice/ID da guilda (Guild Index) à qual o jogador pertence.
        /// </summary>
        public uint GuildIndex { get; set; }

        /// <summary>
        /// Obtém ou define o nome da guilda (Guild Name) do jogador.
        /// </summary>
        public string GuildName { get; set; }

        /// <summary>
        /// Obtém ou define o índice do servidor atual onde o jogador está conectado.
        /// </summary>
        public uint ServerIndex { get; set; }

        /// <summary>
        /// Obtém ou define o nível (Level) atual do jogador no jogo.
        /// </summary>
        public ushort Level { get; set; }

        /// <summary>
        /// Obtém ou define o gênero do personagem/jogador (Ex: 0 = Masculino, 1 = Feminino, etc.).
        /// </summary>
        public byte Gender { get; set; }

        /// <summary>
        /// Obtém ou define o identificador de login (Account ID / Username) da conta.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Obtém ou define o apelido ou nome de exibição (NickName) do player.
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Obtém ou define o estado de conexão atual do jogador na rede (State Logged).
        /// </summary>
        public byte StateLogged { get; set; }

        /// <summary>
        /// Obtém ou define o endereço MAC da máquina do cliente conectado.
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PlayerInfoBase"/> com valores padrão seguros (strings vazias e instâncias limpas).
        /// </summary>
        public PlayerInfoBase()
        {
            Clear();
        }

        /// <summary>
        /// Reseta todas as propriedades da classe para os seus valores iniciais ou zerados.
        /// Útil para limpar os dados quando um jogador se desconecta.
        /// </summary>
        public virtual void Clear()
        {
            UID = 0;
            Capability = 0;
            ShopToken = "302540"; // Valor fixo padrão utilizado pelo sistema
            EntryRegister = DateTime.Now;
            BlockFlag = new PlayerBlockFlag();
            MacAddress = string.Empty;
            GuildIndex = 0;
            GuildName = string.Empty;
            ServerIndex = 0;
            Level = 0;
            Gender = 0;
            Login = string.Empty;
            NickName = string.Empty;
            StateLogged = 0;
        }

        /// <summary>
        /// Copia os dados de outra instância de <see cref="PlayerInfoBase"/> para o objeto atual, 
        /// realizando validações contra valores nulos.
        /// </summary>
        /// <param name="info">A instância de origem contendo os novos dados do jogador.</param>
        /// <exception cref="ArgumentNullException">Lançado se o parâmetro <paramref name="info"/> for nulo.</exception>
        public void Set(PlayerInfoBase info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info), "O parâmetro 'info' não pode ser nulo.");

            this.UID = info.UID;
            this.Capability = info.Capability;
            this.EntryRegister = info.EntryRegister;

            // Usado no sistema de cookie shop dentro do jogo. 
            // Não é recomendado desativar pois o sistema de cookie shop é necessário para comprar itens com cash/cookies.
            this.ShopToken = info.ShopToken ?? "302540";

            this.BlockFlag = info.BlockFlag ?? new PlayerBlockFlag();
            this.GuildIndex = info.GuildIndex;
            this.GuildName = info.GuildName ?? string.Empty;
            this.ServerIndex = info.ServerIndex;
            this.Level = info.Level;
            this.Gender = info.Gender;
            this.Login = info.Login ?? string.Empty;
            this.NickName = info.NickName ?? string.Empty;
            this.StateLogged = info.StateLogged;
        }
    }
}