using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using static System.Console;

namespace uploadr
{
    public class Program
    {
        private IConfiguration Configuration { get; set; }
        private ILogger Logger { get; set; }

        public void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(minLevel: LogLevel.Verbose);
            Logger = loggerFactory.CreateLogger("uploadr");

            Logger.LogVerbose("Accessing configuration");

            Configuration = new Configuration()
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            var source = Configuration["uploadr:source"];
            var destination = Configuration["uploadr:destination"];
            var packageList = Configuration["uploadr:packageList"];

            Logger.LogVerbose($"Source: {source}");
            Logger.LogVerbose($"Destination: {destination}");
            Logger.LogVerbose($"Package list (csv): {packageList}");

            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(destination))
            {
                Logger.LogCritical("Configuration is incomplete");
                throw new Exception("Configuration");
            }

            if(!Directory.Exists(source))
            {
                Logger.LogCritical($"Source directory {source} doesn't exist.");
                throw new DirectoryNotFoundException(source);
            }

            if(!Directory.Exists(destination))
            {
                Logger.LogInformation($"Destination directory {destination} doesn't exits. Creating...");
                Directory.CreateDirectory(destination);
            }

            if(!File.Exists(packageList))
            {
                throw new FileNotFoundException(packageList);
            }

            var uploadContext = new UploadContext(Logger, source, destination, packageList);
            uploadContext.Verify();

            WriteLine("Press enter to quit.");
            ReadLine();
        }
    }
}