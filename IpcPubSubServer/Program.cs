using System.IO;
using IpcPubSubServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
.MinimumLevel.Warning()
.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
.MinimumLevel.Override("Grpc", LogEventLevel.Warning)
.Enrich.FromLogContext()
.WriteTo.Console()
.CreateLogger();


CreateGRPCHostBuilder(args).Build().Run();

static IHostBuilder CreateGRPCHostBuilder(string[] args) =>
Host.CreateDefaultBuilder(args)
.UseSerilog()
.ConfigureWebHostDefaults(webBuilder =>
{

    webBuilder.UseUrls();
    webBuilder.ConfigureKestrel(options =>
    {
        string SocketPath = Path.Combine(Path.GetTempPath(), "ipc.tmp");
        if (File.Exists(SocketPath))
        {
            File.Delete(SocketPath);
        }
        options.ListenUnixSocket(SocketPath, o => o.Protocols = HttpProtocols.Http2);
    });
    webBuilder.UseStartup<GrpcStartup>();
});
