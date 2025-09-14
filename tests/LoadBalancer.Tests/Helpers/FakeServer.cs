using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadBalancer.Tests.Helpers
{
    public class FakeServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private Task? _acceptLoop;
        public int Port { get; }

        public string Name { get; }

        public FakeServer(string name, int port = 0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public void Start()
        {
            _acceptLoop = Task.Run(AcceptLoopAsync);
        }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* ignore in tests */ }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[4096];
                    int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), _cts.Token).ConfigureAwait(false);
                    if (read <= 0) return;
                    string input = Encoding.UTF8.GetString(buffer, 0, read);
                    string response = $"{Name}:{input}";
                    byte[] outBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(outBytes.AsMemory(0, outBytes.Length), _cts.Token).ConfigureAwait(false);
                    await stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (Exception) { /* ignore in tests */ }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
        }
    }
}
