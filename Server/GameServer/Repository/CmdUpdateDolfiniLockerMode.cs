using System;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateDolfiniLockerMode : Pangya_DB
    {
        public CmdUpdateDolfiniLockerMode()
        {
            this.m_uid = 0;
            this.m_locker = 0;
        }

        public CmdUpdateDolfiniLockerMode(uint _uid,
            byte _locker)
        {
            this.m_uid = _uid;
            //this.
            this.m_locker = _locker;
        }

        public virtual void Dispose()
        {
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public byte getLocker()
        {
            return m_locker;
        }

        public void setLocker(byte _locker)
        {
            m_locker = _locker;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = _update(m_szConsulta[0] + Convert.ToString((ushort)m_locker) + m_szConsulta[1] + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu atualizar o modo[locker=" + Convert.ToString(m_locker) + "] do dolfini locker do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private byte m_locker;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_dolfini_locker SET locker = ", " WHERE UID = " };
    }
}
