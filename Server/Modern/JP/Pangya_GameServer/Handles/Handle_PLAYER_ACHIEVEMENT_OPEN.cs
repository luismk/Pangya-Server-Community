using System;
using System.Threading.Tasks;
using Pangya_GameServer.Manager;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ACHIEVEMENT_OPEN : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint uid = Packet.ReadUInt32();
                AchievementManager? mgr = GetManager(uid, Player);

                if (mgr == null)
                {
                    Player.Send(Handle_PACKET_RESPONSE.pacote22C(1)); // Falha
                    return;
                }

                mgr.sendAchievementGuiToPlayer(Player);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private AchievementManager? GetManager(uint uid, Player session)
        {
            // 1. Caso seja o próprio jogador
            if (Player.UserInfo.UID == uid)
                return Player.UserInfo.Achievements;

            // 2. Caso seja outro jogador online
            var targetPlayer = GameServer.Instance.FindPlayer(uid);
            if (targetPlayer != null)
                return targetPlayer.UserInfo.Achievements;

            // 3. Caso o jogador esteja offline (Busca temporária)
            var offlineMgr = new AchievementManager();
            offlineMgr.initAchievement(uid);

            return offlineMgr;
        }
    }
}