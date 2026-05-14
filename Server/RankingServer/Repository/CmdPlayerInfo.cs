using System; 
using Pangya_RankingServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_RankingServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB 
    { 

        public CmdPlayerInfo(uint _uid, bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
        }
         
        public uint getUID()
        {
            return m_uid;
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

            checkColumnNumber(8, (uint)_result.cols);

            m_pi.uid = IFNULL<uint>(_result.data[0]);
             
            if (is_valid_c_string(_result.data[1]))
            {
                m_pi.id = _result.GetString(1); 
            }

            if (is_valid_c_string(_result.data[2]))
            {
                m_pi.nickname = _result.GetString(2); 
            }

            m_pi.m_cap = IFNULL<uint>(_result.data[3]);
            m_pi.server_uid = IFNULL<uint>(_result.data[4]);
            m_pi.level = IFNULL<ushort>(_result.data[5]);
            m_pi.block_flag.setIDState(IFNULL<ulong>(_result.data[6]));
            m_pi.block_flag.m_id_state.block_time = IFNULL<int>(_result.data[7]);

            if (m_pi.uid != m_uid)
            {
                throw new exception("[CmdPlayerInfo::lineResult][Error] Player UID_REQUEST=" + Convert.ToString(m_uid) + " not match from UID_RETURNED=" + Convert.ToString(m_pi.uid), STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdPlayerInfo::prepareConsulta][Error] m_uid(" + Convert.ToString(m_uid) + ") is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_pi.clear();

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "Nao conseguiu pegar o info do player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
         
        private uint m_uid = new uint();
        private player_info m_pi = new player_info();

        private string m_szConsulta = "pangya.ProcGetPlayerInfoRank";
    }
}
