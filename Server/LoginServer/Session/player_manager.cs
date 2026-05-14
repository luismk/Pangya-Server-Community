using System.Collections.Generic;
using PangyaAPI.Network.PangyaSession;
using Pangya_LoginServer.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;

namespace Pangya_LoginServer.Session
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

        public Player findPlayer(uint? _uid, bool _oid = true)
        {

            foreach (var el in m_sessions)
            {
                if ((_oid ? el.getUID() : (uint)el.m_oid) == _uid)
                {
                    return (Player)el;
                }
            }


            return null;
        }

        public Player FindPlayer(uint uid, bool oid)
        {
            Player p = null;
            foreach (var el in m_sessions)
            {
                if (el.m_client != null && ((!oid) ? el.getUID() : (uint)el.m_oid) == uid)
                {
                    p = (Player)el;
                    break;
                }
            }

            return p;
        }

        public List<Player> FindAllGM()
        {
            var gmList = new List<Player>();

            foreach (var el in m_sessions)
            {
                if (el.m_client != null && ((el.getCapability() & 4) != 0 || (el.getCapability() & 128) != 0))
                {
                    gmList.Add((Player)el);
                }
            }

            return gmList;
        }
         
    }
}