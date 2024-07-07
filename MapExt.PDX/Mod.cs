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
//using HarmonyLib;
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

            /*
            // enable harmony patches;
            var harmony = new Harmony(harmonyID);
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            }
            */

            ///系统替换&重用；
            //Disable vanilla systems & enable custom systems；

            ///地图系统；
            //MapTileSystem;
            ///Postfix mode;
            updateSystem.UpdateAfter<PostDeserialize<Systems.MapTileSystem>, PostDeserialize<Game.Areas.MapTileSystem>>(SystemUpdatePhase.Deserialize);

            //AreaToolSystem;
            ///Postfix mode;
            updateSystem.UpdateAfter<Systems.AreaToolSystem, Game.Tools.AreaToolSystem>(SystemUpdatePhase.ToolUpdate);

            //TerrainSystem;WaterSystem;
            ///disabled if using preloader patcher;

            //CellMapSystem<T>;
            ///Prefix；
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AirPollutionSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<AirPollutionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AirPollutionSystem>();
            //updateSystem.UpdateAfter<Systems.AirPollutionSystem, AirPollutionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.AirPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AvailabilityInfoToGridSystem>();
            //updateSystem.UpdateAfter<Systems.AvailabilityInfoToGridSystem, AvailabilityInfoToGridSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.AvailabilityInfoToGridSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GroundPollutionSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<GroundPollutionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.GroundPollutionSystem>();
            updateSystem.UpdateAt<MapExt.Systems.GroundPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GroundWaterSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<GroundWaterSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.GroundWaterSystem>();
            updateSystem.UpdateAt<MapExt.Systems.GroundWaterSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NaturalResourceSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<NaturalResourceSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.NaturalResourceSystem>();
            updateSystem.UpdateAt<MapExt.Systems.NaturalResourceSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NoisePollutionSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<NoisePollutionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.NoisePollutionSystem>();
            //updateSystem.UpdateAfter<Systems.NoisePollutionSystem, NoisePollutionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.NoisePollutionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PopulationToGridSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<PopulationToGridSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.PopulationToGridSystem>();
            //updateSystem.UpdateAfter<Systems.PopulationToGridSystem, PopulationToGridSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.PopulationToGridSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SoilWaterSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<SoilWaterSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.SoilWaterSystem>();
            //updateSystem.UpdateAfter<PostDeserialize<Systems.SoilWaterSystem>, PostDeserialize<SoilWaterSystem>>(SystemUpdatePhase.Deserialize);
            updateSystem.UpdateAfter<PostDeserialize<MapExt.Systems.SoilWaterSystem>>(SystemUpdatePhase.Deserialize);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TelecomCoverageSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<TelecomCoverageSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.TelecomCoverageSystem>();
            //updateSystem.UpdateAfter<Systems.TelecomCoverageSystem, TelecomCoverageSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.TelecomCoverageSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TerrainAttractivenessSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.TerrainAttractivenessSystem>();
            //updateSystem.UpdateAfter<Systems.TerrainAttractivenessSystem, TerrainAttractivenessSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.TerrainAttractivenessSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficAmbienceSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.TrafficAmbienceSystem>();
            updateSystem.UpdateAt<MapExt.Systems.TrafficAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WindSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<WindSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.WindSystem>();
            updateSystem.UpdateAfter<MapExt.Systems.WindSystem, WindSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<MapExt.Systems.WindSystem, WindSystem>(SystemUpdatePhase.EditorSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ZoneAmbienceSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<WindSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.ZoneAmbienceSystem>();
            updateSystem.UpdateAt<MapExt.Systems.ZoneAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LandValueSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<LandValueSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.LandValueSystem>();
            //updateSystem.UpdateAfter<Systems.LandValueSystem, LandValueSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MapExt.Systems.LandValueSystem>(SystemUpdatePhase.GameSimulation);


            ///Cell ref Sys;                 
            updateSystem.World.GetOrCreateSystemManaged<AttractionSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AttractionSystem>();
            updateSystem.UpdateAt<MapExt.Systems.AttractionSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AudioGroupingSystem>().Enabled = false;
            updateSystem.World.GetOrCreateSystemManaged<AudioGroupingSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.AudioGroupingSystem>();
            updateSystem.UpdateAt<MapExt.Systems.AudioGroupingSystem>(SystemUpdatePhase.Modification2);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CarNavigationSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.CarNavigationSystem>();
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
            //updateSystem.UpdateAfter<Systems.WindSimulationSystem, WindSimulationSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAfter<Systems.WindSimulationSystem, WindSimulationSystem>(SystemUpdatePhase.EditorSimulation);

            updateSystem.World.GetOrCreateSystemManaged<ZoneSpawnSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MapExt.Systems.ZoneSpawnSystem>();
            updateSystem.UpdateAt<MapExt.Systems.ZoneSpawnSystem>(SystemUpdatePhase.GameSimulation);


        }


        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            Instance = null;

            // un-Harmony;
            //var harmony = new Harmony(harmonyID);
            //harmony.UnpatchAll(harmonyID);

            // un-Setting;
            //if (Setting != null)
            //{
            //    Setting.UnregisterInOptionsUI();
            //    Setting = null;
            //}
        }

    }
}
