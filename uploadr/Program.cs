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
        public void Main(string[] args)
        {
            Configuration = new Configuration()
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            var source = Configuration["uploadr:source"];
            var destination = Configuration["uploadr:destination"];
            var packageList = Configuration["uploadr:packageList"];

            if(String.IsNullOrEmpty(source) || String.IsNullOrEmpty(destination))
            {
                throw new Exception("Configuration");
            }

            if(!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException(source);
            }

            if(!Directory.Exists(destination))
            {
                WriteLine($"Directory {destination} does not exist. Creating...");
                Directory.CreateDirectory(destination);
            }

            if(!File.Exists(packageList))
            {
                throw new FileNotFoundException(packageList);
            }
            
            WriteLine("Press enter to quit.");
            ReadLine();
        }

        public void Configure()
        {
            WriteLine("configure");
        }
    }
}