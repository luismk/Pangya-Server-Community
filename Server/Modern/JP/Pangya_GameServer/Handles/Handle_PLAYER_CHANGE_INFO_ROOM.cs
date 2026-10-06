using Pangya_GameServer.Engine;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_INFO_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {

                var _channel = Player.GetChannel();

                if (_channel == null)
                    throw new exception("[Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou trocar info da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas a sala nao esta em um canal.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        10, 0));


                var room = Player.GetRoom();

                if (room == null) 
                    throw new exception("[Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] Channel[ID=" + +Player.GetChannel().getId() + "] tentou trocar info da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas a sala nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        10, 0));


                if (room.GameRun())
                {
                    throw new exception("[Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] Channel[ID=" + +Player.GetChannel().getId() + "] tentou trocar info da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas a sala ja foi iniciada.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                      10, 0));
                }

                if (room.GetTipo() == RoomTypeFlags.LOUNGE)
                {
                    throw new exception("[Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] Channel[ID=" + +Player.GetChannel().getId() + "] tentou trocar info da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas a sala nao contem essa funcao.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    10, 0));
                }
                 
                byte num_info;
                short roomId;

                if (room.GetMaster() != Player.UserInfo.UID)
                {
                    if (!Player.UserInfo.UserCapabilities.IsGameMaster)
                        throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou trocar o info da sala[NUMERO=" + room.GetRoomId() + ", MASTER=" + Convert.ToString(room.GetMaster()) + "], mas nao pode trocar o info da sala sem ser Master.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        11, 0));
                }

                roomId = Packet.ReadInt16();
                num_info = Packet.ReadByte();

                if (num_info <= 0)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou trocar o info da sala[NUMERO=" + room.GetRoomId() + ", MASTER=" + Convert.ToString(room.GetMaster()) + "], mas nao tem nenhum info para trocar do buffer do cliente.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        8, 0));
                }

                for (var i = 0; i < num_info; ++i)
                {
                    var type = (ROOM_INFO_CHANGE)Packet.ReadByte();

                    switch (type)
                    {
                        case ROOM_INFO_CHANGE.NAME:
                            {
                                var title = Packet.ReadString();

                                if ((room.GetTipo() == RoomTypeFlags.PRACTICE || room.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE) && title.CompareTo("Single Player Practice Mode") != 0)
                                    room.SetNome("Single Player Practice Mode");
                                else
                                    room.SetNome(title);
                            }
                            break;
                        case ROOM_INFO_CHANGE.SENHA:
                            {
                                var pwd = Packet.ReadString();

                                if (!string.IsNullOrEmpty(pwd) && pwd.Length > 8 && (room.GetTipo() == RoomTypeFlags.PRACTICE || room.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE))
                                    ThrowHackException(Player, "tamanho da str da Password na sala inválida: " + room.GetTipo());

                                room.SetSenha(pwd);
                            }
                            break;
                        case ROOM_INFO_CHANGE.TIPO:
                            {
                                var T8 = Packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomTypeFlags), T8))
                                {
                                    room.SetType(T8);
                                }
                                else
                                    ThrowHackException(Player, "falha ao setar o Type: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.COURSE:
                            {
                                var T8 = Packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomCourseFlags), T8))
                                {
                                    room.SetCourse(T8);
                                }
                                else
                                    ThrowHackException(Player, "falha ao setar o CourseIndex: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.QNTD_HOLE:
                            room.SetQntdHole(Packet.ReadByte());
                            break;
                        case ROOM_INFO_CHANGE.MODO:
                            {
                                var T8 = Packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomHoleType), T8))
                                {
                                    room.SetModo(T8);
                                }
                                else
                                    ThrowHackException(Player, "falha ao setar o Modo: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.TEMPO_VS:
                            {
                                var timevs = (uint)Packet.ReadUInt16();
                                if (timevs > 0)
                                    room.SetStrokeTime(timevs * 1000);
                                else
                                    ThrowHackException(Player, "falha ao setar o tempo vs: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.MAX_PLAYER:
                            {
                                var T8 = Packet.ReadByte();
                                if (!(T8 <= room.Players.Count))
                                    room.SetMaxUsers(T8);
                                else
                                    ThrowHackException(Player, "falha ao setar o RoomID de maximo de Players: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.TEMPO_30S:
                            {
                                var time30s = (uint)Packet.ReadByte();
                                if (time30s > 0)
                                    room.SetTime30S(time30s * 60000);
                                else
                                    ThrowHackException(Player, "falha ao setar o tempo minutos: " + room.GetTipo());
                            }
                            break;
                        case ROOM_INFO_CHANGE.STATE_FLAG:
                            room.SetStateAFK(Packet.ReadByte());
                            break;
                        case ROOM_INFO_CHANGE.GALLERY_LIMIT:
                            room.SetGalleryLimit(Packet.ReadByte()); 
                            break;
                        case ROOM_INFO_CHANGE.HOLE_REPEAT:
                            room.SetHoleRepeted(Packet.ReadByte());
                            break;
                        case ROOM_INFO_CHANGE.FIXED_HOLE:
                            room.SetFixedHole(Packet.ReadUInt32());
                            break;
                        case ROOM_INFO_CHANGE.ARTEFATO:
                            room.SetArtefato(Packet.ReadUInt32());
                            break;
                        case ROOM_INFO_CHANGE.NATURAL:
                            {
                                var value = Packet.ReadUInt32();
                                var natural = new SpecialModeFlag(value);

                                if (!natural.IsNaturalMode && GameServer.Instance.getInfo().Property.NaturalMode)
                                {
                                    natural.IsNaturalMode = true;
                                }
                                room.SetNatural(natural.Value);
                                break;
                            }
                        default:
                            throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou trocar info da sala[NUMERO=" + room.GetRoomId() + ", MASTER=" + Convert.ToString(room.GetMaster()) + "], mas info change é desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                9, 0));
                    }
                }

                HandleChangeRoom(Player, room?.GetInfo(), _channel?.getInfo());

                room.SendHeadRoom();
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHANGE_INFO_ROOM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        #region HANDLE CHANGE ROOM - Validations for each field change, based on room type and channel info. If any validation fails, a hack exception is thrown, marking the Player as a cheater.
        /// <summary>
        /// so vai ser permitido, se passar pelas verificacoes.
        /// </summary>
        /// <param Name="session"></param>
        /// <param Name="ri"></param>
        /// <param Name="_ChannelInfo"></param>
        private void HandleChangeRoom(Player session, GameRoomInfoModel ri, ChannelInfo? _ChannelInfo)
        {
            try
            {
                if (session == null || !Player.getState())
                    ThrowHackException(Player, "Sessão inexistente ou desconectada");

                if (_ChannelInfo == null)
                    ThrowHackException(Player, "Informacões do canal inexistente");

                if (ri == null)
                    ThrowHackException(Player, "Informacões da sala inexistente"); 

                ValidateRoomName(Player, ri);
                ValidateRoomPass(Player, ri);
                ValidateRoomCreate(Player, ri);
                ValidateMaxPlayers(Player, ri);
                ValidateRoomTime(Player, ri, _ChannelInfo);
                ValidateHoleCount(Player, ri);
                ValidateForbiddenModes(Player, ri);

                switch (ri.GetRoomType())
                {
                    case RoomTypeFlags.STROKE:
                        ValidateStrokeSpecific(Player, ri);
                        break;
                    case RoomTypeFlags.PANG_BATTLE:
                        ValidatePangBattleSpecific(Player, ri);
                        break;
                    case RoomTypeFlags.MATCH: // se MATCH corresponde ao VS/Approach no seu enum
                    case RoomTypeFlags.APPROCH:
                        ValidateVsApproach(Player, ri, _ChannelInfo);
                        break;
                    case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                        ValidateShuffleSpecific(Player, ri);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                Player.SetReason(PangyaAPI.Network.Flags.CloseReason.Cheating);//marcar como cheat.
                throw;
            }
        }

        private void ValidateRoomTime(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            switch (ri.GetRoomType())
            {
                case RoomTypeFlags.STROKE:
                    if (ri.HoleCount == 3 || ri.HoleCount == 6 || ri.HoleCount == 9 || ri.HoleCount == 18)
                    {
                        ValidateTimeVs(Player, ri, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(Player, $"TimeSec inválido: {ri.TimeSec}");
                    break;

                case RoomTypeFlags.MATCH:
                case RoomTypeFlags.PANG_BATTLE:
                    if (ri.HoleCount == 6 || ri.HoleCount == 9 || ri.HoleCount == 18)
                    {
                        ValidateTimeVs(Player, ri, 30, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(Player, $"TimeSec inválido: {ri.TimeSec}");
                    break;
                case RoomTypeFlags.PRACTICE:
                case RoomTypeFlags.TOURNEY:
                    // Tournament: pode ter ShortGame / NaturalMode branches; TimeMin used (ms)
                    if (ri.SpecialModeRoom != null)
                    {
                        if (ri.SpecialModeRoom.IsShotMode)
                        {
                            if (ri.HoleCount == 9 || ri.HoleCount == 18)
                                ValidateTime30s(Player, ri, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
                        }
                        else if (ri.SpecialModeRoom.IsNaturalMode)
                        {
                            if (ri.HoleCount == 9)
                                ValidateTime30s(Player, ri, 15, 30, 20, 25, 35);
                            else if (ri.HoleCount == 18)
                                ValidateTime30s(Player, ri, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
                        }
                    }
                    else
                    {
                        if (ri.HoleCount == 9)
                            ValidateTime30s(Player, ri, 15, 20, 25, 30);
                        else if (ri.HoleCount == 18)
                            ValidateTime30s(Player, ri, 35, 40, 45, 50, 55);
                        else
                            ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    }
                    break;

                case RoomTypeFlags.GUILD_BATTLE:
                    if (ri.HoleCount == 9)
                        ValidateTime30s(Player, ri, 15, 20, 25, 30);
                    else if (ri.HoleCount == 18)
                        ValidateTime30s(Player, ri, 35, 40, 45, 50, 55);
                    else
                        ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    break;
                case RoomTypeFlags.APPROCH:
                    if (ri.HoleCount == 3 || ri.HoleCount == 6 || ri.HoleCount == 9)
                        ValidateTime30s(Player, ri, 40);
                    else
                        ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 1000}");
                    break;

                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                    if (ri.HoleCount == 18)
                        ValidateTime30s(Player, ri, 40);
                    else
                        ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    break;

                default:
                    break;
            }
        }

        // --------- Stroke specific checks (shotTime, Modo for 18H, holes set) ----------
        private void ValidateStrokeSpecific(Player session, GameRoomInfoModel ri)
        {
            // hole count already validado em ValidateHoleCount; validar shotTime (TimeSec) em segundos permitidos
            uint[] allowedShotSeconds = [40, 60, 120, 300];
            if (!allowedShotSeconds.Contains(ri.TimeSec / 1000))
                ThrowHackException(Player, "ShotTime inválido (Stroke)");

            // Se 18 holes então Modo deve ser 0 ou 3
            if (ri.HoleCount == 18)
            {
                if (ri.HoleMode != 0 && ri.HoleMode != 3)
                    ThrowHackException(Player, "Modo inválido no Stroke 18H");
            }
        }

        // --------- Pang Battle specific ----------
        private void ValidatePangBattleSpecific(Player session, GameRoomInfoModel ri)
        {
            // Modo valid
            if (ri.HoleMode != 0 && ri.HoleMode != 3)
                ThrowHackException(Player, "Modo inválido no Pang Battle");

            // shotTime valid (seconds)
            uint[] allowedShotSeconds = [30, 40, 60, 120, 300];
            if (!allowedShotSeconds.Contains(ri.TimeSec / 1000))
                ThrowHackException(Player, "ShotTime inválido no Pang Battle");
        }

        // --------- VS / APPROACH (game type 4 / 5) ----------
        private void ValidateVsApproach(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            if (ri.GetRoomType() == RoomTypeFlags.MATCH)
            {
                // gameTimeLimit checks (valores em milissegundos como no C++)
                if (ri.HoleCount == 9)
                {
                    uint[] allowed = [900000, 1200000, 1500000, 1800000];
                    if (!allowed.Contains(ri.TimeSec))
                        ThrowHackException(Player, "gameTimeLimit inválido para 9H");
                }
                else if (ri.HoleCount == 18)
                {
                    uint[] allowed = [1800000, 2100000, 2400000, 2700000, 3000000];
                    if (!allowed.Contains(ri.TimeSec))
                        ThrowHackException(Player, "gameTimeLimit inválido para 18H");

                    if (ri.HoleMode != 0 && ri.HoleMode != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/HoleMode'
                        ThrowHackException(Player, "Modo inválido em 18H");
                }
                else if (ri.HoleCount == 6)
                {
                    uint[] allowed = [1800000, 2100000, 2400000, 2700000, 40000];
                    if (!allowed.Contains(ri.TimeSec))
                        ThrowHackException(Player, "gameTimeLimit inválido para 6H");

                    if (ri.HoleMode != 0 && ri.HoleMode != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/HoleMode'
                        ThrowHackException(Player, "Modo inválido em 6");
                }

                else
                    ThrowHackException(Player, "HoleNum inválido para Match");

                // UserLimit válido? (4,10,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = [4, 10, 20, 30];
                if (!allowedPlayers.Contains(ri.MaxUsers) && !Player.UserInfo.UserCapabilities.IsGameMaster)
                    ThrowHackException(Player, "UserLimit inválido no Match");
            }
            else
            {
                // gameTimeLimit checks (valores em milissegundos como no C++)
                if (ri.HoleCount == 3 || ri.HoleCount == 6 || ri.HoleCount == 9)
                {
                    uint[] allowed = [40000];
                    if (!allowed.Contains(ri.TimeMin))
                        ThrowHackException(Player, "gameTimeLimit inválido para 9H");
                }
                else
                    ThrowHackException(Player, "HoleNum inválido para Approach");

                // UserLimit válido? (4,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = [6, 20, 30];
                if (!allowedPlayers.Contains(ri.MaxUsers) && !Player.UserInfo.UserCapabilities.IsGameMaster)
                    ThrowHackException(Player, "UserLimit inválido Approach");
            }

            // Canal Normal não permite Modo aleatório (random) — apenas Modo == 3 é permitido para "random"
            if (m_ci != null && m_ci.type.all && ri.HoleMode != 3)
                ThrowHackException(Player, "Random Modo proibido no canal Normal");
        }

        // --------- Shuffle (Type 6) ----------
        private void ValidateShuffleSpecific(Player session, GameRoomInfoModel ri)
        {
            int[] allowedHoles = [18];
            if (!allowedHoles.Contains(ri.HoleCount))
                ThrowHackException(Player, "HoleNum inválido no Shuffle");

            int[] allowedPlayers = [30];
            if (!allowedPlayers.Contains(ri.MaxUsers))
                ThrowHackException(Player, "UserLimit inválido no Shuffle");

            uint[] allowedTime = [2400000 / 60000];
            if (!allowedTime.Contains(ri.TimeMin / 60000))
                ThrowHackException(Player, "TimeMin inválido no Shuffle");

            if (ri.CourseIndex != RoomCourseFlags.RANDOM)
                ThrowHackException(Player, "Course/Map inválido no Shuffle");

            if (ri.HoleMode != 0 && ri.HoleMode != 5)
                ThrowHackException(Player, "Modo inválido no Shuffle");
        }

        // --------- ValidateRoomName ----------
        private void ValidateRoomName(Player session, GameRoomInfoModel ri)
        {
            bool _check = true;
            switch (ri.GetRoomType())
            {
                case RoomTypeFlags.STROKE:
                case RoomTypeFlags.MATCH:
                case RoomTypeFlags.TOURNEY:
                case RoomTypeFlags.TOURNEY_TEAM:
                case RoomTypeFlags.GUILD_BATTLE:
                case RoomTypeFlags.APPROCH:
                case RoomTypeFlags.PANG_BATTLE:
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                case RoomTypeFlags.LOUNGE:
                    if (string.IsNullOrEmpty(ri.Name))
                        _check = false;
                    break;
                case RoomTypeFlags.PRACTICE:
                    if (!string.IsNullOrEmpty(ri.Name) && ri.Name.CompareTo("Single Player Practice Mode") != 0)
                        _check = false;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(Player, "Nome da sala inválido: " + ri.GetRoomType());
        }

        // --------- ValidateRoomPass ----------
        private void ValidateRoomPass(Player session, GameRoomInfoModel ri)
        {
            if (ri.GetRoomType() == RoomTypeFlags.PRACTICE || ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
            {
                if (!string.IsNullOrEmpty(ri.Password) && ri.Password.Length < 8 && !ri.Password.Contains("MDA"))
                    ThrowHackException(Player, "tamanho da str da Password na sala inválida: " + ri.GetRoomType());
            }
            else
            {
                if (!string.IsNullOrEmpty(ri.Password) && ri.Password.Length > 14)
                    ThrowHackException(Player, "tamanho da str da Password na sala inválida: " + ri.GetRoomType());
            }
        }

        private void ValidateRoomCreate(Player session, GameRoomInfoModel ri)
        {
            bool _check;
            switch (ri.GetRoomType())
            {
                case RoomTypeFlags.STROKE:
                case RoomTypeFlags.MATCH:
                case RoomTypeFlags.TOURNEY:
                case RoomTypeFlags.TOURNEY_TEAM:
                case RoomTypeFlags.GUILD_BATTLE:
                case RoomTypeFlags.APPROCH:
                case RoomTypeFlags.PANG_BATTLE:
                case RoomTypeFlags.GRAND_PRIX:
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                case RoomTypeFlags.PRACTICE:
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                case RoomTypeFlags.LOUNGE:
                    _check = true;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(Player, "Tipo de jogo inválido: " + ri.GetRoomType());
        }

        private void ValidateMaxPlayers(Player session, GameRoomInfoModel ri)
        {
            int[] allowedPlayers;

            switch (ri.GetRoomType())
            {
                case RoomTypeFlags.STROKE:
                    allowedPlayers = [2, 3, 4];
                    break;
                case RoomTypeFlags.MATCH:
                    allowedPlayers = [2, 4];
                    break;
                case RoomTypeFlags.TOURNEY:
                case RoomTypeFlags.TOURNEY_TEAM:
                case RoomTypeFlags.GUILD_BATTLE:
                    allowedPlayers = [10, 20, 30];
                    break;
                case RoomTypeFlags.APPROCH:
                    allowedPlayers = [6, 20, 30];
                    break;
                case RoomTypeFlags.PANG_BATTLE:
                    allowedPlayers = [2, 4];
                    break;
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                case RoomTypeFlags.GRAND_ZODIAC_ADV:
                case RoomTypeFlags.GRAND_ZODIAC_INT:
                case RoomTypeFlags.PRACTICE:
                    allowedPlayers = [1];
                    break;
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                    allowedPlayers = [30];
                    break;
                case RoomTypeFlags.LOUNGE:
                    allowedPlayers = [10, 20, 30];
                    break;
                default:
                    allowedPlayers = Array.Empty<int>();
                    break;
            }

            if (allowedPlayers.Length > 0 && Array.IndexOf(allowedPlayers, ri.MaxUsers) == -1)
                ThrowHackException(Player, "MaxUsers inválido: " + ri.MaxUsers);
        }

        private void ValidateHoleCount(Player session, GameRoomInfoModel ri)
        {
            int[] allowedHoles = Array.Empty<int>();
            switch (ri.GetRoomType())
            {
                case RoomTypeFlags.STROKE:
                    allowedHoles = [3, 6, 9, 18];
                    break;
                case RoomTypeFlags.MATCH:
                    allowedHoles = [6, 9, 18];
                    break;
                case RoomTypeFlags.TOURNEY:
                case RoomTypeFlags.TOURNEY_TEAM:
                case RoomTypeFlags.GUILD_BATTLE:
                    allowedHoles = [9, 18];
                    break;
                case RoomTypeFlags.APPROCH:
                    allowedHoles = [3, 6, 9];
                    break;
                case RoomTypeFlags.PANG_BATTLE:
                    allowedHoles = [6, 9, 18];
                    break;
                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                    allowedHoles = [18];
                    break;
                case RoomTypeFlags.GRAND_ZODIAC_PRACTICE:
                case RoomTypeFlags.GRAND_ZODIAC_ADV:
                case RoomTypeFlags.GRAND_ZODIAC_INT:
                    allowedHoles = [1];
                    break;
                case RoomTypeFlags.PRACTICE:
                    allowedHoles = [1, 9, 18];
                    break;
                case RoomTypeFlags.LOUNGE://limite e 18, eu acho
                    allowedHoles = [1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18];
                    break;
            }

            if (allowedHoles.Length > 0 && Array.IndexOf(allowedHoles, ri.HoleCount) == -1)
                ThrowHackException(Player, "HoleCount inválido: " + ri.HoleCount);
        }

        private void ValidateForbiddenModes(Player session, GameRoomInfoModel ri)
        {
            if (ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_INT || ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_ADV)
            {
                ThrowHackException(Player, "tentou criar HoleMode proibido");
            }
        }

        private void ValidateTime30s(Player session, GameRoomInfoModel ri, params uint[] allowedMinutes)
        {
            if (ri.GetRoomType() == RoomTypeFlags.APPROCH)//unico com time minute em segundos
            {
                if (ri.TimeMin < (40 * 1000))
                    ThrowHackException(Player, $"TimeMin inválido para o Approach: {ri.TimeMin}");

                if (!allowedMinutes.Contains(ri.TimeMin / 1000))
                    ThrowHackException(Player, $"TimeMin inválido para o Approach: {ri.TimeMin / 1000}");
            }
            else
            {
                if (ri.TimeMin < (15 * 60000))
                    ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");

                if (!allowedMinutes.Contains(ri.TimeMin / 60000))
                    ThrowHackException(Player, $"TimeMin inválido: {ri.TimeMin / 60000}");
            }
        }

        private void ValidateTimeVs(Player session, GameRoomInfoModel ri, params uint[] allowedSeconds)
        {
            if ((ri.GetRoomType() == RoomTypeFlags.STROKE || ri.GetRoomType() == RoomTypeFlags.MATCH)
               && ri.TimeSec < (40 * 1000))
            {
                ThrowHackException(Player, $"TimeSec inválido: {ri.TimeSec}");
            }

            if (!allowedSeconds.Contains(ri.TimeSec / 1000))
            {
                ThrowHackException(Player, $"TimeSec inválido: {ri.TimeSec}");
            }
        }

        // --------- ThrowHackException ----------
        public void ThrowHackException(Player session, string motivo)
        {
            var ri = Player.GetRoom();

            string msg = $"[Room::ThrowHackException] [Error] Normal [UID={Player.UserInfo.UID}] " +
                         $"Channel[ID={ri.GetChannelId()}] tentou criar sala [Nome={ri.getName()}, PWD={ri.getPass()}, TIPO={ri.GetTipo()}], {motivo}. Hacker ou Bug";

            throw new exception(msg, ExceptionError.STDA_MAKE_ERROR_TYPE(
                STDA_ERROR_TYPE.ROOM, 10, 0x770001));
        }
        #endregion
    }
}