using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace uploadr 
{
    public class PackageNameInfo
    {
        public PackageNameInfo(string package)
        {
            Directory = Path.GetDirectoryName(package);
            var filename = Path.GetFileName(package);
            var fullNameParts = filename.Split(new char[] { '.'});
            // Find out where version information...
            var verIdx = fullNameParts.Length;
            // delete ".nupgk"
            for (var i = 0; i < fullNameParts.Length; i++)
            {
                var part = fullNameParts[i];
                var found = false;
                try
                {
                    var n = int.Parse($"{part[0]}");
                    found = true;
                }
                catch (FormatException)
                {
                    // continue
                }
                if (found)
                {
                    verIdx = i;
                    break;
                }
            }
            var tail = String.Join(".", fullNameParts.Skip(verIdx));
            Id = filename.Replace(String.Concat(".", tail), "");
            Version = tail.Replace(".nupkg", ""); 
        }

        public string Directory { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
    }
}
