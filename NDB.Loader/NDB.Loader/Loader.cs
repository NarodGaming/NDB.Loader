using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace NDB.Loader
{
    public class Loader : ModuleBase<SocketCommandContext>
    {
        private static List<Loader_Structs.LibraryItem> LibraryItems = new();

        private static bool hasFirstRun = false;

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.loader.json").Build();
        }

        public Loader() {
            if (hasFirstRun == false && File.Exists(Path.GetFileName("config.loader.json")))
            {
                IConfiguration _loaderconfig = BuildConfig();

                foreach (var item in _loaderconfig.AsEnumerable())
                {
                    Loader_Structs.LibraryItem newLibrary = new();
                    newLibrary.LibraryName = item.Value;
                    newLibrary.LibraryAssembly = Assembly.LoadFrom(item.Value);
                    newLibrary.LibraryVersion = FileVersionInfo.GetVersionInfo(item.Value).ProductVersion;
                    if (item.Key.Contains("lib")) { newLibrary.LibraryType = "Library"; } else if (item.Key.Contains("ser")) { newLibrary.LibraryType = "Service"; } else { throw new ArgumentException($"Invalid key naming in loader.config.json: Expected lib* or ser*, got {item.Key}"); }
                    LibraryItems.Add(newLibrary);
                }
                loadModulesConstructor();
                hasFirstRun = true;
            }
        }

        private async void loadModulesConstructor()
        {
            for (int i = 0; i < LibraryItems.Count; i++)
            {
                Console.WriteLine($"Loading: {LibraryItems[i].LibraryName} {Environment.NewLine}");
                var item = LibraryItems[i];
                if(item.LibraryType == "Library")
                {
                    item.LibraryModules = await NDB_Main._commands.AddModulesAsync(item.LibraryAssembly, NDB_Main._services);
                    LibraryItems[i] = item;
                } else
                {
                    foreach (var type in item.LibraryAssembly.GetTypes())
                    {
                        NDB_Main._lateServices.Add(type);
                    }
                    NDB_Main.AddServices(); // immediately add this service, may be required for next library
                }
            }
        } 
        
        [Command("liblist")]
        [Summary("OWNER: Lists all currently loaded libraries")]
        [Remarks("liblist")]
        public Task listLibs()
        {
            if(Context.User.Id.ToString() == NDB_Main._config["ownerid"])
            {
                String builderString = "";
                foreach (Loader_Structs.LibraryItem library in LibraryItems)
                {
                    builderString += $"{library.LibraryName} ({library.LibraryType} | v{library.LibraryVersion}) {Environment.NewLine}";
                }
                if (builderString == "") { return ReplyAsync("You don't have any libraries currently loaded!"); }
                return ReplyAsync($"Currently loaded libraries: {Environment.NewLine}{builderString}");
            } else
            {
                return ReplyAsync($"Sorry, you aren't an owner.");
            }
        }

        [Command("unload", RunMode = RunMode.Async)]
        [Summary("OWNER: Unloads a specified library/service.")]
        [Remarks("unload <library>")]
        public async Task unloadLib(String library)
        {
            if (Context.User.Id.ToString() == NDB_Main._config["ownerid"])
            {
                if (library == "all")
                {
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        if (libraryInList.LibraryType == "Library")
                        {
                            foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                            {
                                await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                            }
                        }
                    }
                    NDB_Main._lateServices.Clear();
                    NDB_Main.AddServices(); // this will unload ANY and ALL services which were 'late loaded'
                    LibraryItems.Clear();
                    await ReplyAsync("All libraries & services have been successfully detached.");
                } else
                {
                    int removeIndex = -1;
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        if (library == libraryInList.LibraryName)
                        {
                            removeIndex = LibraryItems.IndexOf(libraryInList);
                            if (libraryInList.LibraryType == "Library")
                            {
                                foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                                {
                                    await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                                }
                                break;
                            } else
                            {
                                foreach (var item in NDB_Main._lateServices)
                                {
                                    if(libraryInList.LibraryAssembly.GetTypes().Contains(item.GetType()))
                                    {
                                        NDB_Main._lateServices.Remove(item);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (removeIndex != -1)
                    {
                        LibraryItems.RemoveAt(removeIndex);
                        await ReplyAsync($"Successfully removed {library}!");
                    } else
                    {
                        await ReplyAsync($"Unsuccessful in removing {library}.");
                    }
                }
            } else
            {
                await ReplyAsync($"Sorry, you aren't an owner.");
            }
        }

        [Command("load", RunMode = RunMode.Async)]
        [Summary("OWNER: Loads a specified library/service.")]
        [Remarks("load <library> <type>")]
        public async Task loadLib(String library, String libType = "")
        {
            if (libType == "")
            {
                String templib = library.ToLower();
                bool containsLib = templib.Contains("lib");
                bool containsSer = templib.Contains("ser");

                if (containsLib && !containsSer) {
                    libType = "Library";
                } else if (!containsLib && containsSer) {
                    libType = "Service";
                } else
                {
                    await ReplyAsync("Invalid library type. Valid: Library, Service.");
                    return;
                }
            }
            libType= libType.ToLower();
            if (Context.User.Id.ToString() == NDB_Main._config["ownerid"])
            {
                bool matchFound = false;
                foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                {
                    if (libraryInList.LibraryName == library)
                    {
                        matchFound = true;
                    }
                }
                if (matchFound)
                {
                    await ReplyAsync("This library is already loaded! You must first unload this library before loading it again.");
                    return;
                } else if (!File.Exists(library))
                {
                    await ReplyAsync("This library does not exist in the current directory. Please double check the spelling and try again.");
                    return;
                } else
                {
                    Loader_Structs.LibraryItem newLibrary = new();
                    newLibrary.LibraryName = library;
                    newLibrary.LibraryAssembly = Assembly.LoadFrom(library);
                    newLibrary.LibraryVersion = FileVersionInfo.GetVersionInfo(library).ProductVersion;
                    if (libType == "library" || libType == "lib")
                    {
                        newLibrary.LibraryModules = await NDB_Main._commands.AddModulesAsync(newLibrary.LibraryAssembly, NDB_Main._services);
                        newLibrary.LibraryType = "Library";
                        LibraryItems.Add(newLibrary);
                    } else if (libType == "service" || libType == "ser")
                    {
                        foreach (var type in newLibrary.LibraryAssembly.GetTypes())
                        {
                            NDB_Main._lateServices.Add(type);
                        }
                        NDB_Main.AddServices(); // immediately add this service, may be required for next library
                        newLibrary.LibraryType = "Service";
                        LibraryItems.Add(newLibrary);
                    } else
                    {
                        await ReplyAsync("Invalid library type. Valid: Library, Service.");
                        return;
                    }
                    await ReplyAsync($"Successfully loaded {library}!");
                }
            }
            else
            {
                await ReplyAsync($"Sorry, you aren't an owner.");
            }
        }
    }
}