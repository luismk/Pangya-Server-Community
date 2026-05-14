using System;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateClubSetStats : Pangya_DB
    {
        public CmdUpdateClubSetStats(uint _uid,
            WarehouseItemEx _wi,
            uint _pang)
        {

            this.m_uid = _uid;
            //this.

            this.m_pang = _pang;
            this.m_wi = (_wi);
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

        public ulong getPang()
        {
            return (m_pang);
        }

        public void setPang(ulong _pang)
        {

            m_pang = _pang;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {

            m_wi = _wi;
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
                throw new exception("[CmdUpdateClubSetStats][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi.id <= 0 || m_wi._typeid == 0)
            {
                throw new exception("[CmdUpdateClubSetStats][Error] WarehouseItem[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_pang) + ", " + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_POWER]) + ", " + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_CONTROL]) + ", " + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_ACCURACY]) + ", " + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_SPIN]) + ", " + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_CURVE]));

            checkResponse(r, "nao conseguiu Atualizar ClubSet[ID=" + Convert.ToString(m_wi.id) + "] Stats[C0=" + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + Convert.ToString(m_wi.c[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private ulong m_pang = new ulong();
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcUpdateClubSetStats";
    }
}
