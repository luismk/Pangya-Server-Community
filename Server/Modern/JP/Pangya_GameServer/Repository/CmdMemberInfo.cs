using System;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;
namespace Pangya_GameServer.Repository
{
    public class CmdMemberInfo : Pangya_DB
    {
        uint m_uid;
        PlayerMemberInfo m_mi;
        public CmdMemberInfo(uint _uid)
        {
            m_uid = _uid;
            m_mi = new PlayerMemberInfo();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(28);
            try
            {
                // Aqui faz as coisas 
                if (is_valid_c_string(_result.data[0]))
                    m_mi.Login = Convert.ToString(_result.data[0]);

                m_mi.UID = Convert.ToUInt32(_result.data[1]);
                m_mi.Gender = Convert.ToByte(_result.data[2]);
                m_mi.Tutorial = Convert.ToByte(_result.data[3]);

                if (is_valid_c_string(_result.data[4]))
                    m_mi.NickName = Convert.ToString(_result.data[4]);

                m_mi.DisplayID = "@NT_" + m_mi.NickName;
                m_mi.SchoolIndex = Convert.ToUInt32(_result.data[5]);
                m_mi.Capability.Value = Convert.ToInt32(_result.data[6]);
                m_mi.MannerFlag = Convert.ToUInt32(_result.data[9]);
                if (is_valid_c_string(_result.data[11]))
                    m_mi.GuildName = Convert.ToString(_result.data[11]);

                m_mi.GuildIndex = Convert.ToUInt32(_result.data[12]);
                m_mi.GuildWinPangs = Convert.ToInt64(_result.data[13]);
                m_mi.GuildWinPoints = Convert.ToUInt32(_result.data[14]);
                m_mi.GuildMarkIndex = Convert.ToUInt32(_result.data[15]); // Guild Idx é o ultilizado no PangYa JP
                m_mi.Event1 = Convert.ToByte(_result.data[16]);
                m_mi.Event2 = Convert.ToByte(_result.data[17]);

                // 1 Player loga primeira vezes, 2 é o um player que já logou mais de 1x
                m_mi.FlagLoginTime = 2;//eu uso 0

                // Sexo do player
                m_mi.State.Gender = m_mi.Gender; //tem que setar uma identidade aqui.
                m_mi.State.Value = m_mi.Gender;
                m_mi.PapelShop.LimitCount = Convert.ToUInt16(_result.data[18]);
                m_mi.PapelShop.CurrentCount = Convert.ToUInt16(_result.data[22]);
                m_mi.PapelShop.RemainCount = Convert.ToUInt16(_result.data[23]);

                if (_result.IsNotNull(24))
                    m_mi.PapelShopLastUpdate.CreateTime(_result.data[24].ToString());

                m_mi.GameLevel = Convert.ToByte(_result.data[25]);

                if (is_valid_c_string(_result.data[26]))
                    m_mi.GuildMarkImage = Convert.ToString(_result.data[26]);


                if (m_mi.UID != m_uid)
                    throw new Exception("[CmdMemberInfo::lineResult][Error] UID do member info do player nao e igual ao requisitado. UID Req: " + (m_uid) + " != " + (m_mi.UID));

            }
            catch (Exception ex)
            {
                Console.WriteLine("[CmdMemberInfo::lineResult][Error]: " + ex.Message);
            }
        }

        public PlayerMemberInfo getInfo()
        {
            return m_mi;
        }

        public PlayerCapability getCap()
        {
            return m_mi.Capability;
        }


        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetUserInfo", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }
    }
}
