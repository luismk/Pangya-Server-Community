using System;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCaddieEquiped : Pangya_DB
    {
        public CmdUpdateCaddieEquiped(uint _uid,
            int _caddie_id)
        {
            this.m_uid = _uid;
            this.m_caddie_id = _caddie_id;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public int getCaddieID()
        {
            return m_caddie_id;
        }

        public void setCaddieID(int _caddie_id)
        {
            m_caddie_id = _caddie_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um update
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_caddie_id));

            checkResponse(r, "nao conseguiu atualizar o caddie[ID=" + Convert.ToString(m_caddie_id) + "] equipado. do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private int m_caddie_id = new int();

        private const string m_szConsulta = "pangya.USP_FLUSH_CADDIE";
    }
}
