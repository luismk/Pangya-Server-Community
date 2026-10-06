using PangyaAPI.Network.Core;
using PangyaAPI.Network.Service.Auth;
using PangyaAPI.Network.Session;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Numerics;

public abstract class AppSessionManager<T> : IAppSessionManager where T : class, IAppSession
{
    // Usamos T diretamente para evitar cast toda vez que buscamos
    protected ConcurrentDictionary<int, T> _sessions = new();
    public int _idCounter;

    public int Count => _sessions.Count;

    public int MaxUsers { get; private set; }

    public AppSessionManager(int maxUsers)
    {
        MaxUsers = maxUsers;
        //   var Login = -1;
        //for (int i = 0; i < MaxUsers; i++)//mais rapido.
        //    _sessions.TryAdd(Login, (T)Activator.CreateInstance(typeof(T), null, -1));
    }

    public IAppSession Add(IAppServer server, Socket socket)
    {
        var id = Interlocked.Increment(ref _idCounter);
        var session = (T)Activator.CreateInstance(typeof(T), server, socket, id);

        session.TimeStart = Environment.TickCount;
        session.Tick = Environment.TickCount;
        _sessions[id] = session;

        return session;
    } 


    public void Remove(IAppSession session)
    {
        if (session == null) return;

        _sessions.TryRemove(session.ConnectionID, out _);
    }

    public IAppSession Get(int id)
    {
        _sessions.TryGetValue(id, out var session);
        return session;
    }

    public IReadOnlyCollection<IAppSession> GetAll()
        => (IReadOnlyCollection<IAppSession>)_sessions.Values;

    // Remova o <TResult> do Name do método, use o T da classe
    public List<T> GetAllSessions()
    {
        return _sessions.Values
            .Where(s => s.Connected) // Agora o ponto (.) funciona!
            .ToList();
    }

    public List<T> FindAllGM()
    {
        return _sessions.Values
            .Where(s => s.Connected && ((s.GetCapability() & 4) != 0 || (s.GetCapability() & 128) != 0))
            .ToList();
    }

    public List<T> FindAllSessionByUid(uint uid)
    {
        return _sessions.Values
            .OfType<T>()
            .Where(s => s.Connected && s.GetUID() == uid)
            .ToList();
    }


    public T FindSessionByOid(int oid)
    {
        return _sessions.Values
            .OfType<T>()
            .FirstOrDefault(s => s.Connected && s.ConnectionID == oid);
    }

    public T FindSessionByUID(uint uid)
    {
        return _sessions.Values
            .OfType<T>()
            .FirstOrDefault(s => s.Connected && s.GetUID() == uid);
    }

    public T FindSessionByNickname(string nickname)
    {
        return _sessions.Values
            .OfType<T>()
            .FirstOrDefault(s => s.Connected && s.GetNickname() == nickname);
    }

    public bool HasSessionWithIP(string ip)
    {
        return _sessions.Values
            .OfType<IAppSession>()
            .Any(s => s.Connected && s.GetIP() == ip);
    }

    public bool IsFull() => Count >= MaxUsers;

    public int CurrUsers() => Count;

    //public virtual int FindSessionFree()//
    //{
    //    int i = 0;
    //    foreach (var _session in _sessions.Values)
    //    {
    //        if (_session.ConnectionID == -1)
    //        {
    //            return i;
    //        }
    //        i++;
    //    }
    //    return -1;
    //}

    //public abstract bool DeleteSession(T session);
}