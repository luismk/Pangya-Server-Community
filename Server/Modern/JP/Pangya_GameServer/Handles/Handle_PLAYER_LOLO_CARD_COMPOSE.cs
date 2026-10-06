using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;
using System.Linq;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_LOLO_CARD_COMPOSE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Verificação de Bloqueio de Feature
                if (Player.UserInfo.BlockFlag.Flag.LoloCopoundCard)
                {
                    throw new exception("[Handle_PLAYER_LOLO_CARD_COMPOSE][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fundir card, mas está bloqueado.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0x790001));
                }

                LoloCardComposeEx lcc = new LoloCardComposeEx().ToRead(Packet) as LoloCardComposeEx;
                List<stItem> v_items_to_sync = new List<stItem>(); // Itens para sync final (removidos e adicionados)
                List<stItem> v_items_to_remove = new List<stItem>();
                AchievementSystem sys_achieve = new AchievementSystem();
                ulong total_pang_cost = 0;

                var r = Player.GetRoom();

                // 2. Validação das Cartas de Entrada
                for (int i = 0; i < lcc._typeid.Length; i++)
                {
                    uint current_typeid = lcc._typeid[i];
                    var card_iff = sIff.Instance.findCard(current_typeid);

                    if (card_iff == null)
                    {
                        throw new exception($"[Handle] Card [TYPEID={current_typeid}] não existe no IFF.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 150, 0x5400151));
                    }

                    // Proibe fusão de cartas SECRET
                    if (card_iff.Rarity == (byte)CARD_TYPE.T_SECRET)
                    {
                        throw new exception("[Handle] Não é permitido fundir cartas do Type SECRET.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 151, 0x5400152));
                    }

                    // Verifica se o Player possui a carta no inventário
                    var pCi = Player.Inventory.FindCardByTypeid(card_iff.ID);
                    if (pCi == null || pCi.qntd < 1)
                    {
                        throw new exception($"[Handle] Player não possui a carta [TYPEID={current_typeid}].",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 152, 0x5400153));
                    }

                    // Anti-Exploit: Verifica se o item está à venda no Personal Shop
                    if (r != null && r.CheckPersonalShopItem(Player, pCi.id))
                    {
                        throw new exception($"[Handle] Card [ID={pCi.id}] está à venda no ShopRoom pessoal.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                    }

                    // Agrupa itens para remoção
                    var existing_item = v_items_to_remove.FirstOrDefault(el => el.id == pCi.id);
                    if (existing_item != null)
                    {
                        existing_item.qntd += 1;
                        existing_item.STDA_C_ITEM_QNTD = (short)(existing_item.qntd * -1);
                    }
                    else
                    {
                        stItem newItem = new stItem
                        {
                            type = 2,
                            _typeid = current_typeid,
                            id = (int)pCi.id,
                            qntd = 1
                        };
                        newItem.STDA_C_ITEM_QNTD = (short)(newItem.qntd * -1);
                        v_items_to_remove.Add(newItem);
                    }

                    // Calcula custo baseado na raridade
                    lcc.tipo = card_iff.Rarity;
                    total_pang_cost += (uint)(lcc.tipo == (byte)CARD_TYPE.T_NORMAL ? 1000 :
                                       (lcc.tipo == (byte)CARD_TYPE.T_RARE ? 2000 :
                                       (lcc.tipo == (byte)CARD_TYPE.T_SUPER_RARE ? 5000 : 1000)));
                }

                // 3. Validação de Custo de Pang
                if (total_pang_cost != lcc.pang)
                {
                    throw new exception($"[Handle] Mismatch de Pangs. Server: {total_pang_cost}, Client: {lcc.pang}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 154, 0x5400155));
                }

                // 4. Sorteio da Nova Carta (Gacha)
                var new_card = sCardSystem.Instance.drawsLoloCardCompose(lcc);
                if (new_card == null || new_card._typeid == 0)
                {
                    throw new exception("[Handle] Falha no sorteio da carta (sCardSystem).",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 155, 0x5400156));
                }

                // 5. Execução: Remover cartas antigas
                if (ItemManager.removeItem(v_items_to_remove, Player) <= 0)
                {
                    throw new exception("[Handle] Falha crítica ao remover as cartas do inventário.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 156, 0x5400157));
                }
                v_items_to_sync.AddRange(v_items_to_remove);

                // 6. Execução: Adicionar nova carta
                stItem item_ganho = new stItem();
                BuyItem bi = new BuyItem { id = -1, _typeid = new_card._typeid, qntd = 1 };
                ItemManager.initItemFromBuyItem(Player.UserInfo, item_ganho, bi, false, 0, 0, 1);

                var rt = ItemManager.addItem(item_ganho, Player, 0, 0);
                if (rt < 0)
                {
                    throw new exception("[Handle] Falha ao adicionar a nova carta sorteada.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 158, 0x5400159));
                }

                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_items_to_sync.Add(item_ganho);
                }

                // 7. Atualização Financeira e Conquistas
               Player.UserInfo.consomePang(total_pang_cost);
                sys_achieve.incrementCounter(0x6C40008Au + (uint)new_card.tipo); // Tipo sorteado
                sys_achieve.incrementCounter(0x6C400089u); // Contador geral de fusão

                // 8. Sincronização de Pacotes (Pang -> Itens -> Resultado Lolo)

                // Pacote 0xC8: Update Pang
                p.init_plain(0xC8);
                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt64(total_pang_cost);
                Player.Send(p);

                // Pacote 0x216: Update Items (Visual)
                p.init_plain(0x216);
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_items_to_sync.Count);
                foreach (var el in v_items_to_sync)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                }
                Player.Send(p);

                // Resposta visual do resultado (0x229 e 0x22A)
                p.init_plain(0x229);
                p.WriteUInt32((uint)new_card.tipo);
                Player.Send(p);

                p.init_plain(0x22A);
                p.WriteUInt32(0); // OK
                p.WriteUInt32(new_card._typeid);
                Player.Send(p);

                // Finaliza Achievements
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LOLO_CARD_COMPOSE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x22A);
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x5400150;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}