# gRPCPubSub
A sample project for using gRPC as IPC with pub/sub.

This example will use UnixDomainSockets as transport for efficient IPC between processes on the same machine.

## Example

The sample client will publish messages with the topics `foo/bar/baz` and `foo/baz/bar`.
The example supports MQTT subscriptions to that you can use wildcards...
For example `foo/+/baz` will match `foo/bar/baz` and `foo/#` will match both `foo/bar/baz` and `foo/baz/bar`

 1. Start the server

    From the project root execute `dotnet run --project GrpcIPCServer/GrpcIPCServer.csproj`

 2. Start client 1

    From the project root execute `dotnet run --project GrpcIPCClient/GrpcIPCClient.csproj foo/+/baz`

 3. Start client 2

    From the project root execute `dotnet run --project GrpcIPCClient/GrpcIPCClient.csproj foo/#`


Client 2 should get all messages but client 1 should only get `foo/bar/baz`