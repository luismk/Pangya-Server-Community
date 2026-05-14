using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUserInfo : Pangya_DB
    {
        readonly uint m_uid = uint.MaxValue;
        UserInfoEx m_ui = new UserInfoEx();
        public CmdUserInfo(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(68);
            try
            {
                var i = 0;
                m_ui.tacada = _result.GetInt32(1);
                m_ui.putt = _result.GetInt32(2);
                m_ui.tempo = _result.GetInt32(3);
                m_ui.tempo_tacada = _result.GetInt32(4);
                m_ui.best_drive = _result.GetFloat(5);
                m_ui.acerto_pangya = _result.GetInt32(6);
                m_ui.bunker = _result.GetInt16(7);
                m_ui.ob = _result.GetInt32(8);
                m_ui.total_distancia = _result.GetInt32(9);
                m_ui.hole = _result.GetInt32(10);
                m_ui.hole_in = _result.GetInt32(11);
                m_ui.hio = _result.GetInt32(12);
                m_ui.timeout = _result.GetInt32(13);
                m_ui.fairway = _result.GetInt32(14);
                m_ui.albatross = _result.GetInt32(15);
                m_ui.mad_conduta = _result.GetInt32(16);
                m_ui.putt_in = _result.GetInt32(17);
                m_ui.best_long_putt = _result.GetFloat(18);
                m_ui.best_chip_in = _result.GetFloat(19);
                m_ui.exp = _result.GetInt32(20);
                m_ui.level = _result.GetByte(21);
                m_ui.pang = _result.GetUInt64(22);
                m_ui.media_score = _result.GetInt32(23);
                for (i = 0; i < 5; i++)
                    m_ui.best_score[i] = _result.GetSByte(24 + i);    // 24 + 5
                for (i = 0; i < 5; i++)
                    m_ui.best_pang[i] = _result.GetInt64(29 + i);           // 29 + 5
                m_ui.sum_pang = _result.GetInt64(34);
                m_ui.event_flag = _result.GetByte(35);
                m_ui.jogado = _result.GetInt32(36);
                m_ui.quitado = _result.GetInt32(37);
                m_ui.skin_pang = _result.GetInt64(38);
                m_ui.skin_win = _result.GetInt32(39);
                m_ui.skin_lose = _result.GetInt32(40);
                m_ui.skin_run_hole = _result.GetInt32(41);
                m_ui.skin_strike_point = _result.GetInt32(42);
                m_ui.skin_all_in_count = _result.GetInt16(43);
                m_ui.all_combo = _result.GetInt32(44);
                m_ui.combo = _result.GetInt32(45);
                m_ui.team_win = _result.GetInt32(46);
                m_ui.team_game = _result.GetInt32(47);
                m_ui.team_hole = _result.GetInt32(48);
                m_ui.ladder_point = _result.GetInt32(49);
                m_ui.ladder_win = _result.GetInt32(50);
                m_ui.ladder_lose = _result.GetInt32(51);
                m_ui.ladder_draw = _result.GetInt32(52);
                m_ui.ladder_hole = _result.GetInt32(53);
                m_ui.event_value = (short)_result.GetInt32(54);
                m_ui.disconnect = _result.GetInt32(55);
                m_ui.sys_school_serie = _result.GetInt32(56);
                m_ui.jogados_disconnect = _result.GetInt32(57);//jogos nao sei
                m_ui.game_count_season = _result.GetInt32(58);//
                m_ui.medal.lucky = _result.GetByte(60);
                m_ui.medal.fast = _result.GetByte(61);
                m_ui.medal.best_drive = _result.GetByte(62);
                m_ui.medal.best_chipin = _result.GetByte(63);
                m_ui.medal.best_puttin = _result.GetByte(64);
                m_ui.medal.best_recovery = _result.GetByte(65);
                m_ui._16bit_nao_sei = (short)_result.GetInt16(66);
                m_ui.total_pang_win_game = _result.GetUInt64(67);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.GetInfo_User", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }
        public UserInfoEx getInfo()
        {
            return m_ui;
        }
    }
}
