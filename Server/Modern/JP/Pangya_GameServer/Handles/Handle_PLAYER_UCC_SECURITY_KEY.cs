using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_UCC_SECURITY_KEY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var response = new Packet();

            try
            {
                // 1. Leitura dos dados do pacote
                byte opt = Packet.ReadByte();
                uint targetUid = Packet.ReadUInt32();
                byte seq = Packet.ReadByte();
                int itemId = Packet.ReadInt32();

                // 2. Validações de integridade
                if (targetUid == 0 || itemId <= 0)
                {
                    throw new exception($"[UCC_SECURITY] Dados inválidos enviados por UID={Player.UserInfo.UID}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5100101));
                }

                // 3. Verificação de permissão GM (PlayerUserStatistics)
                if (!Player.UserInfo.UserCapabilities.IsGameMaster)
                {
                    throw new exception($"[UCC_SECURITY][Error] Normal[UID={Player.UserInfo.UID}] não é GM.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));
                }

                // 4. Localização do Player alvo
                var targetPlayer = (Player)GameServer.Instance.FindSessionByUid(targetUid);

                if (targetPlayer == null)
                {
                    throw new exception($"[UCC_SECURITY] Alvo [UID={targetUid}] offline.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5100103));
                }

                // 5. Busca do item no Warehouse do alvo
                var warehouseItem = targetPlayer.Inventory.FindWarehouseItemById(itemId);

                if (warehouseItem == null)
                {
                    throw new exception($"[UCC_SECURITY] Item {itemId} não encontrado no Warehouse do alvo.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5100104));
                }

                // 6. Geração da Chave no Banco de Dados
                var key = CommandDB.GenerationSecurityKey(Player.UserInfo.UID, warehouseItem.id);

                // 7. Resposta de Sucesso (Packet 0x153)
                response.init_plain(0x153);
                response.WriteByte(0); // Status OK
                response.WriteByte(1); // OK
                response.WriteInt32(warehouseItem.id);
                response.WriteString(key);
                response.WriteByte(seq);

                Player.Send(response);

                Console.WriteLine($"[UCC-Security] Chave gerada com sucesso para {targetPlayer.UserInfo.NickName} por {Player.UserInfo.NickName}");
            }
            catch (exception e)
            {
                Console.WriteLine($"[UCC_SECURITY][Error] {e.getFullMessageError()}");

                response.init_plain(0x153);
                response.WriteByte(1); // Status Error
                response.WriteByte(1);

                uint errorType = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.GAME_SERVER)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError())
                    : 0x5100100;

                response.WriteUInt32(errorType);

                Player.Send(response);
            }

        await Task.CompletedTask;
        }
    }
}