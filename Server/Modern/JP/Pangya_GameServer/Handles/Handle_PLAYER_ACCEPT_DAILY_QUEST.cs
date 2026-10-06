using Pangya_GameServer.Handles.Packets;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Handle;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ACCEPT_DAILY_QUEST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            var p = new Packet();

            int[] quest_id = null;

            try
            {

                if (Packet == null)
                {
                    throw new exception("Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        2, 0));
                }

                int num_quest = Packet.ReadInt32();

                if (num_quest <= 0u)
                {
                    throw new exception("Normal[UID=" + Convert.ToString(Player.UserInfo.UID) + "] tentou aceitar o Daily Quest, mas o RoomID de quest para aceitar is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        5000, 0));
                }

                quest_id = new int[num_quest];

                quest_id = Packet.ReadInt32(num_quest);


                var v_quest = DailyQuestManager.AcceptQuestUser(Player,
                    quest_id, num_quest);

                Dictionary<int, CounterItemInfo> map_cii = new Dictionary<int, CounterItemInfo>();

                foreach (var el in v_quest)
                {
                    foreach (var kvp in el.map_counter_item)
                    {
                        map_cii[(int)kvp.Key] = kvp.Value;  // ou Add se tiver certeza que não existe a chave
                    }
                }

                p = new Packet((ushort)0x216);

                p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                if (map_cii.Count > 0)
                {
                    p.WriteInt32(map_cii.Count);

                    foreach (var el in map_cii)
                    {
                        p.WriteByte(2);
                        p.WriteUInt32(el.Value._typeid);
                        p.WriteInt32(el.Value.id);
                        p.WriteUInt32(0); // type
                        p.WriteInt32(0); // Qntd antes
                        p.WriteInt32(0); // Qntd depois
                        p.WriteInt32(0); // add quantos, Type de add tinha 0(antes) + 3(qntd) = 3(depois)
                        p.WriteZero(25);
                    }
                }
                else
                {
                    p.WriteInt32(0);
                }

                Player.Send(p);


                Player.Send(Handle_PACKET_RESPONSE.pacote226(v_quest));

                if (quest_id != null)
                {
                    quest_id = null;
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ACCEPT_DAILY_QUEST][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                Player.Send(Handle_PACKET_RESPONSE.pacote226(new List<AchievementInfoEx>(), 1));

                if (quest_id != null)
                {
                    quest_id = null;
                }
            }
        }
    }
}