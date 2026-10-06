using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHARACTER_CARD_EQUIP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet response = new Packet();
            try
            {
                // 1. Validação de bloqueio do jogador
                if (Player.UserInfo.BlockFlag.Flag.CharacterMastery)
                {
                    throw new exception($"[EquipCard] Player UID={Player.UserInfo.UID} bloqueado para maestria.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 9, 0x790001));
                }

                CardEquip ce = new CardEquip().ToRead(Packet);

                // 2. Validação do Card no IFF
                var cardIff = sIff.Instance.findCard(ce.card_typeid);
                if (cardIff == null)
                {
                    throw new exception($"[EquipCard] Card TypeID={ce.card_typeid} não existe no IFF.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 756, 0x5200757));
                }

                // 3. Validação de Posse (Character e Card)
                var pCi = Player.Inventory.FindCharacterById(ce.char_id);
                if (pCi == null || pCi._typeid != ce.char_typeid)
                {
                    throw new exception($"[EquipCard] Player UID={Player.UserInfo.UID} não possui o Character ID={ce.char_id}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 750, 0x5200751));
                }

                var pCardInfo = Player.Inventory.FindCardById(ce.card_id);
                if (pCardInfo == null || pCardInfo._typeid != ce.card_typeid)
                {
                    throw new exception($"[EquipCard] Player UID={Player.UserInfo.UID} não possui o Card ID={ce.card_id}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 751, 0x5200752));
                }

                // 4. Verificação de Segurança (Personal Shop)
                var room = Player.GetRoom();
                if (room != null && room.CheckPersonalShopItem(Player, ce.card_id))
                {
                    throw new exception($"[EquipCard] Card ID={ce.card_id} está à venda no Shop.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                }

                // 5. Validação de Slots Específicos
                ValidateSlotRules(ce, pCi);

                // 6. Lógica de Grupos de Cards e Equipamento
                uint group = sIff.Instance.getItemSubGroupIdentify22(ce.card_typeid);
                EquipCardToInfo(pCi, ce, group);

                // 7. Consumo do item (Remover do inventário)
                var itemsToRemove = new List<stItem> {
                    new stItem { type = 2, id = pCardInfo.id, _typeid = pCardInfo._typeid, qntd = 1, STDA_C_ITEM_QNTD = -1 }
                };

                if (ItemManager.removeItem(itemsToRemove, Player) <= 0)
                {
                    throw new exception("[EquipCard] Erro ao excluir card do inventário.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 757, 0x5200758));
                }

                // 8. Registro de Equipamento e DB
                CardEquipInfoEx cei = CreateCardEquipInfo(ce, cardIff, group);

                Player.Inventory.CardEquipment.Add(cei); 
                NormalManagerDB.Instance.add(10, new CmdEquipCard(Player.UserInfo.UID, cei, 0));
                Player.Inventory.SyncCharacter(pCi.id, pCi); 

                var item = new stItem
                {
                    type = 0xCB,
                    id = pCi.id,
                    _typeid = pCi._typeid,
                    price = cei._typeid,
                    type_iff = (byte)cei.slot
                };
                // 9. Envio de Pacotes de Resposta
                SendSuccessPackets(Player, item, cei);

                // 10. Sistema de Conquistas
                UpdateAchievements(Player);
            }
            catch (exception e)
            {
                HandleException(Player, e);
            }
        }

        private void ValidateSlotRules(CardEquip ce, CharacterInfo pCi)
        {
            // Slots 4 e 8 exigem Club Patcher (lógica do Pangya original)
            if (ce.char_card_slot == 4 || ce.char_card_slot == 8)
                throw new exception("Slot requer Club Patcher.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 752, 0x5200753));

            // Slot 7 exige uma parte específica de Caddie
            if (ce.char_card_slot == 7 && !pCi.isEquipedPartSlotThirdCaddieCardSlot())
                throw new exception("Slot 7 bloqueado (requer Part especial).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 753, 0x5200754));
        }

        private void EquipCardToInfo(CharacterInfo pCi, CardEquip ce, uint group)
        {
            uint slotIndex = (ce.char_card_slot - 1) % 4;

            // Mapeamento: 1-4 (Character), 5-8 (Caddie), 9-12 (NPC)
            if (ce.char_card_slot >= 1 && ce.char_card_slot <= 4)
            {
                if (group != 0) throw new exception("Card não é do Type Character.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 755, 0x5200756));
                if (pCi.Card_Character[slotIndex] != 0) throw new exception("Slot Character já ocupado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 758, 0x5200759));
                pCi.Card_Character[slotIndex] = ce.card_typeid;
            }
            else if (ce.char_card_slot >= 5 && ce.char_card_slot <= 8)
            {
                if (group != 1) throw new exception("Card não é do Type Caddie.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 755, 0x5200756));
                if (pCi.Card_Caddie[slotIndex] != 0) throw new exception("Slot Caddie já ocupado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 758, 0x5200759));
                pCi.Card_Caddie[slotIndex] = ce.card_typeid;
            }
            else if (ce.char_card_slot >= 9 && ce.char_card_slot <= 12)
            {
                if (group != 5) throw new exception("Card não é do Type NPC.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 755, 0x5200756));
                if (pCi.Card_NPC[slotIndex] != 0) throw new exception("Slot NPC já ocupado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 758, 0x5200759));
                pCi.Card_NPC[slotIndex] = ce.card_typeid;
            }
            else
            {
                throw new exception("Slot de card inválido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 754, 0x5200755));
            }
        }

        private CardEquipInfoEx CreateCardEquipInfo(CardEquip ce, PangyaAPI.IFF.Regions.JP.Models.IFF.Card card, uint group)
        {
            return new CardEquipInfoEx
            {
                index = -1,
                _typeid = ce.card_typeid,
                id = (uint)ce.card_id,
                efeito = card.Effect,
                efeito_qntd = card.EffectValue,
                slot = ce.char_card_slot,
                tipo = group,
                use_yn = 1,
                parts_typeid = ce.char_typeid,
                parts_id = (uint)ce.char_id
            };
        }

        private void SendSuccessPackets(Player session, stItem el, CardEquipInfoEx cei)
        {
            // Pacote 0x216
            Packet p = new Packet(0x216);
            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
            p.WriteUInt32(1);
            p.WriteByte(el.type);
            p.WriteUInt32(el._typeid);
            p.WriteInt32(el.id);
            p.WriteUInt32(el.flag_time);
            p.WriteInt32(el.stat.qntd_ant);
            p.WriteInt32(el.stat.qntd_dep);
            p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
            p.WriteInt16(el.c);
            p.WriteZero(10);  // UCC IDX e outras coisas
            p.WriteUInt32(el.price);      // Card typeid
            p.WriteByte(el.type_iff);   // Card Slot
            Player.Send(p);

            // Pacote 0x271
            p = new Packet(0x271);
            p.WriteUInt32(0); // Código OK
            p.WriteUInt32(cei._typeid);
            Player.Send(p);
        }

        private void UpdateAchievements(Player session)
        {
            AchievementSystem sys = new AchievementSystem();
            sys.incrementCounter(0x6C400087u);
            sys.finish_and_update(Player);
        }

        private void HandleException(Player session, exception e)
        {
            _smp.LogManager.Instance.push(new AppMessage($"[EquipCard][ErrorSystem] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

            Packet pErr = new Packet(0x271);
            uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                : 0x5200750;

            pErr.WriteUInt32(errorCode);
            Player.Send(pErr);
        }
    }
}