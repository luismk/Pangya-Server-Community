using Pangya_GameServer.Models;
using PangyaAPI.Utilities;
using System;
using System.Linq;
namespace Pangya_GameServer.Game
{
    /// <summary>
    /// class handle for modes game, check, revision data, +++++
    /// idea: Luiz Lopes
    /// luizinrc@hotmail.com
    /// </summary>
    public class FilterRoom
    {
        public FilterRoom(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (session == null || !session.m_connected)
                ThrowHackException(session, ri, m_ci, "Sessão inexistente ou desconectada");

            if (m_ci == null)
                ThrowHackException(session, ri, m_ci, "Informacões do canal inexistente");

            if (session.m_pi.m_cap.game_master)
                return;

            ValidateRoomName(session, ri, m_ci);
            ValidateRoomPass(session, ri, m_ci);
            ValidateRoomCreate(session, ri, m_ci);
            ValidateMaxPlayers(session, ri, m_ci);
            ValidateRoomTime(session, ri, m_ci);
            ValidateHoleCount(session, ri, m_ci);
            ValidateForbiddenModes(session, ri, m_ci);

            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                    ValidateStrokeSpecific(session, ri, m_ci);
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                    ValidatePangBattleSpecific(session, ri, m_ci);
                    break;
                case RoomInfo.ROOM_INFO_TYPE.MATCH: // se MATCH corresponde ao VS/Approach no seu enum
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                    ValidateVsApproach(session, ri, m_ci);
                    break;
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                    ValidateShuffleSpecific(session, ri, m_ci);
                    break;
                default:
                    break;
            }
        }

