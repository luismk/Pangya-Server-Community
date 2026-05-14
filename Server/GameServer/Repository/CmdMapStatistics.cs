using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdMapStatistics : Pangya_DB
    {
        private uint m_uid;
        TYPE m_type;
        TYPE_SEASON m_season;
        TYPE_MODO m_modo;
        List<MapStatisticsEx> v_map_statistics;
        public enum TYPE : int
        {
            NORMAL,
            ASSIST,
        }

        public enum TYPE_MODO : int
        {
            M_NORMAL,
            M_NATURAL,
            M_GRAND_PRIX
        }

        public enum TYPE_SEASON : int
        {
            ALL,            // Todas Seasons
            ONE,            // 1
            TWO,            // 2
            THREE,          // 3
            FOUR,           // 4
            CURRENT,        // Atual
        }

        public CmdMapStatistics(uint _uid, TYPE_SEASON _season, TYPE _type, TYPE_MODO _modo)
        {
            v_map_statistics = new List<MapStatisticsEx>();
            m_uid = _uid;
            m_type = _type;
            m_season = _season;
            m_modo = _modo;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(14);
            try
            {
                var ms = new MapStatisticsEx
                {
                    course = _result.GetSByte(2),
                    tacada = _result.GetUInt32(3),
                    putt = _result.GetUInt32(4),
                    hole = _result.GetUInt32(5),
                    fairway = _result.GetUInt32(6),
                    hole_in = _result.GetUInt32(7),
                    putt_in = _result.GetUInt32(8),
                    total_score = _result.GetInt32(9),
                    best_score = _result.GetSByte(10),
                    best_pang = _result.GetUInt64(11),
                    character_typeid = _result.GetUInt32(12),
                    event_score = _result.GetByte(13)
                };

                v_map_statistics.Add(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            // 1. Calculate the specific season/mode identifier
            uint seasonValue = (m_season == TYPE_SEASON.ALL)
                ? 9 + (uint)m_modo
                : (m_modo == TYPE_MODO.M_NORMAL ? (uint)m_season : (uint)m_season * 10 + (uint)m_modo);

            // 2. Pick the procedure name
            string procName = (m_type == TYPE.NORMAL)
                ? "pangya.ProcGetMapStatistics"
                : "pangya.ProcGetMapStatisticsAssist";

            // 3. Execute with the calculated seasonValue 
            var r = procedure(procName, $"{m_uid}, {seasonValue}");

            checkResponse(r, $"Não conseguiu pegar as estatísticas do mapa para o player: {m_uid}");

            return r;
        }

        public List<MapStatisticsEx> getMapStatistics()
        {
            return v_map_statistics;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public TYPE_SEASON getSeason()
        {
            return m_season;
        }

        public void setSeason(TYPE_SEASON _type)
        {
            m_season = _type;
        }

        public TYPE_MODO getModo()
        {
            return m_modo;
        }

        public void setModo(TYPE_MODO _type)
        {
            m_modo = _type;
        }

    }
}