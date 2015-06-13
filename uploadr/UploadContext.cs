using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using NuGet;
using ILogger = Microsoft.Framework.Logging.ILogger;    // ambiguity between Nuget.ILogger and M.F.L.ILogger

namespace uploadr
{
    public class UploadContext
    {
        private string Source { get; set; }
        private string PackageList { get; set; }
        private string Feed { get; set; }
        private string ApiKey { get; set; }

        private ILogger Logger { get; set; }
        public IEnumerable<SpecInfo> UploadSpec { get; set; }
        public IEnumerable<SourceInfo> SourceList { get; set; }
        public UploadContext(ILogger logger, string source, string packageList, string feed, string apiKey)
        {
            logger.LogVerbose("UploadContext");

            Logger = logger;
            Source = source;
            Feed = feed;
            ApiKey = apiKey;
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

        /*
            - Verify the folder matching the upload list spec
            - upload via nuget apis.
            
            */

        public void Verify()
        {
            VerifySpec();
        }

        public bool VerifySpec()
        {
            Logger.LogVerbose("Verify");

            var uploadSpecNames = UploadSpec.ToList().Select(si => si.PackageName).OrderBy(n => n).ToList();
            var sourceNames = SourceList.ToList().Select(sn => sn.PackageName).OrderBy(n => n).ToList();

            if(uploadSpecNames.SequenceEqual(sourceNames))
            {
                return true;
            }
            else
            {
                var i = 0; var j = 0;

                var missingUploadSpecNames = new List<string>();
                var missingsourceNames = new List<string>();

                while ( i < uploadSpecNames.Count() && j < sourceNames.Count())
                {
                    var usn = uploadSpecNames[i];
                    var sn = sourceNames[j];

                    var comparison = String.CompareOrdinal(usn, sn);
                    if (comparison == 0)
                    {
                        i++; j++;
                    }
                    else if (comparison < 0)
                    {
                        missingUploadSpecNames.Add(usn);
                        i++;
                    }
                    else if (comparison > 0)
                    {
                        missingsourceNames.Add(sn);
                        j++;
                    }
                }

                while (i < uploadSpecNames.Count())
                {
                    missingUploadSpecNames.Add(uploadSpecNames[i]);
                    i++;
                }

                while (j < sourceNames.Count())
                {
                    missingsourceNames.Add(sourceNames[j]);
                    j++;
                }

                return false;
            }
        }

        public void Upload()
        {
            var sourceRepo = PackageRepositoryFactory.Default.CreateRepository(Source);
            var destinationRepo = new PackageServer(Feed, "Uploadr");

            foreach(var specInfo in UploadSpec)
            {
                var package = sourceRepo.FindPackagesById(specInfo.PackageName);
                var sourceInfo = SourceList.Where(s => s.PackageName == specInfo.PackageName).First();
                if (sourceInfo == null)
                {
                    Logger.LogCritical($"Package {specInfo.PackageName} could not be found");
                    throw new InvalidOperationException("Upload");
                }
                var fileInfo = new FileInfo(Path.Combine(sourceInfo.Directory, String.Concat(sourceInfo.PackageName, ".", sourceInfo.PackageVersion, ".nupkg")));
                if(fileInfo == null)
                {
                    Logger.LogCritical("FileInfo");
                    throw new InvalidOperationException("Upload");
                }
                if (package.Count() == 0)
                {
                    Logger.LogCritical($"Package {specInfo.PackageName} could not be found");
                    throw new InvalidOperationException("Upload");
                }
                else
                {
                    Logger.LogInformation("found package: " + specInfo.PackageName);
                    if (specInfo.ShouldUpload)
                    {
                        destinationRepo.PushPackage(ApiKey, package.First(), fileInfo.Length, 0, false);
                    }
                    if(specInfo.ShouldUpload && !specInfo.ShouldList)
                    {
                        destinationRepo.DeletePackage(ApiKey, package.First().Id, package.First().Version.ToString());
                    }
                }
            }

        }
    }
}
