namespace LoadBalancer.Interfaces;
public interface ILoadBalancingStrategy
{
    IBackendServer? SelectServer(IReadOnlyList<IBackendServer> servers);
}