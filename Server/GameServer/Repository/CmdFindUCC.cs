using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindUCC : Pangya_DB
    {
        public CmdFindUCC(int _id)
        {
            this.m_id = _id;
            this.m_wi = new WarehouseItemEx();
        }


        public int getId()
        {
            return m_id;
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(45);

            m_wi.id = IFNULL<int>(_result.data[0]);

            if (m_wi.id > 0)
            { // found
                var i = 0;

                m_wi._typeid = IFNULL(_result.data[2]);
                m_wi.ano = IFNULL<int>(_result.data[3]);
                for (i = 0; i < 5; i++)
                {
                    m_wi.c[i] = (short)IFNULL(_result.data[4 + i]); // 4 + 5
                }
                m_wi.purchase = (byte)IFNULL(_result.data[9]);
                m_wi.flag = (sbyte)IFNULL(_result.data[11]);
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
                    m_wi.ucc.name = IFNULL<string>(_result.data[37]);

                }
                if (is_valid_c_string(_result.data[38]))
                {
                    m_wi.ucc.idx = IFNULL<string>(_result.data[38]);
                }
                m_wi.ucc.seq = IFNULL<short>(_result.data[39]);
                if (is_valid_c_string(_result.data[40]))
                {
                    m_wi.ucc.copier_nick = IFNULL<string>(_result.data[40]);
                }
                m_wi.ucc.copier = IFNULL(_result.data[41]);
                m_wi.ucc.trade = (sbyte)IFNULL(_result.data[42]);
                m_wi.ucc.status = (byte)IFNULL(_result.data[44]);
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_id <= 0)
            {
                throw new exception("[CmdFindUCC::prepareConsulta][Error] m_id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_wi = new WarehouseItemEx();

            var r = procedure(m_szConsulta,
                Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu executar o procedure para procurar a UCC[ID=" + Convert.ToString(m_id) + "]");

            return r;
        }

        private int m_id = new int();
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcFindUCC";
    }
}