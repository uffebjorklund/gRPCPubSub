﻿using System;
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

            subReq.Topics.Add("foo/bar");
            subReq.Topics.Add("foo/bar/baz");
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
                    var m = new PubSubMessage();
                    m.ConnectionId = connId;
                    m.Topic = "foo/bar";
                    m.Message = "Hello foo/bar";
                    await client.PublishAsync(m);

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
