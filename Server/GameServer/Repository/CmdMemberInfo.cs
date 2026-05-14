using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdMemberInfo : Pangya_DB
    {
        uint m_uid;
        MemberInfoEx m_mi;
        public CmdMemberInfo(uint _uid)
        {
            m_uid = _uid;
            m_mi = new MemberInfoEx();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(28);
            try
            {
                // Aqui faz as coisas 
                if (is_valid_c_string(_result.data[0]))
                    m_mi.id = Convert.ToString(_result.data[0]);

                m_mi.uid = Convert.ToUInt32(_result.data[1]);
                m_mi.sexo = Convert.ToByte(_result.data[2]);
                m_mi.do_tutorial = Convert.ToByte(_result.data[3]);

                if (is_valid_c_string(_result.data[4]))
                    m_mi.nick_name = Convert.ToString(_result.data[4]);

                m_mi.sDisplayID = "@NT_" + m_mi.nick_name;
                m_mi.school = Convert.ToUInt32(_result.data[5]);
                m_mi.capability.ulCapability = Convert.ToInt32(_result.data[6]);
                m_mi.manner_flag = Convert.ToUInt32(_result.data[9]);
                if (is_valid_c_string(_result.data[11]))
                    m_mi.guild_name = Convert.ToString(_result.data[11]);

                m_mi.guild_uid = Convert.ToUInt32(_result.data[12]);
                m_mi.guild_pang = Convert.ToInt64(_result.data[13]);
                m_mi.guild_point = Convert.ToUInt32(_result.data[14]);
                m_mi.guild_mark_img_no = Convert.ToUInt32(_result.data[15]); // Guild Idx é o ultilizado no PangYa JP
                m_mi.event_1 = Convert.ToByte(_result.data[16]);
                m_mi.event_2 = Convert.ToByte(_result.data[17]);

                // 1 Player loga primeira vezes, 2 é o um player que já logou mais de 1x
                m_mi.flag_login_time = 2;//eu uso 0

                // Sexo do player
                m_mi.state_flag.sexo = m_mi.sexo; //tem que setar uma identidade aqui.
                m_mi.state_flag.ucByte = m_mi.sexo;
                m_mi.papel_shop.limit_count = Convert.ToUInt16(_result.data[18]);
                m_mi.papel_shop.current_count = Convert.ToUInt16(_result.data[22]);
                m_mi.papel_shop.remain_count = Convert.ToUInt16(_result.data[23]);

                if (_result.IsNotNull(24))
                    m_mi.papel_shop_last_update.CreateTime(_result.data[24].ToString());

                m_mi.level = Convert.ToByte(_result.data[25]);

                if (is_valid_c_string(_result.data[26]))
                    m_mi.guild_mark_img = Convert.ToString(_result.data[26]);


                if (m_mi.uid != m_uid)
                    throw new Exception("[CmdMemberInfo::lineResult][Error] UID do member info do player nao e igual ao requisitado. UID Req: " + (m_uid) + " != " + (m_mi.uid));

            }
            catch (Exception ex)
            {
                Console.WriteLine("[CmdMemberInfo::lineResult][Error]: " + ex.Message);
            }
        }

        public MemberInfoEx getInfo()
        {
            return m_mi;
        }

        public uCapability getCap()
        {
            return m_mi.capability;
        }


        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetUserInfo", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }
    }
}
