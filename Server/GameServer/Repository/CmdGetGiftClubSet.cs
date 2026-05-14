using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGetGiftClubSet : Pangya_DB
    {
        public CmdGetGiftClubSet()
        {
            this.m_uid = 0;
            this.m_wi = new WarehouseItemEx();
        }

        public CmdGetGiftClubSet(uint _uid,
            WarehouseItemEx _wi)
        {
            this.m_uid = _uid;
            this.m_wi = (_wi);
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {
            m_wi = _wi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(45, (uint)_result.cols);

            m_wi.id = (int)IFNULL<int>(_result.data[0]);

            if (m_wi.id > 0)
            { // Found

                var i = 0;

                m_wi.id = IFNULL<int>(_result.data[0]);
                m_wi._typeid = IFNULL(_result.data[2]);
                m_wi.ano = IFNULL<int>(_result.data[3]);
                for (i = 0; i < 5; i++)
                {
                    m_wi.c[i] = (short)IFNULL(_result.data[4 + i]); // 4 + 5
                }
                m_wi.purchase = (byte)IFNULL(_result.data[9]);
                m_wi.flag = (sbyte)IFNULL(_result.data[11]);
                //m_wi.apply_date = IFNULL(atoll, _result->data[12]);
                //m_wi.end_date = IFNULL(atoll, _result->data[13]);

                // Salve local unix date on WarehouseItemEx and System Unix Date on apply_date to send to client
                m_wi.apply_date_unix_local = IFNULL(_result.data[12]);
                m_wi.end_date_unix_local = IFNULL(_result.data[13]);

                // Date
                if (m_wi.apply_date_unix_local > 0)
                {
                    m_wi.apply_date = UtilTime.TzLocalUnixToUnixUTC(m_wi.apply_date_unix_local);
                }

                if (m_wi.end_date_unix_local > 0)
                {
                    m_wi.end_date = UtilTime.TzLocalUnixToUnixUTC(m_wi.end_date_unix_local);
                }

                m_wi.type = (sbyte)IFNULL(_result.data[14]);
                for (i = 0; i < 4; i++)
                {
                    m_wi.card.character[i] = IFNULL(_result.data[15 + i]); // 15 + 4
                }
                for (i = 0; i < 4; i++)
                {
                    m_wi.card.caddie[i] = IFNULL(_result.data[19 + i]); // 19 + 4
                }
                for (i = 0; i < 4; i++)
                {
                    m_wi.card.NPC[i] = IFNULL(_result.data[23 + i]); // 23 + 4
                }
                m_wi.clubset_workshop.flag = (short)IFNULL(_result.data[27]);
                for (i = 0; i < 5; i++)
                {
                    m_wi.clubset_workshop.c[i] = (short)IFNULL(_result.data[28 + i]); // 28 + 5
                }
                m_wi.clubset_workshop.mastery = IFNULL(_result.data[33]);
                m_wi.clubset_workshop.recovery_pts = IFNULL(_result.data[34]);
                m_wi.clubset_workshop.level = IFNULL<int>(_result.data[35]);
                m_wi.clubset_workshop.rank = IFNULL<int>(_result.data[36]);
                if (is_valid_c_string(_result.data[37]))
                {
                    m_wi.ucc.name = _result.GetString(37);
                }
                if (is_valid_c_string(_result.data[38]))
                {
                    m_wi.ucc.idx = _result.GetString(38);
                }
                m_wi.ucc.seq = (short)((short)IFNULL<uint>(_result.data[39]));
                if (is_valid_c_string(_result.data[40]))
                {
                    m_wi.ucc.copier_nick = _result.GetString(40);
                }
                m_wi.ucc.copier = IFNULL(_result.data[41]);
                m_wi.ucc.trade = (sbyte)IFNULL<int>(_result.data[42]);
                m_wi.ucc.status = (byte)IFNULL<int>(_result.data[44]);
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_wi.id <= 0 || m_wi._typeid == 0)
            {
                throw new exception("[CmdGetGiftClubSet::prepareConsulta][Error] ClubSet[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi._typeid));

            checkResponse(r, "nao conseguiu pegar o presente de ClubSet[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcGetGiftClubSet";
    }
}