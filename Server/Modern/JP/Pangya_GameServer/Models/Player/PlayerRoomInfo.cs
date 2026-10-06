using Pangya_GameServer.Models;
using PangyaAPI.Network.Models;

namespace Pangya_GameServer.Models
{
    /// <summary>
    /// Representa todas as informações detalhadas de um jogador dentro de uma sala de jogo no servidor Pangya.
    /// Utilizada tanto para manter o estado em memória quanto para serializar os dados binários enviados aos clientes via rede.
    /// </summary>
    public class PlayerRoomInfo
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PlayerRoomInfo"/> com valores e instâncias padrão seguras.
        /// </summary>
        public PlayerRoomInfo()
        {
            Action = new PlayerRoomInfoAction();
            LadderGrade = 10; // Valor padrão recorrente observado em pacotes (não representa a posição/place).
            Capability = new PlayerCapability();
            State = new PlayerRoomStateFlag();
            ItemSkin = new uint[6];
            LocationInfo = new PlayerRoomLocationInfo();
            ShopRoom = new PlayerRoomInfoShop();
            ItemSpecial = new PlayerItemSpecialBoost();
            GuildMark = "";
            NickName = "";
            Invite = 0;
            GuildName = "";
        }

        /// <summary>
        /// Obtém ou define o identificador de objeto do jogador na sala (Object ID / OID). Padrão é -1.
        /// </summary>
        public int OID { get; set; } = -1;

        /// <summary>
        /// Obtém ou define o apelido ou nome de exibição (NickName) do jogador na sala.
        /// </summary>
        public string NickName { get; set; } = "";

        /// <summary>
        /// Obtém ou define o nome da guilda (Guild Name) à qual o jogador pertence.
        /// </summary>
        public string GuildName { get; set; } = "";

        /// <summary>
        /// Obtém ou define a posição ou classificação de RankPosition do jogador.
        /// </summary>
        public byte RankPosition { get; set; }

        /// <summary>
        /// Obtém ou define as capacidades ou permissões do jogador na sala (ocupa 2 bytes).
        /// </summary>
        public PlayerCapability Capability { get; set; } = new();

        /// <summary>
        /// Obtém ou define o identificador visual do título equipado (Title Skin).
        /// </summary>
        public uint TitleSkin { get; set; }

        /// <summary>
        /// Obtém ou define o identificador do RealRoomType do personagem (Character TypeID).
        /// </summary>
        public uint CharacterID { get; set; }

        /// <summary>
        /// Obtém ou define um array com os IDs dos itens visuais/skins equipados pelo personagem.
        /// </summary>
        public uint[] ItemSkin { get; set; }

        /// <summary>
        /// Obtém ou define as flags de estado do jogador na sala, como pronto, líder, etc. (ocupa 2 bytes).
        /// </summary>
        public PlayerRoomStateFlag State { get; set; } = new();

        /// <summary>
        /// Obtém ou define o nível de jogo (Game Level) do jogador.
        /// </summary>
        public byte GameLevel { get; set; }

        /// <summary>
        /// Obtém ou define o estado do ícone de anjo do jogador na sala.
        /// </summary>
        public PlayerRoomStateIconAngel StateAngel { get; set; }

        /// <summary>
        /// Obtém ou define o grau de classificação ou ranqueamento (Ladder Grade). 
        /// Frequente o valor 0x0A (10) em pacotes analisados.
        /// </summary>
        public byte LadderGrade { get; set; }

        /// <summary>
        /// Obtém ou define o índice/ID numérico da guilda no banco de dados.
        /// </summary>
        public int GuildIndex { get; set; }

        /// <summary>
        /// Obtém ou define a sigla, texto ou identificador textual da marca da guilda (Guild Mark).
        /// </summary>
        public string GuildMark { get; set; } = "";

        /// <summary>
        /// Obtém ou define o ID do ícone/emblema gráfico da guilda (Guild Mark Index).
        /// </summary>
        public uint GuildMarkIndex { get; set; }

        /// <summary>
        /// Obtém ou define o identificador único global do usuário (User ID / UID).
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Obtém ou define as ações atuais do jogador na sala (Animações, Sub-sala e Postura).
        /// </summary>
        public PlayerRoomInfoAction Action { get; set; } = new();

