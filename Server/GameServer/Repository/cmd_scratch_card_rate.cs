using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
	public class CmdScratchCardRate : Pangya_DB
	{
		public CmdScratchCardRate(bool _waiter = false) : base(_waiter)
		{
			this.m_rate = new Dictionary<SCRATCH_CARD_TYPE,ctx_scratch_card_rate>();
		} 
		
		public ctx_scratch_card_rate getInfo(SCRATCH_CARD_TYPE _type)
		{

			ctx_scratch_card_rate rate = new ctx_scratch_card_rate();

			foreach(var el in m_rate)
			{

				if(el.Value.Tipo == _type)
				{

					rate = el.Value;
				}
			}
			return rate;

		}

		public Dictionary< SCRATCH_CARD_TYPE, ctx_scratch_card_rate > getInfo()
		{

		return new Dictionary<SCRATCH_CARD_TYPE,ctx_scratch_card_rate>(m_rate);
		}

		protected override void lineResult(ctx_res _result, uint _index_result)
		{

			checkColumnNumber(3, (uint)_result.cols);

			ctx_scratch_card_rate rate = new ctx_scratch_card_rate();
			rate.Nome = _result.data[0].ToString();
			rate.Tipo = (SCRATCH_CARD_TYPE)(IFNULL(_result.data[1]));
			rate.Prob = IFNULL(_result.data[2]); 
			m_rate[rate.Tipo]= rate;
		}

		protected override Response prepareConsulta()
		{

			var r = consulta(m_szConsulta);

			checkResponse(r, "nao conseguiu pegar os scratch card itens ativos !");

			return r;
		}
 
		private Dictionary<SCRATCH_CARD_TYPE,ctx_scratch_card_rate> m_rate = new Dictionary<SCRATCH_CARD_TYPE,ctx_scratch_card_rate>();

		private string m_szConsulta = "SELECT nome, tipo,probabilidade FROM pangya.scratchy_rate order by tipo";
	}
}
