using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdMemorialNormalItemInfo : Pangya_DB
    {
        public CmdMemorialNormalItemInfo()
        {
            this.m_item = new Dictionary<uint, ctx_coin_set_item>();
        }

        public Dictionary<uint, ctx_coin_set_item> getInfo()
        {
            return m_item;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(4);

            ctx_coin_set_item csi = new ctx_coin_set_item();
            ctx_coin_item_ex ci = new ctx_coin_item_ex();
            try
            {

                csi.flag = -100; // SetItem Flag, SEMPRE TEM QUE SER -100
                csi._typeid = (uint)IFNULL(_result.data[0]);

                ci.tipo = -1; // Normal Item
                ci.gacha_number = -1;
                ci.probabilidade = 0;

                ci._typeid = (uint)IFNULL(_result.data[2]);
                ci.qntd = (uint)IFNULL(_result.data[3]);

                var it = m_item.FirstOrDefault(c => c.Key == csi._typeid);

                if (it.Value != null) // add um item novo ao vector do map
                {
                    it.Value.item.Add(ci);
                }
                else
                { // Add um novo ao map
                    csi.tipo = (byte)IFNULL(_result.data[1]);

                    csi.item.Add(ci);
                    m_item[csi._typeid] = csi;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar os Memorial Normal Item Info");

            return r;
        }

        private Dictionary<uint, ctx_coin_set_item> m_item = new Dictionary<uint, ctx_coin_set_item>();

        private const string m_szConsulta = "pangya.ProcGetMemorialNormalItemInfo";
    }
}