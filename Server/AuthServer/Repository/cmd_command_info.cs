using Pangya_AuthServer.Models;
using PangyaAPI.SQL;
using System;
using System.Collections.Generic;
 
namespace Pangya_AuthServer.Repository
{
	public class CmdCommandInfo : Pangya_DB
	{
			public CmdCommandInfo(bool _waiter = false) : base(_waiter)
			{
			}

			public virtual void Dispose()
			{
			}

			public List< CommandInfo > getInfo()
			{
				return new List< CommandInfo >(v_ci);
			}

			protected override void lineResult(ctx_res _result, uint _index_result)
			{

				checkColumnNumber(11);

				CommandInfo ci = new CommandInfo();

				ci.idx = (uint)IFNULL(_result.data[0]);
				ci.id = (uint)IFNULL(_result.data[1]);

				for(var i = 0; i < 5; ++ i)
				{
					ci.arg[i] = (uint)IFNULL(_result.data[i + 2]); // 2 + 5
				}

				ci.target = (uint)IFNULL(_result.data[7]);
				ci.flag = (ushort)IFNULL(_result.data[8]);
				ci.valid = (byte)IFNULL(_result.data[9]);
			if (_result.data[10] is DateTime) 
 ci.reserveDate = (System.DateTime)_translateDate(_result.data[10]);
				v_ci.Add(ci);
			}

			protected override Response prepareConsulta()
			{

				if(v_ci.Count > 0)
				{
					v_ci.Clear();
 				}

				var r = procedure(
					m_szConsulta, "");

				checkResponse(r, "nao conseguiu pegar os comandos do server no banco de dados");

				return r;
			}
		 
			private List< CommandInfo > v_ci = new List< CommandInfo >();

			private string m_szConsulta = "pangya.ProcGetCommands";
	}
}
