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
        private HashSet<string> cache = new();


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
                    if(this.IsMatch(topic, t))
                    {
                        yield return subscriptionContext.Value.Writer;
                        break;
                    }
                }
            }
        }

        // TODO: use ReadOnlySpan<char> instead
        public bool IsMatch(string messageTopic, string subscriptionTopic)
        {
            if (messageTopic == subscriptionTopic) return true;

            if (subscriptionTopic == "#") return true;

            var key = string.Concat(messageTopic, subscriptionTopic);

            if (cache.Contains(key)) return true;

            var pathLevels = messageTopic.Trim('/').Split('/');
            var topicLevels = subscriptionTopic.Trim('/').Split('/');

            var i = 0;
            for (; i < pathLevels.Length; i++)
            {
                if (i >= topicLevels.Length)
                {
                    return false;
                }
                if (topicLevels[i] == "+")
                {
                    continue;
                }
                if (topicLevels[i] == "#")
                {
                    cache.Add(key);
                    return true;
                }
                if (topicLevels[i] != pathLevels[i])
                {
                    return false;
                }
            }
            if (i == topicLevels.Length)
            {
                cache.Add(key);
                return true;
            }
            return false;
        }
    }
}