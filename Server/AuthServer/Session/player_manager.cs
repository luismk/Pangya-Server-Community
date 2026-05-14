using System.Collections.Generic;
using System.Linq;
using PangyaAPI.Network.PangyaSession; 
namespace Pangya_AuthServer.Session
{
    public class player_manager : SessionManager
    {
        public player_manager()
        { 
            for (int i = 0; i < m_max_session; i++)
            {
                var s = new Player
                {
                    m_oid = -1
                };
                s.setState(false); // livre
                m_sessions.Add(s);
            }
        }

        public List<Player> getAllPlayer()
        {
            return m_sessions
                .OfType<Player>() 
                .Where(p => p.isConnected())
                .ToList();
        }

        public Player findPlayer(uint _uid, bool _oid = false)
        {

            Player _Player = null;

            foreach (var el in m_sessions)
            {
                if (((!_oid) ? el.getUID() : (uint)el.m_oid) == _uid)
                {
                    _Player = (Player)el;
                    break;
                }
            }

            return _Player;
        }

        public List<Player> findPlayerByType(uint _type)
        {

            List<Player> v_p = new List<Player>();
            foreach (var el in m_sessions)
            {
                if (el != null && el.getCapability() == _type)
                {
                    v_p.Add((Player)el);
                }
            }

            return new List<Player>(v_p);
        }
        public List<Player> findPlayerByTypeExcludeUID(uint _type, uint _uid)
        {

            List<Player> v_p = new List<Player>();

            foreach (var el in m_sessions)
            {
                if (el != null
                    && el.getCapability() == _type
                    && el.getUID() != _uid)
                {
                    v_p.Add((Player)el);
                }
            }
            return new List<Player>(v_p);
        }

        public override bool DeleteSession(PangyaAPI.Network.PangyaSession.Session _session)
        {
            if (_session == null) return false;

            lock (_lock) // Importante: Use um lock aqui também!
            {
                int tmp_oid = _session.m_oid;

                // Só processa se o OID for válido (maior que 0 no Pangya)
                if (tmp_oid != -1)
                {
                    if (_session.clear())
                    { 

                        // 3. Reseta o OID da sessão para evitar reuso acidental
                        _session.m_oid = -1;

                        // 1. Fecha o Socket e limpa buffers de rede
                        _session.ClearConnection();

                        // 2. Reseta flags de estado
                        _session.setConnected(false);
                        _session.setState(false);
                        // 3. deleta da memoria
                        m_sessions[tmp_oid] = _session;

                        if (m_count > 0) m_count--;

                        return true;
                    }
                }
            }
            return false;
        }
    }
}