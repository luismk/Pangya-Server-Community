using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdMyRoomConfig : Pangya_DB
    {
        private uint m_uid;
        MyRoomConfig m_mrc;
        public CmdMyRoomConfig(uint _uid)
        {
            m_uid = (_uid);
            m_mrc = new MyRoomConfig();
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3);
            try
            {
                if (_result.data[0] != null)
                {
                    m_mrc.pass = (_result.data[0]).ToString();
                }
                m_mrc.public_lock = Convert.ToByte(_result.data[1]);
                m_mrc.allow_enter = Convert.ToInt16(_result.data[2]);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = consulta("SELECT senha, public_lock, state FROM pangya.pangya_myroom WHERE uid = " + m_uid);
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }


        public MyRoomConfig getMyRoomConfig()
        {
            return m_mrc;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }
    }
}