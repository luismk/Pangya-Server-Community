using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Repository
{
	public class CmdScratchCardCoupon : Pangya_DB
	{
		public CmdScratchCardCoupon(bool _waiter = false) : base(_waiter)
		{
			this.m_ctx_psc = new SortedDictionary< uint, ctx_scratch_card_coupon >();
		}
 

		public SortedDictionary< uint, ctx_scratch_card_coupon > getInfo()
		{
			return new SortedDictionary< uint, ctx_scratch_card_coupon >(m_ctx_psc);
		}

		protected override void lineResult(ctx_res _result, uint _index_result)
		{

			checkColumnNumber((uint)_result.cols, (uint)_result.cols);

			ctx_scratch_card_coupon ctx_psc = new ctx_scratch_card_coupon();

			ctx_psc._typeid = IFNULL(_result.data[0]);
			if(ctx_psc._typeid != 0)
			{
				ctx_psc.active = true;
			}

			var it = m_ctx_psc.FirstOrDefault(c=> c.Key == ctx_psc._typeid);

			if(it.Key == 0) // N�o tem add um novo coupon
			{ 
			m_ctx_psc[ctx_psc._typeid] = ctx_psc;
			}
			else // J� tem um coupon no map, est� duplicado no banco de dados
			{
				_smp.message_pool.getInstance().push(new message("[CmdScratchCardCoupon::lineResult][WARNING] ja tem Scratch Coupon[TYPEID=" + Convert.ToString(ctx_psc._typeid) + "] duplicado no banco de dados.", type_msg.CL_FILE_LOG_AND_CONSOLE));
			}
		}

		protected override Response prepareConsulta()
		{

			var r = consulta(m_szConsulta);

			checkResponse(r, "nao conseguiu pegar os papel shop coupon(s).");

			return r;
		}
 
		private SortedDictionary< uint, ctx_scratch_card_coupon > m_ctx_psc = new SortedDictionary< uint, ctx_scratch_card_coupon >();

		private string m_szConsulta = "SELECT typeid FROM pangya.scratch_card_coupon where active = 1";
	}
}
