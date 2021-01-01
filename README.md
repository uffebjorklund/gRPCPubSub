# gRPCPubSub
A sample project for using gRPC as IPC with pub/sub.

This example will use UnixDomainSockets as transport for efficient IPC between processes on the same machine.

## Example

 1. Start the server

    From the project root execute `dotnet run --project GrpcIPCServer/GrpcIPCServer.csproj`

 2. Start client 1

    From the project root execute `dotnet run --project GrpcIPCClient/GrpcIPCClient.csproj foo/bar`

 3. Start client 2

    From the project root execute `dotnet run --project GrpcIPCClient/GrpcIPCClient.csproj bar/foo`


Each client will now setup a subscription to individual topics. The sample client will publish messages with these two topics (`foo/bar` and `bar/foo`) with a 5 seconds interval