using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Linq;
using LoadBalancer.Interfaces;
using LoadBalancer.Repositories;

namespace LoadBalancer.Tests;

[TestClass]
public class LoadBalancerTests
{
    [TestMethod]
    public void LoadBalancer_AddRemove_GetNextServer_WorksWithOfflineServers()
    {
        ServerRepository repo = new ServerRepository();
        RoundRobinStrategy strategy = new RoundRobinStrategy();
        LoadBalancer lb = new LoadBalancer(repo, strategy);


        BackendServer s1 = new BackendServer("S1", IPAddress.Parse("127.0.0.1"), 80);
        BackendServer s2 = new BackendServer("S2", IPAddress.Parse("127.0.0.2"), 80);
        BackendServer s3 = new BackendServer("S3", IPAddress.Parse("127.0.0.3"), 80);


        lb.AddServer(s1);
        lb.AddServer(s2);
        lb.AddServer(s3);


        // s2 goes offline
        s2.SetOffline();


        // Only S1 and S3 should be selected in round-robin
        IBackendServer[] picks = Enumerable.Range(0, 4).Select(_ => lb.GetNextServer()!).ToArray();


        Assert.IsTrue(picks.All(p => p.Name == "S1" || p.Name == "S3"));
    }


    [TestMethod]
    public void LoadBalancer_RemoveServer_RemovesByName()
    {
        ServerRepository repo = new ServerRepository();
        RoundRobinStrategy strategy = new RoundRobinStrategy();
        LoadBalancer lb = new LoadBalancer(repo, strategy);


        BackendServer s1 = new BackendServer("S1", IPAddress.Parse("127.0.0.1"), 80);
        lb.AddServer(s1);
        Assert.AreEqual(1, lb.GetAllServers().Count);


        bool removed = lb.RemoveServer("S1");
        Assert.IsTrue(removed);
        Assert.AreEqual(0, lb.GetAllServers().Count);
    }
}