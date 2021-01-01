using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcIPC;

namespace GrpcIPCClient
{

    class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = CreateChannel();
            var client = new PubSub.PubSubClient(channel);
            var subReq = new SubscribeRequest();
            var connId = Guid.NewGuid().ToString("N");
            subReq.ConnectionId = connId;

            foreach(var topic in args)
            {
                subReq.Topics.Add(topic);
            }

            var subResponse = await client.SubscribeAsync(subReq);
            Console.WriteLine("SubResponse: success = " + subResponse.Success);


            var _ = Task.Run(async ()=>{
                try
                {
                    using var call = client.StartReceiveStream(new AddStreamRequest{ConnectionId = connId});
                    await foreach (var response in call.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine("Topic:   " + response.Topic);
                        Console.WriteLine("Message: " + response.Message);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ABORTED CONNECTION" + ex.Message);
                }
            });


            while(true)
            {
                try
                {
                    var m1 = new PubSubMessage();
                    m1.ConnectionId = connId;
                    m1.Topic = "foo/bar";
                    m1.Message = $"Hello foo/bar from {connId}";
                    await client.PublishAsync(m1);

                    var m2 = new PubSubMessage();
                    m2.ConnectionId = connId;
                    m2.Topic = "bar/foo";
                    m2.Message = $"Hello bar/foo from {connId}";
                    await client.PublishAsync(m2);

                    await Task.Delay(5000);
                }
                catch{}
            }
        }

        private static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "ipc.tmp");

        private static GrpcChannel CreateChannel()
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
