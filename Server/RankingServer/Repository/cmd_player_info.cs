using stdA;
using System;

// Arquivo cmd_player_info.cpp
// Criado em 15/06/2020 as 16:41 por Acrisio
// Implementa��o da classe CmdPlayerInfo

#if _WIN32
// C++ TO C# CONVERTER TASK: There is no equivalent to most C++ 'pragma' directives in C#:
//#pragma pack(1)
#endif

// Arquivo cmd_player_info.hpp
// Criado em 15/06/2020 as 16:35 por Acrisio
// Defini��o da classe CmdPlayerInfo


// C++ TO C# CONVERTER WARNING: The following #include directive was ignored:
//#include "../../Projeto IOCP/PANGYA_DB/pangya_db.h"
// C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
// ORIGINAL LINE: #define ENUM_OPERATOR_PLUS_PLUS(__type, _element) (_element) = __type(static_cast< std::underlying_type< __type >::type >((_element)) + 1)

namespace stdA
{
	public class CmdPlayerInfo : pangya_db, System.IDisposable
	{
			public CmdPlayerInfo(bool _waiter = false) : base(_waiter)
			{
				this.m_uid = 0u;
			}

			public CmdPlayerInfo(uint32_t _uid, bool _waiter = false) : base(_waiter)
			{
// C++ TO C# CONVERTER TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
// ORIGINAL LINE: this.m_uid = _uid;
				this.m_uid.CopyFrom(_uid);
			}

			public virtual void Dispose()
			{
			}

			public uint32_t getUID()
			{
				return new uint32_t(m_uid);
			}

			public void setUID(uint32_t _uid)
			{
// C++ TO C# CONVERTER TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
// ORIGINAL LINE: m_uid = _uid;
				m_uid.CopyFrom(_uid);
			}

			public player_info getInfo()
			{
// C++ TO C# CONVERTER TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
// ORIGINAL LINE: return m_pi;
				return new stdA.player_info(m_pi);
			}

			protected override void lineResult(result_set.ctx_res _result, uint32_t _index_result)
			{

				checkColumnNumber(8, (uint32_t)_result.cols);

				m_pi.uid = IFNULL(atoi, _result.data[0]);

				size_t len = 0u;

				if(is_valid_c_string(_result.data[1]))
				{
					STRCPY_TO_MEMORY_FIXED_SIZE(m_pi.id,
						sizeof(char), _result.data[1]);
				}

				if(is_valid_c_string(_result.data[2]))
				{
					STRCPY_TO_MEMORY_FIXED_SIZE(m_pi.nickname,
						sizeof(char), _result.data[2]);
				}

				m_pi.m_cap = IFNULL(atoi, _result.data[3]);
				m_pi.server_uid = IFNULL(atoi, _result.data[4]);
				m_pi.level = (ushort)IFNULL(atoi, _result.data[5]);
				m_pi.block_flag.setIDState((uint64_t)IFNULL(atoll, _result.data[6]));
				m_pi.block_flag.m_id_state.block_time = IFNULL(atoi, _result.data[7]);

				if(m_pi.uid != m_uid)
				{
					throw exception("[CmdPlayerInfo::lineResult][Error] Player UID_REQUEST=" + Convert.ToString(m_uid) + " not match from UID_RETURNED=" + Convert.ToString(m_pi.uid), STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
						3, 0));
				}
			}

			protected override response prepareConsulta(database _db)
			{

				if(m_uid == 0u)
				{
					throw exception("[CmdPlayerInfo::prepareConsulta][Error] m_uid(" + Convert.ToString(m_uid) + ") is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
						4, 0));
				}

				m_pi.clear();

				var r = procedure(_db,
					m_szConsulta,
					Convert.ToString(m_uid));

				checkResponse(r, "Nao conseguiu pegar o info do player[UID=" + Convert.ToString(m_uid) + "]");

				return r;
			}

			// Class Name
			protected override string _getName()
			{
				return "CmdPlayerInfo";
			}
			protected override string _wgetName()
			{
				return "CmdPlayerInfo";
			}

			private uint32_t m_uid = new uint32_t();
			private player_info m_pi = new player_info();

			private string m_szConsulta = "pangya.ProcGetPlayerInfoRank";
	}
}
