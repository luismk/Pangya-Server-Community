using PangyaAPI.Network.Models;

namespace Pangya_GameServer.Models
{
    /// <summary>
    /// Representa as informações principais de membro/jogador no servidor Pangya 
    /// (contém Login, Nickname, Guilda, Nível, Experiência e dados de estado).
    /// </summary>
    public class PlayerMemberInfo
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PlayerMemberInfo"/> com valores padrão e instâncias limpas.
        /// </summary>
        public PlayerMemberInfo()
        {
            RankPosition = new uint[3];
            GuildMarkImage = "";
            Commentary = "";
            ChannelingFlag = 0;
            CountPointEvent = 0;
            GalleryIndex = 0;
            Capability = new PlayerCapability();
            State = new PlayerMemberInfoStateFlag();
            PapelShop = new PlayerPapelShopInfo();
            OID = -1;
            BlockFlag = new PlayerBlockFlag();
            PapelShopLastUpdate = new SystemTime();
            PapelShopLastUpdate.CreateTime();
            RoomID = DefineConstants.DEFAULT_ROOM_ID;
        }

        /// <summary>
        /// Obtém ou define o identificador de login (Account ID / Username) da conta.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Obtém ou define o apelido ou nome de exibição (NickName) do personagem no jogo.
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Obtém ou define o nome da guilda (Guild Name) à qual o jogador pertence.
        /// </summary>
        public string GuildName { get; set; }

        /// <summary>
        /// Obtém ou define a imagem ou URL/caminho do brasão/marca da guilda (Guild Mark Image).
        /// </summary>
        public string GuildMarkImage { get; set; }

        /// <summary>
        /// Obtém ou define o texto de comentário ou status personalizado atribuído ao jogador.
        /// </summary>
        public string Commentary { get; set; }

        /// <summary>
        /// Obtém ou define o índice da escola (School Index) associada ao jogador.
        /// </summary>
        public uint SchoolIndex { get; set; }

        /// <summary>
        /// Obtém ou define as capacidades ou permissões da conta.
        /// </summary>
        public PlayerCapability Capability { get; set; } = new();

        /// <summary>
        /// Obtém ou define o índice da galeria (Gallery Index) do jogador.
        /// </summary>
        public uint GalleryIndex { get; set; }

        /// <summary>
        /// Obtém ou define o identificador de objeto do jogador (Object ID / OID). Padrão é -1.
        /// </summary>
        public int OID;

        /// <summary>
        /// Obtém ou define as posições de ranking do jogador: 
        /// [0] = Total, [1] = Diário, [2] = Guilda.
        /// </summary>
        public uint[] RankPosition { get; set; }

        /// <summary>
        /// Obtém ou define o índice numérico da guilda no banco de dados.
        /// </summary>
        public uint GuildIndex { get; set; }

        /// <summary>
        /// Obtém ou define o ID do ícone/emblema da guilda (presente especificamente na versão japonesa).
        /// </summary>
        public uint GuildMarkIndex { get; set; }

        /// <summary>
        /// Obtém ou define as flags de estado do membro (<see cref="PlayerMemberInfoStateFlag"/>).
        /// </summary>
        public PlayerMemberInfoStateFlag State { get; set; } = new();

        /// <summary>
        /// Obtém ou define a FlagRoom de tempo/contagem de login: 
        /// 1 = Primeira vez que logou, 2 = Já realizou login anterior no servidor.
        /// </summary>
        public ushort FlagLoginTime { get; set; }

        /// <summary>
        /// Obtém ou define as informações referentes ao sistema Papel Shop do jogador.
        /// </summary>
        public PlayerPapelShopInfo PapelShop { get; set; } = new();

        /// <summary>
        /// Obtém ou define a contagem de pontos de evento (recurso introduzido na S4 TH).
        /// </summary>
        public uint CountPointEvent { get; set; }

        /// <summary>
        /// Obtém ou define as flags de bloqueio do jogador (<see cref="PlayerBlockFlag"/>). 
        /// Nota: Em versões mais antigas continha dados de time_block, mas no Fresh UP JP o RealRoomType de bloco principal passou a ser de 64 bits.
        /// </summary>
        public PlayerBlockFlag BlockFlag { get; set; } = new();

        /// <summary>
        /// Obtém ou define a FlagRoom de canalização (recurso introduzido na S4 TH).
        /// </summary>
        public uint ChannelingFlag { get; set; }

        /// <summary>
        /// Obtém ou define o ID de exibição externo da conta na publisher (ex: Gamepot no JP).
        /// </summary>
        public string DisplayID { get; set; }

        #region Extensão (Não faz parte do pacote original, mas é útil para o servidor)

        /// <summary>
        /// Obtém ou define o identificador único global do usuário (User ID / UID).
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Obtém ou define os pontos de vitória conquistados pela guilda.
        /// </summary>
        public uint GuildWinPoints { get; set; }

        /// <summary>
        /// Obtém ou define a quantidade de pangs ganhos pela guilda.
        /// </summary>
        public long GuildWinPangs { get; set; }

        /// <summary>
        /// Obtém ou define o ID da sala atual onde o jogador está alocado.
        /// </summary>
        public short RoomID { get; set; }

        /// <summary>
        /// Obtém ou define o gênero do personagem/jogador.
        /// </summary>
        public byte Gender { get; set; }

        /// <summary>
        /// Obtém ou define o nível de jogo (Game Level) atual.
        /// </summary>
        public byte GameLevel { get; set; }

        /// <summary>
        /// Obtém ou define o status de conclusão do tutorial.
        /// </summary>
        public byte Tutorial { get; set; }

        /// <summary>
        /// Obtém ou define a primeira FlagRoom de controle de evento.
        /// </summary>
        public byte Event1 { get; set; }

        /// <summary>
        /// Obtém ou define a segunda FlagRoom de controle de evento.
        /// </summary>
        public byte Event2 { get; set; }

        /// <summary>
        /// Obtém ou define a FlagRoom de comportamento/manner do jogador.
        /// </summary>
        public uint MannerFlag { get; set; }

        /// <summary>
        /// Obtém ou define o registro de data/hora da última atualização do Papel Shop.
        /// </summary>
        public SystemTime PapelShopLastUpdate { get; set; } = new();

        #endregion

        /// <summary>
        /// Serializa as informações principais do membro em um array de bytes binário bruto, 
        /// respeitando estritamente o layout de tamanho fixo do pacote original (Tamanho total esperado: 297 bytes).
        /// </summary>
        /// <returns>Um array de bytes contendo o stream binário estruturado.</returns>
        public byte[] ToArray(bool IncludeRoomID = false)
        {
            using var p = new Packet();
            if(IncludeRoomID)
                p.WriteInt16(RoomID);
            p.WriteString(Login, 22);
            p.WriteString(NickName, 22);
            p.WriteString(GuildName, 17);
            p.WriteString(GuildMarkImage, 12);
            p.WriteString(Commentary, 35);
            p.WriteUInt32(SchoolIndex);
            p.WriteInt32(Capability.Value);
            p.WriteUInt32(GalleryIndex);
            p.WriteInt32(OID);
            p.WriteUInt32(RankPosition);
            p.WriteUInt32(GuildIndex);
            p.WriteUInt32(GuildMarkIndex);
            p.WriteByte(State.Value);
            p.WriteUInt16(FlagLoginTime);
            p.WriteBytes(PapelShop.ToArray());
            p.WriteUInt32(CountPointEvent);
            p.WriteUInt64(BlockFlag.State.Value);
            p.WriteUInt32(ChannelingFlag);
            p.WriteString(DisplayID, 128);
            return p.GetBytes;
        }
         
    }
}