using System;
using System.Threading.Tasks;
using Grpc.Core;
using IpcPubSub;
using IpcPubSubClient;

if (args.Length == 0)
{
    Console.WriteLine("No topic provided, terminating client");
    return;
}

var (client, connectionId) = CreateClient();
await CreateSubscriptions(client, connectionId, args);

StartReceivingFromStream(client, connectionId);

// Publish some example messages every 5 sec
while(true)
{
    try
    {
        var m1 = new PubSubMessage();
        m1.ConnectionId = connectionId;
        m1.Topic = "foo/baz/bar";
        m1.Message = $"Hello foo/baz/bar from {connectionId}";
        await client.PublishAsync(m1);

        var m2 = new PubSubMessage();
        m2.ConnectionId = connectionId;
        m2.Topic = "foo/bar/baz";
        m2.Message = $"Hello foo/bar/baz from {connectionId}";
        await client.PublishAsync(m2);

        await Task.Delay(5000);
    }
    catch{}
}


static (PubSub.PubSubClient client, string connectionId) CreateClient()
{
    var channel = UnixDomainSocketConnectionFactory.CreateChannel();
    return (new PubSub.PubSubClient(channel), Guid.NewGuid().ToString("N"));
}

static async Task CreateSubscriptions(PubSub.PubSubClient client, string connectionId, string[] topics)
{
    var subReq = new SubscribeRequest();
    subReq.ConnectionId = connectionId;

    foreach (var topic in topics)
    {
        subReq.Topics.Add(topic);
    }

    var subResponse = await client.SubscribeAsync(subReq);
    Console.WriteLine("SubResponse: success = " + subResponse.Success);

}

static void StartReceivingFromStream(PubSub.PubSubClient client, string connectionId)
{
    var _ = Task.Run(async () =>
    {
        try
        {
            using var call = client.StartReceiveStream(new AddStreamRequest { ConnectionId = connectionId });
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
}