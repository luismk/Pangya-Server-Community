using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ATTENDANCE_REWARD_LOGIN_COUNT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            try
            {
                // Attendance Reward System
                if (!sAttendanceRewardSystem.Instance.isLoad())
                {
                    sAttendanceRewardSystem.Instance.load();
                }

                sAttendanceRewardSystem.Instance.requestUpdateCountLogin(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestAttendanceRewardLoginCount][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}