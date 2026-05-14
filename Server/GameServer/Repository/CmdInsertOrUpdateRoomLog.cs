using System;
using System.Data;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertOrUpdateRoomLog : Pangya_DB
    {
        RoomInfoLog m_log;
        TYPE m_type;
        int m_state;
        public CmdInsertOrUpdateRoomLog(RoomInfoLog _log, TYPE _type = TYPE.INSERT, bool _waiter = false) : base(_waiter)
        {
            m_log = _log;
            m_type = _type;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);

            switch (m_type)
            {
                case TYPE.INSERT:
                    // Verifica se a coluna possui dados válidos
                    if (is_valid_c_string(_result.data[0]))
                    {
                        var guid_cstr = (_result.GetString(0)).ToUpper();

                        guid_cstr.Replace("{", "");
                        guid_cstr.Replace("}", "");
                    }
                    break;
                case TYPE.UPDATE:
                    {
                        m_state = (int)IFNULL(_result.data[0]);
                    }
                    break;
                default:
                    break;
            }
        }

        public RoomInfoLog getRoom()
        {
            return m_log;
        }

        public void setRoomLog(RoomInfoLog _log)
        {
            m_log = _log;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public int getState()
        {
            return m_state;
        }

        protected override Response prepareConsulta()
        {
            var query = m_szConsulta[(int)m_type];
            //para adicionar salas com string em japones!
            var r = procedure(query, makeText(m_log.name) + ", " +
            m_log.num_player + ", " +
            m_log.max_player + ", " +
            m_log.type_extend + ", " +
            m_log.uid + ", " +
            makeText(m_log.roomId.ToString()) + ", " + // deu erro -> Conversão inválida de 'System.String' em 'System.Guid'.
            m_log.character + ", " +
            m_log.caddie + ", " +
            m_log.mascot + ", " +
            m_log.club + ", " +
            m_log.tipo + ", " +
            m_log.modo + ", " +
            m_log.qntd_hole + ", " +
            Convert.ToInt32(m_log.course) + ", " +
            (m_log.hole == 0 ? 1 : m_log.hole) + ", " + //o primeiro hole é zero né
            m_log.score + ", " +
            m_log.exp + ", " +
            m_log.pang + ", " +
            m_log.bonus_pang + ", " +
            m_log.tacada_num + ", " +
            m_log.total_tacada_num + ", " +
            m_log.giveup + ", " +
            m_log.timeout + ", " +
           Convert.ToInt32(m_log.enter_after_started) + ", " +
           Convert.ToInt32(m_log.finish_game) + ", " +
            m_log.assist_flag + ", " +
            m_log.Win_trofeu + ", " +
            m_log.master + ", " +
            Convert.ToInt32(m_log.Is_short_game) + ", " +
            Convert.ToInt32(m_log.Is_natural) + ", " +
            m_log.HitHio + ", " +
            m_log.HitAlba + ", " +
            m_log.HitEagle + ", " +
            m_log.HitBirdie + ", " +
            m_log.HitPar + ", " +
            m_log.HitBogey + ", " +
            m_log.Hit_x2_Bogey + ", " +
            m_log.Hit_x3_Bogey);

            checkResponse(r, $"Não foi possível fazer {m_type} log game!");

            return r;
        }


        public enum TYPE
        {
            INSERT,
            UPDATE,
        }

        string[] m_szConsulta = { "pangya.ProcInsertRoomLog", "pangya.ProcUpdateRoomLog" };
    }
}
