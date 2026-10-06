using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    partial class CmdInsertShopGiftLog : Pangya_DB
    {
        private uint m_uid;
        private int gift_id;
        private int m_gift_id;
        bool m_can_receive;

        public CmdInsertShopGiftLog(uint uid, int gift_id, int item_typeid, int item_qntd)
        {
            this.m_uid = uid;
            this.gift_id = gift_id;
            this.m_gift_id = item_typeid;
            this.m_can_receive = false;
        }


        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);

            m_can_receive = IFNULL<bool>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0)
                throw new exception("[CmdShopGiftLog::prepareConsulta][Error] UID[value=" + (m_uid) + "] is invalid",
                    STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));

            var r = procedure(m_szConsulta, (m_uid) + ", " + (m_gift_id));

            checkResponse(r, "nao conseguiu inserir log ShopRoom gift");

            return r;
        }


        public const string m_szConsulta = "pangya.ProcGetShopGiftLog";

    }
}