﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using McMaster.NETCore.Plugins;
using System.Runtime.Loader;

namespace NDB.Loader
{
    public class Loader : ModuleBase<SocketCommandContext>
    {
        private static List<Loader_Structs.LibraryItem> LibraryItems = new();

        private static bool hasFirstRun = false;

        private static string currentDir = AppContext.BaseDirectory;


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
                    if (item.Key.Contains("dep") || item.Key.Contains("ser")) {
                        Loader_Structs.LibraryItem newLibrary = new();
                        newLibrary.LibraryName = item.Value;
                        newLibrary.LibraryAssembly = PluginLoader.CreateFromAssemblyFile(assemblyFile: currentDir + item.Value, sharedTypes: new[] { typeof(NDB_Main) }, isUnloadable: true, configure: config => config.LoadInMemory = true);
                        newLibrary.LibraryVersion = FileVersionInfo.GetVersionInfo(item.Value).ProductVersion;
                        newLibrary.timeLoaded = DateTime.Now;
                        if (item.Key.Contains("lib")) { newLibrary.LibraryType = "Library"; } else if (item.Key.Contains("ser")) { newLibrary.LibraryType = "Service"; } else if (item.Key.Contains("dep")) { newLibrary.LibraryType = "Dependency"; } else { throw new ArgumentException($"Invalid key naming in loader.config.json: Expected lib*, ser* or dep*, got {item.Key}"); }
                        LibraryItems.Add(newLibrary);
                    }
                }
                //loadModulesConstructor();
                /*                Type[] sharedTypesWithDep = new Type[LibraryItems.Count+1];
                                for (int i = 0; i < LibraryItems.Count; i++)
                                {
                                    sharedTypesWithDep[i] = LibraryItems[i].LibraryAssemblyDirect.ExportedTypes.First();
                                    Console.WriteLine(LibraryItems[i].LibraryAssemblyDirect.ExportedTypes.First().AssemblyQualifiedName);
                                    Console.WriteLine(typeof(NDB_Main).AssemblyQualifiedName);
                                }
                                sharedTypesWithDep[LibraryItems.Count] = typeof(NDB_Main);*/

                foreach (var item in _loaderconfig.AsEnumerable())
                {
                    if (item.Key.Contains("dep") || item.Key.Contains("ser")) { continue; }
                    Loader_Structs.LibraryItem newLibrary = new();
                    newLibrary.LibraryName = item.Value;
                    newLibrary.LibraryAssembly = PluginLoader.CreateFromAssemblyFile(assemblyFile: currentDir + item.Value, sharedTypes: new[] {typeof(NDB_Main) }, isUnloadable: true, configure: config => config.LoadInMemory = true);
                    newLibrary.LibraryVersion = FileVersionInfo.GetVersionInfo(item.Value).ProductVersion;
                    newLibrary.timeLoaded = DateTime.Now;
                    if (item.Key.Contains("lib")) { newLibrary.LibraryType = "Library"; } else if (item.Key.Contains("ser")) { newLibrary.LibraryType = "Service"; } else if (item.Key.Contains("dep")) { newLibrary.LibraryType = "Dependency"; } else { throw new ArgumentException($"Invalid key naming in loader.config.json: Expected lib*, ser* or dep*, got {item.Key}"); }
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
/*                if (item.LibraryType == "Dependency" || item.LibraryType == "Service")
                {
                    item.LibraryAssemblyDirect = AssemblyLoadContext.Default.LoadFromAssemblyPath(currentDir + item.LibraryName);
                }
                else
                {
                    
                }*/
                item.LibraryAssemblyDirect = item.LibraryAssembly.LoadDefaultAssembly();
                if (item.LibraryType == "Library")
                {
                    item.LibraryModules = await NDB_Main._commands.AddModulesAsync(item.LibraryAssemblyDirect, NDB_Main._services);
                } else if (item.LibraryType == "Service")
                {
                    foreach (var type in item.LibraryAssemblyDirect.GetExportedTypes())
                    {
                        NDB_Main._lateServices.Add(type);
                    }
                    NDB_Main.AddServices(); // immediately add this service, may be required for next library
                }
                LibraryItems[i] = item;
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
                    builderString += $"{library.LibraryName.Split(".dll")[0]} *({library.LibraryType} | v{library.LibraryVersion} | Loaded at {library.timeLoaded})* {Environment.NewLine}";
                }
                if (builderString == "") { return ReplyAsync("You don't have any libraries currently loaded!"); }
                return ReplyAsync($"**Currently loaded libraries:** {Environment.NewLine}{builderString}");
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
                    List<Loader_Structs.LibraryItem> dependencyList = new();
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        if (libraryInList.LibraryType == "Library")
                        {
                            foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                            {
                                await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                            }
                        }
                        if (libraryInList.LibraryType == "d" || libraryInList.LibraryType == "s") // services are temporarily considered the same as dependencies
                        {
                            dependencyList.Add(libraryInList);
                        } else
                        {
                            libraryInList.LibraryAssembly.Dispose();
                        }
                    }
                    NDB_Main._lateServices.Clear();
                    NDB_Main.AddServices(); // this will unload ANY and ALL services which were 'late loaded'
                    LibraryItems.Clear();
                    foreach (Loader_Structs.LibraryItem dependencyInList in dependencyList)
                    {
                        LibraryItems.Add(dependencyInList);
                    }
                    await ReplyAsync("All libraries & services have been successfully detached.");
                } else
                {
                    int removeIndex = -1;
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        if (library == libraryInList.LibraryName)
                        {
                            if (libraryInList.LibraryType == "s" || libraryInList.LibraryType == "d")
                            {
                                await ReplyAsync("Unloading of services and dependencies is currently unsupported.");
                                return;
                            }
                            removeIndex = LibraryItems.IndexOf(libraryInList);
                            if (libraryInList.LibraryType == "Library")
                            {
                                foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                                {
                                    await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                                }
                                libraryInList.LibraryAssembly.Dispose();
                                break;
                            } else
                            {
                                foreach (var item in NDB_Main._lateServices)
                                {
                                    if(libraryInList.LibraryAssemblyDirect.GetTypes().Contains(item.GetType()))
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
                    newLibrary.LibraryAssembly = PluginLoader.CreateFromAssemblyFile(assemblyFile: currentDir + newLibrary.LibraryName, sharedTypes: new[] { typeof(NDB_Main) }, isUnloadable: true, configure: config => config.LoadInMemory = true);
                    newLibrary.LibraryVersion = FileVersionInfo.GetVersionInfo(library).ProductVersion;
                    newLibrary.timeLoaded = DateTime.Now;
                    newLibrary.LibraryAssemblyDirect = newLibrary.LibraryAssembly.LoadDefaultAssembly();
                    if (libType == "library" || libType == "lib")
                    {
                        newLibrary.LibraryModules = await NDB_Main._commands.AddModulesAsync(newLibrary.LibraryAssemblyDirect, NDB_Main._services);
                        newLibrary.LibraryType = "Library";
                        LibraryItems.Add(newLibrary);
                    }
                    else if (libType == "service" || libType == "ser")
                    {
                        foreach (var type in newLibrary.LibraryAssemblyDirect.GetTypes())
                        {
                            NDB_Main._lateServices.Add(type);
                        }
                        NDB_Main.AddServices(); // immediately add this service, may be required for next library
                        newLibrary.LibraryType = "Service";
                        LibraryItems.Add(newLibrary);
                    } else if (libType == "dependency") {
                        newLibrary.LibraryType = "Dependency";
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