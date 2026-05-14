using Pangya_MessengerServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pangya_MessengerServer.Repository
{
    public class CmdFriendInfo : Pangya_DB
    {
        public enum TYPE : byte
        {
            ALL,
            ONE
        }

        public CmdFriendInfo()
        {
            this.m_uid = 0u;
            this.m_type = TYPE.ALL;
            this.m_friend_uid = 0u;
            this.m_fi = new Dictionary<uint, FriendInfoEx>();
        }

        public CmdFriendInfo(uint _uid,
            TYPE _type,
            uint _friend_uid = 0u
            )
        {
            this.m_uid = _uid;
            this.m_type = (_type);
            this.m_friend_uid = _friend_uid;
            this.m_fi = new Dictionary<uint, FriendInfoEx>();
        }  

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
             m_uid = _uid;
        }

        public uint getFriendUID()
        {
            return (m_friend_uid);
        }

        public void setFriendUID(uint _friend_uid)
        {
            m_friend_uid = _friend_uid;
        }

        public CmdFriendInfo.TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public SortedDictionary<uint, FriendInfoEx> getInfo()
        {
            return new SortedDictionary<uint, FriendInfoEx>(m_fi);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(13, (uint)_result.cols);

            FriendInfoEx fi = new FriendInfoEx();

            if (is_valid_c_string(_result.data[0]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref fi.nickname,
                    sizeof(char), _result.data[0]);
            }
            fi.uid = (uint)IFNULL<int>(_result.data[1]);
            if (is_valid_c_string(_result.data[2]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref fi.apelido,
                    sizeof(char), _result.data[2]);
            }
            fi.lUnknown = IFNULL<int>(_result.data[3]);
            fi.lUnknown2 = IFNULL<int>(_result.data[4]);
            fi.lUnknown3 = IFNULL<int>(_result.data[5]);
            fi.lUnknown4 = IFNULL<int>(_result.data[6]);
            fi.lUnknown5 = IFNULL<int>(_result.data[7]);
            fi.lUnknown6 = IFNULL<int>(_result.data[8]);
            fi.cUnknown_flag = (byte)IFNULL<int>(_result.data[9]);
            fi.state.ucState = (byte)IFNULL<int>(_result.data[10]);
            fi.level = (byte)IFNULL<int>(_result.data[11]);
            fi.flag.ucFlag = (byte)IFNULL<int>(_result.data[12]);

            var it = m_fi.Any(c => c.Value.uid ==fi.uid);

            if (!it)
            {
                m_fi.Add(fi.uid, fi);
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[CmdFriendInfo::lineResult][Error][WARNIG] retornou duplicata de Amigos[UID=" + Convert.ToString(fi.uid) + "] do player[UID=" + Convert.ToString(m_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdFriendInfo::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(
                (m_type == TYPE.ALL) ? m_szConsulta[0] : m_szConsulta[1],
                Convert.ToString(m_uid) + ((m_type == TYPE.ALL) ? "" : ", " + Convert.ToString(m_friend_uid)));

            checkResponse(r, "nao conseguiu pegar a o Friend list do player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }



        private uint m_uid = 0;
        private uint m_friend_uid = 0;
        private TYPE m_type;
        private Dictionary<uint, FriendInfoEx> m_fi = new Dictionary<uint, FriendInfoEx>();

        private string[] m_szConsulta = { "pangya.ProcGetFriendAndGuildMemberInfo", "pangya.ProcGetFriendAndGuildMemberInfo_One" };
    }
}
