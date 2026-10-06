using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_PLAY_MEMORIAL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            Packet p = new Packet();

            try
            {
                // 1. Validações Iniciais
                if (Player.UserInfo.BlockFlag.Flag.MemorialShop)
                {
                    throw new exception($"[Memorial] Player {Player.UserInfo.UID} bloqueado.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x790001));
                }

                if (!sMemorialSystem.Instance.isLoad())
                    sMemorialSystem.Instance.load();

                uint coin_typeid = Packet.ReadUInt32();

                if (coin_typeid == 0)
                    throw new exception("[Memorial] Coin TypeID inválido (zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x6300301));

                if (sIff.Instance.getItemGroupIdentify(coin_typeid) != IFF_GROUP.ITEM)
                    throw new exception("[Memorial] O item enviado não é uma moeda válida.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x6300302));

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(coin_typeid);
                if (pWi == null)
                    throw new exception("[Memorial] Player não possui a moeda no inventário.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x6300303));

                var coinIff = sIff.Instance.findItem(pWi._typeid);
                if (coinIff == null || !coinIff.Active)
                    throw new exception("[Memorial] Moeda não encontrada no IFF do Server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4, 0x6300304));

                var memorialCoin = sMemorialSystem.Instance.findCoin(coinIff.ID);
                if (memorialCoin == null)
                    throw new exception("[Memorial] Moeda não registrada no Memorial System.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 5, 0x6300305));

                // 2. Sorteio e Achievements
                AchievementSystem sys_achieve = new AchievementSystem();

                if (memorialCoin.tipo == MEMORIAL_COIN_TYPE.MCT_NORMAL)
                    sys_achieve.incrementCounter(0x6C4000B2u);
                else if (memorialCoin.tipo == MEMORIAL_COIN_TYPE.MCT_SPECIAL)
                    sys_achieve.incrementCounter(0x6C4000B3u);

                var win_item = sMemorialSystem.Instance.drawCoin(Player, memorialCoin);
                if (win_item == null || win_item.Count == 0)
                    throw new exception("[Memorial] Sorteio retornou vazio.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x6300306));

                List<stItem> v_item_to_sync = new List<stItem>();

                // 3. Processamento dos Itens Ganhos
                foreach (var el in win_item)
                {
                    BuyItem bi = new BuyItem { id = -1, _typeid = el._typeid };
                    stItem item_tmp = new stItem();

                    // Lógica de Mascote por tempo
                    var mascot = sIff.Instance.findMascot(el._typeid);
                    if (sIff.Instance.getItemGroupIdentify(el._typeid) == IFF_GROUP.MASCOT
                        && mascot != null && mascot.Shop.flag_shop.time_shop.dia > 0 && mascot.Shop.flag_shop.time_shop.active)
                    {
                        bi.qntd = 1;
                        bi.time = (short)(ushort)el.qntd;
                    }
                    else
                    {
                        bi.qntd = el.qntd;
                    }

                    ItemManager.initItemFromBuyItem(Player.UserInfo, item_tmp, bi, false, 0, 0, 1);

                    if (item_tmp._typeid == 0)
                        throw new exception($"[Memorial] Falha ao inicializar item ganho: {bi._typeid}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0x6300307));

                    // Lógica de duplicidade e Sets
                    bool canOverlap = sIff.Instance.IsCanOverlapped(item_tmp._typeid);
                    bool isCadItem = sIff.Instance.getItemGroupIdentify(item_tmp._typeid) == IFF_GROUP.CAD_ITEM;

                    if ((canOverlap && !isCadItem) || !Player.Inventory.ownerItem(item_tmp._typeid))
                    {
                        if (ItemManager.isSetItem(item_tmp._typeid))
                        {
                            var v_setItems = ItemManager.GetItemOfSetItem(Player, item_tmp._typeid, false, 1);
                            if (v_setItems.Count == 0)
                                throw new exception("[Memorial] SetItem ganho não possui itens internos.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x6300308));

                            foreach (var si in v_setItems)
                            {
                                if ((sIff.Instance.IsCanOverlapped(si._typeid) && sIff.Instance.getItemGroupIdentify(si._typeid) != IFF_GROUP.CAD_ITEM) || !Player.Inventory.ownerItem(si._typeid))
                                    v_item_to_sync.Add(new stItem(si));
                            }
                        }
                        else
                        {
                            v_item_to_sync.Add(new stItem(item_tmp));
                        }
                    }
                    else if (isCadItem)
                    {
                        throw new exception("[Memorial] Erro de CaddieItem ou Caddie ausente.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 9, 0x6300309));
                    }
                    else
                    {
                        throw new exception("[Memorial] Player já possui o item (duplicidade proibida).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0x6300310));
                    }

                    // Achievement Raro
                    if (el.tipo >= 0 && el.tipo < 3) sys_achieve.incrementCounter(0x6C4000B5u);
                    else if (el.tipo >= 3) sys_achieve.incrementCounter(0x6C4000B4u);
                }

                // 4. Consumo da Moeda e Persistência
                stItem coin_to_remove = new stItem
                {
                    type = 2,
                    id = (int)pWi.id,
                    _typeid = memorialCoin._typeid,
                    qntd = 1,
                    STDA_C_ITEM_QNTD = -1
                };

                if (ItemManager.removeItem(coin_to_remove, Player) <= 0)
                    throw new exception("[Memorial] Falha ao deletar a moeda utilizada.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 11, 0x6300311));

                var rai = ItemManager.addItem(v_item_to_sync, Player, 0, 0);
                if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    throw new exception("[Memorial] Erro ao adicionar itens ganhos ao banco de dados.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 12, 0x6300312));

                // Log de Item Raro
                if (win_item.Any() && win_item[0].tipo > 0 && win_item.Count == 1)
                {
                    NormalManagerDB.Instance.add(24, new CmdInsertMemorialRareWinLog(Player.UserInfo.UID, memorialCoin._typeid, win_item.FirstOrDefault()));
                }

                // 5. Envio de Pacotes
                v_item_to_sync.Add(new stItem(coin_to_remove)); // Adiciona a moeda (com qnt negativa) para atualizar o inventário

                // 0x216: Sincronização de Inventário
                Packet pSync = new Packet(0x216);
                pSync.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                pSync.WriteUInt32((uint)v_item_to_sync.Count);
                foreach (var el in v_item_to_sync)
                {
                    pSync.WriteByte(el.type);
                    pSync.WriteUInt32(el._typeid);
                    pSync.WriteInt32(el.id);
                    pSync.WriteUInt32(el.flag_time);
                    pSync.WriteBytes(el.stat.ToArray());
                    pSync.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    pSync.WriteZero(25);
                }
                Player.Send(pSync);

                // 0x264: Resposta do Memorial (Animação visual dos itens ganhos)
                Packet pResult = new Packet(0x264);
                pResult.WriteUInt32(0); // OK
                pResult.WriteUInt32((uint)win_item.Count);
                foreach (var el in win_item)
                {
                    pResult.WriteInt32(el.tipo);
                    pResult.WriteUInt32(el._typeid);
                    pResult.WriteUInt32(el.qntd);
                }
                Player.Send(pResult);

                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Memorial] Erro: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                Packet pErr = new Packet(0x264);
                uint errCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                               ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                               : 0x6300300;
                pErr.WriteUInt32(errCode);
                Player.Send(pErr);
            }
        }
    }
}