using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NDB.Loader
{
    public class Loader_Structs
    {
        public struct LibraryItem
        {
            public String LibraryName;
            public String LibraryVersion;
            public String LibraryType;
            public Assembly LibraryAssembly;
            public IEnumerable<ModuleInfo>? LibraryModules;
        }
    }
}
