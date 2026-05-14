using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
	public class CmdPlayerInfo : Pangya_DB
	{
			public CmdPlayerInfo()
			{
				this.m_uid = 0;
				this.m_pi = new player_info();
			}

			public CmdPlayerInfo(uint _uid)
			{
this.m_uid = _uid;
				this.m_pi = new player_info();
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

			public player_info getInfo()
			{
return m_pi; 
			}

			public void updateInfo(player_info _pi)
			{
m_pi = _pi; 
			}

			protected override void lineResult(ctx_res _result, uint _index_result)
			{

				checkColumnNumber(8);

				// Aqui faz as coisas
				m_pi.uid = IFNULL(_result.data[0]);
				if(is_valid_c_string(_result.data[1]))
				{
					STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.id,
						sizeof(char), _result.data[1]);
				}
				if(is_valid_c_string(_result.data[2]))
				{
					STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.nickname,
						sizeof(char), _result.data[2]);
				}
				if(is_valid_c_string(_result.data[3]))
				{
					STRCPY_TO_MEMORY_FIXED_SIZE(ref m_pi.pass,
						sizeof(char), _result.data[3]);
				}
				m_pi.m_cap = IFNULL(_result.data[4]);
				m_pi.level = (ushort)IFNULL(_result.data[5]);
				m_pi.block_flag.setIDState(IFNULL(_result.data[6]));
				m_pi.block_flag.m_id_state.block_time = IFNULL<int>(_result.data[7]);
				// Fim

				if(m_pi.uid != m_uid)
				{ 
				}
			}

			protected override Response prepareConsulta()
			{

				m_pi.clear();

				var r = procedure(m_szConsulta,
					Convert.ToString(m_uid));

				checkResponse(r, "nao conseguiu pegar o info do player: " + Convert.ToString(m_uid));

				return r;
			}
		 
			protected player_info m_pi = new player_info();
			protected uint m_uid = new uint();

			private const string m_szConsulta = "pangya.ProcGetPlayerInfoLogin";
	}
}


