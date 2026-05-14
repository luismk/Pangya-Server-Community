using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdFuncPartsCharacter : Pangya_DB
    {
        private readonly uint m_uid;
        private readonly int m_typeid;
        public CmdFuncPartsCharacter(uint _uid, int _typeid)
        {
            m_uid = _uid;
            m_typeid = _typeid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            //e um update
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.FuncConcertaPartsCharacter", m_uid.ToString() + ", " + m_typeid.ToString());
            checkResponse(r, "nao conseguiu concertar o character[TYPEID=" + (m_typeid) + "] para o player: " + (m_uid));
            return r;
        }
    }
}
