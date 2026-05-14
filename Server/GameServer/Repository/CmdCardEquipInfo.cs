using System;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using PangyaAPI.SQL; 
namespace Pangya_GameServer.Repository
{
    public class CmdCardEquipInfo : Pangya_DB
    { 
        public CmdCardEquipInfo(uint _uid)
        {
            this.m_uid = _uid;
        }

        public CardEquipManager getInfo()
        {
            return v_cei;
        } 

        protected override void lineResult(ctx_res _result, uint _index_reuslt)
        {
            checkColumnNumber(13/*tempo*/);

            CardEquipInfoEx cei = new CardEquipInfoEx
            {
                index = IFNULL<int>(_result.data[0]),
                _typeid = IFNULL(_result.data[1]),
                parts_typeid = IFNULL(_result.data[3]),
                parts_id = IFNULL(_result.data[4]),
                efeito = IFNULL(_result.data[5]),
                efeito_qntd = IFNULL(_result.data[6]),
                slot = IFNULL(_result.data[7])
            };
            if (_result.IsNotNull(8))
                cei.use_date.CreateTime(_result.GetDateTime(8));
            if (_result.IsNotNull(9))
                cei.end_date.CreateTime(_result.GetDateTime(9));

            cei.tipo = IFNULL(_result.data[11]);
            cei.use_yn = (byte)IFNULL(_result.data[12]); 
                v_cei.Add(cei);
        }

        protected override Response prepareConsulta()
        {

            v_cei.Clear();

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", 0");

            checkResponse(r, "nao conseguiu pegar o card equip info do player: " + Convert.ToString(m_uid));

            return r;
        }


        private uint m_uid = new uint();
        private CardEquipManager v_cei = new CardEquipManager();

        private const string m_szConsulta = "pangya.ProcGetCardEquip";
    }

}