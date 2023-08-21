using Discord.Commands;
using McMaster.NETCore.Plugins;
using System.Reflection;

namespace NDB.Loader
{
    public class Loader_Structs
    {
        public struct LibraryItem // every dependency, library or service will be a libraryItem
        {
            public String LibraryName; // name of library
            public String LibraryVersion; // version of library
            public String LibraryType; // type, aka dependency, library or service
            public DateTime timeLoaded; // time it was last loaded
            public PluginLoader LibraryAssembly; // assembly (as a pluginloader, each libraryitem uses its own loader)
            public Assembly LibraryAssemblyDirect; // the direct assembly
            public IEnumerable<ModuleInfo>? LibraryModules; // the modules of that library, only used for libraries (not dependencies / services)
        }
    }
}
