using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
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
    public class Handle_PLAYER_DAILY_QUEST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                if (Packet == null)
                {
                    throw new exception("Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        2, 0));
                }


                var quest = GameServer.Instance.DailyQuestsInfo;
                if (DailyQuestManager.CheckCurrentQuestUser(quest, Player))
                {
                    // Get Old Quest do Player
                    var old_quest = DailyQuestManager.GetOldQuestUser(Player);

                    foreach (var el in old_quest)
                        Player.UserInfo.Achievements.removeAchievement(el.id);

                    // Add nova quest para o Player
                    var v_ai = DailyQuestManager.NewQuestUser(quest, Player);

                    var p = new Packet(0x216);

                    p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                    // Achievement
                    if (v_ai.Count > 0)
                    {

                        p.WriteInt32(v_ai.Count);

                        foreach (var el in v_ai)
                        {
                            p.WriteByte(2);
                            p.WriteUInt32(el._typeid);//ta vindo Login repetido
                            p.WriteInt32(el.id);//tá vindo idex repedito
                            p.WriteUInt32(0); // type
                            p.WriteInt32(0); // Qntd antes
                            p.WriteInt32(1); // Qntd depois
                            p.WriteInt32(1); // add value
                            p.WriteZero(25);
                        }
                    }
                    else
                    {
                        p.WriteUInt32(0u);
                    }
                    //send 216
                    Player.Send(p);
                    //send 225
                    Player.Send(Handle_PACKET_RESPONSE.pacote225(Player.UserInfo.DailyQuests, old_quest));

                }
                else
                {
                    var p = new Packet(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteInt32(0);
                    //send 216
                    Player.Send(p);
                    //send 225
                    Player.Send(Handle_PACKET_RESPONSE.pacote225(Player.UserInfo.DailyQuests, null));
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_DAILY_QUEST][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}