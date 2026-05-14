using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateSkinEquiped : Pangya_DB
    {
        public CmdUpdateSkinEquiped()
        {
            this.m_uid = 0;
            this.m_ue = new UserEquip();
        }

        public CmdUpdateSkinEquiped(uint _uid,
            UserEquip _ue)
        {
            this.m_uid = _uid;
            this.m_ue = _ue;
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

        public UserEquip getInfo()
        {
            // 
            return m_ue;
        }

        public void setInfo(UserEquip _ue)
        {
            m_ue = _ue;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usar aqui por que � UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ue.skin_typeid[0]) + ", " + Convert.ToString(m_ue.skin_typeid[1]) + ", " + Convert.ToString(m_ue.skin_typeid[2]) + ", " + Convert.ToString(m_ue.skin_typeid[3]) + ", " + Convert.ToString(m_ue.skin_typeid[4]) + ", " + Convert.ToString(m_ue.skin_typeid[5]));

            checkResponse(r, "nao conseguiu atualizar o skin equipado do player: " + Convert.ToString(m_uid));

            return r;
        }


        private uint m_uid = new uint();
        private UserEquip m_ue = new UserEquip();

        private const string m_szConsulta = "pangya.USP_FLUSH_SKIN";
    }
}
