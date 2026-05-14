using System;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
	public class CmdInsertScratchCardRareWinLog : Pangya_DB
	{
		public CmdInsertScratchCardRareWinLog(bool _waiter = false) : base(_waiter)
		{
			this.m_uid = 0u;
			this.m_ctx_psb =  new ctx_scratch_card_item_win();
		}

		public CmdInsertScratchCardRareWinLog(uint _uid,
			ctx_scratch_card_item_win _ctx_psb,
			bool _waiter = false) : base(_waiter)
			{ 
			this.m_uid = _uid; 
			this.m_ctx_psb = _ctx_psb;
			}
 
		public uint getUID()
		{
			return (m_uid);
		}

		public void setUID(uint _uid)
		{
			m_uid = _uid;
		}

		public ctx_scratch_card_item_win getInfo()
		{
			return (m_ctx_psb);
		}

		public void setInfo(ctx_scratch_card_item_win _ctx_psb)
		{ 
		m_ctx_psb = _ctx_psb;
		}

		protected override void lineResult(ctx_res _result, uint _index_result)
		{

			// N�o usa por que � um INSERT
			return;
		}

		protected override Response prepareConsulta()
		{

			if(m_uid == 0)
			{
				throw new exception("[CmdInsertScratchCardRareWinLog::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
					4, 0));
			}

			if(m_ctx_psb.ctx_psi._typeid == 0)
			{
				throw new exception("[CmdInsertScratchCardRareWinLog::prepareConsulta][Error] m_ctx_psb.ctx_psi._typeid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
					4, 0));
			}

			var r = procedure(m_szConsulta,Convert.ToString(m_uid) + ", " + Convert.ToString(m_ctx_psb.ctx_psi._typeid));

			checkResponse(r, "nao conseguiu adicionar o Log de Rare Win[TYPEID=" + Convert.ToString(m_ctx_psb.ctx_psi._typeid) + ", QNTD=" + Convert.ToString(m_ctx_psb.qntd) + ", PROBABILIDADE=" + Convert.ToString(m_ctx_psb.ctx_psi.probabilidade) + "] do Scratch Card para o player[UID=" + Convert.ToString(m_uid) + "]");

			return r;
		}
 
		private uint m_uid = new uint();
		private ctx_scratch_card_item_win m_ctx_psb = new ctx_scratch_card_item_win();

		private string m_szConsulta = "pangya.ProcInsertScratchyRareWin";
	}
}
