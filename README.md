# Cities Skylines 2 Map Extended Mod (Beta)

## Introduction
- 57km^2 Extended Map (16x the size of the vanilla map)
- 229km^2 Extended Map (256x size)

## Requirements
- BepInEx 5.4.21

## Install
- install BepInEx 5.4.21 to you game root, run the game once then exit.
- download and unzip the Release zip file, then you'll get MapExt.Patch folder(1 file) and MapExt.PDX folder(3 files).
- copy MapExt.Patch folder to gameroot\BepInEx\patchers folder. (not \plungins !)
- (optional, only required for some simulation fix) copy MapExt.PDX folder to game local pdx mod folder. (usually located in: Users\youraccountname\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods)

## Usage
For 57km^2 version(stable):
- create map in game editor manually or import 57.344km heightmap and 229.376km worldmap. (or any size you want but it scales)

For 229km^2 version(under test):
- create map in game editor manually or import 229.376km heightmap and 917.504km worldmap(recommand only use heightmap for better performance). (or any size you want but it scales)

Currently 4096x4096(stable) 16bit grayscale terrain image (PNG or TIFF) are supported.(8192 causes bugy water rendering,16384 totally not work) 

## Caution 
It's recommanded to select initial starting tiles before you save map in Editor or it will crash when you loaded a saved map in Editor.
(This is a vanilla bug that may have been fixed since version 1.1.8f)

## Compatibility
The mod uses preprocessing to patch static constants , so they don't theoretically cause other mod conflicts.
(not using PDX mods is deferred loading, so they can't be achieved without compromising compatibility and performance for the time being)

## Changelog
- 1.0.5.0
    - Theoretically its a relatively stable version.    
    - Deleted a lot of unnecessary codes
    - The PDX part of mod is only a fix for a coupling flaw in vanilla where the noise pollution effect amplifies as the mapsize expands. Even if you don't use it, it will not affect gameplay at all, except that citizen happiness will show a high noise warning. Consider using mods like City Control to disable these notice, as happiness is very easy to achieve.

- 1.0.3.0
    - Compatible with 1.2.0f1
	- Only 4096x4096 / 16bit grayscale PNG or TIFF format of heightmap is supported.

- 1.0.2.5 
	- Minor fix.

- 1.0.2.3 (57km or 229km with 8k resolution version)
	- fix water system rendering bug in 8k heightmap resoluton version.
	- change the total number of maptiles to 529 (8463 in 229km). (4x size of vanilla each maptile)

- 1.0.0
	- Original version.  
  
## Notes
 - Bugs with all vanilla maps, and you have to use a custom 57km*57km(or larger) map.

## Issues
- May not be compatible with mods that specific to the 14336m mapsize developed, such as Water Feature. Hopefully the author will change the map size constant to a dynamically fetched variable.
- Repeatedly replicate the overlayinfomation of the playable area to the scope of the world map, its a vanilla bug, hasn't been fixed yet, so please ignore it for now, or don't use too much zoom out.
- If you found issues please report in github, thank you.

## Disclaimer
- it's experimental. SAVE YOUR GAME before use this. Please use at your own risk.

## Build Tips
- If you want to build your own version, such as modifying larger or smaller map limits, it is recommended that you first use the Bepinex dump to get the "prepatched" dll, and then replace the reference in your PDX project.

## Credits
- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community.
- [Discord](https://discord.gg/ABrJqdZJNE): Cities 2 Modding (in testing channel https://discord.com/channels/1169011184557637825/1252265608607961120)
- Thanks  Rebeccat, HideoKuze2501, Nulos, Jack the Stripper,Bbublegum/Blax (in no particular order) and other good people who are not mentioned above for the test!
