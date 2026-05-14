using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdPlayerInfo : Pangya_DB
    {
        uint m_uid = 0;
        player_info m_pi;
        public CmdPlayerInfo(uint _uid)
        {
            m_uid = _uid;
            m_pi = new player_info();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(8);
            try
            {
                // Aqui faz as coisas
                m_pi.uid = uint.Parse(_result.data[0].ToString());
                if (is_valid_c_string(_result.data[1].ToString()))
                    m_pi.id = _result.data[1].ToString();
                if (is_valid_c_string(_result.data[2].ToString()))
                    m_pi.nickname = _result.data[2].ToString();
                if (is_valid_c_string(_result.data[3].ToString()))
                    m_pi.pass = _result.data[3].ToString();
                m_pi.level = short.Parse(_result.data[5].ToString());
                m_pi.block_flag.setIDState(ulong.Parse(_result.data[6].ToString()));
                m_pi.block_flag.m_id_state.block_time = (int.Parse(_result.data[7].ToString()));
                // Fim

                if (m_pi.uid != m_uid)
                    throw new Exception("[CmdPlayerInfo::lineResult][Error] UID do player info nao e igual ao requisitado. UID Req: " + (m_uid) + " != " + (m_pi.uid));
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


        public player_info getInfo()
        {
            return m_pi;
        }

    }
}
