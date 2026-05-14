using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdItemBuffInfo : Pangya_DB
    {
        public CmdItemBuffInfo()
        {
            this.m_uid = 0;
            this.v_ib = new List<ItemBuffEx>();
        }

        public CmdItemBuffInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.v_ib = new List<ItemBuffEx>();
        }

        public List<ItemBuffEx> GetInfo()
        {
            return new List<ItemBuffEx>(v_ib);
        }

        public uint GetUID()
        {
            return m_uid;
        }

        public void SetUID(uint _uid)
        {
            this.m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(7);

            ItemBuffEx ib = new ItemBuffEx();

            ib.index = IFNULL(_result.data[0]);
            ib._typeid = IFNULL(_result.data[1]);

            if (_result.IsNotNull(2))
                ib.use_date.CreateTime(_translateDate(_result.data[2]));

            if (_result.IsNotNull(3))
                ib.end_date.CreateTime(_translateDate(_result.data[3]));

            ib.tipo = IFNULL(_result.data[4]);
            ib.percent = IFNULL(_result.data[5]);
            ib.use_yn = (byte)IFNULL(_result.data[6]);
            if (ib.end_date != null)
                ib.tempo.setTime((uint)(UtilTime.SystemTimeToUnix(ib.end_date.ConvertTime()) - UtilTime.GetLocalTimeAsUnix())); // fim - agora (em segundos)

            v_ib.Add(ib);
        }

        protected override Response prepareConsulta()
        {
            v_ib.Clear();

            var r = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(r, "Não conseguiu pegar Yam (ou itens parecidos com o efeito do yam) Equip do player: " + m_uid);

            return r;
        }

        private uint m_uid;
        private List<ItemBuffEx> v_ib;

        private const string m_szConsulta = "pangya.ProcGetItemBuff";
    }
}
