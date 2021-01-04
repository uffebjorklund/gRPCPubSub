using System;
using System.Collections.Generic;
using System.Threading.Channels;
using IpcPubSub;

namespace IpcPubSubServer
{
    public class SubscriptionContext
    {
        public ChannelWriter<PubSubMessage> Writer{get; private set;}

        public ChannelReader<PubSubMessage> Reader { get; private set; }

        public List<string> Topics{get; set;} = new();

        public Guid ConnectionId{get;private set;}

        public SubscriptionContext(Guid connectionId)
        {
            this.ConnectionId = connectionId;
            var channel = Channel.CreateBounded<PubSubMessage>(new BoundedChannelOptions(1000) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
            this.Reader = channel.Reader;
            this.Writer = channel.Writer;
        }

        public SubscriptionContext(Guid connectionId, string[] topics): this(connectionId)
        {
            this.Topics.AddRange(topics);
        }
    }
}