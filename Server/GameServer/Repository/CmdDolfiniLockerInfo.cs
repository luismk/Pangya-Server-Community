using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDolfiniLockerInfo : Pangya_DB
    {
        readonly uint m_uid = uint.MaxValue;
        DolfiniLocker m_df = new DolfiniLocker();
        public CmdDolfiniLockerInfo(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            try
            {
                var uid_req = 0u;

                if (_result.cols == 4)
                { // Primeira consulta retorna o info do dolfini locker

                    checkColumnNumber(4);

                    uid_req = IFNULL<uint>(_result.data[0]);
                    if (is_valid_c_string(_result.data[1]))
                    {
                        STRCPY_TO_MEMORY_FIXED_SIZE(ref m_df.pass,
                            sizeof(char), _result.data[1]);
                    }
                    m_df.pang = IFNULL<ulong>(_result.data[2]);
                    m_df.locker = IFNULL<bool>(_result.data[3]);

                }
                else if (_result.cols == 10)
                { // Segunda consulta retorna os itens guardado no dolfini locker

                    checkColumnNumber(10);

                    DolfiniLockerItem dli = new DolfiniLockerItem();

                    dli.item.id = IFNULL<int>(_result.data[0]);
                    uid_req = IFNULL<uint>(_result.data[1]);
                    dli.item._typeid = IFNULL<uint>(_result.data[2]);
                    if (is_valid_c_string(_result.data[3]))
                    {
                        STRCPY_TO_MEMORY_FIXED_SIZE(ref dli.item.sd_name,
                            sizeof(char), _result.data[3]);
                    }
                    if (is_valid_c_string(_result.data[4]))
                    {
                        STRCPY_TO_MEMORY_FIXED_SIZE(ref dli.item.sd_idx,
                            sizeof(char), _result.data[4]);
                    }
                    //strcpy_s(dli.item.sd_idx, _result->data[4]);
                    dli.item.sd_seq = IFNULL<short>(_result.data[5]);
                    if (is_valid_c_string(_result.data[6]))
                    {
                        STRCPY_TO_MEMORY_FIXED_SIZE(ref dli.item.sd_copier_nick,
                            sizeof(char), _result.data[6]);
                    }
                    //strcpy_s(dli.item.sd_copier_nick, _result->data[6]);
                    dli.item.sd_status = IFNULL<byte>(_result.data[7]);
                    dli.index = IFNULL<uint>(_result.data[8]);
                    dli.item.qntd = IFNULL<int>(_result.data[9]); // DOLFINI_LOCKER_FLAG, mas � quantidade

                    m_df.v_item.Add(dli);
                }

                if (uid_req != m_uid)
                {
                    throw new exception("[CmdDolfiniLockerInfo::lineResult][Error] O dolfini info requerido retornou um m_uid diferente. UID_req: " + Convert.ToString(m_uid) + " != " + Convert.ToString(uid_req), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                        3, 0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var m_szConsulta = new string[] { "pangya.ProcGetDolfiniLockerInfo", "pangya.ProcGetDolfiniLockerItem" };

            var r = procedure(m_szConsulta[0], m_uid.ToString());

            checkResponse(r, "nao conseguiu pegar o dolfini locker info do player: " + (m_uid));

            var r2 = procedure(m_szConsulta[1], m_uid.ToString());

            checkResponse(r2, "nao conseguiu pegar o dolfini locker item(ns) do player: " + (m_uid));

            // add second result_set to response
            for (var i = 0u; i < r2.getNumResultSet(); i++)
                r.addResultSet(r2.getResultSetAt(i));

            return r;
        }


        public DolfiniLocker getInfo()
        {
            return m_df;
        }

    }
}
