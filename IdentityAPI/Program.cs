using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            string portFlag = args
                .Where(arg => arg.StartsWith("--port="))
                .SingleOrDefault()
                ?.Split("=")[1];
            int port = portFlag != null ? Convert.ToInt32(portFlag) : 5000;

            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, port);
                });
        }
    }
}
