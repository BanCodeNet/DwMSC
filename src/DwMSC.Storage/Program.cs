using DwFramework;
using DwFramework.Web;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace DwMSC.Storage;

public static class Program
{
    private record class Listen
    {
        public string Ip { get; init; }
        public int Port { get; init; }
        public HttpProtocols Protocol { get; init; }
        public bool UseSSL { get; init; }
        public string Cert { get; init; }
        public string Password { get; init; }
    }

    private record class Mail
    {
        public bool Enable { get; init; }
    }

    public static async Task Main(params string[] args)
    {
        var host = new ServiceHost(args);
        host.UserNLog();
        host.ConfigureWebHostDefaults(builder =>
        {
            builder.UseKestrel((context, options) =>
            {
                var listens = context.Configuration.ParseConfiguration<IEnumerable<Listen>>("Host:Listens");
                if (listens is null || !listens.Any()) throw new Exception("listens must set");
                foreach (var item in listens)
                {
                    options.Listen(string.IsNullOrEmpty(item.Ip) ? IPAddress.Any : IPAddress.Parse(item.Ip), item.Port, listenOptions =>
                    {
                        listenOptions.Protocols = item.Protocol == HttpProtocols.None ? HttpProtocols.Http1 : item.Protocol;
                        if (item.UseSSL) listenOptions.UseHttps(item.Cert, item.Password);
                    });
                }
            });
            builder.ConfigureServices((context, services) =>
            {
                var mode = context.Configuration.ParseConfiguration<StorageModes>("Mode");
                services.AddControllers();
                // Services
                switch (mode)
                {
                    case StorageModes.IPFS:
                        services.AddScoped<IStorageService, IPFSService>();
                        break;
                    default: throw new Exception("not supported mode");
                }
            });
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
        });
        await host.RunAsync();
    }
}

