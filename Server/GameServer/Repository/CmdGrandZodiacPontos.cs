using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdGrandZodiacPontos : Pangya_DB
    {
        public enum eCMD_GRAND_ZODIAC_TYPE : byte
        {
            CGZT_GET,
            CGZT_UPDATE
        }

        public CmdGrandZodiacPontos(uint _uid,
            eCMD_GRAND_ZODIAC_TYPE _type)
        {
            this.m_uid = (_uid);
            this.m_pontos = 0;
            this.m_type = _type;
        }

        public CmdGrandZodiacPontos(uint _uid,
            uint _pontos,
            eCMD_GRAND_ZODIAC_TYPE _type)
        {
            this.m_uid = (_uid);
            this.m_pontos = _pontos;
            this.m_type = _type;
        }

        public CmdGrandZodiacPontos()
        {
            this.m_uid = 0;
            this.m_pontos = 0;
            this.m_type = eCMD_GRAND_ZODIAC_TYPE.CGZT_GET;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public ulong getPontos()
        {
            return m_pontos;
        }

        public eCMD_GRAND_ZODIAC_TYPE getType()
        {
            return m_type;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            if (m_type == eCMD_GRAND_ZODIAC_TYPE.CGZT_GET)
            {

                checkColumnNumber(1);
                m_pontos = IFNULL(_result.data[0]);
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdGrandZodiacPontos::prepareConsulta][Error] m_uid(" + Convert.ToString(m_uid) + ") is invalid");
            }

            string query = "";

            if (m_type == eCMD_GRAND_ZODIAC_TYPE.CGZT_GET)
            {
                query = m_szConsulta[0] + Convert.ToString(m_uid);
            }
            else if (m_type == eCMD_GRAND_ZODIAC_TYPE.CGZT_UPDATE)
            {
                query = m_szConsulta[1] + Convert.ToString(m_pontos) + m_szConsulta[2] + Convert.ToString(m_uid);
            }

            var r = consulta(query);

            checkResponse(r, "nao conseguiu " + (m_type == eCMD_GRAND_ZODIAC_TYPE.CGZT_GET ? "pegar os pontos do Grand Zodiac" : "atualizar os pontos[" + Convert.ToString(m_pontos) + "]") + " do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = 0;
        private UInt64 m_pontos = 0;
        private eCMD_GRAND_ZODIAC_TYPE m_type;

        private string[] m_szConsulta = { "SELECT pontos FROM pangya.pangya_grand_zodiac_pontos WHERE UID = ", "UPDATE pangya.pangya_grand_zodiac_pontos SET pontos = ", " WHERE UID = " };

    }
}