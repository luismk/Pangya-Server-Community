using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdTreasureHunterItem : Pangya_DB
    {
        public CmdTreasureHunterItem()
        {
            this.v_thi = new List<TreasureHunterItem>();
        }

        protected override void lineResult(ctx_res _result, uint _index)
        {

            TreasureHunterItem thi = new TreasureHunterItem
            {
                _typeid = IFNULL(_result.data[0]),
                qntd = IFNULL(_result.data[1]),
                probabilidade = IFNULL(_result.data[2]),
                active = (byte)IFNULL(_result.data[3]),
                flag = (byte)IFNULL(_result.data[4])
            }; // treasure hunter info

            v_thi.Add(thi);
        }
        protected override Response prepareConsulta()
        {

            v_thi.Clear();
            var r = consulta(
                m_szConsulta);

            checkResponse(r, "nao conseguiu pegar Treasure Hunter do server");

            return r;
        }
        public List<TreasureHunterItem> getInfo()
        {
            return v_thi;
        }

        List<TreasureHunterItem> v_thi;          // Treasure Hunter Info = THI

        string m_szConsulta = "SELECT typeid, quantidade, probabilidade, tipo, flag FROM pangya.pangya_treasure_item";
    }
}