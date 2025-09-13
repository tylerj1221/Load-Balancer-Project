using LoadBalancer.Interfaces;
using LoadBalancer.Repositories;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LoadBalancer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ServerRepository repository = new ServerRepository();
        repository.Add(new BackendServer("S1", IPAddress.Parse("127.0.0.1"), 5001));
        repository.Add(new BackendServer("S2", IPAddress.Parse("127.0.0.1"), 5002));
        repository.Add(new BackendServer("S3", IPAddress.Parse("127.0.0.1"), 5003));

        RoundRobinStrategy strategy = new RoundRobinStrategy();
        LoadBalancer loadBalancer = new LoadBalancer(repository, strategy);

        CancellationTokenSource tokenSource = new CancellationTokenSource();
        _ = Task.Run(() => HealthCheckLoop(repository, TimeSpan.FromSeconds(2), tokenSource.Token));

        int listeningPort = 9000;
        TcpListener listener = new TcpListener(IPAddress.Any, listeningPort);
        listener.Start();
        Console.WriteLine($"Load balancer listening on port {listeningPort}");

        try
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(tokenSource.Token);
                _ = HandleClientAsync(client, loadBalancer, tokenSource.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
            tokenSource.Cancel();
        }
    }

    private static async Task HandleClientAsync(TcpClient client, LoadBalancer loadBalancer, CancellationToken token)
    {
        using (client)
        {
            IBackendServer? backend = loadBalancer.GetNextServer();
            if (backend == null)
            {
                try
                {
                    client.GetStream().Write(System.Text.Encoding.UTF8.GetBytes("No backends available\r\n"));
                }
                catch { }
                return;
            }

            using TcpClient backendClient = new TcpClient();
            try
            {
                Task connectTask = backendClient.ConnectAsync(backend.IPAddress, backend.Port);
                Task timeout = Task.Delay(TimeSpan.FromSeconds(5), token);
                Task finished = await Task.WhenAny(connectTask, timeout);
                if (finished != connectTask) throw new TimeoutException("Timeout connecting to backend");
            }
            catch (Exception)
            {
                backend.SetOffline();
                return;
            }

            NetworkStream clientStream = client.GetStream();
            NetworkStream backendStream = backendClient.GetStream();

            Task t1 = Relay(clientStream, backendStream, token);
            Task t2 = Relay(backendStream, clientStream, token);

            await Task.WhenAny(t1, t2);
        }
    }

    private static async Task Relay(NetworkStream from, NetworkStream to, CancellationToken token)
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (!token.IsCancellationRequested)
            {
                int read = await from.ReadAsync(buffer, 0, buffer.Length, token);
                if (read == 0) break;
                await to.WriteAsync(buffer, 0, read, token);
                await to.FlushAsync(token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }

    private static async Task HealthCheckLoop(IServerRepository repo, TimeSpan period, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            IReadOnlyList<IBackendServer> servers = repo.GetAll();
            foreach (IBackendServer s in servers)
            {
                using TcpClient test = new TcpClient();
                try
                {
                    Task connectTask = test.ConnectAsync(s.IPAddress, s.Port);
                    Task finished = await Task.WhenAny(connectTask, Task.Delay(1000, token));
                    if (finished == connectTask && test.Connected)
                    {
                        s.SetOnline();
                    }
                    else
                    {
                        s.SetOffline();
                    }
                }
                catch
                {
                    s.SetOffline();
                }
            }
            await Task.Delay(period, token);
        }
    }
}
