using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
	public class CmdRegisterPlayerLogin : Pangya_DB
	{
			public CmdRegisterPlayerLogin()
			{
				this.m_uid = 0;
				this.m_ip = "";
				this.m_server_uid = 0;
			}

			public CmdRegisterPlayerLogin(uint _uid,
				string _ip,
				uint _server_uid)
				{
this.m_uid = _uid;
				this.m_ip = _ip;
this.m_server_uid = _server_uid;
 				}

			public void Dispose()
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

			public string getIP()
			{
				return m_ip;
			}

			public void setIP(string _ip)
			{
				m_ip = _ip;
			}

			public uint getServerUID()
			{
				return m_server_uid;
			}

			public void setServerUID(uint _server_uid)
			{
m_server_uid = _server_uid;
			}

			protected override void lineResult(ctx_res _result, uint _index_result)
			{

				// N�o usa por que � um UPDATE
				return;
			}

			protected override Response prepareConsulta()
			{

				if(m_ip.Length == 0)
				{
					throw new exception("[CmdRegisterPlayerLogin::prepareConsulta][Error] ip is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
						4, 0));
				}

				var r = procedure(m_szConsulta,
					Convert.ToString(m_uid) + ", " + makeText(m_ip) + ", " + Convert.ToString(m_server_uid));

				checkResponse(r, "nao conseguiu registrar o login do player: " + Convert.ToString(m_uid) + ", IP: " + m_ip);

				return r;
			}
		  
			private uint m_uid = new uint();
			private uint m_server_uid = new uint();
			private string m_ip = "";

			private const string m_szConsulta = "pangya.ProcRegisterLogin";
	}
}
