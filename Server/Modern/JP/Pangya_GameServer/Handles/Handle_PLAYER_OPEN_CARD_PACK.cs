using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager; // Ajuste conforme seu namespace de managers
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_OPEN_CARD_PACK : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            try
            {
                // 1. Leitura do pacote enviado pelo cliente
                uint _typeid = Packet.ReadUInt32();
                int id = Packet.ReadInt32();

                if (!sCardSystem.Instance.isLoad())
                    sCardSystem.Instance.load();

                // 2. Validação de posse do item
                var pCi = Player.Inventory.FindCardById(id);
                if (pCi == null || pCi.qntd < 1)
                {
                    throw new exception($"[Handle_PLAYER_OPEN_CARD_PACK] Player [UID={Player.UserInfo.UID}] não possui o Card Pack.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 102, 0x5400103));
                }

                // 3. Busca a definição do Card Pack (Box ou Comum)
                CardPack cp = (sIff.Instance.getItemSubGroupIdentify22(_typeid) == 4)
                    ? sCardSystem.Instance.findBoxCardPack(_typeid)
                    : sCardSystem.Instance.findCardPack(_typeid);

                if (cp == null)
                    throw new exception("Card Pack não configurado no servidor.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 100, 0x5400101));

                // 4. Sorteio dos Cards
                var cards = sCardSystem.Instance.draws(cp);
                if (cards == null || cards.Count == 0)
                    throw new exception("Erro ao sortear cards.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 101, 0x5400102));

                // 5. Consumo do Pack (Remover 1 unidade)
                stItem item_rm = new stItem
                {
                    type = 2,
                    id = pCi.id,
                    _typeid = pCi._typeid,
                    qntd = 1,
                    STDA_C_ITEM_QNTD = -1
                };

                if (ItemManager.removeItem(item_rm, Player) <= 0)
                    throw new exception("Erro ao remover o Card Pack do inventário.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 104, 0x5400105));

                // 6. Preparação dos ganhos
                List<stItem> v_item_add = new List<stItem>();
                List<stItem> v_item_response = new List<stItem>();
                AchievementSystem sys_achieve = new AchievementSystem();

                v_item_response.Add(item_rm); // Adicionado para atualizar o decremento no cliente

                foreach (var el in cards)
                {
                    var item = new stItem();
                    var bi = new BuyItem { id = -1, _typeid = el._typeid, qntd = 1 };

                    ItemManager.initItemFromBuyItem(Player.UserInfo, item, bi, false, 0, 0, 1);

                    if (item._typeid == 0) continue;

                    // Agrupamento para o DB
                    var existing = v_item_add.FirstOrDefault(x => x._typeid == item._typeid);
                    if (existing != null)
                    {
                        existing.qntd++;
                        existing.STDA_C_ITEM_QNTD = (short)existing.qntd;
                    }
                    else
                    {
                        v_item_add.Add(item);
                    }

                    v_item_response.Add(item);
                    UpdateCardAchievements(sys_achieve, el);
                }

                // 7. Salvar no Banco de Dados
                var rai = ItemManager.addItem(v_item_add, Player, 0, 0);
                if (rai.fails.Count > 0) throw new exception("Erro ao persistir cards no DB.");

                // 8. Sincronizar IDs reais vindos do DB para o pacote de resposta
                foreach (var resItem in v_item_response.Where(x => x.id == -1))
                {
                    var dbItem = v_item_add.FirstOrDefault(x => x._typeid == resItem._typeid);
                    if (dbItem != null)
                    {
                        resItem.id = dbItem.id;
                        resItem.stat = dbItem.stat;
                    }
                }

                // 9. Enviar resposta para o cliente (Opcode 0x154)
                p.init_plain(0x154);
                p.WriteUInt32(0); // Sucesso

                foreach (stItem el in v_item_response)
                {
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el._typeid);
                    p.WriteZero(12);

                    var subGroup = sIff.Instance.getItemSubGroupIdentify22(el._typeid);

                    // Prevenção de valor zerado no pacote
                    int qtyToSend = (subGroup == 3 || subGroup == 4) ? 1 : Math.Max(1, el.stat.qntd_dep);
                    p.WriteInt32(qtyToSend);

                    p.WriteZero(32);
                    p.WriteUInt16(1);

                    if (subGroup == 3 || subGroup == 4)
                        p.WriteByte((byte)(v_item_response.Count - 1));
                    else
                        p.WriteUInt32(1);
                }

                Player.Send(p);

                // Finalizar Achievements
                sys_achieve.incrementCounter(0x6C400078u);
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_OPEN_CARD_PACK][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x154);
                p.WriteUInt32(1); // Falha
                Player.Send(p);
            }
        }

        private void UpdateCardAchievements(AchievementSystem sys, Card cardDraw)
        {
            // Lógica de incremento baseada no Type de card sorteado
            var subType = (CARD_SUB_TYPE)sIff.Instance.getItemSubGroupIdentify22(cardDraw._typeid);
            switch (subType)
            {
                case CARD_SUB_TYPE.T_CHARACTER: sys.incrementCounter(0x6C400079u); break;
                case CARD_SUB_TYPE.T_CADDIE: sys.incrementCounter(0x6C40007Au); break;
                case CARD_SUB_TYPE.T_SPECIAL: sys.incrementCounter(0x6C40007Bu); break;
                case CARD_SUB_TYPE.T_NPC: sys.incrementCounter(0x6C4000A8u); break;
            }
            sys.incrementCounter(0x6C40007Cu + (uint)cardDraw.tipo);
        }
    }
}