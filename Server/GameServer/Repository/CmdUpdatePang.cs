using System;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdatePang : Pangya_DB
    {
        public enum T_UPDATE_PANG : byte
        {
            INCREASE,
            DECREASE
        }

        public CmdUpdatePang(uint _uid, ulong _pang, T_UPDATE_PANG _type_update)
        {
            this.m_uid = _uid;
            this.m_pang = _pang;
            this.m_type_update = _type_update;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public ulong getPang()
        {
            return (m_pang);
        }

        public void setPang(ulong _pang)
        {
            m_pang = _pang;
        }

        public CmdUpdatePang.T_UPDATE_PANG getTypeUpdate()
        {
            return m_type_update;
        }

        public void setTypeUpdate(T_UPDATE_PANG _type_update)
        {
            m_type_update = _type_update;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // Aqui � update ent�o nao usa o _result e nem o index
            return;
        }
        protected override Response prepareConsulta()
        {
            string m_consulta = "";

            if (m_type_update == T_UPDATE_PANG.INCREASE)
            {
                // Resultado: UPDATE pangya.user_info SET pang = pang + 1000 WHERE UID = 9
                m_consulta = $"{m_szConsulta[0]} + {m_pang} {m_szConsulta[1]} {m_uid}";
            }
            else
            {
                m_consulta = $@"
            DECLARE @atual BIGINT;
            DECLARE @reducao BIGINT = {m_pang};
            SELECT @atual = pang FROM pangya.user_info WHERE UID = {m_uid};
            
            UPDATE pangya.user_info 
            SET pang = CASE WHEN (@atual - @reducao) < 0 THEN 0 ELSE (@atual - @reducao) END 
            WHERE UID = {m_uid}";
            }

            var r = _update(m_consulta);

            checkResponse(r, "Não conseguiu atualizar o pang do player: " + m_uid);

            return r;
        }
        private uint m_uid = new uint();
        private ulong m_pang = new ulong();
        private T_UPDATE_PANG m_type_update;

        private string[] m_szConsulta = { "UPDATE pangya.user_info SET pang = pang ", " WHERE UID = " };
    }
}
