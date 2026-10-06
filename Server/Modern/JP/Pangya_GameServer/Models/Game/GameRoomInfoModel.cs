/// create and converted by LUIS MK
using Pangya_GameServer.Flags;
using System.Text;

namespace Pangya_GameServer.Models.Game
{
    /// <summary>
    /// Representa todas as informações detalhadas de uma sala de jogo (<see cref="GameRoomInfoModel"/>) no servidor Pangya.
    /// Gerencia configurações de estado, regras de partida, mapas, taxas de bônus, serialização de pacotes de rede e exibição textual.
    /// </summary>
    public class GameRoomInfoModel
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="GameRoomInfoModel"/> invocando o método de limpeza padrão.
        /// </summary>
        public GameRoomInfoModel()
        {
            Name = "";
            IsPublicRoom = 1;
            StateRoom = 1;
            FlagRoom = 0;
            MaxUsers = 0;
            CurrentUsers = 0;
            GameKey = new byte[16];
            GalleryLimite = 30;
            HoleCount = 0;
            RoomType = 0;
            RoomID = -1;
            HoleMode = 0;
            CourseIndex = RoomCourseFlags.BLUE_LAGOON;
            TimeSec = 0;
            TimeMin = 0;
            TrophyID = 0;
            SpecialFlag = 0;
            GuildBattle = new RoomGuildBattleInfo();
            RatePangs = 0;
            RateExperience = 0;
            IsGameMaster = 0;
            OwnerUID = 0;
            SpecialRoomFLag = 0;
            ItemIDArtifact = 0;
            SpecialModeRoom = new SpecialModeFlag();
            grand_prix = new RoomGrandPrixInfo();
            Password = "";
            IDHoleRepeted = 0;
            HoleFixed = 0;
            RealRoomType = 0;
            StateSleep = 0;
            IsChannelRookie = false;
            IsAngelQuiterEvent = false;
        } 
        /// <summary>
        /// Identificador único global (GUID) da sala no servidor.
        /// </summary>
        public Guid roomId { get; set; } = Guid.Empty;

        /// <summary>
        /// Obtém ou define o nome da sala (tamanho máximo de 64 caracteres).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indica se a sala é pública ou privada: 1 = Sala sem senha, 0 = Sala com senha.
        /// </summary>
        public byte IsPublicRoom { get; set; }

        /// <summary>
        /// Obtém ou define o estado atual da sala: 1 = Sala em espera (Waiting), 0 = Sala em partida (In Game).
        /// </summary>
        public byte StateRoom { get; set; }

        /// <summary>
        /// Flag que indica se jogadores podem entrar na sala após o início da partida: 1 = Permitido, 0 = Bloqueado.
        /// </summary>
        public byte FlagRoom { get; set; }

        /// <summary>
        /// Obtém ou define o número máximo de usuários permitidos na sala.
        /// </summary>
        public byte MaxUsers { get; set; }

        /// <summary>
        /// Obtém ou define o número atual de usuários presentes na sala.
        /// </summary>
        public byte CurrentUsers { get; set; }

        /// <summary>
        /// Chave de segurança/criptografia da sala de 16 bytes (usada como senha encriptografada).
        /// </summary>
        public byte[] GameKey { get; set; }

        /// <summary>
        /// Identificador de galeria / modo multiplayer (introduzido na S4).
        /// </summary>
        public byte GalleryID { get; set; }

        /// <summary>
        /// Limite de galeria ou modo multiplayer do Pangya (padronizado como 30 / 0x1E).
        /// </summary>
        public byte GalleryLimite { get; set; }

        /// <summary>
        /// Obtém ou define a quantidade de buracos (Holes) configurados para a partida.
        /// </summary>
        public byte HoleCount { get; set; }

        /// <summary>
        /// Tipo de sala exibido no pacote de rede (pode variar conforme o tipo real, como Camp, VS ou Especial).
        /// </summary>
        public byte RoomType { get; set; }

        /// <summary>
        /// Obtém ou define o identificador numérico interno da sala (Room ID). Padrão é -1.
        /// </summary>
        public short RoomID { get; set; }

        /// <summary>
        /// Modo dos buracos, front, back e etc...
        /// </summary>
        public byte HoleMode { get; set; }

        /// <summary>
        /// Obtém ou define o curso/mapa selecionado para a partida (<see cref="RoomCourseFlags"/>).
        /// </summary>
        public RoomCourseFlags CourseIndex { get; set; }

        /// <summary>
        /// Tempo limite em segundos da partida ou turno.
        /// </summary>
        public uint TimeSec { get; set; }

        /// <summary>
        /// Tempo limite em minutos da partida.
        /// </summary>
        public uint TimeMin { get; set; }

        /// <summary>
        /// Identificador do troféu da sala (Trophy ID).
        /// </summary>
        public uint TrophyID { get; set; }

