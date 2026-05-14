using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;  
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateUserInfo : Pangya_DB
    {
        public CmdUpdateUserInfo(uint _uid, UserInfoEx _ui)
        {
            this.m_uid = _uid;
            this.m_ui = _ui;
        }
         
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid <= 0 || m_uid == uint.MaxValue)
            {
                throw new exception("[CmdUpdateUserInfo::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }


            var r = procedure(m_szConsulta, (m_uid) + ", " + ToString(m_ui.best_drive) + ", " + ToString(m_ui.best_long_putt) + ", " + ToString(m_ui.best_chip_in) + ", " + ToString(m_ui.combo) + ", " + ToString(m_ui.all_combo) + ", " + ToString(m_ui.tacada) + ", " + ToString(m_ui.putt) + ", " + ToString(m_ui.tempo) + ", " + ToString(m_ui.tempo_tacada) + ", " + ToString(m_ui.acerto_pangya) + ", " + ToString(m_ui.timeout) + ", " + ToString(m_ui.ob) + ", " + ToString(m_ui.total_distancia) + ", " + ToString(m_ui.hole) + ", " + ToString(m_ui.hole_in) + ", " + ToString(m_ui.hio) + ", " + ToString(m_ui.bunker) + ", " + ToString(m_ui.fairway) + ", " + ToString(m_ui.albatross) + ", " + ToString(m_ui.mad_conduta) + ", " + ToString(m_ui.putt_in) + ", " + ToString(m_ui.media_score) + ", " + ToString(m_ui.best_score[0]) + ", " + ToString(m_ui.best_score[1]) + ", " + ToString(m_ui.best_score[2]) + ", " + ToString(m_ui.best_score[3]) + ", " + ToString(m_ui.best_score[4]) + ", " + ToString(m_ui.best_pang[0]) + ", " + ToString(m_ui.best_pang[1]) + ", " + ToString(m_ui.best_pang[2]) + ", " + ToString(m_ui.best_pang[3]) + ", " + ToString(m_ui.best_pang[4]) + ", " + ToString(m_ui.sum_pang) + ", " + ToString(m_ui.event_flag) + ", " + ToString(m_ui.jogado) + ", " + ToString(m_ui.team_game) + ", " + ToString(m_ui.team_win) + ", " + ToString(m_ui.team_hole) + ", " + ToString(m_ui.ladder_point) + ", " + ToString(m_ui.ladder_hole) + ", " + ToString(m_ui.ladder_win) + ", " + ToString(m_ui.ladder_lose) + ", " + ToString(m_ui.ladder_draw) + ", " + ToString(m_ui.quitado) + ", " + ToString(m_ui.skin_pang) + ", " + ToString(m_ui.skin_win) + ", " + ToString(m_ui.skin_lose) + ", " + ToString(m_ui.skin_run_hole) + ", " + ToString(m_ui.skin_all_in_count) + ", " + ToString(m_ui.disconnect) + ", " + ToString(m_ui.jogados_disconnect) + ", " + ToString(m_ui.event_value) + ", " + ToString(m_ui.skin_strike_point) + ", " + ToString(m_ui.sys_school_serie) + ", " + ToString(m_ui.game_count_season) + ", " + ToString(m_ui.total_pang_win_game) + ", " + ToString(m_ui.medal.lucky) + ", " + ToString(m_ui.medal.fast) + ", " + ToString(m_ui.medal.best_drive) + ", " + ToString(m_ui.medal.best_chipin) + ", " + ToString(m_ui.medal.best_puttin) + ", " + ToString(m_ui.medal.best_recovery) + ", " + ToString(m_ui._16bit_nao_sei));

            checkResponse(r, "nao conseguiu atualizar o User Info do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private UserInfoEx m_ui = new UserInfoEx();

        private const string m_szConsulta = "pangya.ProcUpdateUserInfo";
    }
}
