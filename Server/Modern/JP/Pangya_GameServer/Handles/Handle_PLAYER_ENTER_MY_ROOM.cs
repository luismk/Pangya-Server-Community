using Pangya_GameServer.Models;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ENTER_MY_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            { 
                var pri = BuildPlayerRoomInfo(Player);

                // Envio do Pacote 0x168 (Dados do Personagem/Estado)
                var p168 = new Packet(0x168);
                p168.WriteBytes(pri.ToArray(WithCharacter: true));
                Player.Send(p168);

                // Envio do Pacote 0x12D (Itens do MyRoom - Posters/Móveis)
                var p12D = new Packet(0x12D);
                p12D.WriteUInt32(1); // Option: Load Items

                var items = Player.Inventory.MyRoomItems;
                p12D.WriteUInt16((ushort)items.Count);

                foreach (var item in items)
                {
                    p12D.WriteBytes(item.ToArray());
                }

                Player.Send(p12D);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[MyRoom::Enter] Player[UID: {Player.UserInfo.UID}] Error: {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }

        private PlayerRoomInfo BuildPlayerRoomInfo(Player s)
        {
            var ui = s.UserInfo;
            var inv = s.Inventory;

            var pri = new PlayerRoomInfo
            {
                OID = s.ConnectionID,
                UID = ui.UID,
                NickName = ui.NickName,
                GuildName = ui.Guild.name,
                GuildIndex = ui.Guild.uid,
                GuildMark = ui.Guild.mark_emblem,
                GuildMarkIndex = ui.Guild.index_mark_emblem,
                GameLevel = ui.Member.GameLevel,
                RankPosition = 0,
                Capability = ui.UserCapabilities,
                TitleSkin = inv.UserEquipment.m_title,
                AvengeScore = ui.Statistics.getMediaScore(),
                LadderGrade = 0, // My Room State
                Action = { Posture = ui.PostureRoom, Animation = ui.LoungeState },
                LocationInfo = new PlayerRoomLocationInfo { X = ui.CurrentLocation.x, Z = ui.CurrentLocation.z, Y = ui.CurrentLocation.r },
                ShopRoom = new PlayerRoomInfoShop(),
                ItemSpecial = inv.CheckHaveItemBoost(),
                Invite = 0
            };

            // Setup de Skin e Personagem
            if (inv.UserEquippedItem.CharacterEquiped != null)
            {
                pri.CharacterInfo = inv.UserEquippedItem.CharacterEquiped;
                pri.CharacterID = inv.UserEquippedItem.CharacterEquiped._typeid;
                pri.ItemSkin = (uint[])inv.UserEquipment.skin_typeid.Clone();
                pri.ItemSkin[4] = 0; // Cut-in fix para exibição correta
            }

            // Mascot
            if (inv.UserEquippedItem.MascotEquiped != null)
                pri.MascotID = inv.UserEquippedItem.MascotEquiped._typeid;

            // Flags de Estado
            pri.State.Master = 1;
            pri.State.Ready = 1;
            pri.State.Gender = ui.Member.Gender;

            // Lógica de Ícones (Quit Rate / Angel)
            UpdatePlayerIcons(s, pri);

            return pri;
        }

        private void UpdatePlayerIcons(Player s, PlayerRoomInfo pri)
        {
            float quitRate = s.UserInfo.Statistics.getQuitRate();
            bool isBeginnerPlus = s.UserInfo.Member.GameLevel >= 6 && s.UserInfo.Statistics.jogado >= 50;

            if (isBeginnerPlus)
            {
                if (quitRate < GOOD_PLAYER_ICON) pri.State.Wings = 1;
                else if (quitRate < QUITER_ICON_2) pri.State.Quit10Porcent = 1;
                else pri.State.Quit20Porcent = 1;
            }

            // Angel Icon
            if (s.Inventory.UserEquippedItem.CharacterEquiped != null && quitRate < GOOD_PLAYER_ICON)
            {
                pri.StateAngel.Value = s.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped();
            }
        }
    }
}