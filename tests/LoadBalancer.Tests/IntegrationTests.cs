using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LoadBalancer.Tests.Helpers;
using LoadBalancer.Interfaces;
using LoadBalancer.Repositories;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task Integration_EchoServers_ForwardTraffic_RoundRobin()
        {
            using FakeServer s1 = new FakeServer("S1");
            using FakeServer s2 = new FakeServer("S2");
            s1.Start();
            s2.Start();

            ServerRepository repo = new ServerRepository();
            repo.Add(new BackendServer("S1", IPAddress.Loopback, s1.Port));
            repo.Add(new BackendServer("S2", IPAddress.Loopback, s2.Port));
            LoadBalancer lb = new LoadBalancer(repo, new RoundRobinStrategy());

            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int lbPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            int acceptCount = 4;
            Task acceptTask = Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < acceptCount; i++)
                    {
                        TcpClient client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                        _ = Task.Run(() => ProxySingleRequestAsync(client, lb, cts.Token));
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    try { listener.Stop(); } catch { }
                }
            });

            await Task.Delay(100);

            string[] results = new string[acceptCount];
            for (int i = 0; i < acceptCount; i++)
            {
                using TcpClient tcp = new TcpClient();
                await tcp.ConnectAsync(IPAddress.Loopback, lbPort);
                NetworkStream stream = tcp.GetStream();
                string payload = $"hello-{i}";
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                // read reply
                byte[] buffer = new byte[4096];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                results[i] = Encoding.UTF8.GetString(buffer, 0, read);
            }

            await acceptTask;

            Assert.AreEqual(acceptCount, results.Length);
            string[] prefixes = results.Select(r => r.Split(':')[0]).ToArray();
            CollectionAssert.AreEqual(new[] { "S1", "S2", "S1", "S2" }, prefixes);
        }

        private static async Task ProxySingleRequestAsync(TcpClient client, LoadBalancer lb, CancellationToken token)
        {
            using (client)
            {
                try
                {
                    IBackendServer backend = lb.GetNextServer();
                    if (backend == null) return;
                    using TcpClient backendClient = new TcpClient();
                    await backendClient.ConnectAsync(backend.IPAddress, backend.Port);
                    NetworkStream clientStream = client.GetStream();
                    NetworkStream backendStream = backendClient.GetStream();

                    byte[] buffer = new byte[4096];
                    int read = await clientStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read <= 0) return;
                    await backendStream.WriteAsync(buffer, 0, read, token);
                    await backendStream.FlushAsync(token);

                    int r = await backendStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (r <= 0) return;
                    await clientStream.WriteAsync(buffer, 0, r, token);
                    await clientStream.FlushAsync(token);
                }
                catch (Exception) { /* ignore in test */ }
            }
        }
    }
}
