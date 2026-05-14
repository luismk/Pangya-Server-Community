using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdLoginRewardPlayerInfo : Pangya_DB
    {

        public CmdLoginRewardPlayerInfo(ulong _id,
            uint _uid)
        {
            this.m_id = _id;
            this.m_uid = _uid;
            this.m_player = new stPlayerState(0u);
        }

        public CmdLoginRewardPlayerInfo(uint _uid)
        {
            this.m_id = 0Ul;
            this.m_uid = _uid;
            this.m_player = new stPlayerState(0u);
        }

        public CmdLoginRewardPlayerInfo()
        {
            this.m_id = 0Ul;
            this.m_uid = 0;
            this.m_player = new stPlayerState(0u);
        }

        public ulong getId()
        {
            return (m_id);
        }

        public void setId(ulong _id)
        {
            m_id = _id;
        }

        public uint getPlayerUID()
        {
            return (m_uid);
        }

        public void setPlayerUID(uint _uid)
        {
            m_uid = _uid;
        }

        public stPlayerState getInfo()
        {
            return m_player;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(6);

            SYSTEMTIME upt_date = new SYSTEMTIME();

            if (!(_result.data[5] is DBNull))
            {
                upt_date.CreateTime(_translateDate(_result.data[5]));
            }

            m_player.id = (ulong)IFNULL(_result.data[0]);
            m_player.uid = (uint)IFNULL(_result.data[1]);
            m_player.count_days = (uint)IFNULL(_result.data[2]);
            m_player.count_seq = (uint)IFNULL(_result.data[3]);
            m_player.update_date = upt_date;
            m_player.is_clear = (IFNULL<bool>(_result.data[4]) ? true : false);
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0Ul)
            {
                throw new exception("[CmdLoginRewardPlayerInfo::prepareConsulta][Error] m_id is invalid(" + Convert.ToString(m_id) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_uid == 0u)
            {
                throw new exception("[CmdLoginRewardPlayerInfo::prepareConsulta][Error] m_uid is invalid(" + Convert.ToString(m_id) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_player.clear();

            var r = consulta(m_szConsulta(m_id, m_uid));

            checkResponse(r, "nao conseguiu pegar o PLAYER[UID=" + Convert.ToString(m_uid) + "] do Login Reward[ID=" + Convert.ToString(m_id) + "]");

            return r;
        }

        private ulong m_id = new ulong();
        private uint m_uid = new uint();

        private stPlayerState m_player = new stPlayerState();

        // Uma alternativa mais limpa ao array de strings:
        public string m_szConsulta(ulong rewardId, uint uid)
        {
            return $"SELECT {makeEscapeKeyword("index")}, uid, count_days, count_seq, is_clear, update_date " +
                   $"FROM pangya.pangya_login_reward_player " +
                   $"WHERE login_reward_id = {rewardId} AND uid = {uid}";
        }
    }
}