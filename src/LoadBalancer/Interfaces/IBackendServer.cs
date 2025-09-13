using System.Net;

namespace LoadBalancer.Interfaces;
public interface IBackendServer
{
    string Name { get; }
    IPAddress IPAddress { get; }
    int Port { get; }
    bool IsOnline { get; }


    void SetOnline();
    void SetOffline();
}
