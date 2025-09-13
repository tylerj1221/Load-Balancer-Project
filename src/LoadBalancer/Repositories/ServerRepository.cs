using LoadBalancer.Interfaces;

namespace LoadBalancer.Repositories;
public class ServerRepository : IServerRepository
{
    private readonly List<IBackendServer> _servers = new();
    private readonly object _lock = new();


    public void Add(IBackendServer server)
    {
        if (server == null) throw new ArgumentNullException(nameof(server));
        lock (_lock)
        {
            if (_servers.Any(s => s.Name == server.Name))
                throw new InvalidOperationException($"Server with name '{server.Name}' already exists.");


            _servers.Add(server);
        }
    }


    public bool Remove(string name)
    {
        lock (_lock)
        {
            IBackendServer? existing = _servers.FirstOrDefault(s => s.Name == name);
            if (existing is null) return false;
            return _servers.Remove(existing);
        }
    }


    public IReadOnlyList<IBackendServer> GetAll()
    {
        lock (_lock)
        {
            return _servers.ToList().AsReadOnly();
        }
    }


    public IEnumerable<IBackendServer> GetOnlineServers()
    {
        lock (_lock)
        {
            return _servers.Where(s => s.IsOnline).ToList();
        }
    }
}
