# Subnautica Terraforming Mod
Terraforming modification for PC games running on Unity Engine - Subnautica and it's expansion Below Zero.

## Overview

Modification features voxel-based **terrain editing game mechanic** which was previously cut by game devs. This mod not only just restores the mechanic but also seamlessly **intergrates into multithreaded terrain streaming system** implemented by game devs here for terrain updates **on-demand**, ensuring stability of game experience.

Terrain editing can be performed by various gameplay tools, such as:
 - Terraformer (which is also was cut);
 - Builder (used by player to build modular habitats);

This mod adds option for habitat builders to remove some overlapping terrain (and structures) to fit modules to their desired locations. Every module have construction bounding boxes used for checking obstructions such as terrain, and mod uses them for removal of terrain when necessary.

## Solution structure

Solution contains 3 projects: 1 containing shared logic for both games and 2 is class library projects used for targeting each game separately.

Class libraries are meant to be loaded by mod loaders: 
 - **BepInEx** (general mod loader supporting almost all Unity versions of shipped games running on)
 - or **QModManager** (Subnautica specific, _obsolete_).
