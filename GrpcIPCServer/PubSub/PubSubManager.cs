using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcIPC;

namespace GrpcIPCServer.PubSub
{
    public class PubSubManager
    {
        private readonly ConcurrentDictionary<Guid, SubscriptionContext> Subscriptions = new ();

        public async Task Publish(string topic, string message)
        {
            // TODO: if topic matches connection id send directly to that connection.
            var msg = new PubSubMessage{Topic = topic, Message = message};
            foreach(var writer in this.GeSubscriptions(topic))
            {
                // TODO: Handle exceptions...
                await writer.WriteAsync(msg);
            }
        }

        public bool Subscribe(string connectionId, params string[] topics)
        {
            if(Guid.TryParse(connectionId, out Guid id) is false)
            {
                return false;
            }
            if(this.Subscriptions.ContainsKey(id) is false)
            {
                return this.Subscriptions.TryAdd(id, new SubscriptionContext(id, topics));
            }
            // TODO: do not add topics if they already exist
            this.Subscriptions[id].Topics.AddRange(topics);
            return true;
        }

        public void Remove(string connectionId)
        {
            this.Subscriptions.TryRemove(Guid.Parse(connectionId), out SubscriptionContext value);
        }

        public ChannelReader<PubSubMessage> AddStream(string connectionId)
        {
            if (Guid.TryParse(connectionId, out Guid id) is false)
            {
                return null;
            }
            if (this.Subscriptions.ContainsKey(id) is false)
            {
                if(this.Subscriptions.TryAdd(id, new SubscriptionContext(id)) is false)
                {
                    return null;
                }
            }

            return this.Subscriptions[id].Reader;
        }

        private IEnumerable<ChannelWriter<PubSubMessage>> GeSubscriptions(string topic)
        {
            // TODO: Add cache
            foreach(var subscriptionContext in this.Subscriptions)
            {
                if(subscriptionContext.Value.Writer == null)
                {
                    continue;
                }

                foreach(var t in subscriptionContext.Value.Topics)
                {
                    // TODO: use MQTT matching for topics
                    if(t == topic)
                    {
                        yield return subscriptionContext.Value.Writer;
                        break;
                    }
                }
            }
        }
    }
}