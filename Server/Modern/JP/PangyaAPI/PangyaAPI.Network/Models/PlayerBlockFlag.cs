namespace PangyaAPI.Network.Models
{
    /// <summary>
    /// Gerencia as flags de bloqueio e restrições específicas de um jogador no servidor Pangya.
    /// Realiza a ponte entre o estado individual de bloqueio do usuário e as flags globais do servidor.
    /// </summary>
    public class PlayerBlockFlag
    {
        /// <summary>
        /// Representa o estado atual de bloqueios específicos aplicados ao jogador (<see cref="PlayerStateBlockFlag"/>).
        /// </summary>
        public PlayerStateBlockFlag State { get; set; } = new PlayerStateBlockFlag();

        /// <summary>
        /// Representa as flags globais de recursos e bloqueios do servidor (<see cref="ServerFlag"/>) afetadas pelo estado do jogador.
        /// </summary>
        public ServerFlag Flag { get; set; } = new ServerFlag();

        /// <summary>
        /// Define um novo estado de bloqueio para o jogador com base em um valor numérico de 64 bits (máscara de bits) 
        /// e sincroniza automaticamente as restrições correspondentes nas flags do servidor (<see cref="Flag"/>).
        /// </summary>
        /// <param name="_id_state">O valor numérico contendo os bits de estado de bloqueio do jogador.</param>
        public void SetState(ulong _id_state)
        {
            // Inicializa o estado de bloqueio do jogador com o valor fornecido
            State = new PlayerStateBlockFlag(_id_state);

            // Sincroniza os bloqueios individuais do jogador com as restrições correspondentes do servidor

            // Bloqueado no Lounge / Praça de Chat
            if (State.BlockInLounger)
            {
                Flag.Lounge = true;
            }

            // Bloqueado na Loja Pessoal / Personal Shop dentro do Lounge
            if (State.BlockInShopLounger)
            {
                Flag.PersonalShop = true;
            }

            // Bloqueado no envio de presentes da Loja
            if (State.BlockInGiftShop)
            {
                Flag.GiftShop = true;
            }

            // Bloqueado no Papel Shop
            if (State.BlockInPapelShop)
            {
                Flag.PapelShop = true;
            }

            // Bloqueado no sistema de Raspadinha (Scratchy)
            if (State.BlockInScratchy)
            {
                Flag.Scratchy = true;
            }

            // Bloqueado no recebimento/envio de mensagens globais (Ticker)
            if (State.BlockInNoticeTicker)
            {
                Flag.TIcker = true;
            }

            // Bloqueado no Memorial Shop
            if (State.BlockInMemorialShop)
            {
                Flag.MemorialShop = true;
            }
        }
    }
}