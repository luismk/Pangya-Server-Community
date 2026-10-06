using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHARACTER_STATS_DOWN : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                if (Player.UserInfo.BlockFlag.Flag.CharacterMastery)
                {
                    throw new exception("[Lobby::RequestCharacterStatsDown][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar Stats do character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                uint stat = Packet.ReadUInt32();

                CharacterInfo ci = new CharacterInfo().ToRead(Packet);

                var pCi = Player.Inventory.FindCharacterById(ci.id);

                if (pCi == null || pCi._typeid != ci._typeid)
                {
                    throw new exception("[Lobby::RequestCharacterStatsDown][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar o stat[value=" + (stat) + "] do Character[TYPEID=" + (ci._typeid) + ", ID=" + (ci.id) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        550, 0x5200551));
                }

                var character = sIff.Instance.findCharacter(pCi._typeid);

                if (character == null)
                {
                    throw new exception("[Lobby::RequestChracterStatsDown][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar stat[value=" + (stat) + "] do Character[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas ele nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        553, 0x5200554));
                }

                if (stat > (int)CharacterInfo.Stats.S_CURVE)
                {
                    throw new exception("[Lobby::RequestCharacterStatsDown][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar um stat[value=" + (stat) + "] invalido do Character[ID=" + (pCi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        551, 0x5200552));
                }

                if ((char)(pCi.pcl[stat] - 1) < 0)
                {
                    throw new exception("[Lobby::RequestCharacterStatsDown][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou desupar um stat[value=" + (stat) + "] do Character[ID=" + (pCi.id) + "] que ele nao tem mais valor upado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        552, 0x5200553));
                }

                pCi.pcl[stat]--;

                // Update on DB
                NormalManagerDB.Instance.add(7,
                     new CmdUpdateCharacterPCL(Player.UserInfo.UID, pCi),
                     null, null);

                // Atualiza item no Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(0xC9);
                p.WriteUInt32(pCi._typeid);
                p.WriteInt32(pCi.id);
                p.WriteUInt32(0); // ServerFlag Time
                p.WriteUInt32(0); // qntd ant
                p.WriteUInt32(0); // qntd dep
                p.WriteUInt32(0); // qntd
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_POWER]); // stats.PWR
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CONTROL]); // stats.CTRL
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_ACCURACY]); // stats.ACCRY
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_SPIN]); // stats.SPIN
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CURVE]); // stats.CURVE
                p.WriteZero(15);

                Player.Send(p);

                // Resposta de Downgrade Character Stats no Jogo
                p.init_plain(0x270);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(stat);

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400085u);

                sys_achieve.finish_and_update(Player);

                Player.Inventory.SyncCharacter(pCi.id, pCi);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestCharacterStatsDown][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x270);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200550);

                Player.Send(p);
            }
        }
    }
}