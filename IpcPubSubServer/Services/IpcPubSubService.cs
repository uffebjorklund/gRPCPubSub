using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using IpcPubSub;
using IpcPubSubServer;
using Microsoft.Extensions.Logging;

namespace IpcPubSubServer.Services
{
    public class IpcPubSubService : PubSub.PubSubBase
    {
        private readonly ILogger<IpcPubSubService> Logger;
        private readonly PubSubManager PubSubManager;
        public IpcPubSubService(ILogger<IpcPubSubService> logger, PubSubManager pubSubManager)
        {
            this.Logger = logger;
            this.PubSubManager = pubSubManager;
        }

        public override async Task<PubSubReceipt> Publish(PubSubMessage request, ServerCallContext context)
        {
            this.Logger.LogInformation($"Publish {request.ConnectionId} {request.Topic} {request.Message}");
            await this.PubSubManager.Publish(request);
            return new PubSubReceipt { Success = true, Message = string.Empty };
        }

        public override Task<PubSubReceipt> Subscribe(SubscribeRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation($"Subscribe {request.ConnectionId} {string.Join(',',request.Topics)}");
            var result = this.PubSubManager.Subscribe(request.ConnectionId, request.Topics.ToArray());
            return Task.FromResult(new PubSubReceipt{Success = result, Message = result ? string.Empty : "Failed to create subscription"});
        }

        public override async Task StartReceiveStream(AddStreamRequest request, IServerStreamWriter<PubSubMessage> responseStream, ServerCallContext context)
        {
            try
            {
                var channelReader = this.PubSubManager.AddStream(request.ConnectionId);
                if(channelReader is null)
                {
                    return;
                }

                await foreach (var msg in channelReader.ReadAllAsync(context.CancellationToken))
                {
                    if(context.CancellationToken.IsCancellationRequested is true)
                    {
                        break;
                    }
                    this.Logger.LogInformation($"Writing message {msg} to client {request.ConnectionId}");
                    await responseStream.WriteAsync(msg);
                }
            }
            catch(Exception ex)
            {
                this.Logger.LogWarning($"Stream error: {ex.Message}");
            }
            finally
            {
                this.Logger.LogWarning("Client removed");
                this.PubSubManager.Remove(request.ConnectionId);
            }
        }
    }
}
