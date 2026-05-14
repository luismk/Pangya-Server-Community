using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace PangyaAPI.Network.PangyaSession
{
    public abstract class SessionManager
    {
        public List<Session> m_sessions;
        public readonly object _lock = new object();

        public uint m_max_session;
        private uint m_ttl;
        public uint m_count;
        protected IniHandle m_reader_ini;

        public SessionManager()
        {
            m_max_session = 0;
            // Carrega as config do arquivo server.ini
            config_init();
            m_sessions = new List<Session>((int)m_max_session);
        }


        public void config_init()
        {
            try
            {
                m_reader_ini = new IniHandle("server.ini");
                //read file
                m_max_session = m_reader_ini.ReadUInt32("SERVERINFO", "MAXUSER");
                m_ttl = m_reader_ini.ReadUInt32("OPTION", "TTL", 0);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Procura sessão livre
        private Session FindFreeSession()
        {
            foreach (var s in m_sessions)
            {
                if (s.m_oid == -1) // não está em uso
                    return s;
            }
            return null;
        }

        // ADD SESSION
        public Session AddSession(pangya_packet_handle packetHandle, TcpClient client, IPEndPoint address, byte key)
        {
            if (client == null || !client.Connected)
                throw new Exception("[SessionManager::AddSession] Client inválido ou desconectado.");

            lock (_lock)
            {
                // 1. Buscamos uma sessão que não esteja em uso (State == false)
                var session = FindFreeSession();

                if (session == null)
                {
                    _smp.message_pool.getInstance().push(new message("[SessionManager] Limite de conexões atingido!", 0));
                    return null;
                }

                // 2. Pegamos um OID único para esta nova conexão
                // Use o gerenciador de IDs diretamente aqui para evitar loops extras
                int newOid = findSessionFree();

                if (newOid < 0) // No Pangya, OID 0 costuma ser reservado/inválido
                    throw new Exception("[SessionManager::AddSession] Falha ao gerar OID único.");

                // 3. Resetamos os dados antigos da sessão antes de reusar o objeto
                session.clear();
                session.ClearConnection();
                // 4. Atribuímos a nova identidade de rede
                session._Packet_Handle_Base = packetHandle;
                session.m_client = client;
                session.Stream = client.GetStream(); // Importante para o BinaryReader/Writer
                session.m_addr = address;
                session.m_key = key;
                session.m_oid = newOid;

                // 5. Timestamps (usando TickCount para controle de timeout/ping)
                session.m_time_start = Environment.TickCount;
                session.m_tick = Environment.TickCount;

                // 6. Ativamos a sessão
                session.setState(true);
                session.setConnected(true);

                m_count++;

                return session;
            }
        }
        public virtual int findSessionFree()
        {
            int i = 0;
            foreach (var _session in m_sessions)
            {
                if (_session.m_oid == -1)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        // REMOVE (libera para pool)
        public abstract bool DeleteSession(Session session);

        public List<Session> findAllGM()
        {
            List<Session> v_gm = new List<Session>();

            foreach (Session el in m_sessions)
            {
                if ((el.getCapability() & 4) != 0 || (el.getCapability() & 128) != 0)    // GM
                    v_gm.Add(el);
            }

            return v_gm;
        }

        public List<Session> getAllSessions()
        {
            List<Session> v_gm = new List<Session>();
            foreach (var el in m_sessions.Where(el => el.m_client != null))
            {
                v_gm.Add(el);
            }
            return v_gm;
        }

        public virtual Session FindSessionByOid(uint oid)
        {
            Session session = null;
            foreach (var el in m_sessions.Where(el => el.m_client != null))
            {
                if (el.m_oid == oid)
                    session = el;
            }
            return session;
        }

        public virtual Session findSessionByUID(uint uid)
        {
            Session session = null;
            session = m_sessions.FirstOrDefault(el => el.m_client != null && el.getUID() == uid);
            return session;
        }

        public virtual List<Session> FindAllSessionByUid(uint uid)
        {
            List<Session> sessions = new List<Session>();
            sessions = m_sessions.Where(el => el.m_client != null && el.getUID() == uid).ToList();
            return sessions;
        }

        public virtual Session FindSessionByNickname(string nickname)
        {
            Session session = null;
            session = m_sessions.FirstOrDefault(el => el.m_client != null && el.getNickname() == nickname);
            return session;
        }

        public bool HasSessionWithIP(string ip)
        {
            return m_sessions.Any(s => s.isConnected() && s.getIP() == ip);
        }

        public Session findSessionByIP(string ip)
        {
            return m_sessions.FirstOrDefault(s => s.isConnected() && s.getIP() == ip);
        }

        public List<Session> findAllSessionByIP(string ip)
        {
            return m_sessions.Where(s => s.isConnected() && s.getIP() == ip).ToList();
        }

        public List<Session> GetAllOnline()
        {
            return m_sessions
                .Where(s => s.m_oid != -1)
                .ToList();
        }

        public bool IsFull()
        {
            return m_count >= m_max_session;
        }

        public uint NumSessionConnected()
        { 
            return m_count;
        }
    }
}