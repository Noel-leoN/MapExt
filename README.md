# Cities Skylines 2 Map Extended Mod

## Introduction

- 57km^2 Extended Map (4x4 the size of the vanilla map)
- 229km^2 Extended Map (16x16 size)

## Requirements

- Game version 1.2.5f1.
- BepInEx 5.4.21

## Install
- install BepInEx 5.4.21 to you game root, run the game once then exit.
- download and unzip the Release zip file, then you'll get MapExt.PDX folder(2 file) and MapExt.Patch folder(1 files).
- copy MapExt.PDX folder to game local pdx mod folder. (usually located in: Users\youraccountname\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods)
- copy MapExt.Patch folder to gameroot\BepInEx\patchers folder. (not \plungins !)



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


## Changelog

  
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
