using Pangya_LoginServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using System;
using System.Data;

namespace Pangya_LoginServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB
    {
        public CmdPlayerInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.m_pi = new PlayerInfoBase();
        }
 
        public PlayerInfoBase getInfo()
        {
            return m_pi;
        } 

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(8);
            try
            { 
                // Aqui faz as coisas
                m_pi.UID = IFNULL<uint>(_result.data[0]);
                if (is_valid_c_string(_result.data[1]))
                {
                    m_pi.Login = _result.GetString(1);
                }
                if (is_valid_c_string(_result.data[2]))
                {
                    m_pi.NickName = _result.GetString(2);
                } 
                m_pi.Capability = IFNULL<uint>(_result.data[4]);
                m_pi.Level = IFNULL<ushort>(_result.data[5]);
                m_pi.BlockFlag.SetState(IFNULL<ulong>(_result.data[6]));
                m_pi.BlockFlag.State.TimeBlock = IFNULL<int>(_result.data[7]);
                // Fim 
                if (m_pi.UID != m_uid)
                {
                    throw new exception("[CmdRegisterPlayerLogin::lineResult][Error] UID is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                       4, 0));
                }
            }
            catch (Exception)
            { 
                throw;
            }
        }

        protected override Response prepareConsulta()
        {

            m_pi.Clear();

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar o info do player: " + Convert.ToString(m_uid));

            return r;
        }

        protected PlayerInfoBase m_pi = new PlayerInfoBase();
        protected uint m_uid = new uint();

        private const string m_szConsulta = "pangya.ProcGetPlayerInfoLogin";
    }
}


