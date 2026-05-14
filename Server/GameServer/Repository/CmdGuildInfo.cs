using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdGuildInfo : Pangya_DB
    {
        private uint m_uid;
        private uint m_option;
        GuildInfoEx m_gi = new GuildInfoEx();
        public CmdGuildInfo(uint uid, uint _option)
        {
            this.m_uid = uid;
            this.m_option = _option;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(13);
            try
            {
                m_gi.uid = IFNULL<int>(_result.data[0]);

                if (is_valid_c_string(_result.data[1]))
                    m_gi.name = _result.GetString(1);

                if (is_valid_c_string(_result.data[3]))
                    m_gi.mark_img = m_gi.mark_emblem = _result.GetString(3);

                m_gi.index_mark_emblem = IFNULL<uint>(_result.data[4]);

                m_gi.point = IFNULL(_result.data[7]);
                m_gi.pang = IFNULL(_result.data[8]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetGuildInfo", m_uid.ToString() + ", " + m_option.ToString());
            checkResponse(r, "nao conseguiu pegar o guild info do player: " + (m_uid));
            return r;
        }


        public GuildInfoEx getInfo()
        {
            return m_gi;
        }
    }
}