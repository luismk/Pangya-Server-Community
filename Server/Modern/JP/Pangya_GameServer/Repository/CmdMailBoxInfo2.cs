using Pangya_GameServer.Models;

using PangyaAPI.DataBase;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pangya_GameServer.Repository
{
    public class CmdMailBoxInfo2 : Pangya_DB
    {
        public CmdMailBoxInfo2(uint _uid)
        {
            this.m_uid = _uid;
        }

        public CmdMailBoxInfo2()
        {
            this.m_uid = 0;
        }


        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint _uid)
        {
            this.m_uid = _uid;
        }

        public Dictionary<int, EmailInfoEx> getInfo()
        {
            return new Dictionary<int, EmailInfoEx>(m_emails);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(17);

            try
            {
                int id = IFNULL<int>(_result.data[0]);

                var it_email = m_emails.Where(c => c.Key == id).FirstOrDefault();

                if (it_email.Key != 0) // Verifica se o email já existe
                {
                    // Já tem, adiciona apenas o item
                    EmailInfo.ItemGift item = new EmailInfo.ItemGift
                    {
                        _typeid = IFNULL<uint>(_result.data[7])
                    };

                    if (item._typeid != 0)
                    {
                        item.id = IFNULL<int>(_result.data[6]); // ID vem antes de typeid na consulta
                        item.flag_time = (byte)IFNULL<uint>(_result.data[8]);
                        item.qntd = IFNULL<uint>(_result.data[9]);
                        item.tempo_qntd = IFNULL<uint>(_result.data[10]);
                        item.pang = IFNULL<uint>(_result.data[11]);
                        item.cookie = IFNULL<uint>(_result.data[12]);
                        item.gm_id = IFNULL<int>(_result.data[13]);
                        item.flag_gift = IFNULL<uint>(_result.data[14]);

                        if (is_valid_c_string(_result.data[15]))
                        {
                            item.ucc_img_mark = _result.data[15].ToString(); 
                        }

                        item.type = (short)IFNULL<uint>(_result.data[16]);

                        // Adiciona o item ao email existente
                        it_email.Value.itens.Add(item);
                    }
                }
                else
                {
                    // Não tem, cria um novo
                    EmailInfoEx email = new EmailInfoEx(0u)
                    {
                        id = id
                    };

                    if (is_valid_c_string(_result.data[1]))
                    {
                        email.from_id = _result.data[1].ToString();
                    }

                    if (is_valid_c_string(_result.data[2]))
                    {
                        email.gift_date = _result.data[2].ToString();
                    }

                    if (is_valid_c_string(_result.data[3]))
                    {
                        try
                        {
                            // Translate Msg From Encoded Char not printed 
                            email.msg = _result.data[3].ToString();


                        }
                        catch (exception e)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[CmdMailBoxInfo2::LineResult][ErrorSystem][Error during translation] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    email.visit_count = IFNULL<uint>(_result.data[4]);
                    email.lida_yn = (byte)IFNULL<uint>(_result.data[5]);

                    // Adiciona o item
                    EmailInfo.ItemGift item = new EmailInfo.ItemGift
                    {
                        _typeid = IFNULL<uint>(_result.data[7])
                    };

                    if (item._typeid != 0)
                    {
                        item.id = IFNULL<int>(_result.data[6]); // ID vem antes de typeid na consulta
                        item.flag_time = (byte)IFNULL<uint>(_result.data[8]);
                        item.qntd = IFNULL<uint>(_result.data[9]);
                        item.tempo_qntd = IFNULL<uint>(_result.data[10]);
                        item.pang = IFNULL<uint>(_result.data[11]);
                        item.cookie = IFNULL<uint>(_result.data[12]);
                        item.gm_id = IFNULL<int>(_result.data[13]);
                        item.flag_gift = IFNULL<uint>(_result.data[14]);

                        if (is_valid_c_string(_result.data[15]))
                        {
                            item.ucc_img_mark = _result.data[15].ToString(); 
                        }

                        item.type = (short)IFNULL<uint>(_result.data[16]);

                        // Adiciona o item ao novo email
                        email.itens.Add(item);
                    }

                    // Adiciona o email ao dicionário
                    m_emails.Add(email.id, email);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel::pacote04B][Error]: " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
                return new Response();

            if (m_emails.Count > 0)
            {
                m_emails.Clear();
            }

            var r = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(r, "Não conseguiu pegar todos os emails da caixa de correio do Normal[UID=" + m_uid + "]");

            return r;
        }

        private uint m_uid;
        private Dictionary<int, EmailInfoEx> m_emails = new Dictionary<int, EmailInfoEx>();

        private const string m_szConsulta = "pangya.ProcGetAllEmailFromMailBox";
    }
}
