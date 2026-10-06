using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.Generic;
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
    public class Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            DolfiniLockerItem[] aTI = null;
            var m_ci = Player.GetChannel();
            try
            { 
                byte count = Packet.ReadByte();
                aTI = new DolfiniLockerItem[count];

                for (int index = 0; index < count; index++)
                    aTI[index] = new DolfiniLockerItem().ToRead(Packet);

                uint char_typeid = 0;
                uint i = 0;

                var r = Player.GetRoom();

                for (i = 0; i < count; ++i)
                {
                    // Verifica se o Player está com ShopRoom aberto e se está vendendo o item no ShopRoom
                    if (r != null && r.CheckPersonalShopItem(Player, aTI[i].item.id))
                    {
                        throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar o item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker, mas o item esta sendo vendido no Personal ShopRoom dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                    }

                    if (sIff.Instance.getItemGroupIdentify(aTI[i].item._typeid) != IFF_GROUP.PART)
                    {
                        throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + "] no Dolfini Locker que nao é um IFF::PART.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 500, 109));
                    }

                    var part = sIff.Instance.findPart(aTI[i].item._typeid);

                    if (part == null)
                    {
                        throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker que nao tem no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 504, 5100405));
                    }

                    if (part.type_item == PART_TYPE.UCC_DRAW_ONLY || part.type_item == PART_TYPE.UCC_COPY_ONLY)
                    {
                        throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar um Self Design Original/Copy item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker, mas nao é permitido", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 505, 5100406));
                    }

                    char_typeid = ((Convert.ToUInt32(sIff.Instance.CHARACTER) << 26) | sIff.Instance.getItemCharIdentify(aTI[i].item._typeid));

                    var character = Player.Inventory.FindCharacterByTypeid(char_typeid);

                    if (character != null)
                    {
                        var part_num = sIff.Instance.getItemCharPartNumber(aTI[i].item._typeid);

                        if (character.parts_id[part_num] == aTI[i].item.id && character.parts_typeid[part_num] == aTI[i].item._typeid)
                        {
                            throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] equipado Part[num=" + (part_num) + "] no Dolfini Locker. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 501, 5100402));
                        }
                    }

                    var it = Player.Inventory.FindWarehouseItemById(aTI[i].item.id);

                    if (it == null)
                    {
                        throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker que ele nao tem. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 502, 5100403));
                    }

                    CmdAddDolfiniLockerItem cmd_adli = new CmdAddDolfiniLockerItem(Player.UserInfo.UID, aTI[i]);

                    NormalManagerDB.Instance.add(0, cmd_adli, null, null);

                    if (cmd_adli.getException().getCodeError() != 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] " + cmd_adli.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (i < (count - 1u))
                        {
                            aTI[i] = aTI[i + 1];
                        }

                        i--;
                        count--;

                        continue;
                    }

                    aTI[i] = cmd_adli.getInfo();

                    if (aTI[i].index == -1)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] nao conseguiu add o item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker no DB", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (i < (count - 1u))
                        {
                            aTI[i] = aTI[i + 1];
                        }

                        i--;
                        count--;

                        continue;
                    }

                    Player.Inventory.WarehouseItems.Remove(it.id);

                    Player.Inventory.DolfineLocker.v_item.Add(aTI[i]);

                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Sucess] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] Adicionou Item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (count == 0)
                {
                    throw new exception("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][Error] nenhum item passou nas verificacoes, Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 503, 5100404));
                }

                p.init_plain(0x139);
                p.WriteUInt16(0);
                Player.Send(p);

                p.init_plain(0xEC);
                p.WriteUInt32(count);
                p.WriteByte(1); // Add Item no Dolfini Locker
                p.WriteUInt64(0); // Pang add para o Player
                p.WriteUInt32(0);

                for (i = 0; i < count; ++i)
                {
                    p.WriteBytes(aTI[i].item.ToArray());
                }
                Player.Send(p);

                for (i = 0; i < count; ++i)
                {
                    p.init_plain(0x16E);
                    p.WriteUInt32(0); // opt[Error Code]
                    p.WriteUInt64(0);
                    p.WriteBytes(aTI[i].item.ToArray());
                    Player.Send(p);
                }

                if (aTI != null)
                {
                    aTI = null;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ADD_DOLFINI_LOCKER_ITEM][ErrorSystem] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16E);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100400);
                Player.Send(p);

                if (aTI != null)
                {
                    aTI = null;
                }
            }
        }
    }
}