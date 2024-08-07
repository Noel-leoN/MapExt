# Cities Skylines 2 Extended Map Mod (Beta)

## Introduction

- 57km^2 Extended Map (16x the size of the vanilla map)
- 229km^2 Extended Map (256x size)

## Requirements

- Game version 1.1.7f1.
- BepInEx 5.4.21

## Install

- install BepInEx 5.4.21 to you game root, run the game once then exit.
- download and unzip the Release zip file, then you'll get MapExt.PDX folder(1 file) and MapExt.Patch folder(5 files).
- copy MapExt.PDX folder to game local pdx mod folder. (usually located in: Users\youraccountname\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods)
- copy MapExt.Patch folder to gameroot\BepInEx\patchers folder. (not \plungins !)

## Usage
For 57km^2 version(more stable):
- create map in game editor manually or import 57.344km heightmap and 229.376km worldmap. (or any size you want but it scales)

For 229km^2 version(under test):
- create map in game editor manually or import 229.376km heightmap and 917.504km worldmap(recommand only use heightmap for better performance). (or any size you want but it scales)

Currently 4096x4096(more stable) or 8192x8192(test) 16bit grayscale terrain image (PNG or TIFF) are supported.(16384 not work currently) 

## Caution 
It's needed to select initial starting tiles before you save map in Editor or it will crash when you loaded a saved map in Editor.

## Compatibility
The current solution is mainly to replace the burst job system that references mapsize, which may not be compatible with other mods.
- Modifies:
	- maptile/areatool system.(postfix)
	- .cctor of water/terrain system.(preloader)
 	- some water-related systems.(replace)
	- some generic subclasses of cellmapsystem ,and most of the systems that reference these subsystems.(replace)

## Changelog
- 1.0.2.5 
	- Minor fix.

- 1.0.2.3 (57km or 229km with 8k resolution version)
	- fix water system rendering bug in 8k heightmap resoluton version.
	- change the total number of maptiles to 529 (8463 in 229km). (4x size of vanilla each maptile)

- 1.0.2.0
    - fix water-related systems.
  	- add 229km^2 version for test. 

- 1.0.1.1
	- Minor fix.

- 1.0.1.0 MapExt
	- Fix telecom heatmap.
	- Restore harmony patcher. (harmony is still the easiest way)
	- Add some simulation systems that may be necessary to modify.

- 1.0.0.1 for MapExt
	- Renamed rootnamespace to MapExt.
	- temporarily removed harmony patches.

- 1.0.7.6
	- Compatibility with game version 1.1.6f1.
	- Fix most of all simulation systems.
	- Remove some systems that don't need to be modified.
	- Adjust default heightmapscale from 4096 to 16384 (can be manually changed in Editor's import heightmap)

- 1.0.5
	- Compatibility with game version 1.1.5f1.
	- fix some simulation systems.

- 1.0.0
	- Original version.  
  
## Notes
 - Bugs with all vanilla maps, and you have to use a custom 57km*57km(or larger) map.

## Issues
- May not be compatible with some mods.
- Repeatedly replicate the overlayinfomation of the playable area to the scope of the world map, its a vanilla bug, hasn't been fixed yet, so please ignore it for now, or don't use too much zoom out.
- a few simulation systems may not be working properly,such as water pumping/tempwater powerstation.
- Water Feature Mod needs to override the "mapextend" constant specified inside it in order to work properly.
- If you found issues please report in github, thank you.

## Disclaimer

- it's experimental. SAVE YOUR GAME before use this. Please use at your own risk.

## Build Tips
- If you want to build your own version, such as modifying larger or smaller map limits, it is recommended that you first use the Bepinex dump to get the "prepatched" dll, and then replace the reference in your PDX project.
- Generally, no other specification versions will be made, anyone who is interested is welcome to remake it and release.

## Credits

- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community.
- [Discord](https://discord.gg/ABrJqdZJNE): Cities 2 Modding (in testing channel https://discord.com/channels/1169011184557637825/1252265608607961120)
- Thanks  Rebeccat, HideoKuze2501, Nulos, Jack the Stripper,Bbublegum/Blax (in no particular order) and other good people who are not mentioned above for the test!
