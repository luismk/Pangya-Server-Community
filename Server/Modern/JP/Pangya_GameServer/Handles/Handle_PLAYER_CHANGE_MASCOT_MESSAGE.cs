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
    public class Handle_PLAYER_CHANGE_MASCOT_MESSAGE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                int mascot_id = Packet.ReadInt32();
                string msg = Packet.ReadString();

                if (msg.Length == 0)
                {
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas a AppMessage esta vazia. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200100, 0));
                }

                if (msg.Length > 30)
                {
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas o comprimento da AppMessage ultrapassa os 30 caracteres permitido. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200101, 0));
                }

                if (string.IsNullOrEmpty(msg))
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(msg))
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE="
                            + msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                var pMi = Player.Inventory.FindMascotById(mascot_id);

                if (pMi == null)
                {
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas ele nao tem esse mascot. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200102, 0));
                }

                if (!sIff.Instance.isLoad())
                {
                    sIff.Instance.Init();
                }

                var mascot = sIff.Instance.findMascot(pMi._typeid);

                if (mascot == null || !(mascot.msg.active))
                {
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas nao existe ou nao esta ativado esse mascot no IFF_STRUCT do server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200103, 0));
                }

                if (!(mascot.msg.active))
                {
                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas a AppMessage do mascot nao esta ativado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200104, 0));
                }

                try
                {
                    if (mascot.msg.change_price > 0)
                    {
                       Player.UserInfo.consomePang(mascot.msg.change_price);
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestChangeMascotMessage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    throw new exception("[Lobby::RequestChangeMascotMessage][Error] Normal [UID=" + Player.UserInfo.UID + "], tentou trocar a AppMessage[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas o Player nao tem Pang[HAVE=" + (Player.UserInfo.Statistics.pang) + ", REQ=" + (mascot.msg.change_price) + "] suficiente para trocar a mensagem do mascot. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200105, 0));
                }

                // limpa e move AppMessage para o Mascot Info do Player no server 
                pMi.message = msg;

                // Update Mascot info no DB
                NormalManagerDB.Instance.add(26, new CmdUpdateMascotInfo(Player.UserInfo.UID, pMi));

                // Update on GAME
                p.init_plain(0xE2);

                p.WriteByte(4); // Update Mascot Message

                p.WriteInt32(pMi.id); // Mascot ID

                p.WriteString(pMi.message);

                p.WriteUInt64(Player.UserInfo.Statistics.pang);

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestChangeMascotMessage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain(0xE2);

                p.WriteSByte(-1); // Option [Error]

                p.WriteInt32(-1); // Mascot ID

                p.WriteUInt16(0); // Msg Length

                p.WriteUInt64(Player.UserInfo.Statistics.pang);

                Player.Send(p);
            }
        }
    }
}