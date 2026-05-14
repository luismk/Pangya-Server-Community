using System;
using System.Collections.Generic;
using System.Linq;

using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer
{
    public class Guild
    {
        public enum eTEAM : byte
        {
            RED,
            BLUE
        }

        public Guild(int _ul = 0)
        {
            this.v_players = new List<Player>();
            this.m_team = eTEAM.RED;
            this.m_uid = 0;
            this.m_point = 0;
            this.m_pang = 0Ul;
            this.m_pang_win = 0;
        }

        public Guild(int _uid, eTEAM _team)
        {
            this.v_players = new List<Player>();
            this.m_team = _team;
            this.m_uid = (uint)_uid;
            this.m_point = 0;
            this.m_pang = 0Ul;
            this.m_pang_win = 0;
        }

        public void Dispose()
        {
            clear();
        }

        public void clear()
        {

            if (v_players.Any())
            {
                v_players.Clear();
            }

            m_team = eTEAM.RED;
            m_uid = 0;
            m_point = 0;
            m_pang = 0Ul;
            m_pang_win = 0;
        }

        public Guild.eTEAM getTeam()
        {
            return m_team;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public int getPoint()
        {
            return m_point;
        }

        public uint getPangWin()
        {
            return m_pang_win;
        }

        public ulong getPang()
        {
            return m_pang;
        }

        public void setTeam(eTEAM _team)
        {
            m_team = _team;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public void setPoint(int _point)
        {
            m_point = _point;
        }

        public void setPangWin(uint _pang_win)
        {
            m_pang_win = _pang_win;
        }

        public void setPang(ulong _pang)
        {
            m_pang = _pang;
        }

        public void addPlayer(Player _session)
        {

            if (findPlayerByUID(_session.m_pi.uid) != null)
            {

                // Log
                _smp.message_pool.getInstance().push(new message("[Guild::addPlayer][Warning] tentou adicionar o PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] na guild, mas ele ja existe na guild. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }


            v_players.Add(_session);
        }

        public void deletePlayer(Player _session)
        {

            if (_session == null)
            {

                _smp.message_pool.getInstance().push(new message("[Guild::deletePlayer][Error] _session is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            var it = v_players.FirstOrDefault(_el =>
            {
                return _el.m_pi.uid == _session.m_pi.uid;
            });

            if (it != null)  // deleta o player do map
            {
                v_players.Remove(it);
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[Guild::deletePlayer][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] ja foi deletado do vector. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public Player findPlayerByOID(int _oid)
        {
            var it = v_players.FirstOrDefault(_el =>
            {
                return _el.m_oid == _oid;
            });

            return it;
        }

        public Player findPlayerByUID(uint _uid)
        {
            var it = v_players.FirstOrDefault(_el =>
            {
                return _el.m_pi.uid == _uid;
            });

            return it;
        }

        public Player findPlayerByNickname(string _nickname)
        {

            var it = v_players.FirstOrDefault(_el =>
            {
                return string.CompareOrdinal(_nickname, _el.m_pi.nickname) == 0;
            });

            return it;
        }

        public Player getPlayerByIndex(uint _index)
        {

            if (_index > v_players.Count())
            {

                _smp.message_pool.getInstance().push(new message("[Guild::getPlayerByIndex][Error] index[VALUE=" + Convert.ToString(_index) + "] is invalid(out_of_bounds)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return null;
            }

            Player p = null;



            p = v_players[(int)_index];


            return p;
        }

        public uint numPlayers()
        {
            return (uint)v_players.Count();
        }

        private eTEAM m_team = new eTEAM(); // Time que a guild está na sala
        private uint m_uid = new uint(); // UID da guild
        private int m_point; // Pontos da guild
        private uint m_pang_win = new uint(); // Pangs ganho no jogo
        private ulong m_pang = new ulong(); // Pangs da guild

        private List<Player> v_players = new List<Player>(); // Players 
    }
}
