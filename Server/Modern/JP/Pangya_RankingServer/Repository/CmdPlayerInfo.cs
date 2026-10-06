using System; 
using Pangya_RankingServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;

namespace Pangya_RankingServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB 
    { 

        public CmdPlayerInfo(uint _uid)
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

        public PlayerInfoBase getInfo()
        {
            return m_pi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(8, (uint)_result.cols);

            m_pi.UID = IFNULL<uint>(_result.data[0]);
             
            if (is_valid_c_string(_result.data[1]))
            {
                m_pi.Login = _result.GetString(1); 
            }

            if (is_valid_c_string(_result.data[2]))
            {
                m_pi.NickName = _result.GetString(2); 
            }

            m_pi.Capability = IFNULL<uint>(_result.data[3]);
            m_pi.ServerIndex = IFNULL<uint>(_result.data[4]);
            m_pi.Level = IFNULL<ushort>(_result.data[5]);
            m_pi.BlockFlag.SetState(IFNULL<ulong>(_result.data[6]));
            m_pi.BlockFlag.State.TimeBlock = IFNULL<int>(_result.data[7]);

            if (m_pi.UID != m_uid)
            {
                throw new exception("[CmdPlayerInfo::lineResult][Error] Player UID_REQUEST=" + Convert.ToString(m_uid) + " not Match from UID_RETURNED=" + Convert.ToString(m_pi.UID), STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
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

            m_pi.Clear();

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "Nao conseguiu pegar o info do player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
         
        private uint m_uid = new uint();
        private PlayerInfoBase m_pi = new PlayerInfoBase();

        private string m_szConsulta = "pangya.ProcGetPlayerInfoRank";
    }
}
