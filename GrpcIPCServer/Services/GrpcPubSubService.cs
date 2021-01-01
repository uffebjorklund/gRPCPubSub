using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcIPC;
using GrpcIPCServer.PubSub;
using Microsoft.Extensions.Logging;

namespace GrpcIPCServer.Services
{
    public class GrpcPubSubService : GrpcIPC.PubSub.PubSubBase
    {
        private readonly ILogger<GrpcPubSubService> Logger;
        private readonly PubSubManager PubSubManager;
        public GrpcPubSubService(ILogger<GrpcPubSubService> logger, PubSubManager pubSubManager)
        {
            this.Logger = logger;
            this.PubSubManager = pubSubManager;
        }

        public override async Task<PubSubReceipt> Publish(PubSubMessage request, ServerCallContext context)
        {
            this.Logger.LogWarning($"Publish {request.ConnectionId} {request.Topic} {request.Message}");
            await this.PubSubManager.Publish(request.Topic, request.Message);
            return new PubSubReceipt { Success = true, Message = string.Empty };
        }

        public override Task<PubSubReceipt> Subscribe(SubscribeRequest request, ServerCallContext context)
        {
            this.Logger.LogWarning($"Subscribe {request.ConnectionId} {string.Join(',',request.Topics)}");

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

                await foreach (var msg in channelReader.ReadAllAsync())
                {
                    if(context.CancellationToken.IsCancellationRequested is true)
                    {
                        break;
                    }
                    await responseStream.WriteAsync(msg);
                }
            }
            catch(Exception ex)
            {
                this.Logger.LogWarning($"Stream error: {ex.Message}");
            }
            finally
            {
                this.PubSubManager.Remove(request.ConnectionId);
            }
        }
    }
}
