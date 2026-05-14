using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdMsgOffInfo : Pangya_DB
    {
        public CmdMsgOffInfo()
        {
            this.m_uid = 0;
            this.v_moi = new List<MsgOffInfo>();
        }

        public CmdMsgOffInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.v_moi = new List<MsgOffInfo>();
        }

        public List<MsgOffInfo> GetInfo()
        {
            return new List<MsgOffInfo>(v_moi);
        }

        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint _uid)
        {
            this.m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(5);

            MsgOffInfo moi = new MsgOffInfo
            {
                id = (short)IFNULL(_result.data[0]),
                from_uid = IFNULL(_result.data[1])
            };

            if (is_valid_c_string(_result.data[2]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref moi.nick, sizeof(char), _result.data[2]);
            }

            if (is_valid_c_string(_result.data[3]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref moi.msg, sizeof(char), _result.data[3]);
            }

            if (is_valid_c_string(_result.data[4]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref moi.date, sizeof(char), _result.data[4]);
            }

            v_moi.Add(moi);
        }

        protected override Response prepareConsulta()
        {
            v_moi.Clear();

            var r = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(r, "Não conseguiu pegar info de mensagens off do player: " + m_uid);

            return r;
        }

        private uint m_uid;
        private List<MsgOffInfo> v_moi;

        private const string m_szConsulta = "pangya.ProcGetMsgOff";
    }
}
