using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCardInfo : Pangya_DB
    {
        public enum TYPE : byte
        {
            ALL,
            ONE
        }

        public CmdCardInfo()
        {
            m_uid = 0;
            m_type = TYPE.ALL;
            m_card_id = 0;
            v_ci = new CardManager();
        }

        public CmdCardInfo(uint uid, TYPE type, uint cardId = 0)
        {
            m_uid = uid;
            m_type = type;
            m_card_id = cardId;
            v_ci = new CardManager();
        }

        public CardManager getInfo()
        {
            return v_ci;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint uid)
        {
            m_uid = uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE type)
        {
            m_type = type;
        }

        public uint getCardID()
        {
            return m_card_id;
        }

        public void setCardID(uint cardId)
        {
            m_card_id = cardId;
        }

        protected override void lineResult(ctx_res result, uint indexResult)
        {
            checkColumnNumber(11);

            var ci = new CardInfo
            {
                id = IFNULL<int>(result.data[0]),
                _typeid = IFNULL(result.data[2]),
                slot = IFNULL(result.data[3]),
                efeito = IFNULL(result.data[4]),
                efeito_qntd = IFNULL(result.data[5]),
                qntd = IFNULL<int>(result.data[6]),
                type = (byte)IFNULL(result.data[9]),
                use_yn = (byte)IFNULL(result.data[10])
            };

            if (result.IsNotNull(7))
                ci.use_date.CreateTime(_translateDate(result.data[7]));

            if (result.IsNotNull(8))
                ci.end_date.CreateTime(_translateDate(result.data[8]));

            v_ci.Add(ci.id, ci);
        }

        protected override Response prepareConsulta()
        {
            // 1. Reset de estado
            v_ci.Clear();

            // 2. Define the procedures
            string procName = (m_type == TYPE.ALL)
                ? m_szConsulta[0]
                : m_szConsulta[1];

            // 3. Define the parameters
            // Se for TYPE.ONE, envia UID e CardID. Se for ALL, envia apenas o UID.
            string parameters = (m_type == TYPE.ONE)
                ? $"{m_uid}, {m_card_id}"
                : $"{m_uid}";

            // 4. Execute and Validate
            var r = procedure(procName, parameters);

            checkResponse(r, $"Não foi possível carregar o card info do player: {m_uid}");

            return r;
        }

        private uint m_uid;
        private TYPE m_type;
        private uint m_card_id;
        private CardManager v_ci;

        private readonly string[] m_szConsulta = { "pangya.ProcGetCardInfo", "pangya.ProcGetCardInfo_One" };
    }
}
