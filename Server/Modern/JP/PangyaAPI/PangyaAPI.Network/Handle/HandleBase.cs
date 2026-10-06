using PangyaAPI.Network.Core;
using System;
using System.Threading.Tasks;

namespace PangyaAPI.Network.Handle
{
    /// <summary>
    /// Classe base abstrata para o tratamento de pacotes de rede no servidor Pangya.
    /// Automatiza a injeção de dependência da sessão do jogador, o armazenamento do pacote bruto
    /// e a desserialização automática dos dados através de uma classe de resultado (<typeparamref name="TPacketResult"/>).
    /// </summary>
    /// <typeparam name="TSession">Tipo da sessão do jogador, que deve herdar de IAppSession.</typeparam>
    /// <typeparam name="TPacketResult">Tipo do resultado do pacote, que deve herdar de PacketResult e possuir um construtor vazio.</typeparam>
    public abstract class HandleBase<TSession, TPacketResult> : IPacketHandler<TSession>
       where TPacketResult : PacketResult, new()
       where TSession : class, IAppSession
    {
        /// <summary>
        /// Obtém a sessão do jogador (Client) que enviou o pacote atual.
        /// O modificador 'private set' garante que a sessão só possa ser alterada internamente pela classe base.
        /// </summary>
        public TSession Player { get; private set; }

        /// <summary>
        /// Obtém o pacote bruto recebido da rede, extraindo-o diretamente a partir do objeto <see cref="PacketResult"/>.
        /// Implementa a ServerProperty exigida pela interface <see cref="IPacketHandler{TSession}"/>.
        /// </summary>
        public Packet Packet => PacketResult?._Packet;

        /// <summary>
        /// Obtém a instância do resultado do pacote estruturado. 
        /// É responsável por armazenar e expor os dados após a conversão dos bytes binários.
        /// </summary>
        public TPacketResult PacketResult { get; } = new();

        /// <summary>
        /// Obtém ou define o pacote de resposta que será enviado de volta ao cliente.
        /// Inicializado com uma nova instância vazia de <see cref="Packet"/> pronta para escrita.
        /// </summary>
        public Packet Response { get; protected set; } = new();

        /// <summary>
        /// Método abstrato que deve ser implementado pelas classes filhas.
        /// Contém a regra de negócio específica para processar o pacote recebido.
        /// </summary>
        public abstract Task Handle();

        /// <summary>
        /// Método responsável por gerenciar o ciclo de vida do pacote.
        /// Injeta o contexto da sessão, atribui o pacote bruto, executa a desserialização (Load)
        /// e chama o método de negócio <see cref="Handle"/>.
        /// </summary>
        /// <param name="session">A sessão ativa do jogador.</param>
        /// <param name="rawPacket">Os dados binários brutos recebidos da rede.</param>
        /// <exception cref="ArgumentNullException">Lançado se a sessão fornecida for nula.</exception>
        public virtual async Task ExecuteAsync(TSession session, Packet rawPacket)
        {
            // Vincula e valida a sessão do jogador que disparou a requisição
            Player = session ?? throw new ArgumentNullException(nameof(session));

            // Se houver dados brutos, associa ao resultado e executa o parse automático dos bytes
            if (rawPacket != null)
            {
                PacketResult._Packet = rawPacket;
                PacketResult.Load(); // Realiza o parse automático dos dados binários para as propriedades tipadas
            }

            // Reseta/inicializa o pacote de resposta para evitar dados residuais
            Response = new Packet();

            // Executa a lógica de negócios implementada na classe derivada
            await Handle();
        }
    }
}