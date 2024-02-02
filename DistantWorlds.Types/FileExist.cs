using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistantWorlds.Types
{

    static class FileExist
    {
        private static object _lock = new object();
        public static bool FileExists(string path) { lock (_lock) { return File.Exists(path); } }
    }
}
