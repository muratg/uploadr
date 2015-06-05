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
        public IEnumerable<SpecInfo> UploadSpec { get; set; }
        public IEnumerable<SourceInfo> SourceList { get; set; }
        public UploadContext(ILogger logger, string source, string destination, string packageList)
        {
            logger.LogVerbose("UploadContext");

            Logger = logger;
            Source = source;
            Destination = destination;
            PackageList = packageList;

            UploadSpec =
                File.ReadAllLines(PackageList).Skip(1).Select(file =>
                {
                    var line = file.Split(new Char[] { ',' });
                    var upload = false;
                    var list = false;
                    
                    if (String.Equals(line[1], "y", StringComparison.OrdinalIgnoreCase))
                    {
                        upload = true;
                    }
                    else if (String.Equals(line[1], "n", StringComparison.OrdinalIgnoreCase))
                    {
                        upload = false;
                    }
                    else
                    {
                        Logger.LogCritical($"Package list file {PackageList} is in incorrect format. Should be <FileName><,><Y> or <N> on each line");
                        throw new Exception("PackageList");
                    }

                    if (String.Equals(line[2], "y", StringComparison.OrdinalIgnoreCase))
                    {
                        list = true;
                    }
                    else if (String.Equals(line[2], "n", StringComparison.OrdinalIgnoreCase))
                    {
                        list = false;
                    }
                    else
                    {
                        Logger.LogCritical($"Package list file {PackageList} is in incorrect format. Should be <FileName><,><Y> or <N>,<Y> or <N> on each line");
                        throw new Exception("PackageList");
                    }

                    var packageName = new PackageNameInfo(line[0]);
                    return new SpecInfo { PackageName = packageName.Id, ShouldUpload = upload, ShouldList = list };
                }).OrderBy(spec => spec.PackageName);

            SourceList = 
                Directory.EnumerateFiles(Source).Select(path => 
                {
                    var packageName = new PackageNameInfo(path);
                    return new SourceInfo { Directory = packageName.Directory, PackageName = packageName.Id, PackageVersion = packageName.Version };
                }).OrderBy(src => src.PackageName);
        }
        public void Verify()
        {
            Logger.LogVerbose("Verify");

            // TODO: compare the lists
            UploadSpec.ToList().ForEach(spec => {
                Logger.LogInformation(spec.PackageName);
            });

            SourceList.ToList().ForEach(src =>
            {
                Logger.LogCritical(src.PackageName + " Version: " + src.PackageVersion);
            });
  
        }
    }
}
