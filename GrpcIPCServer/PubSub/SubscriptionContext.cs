using System;
using System.Collections.Generic;
using System.Threading.Channels;
using GrpcIPC;

namespace GrpcIPCServer.PubSub
{
    public class SubscriptionContext
    {
        public ChannelWriter<PubSubMessage> Writer{get;set;}

        public ChannelReader<PubSubMessage> Reader { get; set; }

        public List<string> Topics{get; set;} = new();

        public Guid ConnectionId{get;private set;}

        public SubscriptionContext(Guid connectionId)
        {
            this.ConnectionId = connectionId;
            var channel = Channel.CreateBounded<PubSubMessage>(new BoundedChannelOptions(100) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
            this.Reader = channel.Reader;
            this.Writer = channel.Writer;
        }

        public SubscriptionContext(Guid connectionId, string[] topics): this(connectionId)
        {
            this.Topics.AddRange(topics);
        }
    }
}