# Boats

Unsure if your idea or bug has already been reported? [This](https://trello.com/b/xOZiNOOU/rimships) is everything I am currently working on or looking into 

## FAQ

**How do boats work?**

Much like cryptosleep caskets or even caravans, the sailors are stored as World Pawns. Everything about them, including their inventory, is saved. They are still affected by needs such as hunger or mood, and *can* die. Select a boat when forming a caravan to turn the caravan into a boat caravan, or board pawns directly onto ships by right clicking the ship. You can do this one at a time or with a group pawns, but in both cases the pawns cannot be drafted.

**What incompatibilities does this mod have?**

For now the hard incompatabilities are Combat Extended and Multiplayer, although I do intend to role out patches for both, as soon as I feel comfortable with how this mod performs. Soft incompatabilities include World Gen mods such as Faction Control, which overwrite the world gen option of this mod which can push settlements to the coast, allowing you to interact with more settlements.  There is nothing I can do about this other than changing the other mod author's code, as some people just blatantly overwrite these classes and methods. 

**Current Bugs you may come across**

These are all harmless bugs and you can ignore them. I am looking into all of them.
- Achtung can cause a small error when selecting a pawn that is on top of a boat, and boarding them onto the ship.
- GiddyUpCore will cause a 1 time error when undrafting a boat while it is traveling along its path.
Note: I highly recommend against using RimCities with Boats. You may have them both loaded in the same game, but attempting to attack or visit a City from RimCities with a boat will more than likely result in a bug. This could mean your boat spawns somewhere getting you stuck, not entering the city at all, or even disappearing, killing all of your pawns and the boat in the process. This is due to the fact that RimCities do not generate well along coastlines and rivers.

**Why does Map Generation take so long now?**

One of the challenges with boats is dealing with impassable deep water. Do I make it passable for everyone? Or do I find a workaround that keeps pawns off those deep water tiles? I chose the latter, implementing my own version (albeit still a work in progress) of RimWorld's A* pathfinding algorithm. This includes all of the Regions, Reachability classes, PathGrid, and Utility classes that intertwine with it all. Of the 86 classes that make up this mod, I'd say about 1/4th of them are for the custom pathfinding. How is that relevant? Because the game will now generate 2 sets of regions, 2 pathgrids, 2 reachability caches, etc... per map.  Does this slow down performance? No. But Map Generation time is increased of course. 

**Can I add my own Boat design? How?**

Yes! In fact, I made this mod with that in mind. Everything needed to create a boat is provided for you in xml. I made everything in a framework style so that it is easy to read, understand, and implement in your own way. I'll be putting a text guide that will explain what to do, step by step, for those that don't wish to explore in all my .xml files to figure it out. I promise though, it's not too difficult.

*If you have any suggestions, improvements, ideas, or bugs to report. Please do not hesitate to leave it either here on GitHub or on the Steam Workshop Page*
