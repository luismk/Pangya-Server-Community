
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCaddieItem : Pangya_DB
    {

        public CmdUpdateCaddieItem(uint _uid,
            string _time, CaddieInfoEx _ci)
        {
            this.m_time = _time;
            this.m_ci = _ci;
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

        public string getTime()
        {
            return m_time;
        }

        public void setTime(string _time)
        {
            m_time = _time;
        }

        public CaddieInfoEx getInfo()
        {
            return m_ci;
        }

        public void setInfo(CaddieInfoEx _ci)
        {
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_ci.id <= 0
                || m_ci._typeid == 0
                || m_ci.parts_typeid == 0
                || m_time.Length == 0)
            {
                throw new exception("[CmdUpdateCaddieItem::prepareConsulta][Error] invalid Caddie[TYPEID=" + Convert.ToString(m_ci._typeid) + ", ID=" + Convert.ToString(m_ci.id) + "] or Caddie Item[TYPEID=" + Convert.ToString(m_ci.parts_typeid) + ", TIME=" + m_time + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString(m_ci.parts_typeid) + ", " + makeText(m_time));

            checkResponse(r, "nao conseguiu atualizar o caddie item[TYPEID=" + Convert.ToString(m_ci.parts_typeid) + "] do caddie[ID=" + Convert.ToString(m_ci.id) + "] do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private string m_time = "";
        private CaddieInfoEx m_ci = new CaddieInfoEx();

        private const string m_szConsulta = "pangya.ProcUpdateCaddieItem";
    }
}
