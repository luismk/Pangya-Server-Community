using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;


namespace Pangya_GameServer.Repository
{
    public class CmdEmailInfo2 : Pangya_DB
    {

        public CmdEmailInfo2(uint _uid,
                int _email_id)
        {
            this._uid = _uid;
            this.m_email_id = _email_id;
        }
        public uint getUID()
        {
            return (_uid);
        }

        public void setUID(uint _uid)
        {
            this._uid = _uid;
        }

        public int getEmailId()
        {
            return (m_email_id);
        }

        public void setEmailId(int _email_id)
        {
            m_email_id = _email_id;
        }

        public EmailInfoEx getInfo()
        {
            return m_ei;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(17, (uint)_result.cols);

            if (m_ei.id <= 0)
            {

                m_ei.id = IFNULL<int>(_result.data[0]);

                if (is_valid_c_string(_result.data[1]))
                {
                    m_ei.from_id = _result.data[1].ToString();
                }
                if (is_valid_c_string(_result.data[2]))
                {
                    m_ei.gift_date = _result.data[2].ToString();
                }

                if (is_valid_c_string(_result.data[3]))
                {

                    try
                    {

                        // Translate Msg From Encoded Char not printed
                        m_ei.msg = _result.data[3].ToString();

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[CmdEmailInfo2::lineResult][ErrorSystem][Teste com o try para nao sair do cmd db] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                m_ei.visit_count = IFNULL(_result.data[4]);

                m_ei.lida_yn = (byte)IFNULL(_result.data[5]);

            }

            // Add o item
            EmailInfo.item item = new EmailInfo.item();

            item._typeid = IFNULL(_result.data[7]);

            if (item._typeid != 0)
            {

                item.id = IFNULL<int>(_result.data[6]); // ID vem antes de typeid na consulta

                item.flag_time = (byte)IFNULL(_result.data[8]);
                item.qntd = IFNULL(_result.data[9]);
                item.tempo_qntd = IFNULL(_result.data[10]);
                item.pang = IFNULL(_result.data[11]);
                item.cookie = IFNULL(_result.data[12]);
                item.gm_id = IFNULL<int>(_result.data[13]);
                item.flag_gift = IFNULL(_result.data[14]);
                if (is_valid_c_string(_result.data[15]))
                {
                    item.ucc_img_mark = _result.data[15].ToString();
                }
                item.type = (short)IFNULL(_result.data[16]);

                // Add Item
                m_ei.itens.Add(item);
            }

            if (m_ei.id != m_email_id)
            {
                throw new exception("[CmdEmailInfo2::lineResult][Error] o email info retornado nao e igual ao requisitado. req id: " + Convert.ToString(m_email_id) + " != " + Convert.ToString(m_ei.id));
            }
        }

        protected override Response prepareConsulta()
        {

            if (_uid == 0u)
            {
                throw new exception("[CmdEmailInfo2::prepareConsulta][Error] m_uid is invalid(0).");
            }

            if (m_email_id <= 0)
            {
                throw new exception("[CmdEmailInfo2::prepareConsulta][Error] m_email_id is invalid(" + Convert.ToString(m_email_id) + ").");
            }

            m_ei.clear();

            var r = procedure(m_szConsulta,
                Convert.ToString(_uid) + ", " + Convert.ToString(m_email_id));

            checkResponse(r, "nao conseguiu pegar o Email[ID=" + Convert.ToString(m_email_id) + "] information do PLAYER[UID=" + Convert.ToString(_uid) + "]");

            return r;
        }
        private uint _uid;
        private int m_email_id;

        private EmailInfoEx m_ei = new EmailInfoEx();

        private const string m_szConsulta = "pangya.ProcGetInformationEmail2";
    }
}