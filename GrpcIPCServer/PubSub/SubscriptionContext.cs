using System;
using System.Collections.Generic;
using Grpc.Core;
using GrpcIPC;

namespace GrpcIPCServer.PubSub
{
    public class SubscriptionContext
    {
        public IServerStreamWriter<PubSubMessage> streamWriter{get;set;}

        public List<string> Topics{get; set;} = new();

        public Guid ConnectionId{get;private set;}

        public SubscriptionContext(Guid connectionId)
        {
            this.ConnectionId = connectionId;
        }

        public SubscriptionContext(Guid connectionId, string[] topics)
        {
            this.ConnectionId = connectionId;
            this.Topics.AddRange(topics);
        }
    }
}