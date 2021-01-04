using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace IpcPubSubClient
{
    public class UnixDomainSocketConnectionFactory
    {
        private readonly EndPoint _endPoint;
        private static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "ipc.tmp");


        public UnixDomainSocketConnectionFactory(EndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _,
            CancellationToken cancellationToken = default)
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                await socket.ConnectAsync(_endPoint, cancellationToken);
                return new NetworkStream(socket, true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }


        public static GrpcChannel CreateChannel()
        {
            var udsEndPoint = new UnixDomainSocketEndPoint(SocketPath);
            var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
            var socketsHttpHandler = new SocketsHttpHandler
            {
                ConnectCallback = connectionFactory.ConnectAsync,
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            return GrpcChannel.ForAddress("http://unix:/tmp/ipc.tmp", new GrpcChannelOptions
            {
                HttpHandler = socketsHttpHandler
            });
        }
    }
}
