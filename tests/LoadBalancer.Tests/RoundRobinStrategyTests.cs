using LoadBalancer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;

namespace LoadBalancer.Tests;

[TestClass]
public class RoundRobinStrategyTests
{
    [TestMethod]
    public void RoundRobin_SelectsInOrder_WhenServersAreOnline()
    {
        IBackendServer[] servers = new IBackendServer[] {
            new BackendServer("S1", IPAddress.Parse("127.0.0.1"), 80),
            new BackendServer("S2", IPAddress.Parse("127.0.0.2"), 80),
            new BackendServer("S3", IPAddress.Parse("127.0.0.3"), 80),
        };


        RoundRobinStrategy strategy = new RoundRobinStrategy();


        IBackendServer[] selected = Enumerable.Range(0, 6)
            .Select(_ => strategy.SelectServer(servers.ToList().AsReadOnly()))
            .ToArray();


        Assert.AreEqual("S1", selected[0]!.Name);
        Assert.AreEqual("S2", selected[1]!.Name);
        Assert.AreEqual("S3", selected[2]!.Name);
        Assert.AreEqual("S1", selected[3]!.Name);
        Assert.AreEqual("S2", selected[4]!.Name);
        Assert.AreEqual("S3", selected[5]!.Name);
    }


    [TestMethod]
    public void RoundRobin_ReturnsNull_WhenNoServers()
    {
        RoundRobinStrategy strategy = new RoundRobinStrategy();
        IBackendServer selected = strategy.SelectServer(new System.Collections.Generic.List<IBackendServer>().AsReadOnly());
        Assert.IsNull(selected);
    }
}