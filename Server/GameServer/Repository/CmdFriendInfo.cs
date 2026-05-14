using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;


namespace Pangya_GameServer.Repository
{
    public class CmdFriendInfo : Pangya_DB
    {
        public CmdFriendInfo(uint _uid)
        {
            m_uid = _uid;
            m_fi = new Dictionary<uint, FriendInfo>();
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public Dictionary<uint, FriendInfo> getInfo()
        {
            return m_fi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(5);

            FriendInfo fi = new FriendInfo();
            fi.uid = (uint)IFNULL(_result.data[0]);

            if (is_valid_c_string(_result.data[1]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref fi.apelido, sizeof(char), _result.data[1]);
            }

            if (is_valid_c_string(_result.data[2]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref fi.id, sizeof(char), _result.data[2]);
            }

            if (is_valid_c_string(_result.data[3]))
            {
                STRCPY_TO_MEMORY_FIXED_SIZE(ref fi.nickname, sizeof(char), _result.data[3]);
            }

            fi.sex = (byte)IFNULL(_result.data[4]);

            if (!m_fi.ContainsKey(fi.uid)) // Não tem, adiciona um novo amigo
            {
                m_fi.Add(fi.uid, fi);
            }
            else
            {
                _smp.message_pool.getInstance().push(new message(
                     $"[CmdFriendInfo::lineResult][Error][Warning] PLAYER[UID={m_uid}] tentou adicionar o amigo[UID={fi.uid}, ID={fi.id}] duplicado no banco de dados.",
                     type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdFriendInfo::prepareConsulta][Error] m_uid is invalid(0)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));
            }

            var r = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(r, $"Não conseguiu pegar a lista de amigos do jogador[UID={m_uid}]");

            return r;
        }

        private uint m_uid;
        private Dictionary<uint, FriendInfo> m_fi;

        private const string m_szConsulta = "pangya.ProcGetFriendInfo";
    }
}
