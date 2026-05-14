using System;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateClubsetEquiped : Pangya_DB
    {
        public CmdUpdateClubsetEquiped()
        {
            this.m_uid = 0;
            this.m_clubset_id = 0;
        }

        public CmdUpdateClubsetEquiped(uint _uid,
            int _clubset_id)
        {

            this.m_uid = _uid;
            //this.

            this.m_clubset_id = _clubset_id;
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

        public int getClubsetID()
        {
            return (m_clubset_id);
        }

        public void setClubsetID(int _clubset_id)
        {

            m_clubset_id = _clubset_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_clubset_id));

            checkResponse(r, "nao conseguiu atualizar o clubset[ID=" + Convert.ToString(m_clubset_id) + "] equipado do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private int m_clubset_id = new int();

        private const string m_szConsulta = "pangya.USP_FLUSH_CLUB";
    }
}
