using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdPutItemMailBox : Pangya_DB
    {
        public CmdPutItemMailBox(uint _uid_from,
            uint _uid_to,
            int _mail_id, stItem _item)
        {
            this.m_uid_from = _uid_from;
            this.m_uid_to = _uid_to;
            this.m_mail_id = _mail_id;
            this.m_item = (_item);
        }

        public CmdPutItemMailBox(uint _uid_from,
            uint _uid_to,
            int _mail_id,
            EmailInfo.item _item)
        {
            this.m_uid_from = _uid_from;
            this.m_uid_to = _uid_to;
            this.m_mail_id = _mail_id;
            this.m_item = new stItem();

            m_item.id = _item.id;
            m_item._typeid = _item._typeid;
            m_item.flag_time = _item.flag_time;
            m_item.c[0] = (short)((ushort)(m_item.qntd = (int)_item.qntd));
            m_item.c[3] = (short)((ushort)_item.tempo_qntd);
        }

        public uint getUIDFrom()
        {
            return (m_uid_from);
        }

        public void setUIDFrom(uint _uid_from)
        {
            m_uid_from = _uid_from;
        }

        public uint getUIDTo()
        {
            return (m_uid_to);
        }

        public void setUIDTo(uint _uid_to)
        {
            m_uid_to = _uid_to;
        }

        public int getMailID()
        {
            return (m_mail_id);
        }

        public void setLong(int _mail_id)
        {
            m_mail_id = _mail_id;
        }

        public stItem getItem()
        {
            return m_item;
        }

        public void setItem(stItem _item)
        {
            m_item = _item;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_mail_id <= 0 || m_uid_to == 0)
            {
                throw new exception("[CmdPutItemMailBox::prepareConsulta][Error] mail_id[value=" + Convert.ToString(m_mail_id) + "] is invalid or uid to send is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_item._typeid == 0)
            {
                throw new exception("[CmdPutItemMailBox::prepareConsulta][Error] item is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid_from) + ", " + Convert.ToString(m_uid_to) + ", " + Convert.ToString(m_mail_id) + ", " + Convert.ToString(m_item.id) + ", " + Convert.ToString(m_item._typeid) + ", " + Convert.ToString((ushort)m_item.flag_time) + ", " + Convert.ToString((m_item.qntd > 0xFFu) ? m_item.qntd : m_item.STDA_C_ITEM_QNTD) + ", " + Convert.ToString(m_item.c[3]));


            checkResponse(r, "PLAYER[UID=" + Convert.ToString(m_uid_from) + "] nao conseguiu adicionar item[TYPEID=" + Convert.ToString(m_item._typeid) + ", ID=" + Convert.ToString(m_item.id) + "] no mail[ID=" + Convert.ToString(m_mail_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid_to) + "]");

            return r;
        }

        private uint m_uid_from = 0;
        private uint m_uid_to = 0;
        private int m_mail_id = new int();
        private stItem m_item = new stItem();

        private const string m_szConsulta = "pangya.ProcInsertItemNoEmail";
    }
}