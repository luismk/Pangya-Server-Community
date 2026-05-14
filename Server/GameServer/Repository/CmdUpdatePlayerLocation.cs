using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdatePlayerLocation : Pangya_DB
    {
        public CmdUpdatePlayerLocation(uint _uid, stPlayerLocationDB _pl)
        {
            this.m_uid = _uid;
            this.m_pl = _pl;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public stPlayerLocationDB getInfo()
        {
            return m_pl;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // Não usa por que é um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdatePlayerLocation::prepareConsulta][Error] PLAYER[UID=" + Convert.ToString(m_uid) + "] is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString((short)m_pl.channel) + ", " + Convert.ToString((short)m_pl.lobby) + ", " + Convert.ToString((short)m_pl.room) + ", " + Convert.ToString(m_pl.place.ulPlace));

            checkResponse(r, "nao conseguiu atualizar PLAYER[UID=" + Convert.ToString(m_uid) + "] Location[CHANNEL=" + Convert.ToString((short)m_pl.channel) + ", LOBBY=" + Convert.ToString((short)m_pl.lobby) + ", ROOM=" + Convert.ToString(m_pl.room) + ", PLACE=" + Convert.ToString((ushort)m_pl.place.ulPlace) + "]");

            return r;
        }

        private stPlayerLocationDB m_pl;
        private uint m_uid = new uint();

        private const string m_szConsulta = "pangya.ProcUpdatePlayerLocation";
    }
}
