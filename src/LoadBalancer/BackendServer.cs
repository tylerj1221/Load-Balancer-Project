using LoadBalancer.Interfaces;
using System.Net;

namespace LoadBalancer;
public class BackendServer : IBackendServer
{
    public string Name { get; }
    public IPAddress IPAddress { get; }
    public int Port { get; }
    public bool IsOnline { get; private set; }


    public BackendServer(string name, IPAddress ipAddress, int port)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
        IsOnline = true;
    }


    public void SetOnline()
    {
        IsOnline = true;
    }


    public void SetOffline()
    {
        IsOnline = false;
    }


    public override string ToString()
    {
        return $"{Name} ({IPAddress}:{Port}) - {(IsOnline ? "Online" : "Offline")}";
    }
}