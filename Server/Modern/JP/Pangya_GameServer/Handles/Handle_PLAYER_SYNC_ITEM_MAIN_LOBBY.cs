using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SYNC_ITEM_MAIN_LOBBY : HandleBase<Player, Packet_EXAMPLE>
    {
        private enum SYNC_ITEM_TYPE : byte
        {
            CADDIE = 1,
            BALL = 2,
            CLUBSET = 3,
            CHARACTER = 4,
            MASCOT = 5
        }

        public override async Task Handle()
        {
            SYNC_ITEM_TYPE type = (SYNC_ITEM_TYPE)255;

            try
            {
                type = (SYNC_ITEM_TYPE)Packet.ReadByte();

                int error = 0;
                 
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_SYNC_ITEM_MAIN_LOBBY][Warning] Normal[UID: {Player.UserInfo.UID}, REQ: {type}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                error = type switch
                {
                    SYNC_ITEM_TYPE.CADDIE => HandleChangeCaddie(Player, Packet.ReadInt32()),
                    SYNC_ITEM_TYPE.BALL => HandleChangeComet(Player, Packet.ReadUInt32()),
                    SYNC_ITEM_TYPE.CLUBSET => HandleChangeClubSet(Player, Packet.ReadInt32()),
                    SYNC_ITEM_TYPE.CHARACTER => HandleChangeCharacter(Player, Packet.ReadInt32()),//seguro
                    SYNC_ITEM_TYPE.MASCOT => HandleChangeMascot(Player, Packet.ReadInt32()),
                    _ => throw new exception($"[SyncItem] Tipo desconhecido: {type}",
                                                ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 13, 1)),
                };

                // 1. Atualiza visual para outros Players (Channel/Room)
                Player.GetChannel().UpdatePlayerInfo(Player);

                // 2. Envia confirmação (0x4B) para o cliente
                Player.Send(Handle_PACKET_RESPONSE.pacote04B(Player, (byte)type, error));

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_SYNC_ITEM_MAIN_LOBBY][ErrorSystem] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                Player.Send(Handle_PACKET_RESPONSE.pacote04B(Player, (byte)type,
                    (int)(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL
                        ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1)));
            }
        }
        private int HandleChangeCaddie(Player Player, int item_id)
        {
            CaddieInfoEx pCi = (item_id != 0) ? Player.Inventory.FindCaddieById(item_id) : null;
            int error = 0;

            // 1. Tenta equipar um novo Caddie
            if (item_id != 0)
            {
                // Validação: Existe e é Caddie?
                if (pCi != null && sIff.Instance.getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                {
                    // Checa se o Caddie ou seus Parts expiraram
                    var v_it = Player.Inventory.FindUpdateItemById(pCi.id);
                    if (v_it != null && v_it.Count != 0)
                    {
                        ProcessCaddieUpdateItems(Player, pCi, v_it);

                        // Se após processar o Caddie foi removido (expirou), resetamos para 0
                        if (Player.Inventory.UserEquipment.caddie_id == 0) item_id = 0;
                    }
                    else
                    {
                        // Caddie está íntegro, equipa normalmente
                        Player.Inventory.UserEquippedItem.CaddieEquiped = pCi;
                        Player.Inventory.UserEquipment.caddie_id = item_id;

                        if (Player.CheckCaddieEquiped(Player.Inventory.UserEquipment))
                            item_id = Player.Inventory.UserEquipment.caddie_id;
                    }

                    if (item_id != 0)
                    {
                        SyncCaddieDB(Player.UserInfo.UID, item_id);
                        return 0;
                    }
                }
                else
                {
                    error = (pCi == null) ? 2 : 3;
                }
            }

            // 2. Se item_id for 0 ou houve erro, desequipa o Caddie
            return UnequipCaddie(Player, error);
        }

        private int HandleChangeComet(Player Player, uint item_id)
        {
            WarehouseItemEx pWi = null;
            int error = 0;

            // 1. Tenta equipar a bola solicitada
            if (item_id != 0)
            {
                pWi = Player.Inventory.FindWarehouseItemByTypeid(item_id);

                // Validação: Existe, é do grupo BALL e não expirou?
                if (pWi != null && sIff.Instance.getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                {
                    var c_it = Player.Inventory.FindUpdateItemById(pWi.id);
                    bool isExpired = (pWi.STDA_C_ITEM_TIME > 0 && (c_it == null || c_it.Count == 0));

                    if (!isExpired)
                    {
                        Player.Inventory.UserEquippedItem.Ball_WI = pWi;
                        Player.Inventory.UserEquipment.ball_typeid = pWi._typeid;

                        if (Player.CheckBallEquiped(Player.Inventory.UserEquipment))
                            item_id = Player.Inventory.UserEquipment.ball_typeid;

                        SyncBallDB(Player.UserInfo.UID, item_id);
                        return 0; // Sucesso
                    }
                    error = 6; // Item Expirado
                }
                else
                {
                    error = (pWi == null) ? 2 : 3; // Não encontrado ou grupo errado
                }
            }

            // 2. Fallback: Se chegou aqui, item_id era 0 ou houve erro
            return CreateDefaultComet(Player, error);
        }

        private int HandleChangeClubSet(Player Player, int item_id)
        {
            WarehouseItemEx pWi = Player.Inventory.FindWarehouseItemById(item_id);
            
            // 1. Validação de existência básica
            if (item_id == 0) return CreateDefaultClubSet(Player, item_id, 1);
            if (pWi == null) return CreateDefaultClubSet(Player, item_id, 2);

            // 2. Validação de Tipo (IFF)
            if (sIff.Instance.getItemGroupIdentify(pWi._typeid) != IFF_GROUP.CLUBSET)
                return CreateDefaultClubSet(Player, item_id, 3);

            var cs_iff = sIff.Instance.findClubSet(pWi._typeid);
            if (cs_iff == null)
                return CreateDefaultClubSet(Player, item_id, 5);

            // 3. Validação de Tempo (O Ponto Crítico)
            // Se o item tem configuração de tempo (STDA_C_ITEM_TIME > 0), ele é temporário.
            var c_it = Player.Inventory.FindUpdateItemById(item_id);
            bool isTemporary = pWi.STDA_C_ITEM_TIME > 0;

            if (isTemporary && (c_it == null || c_it.Count == 0))
            {
                // O item é temporário mas não está na lista de updates ativos (Expirou)
                return CreateDefaultClubSet(Player, item_id, 6);
            }

            // 4. Sucesso: Equipar o Item
            Player.Inventory.EquipClubSetAction(pWi);

            // Atualiza o item_id caso a ação de equipar tenha alterado algo (ex: IDs de slots)
            if (Player.CheckClubSetEquiped(Player.Inventory.UserEquipment))
                item_id = Player.Inventory.UserEquipment.clubset_id;

            SyncClubDB(Player.UserInfo.UID, item_id);
            return 0;
        }
         

        private int HandleChangeCharacter(Player Player, int item_id)
        {
            CharacterInfo pCe = (item_id != 0) ? Player.Inventory.FindCharacterById(item_id) : null;
            int error = 0;

            if (item_id == 0 || pCe == null || sIff.Instance.getItemGroupIdentify(pCe._typeid) != IFF_GROUP.CHARACTER)
            {
                error = (item_id == 0) ? 1 : (pCe == null ? 2 : 3);
            }
            //é diferente do outro
            Player.Inventory.SyncCharacter(item_id);
            //UPDATE ON DB
            SyncCharacterDB(Player.Inventory.uid, item_id); 
            return error;
        }

        private int HandleChangeMascot(Player Player, int item_id)
        {
            MascotInfoEx pMi = null;
            int error = 0;

            // 1. Tentativa de Equipar (item_id > 0)
            if (item_id != 0)
            {
                pMi = Player.Inventory.FindMascotById(item_id);

                // Validação de Existência e Grupo IFF
                if (pMi != null && sIff.Instance.getItemGroupIdentify(pMi._typeid) == IFF_GROUP.MASCOT)
                {
                    // Verifica expiração (Update Items)
                    var m_it = Player.Inventory.FindUpdateItemById(Player.Inventory.UserEquipment.mascot_id);

                    if (m_it.Count > 0)
                    {
                        item_id = 0; // Expulsa o item (expirado)
                        Player.Inventory.UserEquippedItem.MascotEquiped = null;
                        Player.Inventory.UserEquipment.mascot_id = 0;
                    }
                    else
                    {
                        // Equipamento válido
                        Player.Inventory.UserEquippedItem.MascotEquiped = pMi;
                        Player.Inventory.UserEquipment.mascot_id = item_id;

                        // Validação final de integridade do servidor
                        if (Player.CheckMascotEquiped(Player.Inventory.UserEquipment))
                        {
                            item_id = Player.Inventory.UserEquipment.mascot_id;
                        }
                    }

                    SyncMascotDB(Player.UserInfo.UID, item_id);
                }
                else
                {
                    // Caso de Erro: Item não existe ou grupo errado
                    error = (pMi == null) ? 2 : 3;
                    // Força desequipar
                    Player.Inventory.UserEquippedItem.MascotEquiped = null;
                    Player.Inventory.UserEquipment.mascot_id = 0; 
                    SyncMascotDB(Player.UserInfo.UID, 0);
                }
            }
            // 2. Fluxo de Desequipar Manual (item_id == 0)
            else if (Player.Inventory.UserEquipment.mascot_id > 0)
            {
                Player.Inventory.UserEquippedItem.MascotEquiped = null;
                Player.Inventory.UserEquipment.mascot_id = 0;
                SyncMascotDB(Player.UserInfo.UID, 0);
            }

            return error;
        }

        private void SyncMascotDB(uint uid, int item_id)
        {
            // Update ON DB 
            NormalManagerDB.Instance.add(0, new CmdUpdateMascotEquiped(uid, item_id));
        }

        private void SyncCharacterDB(uint uid, int item_id)
        {
            NormalManagerDB.Instance.add(0, new CmdUpdateCharacterEquiped(uid, item_id));
        }

        private void SyncClubDB(uint uid, int item_id)
        {
            NormalManagerDB.Instance.add(0, new CmdUpdateClubsetEquiped(uid, item_id));
        }

        private void SyncCaddieDB(uint uid, int item_id)
        {
            NormalManagerDB.Instance.add(0, new CmdUpdateCaddieEquiped(uid, item_id));
        }

        private void SyncBallDB(uint uid, uint item_typeid)
        {
            NormalManagerDB.Instance.add(0, new CmdUpdateBallEquiped(uid, item_typeid));
        }

        private int CreateDefaultClubSet(Player Player, int original_id, int error_code)
        {
            var pWi = Player.Inventory.FindWarehouseItemByTypeid(DEFAULT_CLUB_TYPEID);

            // Se não tem a CV1, tenta adicionar via ItemManager (lógica do BuyItem)
            if (pWi == null)
            {
                pWi = Player.CreateDefaultClubSet();
            }

            if (pWi != null)
            {
                var cs = sIff.Instance.findClubSet(pWi._typeid);
                if (cs != null) Player.Inventory.EquipClubSetAction(pWi);

                SyncClubDB(Player.UserInfo.UID, pWi.id);
                return 0; // Sucesso ao recuperar
            }

            return error_code; // Falha total
        }

        private int CreateDefaultComet(Player Player, int error_code)
        {
            var pWi = Player.Inventory.FindWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

            // Se não tem a CV1, tenta adicionar via ItemManager (lógica do BuyItem)
            if (pWi == null)
            {
                pWi = Player.CreateDefaultBall();
            }

            if (pWi != null)
            {
                var cs = sIff.Instance.findBall(pWi._typeid);
                if (cs != null)
                    SyncBallDB(Player.UserInfo.UID, pWi._typeid);
                return 0; // Sucesso ao recuperar
            }

            return error_code; // Falha total
        }
          
        private void ProcessCaddieUpdateItems(Player Player, CaddieInfoEx pCi, Dictionary<int, UpdateItem> updates)
        {
            foreach (var el in updates)
            {
                if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                {
                    // O Caddie em si expirou
                    Player.Inventory.UserEquippedItem.CaddieEquiped = null;
                    Player.Inventory.UserEquipment.caddie_id = 0;
                }
                else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                {
                    // Apenas os itens do Caddie expiraram, o Caddie continua
                    pCi.parts_typeid = 0;
                    pCi.parts_end_date_unix = 0;
                    pCi.end_parts_date = new SystemTime();

                    Player.Inventory.UserEquippedItem.CaddieEquiped = pCi;
                    Player.Inventory.UserEquipment.caddie_id = pCi.id;
                }
                // Remove o alerta de update para não processar duas vezes
                Player.Inventory.UpdateItems.Remove(el.Key);
            }
        }

        private int UnequipCaddie(Player Player, int error)
        {
            if (error > 1)
                _smp.LogManager.Instance.push(new AppMessage($"[Caddie] Erro {error} para UID={Player.UserInfo.UID}. Desequipando.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            Player.Inventory.UserEquippedItem.CaddieEquiped = null;
            Player.Inventory.UserEquipment.caddie_id = 0;

            SyncCaddieDB(Player.UserInfo.UID, 0);
            return 0;
        }  
    }
}