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

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
#if RELEASE
                // Log de depuração para ambiente de lançamento
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Success] Normal [UID=" + Player.UserInfo.UID + "] solicitou troca na Tiki Shop.", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif
                // 1. Verificação de Bloqueio
                if (Player.UserInfo.BlockFlag.Flag.LegacyTikiShop)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Normal [UID=" + Player.UserInfo.UID + "] está bloqueado no Legacy Tiki Shop.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4000, 1));
                }

                uint total_tiki_pts_cost = 0;
                List<stItem> v_item_to_add = new List<stItem>();
                AchievementSystem sys_achieve = new AchievementSystem();

                // 2. Leitura e Validação dos Itens
                uint count = Packet.ReadByte();

                for (var i = 0; i < count; ++i)
                {
                    var tsetp = new stLegacyTikiShopExchangeTP().ToRead(Packet);

                    // Valida se o item base existe no IFF
                    var @base = sIff.Instance.findCommomItem(tsetp._typeid);
                    if (@base == null)
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Item [TYPEID=" + tsetp._typeid + "] não existe no IFF.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0x5200902));
                    }

                    // Valida se o item está cadastrado na PointShop (Tiki Shop)
                    var point_shop = sIff.Instance.findPointShop(tsetp._typeid);
                    if (point_shop == null)
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Item [TYPEID=" + tsetp._typeid + "] não está na PointShop.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0x5200902));
                    }

                    // Acumula o custo total em pontos
                    total_tiki_pts_cost += (uint)(point_shop.Points * tsetp.qntd);

                    // Inicializa a estrutura do item para adição
                    stItem item = new stItem();
                    BuyItem bi = new BuyItem();
                    bi.id = -1;
                    bi._typeid = tsetp._typeid;
                    bi.qntd = (uint)(point_shop.Quantity * tsetp.qntd);

                    ItemManager.initItemFromBuyItem(Player.UserInfo, item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Falha ao inicializar Item [TYPEID=" + bi._typeid + "].",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0x5200902));
                    }

                    v_item_to_add.Add(new stItem(item));
                }

                // 3. Verificações Finais de Custo
                if (total_tiki_pts_cost == 0u)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Custo total de Tiki Points inválido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 905, 0x5200905));
                }

                if (total_tiki_pts_cost > Player.UserInfo.PointShopLegacy)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Pontos insuficientes [HAVE=" + Player.UserInfo.PointShopLegacy + ", COST=" + total_tiki_pts_cost + "].",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 906, 0x5200906));
                }

                // 4. Atualização de Saldo e Banco de Dados
                Player.UserInfo.PointShopLegacy -= total_tiki_pts_cost;

                NormalManagerDB.Instance.add(0, new CmdUpdateLegacyTikiShopPoint(Player.UserInfo.UID, Player.UserInfo.PointShopLegacy));

                // 5. Inserção dos Itens no Inventário
                var rai = ItemManager.addItem(v_item_to_add, Player, 0, 0);

                if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    // Tratamento detalhado de falha na adição (Ex: Inventário Cheio)
                    StringBuilder str = new StringBuilder();
                    foreach (var fail in rai.fails)
                    {
                        str.Append($"[TYPEID={fail._typeid}, ID={fail.id}] ");
                    }
                    throw new exception("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][Error] Falha ao adicionar itens: " + str.ToString(),
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 907, 0x5200907));
                }

                // 6. Atualização de Conquistas (Achievement)
                sys_achieve.incrementCounter(0x6C400086u, 1);

                // 7. Sincronização de Pacotes

                // Pacote 0x216: Atualiza os itens visualmente no jogo (Inventário/Notificação)
                p.init_plain(0x216);
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item_to_add.Count);

                foreach (var el in v_item_to_add)
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

                // Pacote 0x1EA: Confirmação da troca e atualização do saldo de pontos na UI da loja
                p.init_plain(0x1EA);
                p.WriteUInt32(0u); // Sucesso
                p.WriteUInt32((uint)Player.UserInfo.PointShopLegacy);
                Player.Send(p);

                // Finaliza e sincroniza as conquistas
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_EXCHANGE_ITEM_BY_TP_LEGACY_TIKI_SHOP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1EA);
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 1u;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}