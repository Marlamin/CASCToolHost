using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                //await Task.Run(() => NGDP.LoadAllIndexes());
                //Console.WriteLine("Loaded indexes");

                var keys = await KeyService.LoadKeys();
                Console.WriteLine("Loaded " + keys.Count + " keys");
            }

            await webHost.RunAsync();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Limits.MaxConcurrentConnections = 500;
                    serverOptions.Limits.MaxConcurrentUpgradedConnections = 500;
                })
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5005/");
            });
    }
}
