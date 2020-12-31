using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace GrpcIPCServer
{
    public class Program
    {
        public static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "ipc.tmp");

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();


            CreateGRPCHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateGRPCHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    .UseSerilog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseUrls();
            webBuilder.ConfigureKestrel(options =>
            {
                if (File.Exists(SocketPath))
                {
                    File.Delete(SocketPath);
                }
                options.ListenUnixSocket(SocketPath, o => o.Protocols = HttpProtocols.Http2);
            });
            webBuilder.UseStartup<GrpcStartup>();
        });
    }
}
