﻿using System;
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

                    //var packageName = new PackageNameInfo(line[0]);
                    return new SpecInfo { PackageName = line[0], ShouldUpload = upload, ShouldList = list };
                }).OrderBy(spec => spec.PackageName);

            SourceList = 
                Directory.EnumerateFiles(Source).Select(path => 
                {
                    var packageName = new PackageNameInfo(path);
                    return new SourceInfo { Directory = packageName.Directory, PackageName = packageName.Id, PackageVersion = packageName.Version };
                }).OrderBy(src => src.PackageName);
        }

        public bool Verify()
        {
            Logger.LogVerbose("Verify");
            // more verification to be added later
            return VerifySpec();
        }

        public bool VerifySpec()
        {
            var uploadSpecNames = UploadSpec.ToList().Select(si => si.PackageName).OrderBy(n => n).ToList();
            var sourceNames = SourceList.ToList().Select(sn => sn.PackageName).OrderBy(n => n).ToList();

            if (uploadSpecNames.SequenceEqual(sourceNames))
            {
                Logger.LogInformation("Package list looks good");
                return true;
            }
            else
            {
                var missingUploadSpecNames = sourceNames.Where(sn => !uploadSpecNames.Contains(sn));
                var missingSourceNames = uploadSpecNames.Where(un => !sourceNames.Contains(un));

                missingUploadSpecNames.ToList().ForEach(x => Logger.LogInformation("missing in spec: " + x));

                missingSourceNames.ToList().ForEach(x => Logger.LogInformation("missing in build folder: " + x));

                if (missingUploadSpecNames.Count() != 0 || missingSourceNames.Count() != 0)
                {
                    Logger.LogInformation("Package list has problems");
                    return false;
                }
                else
                {
                    Logger.LogInformation("Package list looks good");
                    return true;

                }
            }

        }

        public void Upload()
        {
            var sourceRepo = PackageRepositoryFactory.Default.CreateRepository(Source);
            var destinationRepo = new PackageServer(Feed, "Uploadr");

            foreach(var specInfo in UploadSpec)
            {
                if (!specInfo.ShouldUpload)
                {
                    Logger.LogInformation("skipping package: " + specInfo.PackageName);
                }
                else
                {
                    var packages = sourceRepo.FindPackagesById(specInfo.PackageName);

                    if (packages.Count() == 0)
                    {
                        Logger.LogCritical($"Package {specInfo.PackageName} could not be found");
                        throw new InvalidOperationException("Upload");
                    }
                    else
                    {
                        packages.ToList().ForEach(pkg =>
                        {
                            var sourceInfo = SourceList.Where(s => s.PackageName == specInfo.PackageName).First();
                            if (sourceInfo == null)
                            {
                                Logger.LogCritical($"Package {specInfo.PackageName} could not be found");
                                throw new InvalidOperationException("Upload");
                            }

                            //var fileInfo = new FileInfo(Path.Combine(sourceInfo.Directory, String.Concat(sourceInfo.PackageName, ".", sourceInfo.PackageVersion, ".nupkg")));
                            var fileInfo = new FileInfo(Path.Combine(sourceInfo.Directory, String.Concat(sourceInfo.PackageName, ".", pkg.Version.ToString(), ".nupkg")));
                            if (fileInfo == null)
                            {
                                Logger.LogCritical("FileInfo");
                                throw new InvalidOperationException("Upload");
                            }

                            var pushed = false;
                            var retries = 1;
                            var numRetries = 5;
                            while (!pushed && retries <= numRetries)
                            {
                                try
                                {
                                    Logger.LogInformation("found package: " + pkg.Id + " v: " + pkg.Version.ToString() + ", Length: " + fileInfo.Length + " uploading");
                                    destinationRepo.PushPackage(ApiKey, pkg, fileInfo.Length, 0, true);
                                    if (!specInfo.ShouldList)
                                    {
                                        Logger.LogInformation(" ... and unlisting");
                                        destinationRepo.DeletePackage(ApiKey, pkg.Id, pkg.Version.ToString());
                                    }
                                    pushed = true;
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogWarning("Caught an exception: " + ex.Message);
                                    Logger.LogWarning("Will retry: " + retries + " of " + numRetries);
                                    retries++;
                                }
                            }
                        });

                    }
                }
            }
        }
    }
}
