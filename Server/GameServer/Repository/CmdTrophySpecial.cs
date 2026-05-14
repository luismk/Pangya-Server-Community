using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdTrophySpecial : Pangya_DB
    {
        public CmdTrophySpecial(uint _uid, TYPE_SEASON _season, TYPE _type)
        {
            this.m_uid = _uid;
            this.m_season = _season;
            this.m_type = _type;
            this.v_tei = new List<TrofelEspecialInfo>();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3);

            TrofelEspecialInfo tei = new TrofelEspecialInfo
            {
                id = IFNULL<int>(_result.data[0].ToString()),
                _typeid = IFNULL(_result.data[1].ToString()),
                qntd = IFNULL<int>(_result.data[2].ToString())
            };
            v_tei.Add(tei);
        }

        protected override Response prepareConsulta()
        {
            v_tei.Clear();

            var r = procedure((m_type == TYPE.NORMAL) ? m_szConsulta[0] : m_szConsulta[1],
                Convert.ToUInt32(m_uid).ToString() + ", " + Convert.ToUInt32(m_season).ToString());

            checkResponse(r, "nao conseguiu pegar o Trophy Special do player: " + Convert.ToString(m_uid));

            return r;
        }

        public void setType(TYPE type)
        {
            m_type = type;
        }

        public List<TrofelEspecialInfo> getInfo()
        {
            return v_tei;
        }

        private uint m_uid;
        private TYPE m_type;
        private TYPE_SEASON m_season;
        private List<TrofelEspecialInfo> v_tei = new List<TrofelEspecialInfo>();

        private string[] m_szConsulta = { "pangya.ProcGetTrofelSpecial", "pangya.ProcGetTrofelGrandPrix" };

        public enum TYPE_SEASON : byte
        {
            ALL,
            ONE,
            TWO,
            THREE,
            FOUR,
            CURRENT
        }

        public enum TYPE : byte
        {
            NORMAL,
            GRAND_PRIX
        }

    }
}