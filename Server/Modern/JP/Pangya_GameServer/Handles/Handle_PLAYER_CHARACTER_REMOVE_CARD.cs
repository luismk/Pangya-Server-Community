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
    public class Handle_PLAYER_CHARACTER_REMOVE_CARD : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                if (Player.UserInfo.BlockFlag.Flag.CharacterMastery)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card do character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                CardRemove cr = new CardRemove().ToRead(Packet);
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                var pCi = Player.Inventory.FindCharacterById(cr.char_id);

                if (pCi == null || pCi._typeid != cr.char_typeid)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas o ele nao possui esse character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        850, 0x5200851));
                }

                var pWi = Player.Inventory.FindWarehouseItemById(cr.removedor_id);

                if (pWi == null || pWi._typeid != cr.removedor_typeid)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas ele nao possui o removedor[TYPEID=" + (cr.removedor_typeid) + ", ID=" + (cr.removedor_id) + "] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        851, 0x5200852));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas ele nao quantidade suficiente do removedor[TYPEID=" + (cr.removedor_typeid) + ", ID=" + (cr.removedor_id) + "] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        854, 0x5200855));
                }

                switch (cr.card_slot)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4: // Character
                        if (pCi.Card_Character[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_Character[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_Character[(cr.card_slot - 1) % 4] = 0;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8: // Caddie
                        if (pCi.Card_Caddie[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_Caddie[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_Caddie[(cr.card_slot - 1) % 4] = 0;
                        break;
                    case 9:
                    case 10:
                    case 11:
                    case 12: // NPC
                        if (pCi.Card_NPC[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_NPC[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_NPC[(cr.card_slot - 1) % 4] = 0;
                        break;
                    default:
                        throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas o slot é deconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            852, 0x5200853));
                }

                // Update ON Server
                var pCei = Player.Inventory.FindCardEquipedByTypeid(bi._typeid, (int)cr.char_typeid, (int)cr.card_slot);

                if (pCei == null)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem o card equipado no List de cards equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x857, 0x5200858));
                }

                item = new stItem();

                item.type = 2;
                item._typeid = pWi._typeid;
                item.id = (int)pWi.id;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                // Remove Card Removedor Item
                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu excluir/(atualizar qntd) item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        858, 0x5200859));
                }

                v_item.Add(new stItem(item));

                item = new stItem();

                ItemManager.initItemFromBuyItem(Player.UserInfo, item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu initializar item[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        855, 0x5200856));
                }

                // Add Card Desequipado
                var rt = RetAddItem.INIT_VALUE;

                if ((rt = ItemManager.addItem(item, Player, 0, 0)) < 0)
                {
                    throw new exception("[Lobby::RequestCharacterRemoveCard][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu adicionar item[TYPEID=" + (item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        856, 0x5200857));
                }

                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_item.Add(new stItem(item));
                }

                item = new stItem();

                item.type = 0xCB;
                item._typeid = pCi._typeid;
                item.id = (int)pCi.id;
                item.price = 0; // Card Typeid, 0 desequipa
                item.type_iff = (byte)cr.card_slot;

                v_item.Add(new stItem(item));

                // Update ON DB
                NormalManagerDB.Instance.add(11, new CmdRemoveEquipedCard(Player.UserInfo.UID, pCei), null, null);

                // Remove Equiped Card
                var it = Player.Inventory.CardEquipment.FirstOrDefault(_el =>
                {
                    return _el.id == bi._typeid && _el.parts_id == cr.char_id && _el.slot == cr.card_slot;
                });

                if (it != null)
                {
                    Player.Inventory.CardEquipment.Remove(it);
                }
                Player.Inventory.SyncCharacter(pCi.id, pCi);
                ////Player.Inventory.ei.char_info = pCi;//evitar vazamento de memoria

                // Update ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteInt16(el.c);
                    p.WriteZero(10); // UCC IDX e outras coisas
                    p.WriteUInt32(el.price); // Card Typeid
                    p.WriteByte(el.type_iff); // Card Slot
                }

                Player.Send(p);

                // Reposta do Character Remove Card
                p.init_plain(0x273);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(bi._typeid);

                Player.Send(p);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();
                sys_achieve.incrementCounter(0x6C400088u);
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestCharacterRemoveCard][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x273);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200850);

                Player.Send(p);
            }
        }
    }
}