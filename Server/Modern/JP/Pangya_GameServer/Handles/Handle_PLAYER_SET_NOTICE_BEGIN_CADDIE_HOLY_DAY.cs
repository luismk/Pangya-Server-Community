using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
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
    public class Handle_PLAYER_SET_NOTICE_BEGIN_CADDIE_HOLY_DAY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                int caddie_id = Packet.ReadInt32();
                byte check = Packet.ReadByte();

                if (caddie_id <= 0)
                {
                    throw new exception("[Lobby::RequestSetNoticeBeginCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou setar ou desetar o Aviso de ferias do Caddie[ID=" + (caddie_id) + "], mas o caddie_id is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6200101));
                }

                var pCi = Player.Inventory.FindCaddieById(caddie_id);

                if (pCi == null)
                {
                    throw new exception("[Lobby::RequestSetNoticeBeginCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou setar ou desetar o Aviso de ferias do Caddie[ID=" + (caddie_id) + "], mas ele nao tem esse caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6200102));
                }

                var caddie = sIff.Instance.findCaddie(pCi._typeid);

                if (caddie == null)
                {
                    throw new exception("[Lobby::RequestSetNoticeBeginCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou setar ou desetar o Aviso de ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao tem esse caddie no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6200103));
                }

                // Tem caddie que não precisa, checar o end, mas o cliente manda mesmo assim, ai aqui da erro se eu não ignorar
                if ((!caddie.Shop.flag_shop.IsCash && caddie.valor_mensal <= 0) || pCi.rent_flag != 2)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestSetNoticeBeginCaddieHolyDay][Warning] Normal [UID=" + Player.UserInfo.UID + "] tentou setar ou desetar o Aviso de ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas esse nao é um caddie valido para setar aviso de ferias. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // UPDATE ON SERVER
                // Só Att se for diferente do que está no Server
                if (pCi.check_end != check)
                {
                    pCi.check_end = check;

                    // UPDATE ON DB
                    NormalManagerDB.Instance.add(21, new CmdSetNoticeCaddieHolyDay(Player.UserInfo.UID, pCi.id, pCi.check_end), null, this);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RequestSetNoticeBeginCaddieHolyDay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}