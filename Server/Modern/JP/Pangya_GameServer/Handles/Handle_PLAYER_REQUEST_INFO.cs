using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_REQUEST_INFO : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint uid = Packet.ReadUInt32();
                byte season = Packet.ReadByte();

                // 1. Tenta obter o PlayerInfo (Online ou Próprio)
                PlayerInfo? pi = GetOnlinePlayerInfo(uid, Player);

                if (pi != null)
                {
                    HandleOnlinePlayerInfo(Player, pi, season);
                }
                else
                {
                    // 2. Se não estiver online, busca no Banco de Dados
                    HandleOfflinePlayerInfo(Player, uid, season);
                }
            }
            catch (Exception e)
            {
                // Log de erro e resposta de falha
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_INFO_REQUEST][Error] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                Player.Send(Handle_PACKET_RESPONSE.pacote089(0));
            }
        }

        private PlayerInfo? GetOnlinePlayerInfo(uint uid, Player session)
        {
            if (uid == Player.UserInfo.UID) return Player.UserInfo;

            var target = GameServer.Instance.FindPlayer(uid);
            return target?.UserInfo;
        }

        private void HandleOnlinePlayerInfo(Player session, PlayerInfo pi, byte season)
        {
            // Validação de GM
            if (IsRestrictedGM(Player, pi))
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote089(pi.UID, season, 3));
                return;
            }

            var inventory = Player.Inventory;

            var ci = inventory.FindCharacterById(inventory.UserEquipment.character_id) ?? new CharacterInfo();

            // Envio de pacotes para Jogador Online
            SendPlayerPackets(Player, pi.UID, season,
                pi.Member, ci, inventory.UserEquipment, pi.Statistics, pi.Guild,
                pi.NaturalMapStatistics.Where(m => m.best_score != 127).ToList(),
                pi.NaturalMapStatisticsAll.Where(m => m.best_score != 127).ToList(),
                pi.GrandPrixMapStatistics.Where(m => m.best_score != 127).ToList(),
                pi.GrandPrixMapStatisticsAll.Where(m => m.best_score != 127).ToList(),
                pi.NormalMapStatistics.Where(m => m.best_score != 127).ToList(),
                pi.NormalMapStatisticsAll.Where(m => m.best_score != 127).ToList(),
                (season != 0) ? inventory.CurrentSpecialTrophies : inventory.RemainingSpecialTrophies,
                (season != 0) ? inventory.CurrentTrophy : inventory.RemainingTrophy,
                (season != 0) ? inventory.CurrentGrandPrixTrophies : inventory.RemainingGrandPrixTrophies
            );
        }

        private async void HandleOfflinePlayerInfo(Player session, uint uid, byte season)
        {
            // Busca PlayerMemberInfo primeiro para checar GM 
            var mi = CommandDB.LoadMemberInfo(uid);
             
            if (uid != Player.UserInfo.UID && !Player.UserInfo.UserCapabilities.IsGameMaster && mi.Capability.IsGameMaster)
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote089(uid, season, 3));
                return;
            }

            // Busca demais dados do Banco
            var ci = CommandDB.LoadCharacterOne(uid);
            var ue = CommandDB.LoadUserEquip(uid);
            var ui = CommandDB.LoadUserInfo(uid);
            var gi = CommandDB.LoadGuildInfo(uid);
            var ti = CommandDB.LoadTrophy(uid, (CmdTrofelInfo.TYPE_SEASON)season);

            // Estatísticas de Mapas (Buscas múltiplas)
            var ms_na = GetOfflineStats(uid, season, CmdMapStatistics.TYPE_MODO.M_NATURAL).Result;
            var ms_gp = GetOfflineStats(uid, season, CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX).Result;
            var ms_no = GetOfflineStats(uid, season, CmdMapStatistics.TYPE_MODO.M_NORMAL).Result;

            // Troféus Especiais
            var v_tei = CommandDB.LoadTrophySpecial(uid, (CmdTrophySpecial.TYPE_SEASON)season, CmdTrophySpecial.TYPE.NORMAL);
            var v_tegi = CommandDB.LoadTrophySpecial(uid, (CmdTrophySpecial.TYPE_SEASON)season, CmdTrophySpecial.TYPE.GRAND_PRIX);

            SendPlayerPackets(Player, uid, season, mi, ci, ue, ui, gi,
                ms_na.Normal, ms_na.Assist, ms_gp.Normal, ms_gp.Assist, ms_no.Normal, ms_no.Assist,
                v_tei, ti, v_tegi);
        }

        // Helper para centralizar o envio da "enxurrada" de pacotes
        private void SendPlayerPackets(Player session, uint uid, byte season,
            PlayerMemberInfo mi, CharacterInfo ci, UserEquip ue, PlayerUserStatistics ui, GuildInfo gi,
            List<MapStatisticsEx> ms_na, List<MapStatisticsEx> msa_na,
            List<MapStatisticsEx> ms_gp, List<MapStatisticsEx> msa_gp,
            List<MapStatisticsEx> ms_no, List<MapStatisticsEx> msa_no,
            List<TrophySpecialInfo> v_tei, TrophyInfo ti, List<TrophySpecialInfo> v_tegi)
        {
            Player.Send(Handle_PACKET_RESPONSE.pacote157(mi, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote15E(uid, ci));
            Player.Send(Handle_PACKET_RESPONSE.pacote156(uid, ue, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote158(uid, ui, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote15D(uid, gi));
            Player.Send(Handle_PACKET_RESPONSE.pacote15C(uid, ms_na, msa_na, (byte)((season != 0) ? 0x33 : 0x0A)));
            Player.Send(Handle_PACKET_RESPONSE.pacote15C(uid, ms_gp, msa_gp, (byte)((season != 0) ? 0x34 : 0x0B)));
            Player.Send(Handle_PACKET_RESPONSE.pacote15B(uid, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote15A(uid, v_tei, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote159(uid, ti, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote15C(uid, ms_no, msa_no, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote257(uid, v_tegi, season));
            Player.Send(Handle_PACKET_RESPONSE.pacote089(uid, season));
        }

        private bool IsRestrictedGM(Player session, PlayerInfo targetPi)
            => Player.UserInfo.UID != targetPi.UID && !Player.UserInfo.UserCapabilities.IsGameMaster && targetPi.UserCapabilities.IsGameMaster;
         
        private async Task<(List<MapStatisticsEx> Normal, List<MapStatisticsEx> Assist)> GetOfflineStats(uint uid, byte season, CmdMapStatistics.TYPE_MODO modo)
        { 
            var normal = CommandDB.LoadMapStats(uid, (CmdMapStatistics.TYPE_SEASON)season, CmdMapStatistics.TYPE.NORMAL, modo);

            var assist = CommandDB.LoadMapStats(uid, (CmdMapStatistics.TYPE_SEASON)season, CmdMapStatistics.TYPE.ASSIST, modo); 

            return (normal, assist);
        }
    }
}