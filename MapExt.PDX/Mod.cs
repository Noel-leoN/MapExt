using System;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
using Game.Serialization;
using Game.Audio;
using Game.Rendering;
using Game.UI.Tooltip;
using Game.Tools;


namespace MapExt
{
    public class Mod : IMod
    {
        public const string ModName = "MapExt57km";
        public const string ModNameCN = "16倍扩展地图57km";

        public static Mod Instance { get; private set; }

        public static ExecutableAsset ModAsset { get; private set; }

        public static readonly string harmonyID = ModName;

        // log init;
        public static ILog log = LogManager.GetLogger($"{ModName}").SetShowsErrorsInUI(false);

        public static void Log(string text) => log.Info(text);

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;

            // Log;
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"{asset.name} v{asset.version} mod asset at {asset.path}");
            ModAsset = asset;

            
            // enable harmony patches;
            var harmony = new Harmony(harmonyID);
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            }


            ///系统替换&重用；
            //Disable vanilla systems & enable custom systems；

            ///地图系统；
            //MapTileSystem;
            ///Postfix mode;
            //updateSystem.World.GetOrCreateSystemManaged<Game.Areas.MapTileSystem>().Enabled = false;
            updateSystem.UpdateAfter<PostDeserialize<Systems.MapTileSystem>, PostDeserialize<Game.Areas.MapTileSystem>>(SystemUpdatePhase.Deserialize);

            //AreaToolSystem;
            ///Postfix mode;
            //updateSystem.World.GetOrCreateSystemManaged<AreaToolSystem>().Enabled = false;
            updateSystem.UpdateAfter<Systems.AreaToolSystem, Game.Tools.AreaToolSystem>(SystemUpdatePhase.ToolUpdate);

            //TerrainSystem;
            ///disabled if using preloader patcher;
            ///

            //Water-related Systems
            ///should be compiled with modified gamedll;
            ///下列系统被其他mod占用可能性不高；
            updateSystem.World.GetOrCreateSystemManaged<FloodCheckSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.FloodCheckSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<WaterDangerSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.WaterDangerSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<WaterLevelChangeSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.WaterLevelChangeSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<WeatherAudioSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.WeatherAudioSystem>(SystemUpdatePhase.Modification2);

            updateSystem.World.GetOrCreateSystemManaged<WaterSourceInitializeSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.WaterSourceInitializeSystem>(SystemUpdatePhase.ModificationEnd);

            //CellMapSystem<T>;
            ///Prefix；
            ///多数系统被其他mod占用可能性不高，除了LandValueSystem;
            updateSystem.World.GetOrCreateSystemManaged<AirPollutionSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.AirPollutionSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.AvailabilityInfoToGridSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<GroundPollutionSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.GroundPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<GroundWaterSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.GroundWaterSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<NaturalResourceSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.NaturalResourceSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<NoisePollutionSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.NoisePollutionSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<PopulationToGridSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.PopulationToGridSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<SoilWaterSystem>().Enabled = false;
            updateSystem.UpdateAfter<PostDeserialize<MapExt.Systems.SoilWaterSystem>>(SystemUpdatePhase.Deserialize);

            updateSystem.World.GetOrCreateSystemManaged<TelecomCoverageSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.TelecomCoverageSystem>(SystemUpdatePhase.GameSimulation);

            ///注意由HeatmapPreviewSystem进行更新；
            //updateSystem.World.GetOrCreateSystemManaged<TelecomPreviewSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.TelecomPreviewSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.World.GetOrCreateSystemManaged<HeatmapPreviewSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.HeatmapPreviewSystem>(SystemUpdatePhase.ModificationEnd);

            updateSystem.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.TerrainAttractivenessSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.TrafficAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<WindSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.WindSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.WindSystem>(SystemUpdatePhase.EditorSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.ZoneAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.World.GetOrCreateSystemManaged<LandValueSystem>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.LandValueSystem>(SystemUpdatePhase.GameSimulation);


            ///Cell ref Sys;                 
            updateSystem.World.GetOrCreateSystemManaged<AttractionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AttractionSystem>();
            updateSystem.UpdateAt<MapExt.Systems.AttractionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AudioGroupingSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<AudioGroupingSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AudioGroupingSystem>();
            updateSystem.UpdateAt<MapExt.Systems.AudioGroupingSystem>(SystemUpdatePhase.Modification2);


            ///!The chances of this system being taken up by other mods are very high!;
            ///Consider using Harmony Transpiler rewrites that one job ref for better compatibility
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CarNavigationSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.CarNavigationSystem>();
            updateSystem.World.GetOrCreateSystemManaged<CarNavigationSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<CarNavigationSystem.Actions>().Enabled = false;
            updateSystem.UpdateAt<MapExt.Systems.CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<MapExt.Systems.CarNavigationSystem.Actions, MapExt.Systems.CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.CarNavigationSystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateAfter<MapExt.Systems.CarNavigationSystem.Actions, MapExt.Systems.CarNavigationSystem>(SystemUpdatePhase.LoadSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GroundWaterPollutionSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<GroundWaterPollutionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.GroundWaterPollutionSystem>();
            updateSystem.UpdateAt<MapExt.Systems.GroundWaterPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LandValueTooltipSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<LandValueTooltipSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.LandValueTooltipSystem>();
            updateSystem.UpdateAt<MapExt.Systems.LandValueTooltipSystem>(SystemUpdatePhase.UITooltip);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NetColorSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<NetColorSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.NetColorSystem>();
            updateSystem.UpdateAt<MapExt.Systems.NetColorSystem>(SystemUpdatePhase.Rendering);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NetPollutionSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<NetPollutionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.NetPollutionSystem>();
            updateSystem.UpdateAt<MapExt.Systems.NetPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ObjectPolluteSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<ObjectPolluteSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.ObjectPolluteSystem>();
            updateSystem.UpdateAt<MapExt.Systems.ObjectPolluteSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SpawnableAmbienceSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<SpawnableAmbienceSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.SpawnableAmbienceSystem>();
            updateSystem.UpdateAt<MapExt.Systems.SpawnableAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WindSimulationSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<WindSimulationSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.WindSimulationSystem>();
            updateSystem.UpdateAt<MapExt.Systems.WindSimulationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.WindSimulationSystem>(SystemUpdatePhase.EditorSimulation);

            updateSystem.World.GetOrCreateSystemManaged<ZoneSpawnSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.ZoneSpawnSystem>();
            updateSystem.UpdateAt<MapExt.Systems.ZoneSpawnSystem>(SystemUpdatePhase.GameSimulation);

            ///bc job ref static method (indirect calls mapsize)
            ///not sure effect;
            ///
            //updateSystem.World.GetOrCreateSystemManaged<PowerPlantAISystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.PowerPlantAISystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<TempWaterPumpingTooltipSystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.TempWaterPumpingTooltipSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem.World.GetOrCreateSystemManaged<WaterPumpingStationAISystem>().Enabled = false;
            //updateSystem.UpdateAt<MapExt.Systems.WaterPumpingStationAISystem>(SystemUpdatePhase.GameSimulation);


        }


        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            Instance = null;

            // un-Harmony;
            var harmony = new Harmony(harmonyID);
            harmony.UnpatchAll(harmonyID);

            // un-Setting;
            //if (Setting != null)
            //{
            //    Setting.UnregisterInOptionsUI();
            //    Setting = null;
            //}
        }

    }
}
