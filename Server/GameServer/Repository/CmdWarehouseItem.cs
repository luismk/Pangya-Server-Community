
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Pangya_GameServer.Repository
{
    public class CmdWarehouseItem : Pangya_DB
    {
        uint m_uid = uint.MaxValue;
        public enum TYPE : int
        {
            ALL,
            ONE,
        }
        TYPE m_type;
        uint m_item_id;
        WarehouseManager v_wi;
        private int m_ToTalPartsCNT;
        private int m_ToTalClubsetCNT;

        public CmdWarehouseItem(uint _uid, TYPE _type, uint _item_id = 0)
        {
            m_uid = _uid;
            m_type = _type;
            m_item_id = _item_id;
            v_wi = new WarehouseManager();

        }

        public CmdWarehouseItem(uint _uid, int _type, uint _item_id)
        {
            m_uid = _uid;
            m_type = (TYPE)_type;
            m_item_id = _item_id;
            v_wi = new WarehouseManager();

        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(45);
            try
            {

                WarehouseItemEx wi = new WarehouseItemEx();
                var i = 0;

                wi.id = _result.GetInt32(0);
                wi._typeid = _result.GetUInt32(2);

                wi.ano = _result.GetInt32(3);
                for (i = 0; i < 5; i++)
                    wi.c[i] = _result.GetInt16(4 + i);            // 4 + 5
                wi.purchase = _result.GetByte(9);
                wi.flag = _result.GetSByte(11);

                // Salve local unix date on WarehouseItemEx and System Unix Date on apply_date to send to client
                wi.apply_date_unix_local = _result.GetUInt32(12);
                wi.end_date_unix_local = _result.GetUInt32(13);

                // Date 
                if (wi.apply_date_unix_local > 0)
                {
                    wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(wi.apply_date_unix_local);
                }

                if (wi.end_date_unix_local > 0)
                {
                    wi.end_date = UtilTime.TzLocalUnixToUnixUTC(wi.end_date_unix_local);
                }
                wi.type = _result.GetSByte(14);
                for (i = 0; i < 4; i++)
                    wi.card.character[i] = _result.GetUInt32(15 + i); // 15 + 4
                for (i = 0; i < 4; i++)
                    wi.card.caddie[i] = _result.GetUInt32(19 + i);        // 19 + 4
                for (i = 0; i < 4; i++)
                    wi.card.NPC[i] = _result.GetUInt32(23 + i);           // 23 + 4
                wi.clubset_workshop.flag = _result.GetInt16(27);
                for (i = 0; i < 5; i++)
                    wi.clubset_workshop.c[i] = _result.GetInt16(28 + i);  // 28 + 5
                wi.clubset_workshop.mastery = _result.GetUInt32(33);
                wi.clubset_workshop.recovery_pts = _result.GetUInt32(34);
                wi.clubset_workshop.level = _result.GetInt32(35);
                wi.clubset_workshop.rank = _result.GetInt32(36);
                if (is_valid_c_string(_result.data[37]))
                {
                    wi.ucc.name = IFNULL<string>(_result.data[37]);

                }
                if (is_valid_c_string(_result.data[38]))
                {
                    wi.ucc.idx = IFNULL<string>(_result.data[38]);
                }
                wi.ucc.seq = IFNULL<short>(_result.data[39]);
                if (is_valid_c_string(_result.data[40]))
                {
                    wi.ucc.copier_nick = IFNULL<string>(_result.data[40]);
                }
                wi.ucc.copier = IFNULL(_result.data[41]);
                wi.ucc.trade = (sbyte)IFNULL(_result.data[42]);
                wi.ucc.status = (byte)IFNULL(_result.data[44]);

                if (sIff.getInstance().getItemGroupIdentify(wi._typeid) == IFF_GROUP.PART)
                {
                    SetPartsItemCount();
                }
                else if (sIff.getInstance().getItemGroupIdentify(wi._typeid) == IFF_GROUP.CLUBSET)
                {
                    SetClubsetItemCount();
                }

                if (!v_wi.ContainsKey(wi.id))
                {
                    v_wi[wi.id] = wi;
                }
                else
                {
                    var list = v_wi[wi.id];
                    var found = (list._typeid == wi._typeid);
                    if (!found)
                    {
                        v_wi[wi.id] = wi;
                        _smp.message_pool.getInstance().push(new message(
                            $"[CmdWarehouseItemInfo::lineResult][Warning] PLAYER[UID={m_uid}] adicionou WarehouseItem[TYPEID={wi._typeid}, ID={wi.id}], com mesmo id e typeid diferente de outro WarehouseItemEx que tem no multimap",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message(
                            $"[CmdWarehouseItemInfo::lineResult][Warning] PLAYER[UID={m_uid}] tentou adicionar no multimap um WarehouseItem[TYPEID={wi._typeid}, ID={wi.id}] com o mesmo ID e TYPEID, DUPLICATA",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception ex)
            {
                MethodBase methodBase = ex.TargetSite;
                string msg = ex.Message;
                Console.WriteLine("Error : " + msg + ", Nmb: " + new StackTrace(ex).GetFrame(0).GetFileLineNumber());
            }
        }

        private void SetClubsetItemCount()
        {
            m_ToTalClubsetCNT = m_ToTalClubsetCNT + 1;
        }

        private void SetPartsItemCount()
        {
            m_ToTalPartsCNT = m_ToTalPartsCNT + 1;
        }

        public int getClubsetItemCount()
        {
            return m_ToTalClubsetCNT;
        }

        public int getPartsItemCount()
        {
            return m_ToTalPartsCNT;
        }

        protected override Response prepareConsulta()
        {
            // 1. Define the procedures
            string procName = (m_type == TYPE.ALL)
                ? "pangya.ProcGetWarehouseItem"
                : "pangya.ProcGetWarehouseItem_One";

            // 2. Define the parameters
            string parameters = (m_type == TYPE.ALL)
                ? m_uid.ToString()
                : $"{m_uid}, {m_item_id}";

            // 3. Execute and Validate
            var r = procedure(procName, parameters);

            checkResponse(r, $"Não foi possível carregar o Warehouse do player: {m_uid}");

            return r;
        }

        public WarehouseManager getInfo()
        {
            return v_wi;
        }



        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public uint getItemID()
        {
            return m_item_id;
        }

        public void setItemID(uint _item_id)
        {
            m_item_id = _item_id;
        }
    }
}
