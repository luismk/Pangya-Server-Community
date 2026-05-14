using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCharacterPCL : Pangya_DB
    {
        public CmdUpdateCharacterPCL(uint _uid,
            CharacterInfo _ci)
        {

            this.m_uid = _uid;
            //this.

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

        public CharacterInfo getInfo()
        {
            return (m_ci);
        }

        public void setInfo(CharacterInfo _ci)
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

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateCharacterPCL::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ci.id <= 0 || m_ci._typeid == 0)
            {
                throw new exception("[CmdUpdateCharacterPCL::prepareConsulta][Error] CharacterInfo[TYPEID=" + Convert.ToString(m_ci._typeid) + ", ID=" + Convert.ToString(m_ci.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_POWER]) + ", " + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_CONTROL]) + ", " + Convert.ToString((ushort)m_ci.pcl[2]) + ", " + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_SPIN]) + ", " + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_CURVE]));

            checkResponse(r, "nao conseguiu atualizar o Character[TYPEID=" + Convert.ToString(m_ci._typeid) + ", ID=" + Convert.ToString(m_ci.id) + "] PCL[c0=" + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_POWER]) + ", c1=" + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_CONTROL]) + ", c2=" + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_ACCURACY]) + ", c3=" + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_SPIN]) + ", c4=" + Convert.ToString((ushort)m_ci.pcl[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private CharacterInfo m_ci = new CharacterInfo();

        private const string m_szConsulta = "pangya.ProcUpdateCharacterPCL";
    }
}
