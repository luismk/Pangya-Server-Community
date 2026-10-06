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
using PangyaAPI.IFF.Models;
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
    public class Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new();

            try
            {
                if (Player.UserInfo.BlockFlag.Flag.LegacyTikiShop)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] esta bloqueado no Legacy Tiki Shop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4000, 1));
                }

                Func<IFFTikiShopData, (uint, uint)> getNumberItensPerTikiShopPts = (_tiki) =>
                {
                    uint itemCount = (_tiki.Tiki_Qnt_Pts == 0u) ? 1u : _tiki.Tiki_Qnt_Pts;
                    uint tikiPoints = (_tiki.Tiki_Pts == 0u) ? 1u : _tiki.Tiki_Pts;

                    return (itemCount, tikiPoints);
                };

                uint tiki_pts = 0;
                string s_item = "";

                stLegacyTikiShopExchangeItem tsei = new stLegacyTikiShopExchangeItem();
                List<stItem> v_item = new List<stItem>();

                AchievementSystem sys_achieve = new();

                uint count = Packet.ReadByte();

                var r = Player.GetRoom();

                for (var i = 0; i < count; ++i)
                {
                    tsei = new stLegacyTikiShopExchangeItem().ToRead(Packet);

                    var @base = sIff.Instance.findCommomItem(tsei._typeid);

                    if (@base == null)
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0x5200902));
                    }

                    if (!@base.tiki.IsActived())
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao é valido para ser trocado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 904, 0x5200905));
                    }

                    var dados_tiki = getNumberItensPerTikiShopPts(@base.tiki);

                    var _item = ItemManager.exchangeTikiShop(Player, tsei._typeid, tsei.id, (uint)(dados_tiki.Item1 * tsei.qntd));

                    if (_item == null || _item.Count == 0)
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas nao conseguiu inicializar o item. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 900, 0x52000901));
                    }

                    if (r != null && r.CheckPersonalShopItem(Player, tsei.id))
                    {
                        throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas o item esta sendo vendido no Personal ShopRoom dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                    }

                    tiki_pts += (uint)(dados_tiki.Item2 * tsei.qntd);
                    v_item.AddRange(_item);
                }

                if (tiki_pts == 0u)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item(ns)(" + s_item + "), mas ocorreu um erro na inicializacao do Tiki Points from IFF_STRUCT is invalid(" + (tiki_pts) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 905, 0x5200905));
                }

                if (ItemManager.removeItem(v_item, Player) <= 0)
                {
                    throw new exception("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] tentou trocar item(ns)(" + s_item + "), mas nao conseguiu deletar ele(s).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 902, 0x5200903));
                }

                Player.UserInfo.PointShopLegacy += tiki_pts;

                NormalManagerDB.Instance.add(28, new CmdUpdateLegacyTikiShopPoint(Player.UserInfo.UID, Player.UserInfo.PointShopLegacy));

                sys_achieve.incrementCounter(0x6C400086u, 1);

                p.init_plain(0x216);
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
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

                p.init_plain(0x1E9);
                p.WriteUInt32(0u);
                p.WriteUInt32((uint)Player.UserInfo.PointShopLegacy);
                Player.Send(p);

                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_EXCHANGE_TP_BY_ITEM_LEGACY_TIKI_SHOP][ErrorSystem] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E9);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1u);
                Player.Send(p);
            }
        }
    }
}