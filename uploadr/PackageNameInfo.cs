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
            var fullNameParts = Path.GetFileName(package).Split(new char[] { '.'});
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

            Id = String.Join(".", fullNameParts.Take(verIdx - 1));
            Version = String.Join(".", fullNameParts.Skip(verIdx)).Replace(".nupkg","");
        }

        public string Directory { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
    }
}
