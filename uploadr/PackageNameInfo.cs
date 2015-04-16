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
            // TODO: parse 
            Directory = Path.GetDirectoryName(package);
            Name = Path.GetFileName(package);
            //var name = Path.GetFileName(path);
        }

        public string Directory { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
