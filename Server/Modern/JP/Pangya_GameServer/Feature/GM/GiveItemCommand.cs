using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Feature.GM
{
    public class GiveItemCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet packet)
        {  

            if (session.UserInfo.UserCapabilities.IsGameMasterBlockItemGive)
                throw new exception($"Normal[UID={session.UserInfo.UID}] bloqueado para dar itens.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 9, 0x5700100));

            // 3. Leitura do Pacote
            int targetOid = packet.ReadInt32();
            uint itemTypeId = packet.ReadUInt32();
            uint itemQuantity = packet.ReadUInt32();

            if (targetOid < 0)
                throw new exception("Item OID inválido (-1).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100)); 

            // 4. Validação do Alvo
            var target = GameServer.Instance.FindSessionByOid(targetOid);
            if (target == null)
                throw new exception($"Alvo [OID={targetOid}] não encontrado.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x5700100));

            // 5. Validações do Item e IFF
            if (itemTypeId == 0)
                throw new exception("Item TypeID inválido (0).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

            if (itemQuantity > 20000)
                throw new exception("Quantidade excede o limite de 20k.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

            var itemBase = sIff.Instance.findCommomItem(itemTypeId);
            if (itemBase == null || itemBase.ID != itemTypeId)
                throw new exception($"Item [0x{itemTypeId:X8}] não existe no IFF.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

            // 6. Inicialização e Envio
            stItem item = new stItem();
            BuyItem bi = new BuyItem { id = -1, _typeid = itemTypeId, qntd = itemQuantity };

            // O '1' ao final ignora o check de Level, conforme seu código anterior
            ItemManager.initItemFromBuyItem(target.UserInfo, item, bi, false, 0, 0, 1);

            if (item._typeid == 0)
                throw new exception("Falha ao inicializar struct do item.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

            var logMsg = $"GM Send Gift: item[ {itemBase.Name} ]";

            // 7. Entrega via MailBox
            if (MailManager.SendMessageWithItem(0, target.UserInfo.UID, logMsg, item) <= 0)
                throw new exception("Falha ao inserir item no MailBox.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));
        }
    }
}