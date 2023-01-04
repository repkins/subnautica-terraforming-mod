# Subnautica Terraforming Mod
Terraforming Ability mod for Subnautica and it's expansion Below Zero - underwater survival experiences.

**[Download for Subnautica v1.4.4](https://github.com/repkins/subnautica-terraforming-mod/releases/download/v1.4.4/Terraforming_v144.zip)**

**[Download for Below Zero v1.4.4](https://github.com/repkins/subnautica-terraforming-mod/releases/download/v1.4.4/TerraformingBZ_v144.zip)**

## Features:
- **Saves/loads** modified terrain to/from saves. Saves only those terrain areas which was modified, reducing save size bloating, into new "CompiledOctreesCache" folder in your save folders.
- **Allows "partially" burying habitat modules** into terrain. Mod automatically modifies terrain around them after finishing construction so habitat modules does not "overlap" with the terrain.
    - Also allows to **place base module with overlapping rogue objects** (referred as construction obstacles) otherwise vanilla version prevents to place.
    - Allows to destroy rogue objects overlapping with base module **using a hotkey**.
    - **Highlights overlapping objects** to be destroyed. This also includes rogue objects if destroy obstacles enabled.
- **Repulsion cannon** *("Obsolete" in Below Zero)* now can remove small portions of terrain when "shooting" pulses at those spots in terrain.
- **"Obsolete" terraformer** tool (obtainable using console commands only, i.e. "*item terraformer*") now actually works, which removes/adds portions of terrain.
- **"dig #"** console command also now actually works which allows to perform **spherical removal** of terrain at player location as a sphere center within provided radius as a first parameter of command, ex. "dig 5".

## Installation:
1. Install **QModsManager**.
2. Put mod folder into "QMods" folder of game directory.

## Configuration:
There is new section added in **"Mods" tab** of **in-game options** which allows to change the following:
- **Rebuilding messages** - shows terrain rebuilding message while terrain rebuilding is in progress. Enabled by default..
- **Habitant modules burying** - allows habitat burying into terrain and adjusts overlapping terrain around them. Enabled by default..
- **Terrain vs module space** - allows to adjust space between terrain surface and base compartment. High value means more space, low value means less space. Defaults to 1.0.
- **Repulsion terrain impact** - causes the repulsion cannon to remove small portion of terrain after "shooting" pulse at that spot of terrain. Enabled by default.
- **Destroyable obstacle transparency** - allows to adjust transparency amount of destroyable construction obstacles. Transparency serves as warning to be destroyed if destroying obstacles enabled. Defaults to 0.1.
- **Destroy obstacles on construction** - highlights destroyable overlapping certain objects after placing a base module for construction. Destroys them when construction of module finishes, so *careful with this setting on*. Disabled by default. 

## Un-installation
Remove this mod folder in QMods folder. Optionally delete "CompiledOctreesCache" folder in your save folders if they are created.

## Known issues:
1. Terraforming is not immediate[^immediate].
2. Terrain still overlaps at moonpool connectors, or does not adjusts at all [^overlaps] _(SN1 only)_.
3. Player on surface falls-through terrain when trying to terraform. Same with other physics objects [^falls] _(SN1 only)_.
4. Game freezes while doing large terrain edit at a time with terraformer tool or "dig #" console command[^freezes].
5. Sometimes after terrain modifications it leaves "invisible" ground/wall, preventing further edits[^invisible].

## Questions? Problems?
For problems and other technical issues not stated there please ask in **Official Subnautica Modding** discord server.

Please be noticed that creating too much details in terrain could cost more performance as there comes more detailed data to render, like with too many habitats/modules. It's basically designed for doing terrain adjustments, not creating new caves/mines or something which creates new details to render, although creating caves/mines is permitted/free to do so.

There is a possibility I could missed something so I advise to **create a backup** from existing save before saving a game with this mod installed and enabled. Please let me know if any issue happens.

### Other releases:
- [Subnautica](https://www.nexusmods.com/subnautica/mods/504?tab=files)
- [Below Zero](https://www.nexusmods.com/subnauticabelowzero/mods/128?tab=files)

[^immediate]: Please be noticed, that as how new world streaming now works terrain modification is not immediate like was is very older versions of SN1 game. It's not immediate because after terrain modification it needs to rebuild meshes from save files first to render them in the game, which is CPU intensive task, causing the game to "freeze" otherwise. To prevent that freezing it uses new terrain streamers to allow rebuild in their worker threads instead, which does not impact main thread where game "renders". For convenience mod shows message which indicates, that meshes rebuilding is in progress and it hides once new terrain meshes is finally visible in the game. There is still some other potential freezing could occur but that's usually noticeable when doing large terrain edits.

[^overlaps]: It uses bounding boxes previously added by devs to base modules yet for some modules (such as moonpool) have missing them. Try to construct corridor first to "dig" deeper, then try again constructing moonpool. It must pass moonpool connector area, where bounding boxes actually are missing.

[^falls]: For some reason game does not detect collisions after rebuilding terrain meshes. Jumping right before finishing rebuilding meshes could help to avoid that.

[^freezes]: That normal for a pre-build phase for save files which unfortunately does not have multithreading implemented by game devs (because obvious reasons). Try to do small terrain edits if you are not okay with freezing.

[^invisible]: That happens when new terrain edits is issued while meshes building of previous terrain edits is in progress (should be rebuilding message visible). That was already prevented in repulsion cannon and terraformer but not "dig #" command. While meshes rebuilding is in progress terrain edits caused by repulsion cannon is being discarded and terraformer functionality is blocked until meshes rebuilding is finished (rebuilding message is not visible anymore). Try to save/reload a game.
