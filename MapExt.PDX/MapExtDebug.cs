using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapExt
{
    /*
    ///CellMapSystem引用列表；v1.1.5版本共25个系统(不含cellmapsys自身)；
    ///格式为namespace + class + method + isburst + modmethod;
    ///
    namespace Debug:2   
        class BuildableAreaDebugSystem //nobc，nomod;
                BuildableAreaGizmoJob CellMapSystem<NaturalResourceCell>.GetCellCenter 
                OnUpdate this.m_NaturalResourceSystem.AddReader(jobHandle);

        class WindDebugSystem //nobc，nomod;
                WindGizmoJob CellMapSystem<Wind>.kMapSize
                OnUpdate jobData.m_WindMap = this.m_WindSimulationSystem.GetCells(out var deps);
                         this.m_WindSimulationSystem.AddReader(jobHandle);

    namespace Simulation(is CellMapSystem<T>):8 of 15 mapsize ref in bcjob;
        class AirPollutionSystem  //!!!mapsize ref in bcjob!!!需替换系统和所有public method引用，下同；
                ///显调用；
                AirPollutionMoveJob GetCellCenter
                                    WindSystem.GetWind
                GetCellCenter   //CellMapSystem继承，下同；
                GetPollution    //独特public method被其它系统引用，下同；
                OnUpdate    this.m_WindSystem.GetMap  //仅列出引用其它系统method，下同；
                ///隐调用；
                GetMap:  DebugSystem/PollutionDebugSystem/BuildingPollutionAddSystem/CitizenHappinessSystem/CitizenPathfindSetup/HouseholdFindPropertySystem/LandValueSystem/NetPollutionSystem/PollutionTriggerSystem/RentAdjustSystem
                GetCellCenter:  PollutionDebugSystem
                AddReader:  PollutionDebugSystem/OverlayInfomodeSystem/BuildingPollutionAddSystem/CitizenHappinessSystem/LandValueSystem/NetPollutionSystem/PollutionTriggerSystem/RentAdjustSystem
                GetData: OverlayInfomodeSystem
                GetPollution    CitizenHappinessSystem/LandValueSystem/
                GetPollution    ObjectPolluteSystem//in bcjob!!!
                
                

        class AvailabilityInfoToGridSystem  //!!!mapsize ref in bcjob！！！
                AvailabilityInfoToGridJob GetCellCenter
                GetCellCenter
                GetAvailabilityInfo
                OnUpdate 

        class GroundPollutionSystem  //nobc, nomod;
                GetCellCenter
                GetPollution

        class GroundWaterSystem     //nobc, nomod;
                GetCellCenter
                TryGetCell    //private
                GetGroundWater
                ConsumeGroundWater

        class LandValueSystem   //!!!mapsize ref in bcjob！！！注意，引用多个同cellmap子类method，可能需要逐一重写引用；
                LandValueMapUpdateJob   //多个cellmap子类自有method;
                GetCellCenter
                GetCellIndex
                OnUpdate    //多个cellmap子类GetMap/AddReader;

        class NaturalResourceSystem //nobc, nomod;
                ResourceAmountToArea
                OnUpdate    this.m_GroundPollutionSystem.AddReader
                            this.m_GroundPollutionSystem.GetData
        
        class NoisePollutionSystem  //nobc
                GetCellCenter
                GetPollution

        class PopulationToGridSystem    //!!!mapsize ref in bcjob！！！
                PopulationToGridJob     CellMapSystem<PopulationCell>.GetCell
                GetCellCenter
                GetPopulation
                
        class SoilWaterSystem    //!!!mapsize ref in bcjob！！！
                SoilWaterTickJob    CellMapSystem<SoilWater>.kMapSize
                GetCellCenter
                GetSoilWater               
        
        class TelecomCoverageSystem     //!!!mapsize ref in bcjob！！！
                TelecomCoverageJob  AddNetworkCapacity/CalculateSignalStrength/...
                //特殊sys，无其他调用；
                
        class TerrainAttractivenessSystem   //!!!mapsize ref in bcjob！！！
                TerrainAttractivenessPrepareJob     
                TerrainAttractivenessJob
                GetCellCenter
                EvaluateAttractiveness  //注意重载方法；
                GetAttractiveness   
                OnUpdate    this.m_ZoneAmbienceSystem.GetData/AddReader

        class TrafficAmbienceSystem  //nobc
                GetCellCenter
                GetTrafficAmbience2

        class WindSystem    //!!!mapsize ref in bcjob！！！
                WindCopyJob
                GetCellCenter //not ref;
                GetWind     //ref by 3 sys
                SetDefaults     //ref by 2 sys
                OnUpdate    this.m_WindSimulationSystem.GetCells

        class ZoneAmbienceSystem    //nobc
                GetCellCenter   //audiogroup
                GetZoneAmbienceNear     //audiogroup
                GetZoneAmbience

        class TelecomPreviewSystem    //nobc
                OnUpdate    //TelecomCoverageSystem.GetMap;
                //调用TelecomCoverageSystem的job；为TelecomCoverageSystem的同类；


    namespace Simulation or others (non CellMapSystem<T> but directly ref CellMapSystem):
        class BuildingPollutionAddSystem  //nobc,nomod;
                OnUpdate    CellMapSystem<GroundPollution>.kMapSize
                            CellMapSystem<AirPollution>.kMapSize
                            CellMapSystem<NoisePollution>.kMapSize

        class CarNavigationSystem   //!!!mapsize ref in bcjob!!!
                                    //注意本系统虽然为独立系统，但禁用会导致无log不明跳出，估计是bcjob问题；
                ApplyTrafficAmbienceJob  CellMapSystem<TrafficAmbienceCell>.GetCell
                OnUpdate    ApplyTrafficAmbienceJob
                                m_TrafficAmbienceSystem.GetMap
                                m_TrafficAmbienceSystem.AddWriter                                
                //考虑harmony OnUpdate；

        class NetPollutionSystem    //!!!mapsize ref in bcjob!!!独立系统可替换；
                UpdateNetPollutionJob    private void AddAirPollution
                                         private void AddNoise
                OnUpdate    CellMapSystem<AirPollution>.kMapSize
                            this.m_AirPollutionSystem.GetMap/AddWriter
                            this.m_NoisePollutionSystem.GetMap/AddWriter

         class SpawnableAmbienceSystem   //!!!mapsize ref in bcjob！！！
                 SpawnableAmbienceJob    CellMapSystem<ZoneAmbienceCell>.GetCell/kMapSize
                 OnUpdate    this.m_ZoneAmbienceSystem.GetMap/AddWriter
        
         class WindSimulationSystem    //!!!mapsize ref in bcjob！！！  
                 UpdateWindVelocityJob
                 UpdatePressureJob                 
                 DebugSave
                 DebugLoad
                 SetDefaults
                 SetWind
                 GetCenterVelocity  //WindSystem;
                 GetCellCenter
                 GetCells

          class ApplyBrushesSystem    //nobc
                  private ApplyCellMapBrush  CellMapSystem<TCell>   //ref by OnUpdate

          class ResourcePanelSystem   //no bc
                  private void ApplyTexture
                  private void ClearMap<TCell>

          class TempExtractorTooltipSystem  //nobc
                  private bool FindResource  //ref by OnUpdate
                  OnUpdate  
                  
    namespace Other Systems(ref Cell<T> in bcjob!)
            class ObjectPolluteSystem
                    ObjectPolluteJob    AirPollutionSystem.GetPollution/GroundPollutionSystem.GetPollution
                    OnUpdate    this.m_AirPollutionSystem./m_GroundPollutionSystem.GetMap/AddReader

            
            





          

                 




    


    */


    /// <summary>
    /// 辅助debug排查系统
    /// </summary>
    /// 

    internal class ExtMapDebug
    {




    }
}