        /// <summary>
        /// Flag especial da sala. Em salas de 100 jogadores ou eventos de GM, costuma assumir o valor 0x100.
        /// </summary>
        public short SpecialFlag { get; set; }

        /// <summary>
        /// Informações de batalha de guildas na sala (<see cref="RoomGuildBattleInfo"/>).
        /// </summary>
        public RoomGuildBattleInfo GuildBattle { get; set; }

        /// <summary>
        /// Taxa multiplicadora de ganho de Pangs na sala.
        /// </summary>
        public uint RatePangs { get; set; }

        /// <summary>
        /// Taxa multiplicadora de ganho de Experiência na sala.
        /// </summary>
        public uint RateExperience { get; set; }

        /// <summary>
        /// Indica se a sala é controlada ou iniciada por um Game Master (1 = Sim, 0 = Não).
        /// </summary>
        public byte IsGameMaster { get; set; }

        /// <summary>
        /// UID do dono/criador da sala (Owner UID). Pode assumir valores negativos em modos específicos como Grand Prix.
        /// </summary>
        public int OwnerUID { get; set; }

        /// <summary>
        /// Flag de sala estendida que define com precisão o tipo real da sala.
        /// </summary>
        public byte SpecialRoomFLag { get; set; }

        /// <summary>
        /// ID do artefato ou item especial utilizado para efeitos visuais/mecânicos no Grand Prix.
        /// </summary>
        public uint ItemIDArtifact { get; set; }

        /// <summary>
        /// Configuração de modo especial da sala, como Short Game ou Natural Mode (<see cref="SpecialModeFlag"/>).
        /// </summary>
        public SpecialModeFlag SpecialModeRoom { get; set; }

        /// <summary>
        /// Informações detalhadas do modo Grand Prix (<see cref="RoomGrandPrixInfo"/>).
        /// </summary>
        public RoomGrandPrixInfo grand_prix { get; set; }

        /// <summary>
        /// Senha em texto plano da sala (Informação estendida).
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Tipo real da sala (Informação estendida).
        /// </summary>
        public byte RealRoomType { get; set; }

        /// <summary>
        /// Número do buraco (Hole) que será repetido na partida.
        /// </summary>
        public byte IDHoleRepeted { get; set; }

        /// <summary>
        /// Define se a posição do buraco/pino (Pin/Beam) é fixa ou aleatória: 1 = Fixo, 0 = Aleatório.
        /// </summary>
        public uint HoleFixed { get; set; }

        /// <summary>
        /// Estado ausente/ocioso (AFK/Sleep) da sala, utilizado para gerenciar o início tardio da partida.
        /// </summary>
        public byte StateSleep { get; set; }

        /// <summary>
        /// Indica se a sala foi criada em um canal para iniciantes (Channel Rookie).
        /// </summary>
        public bool IsChannelRookie { get; set; }

        /// <summary>
        /// Indica se o evento de anjo desistente (Angel Quiter Event) está ativado.
        /// </summary>
        public bool IsAngelQuiterEvent { get; set; }

        /// <summary>
        /// Obtém o tipo de buraco convertendo a propriedade <see cref="HoleMode"/> para o enum <see cref="RoomHoleType"/>.
        /// </summary>
        /// <returns>O tipo de buraco da sala.</returns>
        public RoomHoleType GetHoleType()
        {
            return (RoomHoleType)HoleMode;
        }

        /// <summary>
        /// Obtém o identificador numérico do mapa/curso extraído de <see cref="CourseIndex"/>.
        /// </summary>
        /// <returns>O byte correspondente ao mapa.</returns>
        public byte GetMap()
        {
            return Convert.ToByte(CourseIndex & RoomCourseFlags.UNK);
        }

        /// <summary>
        /// Obtém o tipo real da sala convertendo <see cref="RealRoomType"/> para o enum <see cref="RoomTypeFlags"/>.
        /// </summary>
        /// <returns>As flags de tipo de sala.</returns>
        public RoomTypeFlags GetRoomType()
        {
            return (RoomTypeFlags)RealRoomType;
        }

