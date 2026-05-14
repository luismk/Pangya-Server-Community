using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateClubSetWorkshop : Pangya_DB
    {
        public enum FLAG : uint
        {
            F_TRANSFER_MASTERY_PTS,
            F_R_RECOVERY_PTS,
            F_UP_LEVEL,
            F_UP_LEVEL_CANCEL,
            F_UP_RANK,
            F_RESET
        }

        public CmdUpdateClubSetWorkshop(uint _uid,
            WarehouseItemEx _wi,
            FLAG _flag)
        {
            this.m_uid = _uid;
            this.m_flag = _flag;
            this.m_wi = _wi;
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

        public CmdUpdateClubSetWorkshop.FLAG getFlag()
        {
            return m_flag;
        }

        public void setFlag(FLAG _flag)
        {
            m_flag = _flag;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {
            m_wi = _wi;
        }

        protected override void lineResult(ctx_res _result, uint _index)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateClubSetWorkShop::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi.id <= 0 || m_wi._typeid == 0)
            {
                throw new exception("[CmdUpdateClubSetWorkShop::prepareConsulta][Error] WarehouseItem(ClubSet)[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (sIff.getInstance().getItemGroupIdentify(m_wi._typeid) != IFF_GROUP.CLUBSET)
            {
                throw new exception("[CmdUpdateClubSetWorkShop::prepareConsulta][Error] Item[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] nao é um ClubSet", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi.clubset_workshop.level) + ", " + Convert.ToString(m_wi.clubset_workshop.c[0]) + ", " + Convert.ToString(m_wi.clubset_workshop.c[1]) + ", " + Convert.ToString(m_wi.clubset_workshop.c[2]) + ", " + Convert.ToString(m_wi.clubset_workshop.c[3]) + ", " + Convert.ToString(m_wi.clubset_workshop.c[4]) + ", " + Convert.ToString(m_wi.clubset_workshop.mastery) + ", " + Convert.ToString(m_wi.clubset_workshop.rank) + ", " + Convert.ToString(m_wi.clubset_workshop.recovery_pts) + ", " + Convert.ToInt32(m_flag));

            checkResponse(r, "nao conseguiu atualizar ClubSet[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + "] WorkShop[C0=" + Convert.ToString(m_wi.clubset_workshop.c[0]) + ", C1=" + Convert.ToString(m_wi.clubset_workshop.c[1]) + ", C2=" + Convert.ToString(m_wi.clubset_workshop.c[2]) + ", C3=" + Convert.ToString(m_wi.clubset_workshop.c[3]) + ", C4=" + Convert.ToString(m_wi.clubset_workshop.c[4]) + ", Level=" + Convert.ToString(m_wi.clubset_workshop.level) + ", Mastery=" + Convert.ToString(m_wi.clubset_workshop.mastery) + ", Rank=" + Convert.ToString(m_wi.clubset_workshop.rank) + ", Recovery=" + Convert.ToString(m_wi.clubset_workshop.recovery_pts) + "] Flag=" + Convert.ToString(m_wi.clubset_workshop.flag) + " do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private FLAG m_flag;
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcUpdateClubSetWorkshop";
    }
}
