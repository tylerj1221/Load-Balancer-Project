namespace LoadBalancer.Interfaces;
public interface IServerRepository
{
    void Add(IBackendServer server);
    bool Remove(string name);
    IReadOnlyList<IBackendServer> GetAll();
    IEnumerable<IBackendServer> GetOnlineServers();
}
