using LoadBalancer.Interfaces;

namespace LoadBalancer;
public class RoundRobinStrategy : ILoadBalancingStrategy
{
    private int _lastIndex = -1;
    private readonly object _lock = new();


    public IBackendServer? SelectServer(IReadOnlyList<IBackendServer> servers)
    {
        if (servers == null || servers.Count == 0) return null;


        lock (_lock)
        {
            _lastIndex = (_lastIndex + 1) % servers.Count;
            return servers[_lastIndex];
        }
    }
}