# A library & dependency loader for Discord.NET Bots

This library provides Discord.NET bots, including [NDB.Main](https://github.com/NarodGaming/NDB.Main), with easy loading & unloading capabilities for external libraries & dependencies.

For example, with this library, you can late-load (and unload, so also reload) extra modules you may wish to tack on to your bot.

So this means you can build command modules & any other functionality in a completely separate project, which is only loaded at runtime, meaning less down-time for your bot, and easier maintainability.

## How do I use this library?

If you're using NDB:

1. Download (or compile) this library.
2. Place it in the same folder as your NDB.Main executable.
3. Add `"loaderlib": "NDB.Loader.dll"` to your config.json file.
4. Create a config.loader.json file, and add any additional libraries & dependencies which should be autoloaded, this should be in the format of:
    - `"lib1": "FileName.dll"` where the number after "lib" is incremented for each new library.
    - `"dep1": "FileName.dll"` where the number after "dep" is incremented for each new library.

If you're using a different bot (or a custom bot):

You'll need to follow your specific bots instructions. If it has none, or you're using a custom bot:

1. Download the source code for this library.
2. Add a reference to this library in your Discord Bot (usually right click project on right hand side -> Add Reference)
3. Load in this library like you would any new library - this may be vague but this can be so very varied!

We are unable to provide any assistance for custom or different bots.

## What is a library? What is a dependency?

A dependency is anything further which your library relies upon outside of it's own library file.

For example, if you use [NDB.Library.Embeds](https://github.com/NarodGaming/NDB.Library.Embeds) in any of your command modules, that is a dependency!

The command modules themselves however are a library, such as [NDB.Library.NScript](https://github.com/NarodGaming/NDB.Library.NScript).

Don't worry - you don't need to add every single dependency in, only those you wish to share between all libraries. We'll automatically load in all other dependencies required as they're needed.