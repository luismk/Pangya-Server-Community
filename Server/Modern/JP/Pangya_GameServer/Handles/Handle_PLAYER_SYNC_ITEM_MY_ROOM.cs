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
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;


namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SYNC_ITEM_MY_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        /// <summary>
        /// Flags de atualização do My Room (Quarto). 
        /// Define qual categoria de equipamento o servidor deve processar e sincronizar.
        /// </summary>
        private enum SYNC_ITEM_FLAGS : byte
        {
            /// <summary>
            /// Atualiza todas as roupas e acessórios (anéis, luvas, camisas, etc.) do personagem
            /// </summary> 
            SYNC_CHAR_ALL_PARTS = 0,

            // Atualiza o Caddie equipado na sessão do jogador
            SYNC_CADDIE = 1,

            // Atualiza os itens de uso rápido (Potions, Phoenix, etc.) nos slots de jogo
            SYNC_USE_ITEMS = 2,

            // Sincroniza a Taqueira (ClubSet) e a Bola (Comet/Ball) escolhidas
            SYNC_CLUB_AND_BALL = 3,

            // Atualiza as Skins de interface ou efeitos aplicados
            SYNC_SKINS = 4,

            // Altera o Personagem Principal (Character) ativo na conta
            SYNC_CHAR_MAIN = 5,

            // Atualiza o Mascote equipado (se houver)
            SYNC_MASCOT = 8,

            // Altera a imagem de Cut-In (animação que aparece no Power Shot)
            SYNC_CHAR_CUTIN = 9,

            // Atualiza o Poster/Fundo de tela do perfil do jogador
            SYNC_POSTER = 10
        }

        public override async Task Handle()
        {
            SYNC_ITEM_FLAGS type = SYNC_ITEM_FLAGS.SYNC_CHAR_ALL_PARTS;
            int error = 4;

            try
            {
                type = (SYNC_ITEM_FLAGS)Packet.ReadByte(); 

                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_SYNC_ITEM_MY_ROOM][Warning] Normal[UID: {Player.UserInfo.UID}, REQ: {type}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                switch (type)
                {
                    case SYNC_ITEM_FLAGS.SYNC_CHAR_ALL_PARTS:
                        // AQUI: Onde o anel e as roupas são processados
                        error = HandleUpdateCharacterParts(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_CADDIE:
                        error = HandleUpdateCaddie(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_USE_ITEMS:
                        error = HandleUpdateUseItems(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_CLUB_AND_BALL:
                        // AQUI: Onde o ClubSet e a Comet são sincronizados
                        error = HandleUpdateClubAndBall(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_SKINS:
                        error = HandleUpdateSkins(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_CHAR_MAIN:
                        // Troca o personagem principal da conta
                        error = HandleUpdateCharacter(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_MASCOT:
                        error = HandleUpdateMascot(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_CHAR_CUTIN:
                        error = HandleUpdateCutin(Player, Packet);
                        break;

                    case SYNC_ITEM_FLAGS.SYNC_POSTER:
                        error = HandleUpdatePoster(Player, Packet);
                        break;

                    default:
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[MyRoom] Tipo de update não implementado: {type} ({type})",
                            type_msg.CL_ONLY_CONSOLE));
                        error = 1;
                        break;
                }
                Player.Send(Handle_PACKET_RESPONSE.pacote06B(Player.Inventory, (byte)type, error));

                Player.GetChannel()?.UpdatePlayerInfo(Player);
            }
            catch (exception e)
            {
                Player.Send(Handle_PACKET_RESPONSE.pacote06B(Player.Inventory, (byte)type, 1));

                _smp.LogManager.Instance.push(
                    new AppMessage(
                        "[Handle_PLAYER_CHANGE_PLAYER_ITEM_MY_ROOM][ErrorSystem] " +
                        e.getFullMessageError(),
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            await Task.CompletedTask;
        }


        private int HandleUpdateCharacterParts(Player session, Packet packet)
        {
            int error = 4;

            CharacterInfo ci = new CharacterInfo().ToRead(packet);
            var pCe = Player.Inventory.FindCharacterById(ci.id);

            if (ci.id == 0 || pCe == null)
                return (ci.id == 0) ? 1 : 2;

            Player.Inventory.SyncCharacter(ci.id, ci); //sempre antes

            Player.CheckCharacterEquipedPart(ci);
            Player.CheckCharacterEquipedAuxPart(ci);

            NormalManagerDB.Instance.add(0, new CmdUpdateCharacterAllPartEquiped(Player.Inventory.uid, ci));

            return error;
        }

        private int HandleUpdateCharacter(Player session, Packet packet)
        {
            int error = 4;
            int charId = packet.ReadInt32();

            var pCe = Player.Inventory.FindCharacterById(charId);

            if (charId == 0 || pCe == null)
                return (charId == 0) ? 1 : 2;
            //é diferente do outro
            Player.Inventory.SyncCharacter(charId);

            NormalManagerDB.Instance.add(0, new CmdUpdateCharacterEquiped(Player.Inventory.uid, charId));

            return error;
        }

        private int HandleUpdateCaddie(Player session, Packet packet)
        {
            int error = 4;
            int itemId = packet.ReadInt32();

            if (itemId != 0)
            {
                var caddie = Player.Inventory.FindCaddieById(itemId);

                if (caddie == null)
                    return 2;

                Player.Inventory.UserEquippedItem.CaddieEquiped = caddie;
                Player.Inventory.UserEquipment.caddie_id = itemId;

                if (Player.CheckCaddieEquiped(Player.Inventory.UserEquipment))
                    itemId = Player.Inventory.UserEquipment.caddie_id;
            }
            else
            {
                Player.Inventory.UserEquipment.caddie_id = 0;
            }

            NormalManagerDB.Instance.add(0, new CmdUpdateCaddieEquiped(Player.Inventory.uid, itemId));

            return error;
        }

        private int HandleUpdateClubAndBall(Player session, Packet packet)
        {
            int error = 4;

            // BALL
            int ballTypeId = packet.ReadInt32();
            var ball = Player.Inventory.FindWarehouseItemByTypeid((uint)ballTypeId);

            if (ball != null)
            {
                Player.Inventory.UserEquippedItem.Ball_WI = ball;
                Player.Inventory.UserEquipment.ball_typeid = (uint)ballTypeId;

                if (Player.CheckBallEquiped(Player.Inventory.UserEquipment))
                    ballTypeId = (int)Player.Inventory.UserEquipment.ball_typeid;
            }
            else
            {
                Player.Inventory.UserEquipment.ball_typeid = 0;
            }

            NormalManagerDB.Instance.add(0, new CmdUpdateBallEquiped(Player.Inventory.uid, (uint)ballTypeId));

            // CLUBSET
            int clubId = packet.ReadInt32();
            var club = Player.Inventory.FindWarehouseItemById(clubId);

            if (club == null)
                return 2;

            Player.Inventory.UserEquippedItem.Club_WI = club;
            Player.Inventory.UserEquipment.clubset_id = clubId;

            if (Player.CheckClubSetEquiped(Player.Inventory.UserEquipment))
                clubId = Player.Inventory.UserEquipment.clubset_id;

            NormalManagerDB.Instance.add(
                 0,
                 new CmdUpdateClubsetEquiped(Player.Inventory.uid, clubId));

            return error;
        }

        private int HandleUpdateUseItems(Player session, Packet packet)
        {
            int error = 4;

            UserEquip ue = new()
            {
                item_slot = packet.ReadUInt32(Player.Inventory.UserEquipment.item_slot.Length)
            };

            if (Player.Inventory.CheckItemEquiped(ue.item_slot)) //verificacao....
                Player.Inventory.UserEquipment.item_slot = ue.item_slot;

            NormalManagerDB.Instance.add(25, new CmdUpdateItemSlot(Player.Inventory.uid, ue.item_slot));

            return error;
        }

        private int HandleUpdateSkins(Player session, Packet packet)
        {
            int error = 4;

            for (int i = 0; i < Player.Inventory.UserEquipment.skin_typeid.Length; i++)
            {
                int id = packet.ReadInt32();

                if (id == 0)
                {
                    Player.Inventory.UserEquipment.skin_id[i] = 0;
                    Player.Inventory.UserEquipment.skin_typeid[i] = 0;
                    continue;
                }

                var skin = Player.Inventory.FindWarehouseItemByTypeid((uint)id);

                if (skin == null)
                    return 2;

                Player.Inventory.UserEquipment.skin_id[i] = (uint)skin.id;
                Player.Inventory.UserEquipment.skin_typeid[i] = skin._typeid;
            }

            NormalManagerDB.Instance.add(0, new CmdUpdateSkinEquiped(Player.Inventory.uid, Player.Inventory.UserEquipment));

            return error;
        }

        private int HandleUpdateMascot(Player session, Packet packet)
        {
            int error = 4;
            int id = packet.ReadInt32();

            if (id != 0)
            {
                var mascot = Player.Inventory.FindMascotById(id);
                if (mascot == null)
                    return 2;

                Player.Inventory.UserEquippedItem.MascotEquiped = mascot;
                Player.Inventory.UserEquipment.mascot_id = id;
            }
            else
            {
                Player.Inventory.UserEquipment.mascot_id = 0;
            }

            NormalManagerDB.Instance.add(0, new CmdUpdateMascotEquiped(Player.Inventory.uid, id));

            return error;
        }

        private int HandleUpdateCutin(Player session, Packet packet)
        {
            int error = 4;

            int charId = packet.ReadInt32();
            var ci = Player.Inventory.FindCharacterById(charId);

            if (charId == 0)
                return 1; // Invalid Item Id

            if (ci == null)
                return 2; // Not Found

            if (Player.Inventory.UserEquippedItem.CharacterEquiped == null)
                return 4; // No character equipped

            if (Player.Inventory.UserEquippedItem.CharacterEquiped.id != ci.id)
                return 5; // Not the equipped character

            int[] cutins = packet.ReadInt32(Player.Inventory.UserEquippedItem.CharacterEquiped.cut_in.Length);

            for (int i = 0; i < cutins.Length; i++)
            {
                int cutinId = cutins[i];

                if (cutinId == 0)
                {
                    ci.cut_in[i] = 0;
                    continue;
                }

                var pWi = Player.Inventory.FindWarehouseItemById(cutinId);

                if (pWi == null ||
                    sIff.Instance.getItemGroupIdentify(pWi._typeid) != IFF_GROUP.SKIN)
                    return 3; // Item Type Wrong

                ci.cut_in[i] = (uint)cutinId;
            }

            Player.Inventory.SyncCharacter(ci.id, ci); //sempre antes

            // Validação final
            Player.CheckCharacterEquipedCutin(ci);

            // Update DB
            NormalManagerDB.Instance.add(0, new CmdUpdateCharacterCutinEquiped(Player.Inventory.uid, ci));

            return error;
        }

        private int HandleUpdatePoster(Player session, Packet packet)
        {
            int error = 4;

            for (int i = 0; i < Player.Inventory.UserEquipment.poster.Length; i++)
            {
                int posterTypeId = packet.ReadInt32();

                if (posterTypeId == 0)
                {
                    Player.Inventory.UserEquipment.poster[i] = 0;
                    continue;
                }

                var pMri = Player.Inventory.FindMyRoomItemByTypeid((uint)posterTypeId);

                if (pMri == null ||
                    sIff.Instance.getItemGroupIdentify(pMri._typeid) != IFF_GROUP.FURNITURE)
                    return 2;

                Player.Inventory.UserEquipment.poster[i] = (uint)posterTypeId;
            }

            if (Player.CheckPosterEquiped(Player.Inventory.UserEquipment) || error == 4)
            {
                NormalManagerDB.Instance.add(0, new CmdUpdatePosterEquiped(Player.Inventory.uid, Player.Inventory.UserEquipment));
            }

            return error;
        }
    }
}
