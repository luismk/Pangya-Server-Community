using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Session;
using PangyaAPI.Utilities;
using System;
using System.Linq;
namespace Pangya_GameServer.Engine
{
    /// <summary>
    /// class handle for modes game, check, revision data, +++++
    /// idea: Luiz Lopes
    /// luizinrc@hotmail.com
    /// </summary>
    public class FilterHacker
    {
        public FilterHacker() { }

        public void HandleRoom(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            try
            {
                if (session == null || !session.getState())
                    ThrowHackException(session, ri, m_ci, "Sessão inexistente ou desconectada");

                if (m_ci == null)
                    ThrowHackException(session, ri, m_ci, "Informacões do canal inexistente");

                if (session.UserInfo.UserCapabilities.IsGameMaster)
                    return;

                ValidateRoomName(session, ri, m_ci);
                ValidateRoomPass(session, ri, m_ci);
                ValidateRoomCreate(session, ri, m_ci);
                ValidateMaxPlayers(session, ri, m_ci);
                ValidateRoomTime(session, ri, m_ci);
                ValidateHoleCount(session, ri, m_ci);
                ValidateForbiddenModes(session, ri, m_ci);

                switch (ri.GetRoomType())
                {
                    case RoomTypeFlags.STROKE:
                        ValidateStrokeSpecific(session, ri, m_ci);
                        break;
                    case RoomTypeFlags.PANG_BATTLE:
                        ValidatePangBattleSpecific(session, ri, m_ci);
                        break;
                    case RoomTypeFlags.MATCH: // se MATCH corresponde ao VS/Approach no seu enum
                    case RoomTypeFlags.APPROCH:
                        ValidateVsApproach(session, ri, m_ci);
                        break;
                    case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                        ValidateShuffleSpecific(session, ri, m_ci);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                session.SetReason(PangyaAPI.Network.Flags.CloseReason.Cheating);//marcar como cheat.
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
                        ValidateTimeVs(session, ri, m_ci, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"TimeSec inválido: {ri.TimeSec}");
                    break;

                case RoomTypeFlags.MATCH:
                case RoomTypeFlags.PANG_BATTLE:
                    if (ri.HoleCount == 6 || ri.HoleCount == 9 || ri.HoleCount == 18)
                    {
                        ValidateTimeVs(session, ri, m_ci, 30, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"TimeSec inválido: {ri.TimeSec}");
                    break;
                case RoomTypeFlags.PRACTICE:
                case RoomTypeFlags.TOURNEY:
                    // Tournament: pode ter ShortGame / NaturalMode branches; TimeMin used (ms)
                    if (ri.SpecialModeRoom != null)
                    {
                        if (ri.SpecialModeRoom.IsShotMode)
                        {
                            if (ri.HoleCount == 9 || ri.HoleCount == 18)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
                        }
                        else if (ri.SpecialModeRoom.IsNaturalMode)
                        {
                            if (ri.HoleCount == 9)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else if (ri.HoleCount == 18)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
                        }
                    }
                    else
                    {
                        if (ri.HoleCount == 9)
                            ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                        else if (ri.HoleCount == 18)
                            ValidateTime30s(session, ri, m_ci, 35, 40, 45, 50, 55);
                        else
                            ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    }
                    break;

                case RoomTypeFlags.GUILD_BATTLE:
                    if (ri.HoleCount == 9)
                        ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                    else if (ri.HoleCount == 18)
                        ValidateTime30s(session, ri, m_ci, 35, 40, 45, 50, 55);
                    else
                        ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    break;
                case RoomTypeFlags.APPROCH:
                    if (ri.HoleCount == 3 || ri.HoleCount == 6 || ri.HoleCount == 9)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 1000}");
                    break;

                case RoomTypeFlags.SPECIAL_SHUFFLE_COURSE:
                    if (ri.HoleCount == 18)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
                    break;

                default:
                    break;
            }
        }

        // --------- Stroke specific checks (shotTime, Modo for 18H, holes set) ----------
        private void ValidateStrokeSpecific(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            // hole count already validado em ValidateHoleCount; validar shotTime (TimeSec) em segundos permitidos
            uint[] allowedShotSeconds = [40, 60, 120, 300];
            if (!allowedShotSeconds.Contains(ri.TimeSec / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido (Stroke)");

            // Se 18 holes então Modo deve ser 0 ou 3
            if (ri.HoleCount == 18)
            {
                if (ri.HoleMode != 0 && ri.HoleMode != 3)
                    ThrowHackException(session, ri, m_ci, "Modo inválido no Stroke 18H");
            }
        }

        // --------- Pang Battle specific ----------
        private void ValidatePangBattleSpecific(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            // Modo valid
            if (ri.HoleMode != 0 && ri.HoleMode != 3)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Pang Battle");

            // shotTime valid (seconds)
            uint[] allowedShotSeconds = [30, 40, 60, 120, 300];
            if (!allowedShotSeconds.Contains(ri.TimeSec / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido no Pang Battle");
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
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
                }
                else if (ri.HoleCount == 18)
                {
                    uint[] allowed = [1800000, 2100000, 2400000, 2700000, 3000000];
                    if (!allowed.Contains(ri.TimeSec))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 18H");

                    if (ri.HoleMode != 0 && ri.HoleMode != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/HoleMode'
                        ThrowHackException(session, ri, m_ci, "Modo inválido em 18H");
                }
                else if (ri.HoleCount == 6)
                {
                    uint[] allowed = [1800000, 2100000, 2400000, 2700000, 40000];
                    if (!allowed.Contains(ri.TimeSec))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 6H");

                    if (ri.HoleMode != 0 && ri.HoleMode != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/HoleMode'
                        ThrowHackException(session, ri, m_ci, "Modo inválido em 6");
                }

                else
                    ThrowHackException(session, ri, m_ci, "HoleNum inválido para Match");

                // UserLimit válido? (4,10,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = [4, 10, 20, 30];
                if (!allowedPlayers.Contains(ri.MaxUsers) && !session.UserInfo.UserCapabilities.IsGameMaster)
                    ThrowHackException(session, ri, m_ci, "UserLimit inválido no Match");
            }
            else
            {
                // gameTimeLimit checks (valores em milissegundos como no C++)
                if (ri.HoleCount == 3 || ri.HoleCount == 6 || ri.HoleCount == 9)
                {
                    uint[] allowed = [40000];
                    if (!allowed.Contains(ri.TimeMin))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
                }
                else
                    ThrowHackException(session, ri, m_ci, "HoleNum inválido para Approach");

                // UserLimit válido? (4,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = [6, 20, 30];
                if (!allowedPlayers.Contains(ri.MaxUsers) && !session.UserInfo.UserCapabilities.IsGameMaster)
                    ThrowHackException(session, ri, m_ci, "UserLimit inválido Approach");
            }

            // Canal Normal não permite Modo aleatório (random) — apenas Modo == 3 é permitido para "random"
            if (m_ci != null && m_ci.type.all && ri.HoleMode != 3)
                ThrowHackException(session, ri, m_ci, "Random Modo proibido no canal Normal");
        }

        // --------- Shuffle (Type 6) ----------
        private void ValidateShuffleSpecific(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            int[] allowedHoles = [18];
            if (!allowedHoles.Contains(ri.HoleCount))
                ThrowHackException(session, ri, m_ci, "HoleNum inválido no Shuffle");

            int[] allowedPlayers = [30];
            if (!allowedPlayers.Contains(ri.MaxUsers))
                ThrowHackException(session, ri, m_ci, "UserLimit inválido no Shuffle");

            uint[] allowedTime = [2400000 / 60000];
            if (!allowedTime.Contains(ri.TimeMin / 60000))
                ThrowHackException(session, ri, m_ci, "TimeMin inválido no Shuffle");

            if (ri.CourseIndex != RoomCourseFlags.RANDOM)
                ThrowHackException(session, ri, m_ci, "Course/Map inválido no Shuffle");

            if (ri.HoleMode != 0 && ri.HoleMode != 5)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Shuffle");
        }

        // --------- ValidateRoomName ----------
        private void ValidateRoomName(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
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
                ThrowHackException(session, ri, m_ci, "Nome da sala inválido: " + ri.GetRoomType());
        }

        // --------- ValidateRoomPass ----------
        private void ValidateRoomPass(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            if (ri.GetRoomType() == RoomTypeFlags.PRACTICE || ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
            {
                if (!string.IsNullOrEmpty(ri.Password) && ri.Password.Length < 8 && !ri.Password.Contains("MDA"))
                    ThrowHackException(session, ri, m_ci, "tamanho da str da Password na sala inválida: " + ri.GetRoomType());
            }
            else
            {
                if (!string.IsNullOrEmpty(ri.Password) && ri.Password.Length > 14)
                    ThrowHackException(session, ri, m_ci, "tamanho da str da Password na sala inválida: " + ri.GetRoomType());
            }
        }

        private void ValidateRoomCreate(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
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
                ThrowHackException(session, ri, m_ci, "Tipo de jogo inválido: " + ri.GetRoomType());
        }

        private void ValidateMaxPlayers(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
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
                ThrowHackException(session, ri, m_ci, "MaxUsers inválido: " + ri.MaxUsers);
        }

        private void ValidateHoleCount(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
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
                ThrowHackException(session, ri, m_ci, "HoleCount inválido: " + ri.HoleCount);
        }

        private void ValidateForbiddenModes(Player session, GameRoomInfoModel ri, ChannelInfo m_ci)
        {
            if (ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_INT || ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_ADV)
            {
                ThrowHackException(session, ri, m_ci, "tentou criar HoleMode proibido");
            }
        }

        private void ValidateTime30s(Player session, GameRoomInfoModel ri, ChannelInfo m_ci, params uint[] allowedMinutes)
        {
            if (ri.GetRoomType() == RoomTypeFlags.APPROCH)//unico com time minute em segundos
            {
                if (ri.TimeMin < (40 * 1000))
                    ThrowHackException(session, ri, m_ci, $"TimeMin inválido para o Approach: {ri.TimeMin}");

                if (!allowedMinutes.Contains(ri.TimeMin / 1000))
                    ThrowHackException(session, ri, m_ci, $"TimeMin inválido para o Approach: {ri.TimeMin / 1000}");
            }
            else
            {
                if (ri.TimeMin < (15 * 60000))
                    ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");

                if (!allowedMinutes.Contains(ri.TimeMin / 60000))
                    ThrowHackException(session, ri, m_ci, $"TimeMin inválido: {ri.TimeMin / 60000}");
            }
        }

        private void ValidateTimeVs(Player session, GameRoomInfoModel ri, ChannelInfo m_ci, params uint[] allowedSeconds)
        {
            if ((ri.GetRoomType() == RoomTypeFlags.STROKE || ri.GetRoomType() == RoomTypeFlags.MATCH)
               && ri.TimeSec < (40 * 1000))
            {
                ThrowHackException(session, ri, m_ci, $"TimeSec inválido: {ri.TimeSec}");
            }

            if (!allowedSeconds.Contains(ri.TimeSec / 1000))
            {
                ThrowHackException(session, ri, m_ci, $"TimeSec inválido: {ri.TimeSec}");
            }
        }

        // --------- ThrowHackException ----------
        private void ThrowHackException(Player session, GameRoomInfoModel ri, ChannelInfo m_ci, string motivo)
        {
            string msg = $"[Error] Normal [UID={(session != null ? session.UserInfo.UID.ToString() : "NULL")}] " +
                         $"Channel[ID={(m_ci != null ? m_ci.id.ToString() : "NULL")}, NAME= {(m_ci != null ? m_ci.name.ToString() : "NULL")}] tentou criar sala [Nome={ri.Name}, PWD={ri.Password}, TIPO={ri.GetRoomType()}], {motivo}. Hacker ou Bug";

            throw new exception(msg, ExceptionError.STDA_MAKE_ERROR_TYPE(
                STDA_ERROR_TYPE.CHANNEL, 10, 0x770001));
        }
    }
}
