using Pangya_MessengerServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_MessengerServer.Repository
{
	public class CmdDeleteFriend : Pangya_DB
	{
			public CmdDeleteFriend()
			{
				this.m_uid = 0u;
				this.m_friend_uid = 0u;
			}

			public CmdDeleteFriend(uint _uid,
				uint _friend_uid
				)
				{
 this.m_uid = _uid;
 this.m_friend_uid = _friend_uid;
 				}

 
			public uint getUID()
			{
				return (m_uid);
			}

			public void setUID(uint _uid)
			{
 m_uid = _uid;
 			}

			public uint getFriendUID()
			{
				return (m_friend_uid);
			}

			public void setFriendUID(uint _friend_uid)
			{
 m_friend_uid = _friend_uid;
 			}

			protected override void lineResult(ctx_res _result, uint _index_result)
			{

				// N�o usa por que � um DELETE
				return;
			}

			protected override Response prepareConsulta()
			{

				if(m_uid == 0)
				{
					throw new exception("[CmdDeleteFriend::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
						4, 0));
				}

				if(m_friend_uid == 0)
				{
					throw new exception("[CmdDeleteFriend::prepareConsulta][Error] m_friend_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
						4, 0));
				}

				var r = _delete(m_szConsulta[0] + Convert.ToString(m_uid) + m_szConsulta[1] + Convert.ToString(m_friend_uid));

				checkResponse(r, "nao conseguiu deletar Amigo[UID=" + Convert.ToString(m_friend_uid) + "] do player[UID=" + Convert.ToString(m_uid) + "]");

				return r;
			}
							  
			private uint m_uid = new uint();
			private uint m_friend_uid = new uint();

			private string[] m_szConsulta = { "DELETE FROM pangya.pangya_friend_list WHERE UID = ", " AND uid_friend = " };
	}
}
