using System.Collections.Generic;

namespace Pangya_GameServer.Models
{
    public enum eMSG_MAKE_HOLE : ushort
    {
        MMH_PERDEU,
        MMH_GANHOU = 2,
        MMH_EMPATOU
    }

    public class PangBattleHolePang
    {
        public PangBattleHolePang(uint _pang)
        {
            this.pang = _pang;
            this.pang_extra = 0;
            this.player_win = -3;
            this.vezes = 1;
        }
        public void clear()
        {
            pang = 0;
            pang_extra = 0;
            player_win = -3; // Padrão -3
            vezes = 1;
        }
        public int player_win = new int();
        public uint pang = new uint();
        public uint pang_extra = new uint();
        public byte vezes;
    }

    public class PangBattleData
    {
        public PangBattleData(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {

            m_hole = -1;
            m_hole_extra = -1;
            m_hole_extra_flag = false;
            m_count_finish_hole = 0; m_player_win_pb = -1;
            if (v_player_win.Count > 0)
            {
                v_player_win.Clear();
            }
        }
        public short m_hole;
        public bool m_hole_extra_flag; // Hole Extra, type true está no hole extra, false não
        public short m_hole_extra; // Sequência do hole extra, para pegar no course, para fazer os calculos no Approach
        public uint m_count_finish_hole = new uint(); // Número(Soma) de holes que foram terminados
        public int m_player_win_pb = new int(); // Player que ganhou o Pang Battle
        public List<PangBattleHolePang> v_player_win = new List<PangBattleHolePang>(); // OID do player que ganhou o hole ou -1 se empatou
    }
}
