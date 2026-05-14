
using System;
using PangyaAPI.SQL;  
namespace Pangya_AuthServer.Repository
{
	public class CmdGuildRankingUpdateTime : Pangya_DB
	{
			public CmdGuildRankingUpdateTime(bool _waiter = false) : base(_waiter)
			{
				
			}

			public virtual void Dispose()
			{
			}

			public DateTime getTime()
			{
				return m_si;
			}

			protected override void lineResult(ctx_res _result, uint _index_result)
			{

				checkColumnNumber(1, (uint)_result.cols);

				if(_result.data[0] != null)
				{
                m_si = (DateTime)_translateDate(_result.data[0]);
				}
			}

			protected override Response prepareConsulta()
			{

				var r = procedure(
					m_szConsulta, "");

				checkResponse(r, "Nao conseguiu pegar a date em que o Guild Ranking foi atualizado.");

				return r;
			} 
			private DateTime m_si = new DateTime();

			private string m_szConsulta = "pangya.ProcGetGuildRankingUpdateTime";
	}
}
