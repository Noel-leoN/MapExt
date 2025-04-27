# Cities Skylines 2 Map Extended Mod

## Introduction

- 57km^2 Extended Map (4x4 the size of the vanilla map)
- 229km^2 Extended Map (16x16 size)

## Requirements

- Game version 1.2.5f1.
- BepInEx 5.4.21

## Install
1. Install BepInEx 5.4.21 x64(https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) in the game root directory, run the game once then exit.
Here is the detailed BepInEx installation introduction.(https://docs.bepinex.dev/articles/user_guide/installation/index.html)
2. download and unzip the Release zip file, then get MapExtPDX folder(2 file) and MapExtPatch folder(1 files).
3. copy MapExtPDX folder to game local pdx mod folder. (%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods)
4. copy MapExtPatch folder to gameroot\BepInEx\patchers folder. (not \plungins !)
 
## Usage
For 57km^2 version(more stable):
- create map in game editor manually to import 57.344km heightmap (229.376km worldmap is optional) . (it's 1:1 scale, or any size you want but it scales)

For 229km^2 version(under test):
- create map in game editor manually to import 229.376km heightmap (optional 917.504km worldmap, but not recommand because of performance drop). (or any size you want but it scales)

Supported terrain image format: 4096x4096 16bit grayscale terrain image (PNG or TIFF) .

## Caution 
- Bugs with all vanilla maps. You have to use a custom map.
  Due to the change in terrain height ratio, DO NOT use vanilla game saves to play, otherwise existing buildings will have visual errors.

- If you have an earlier version installed, be sure to delete all directories and files, including BepInEx/patcher/MapExt and local PDX mods/MapExt.PDX

## Code implementation & Compatibility description for modders
- The main patching logic uses BepinEx MonoCecil Preloader to patch static fields (which is very difficult to do with ECS+Harmony and other methods), and then uses Harmony IL replaces and patches the method that calls the BurstJob of the relevant static fields that have been inlined.
- Interestingly, they have built-in support for BepInEx and Harmony in the vanilla game code. It's not quite clear why some people are against BepInEx for this game.
- Made a Harmony transpiler universal helper tool to great easily replace Burst Jobs and make future maintenance very simple.
- Theoretically, except for a slight delay in starting to enter the game logo screen, the performance in the game will not be much affected. 

## Changelog
- v1.2
  1. more complete simulation system (adding repair vehicle navigation, customizable AreaTool map tiles, etc.) 
  2. rewritten the patch code significantly,it might boost performance a bit.
  If you encounter any problems, you can roll back to version 1.0.5.6
  
## Notes
ººÓïËµÃ÷

## Issues
- May not be compatible with some special mods.
- Repeatedly replicate the overlayinfomation of the playable area to the scope of the world map, its a vanilla bug, hasn't been fixed yet, so please ignore it for now, or don't use too much zoom out.
- a few simulation systems may not be working properly,such as water pumping/tempwater powerstation.
- Water Feature Mod needs to override the "mapextend" constant specified inside it in order to work properly.(now The latest beta version is working fine. )
- If you found issues please report in github, thank you.

## Disclaimer
- SAVE YOUR GAME before use this mod. Please use at your own risk.

## Credits
- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 BepInEx mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community.
- [Discord](https://discord.gg/ABrJqdZJNE): Cities 2 Modding (in testing channel https://discord.com/channels/1169011184557637825/1252265608607961120)
- Thanks  Rebeccat, HideoKuze2501, Nulos, Jack the Stripper,Bbublegum/Blax (in no particular order) and other good people who are not mentioned above for the test!
