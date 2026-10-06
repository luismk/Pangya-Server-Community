using System;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Network.Models;
namespace Pangya_GameServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB
    {
        uint m_uid = 0;
        PlayerInfoBase UserInfo;
        public CmdPlayerInfo(uint _uid)
        {
            m_uid = _uid;
            UserInfo = new PlayerInfoBase();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(8);
            try
            {
                // Aqui faz as coisas
                UserInfo.UID = uint.Parse(_result.data[0].ToString());
                if (is_valid_c_string(_result.data[1].ToString()))
                    UserInfo.Login = _result.data[1].ToString();
                if (is_valid_c_string(_result.data[2].ToString()))
                    UserInfo.NickName = _result.data[2].ToString();
                //if (is_valid_c_string(_result.data[3].ToString()))
                //    PlayerUserStatistics.pass = _result.data[3].ToString();
                UserInfo.Level = ushort.Parse(_result.data[5].ToString());
                UserInfo.BlockFlag.SetState(ulong.Parse(_result.data[6].ToString()));
                UserInfo.BlockFlag.State.TimeBlock = (int.Parse(_result.data[7].ToString()));
                // Fim

                if (UserInfo.UID != m_uid)
                    throw new Exception("[CmdPlayerInfo::lineResult][Error] UID do player info nao e igual ao requisitado. UID Req: " + (m_uid) + " != " + (UserInfo.UID));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetPlayerInfoGame", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o info do player: " + (m_uid));
            return r;
        }


        public PlayerInfoBase getInfo()
        {
            return UserInfo;
        }

    }
}
