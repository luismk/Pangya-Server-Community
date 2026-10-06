using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
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
    public class Handle_PLAYER_OPEN_BOX_MAIL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                if (!sBoxSystem.Instance.isLoad())
                {
                    sBoxSystem.Instance.load();
                }

                uint box_typeid = Packet.ReadUInt32();

                if (box_typeid == 0)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas o typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6300101));
                }

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(box_typeid);

                if (pWi == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas ele nao tem essa Box. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6300102));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiente da Box[value=" + (pWi.STDA_C_ITEM_QNTD) + ", Request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6300103));
                }

                if (sIff.Instance.getItemGroupIdentify(pWi._typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao é uma Box valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6300104));
                }

                var item_iff = sIff.Instance.findItem(pWi._typeid);

                if (item_iff == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6300105));
                }

                var box = sBoxSystem.Instance.findBox(pWi._typeid);

                if (box == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no Box System do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x6300106));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                ctx_box_item ctx_bi = null;
                Mascot mascot = null;

                string msg = box.msg;

                switch (pWi._typeid)
                {
                    case SPINNING_CUBE_TYPEID:
                        {
                            var key = Player.Inventory.FindWarehouseItemByTypeid(KEY_OF_SPINNING_CUBE_TYPEID);

                            if (key == null)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas o ele nao tem a chave para abrir o spinning cube. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    7, 0x6300107));
                            }

                            if (key.STDA_C_ITEM_QNTD < 1)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiante[value=" + (key.STDA_C_ITEM_QNTD) + ", Request=1] de chave para abrir Spinning Cube. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    8, 0x6300108));
                            }

                            ctx_bi = sBoxSystem.Instance.drawBox(Player, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Spinning Cube Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar o Spinning Cube. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item));

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)key.id;
                            item._typeid = KEY_OF_SPINNING_CUBE_TYPEID;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar a Key[TYPEID=" + (KEY_OF_SPINNING_CUBE_TYPEID) + ", DESC=Spinning Cube]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    11, 0x6300111));
                            }

                            v_item.Add(new stItem(item));

                            if (box.opened_typeid > 0)
                            {
                                item = new stItem();
                                item.type = 2;
                                item._typeid = box.opened_typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                var rt = RetAddItem.INIT_VALUE;

                                if ((rt = ItemManager.addItem(item, Player, 0, 0)) < 0)
                                {
                                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Spinning Cube. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {
                                    p.init_plain(0x216);
                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1);
                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZero(25);

                                    Player.Send(p);
                                }
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            if (sIff.Instance.getItemGroupIdentify(ctx_bi._typeid) == IFF_GROUP.MASCOT
                                && (mascot = sIff.Instance.findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4;
                                item.STDA_C_ITEM_QNTD = 1;
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            if (MailManager.SendMessageWithItem(0, Player.UserInfo.UID, msg, item) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            if (ctx_bi.raridade == BOX_TYPE_RARETY.R_SUPER_RARE)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[BoxSystem::SpinningCube][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Spinning Cube[TYPEID=" + (pWi._typeid) + "] ganhou super raro[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + "] no spinning cube.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                msg = "<PARAMS><BOX_TYPEID>" + (box._typeid) + "</BOX_TYPEID><NICKNAME>" + (Player.UserInfo.NickName) + "</NICKNAME><TYPEID>" + (ctx_bi._typeid) + "</TYPEID><QTY>" + (ctx_bi.qntd) + "</QTY></PARAMS>";
                                byte opt = (byte)((ctx_bi._typeid == PANG_POUCH_TYPEID) ? 2 : 1);

                                NormalManagerDB.Instance.add(23,
                                     new CmdInsertSpinningCubeSuperRareWinBroadcast(msg, opt),
                                    null, null);
                            }

                            AchievementSystem sys_achieve = new AchievementSystem();
                            sys_achieve.incrementCounter(0x6C400054u);

                            _smp.LogManager.Instance.push(new AppMessage("[BoxSystem::SpinningCube][Sucess] Normal [UID=" + Player.UserInfo.UID + "] abriu Spinning Cube[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0xA7);
                            p.WriteByte((byte)v_item.Count);
                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }
                            Player.Send(p);

                            p.init_plain(0xAA);
                            p.WriteUInt16(0);
                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                            p.WriteUInt64(Player.UserInfo.Cookie);
                            Player.Send(p);

                            p.init_plain(0x19D);
                            p.WriteUInt32(0);
                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);
                            Player.Send(p);

                            sys_achieve.finish_and_update(Player);
                            break;
                        }
                    case PAPEL_BOX_TYPEID:
                        {
                            ctx_bi = sBoxSystem.Instance.drawBox(Player, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Papel Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Papel Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item));

                            stItem key = new stItem();
                            key.clear();
                            key.type = 2;
                            key.id = -1;
                            key._typeid = KEY_OF_SPINNING_CUBE_TYPEID;
                            key.qntd = 30;
                            key.STDA_C_ITEM_QNTD = (short)key.qntd;

                            var rt = RetAddItem.INIT_VALUE;
                            if ((rt = ItemManager.addItem(key, Player, 0, 0)) < 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], nao conseguiu adicionar Key[TYPEID=" + (KEY_OF_SPINNING_CUBE_TYPEID) + ", DESC=Spinning Cube]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    14, 0x6300114));
                            }

                            if (box.opened_typeid > 0)
                            {
                                item = new stItem();
                                item.type = 2;
                                item._typeid = box.opened_typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                rt = RetAddItem.INIT_VALUE;
                                if ((rt = ItemManager.addItem(item, Player, 0, 0)) < 0)
                                {
                                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Papel Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {
                                    p.init_plain(0x216);
                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1);
                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZero(25);
                                    Player.Send(p);
                                }
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            if (sIff.Instance.getItemGroupIdentify(ctx_bi._typeid) == IFF_GROUP.MASCOT
                                && (mascot = sIff.Instance.findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4;
                                item.STDA_C_ITEM_QNTD = 1;
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            if (MailManager.SendMessageWithItem(0, Player.UserInfo.UID, msg, item) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            _smp.LogManager.Instance.push(new AppMessage("[BoxSystem::PapelBox][Sucess] Normal [UID=" + Player.UserInfo.UID + "] abriu Papel Box[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0xA7);
                            p.WriteByte((byte)v_item.Count);
                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }
                            Player.Send(p);

                            p.init_plain(0xAA);
                            p.WriteUInt16(1);
                            p.WriteUInt32(key._typeid);
                            p.WriteInt32(key.id);
                            p.WriteInt16(key.c[3]);
                            p.WriteByte(key.flag_time);
                            p.WriteUInt16((ushort)key.stat.qntd_dep);
                            if (key.date != null && key.date.active.IsTrue())
                                p.WriteTime(key.date.date.sysDate[1].ConvertTime());
                            else
                                p.WriteZero(16);
                            p.WriteString(key.ucc.IDX, 9);
                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                            p.WriteUInt64(Player.UserInfo.Cookie);
                            Player.Send(p);

                            p.init_plain(0x19D);
                            p.WriteUInt32(0);
                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);
                            Player.Send(p);
                            break;
                        }
                    default:
                        {
                            ctx_bi = sBoxSystem.Instance.drawBox(Player, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item));

                            if (box.opened_typeid > 0)
                            {
                                item = new stItem();
                                item.type = 2;
                                item._typeid = box.opened_typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                var rt = RetAddItem.INIT_VALUE;
                                if ((rt = ItemManager.addItem(item, Player, 0, 0)) < 0)
                                {
                                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {
                                    p.init_plain(0x216);
                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1);
                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZero(25);
                                    Player.Send(p);
                                }
                            }

                            item = new stItem();
                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            if (sIff.Instance.getItemGroupIdentify(ctx_bi._typeid) == IFF_GROUP.MASCOT
                                && (mascot = sIff.Instance.findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4;
                                item.STDA_C_ITEM_QNTD = 1;
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            if (MailManager.SendMessageWithItem(0, Player.UserInfo.UID, msg, item) <= 0)
                            {
                                throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            _smp.LogManager.Instance.push(new AppMessage("[BoxSystem::BoxMail][Sucess] Normal [UID=" + Player.UserInfo.UID + "] abriu Box[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0xA7);
                            p.WriteByte((byte)v_item.Count);
                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }
                            Player.Send(p);

                            p.init_plain(0xAA);
                            p.WriteUInt16(0);
                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                            p.WriteUInt64(Player.UserInfo.Cookie);
                            Player.Send(p);

                            p.init_plain(0x19D);
                            p.WriteUInt32(0);
                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);
                            Player.Send(p);
                            break;
                        }
                }

                if (ctx_bi != null && ctx_bi.raridade > 0)
                {
                    NormalManagerDB.Instance.add(22,
                         new CmdInsertBoxRareWinLog(Player.UserInfo.UID, box._typeid, ctx_bi),
                        null, null);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestOpenBoxMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                p.init_plain(0x19D);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x6300100);
                Player.Send(p);
            }
        } 
    }
}