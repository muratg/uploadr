using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;


namespace uploadr
{
    public class UploadContext
    {
        private string Source { get; set; }
        private string Destination { get; set; }
        private string PackageList { get; set; }
        private ILogger Logger { get; set; }
        public IEnumerable<Tuple<string, bool>> UploadSpec { get; set; }
        public IEnumerable<Tuple<string, string>> SourceList { get; set; }
        public UploadContext(ILogger logger, string source, string destination, string packageList)
        {
            logger.LogVerbose("UploadContext");

            Logger = logger;
            Source = source;
            Destination = destination;
            PackageList = packageList;

            UploadSpec =
                File.ReadAllLines(PackageList).Select(file =>
                {
                    var line = file.Split(new Char[] { ',' });
                    var include = false;
                    if (String.Equals(line[1], "y", StringComparison.OrdinalIgnoreCase))
                    {
                        include = true;
                    }
                    else if (String.Equals(line[1], "n", StringComparison.OrdinalIgnoreCase))
                    {
                        include = false;
                    }
                    else
                    {
                        Logger.LogCritical($"Package list file {PackageList} is in incorrect format. Should be <FileName><,><Y> or <N> on each line");
                        throw new Exception("PackageList");
                    }
                    return new Tuple<string, bool>(line[0], include);
                }).OrderBy(line => line.Item1);

            SourceList = 
                Directory.EnumerateFiles(Source).Select(path => 
                {
                    var directory = Path.GetDirectoryName(path);
                    var name = Path.GetFileName(path);
                    return new Tuple<string, string>(directory, name);
                }).OrderBy(fileDesc => fileDesc.Item2);
        }
        public void Verify()
        {
            Logger.LogVerbose("Verify");

            // TODO: compare the lists
            UploadSpec.ToList().ForEach(file => {
                Logger.LogInformation(file.Item1);
            });

            SourceList.ToList().ForEach(file =>
            {
                Logger.LogCritical(file.Item2);
            });
  
        }
    }
}
