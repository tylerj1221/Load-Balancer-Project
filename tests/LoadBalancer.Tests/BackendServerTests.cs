using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace LoadBalancer.Tests;

[TestClass]
public class BackendServerTests
{
    [TestMethod]
    public void BackendServer_OnlineOffline_TogglesCorrectly()
    {
        BackendServer s = new BackendServer("S1", IPAddress.Parse("127.0.0.1"), 8080);
        Assert.IsTrue(s.IsOnline);
        s.SetOffline();
        Assert.IsFalse(s.IsOnline);
        s.SetOnline();
        Assert.IsTrue(s.IsOnline);
    }
}