        // --------- ValidateRoomTime (ajustado para também checar shotTimeLimits do C++) ----------
        private void ValidateRoomTime(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                    if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9 || ri.qntd_hole == 18)
                    {
                        ValidateTimeVs(session, ri, m_ci, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
                    break;

                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                    if (ri.qntd_hole == 6 || ri.qntd_hole == 9 || ri.qntd_hole == 18)
                    {
                        ValidateTimeVs(session, ri, m_ci, 30, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                    // Tournament: pode ter short_game / natural branches; time_30s used (ms)
                    if (ri.special_flag_mod != null)
                    {
                        if (ri.special_flag_mod.short_game)
                        {
                            if (ri.qntd_hole == 9 || ri.qntd_hole == 18)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                        }
                        else if (ri.special_flag_mod.natural)
                        {
                            if (ri.qntd_hole == 9)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else if (ri.qntd_hole == 18)
                                ValidateTime30s(session, ri, m_ci, 15, 30, 20, 25, 35);
                            else
                                ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                        }
                    }
                    else
                    {
                        if (ri.qntd_hole == 9)
                            ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                        else if (ri.qntd_hole == 18)
                            ValidateTime30s(session, ri, m_ci, 35, 40, 45, 50, 55);
                        else
                            ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    }
                    break;

                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                    if (ri.qntd_hole == 9)
                        ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                    else if (ri.qntd_hole == 18)
                        ValidateTime30s(session, ri, m_ci, 35, 40, 45, 50, 55);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    break;
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                    if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 1000}");
                    break;

                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                    if (ri.qntd_hole == 18)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    break;

                default:
                    break;
            }
        }

        // --------- Stroke specific checks (shotTime, Modo for 18H, holes set) ----------
        private void ValidateStrokeSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            // hole count already validado em ValidateHoleCount; validar shotTime (time_vs) em segundos permitidos
            uint[] allowedShotSeconds = { 40, 60, 120, 300 };
            if (!allowedShotSeconds.Contains(ri.time_vs / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido (Stroke)");

            // Se 18 holes então Modo deve ser 0 ou 3
            if (ri.qntd_hole == 18)
            {
                if (ri.modo != 0 && ri.modo != 3)
                    ThrowHackException(session, ri, m_ci, "Modo inválido no Stroke 18H");
            }
        }

        // --------- Pang Battle specific ----------
        private void ValidatePangBattleSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            // Modo valid
            if (ri.modo != 0 && ri.modo != 3)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Pang Battle");

            // shotTime valid (seconds)
            uint[] allowedShotSeconds = { 30, 40, 60, 120, 300 };
            if (!allowedShotSeconds.Contains(ri.time_vs / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido no Pang Battle");
        }

        // --------- VS / APPROACH (game type 4 / 5) ----------
        private void ValidateVsApproach(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
            {
                // gameTimeLimit checks (valores em milissegundos como no C++)
                if (ri.qntd_hole == 9)
                {
                    uint[] allowed = { 900000, 1200000, 1500000, 1800000 };
                    if (!allowed.Contains(ri.time_vs))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
                }
                else if (ri.qntd_hole == 18)
                {
                    uint[] allowed = { 1800000, 2100000, 2400000, 2700000, 3000000 };
                    if (!allowed.Contains(ri.time_vs))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 18H");

                    if (ri.modo != 0 && ri.modo != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/modo'
                        ThrowHackException(session, ri, m_ci, "Modo inválido em 18H");
                }
                else if (ri.qntd_hole == 6)
                {
                    uint[] allowed = { 1800000, 2100000, 2400000, 2700000, 40000 };
                    if (!allowed.Contains(ri.time_vs))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 6H");

                    if (ri.modo != 0 && ri.modo != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/modo'
                        ThrowHackException(session, ri, m_ci, "Modo inválido em 6");
                }

                else
                    ThrowHackException(session, ri, m_ci, "HoleNum inválido para Match");

                // UserLimit válido? (4,10,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = { 4, 10, 20, 30 };
                if (!allowedPlayers.Contains(ri.max_player) && !session.m_pi.m_cap.game_master)
                    ThrowHackException(session, ri, m_ci, "UserLimit inválido no Match");
            }
            else
            {
                // gameTimeLimit checks (valores em milissegundos como no C++)
                if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9)
                {
                    uint[] allowed = { 40000 };
                    if (!allowed.Contains(ri.time_30s))
                        ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
                }
                else
                    ThrowHackException(session, ri, m_ci, "HoleNum inválido para Approach");

                // UserLimit válido? (4,20,30) — GMs podem usar 100 ou 200
                int[] allowedPlayers = { 6, 20, 30 };
                if (!allowedPlayers.Contains(ri.max_player) && !session.m_pi.m_cap.game_master)
                    ThrowHackException(session, ri, m_ci, "UserLimit inválido Approach");
            }

            // Canal normal não permite Modo aleatório (random) — apenas Modo == 3 é permitido para "random"
            if (m_ci != null && m_ci.type.all && ri.modo != 3)
                ThrowHackException(session, ri, m_ci, "Random Modo proibido no canal normal");
        }

        // --------- Shuffle (tipo 6) ----------
        private void ValidateShuffleSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedHoles = { 18 };
            if (!allowedHoles.Contains(ri.qntd_hole))
                ThrowHackException(session, ri, m_ci, "HoleNum inválido no Shuffle");

            int[] allowedPlayers = { 30 };
            if (!allowedPlayers.Contains(ri.max_player))
                ThrowHackException(session, ri, m_ci, "UserLimit inválido no Shuffle");

            uint[] allowedTime = { 2400000 / 60000 };
            if (!allowedTime.Contains(ri.time_30s / 60000))
                ThrowHackException(session, ri, m_ci, "time_30s inválido no Shuffle");

            if (ri.course != RoomInfo.ROOM_INFO_COURSE.RANDOM)
                ThrowHackException(session, ri, m_ci, "Course/Map inválido no Shuffle");

            if (ri.modo != 0 && ri.modo != 5)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Shuffle");
        }

        // --------- ValidateRoomName ----------
        private void ValidateRoomName(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            bool _check = true;
            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                case RoomInfo.ROOM_INFO_TYPE.LOUNGE:
                    if (string.IsNullOrEmpty(ri.name))
                        _check = false;
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                    if (!string.IsNullOrEmpty(ri.name) && ri.name.CompareTo("Single Player Practice Mode") != 0)
                        _check = false;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(session, ri, m_ci, "Nome da sala inválido: " + ri.getTipo());
        }

        // --------- ValidateRoomPass ----------
        private void ValidateRoomPass(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
            {
                if (!string.IsNullOrEmpty(ri.senha) && ri.senha.Length < 8 && !ri.senha.Contains("MDA"))
                    ThrowHackException(session, ri, m_ci, "tamanho da str da senha na sala inválida: " + ri.getTipo());
            }
            else
            {
                if (!string.IsNullOrEmpty(ri.senha) && ri.senha.Length > 14)
                    ThrowHackException(session, ri, m_ci, "tamanho da str da senha na sala inválida: " + ri.getTipo());
            }
        }

        private void ValidateRoomCreate(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            bool _check;
            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                case RoomInfo.ROOM_INFO_TYPE.LOUNGE:
                    _check = true;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(session, ri, m_ci, "Tipo de jogo inválido: " + ri.getTipo());
        }

        private void ValidateMaxPlayers(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedPlayers;

            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                    allowedPlayers = new[] { 2, 3, 4 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                    allowedPlayers = new[] { 2, 4 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                    allowedPlayers = new[] { 10, 20, 30 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                    allowedPlayers = new[] { 6, 20, 30 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                    allowedPlayers = new[] { 2, 4 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                    allowedPlayers = new[] { 1 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                    allowedPlayers = new[] { 30 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.LOUNGE:
                    allowedPlayers = new[] { 10, 20, 30 };
                    break;
                default:
                    allowedPlayers = new int[0];
                    break;
            }

            if (allowedPlayers.Length > 0 && Array.IndexOf(allowedPlayers, ri.max_player) == -1)
                ThrowHackException(session, ri, m_ci, "max_player inválido: " + ri.max_player);
        }

        private void ValidateHoleCount(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedHoles;
            switch (ri.getTipo())
            {
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                    allowedHoles = new[] { 3, 6, 9, 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                    allowedHoles = new[] { 6, 9, 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                    allowedHoles = new[] { 9, 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                    allowedHoles = new[] { 3, 6, 9 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                    allowedHoles = new[] { 6, 9, 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                    allowedHoles = new[] { 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                    allowedHoles = new[] { 1 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                    allowedHoles = new[] { 1, 9, 18 };
                    break;
                case RoomInfo.ROOM_INFO_TYPE.LOUNGE://limite e 18, eu acho
                    allowedHoles = new[] { 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                    break;
                default:
                    allowedHoles = new int[0];
                    break;
            }

            if (allowedHoles.Length > 0 && Array.IndexOf(allowedHoles, ri.qntd_hole) == -1)
                ThrowHackException(session, ri, m_ci, "qntd_hole inválido: " + ri.qntd_hole);
        }

        private void ValidateForbiddenModes(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT || ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV)
            {
                ThrowHackException(session, ri, m_ci, "tentou criar modo proibido");
            }
        }

        private void ValidateTime30s(Player session, RoomInfoEx ri, ChannelInfo m_ci, params uint[] allowedMinutes)
        {
            if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.APPROCH)//unico com time minute em segundos
            {
                if (ri.time_30s < (40 * 1000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido para o approach: {ri.time_30s}");

                if (!allowedMinutes.Contains(ri.time_30s / 1000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido para o approach: {ri.time_30s / 1000}");
            }
            else
            {
                if (ri.time_30s < (15 * 60000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");

                if (!allowedMinutes.Contains(ri.time_30s / 60000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
            }
        }

        private void ValidateTimeVs(Player session, RoomInfoEx ri, ChannelInfo m_ci, params uint[] allowedSeconds)
        {
            if ((ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
               && ri.time_vs < (40 * 1000))
            {
                ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
            }

            if (!allowedSeconds.Contains(ri.time_vs / 1000))
            {
                ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
            }
        }

        // --------- ThrowHackException ----------
        private void ThrowHackException(Player session, RoomInfoEx ri, ChannelInfo m_ci, string motivo)
        {
            string msg = $"[Error] PLAYER [UID={(session != null ? session.m_pi.uid.ToString() : "NULL")}] " +
                         $"Channel[ID={(m_ci != null ? m_ci.id.ToString() : "NULL")}, NAME= {(m_ci != null ? m_ci.name.ToString() : "NULL")}] tentou criar sala [Nome={ri.name}, PWD={ri.senha}, TIPO={ri.getTipo()}], {motivo}. Hacker ou Bug";

            throw new exception(msg, ExceptionError.STDA_MAKE_ERROR_TYPE(
                STDA_ERROR_TYPE.CHANNEL, 10, 0x770001));
        }
    }
}
