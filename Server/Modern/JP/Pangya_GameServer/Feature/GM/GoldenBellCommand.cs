using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Feature.GM
{
    public class GoldenBellCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        {
            try
            {
                // 1. Verificação de Restrição de Item
                if (session.UserInfo.UserCapabilities.IsGameMasterBlockItemGive)
                {
                    throw new exception($"[GM::GoldenBell] Player[UID={session.UserInfo.UID}] tentou ativar Golden Bell, mas possui restrição de itens.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700100));
                }

                var channel = session.GetChannel();

                // 2. Validação de Contexto (Canal)
                if (channel == null)
                {
                    throw new exception($"[GM::GoldenBell] Player[UID={session.UserInfo.UID}] não está em um canal para executar o broadcast.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));
                }

                // 3. Localização e Validação da Sala
                var room = session.GetRoom();

                if (room == null)
                {
                    throw new exception($"[GM::GoldenBell] Sala {session.UserInfo.Member.RoomID} do player [UID={session.UserInfo.UID}] não foi encontrada.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0x5700100));
                }

                // 4. Execução do Evento
                // O método na Room gerencia os pacotes de efeito visual e a lógica de prêmios
                if (room != null)
                {
                    // 2. Extração e Validação de Dados do Item
                    uint itemTypeId = pkt.ReadUInt32();
                    uint itemQuantity = pkt.ReadUInt32();

                    ValidateGoldenBellRequest(session, itemTypeId, itemQuantity);

                    // 3. Busca Informações do Item no IFF
                    var itemInfo = sIff.Instance.findCommomItem(itemTypeId);
                    if (itemInfo == null)
                    {
                        throw new exception($"[Room::GoldenBell] Item ID {itemTypeId} não existe no IFF do servidor.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));
                    }

                    // 4. Preparação do Template de Compra/Entrega
                    var buyItemTemplate = new BuyItem
                    {
                        id = -1,
                        _typeid = itemTypeId,
                        qntd = itemQuantity
                    };

                    string mailMessage = $"GM enviou um item para você: [{itemInfo.Name}]";

                    // 5. Distribuição em Massa (Loop de Entrega)
                    foreach (var target in room.Players.ToArray())
                    {
                        DeliverItemViaMail(session, target, buyItemTemplate, mailMessage);
                    } 

                    // Log de Sucesso (Console + File)
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::GoldenBell][Success] {session.UserInfo.NickName} ativou o evento na Sala {room.GetRoomId()} (Canal: {channel.getName()})",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GM::GoldenBell][Warning] {session.UserInfo.NickName} tentou ativar Golden Bell fora de uma sala.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[GoldenBellCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
         
        #region Helpers de Logística

        private void ValidateGoldenBellRequest(Player session, uint typeId, uint quantity)
        {
            if (typeId == 0)
            {
                throw new exception($"[Room::GoldenBell] UID {session.UserInfo.UID} tentou enviar ItemID 0 (Inválido).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));
            }

            if (quantity > 20000)
            {
                throw new exception($"[Room::GoldenBell] UID {session.UserInfo.UID} tentou enviar quantidade excessiva ({quantity}).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));
            }
        }

        private void DeliverItemViaMail(Player gm, Player target, BuyItem buyItem, string messageText)
        {
            stItem item = new stItem();
            ItemManager.initItemFromBuyItem(target.UserInfo, item, buyItem, false, 0, 0, 1);

            // Verificação de Integridade Pós-Inicialização
            if (item._typeid == 0 || item.qntd != buyItem.qntd)
            {
                throw new exception($"[Room::GoldenBell] Falha ao inicializar item para UID {target.UserInfo.UID}.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));
            }

            // Envio para o MailBox (Persistência no DB)
            if (MailManager.SendMessageWithItem(0, target.UserInfo.UID, messageText, item) <= 0)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Room::GoldenBell][Fail] Não foi possível entregar item para UID {target.UserInfo.UID}.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        #endregion 
    }
}