using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_TAKE_ITEM_FROM_MAIL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            var m_ci = Player.GetChannel();
            try
            {
                int email_id = Packet.ReadInt32();

                // Level temporário para verificar subida de nível (Exp Pouch)
                ushort tmp_level = (ushort)Player.UserInfo.Member.GameLevel;

                // Não marca como lido ainda
                var ei = Player.UserInfo.MailBox.getEmailInfo(email_id, false);

                List<stItem> v_item = new List<stItem>();

                if (ei.itens != null && ei.itens.Count > 0)
                {
                    for (var i = 0; i < ei.itens.Count; ++i)
                    {
                        stItem item = new stItem();
                        ItemManager.initItemFromEmailItem(Player.UserInfo, item, ei.itens[i]);

                        if (item._typeid == 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou inicializar o item que pegou do mailbox[MAIL_ID=" + (email_id) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            Player.Send(Handle_PACKET_RESPONSE.pacote214(3));
                            return;
                        }

                        // Verifica se já possui o item ou se pode acumular (Overlap)
                        bool canOverlap = sIff.Instance.IsCanOverlapped(ei.itens[i]._typeid);
                        bool isCaddieItem = sIff.Instance.getItemGroupIdentify(ei.itens[i]._typeid) == IFF_GROUP.CAD_ITEM;
                        bool ownerHasItem = Player.Inventory.ownerItem(ei.itens[i]._typeid, 1);

                        if ((canOverlap && !isCaddieItem) || !ownerHasItem)
                        {
                            if (ItemManager.isSetItem(item._typeid))
                            {
                                var v_stItem = ItemManager.GetItemOfSetItem(Player, ei.itens[i]._typeid, false, 1);

                                if (v_stItem != null && v_stItem.Count > 0)
                                {
                                    foreach (var el in v_stItem)
                                    {
                                        bool subCanOverlap = sIff.Instance.IsCanOverlapped(el._typeid);
                                        bool subIsCaddieItem = sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.CAD_ITEM;

                                        if ((subCanOverlap && !subIsCaddieItem) || !Player.Inventory.ownerItem(el._typeid, 1))
                                        {
                                            v_item.Add(new stItem(el));
                                        }
                                    }
                                }
                                else
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][Success] Normal [UID=" + Player.UserInfo.UID + "] tentou add set item sem item dentro, do MailBox[MAIL_ID=" + (email_id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }
                            else
                            {
                                v_item.Add(new stItem(item));
                            }
                        }
                        else if (isCaddieItem)
                        {
                            throw new exception("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pegar um CaddieItem[TYPEID=" + (ei.itens[i]._typeid) + "] do Mail[ID=" + (email_id) + "] de um caddie que ele nao possui",
                                ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 201, 5100072));
                        }
                        else
                        {
                            throw new exception("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pegar um item[TYPEID=" + (ei.itens[i]._typeid) + "] do Mail[ID=" + (email_id) + "] que ele ja possui",
                                ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 201, 5100071));
                        }
                    }

                    // Remove itens do email no Banco de Dados
                    Player.UserInfo.MailBox.leftItensFromEmail(email_id);

                    // Adiciona itens ao Warehouse do jogador
                    var rai = ItemManager.addItem(v_item, Player, 1, 0);

                    if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        foreach (var fail in rai.fails)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou mover o item[TYPEID=" + (fail._typeid) + "] do MailBox para o MyRoom, mas falhou.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        Player.Send(Handle_PACKET_RESPONSE.pacote214(2));
                        return;
                    }

                    // Envia atualizações para o cliente
                    Player.Send(Handle_PACKET_RESPONSE.pacote216(v_item));
                    Player.Send(Handle_PACKET_RESPONSE.pacote214());

                    // Se subiu de nível, sincroniza com o canal/lobby
                    if (tmp_level != Player.UserInfo.Member.GameLevel)
                    {
                        m_ci?.UpdatePlayerInfo(Player);

                        if (Player.UserInfo.Lobby != 255)
                        {
                            var pi = m_ci?.GetPlayerInfo(Player);
                            if (pi != null)
                            {
                                m_ci?.SendBroadcast(Handle_PACKET_RESPONSE.MakePlayerLobby(new List<PlayerLobbyInfo>() { pi }, 3), 1);
                            }
                        }
                    }
                }
                else
                {
                    // Email sem itens
                    Player.Send(Handle_PACKET_RESPONSE.pacote214(1));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TAKE_ITEM_FROM_MAIL][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                int errCode = (int)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x5500100);

                Player.Send(Handle_PACKET_RESPONSE.pacote214(errCode));
            }
        } 
    }
}