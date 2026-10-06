using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System.Numerics;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    /// <summary>
    /// pequeno problema dectado, nao pode excluir a sala antes de sicronizacar disso......
    /// </summary>
    public class Handle_PLAYER_SYNC_ITEM_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        /// <summary>
        /// Define os tipos de sincronização de itens entre o Cliente e o Servidor.
        /// Geralmente utilizado no pacote 0x4B (Sync Check) antes de iniciar uma partida.
        /// </summary>
        public enum SYNC_ITEM_TYPE : byte
        {
            // Sincroniza a alteração do Caddie equipado
            SYNC_CADDIE = 1,

            // Sincroniza a alteração da Comet (Bola)
            SYNC_BALL = 2,

            // Sincroniza a alteração do conjunto de tacos (ClubSet)
            SYNC_CLUBSET = 3,

            // Sincroniza a alteração do Personagem
            SYNC_CHARACTER_MAIN = 4,

            // Sincroniza a alteração do Mascote
            SYNC_MASCOT = 5,

            // Itens de efeito especial (Hermes x2, Twilight, Jester x2, etc.)
            SYNC_ITEM_EFFECT_LOUNGE = 6,

            // Sincronização TOTAL (Executa na ordem: Char, Caddie, ClubSet e Ball)
            // Usado como validação final antes do StartGame
            SYNC_ALL = 7,

            // Tipo desconhecido ou erro de protocolo
            SYNC_UNKNOWN = 255
        }

        public enum LOUNGE_EFFECT_TYPE : uint
        {
            BIG_HEAD = 1,
            FAST_WALK,
            TWILIGHT,
        }

        public override async Task Handle()
        {
            int error = 0; // Começamos com sucesso
            try
            {
                var r = Player.GetRoom();

                if (r == null)
                { 
                        return;
                }
                
                var type = (SYNC_ITEM_TYPE)Packet.ReadByte();
                 
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_SYNC_ITEM_GAMEROOM][Warning] Normal[UID: {Player.UserInfo.UID}, REQ: {type}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                switch (type)
                {
                    case SYNC_ITEM_TYPE.SYNC_CHARACTER_MAIN:
                        // Se você já tiver o HandleChangeCharacter, use-o aqui
                        error = HandleChangeCharacter(Player, Packet.ReadInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_CADDIE:
                        error = HandleChangeCaddie(Player, Packet.ReadInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_BALL:
                        error = HandleChangeBall(Player, Packet.ReadUInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_CLUBSET:
                        error = HandleChangeClubSet(Player, Packet.ReadInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_MASCOT:
                        error = HandleChangeMascot(Player, Packet.ReadInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_ITEM_EFFECT_LOUNGE:
                        error = HandleChangeItemSpecial(Player, Packet.ReadInt32(), Packet.ReadInt32());
                        break;

                    case SYNC_ITEM_TYPE.SYNC_ALL:
                        error |= HandleChangeCharacter(Player, Packet.ReadInt32());
                        error |= HandleChangeCaddie(Player, Packet.ReadInt32());
                        error |= HandleChangeClubSet(Player, Packet.ReadInt32());
                        error |= HandleChangeBall(Player, Packet.ReadUInt32());
                        break;
                    default:
                        error = 1;
                        _smp.LogManager.Instance.push(new AppMessage($"[Room::ChangeItem] Tipo de Sync desconhecido: {type}", type_msg.CL_ONLY_CONSOLE));
                        break;
                }

                r.SendBroadCast(Handle_PACKET_RESPONSE.pacote04B(Player, (byte)type, error));

                if (type == SYNC_ITEM_TYPE.SYNC_ALL && error == 0)// Começa jogo
                    r.StartGame(Player);

                r.UpdatePlayerInfo(Player);
                 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Room::ChangeItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia erro para o cliente não travar a UI
                Player.Send(Handle_PACKET_RESPONSE.pacote04B(Player, 255, 1));
            }
        }

        private int HandleChangeCaddie(Player Player, int caddieId)
        {
            int error = 0;
            CaddieInfoEx targetCaddie = null;

            // 1. Tenta localizar o Caddie se o ID for válido
            if (caddieId > 0)
            {
                targetCaddie = Player.Inventory.FindCaddieById(caddieId);

                // Validação de Existência e Tipo
                if (targetCaddie == null || sIff.Instance.getItemGroupIdentify(targetCaddie._typeid) != IFF_GROUP.CADDIE)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Room::Caddie] ID {caddieId} inválido para UID {Player.UserInfo.UID}. Desequipando.",
                        type_msg.CL_ONLY_CONSOLE));

                    error = (targetCaddie == null) ? 2 : 3;
                    caddieId = 0;
                    targetCaddie = null;
                }
                else
                {
                    // 2. Processa Expiração (UpdateItem) do Caddie ou de suas Parts
                    ProcessCaddieExpiration(Player, targetCaddie, ref caddieId);

                    if (caddieId == 0) targetCaddie = null; // Caso o caddie inteiro tenha expirado
                }
            }

            // 3. Aplicação na Memória (Sincroniza a Sessão)
            Player.Inventory.UserEquippedItem.CaddieEquiped = targetCaddie;
            Player.Inventory.UserEquipment.caddie_id = caddieId;

            // 4. Validação final pela Engine do Player
            if (targetCaddie != null && Player.CheckCaddieEquiped(Player.Inventory.UserEquipment))
            {
                Player.Inventory.UserEquippedItem.CaddieEquiped = null;
                Player.Inventory.UserEquipment.caddie_id = 0;
                caddieId = 0;
            }

            // 5. Persistência Única no DB
            NormalManagerDB.Instance.add(0, new CmdUpdateCaddieEquiped(Player.UserInfo.UID, caddieId));

            return error;
        }

        private int HandleChangeBall(Player Player, uint ballTypeId)
        {
            int error = 0;
            WarehouseItemEx targetBall = null;

            // 1. Tenta equipar a bola solicitada (se não for a padrão ID 0)
            if (ballTypeId > 0)
            {
                targetBall = Player.Inventory.FindWarehouseItemByTypeid(ballTypeId);

                // Validação de integridade
                if (targetBall == null || sIff.Instance.getItemGroupIdentify(targetBall._typeid) != IFF_GROUP.BALL)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Room::Ball] Player[UID={Player.UserInfo.UID}] enviou TypeID {ballTypeId} inválido. Revertendo.",
                        type_msg.CL_ONLY_CONSOLE));

                    error = (targetBall == null) ? 2 : 3;
                    targetBall = null; // Força busca da padrão
                }
            }

            // 2. Fallback: Se a bola for nula ou o ID for 0, busca a padrão (Comet de 1 centavo/iniciante)
            if (targetBall == null)
            {
                targetBall = Player.Inventory.FindWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                // 3. Se o Player não tem nem a padrão (raro, mas acontece em bugs de DB), cria uma
                if (targetBall == null)
                {
                    targetBall = Player.CreateDefaultBall(); // Método auxiliar similar ao da Taqueira
                    error = 0; // Zera erro pois agora ele tem uma bola válida
                }
            }

            // 4. Aplicação na Memória
            if (targetBall != null)
            {
                Player.Inventory.UserEquippedItem.Ball_WI = targetBall;
                Player.Inventory.UserEquipment.ball_typeid = targetBall._typeid;

                // Validação final da engine de jogo
                if (Player.CheckBallEquiped(Player.Inventory.UserEquipment))
                {
                    ballTypeId = Player.Inventory.UserEquipment.ball_typeid;
                }

                // 5. Persistência Única (DB)
                NormalManagerDB.Instance.add(0, new CmdUpdateBallEquiped(Player.UserInfo.UID, ballTypeId));
            }

            return error;
        }

        private int HandleChangeClubSet(Player Player, int clubsetId)
        {
            int error = 0;
            WarehouseItemEx targetClub = null;

            // 1. Tenta localizar a taqueira solicitada
            if (clubsetId > 0)
            {
                targetClub = Player.Inventory.FindWarehouseItemById(clubsetId);

                // Valida se existe, se é ClubSet e se não expirou
                if (targetClub == null ||
                    sIff.Instance.getItemGroupIdentify(targetClub._typeid) != IFF_GROUP.CLUBSET ||
                    !Player.Inventory.IsClubSetValid(clubsetId))
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[Room::ClubSet] ID {clubsetId} inválido ou expirado para UID {Player.UserInfo.UID}. Revertendo para padrão.", type_msg.CL_ONLY_CONSOLE));
                    targetClub = null; // Força busca da padrão
                }
            }

            // 2. Se a taqueira for inválida/nula, busca ou cria a CV1 (Padrão)
            if (targetClub == null)
            {
                targetClub = Player.Inventory.FindWarehouseItemByTypeid(DEFAULT_CLUB_TYPEID);
                if (targetClub == null)
                {
                    targetClub = Player.CreateDefaultClubSet(); // Método auxiliar similar ao da Taqueira
                    error = 0; // Zera erro pois agora ele tem uma bola válida
                }
            }

            // 3. Aplica o Equipamento (Lógica centralizada)
            if (targetClub != null)
            {
                Player.Inventory.EquipClubSetAction(targetClub);

                // 4. Persistência Única
                NormalManagerDB.Instance.add(0,
                    new CmdUpdateClubsetEquiped(Player.UserInfo.UID, targetClub.id));
            }

            return error;
        }

        private int HandleChangeMascot(Player Player, int mascotId)
        {
            MascotInfoEx targetMascot = null;
            int error = 0;

            // 1. Tenta localizar o mascote se o ID for maior que 0
            if (mascotId > 0)
            {
                targetMascot = Player.Inventory.FindMascotById(mascotId);

                // Verifica se o item existe e se é realmente um mascote
                if (targetMascot == null || sIff.Instance.getItemGroupIdentify(targetMascot._typeid) != IFF_GROUP.MASCOT)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Room::Mascot] Player[UID={Player.UserInfo.UID}] enviou ID {mascotId} inválido. Desequipando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    error = (targetMascot == null) ? 2 : 3;
                    mascotId = 0; // Força desequipar
                    targetMascot = null;
                }
                else
                {
                    // 2. Verifica expiração (Tempo do mascote acabou?)
                    var expired = Player.Inventory.FindUpdateItemById(targetMascot.id);
                    if (expired.Count > 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[Room::Mascot] Mascote {mascotId} expirado. Removendo.", type_msg.CL_ONLY_CONSOLE));
                        mascotId = 0;
                        targetMascot = null;
                    }
                }
            }

            // 3. Aplicação na Memória (Sincroniza a Sessão)
            Player.Inventory.UserEquippedItem.MascotEquiped = targetMascot;
            Player.Inventory.UserEquipment.mascot_id = mascotId;

            // 4. Validação Extra (Check final de integridade)
            if (targetMascot != null && !Player.CheckMascotEquiped(Player.Inventory.UserEquipment))
            {
                Player.Inventory.UserEquippedItem.MascotEquiped = null;
                Player.Inventory.UserEquipment.mascot_id = 0;
                mascotId = 0;
            }

            // 5. Persistência Única (DB)
            NormalManagerDB.Instance.add(0, new CmdUpdateMascotEquiped(Player.UserInfo.UID, mascotId));

            return error;
        }

        private int HandleChangeItemSpecial(Player Player, int item_id, int effect_lounge)
        {
            // 1. Defesa Inicial: Sem personagem = Erro Crítico
            if (Player.Inventory.UserEquippedItem.CharacterEquiped == null)
            {
                throw new exception($"[Room::SyncEffect] Player[UID={Player.UserInfo.UID}] sem character equipado.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1000, 0x57007));
            }

            var charInfo = Player.Inventory.UserEquippedItem.CharacterEquiped;
            var effectType = effect_lounge;

            // 2. Busca ou Cria o Estado do Personagem na Lounge (StateCharacterLounge)
            if (!Player.UserInfo.CharacterLoungeStates.TryGetValue(item_id, out var state))
            {
                state = new StateCharacterLounge();
                Player.UserInfo.CharacterLoungeStates.Add(charInfo.id, state);
                _smp.LogManager.Instance.push(new AppMessage($"[Room::SyncEffect] Criado novo StateLounge para Char {charInfo.id}", type_msg.CL_ONLY_CONSOLE));
            }

            // 3. Processamento por Type de efeito
            switch ((LOUNGE_EFFECT_TYPE)effectType)
            {
                case LOUNGE_EFFECT_TYPE.BIG_HEAD: // HERMES (Originalmente o item que aumenta a cabeça)
                    ValidateAndApplyEffect(Player, cadie_cauldron_Hermes_item_typeid, () =>
                    {
                        state.scale_head = (state.scale_head > 1.0f) ? 1.0f : 2.0f;
                    });
                    break;

                case LOUNGE_EFFECT_TYPE.FAST_WALK: // JESTER (Originalmente o item de velocidade)
                    ValidateAndApplyEffect(Player, cadie_cauldron_Jester_item_typeid, () =>
                    {
                        state.walk_speed = (state.walk_speed > 1.0f) ? 1.0f : 2.0f;
                    });
                    break;

                case LOUNGE_EFFECT_TYPE.TWILIGHT: // TWILIGHT (Efeito de Aura/Clima)
                    ValidateAndApplyEffect(Player, cadie_cauldron_Twilight_item_typeid, () =>
                    {
                        // Twilight geralmente é um gatilho de animação, não altera escala/velocidade fixa
                        _smp.LogManager.Instance.push(new AppMessage($"[Room::SyncEffect] Twilight ativado para {Player.UserInfo.UID}", type_msg.CL_ONLY_CONSOLE));
                    });
                    break;

                default:
                    return 1; // Efeito desconhecido
            }

            return 0; // SUCCESS
        }

        private int HandleChangeCharacter(Player Player, int characterId)
        {
            var p = new Packet();
            CharacterInfo targetChar = null;

            // 1. Tenta localizar o personagem solicitado
            if (characterId != 0)
            {
                targetChar = Player.Inventory.FindCharacterById(characterId);
            }

            // 2. Fallback 1: Se não encontrou o ID, tenta pegar o primeiro do mapa (mp_ce)
            if (targetChar == null && Player.Inventory.Characters.Count > 0)
            {
                targetChar = Player.Inventory.Characters.Values.FirstOrDefault();
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Room::Sync] Player[UID={Player.UserInfo.UID}] ID {characterId} inválido. Usando reserva: {targetChar?.id}",
                    type_msg.CL_ONLY_CONSOLE));
            }

            // 3. Fallback 2: Se o Player não tem NENHUM personagem, cria o Nuri (Padrão)
            if (targetChar == null)
            {
                return 1; // Falha crítica ao criar Nuri
            }

            // 4. APLICAÇÃO DA MUDANÇA (Sincronização de Memória)  
            Player.Inventory.SyncCharacter(targetChar.id);
            // 5. PERSISTÊNCIA E BROADCAST
            SyncCharacterToRoom(Player, targetChar.id);

            return 0; // SUCCESS
        }


        #region HELPERS
        private void SyncCharacterToRoom(Player Player, int charId)
        {
            var p = new Packet();
            Room? room = Player.GetRoom();
            // DB Update
            NormalManagerDB.Instance.add(0, new CmdUpdateCharacterEquiped(Player.UserInfo.UID, charId), null, this);

            // Update Room Info
            room?.UpdatePlayerInfo(Player);
            PlayerRoomInfo pri = room?.GetPlayerInfo(Player);

            // Broadcast 0x48 (Update Player)
            if (room?.GetTipo() != RoomTypeFlags.PRACTICE && room?.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
            {
                if (Handle_PACKET_RESPONSE.MakePlayerRoomInfo(p, Player, [pri ?? new PlayerRoomInfo()], 0x103))
                {
                    room?.SendBroadCast(p);
                }
            }

            // Lounge Sync (0x196)
            if (room?.GetTipo() == RoomTypeFlags.LOUNGE)
            {
                if (Player.UserInfo.CharacterLoungeStates.TryGetValue(charId, out StateCharacterLounge characterLounge))
                    room?.SendBroadCast(Handle_PACKET_RESPONSE.pacote196(Player, characterLounge));
            }
        }

        /// <summary>
        /// Gerencia a expiração do Caddie e das Parts (roupas) associadas
        /// </summary>
        private void ProcessCaddieExpiration(Player Player, CaddieInfoEx caddie, ref int caddieId)
        {
            var updates = Player.Inventory.FindUpdateItemById(caddie.id);
            if (!updates.Any() || caddie.rent_flag == 1) return;

            foreach (var el in updates.ToList()) // ToList para evitar erro de modificação de coleção
            {
                if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[Room::Caddie] Caddie {caddieId} expirado.", type_msg.CL_ONLY_CONSOLE));
                    caddieId = 0; // Marca para desequipar
                }
                else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[Room::Caddie] Parts do Caddie {caddieId} expiradas.", type_msg.CL_ONLY_CONSOLE));
                    // Limpa apenas as roupas, mantém o caddie
                    caddie.parts_typeid = 0;
                    caddie.parts_end_date_unix = 0;
                    caddie.end_parts_date = new SystemTime();
                }

                // Remove do mapa de updates (limpeza de memória)
                Player.Inventory.UpdateItems.Remove(el.Key);
            }
        }

        /// <summary>
        /// Método auxiliar para validar se o Player tem a parte necessária equipada no personagem atual
        /// </summary>
        private void ValidateAndApplyEffect(Player Player, uint[] itemPool, Action applyAction)
        {
            var charInfo = Player.Inventory.UserEquippedItem.CharacterEquiped;

            // 1. Segurança: Se o personagem não estiver carregado, não há o que validar
            if (charInfo == null)
                throw new exception("Personagem não carregado na sessão.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1000, 1));

            // Pega o ID base do personagem (Nuri = 0, Hana = 1, etc)
            byte charLowId = (byte)sIff.Instance.getItemCharIdentify(charInfo._typeid);

            // 2. Busca o TypeID do item (Ex: Sapato de Hermes) compatível com o personagem atual
            uint requiredItem = itemPool.FirstOrDefault(typeId => sIff.Instance.getItemCharIdentify(typeId) == charLowId);

            // 3. Validação de existência no Pool
            if (requiredItem == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Effect] Item de efeito não encontrado no IFF para CharacterID {charLowId}.", type_msg.CL_ONLY_CONSOLE));
                throw new exception("Item de efeito não cadastrado para este personagem.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1001, 1));
            }

            // 4. Validação de posse/equipe (Verifica se o Player realmente está vestindo o item)
            if (!charInfo.isPartEquiped(requiredItem))
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Effect] Player UID {Player.UserInfo.UID} tentou usar efeito sem ter o item {requiredItem} equipado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw new exception("Player não está com o item de efeito equipado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1002, 1));
            }

            // 5. Se passou em tudo, executa a ação (ex: aplicar o efeito de fumaça ou velocidade)
            applyAction();
        }
        #endregion
    }
}