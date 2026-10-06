using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
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
    public class Handle_PLAYER_OPEN_TICKET_REPORT_SCROLL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            bool _upt_on_game = false;
            try
            {
                int _ticket_scroll_item_id = Packet.ReadInt32();
                int _ticket_scroll_id = Packet.ReadInt32();

                if (_ticket_scroll_item_id < 0 || _ticket_scroll_id < 0)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] ITEM_ID ou ID inválido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2500, 0));
                }

                var pWi = Player.Inventory.FindWarehouseItemById(_ticket_scroll_item_id);
                if (pWi == null)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] Player não tem o item.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2501, 0));
                }

                // Validação C1/C2 (Bitmask)
                uint expectedId = (uint)(pWi.c[1] * 0x800) | (uint)(ushort)pWi.c[2];
                if (expectedId != (uint)_ticket_scroll_id)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] ID do Ticket não bate. Esperado: " + expectedId,
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2502, 0));
                }

                // Busca dados no Banco
                CmdTicketReportDadosInfo cmd_trdi = new CmdTicketReportDadosInfo(_ticket_scroll_id);
                NormalManagerDB.Instance.add(0, cmd_trdi, null, null);

                if (cmd_trdi.getException().getCodeError() != 0)
                    throw cmd_trdi.getException();

                var trsi = cmd_trdi.getInfo();
                if (trsi == null) throw new Exception("Dados do ticket retornaram nulos.");

                // 1. Calcular EXP ANTES de remover o item
                var PlayerStat = trsi.v_players.FirstOrDefault(_el => _el.uid == Player.UserInfo.UID);
                int expToGain = (PlayerStat != null && PlayerStat.exp > 0) ? (int)PlayerStat.exp : 0;

                // 2. Remover o Item
                stItem itemRem = new stItem();
                itemRem.type = 2;
                itemRem.id = pWi.id;
                itemRem._typeid = pWi._typeid;
                itemRem.STDA_C_ITEM_QNTD = (short)(pWi.STDA_C_ITEM_QNTD * -1);

                if (ItemManager.removeItem(itemRem, Player) <= 0)
                {
                    throw new exception("[item_manager::openTicketReportScroll][Error] Falha ao deletar item.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2503, 0));
                }

                // 3. Checar se o item estava expirado (UpdateItem)
                var ui_it = Player.Inventory.FindUpdateItemById(_ticket_scroll_item_id);

                // CORREÇÃO DO CRASH: Checar se ui_it não é nulo antes do Count
                if (ui_it != null && ui_it.Count > 0)
                {
                    // Se expirou, remove do mapa de updates
                    Player.Inventory.UpdateItems.Remove(ui_it.First().Key);

                    // Se expirou, damos a Experience e paramos por aqui com erro de expiração
                    if (expToGain > 0) Player.addExp(expToGain, _upt_on_game);

                    throw new exception("Item expirado, mas EXP concedida.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE._ITEM_MANAGER, 2504, 0));
                }

                // 4. Fluxo Normal (Sucesso)
                _smp.LogManager.Instance.push(new AppMessage("[item_manager::openTicketReportScroll][Log] Player " + Player.UserInfo.UID + " abriu ticket com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia pacote de resposta do Ticket
                p = new Packet(0x11A);
                p.WriteInt32(trsi.v_players.Count());
                p.WriteTime(trsi.date);
                foreach (var el in trsi.v_players)
                { p.WriteBytes(el.ToArray()); }

                Player.Send(p);

                // 5. Adiciona EXP por ÚLTIMO (para o visual do Pangya não bugar)
                if (expToGain > 0)
                {
                    Player.addExp(expToGain, _upt_on_game);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestOpenTicketReportScroll][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error;
                p.init_plain(0x11A);

                p.WriteInt32(-1); // Error
                p.WriteZero(16); // Date

                Player.Send(p);
            }

        await Task.CompletedTask;
        }
    }
}