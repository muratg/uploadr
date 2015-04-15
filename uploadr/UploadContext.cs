using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace uploadr
{
    public class UploadContext
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string PackageList { get; set; }
        private ILogger Logger { get; set; }
        public UploadContext(ILogger logger, string source, string destination, string packageList)
        {
            Logger = logger;
            Source = source;
            Destination = destination;
            PackageList = packageList;
            Logger.LogVerbose("UploadContext");
        }
    }
}
