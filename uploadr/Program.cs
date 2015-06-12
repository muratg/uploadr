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

            Configuration = new Configuration(".")
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            var source = Configuration["uploadr:source"];
            var packageList = Configuration["uploadr:packageList"];
            var feed = Configuration["uploadr:feed"];
            var apiKey = Configuration["uploadr:apiKey"];

            Logger.LogVerbose($"Source: {source}");
            Logger.LogVerbose($"Package list (csv): {packageList}");
            Logger.LogVerbose($"Feed: {feed}");
            Logger.LogVerbose($"API Key: {apiKey}");

            if(!Directory.Exists(source))
            {
                Logger.LogCritical($"Source directory {source} doesn't exist.");
                throw new DirectoryNotFoundException(source);
            }

            if(!File.Exists(packageList))
            {
                throw new FileNotFoundException(packageList);
            }

            if(String.IsNullOrWhiteSpace("feed"))
            {
                throw new InvalidOperationException("Feed to upload to not configured");
            }

            if (String.IsNullOrWhiteSpace("apiKey"))
            {
                throw new InvalidOperationException("API key not configured.");
            }

            var uploadContext = new UploadContext(Logger, source, packageList, feed, apiKey);
            uploadContext.Verify();
            uploadContext.Upload();

            WriteLine("Press enter to quit.");
            ReadLine();
        }
    }
}