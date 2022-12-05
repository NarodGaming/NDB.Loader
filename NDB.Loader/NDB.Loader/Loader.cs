using Discord.Commands;
using System.Reflection;

namespace NDB.Loader
{
    public class Loader : ModuleBase<SocketCommandContext>
    {
        private static List<Loader_Structs.LibraryItem> LibraryItems = new();
        
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
                    builderString += library.LibraryName + Environment.NewLine;
                }
                return ReplyAsync($"Currently loaded libraries: {Environment.NewLine}{builderString}");
            } else
            {
                return ReplyAsync($"Sorry, you aren't an owner.");
            }
        }

        [Command("unload", RunMode = RunMode.Async)]
        [Summary("OWNER: Unloads a specified library.")]
        [Remarks("unload <library>")]
        public async Task unloadLib(String library)
        {
            if (Context.User.Id.ToString() == NDB_Main._config["ownerid"])
            {
                if (library == "all")
                {
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                        {
                            await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                        }
                    }
                    LibraryItems.Clear();
                    await ReplyAsync("All libraries have been successfully detached.");
                } else
                {
                    int removeIndex = -1;
                    foreach (Loader_Structs.LibraryItem libraryInList in LibraryItems)
                    {
                        if (library == libraryInList.LibraryName)
                        {
                            foreach (ModuleInfo libraryModule in libraryInList.LibraryModules)
                            {
                                await NDB_Main._commands.RemoveModuleAsync(libraryModule);
                            }
                            removeIndex = LibraryItems.IndexOf(libraryInList);
                            break;
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
        [Summary("OWNER: Loads a specified library.")]
        [Remarks("load <library>")]
        public async Task loadLib(String library)
        {
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
                } else if (!File.Exists(library))
                {
                    await ReplyAsync("This library does not exist in the current directory. Please double check the spelling and try again.");
                } else
                {
                    Loader_Structs.LibraryItem newLibrary = new();
                    newLibrary.LibraryName = library;
                    newLibrary.LibraryAssembly = Assembly.LoadFrom(library);
                    newLibrary.LibraryModules = await NDB_Main._commands.AddModulesAsync(newLibrary.LibraryAssembly, NDB_Main._services);
                    newLibrary.LibraryVersion = "1.0.0.0";
                    newLibrary.LibraryType = "Library";
                    LibraryItems.Add(newLibrary);
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