        /// <summary>
        /// Serializa todas as propriedades e subestruturas da sala em um array de bytes binário, 
        /// preparando o pacote padrão de informações da sala para envio via rede.
        /// </summary>
        /// <returns>Um array de bytes contendo o stream binário da sala.</returns>
        public byte[] ToArray()
        {
            using var bw = new Packet();
            bw.WriteString(Name, 64);
            bw.WriteByte(IsPublicRoom);
            bw.WriteByte(StateRoom);
            bw.WriteByte(FlagRoom);
            bw.WriteByte(MaxUsers);
            bw.WriteByte(CurrentUsers);
            bw.WriteBytes(GameKey, 16);
            bw.WriteByte(GalleryID);
            bw.WriteByte(GalleryLimite);
            bw.WriteByte(HoleCount);
            bw.WriteByte(RoomType);
            bw.WriteInt16(RoomID);
            bw.WriteByte(HoleMode);
            bw.WriteByte((byte)CourseIndex);
            bw.WriteUInt32(TimeSec);
            bw.WriteUInt32(TimeMin);
            bw.WriteUInt32(TrophyID);
            bw.WriteInt16(SpecialFlag); 
            bw.WriteBytes((GuildBattle??= new RoomGuildBattleInfo()).ToArray()); 
            bw.WriteUInt32(RatePangs);
            bw.WriteUInt32(RateExperience); 
            bw.WriteByte(IsGameMaster);
            bw.WriteInt32(OwnerUID); 
            bw.WriteByte(SpecialRoomFLag);
            bw.WriteUInt32(ItemIDArtifact); 
            bw.WriteUInt32((SpecialModeRoom??= new SpecialModeFlag()).Value); 
            bw.WriteBytes((grand_prix??= new RoomGrandPrixInfo()).ToArray()); 
            return bw.GetBytes;
        }

        /// <summary>
        /// Serializa uma versão estendida/resumida das informações da sala (<see cref="ToArrayEx"/>) 
        /// contendo dados específicos de configuração rápida para listagem ou atualização de status.
        /// </summary>
        /// <returns>Um array de bytes contendo o stream estendido da sala.</returns>
        public byte[] ToArrayEx()
        {
            using (var p = new Packet())
            {
                p.WriteByte(RoomType);
                p.WriteByte(GetMap());
                p.WriteByte(HoleCount);
                p.WriteByte(HoleMode);

                if (IDHoleRepeted > 0 || GetHoleType() == RoomHoleType.M_REPEAT)
                {
                    p.WriteByte(IDHoleRepeted);
                    p.WriteUInt32(HoleFixed);
                }

                p.WriteUInt32(SpecialModeRoom.Value);
                p.WriteByte(MaxUsers);
                p.WriteByte(GalleryLimite); // Constante de 30 do Pangya
                p.WriteByte((byte)(SpecialFlag & 0xFF));
                p.WriteUInt32(TimeSec);
                p.WriteUInt32(TimeMin);
                p.WriteUInt32(TrophyID);
                p.WriteByte(IsPublicRoom); // Flag de senha
                p.WriteString(Name);

                return p.GetBytes;
            }
        }

        /// <summary>
        /// Retorna uma representação formatada em texto legível (<see cref="string"/>) contendo todos os dados e estados atuais da sala.
        /// </summary>
        /// <returns>Uma string estruturada com as propriedades da sala.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("GameRoomInfoModel {");
            sb.AppendLine($"  Name = \"{Name}\"");
            sb.AppendLine($"  IsPublicRoom = {IsPublicRoom} ({(IsPublicRoom == 1 ? "Sem Senha" : "Com Senha")})");
            sb.AppendLine($"  StateRoom = {StateRoom} ({(StateRoom == 1 ? "Espera" : "Em Jogo")})");
            sb.AppendLine($"  type = {FlagRoom}");
            sb.AppendLine($"  max_users = {MaxUsers}");
            sb.AppendLine($"  players = {CurrentUsers}");
            sb.AppendLine($"  GameKey = {BitConverter.ToString(GameKey)}");
            sb.AppendLine($"  GalleryID = {GalleryID}");
            sb.AppendLine($"  GalleryLimite = {GalleryLimite}");
            sb.AppendLine($"  HoleCount = {HoleCount}");
            sb.AppendLine($"  RoomType = {RoomType}");
            sb.AppendLine($"  RoomID = {RoomID}");
            sb.AppendLine($"  HoleMode = {HoleMode} ({GetHoleType()})");
            sb.AppendLine($"  CourseIndex = {(byte)CourseIndex} ({CourseIndex})");
            sb.AppendLine($"  TimeSec = {TimeSec}");
            sb.AppendLine($"  TimeMin = {TimeMin}");
            sb.AppendLine($"  TrophyID = {TrophyID}");
            sb.AppendLine($"  State = {SpecialFlag}");
            sb.AppendLine($"  GuildBattle = {GuildBattle}");
            sb.AppendLine($"  RatePangs = {RatePangs}");
            sb.AppendLine($"  RateExperience = {RateExperience}");
            sb.AppendLine($"  IsGameMaster = {IsGameMaster}");
            sb.AppendLine($"  Master = {OwnerUID}");
            sb.AppendLine($"  tipo_ex = {SpecialRoomFLag}");
            sb.AppendLine($"  artefato = {ItemIDArtifact}");
            sb.AppendLine($"  NaturalMode = {SpecialModeRoom}");
            sb.AppendLine($"  GrandPrixMode = {grand_prix}");
            sb.Append("}");

            return sb.ToString();
        }
    }
}