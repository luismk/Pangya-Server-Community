using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdMailBoxInfo : Pangya_DB
    {
        public enum TYPE : byte
        {
            NORMAL,
            NAO_LIDO
        }

        public CmdMailBoxInfo(bool _waiter = false)
        {
            this.m_uid = 0;
            this.m_type = TYPE.NORMAL;
            this.m_page = 0;
            this.m_total_page = 0;
            this.v_mb = new List<MailBox>();
        }

        public CmdMailBoxInfo(uint _uid, TYPE _type, uint _page = 1)
        {
            this.m_uid = _uid;
            this.m_type = _type;
            this.m_page = _page;
            this.m_total_page = 0;
            this.v_mb = new List<MailBox>();
        }

        public virtual void Dispose()
        {
        }

        public List<MailBox> GetInfo()
        {
            return new List<MailBox>(v_mb);
        }

        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void SetType(TYPE _type)
        {
            m_type = _type;
        }

        public uint GetPage()
        {
            return m_page;
        }

        public void SetPage(uint _page)
        {
            m_page = _page;
        }

        public uint GetTotalPage()
        {
            return m_total_page;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            if (_index_result == 0)
            {
                checkColumnNumber(18);

                MailBox mb = new MailBox
                {
                    id = IFNULL<int>(_result.data[0]),
                    from_id = is_valid_c_string(_result.data[1]) ? _result.data[1].ToString() : "",
                    msg = _result.data[2].ToString(),
                    visit_count = IFNULL(_result.data[4]),
                    lida_yn = (byte)IFNULL(_result.data[5]),
                    item_num = IFNULL(_result.data[6]),
                    item = GetItemFromResult(_result)
                };

                v_mb.Add(mb);
            }
            else if (_index_result == 1)
            {
                checkColumnNumber(1);
                m_total_page = IFNULL(_result.data[0]);
                m_total_page = (m_total_page % 20 == 0) ? m_total_page / 20 : m_total_page / 20 + 1;
            }
        }

        protected override Response prepareConsulta()
        {
            // 1. Validation
            if (m_uid == 0) throw new Exception("[CmdMailBoxInfo] m_uid is invalid (0).");
            if (m_page <= 0) throw new Exception($"[CmdMailBoxInfo] m_page ({m_page}) is invalid.");

            v_mb.Clear();
            m_total_page = 0;

            // 2. Query Execution
            // Using string interpolation for cleaner parameter passing
            Response r = (m_type == TYPE.NORMAL)
                ? procedure(m_szConsulta[0], $"{m_uid}, {m_page - 1}")
                : procedure(m_szConsulta[1], $"{m_uid}");

            // 3. Response Check
            string mailTypeName = (m_type == TYPE.NAO_LIDO) ? "unread" : "all";
            checkResponse(r, $"Failed to get {mailTypeName} emails from mailbox for player: {m_uid}");

            return r;
        }

        private EmailInfo.item GetItemFromResult(ctx_res _result)
        {
            var mb = new EmailInfo.item();
            mb.id = IFNULL<int>(_result.data[7]);
            mb._typeid = IFNULL(_result.data[8]);
            mb.flag_time = (byte)IFNULL(_result.data[9]);
            mb.qntd = IFNULL(_result.data[10]);
            mb.tempo_qntd = IFNULL(_result.data[11]);
            mb.pang = IFNULL(_result.data[12]);
            mb.cookie = IFNULL(_result.data[13]);
            mb.gm_id = IFNULL<int>(_result.data[14]);
            mb.flag_gift = IFNULL(_result.data[15]);
            mb.ucc_img_mark = is_valid_c_string(_result.data[16].ToString()) ? _result.data[16].ToString() : "";
            mb.type = (short)IFNULL(_result.data[17]);
            return mb;
        }

        private uint m_uid;
        private uint m_page;
        private uint m_total_page;
        private TYPE m_type;
        private List<MailBox> v_mb;
        private string[] m_szConsulta = { "pangya.ProcGetEmailFromMailBox", "pangya.ProcGetEmailNaoLidaFromMailBox" };
    }
}
