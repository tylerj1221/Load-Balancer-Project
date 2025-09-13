using LoadBalancer.Interfaces;
using System.Collections.ObjectModel;

namespace LoadBalancer;
public class LoadBalancer
{
    private readonly IServerRepository _repository;
    private readonly ILoadBalancingStrategy _strategy;


    public LoadBalancer(IServerRepository repository, ILoadBalancingStrategy strategy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }


    public void AddServer(IBackendServer server) => _repository.Add(server);


    public bool RemoveServer(string name) => _repository.Remove(name);


    public IBackendServer? GetNextServer()
    {
        ReadOnlyCollection<IBackendServer> online = _repository.GetOnlineServers().ToList().AsReadOnly();
        return _strategy.SelectServer(online);
    }


    public IReadOnlyList<IBackendServer> GetAllServers() => _repository.GetAll();
}