        /// <summary>
        /// Obtém ou define as informações de coordenadas e posicionamento do jogador na sala.
        /// </summary>
        public PlayerRoomLocationInfo LocationInfo { get; set; } = new();

        /// <summary>
        /// Obtém ou define as informações da lojinha pessoal montada pelo jogador na sala.
        /// </summary>
        public PlayerRoomInfoShop ShopRoom { get; set; } = new();

        /// <summary>
        /// Obtém ou define o ID do mascote (Mascot ID) equipado.
        /// </summary>
        public uint MascotID { get; set; }

        /// <summary>
        /// Obtém ou define os bônus ou modificadores de itens especiais ativos do jogador.
        /// </summary>
        public PlayerItemSpecialBoost ItemSpecial { get; set; } = new();

        /// <summary>
        /// Obtém ou define a FlagRoom de canalização. Pode estar relacionada ao RealRoomType de tesouro ou drop de itens.
        /// </summary>
        public uint ChannelingFlag { get; set; }

        /// <summary>
        /// Obtém ou define o ID de exibição ou identificador externo da conta na publisher (ex: Gamepot no JP).
        /// </summary>
        public string DisplayID { get; set; }

        /// <summary>
        /// Obtém ou define a FlagRoom de convite/sala (Invite). 
        /// Nota: Comum manter valores parecidos mesmo em entradas de salas normais (RealRoomType de espera de convite).
        /// </summary>
        public byte Invite;

        /// <summary>
        /// Obtém ou define a pontuação média (Avenge Score) baseada na média de buracos (holes) concluídos pelo jogador.
        /// </summary>
        public float AvengeScore;
        /// <summary>
        /// Obtém ou define as informações detalhadas do personagem equipado pelo jogador (<see cref="CharacterInfo"/>).
        /// Inicializado por padrão com uma nova instância vazia.
        /// </summary>
        public CharacterInfo CharacterInfo { get; set; } = new();

        /// <summary>
        /// Serializa todas as propriedades e subobjetos da classe em um array de bytes bruto, 
        /// preparando os dados para serem enviados via pacote de rede ao cliente.
        /// </summary>
        /// <param Name="WithCharacter">Parâmetro opcional para indicar se dados adicionais do personagem devem ser incluídos (atualmente não afeta o fluxo básico).</param>
        /// <returns>Um array de bytes contendo o stream binário estruturado da sala do jogador.</returns>
        public byte[] ToArray(bool WithCharacter = false)
        {
            using var p = new Packet();

            p.WriteInt32(OID);
            p.WriteString(NickName, 22);
            p.WriteString(GuildName, 20);
            p.WriteByte(RankPosition);
            p.WriteInt32(Capability.Value);
            p.WriteUInt32(TitleSkin);
            p.WriteUInt32(CharacterID);
            p.WriteUInt32(ItemSkin); // Escreve o array de skins do item
            p.WriteUInt32((State ??= new PlayerRoomStateFlag()).Value);
            p.WriteByte(GameLevel);
            p.WriteByte((StateAngel ??= new PlayerRoomStateIconAngel()).Value);
            p.WriteByte(LadderGrade);
            p.WriteInt32(GuildIndex);
            p.WriteString(GuildMark, 12);
            p.WriteUInt32(GuildMarkIndex);
            p.WriteUInt32(UID);
            p.WriteBytes((Action ??= new PlayerRoomInfoAction()).ToArray());
            p.WriteBytes((LocationInfo ??= new PlayerRoomLocationInfo()).ToArray());
            p.WriteBytes((ShopRoom ??= new PlayerRoomInfoShop()).ToArray());
            p.WriteUInt32(MascotID);
            p.WriteUInt16((ItemSpecial ??= new PlayerItemSpecialBoost()).Value);
            p.WriteUInt32(ChannelingFlag);
            p.WriteString(DisplayID, 128);
            p.WriteUInt32(Invite);
            p.WriteSingle(AvengeScore);
            if (WithCharacter)
                p.WriteBytes((CharacterInfo ??= new CharacterInfo()).ToArray());
            return p.GetBytes;
        }
    }
}