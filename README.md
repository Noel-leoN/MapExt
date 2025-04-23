# Cities Skylines 2 Map Extended Mod

## Introduction

- 57km^2 Extended Map (4x4 the size of the vanilla map)
- 229km^2 Extended Map (16x16 size)

## Requirements

- Game version 1.2.5f1.
- BepInEx 5.4.21

## Install

- 1. Install BepInEx 5.4.21 x64(https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) in the game root directory, run the game once then exit.
  Here is the detailed BepInEx installation introduction.(https://docs.bepinex.dev/articles/user_guide/installation/index.html)
- 2. download and unzip the latest Release zip file(https://github.com/Noel-leoN/MapExt/releases), then get 2 folders: patchers, plugins.
- 3. copy these 2 folders to yourgameroot/BepInEx (DO NOT change the name of the folders and files)
- 4. If correct, you'll get a folder structure like this:
	|- yourgameroot
	        |--- BepInEx
					|---plugins
                    |      |---MapExtPDX
	                |              |---MapExtPDX_win_x86_64.dll
	                |              |---MapExtPDX.dll
	                |---patchers
	                       |---MapExtPatcher
	                               |---MapExtPatcher.dll
     now installation complete.
- To Uninstall or befor reinstall the mod just delete these 3 files.

## Usage
For 57km^2 version(more stable):
- create map in game editor manually to import 57.344km heightmap and 229.376km worldmap. (it's 1:1 scale, or any size you want but it scales)

For 229km^2 version(under test):
- create map in game editor manually to import 229.376km heightmap and 917.504km worldmap(recommand only use heightmap for better performance). (or any size you want but it scales)

Currently 4096x4096 16bit grayscale terrain image (PNG or TIFF) are supported.(8192 causes bugy water rendering,16384 totally not work) 

## Caution 
- Bugs with all vanilla maps, and you have to use a custom 57km*57km(or larger) map.
  Due to the change in terrain height ratio, do not use vanilla game saves to play, otherwise existing buildings will have visual errors.

- If you have an earlier version installed, be sure to delete all directories and files, including BepInEx/patcher/MapEx and local PDX mods/MapExt.PDX

## Code implementation & Compatibility description for modders
The main patching logic uses MonoCecil to patch static fields (which is very difficult to do with Harmony and other methods), and IL replaces and patches the method that calls the BurstJob of the relevant static fields that have been inlined. 
Theoretically, except for a slight delay in starting to enter the game logo screen, the performance in the game will not be affected. 
In terms of compatibility, since it was patched before the PDX mod was loaded, it shouldn't cause crash conflicts.
The new code mechanism implements the use of the MonoCeCil generic helper, so that the replacement of a BurstJob only requires changing the name list

## Changelog
- 1.1
  more complete simulation system (adding repair vehicle navigation, customizable AreaTool map tiles, etc.) 
  Rewritten the patch code significantly, now you only need to install it in the BepInEx directory, no need to install the PDX local mod part.
  
## Notes


## Issues
- May not be compatible with some special mods.
- Repeatedly replicate the overlayinfomation of the playable area to the scope of the world map, its a vanilla bug, hasn't been fixed yet, so please ignore it for now, or don't use too much zoom out.
- a few simulation systems may not be working properly,such as water pumping/tempwater powerstation.
- Water Feature Mod needs to override the "mapextend" constant specified inside it in order to work properly.
- If you found issues please report in github, thank you.

## Disclaimer
- SAVE YOUR GAME before use this mod. Please use at your own risk.

## Build Tips
- If you want to build your own version, such as modifying larger or smaller mapsize limits, it is recommended that first use the Bepinex dump to get the "prepatched" dll, and then replace the reference in your PDX project.

## Credits
- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 BepInEx mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community.
- [Discord](https://discord.gg/ABrJqdZJNE): Cities 2 Modding (in testing channel https://discord.com/channels/1169011184557637825/1252265608607961120)
- Thanks  Rebeccat, HideoKuze2501, Nulos, Jack the Stripper,Bbublegum/Blax (in no particular order) and other good people who are not mentioned above for the test!
