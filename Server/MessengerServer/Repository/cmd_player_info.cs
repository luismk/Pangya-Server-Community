using Pangya_MessengerServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;

namespace Pangya_MessengerServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB
    {
        public CmdPlayerInfo()
        {
            this.m_uid = 0u;
            this.m_pi = new player_info(0);
        }

        public CmdPlayerInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.m_pi = new player_info(0);
        }
                                              

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public player_info getInfo()
        {
            return m_pi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(11, (uint)_result.cols);

            m_pi.uid = (uint)IFNULL(_result.data[0]);
            if (is_valid_c_string(_result.data[1]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.id,
                    sizeof(char), _result.data[1]);
            }
            if (is_valid_c_string(_result.data[2]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.nickname,
                    sizeof(char), _result.data[2]);
            }
            m_pi.m_cap = (uint)IFNULL(_result.data[3]);
            m_pi.guild_uid = (uint)IFNULL(_result.data[4]);

            if (is_valid_c_string(_result.data[5]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.guild_name,
                    sizeof(char), _result.data[5]);
            }

            m_pi.sex = (byte)IFNULL(_result.data[6]);
            m_pi.level = (ushort)IFNULL(_result.data[7]);
            m_pi.server_uid = (uint)IFNULL(_result.data[8]);
            m_pi.block_flag.setIDState(IFNULL<ulong>(_result.data[9]));
            m_pi.block_flag.m_id_state.block_time = IFNULL<int>(_result.data[10]);

            if (m_uid != m_pi.uid)
            {
                throw new exception("[CmdPlayerInfo::lineResult][Error] player[UID_resquest=" + Convert.ToString(m_uid) + ", UID_return=" + Convert.ToString(m_pi.uid) + "] retornou um consulta diferente do esperado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdPlayerInfo::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, " nao conseguiu pegar o Info do Player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private player_info m_pi = new player_info();

        private const string m_szConsulta = "pangya.ProcGetPlayerInfoMessage";
    }
}
