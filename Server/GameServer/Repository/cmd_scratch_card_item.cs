using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
	public class CmdScratchCardItem : Pangya_DB
	{
		public CmdScratchCardItem(bool _waiter = false) : base(_waiter)
		{
			this.m_ctx_psi = new List< ctx_scratch_card_item >();
		}

		public List< ctx_scratch_card_item > getInfo()
		{
			return new List< ctx_scratch_card_item >(m_ctx_psi);
		}

		protected override void lineResult(ctx_res _result, uint _index_result)
		{

			checkColumnNumber((uint)_result.cols, (uint)_result.cols);

			ctx_scratch_card_item ctx_psi = new ctx_scratch_card_item();
			ctx_psi._typeid = IFNULL(_result.data[0]);
			ctx_psi.qntd = IFNULL(_result.data[1]);
			ctx_psi.probabilidade = IFNULL(_result.data[2]);
			ctx_psi.numero = IFNULL<int>(_result.data[3]);
			ctx_psi.tipo = ((SCRATCH_CARD_TYPE)IFNULL(_result.data[4]));
			ctx_psi.active = true; //ja vem ativo
			m_ctx_psi.Add(ctx_psi);
		}

		protected override Response prepareConsulta()
		{

			var r = consulta(m_szConsulta);

			checkResponse(r, "nao conseguiu pegar os scratch card itens ativos !");

			return r;
		}
 
		private List< ctx_scratch_card_item > m_ctx_psi = new List< ctx_scratch_card_item >();

		private string m_szConsulta = "SELECT TypeID, Quantidade, Probabilidade, numero, tipo FROM pangya.scratchy_item where active = 1 ORDER BY Tipo";
	}
}
