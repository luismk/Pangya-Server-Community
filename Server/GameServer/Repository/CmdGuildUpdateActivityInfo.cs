using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGuildUpdateActivityInfo : Pangya_DB
    {
        public CmdGuildUpdateActivityInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_guild_uid = 0;
            this.m_member_uid = 0;
            this.m_info = new List<GuildUpdateActivityInfo>();
        }

        public CmdGuildUpdateActivityInfo(int _guild_uid,
            uint _member_uid,
            bool _waiter = false) : base(_waiter)
        {
            this.m_guild_uid = _guild_uid;
            this.m_member_uid = _member_uid;
            this.m_info = new List<GuildUpdateActivityInfo>();
        }

        public virtual void Dispose()
        {

            // Clear, free memory
            if (m_info.Count > 0)
            {
                m_info.Clear();
            }
        }

        public int getGuildUID()
        {
            return (m_guild_uid);
        }

        public void setGuildUID(int _uid)
        {
            m_guild_uid = _uid;
        }

        public uint getMemberUID()
        {
            return (m_member_uid);
        }

        public void setMemberUID(uint _member_uid)
        {
            m_member_uid = _member_uid;
        }

        public List<GuildUpdateActivityInfo> getInfo()
        {
            return new List<GuildUpdateActivityInfo>(m_info);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(6);

            GuildUpdateActivityInfo guai = new GuildUpdateActivityInfo
            {
                index = IFNULL<ulong>(_result.data[0]),
                club_uid = (uint)IFNULL<uint>(_result.data[1]),
                owner_uid = (uint)IFNULL<uint>(_result.data[2]),
                player_uid = (uint)IFNULL<uint>(_result.data[3]),
                type = (GuildUpdateActivityInfo.TYPE_UPDATE)(IFNULL<int>(_result.data[4]))
            };

            if (_result.data[5] != DBNull.Value)
            {
                guai.reg_date.CreateTime(_translateDate(_result.data[5]));
            }

            if (guai.club_uid != m_guild_uid)
            {
                throw new exception("[CmdGuildUpdateActivityInfo::lineResult][Error] guild_uid requisitado é diferente do retornado pela consulta. QUERY_VALUES[GUILD_UID_REQ=" + Convert.ToString(m_guild_uid) + ", GUILD_UID_RET=" + Convert.ToString(guai.club_uid) + "].", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }

            if (guai.owner_uid != m_member_uid)
            {
                throw new exception("[CmdGuildUpdateActivityInfo::lineResult][Error] owner_uid requisitado é diferente do retornado pela consulta. QUERY_VALUES[OWNER_UID_REQ=" + Convert.ToString(m_member_uid) + ", OWNER_UID_RET=" + Convert.ToString(guai.owner_uid) + "].", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }

            // Add para o vector
            m_info.Add(guai);
        }

        protected override Response prepareConsulta()
        {

            if (m_guild_uid == 0u)
            {
                throw new exception("[CmdGuildUpdateActivityInfo::prepareConsulta][Error] m_guild_uid is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_member_uid == 0u)
            {
                throw new exception("[CmdGuildUpdateActivityInfo::prepareConsulta][Error] m_member_uid is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_info.Count > 0)
            {
                m_info.Clear();
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_guild_uid) + ", " + Convert.ToString(m_member_uid));

            checkResponse(r, "nao conseguiu pegar a Update Activity do Member[UID=" + Convert.ToString(m_member_uid) + "] da Guild[UID=" + Convert.ToString(m_guild_uid) + "]");

            return r;
        }
        private int m_guild_uid = 0;
        private uint m_member_uid = 0;
        private List<GuildUpdateActivityInfo> m_info = new List<GuildUpdateActivityInfo>();

        private const string m_szConsulta = "pangya.ProcGetGuildUpdateActivity";
    }
}
