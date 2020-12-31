using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var msg = new PubSubMessage{Topic = topic, Message = message};
            foreach(var stream in this.GeSubscriptionStreams(topic))
            {
                // TODO: Handle exceptions...
                await stream.WriteAsync(msg);
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

        public bool AddStream(string connectionId, IServerStreamWriter<PubSubMessage> responseStream)
        {
            if (Guid.TryParse(connectionId, out Guid id) is false)
            {
                return false;
            }
            if (this.Subscriptions.ContainsKey(id) is false)
            {
                return this.Subscriptions.TryAdd(id, new SubscriptionContext(id));
            }

            this.Subscriptions[id].streamWriter = responseStream;
            return true;
        }

        private IEnumerable<IServerStreamWriter<PubSubMessage>> GeSubscriptionStreams(string topic)
        {
            foreach(var subscriptionContext in this.Subscriptions)
            {
                if(subscriptionContext.Value.streamWriter == null)
                {
                    continue;
                }

                foreach(var t in subscriptionContext.Value.Topics)
                {
                    if(t == topic)
                    {
                        yield return subscriptionContext.Value.streamWriter;
                        break;
                    }
                }
            }
        }
    }
}