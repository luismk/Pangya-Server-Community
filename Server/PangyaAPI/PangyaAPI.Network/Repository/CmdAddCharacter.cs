using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdAddCharacter : CmdAddItemBase
    {
        CharacterInfo m_ci;
        public CmdAddCharacter(uint _uid, CharacterInfo _ci, byte _purchase, byte _gift_flag) : base(_uid, _purchase, _gift_flag)
        {
            m_uid = _uid;
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                m_ci.id = (int)(_result.data[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {

            var r = procedure("pangya.ProcAddCharacter", m_uid.ToString() + ", " +
                (m_ci.id.ToString()) + ", " +
                (m_ci._typeid.ToString()) + ", " +
                m_ci.default_hair.ToString() + ", " +
                m_ci.default_shirts.ToString() + ", " +
                m_purchase.ToString() + ", " +
                m_gift_flag.ToString() + ", " +
                m_ci.parts_typeid[0].ToString() + ", " +
                m_ci.parts_typeid[1].ToString() + ", " +
                m_ci.parts_typeid[2].ToString() + ", " +
                m_ci.parts_typeid[3].ToString() + ", " +
                m_ci.parts_typeid[4].ToString() + ", " +
                m_ci.parts_typeid[5].ToString() + ", " +
                m_ci.parts_typeid[6].ToString() + ", " +
                m_ci.parts_typeid[7].ToString() + ", " +
                m_ci.parts_typeid[8].ToString() + ", " +
                m_ci.parts_typeid[9].ToString() + ", " +
                m_ci.parts_typeid[10].ToString() + ", " +
                m_ci.parts_typeid[11].ToString() + ", " +
                m_ci.parts_typeid[12].ToString() + ", " +
                m_ci.parts_typeid[13].ToString() + ", " +
                m_ci.parts_typeid[14].ToString() + ", " +
                m_ci.parts_typeid[15].ToString() + ", " +
                m_ci.parts_typeid[16].ToString() + ", " +
                m_ci.parts_typeid[17].ToString() + ", " +
                m_ci.parts_typeid[18].ToString() + ", " +
                m_ci.parts_typeid[19].ToString() + ", " +
                m_ci.parts_typeid[20].ToString() + ", " +
                m_ci.parts_typeid[21].ToString() + ", " +
                m_ci.parts_typeid[22].ToString() + ", " +
                m_ci.parts_typeid[23].ToString());
            checkResponse(r, "nao conseguiu adicionar o character[TYPEID=" + (m_ci._typeid) + "] para o player: " + (m_uid));
            return r;
        }

        public CharacterInfo getInfo()
        {
            return m_ci;
        }

        public void setInfo(CharacterInfo _ci)
        {
            m_ci = _ci;
        }
    }
}
