using System; // 使用BepInEx自带日志
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq; // 用于LINQ查询，例如在OtherPatching中使用
using BepInEx.Logging;
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;


public static class PreloaderPatcher
{
    // 修补程序集名称
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };
    // 可变更为其他程序集或","添加多个程序集； 注意mscorlib不能修补

    // 静态日志源，使用BepInEx的ManualLogSource
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MapExtPreloader");

    /// <summary>
    /// 核心倍率修改；
    /// 地图变更大小时本项目仅修改此值即可；另需要修改MapTile/AreaTool Execute();
    /// </summary>
    private const int CELL_FACTOR = 4;
    // Water_kCellSize=7f; 以下为 MapSize 倍率 / kCellSize：
    // 57344 = 4/28; 229376 = 16/112; 28672 = 2/14; 114688 = 8/56;
    // 超过229376误差过大暂不推荐
    // kMapSize = m_TexSize * kCellSize; m_TexSize默认2048，暂时应保持不变；

    private const float TERRAIN_NEW_SIZE = CELL_FACTOR * 14336f; 
    // TerrainSystem.kDefaultMapSize

    private const int CELL_NEW_SIZE = CELL_FACTOR * 14336; 
    // CellMapSystem<T>.kMapSize; WaterSystem.kMapSize

    /// <summary>
    /// Preloader Patcher主方法
    /// </summary>
    /// <param name="assembly"></param>
    // Patch方法：BepInEx Preloader会为每个加载的程序集调用此方法
    public static void Patch(ref AssemblyDefinition assembly)
    {
        // 只针对指定的程序集进行修改，例如Game.dll
        if (assembly.Name.Name != "Game") return; // 不需要dll后缀

        /// 公开化拟修补的BurstJob调用的私有结构体/方法，以便简化本机代码库，或便于Harmony版本编译调用
        /// 非必要步骤，可考虑在new burstjob中内置相关结构体/方法
        // 定义要修补成员的类型全名列表
        var typesToMakePublicNames = new List<string>
        {
            /// MapTile相关
            // "Game.Areas.MapTileSystem",
            // "Game.Tools.AreaToolSystem", // 仅用作DevUI/Simulation/AreaTool/MapTile工具
            /// WaterSystem相关
            "Game.Simulation.WaterSystem", // 集中处理
            "Game.Simulation.SurfaceDataReader", // 集中处理
            // "Game.Simulation.FloodCheckSystem",
            // "Game.Simulation.WaterDangerSystem",
            // "Game.Audio.WeatherAudioSystem",
            /// CellMapSystem<T>
            "Game.Simulation.AirPollutionSystem",
            "Game.Simulation.AvailabilityInfoToGridSystem",
            // "Game.Simulation.GroundPollutionSystem",
            // "Game.Simulation.GroundWaterSystem",
            "Game.Simulation.LandValueSystem",
            // "Game.Simulation.NaturalResourceSystem",
            // "Game.Simulation.NoisePollutionSystem",
            // "Game.Simulation.PopulationToGridSystem",
            // "Game.Simulation.SoilWaterSystem",
            "Game.Simulation.TelecomCoverageSystem",
            // "Game.Simulation.TelecomPreviewSystem",
            // "Game.Simulation.TerrainAttractivenessSystem",
            // "Game.Simulation.TrafficAmbienceSystem",
            // "Game.Simulation.WindSystem",
            // "Game.Simulation.ZoneAmbienceSystem",
            /// 其他class调用CellMapSystem<T>方法
            // "Game.Simulation.AttractionSystem",
            // "Game.Audio.AudioGroupingSystem", //！注意可能与ExtendedRadio冲突
            // "Game.Simulation.CarNavigationSystem",
            //// "Game.Simulation.CarNavigationSystem.Actions", // 嵌套类,修补逻辑已实现
            "Game.Simulation.CitizenHappinessSystem",
            // "Game.Simulation.GroundWaterPollutionSystem",
            // "Game.UI.Tooltip.LandValueTooltipSystem",
            //// "Game.Rendering.NetColorSystem", // 功能不大，job复杂
            //// "Game.Simulation.PowerPlantAISystem", //功能不大，job复杂
            "Game.Simulation.SpawnableAmbienceSystem",
            //// "Game.UI.Tooltip.TempRenewableElectricityProductionTooltipSystem", // 功能不大，job复杂
            //// "Game.UI.Tooltip.TempWaterPumpingTooltipSystem", // 功能不大，job复杂
            //// "Game.Simulation.WaterPumpingStationAISystem", // 功能不大，job复杂
            // "Game.Simulation.WindSimulationSystem",
            // "Game.Simulation.ZoneSpawnSystem"
        };

        // 存储被修补的类型列表
        var patchedTypes = new List<string>();

        // 遍历要修补的类型名称，找到并修补成员
        foreach (var typeName in typesToMakePublicNames)
        {
            var type = assembly.MainModule.GetType(typeName);
            if (type != null)
            {
                // 修改类型本身的访问修饰符为Public，如果不是Public
                if ((type.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public)
                {
                    type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
                    Logger.LogInfo($"公开化类型工具: {type.FullName}");
                }

                // 修补成员（包括嵌套类型、方法、字段、属性）
                MakeMembersPublic(type);

                // 添加到patchedTypes列表
                patchedTypes.Add(type.FullName);
            }
        }

        // 日志输出被修补的类型列表
        if (patchedTypes.Count > 0)
        {
            Logger.LogInfo($"已修补成员公开化: {string.Join(", ", patchedTypes)}");
        }
        else
        {
            Logger.LogInfo("未修补任何成员公开化.");
        }

        // 地形系统 kDefaultMapSize/Get 2 Methods
        TerrainSystemPatch(assembly);

        // 水系统 kMapSize/kCellSize
        WaterSystemPatch(assembly);

        // CellMap系统 kMapSize
        CellMapSystemPatch(assembly);

        // CellMap Burst Job 自动替换工具
        // 遍历所有定义的补丁目标
        foreach (var target in PatchTargets)
        {
            // 检查当前处理的程序集是否是此补丁目标所指定的程序集
            if (assembly.Name.Name == target.TargetAssemblyName)
            {
                Logger.LogInfo($"BurstJobReplacer尝试修补方法: {target.TargetTypeName}.{target.TargetMethodName}");
                // 如果匹配，则调用实际执行修改的辅助方法
                ApplySinglePatch(assembly, target);
            }
            else
            {
                //     // 可选：记录跳过不匹配的补丁目标
                Logger.LogDebug($"BurstJobReplacer未找到指定程序集方法 '{target.TargetTypeName}.{target.TargetMethodName}' '{assembly.Name.Name}'");
            }
        }
    }

    // 方法：修补指定类型的所有非public成员，跳过.ctor和.cctor
    private static void MakeMembersPublic(TypeDefinition type)
    {
        // 处理嵌套类型（包括类和结构），修改非public的为Public
        foreach (var nestedType in type.NestedTypes)
        {
            if ((nestedType.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NestedPublic)
            {
                nestedType.Attributes = (nestedType.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
            }
        }

        // 处理方法，修改非public的为Public，但跳过.ctor和.cctor
        foreach (var method in type.Methods)
        {
            if (method.Name == ".ctor" || method.Name == ".cctor") continue; // 跳过构造函数和静态构造函数
            if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            }
        }

        // 处理字段，修改非public的为Public
        foreach (var field in type.Fields)
        {
            if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public)
            {
                field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
            }
        }

        /*
        // 处理属性，修改get/set方法的访问修饰符为Public
        foreach (var property in type.Properties)
        {
            if (property.GetMethod != null && (property.GetMethod.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                property.GetMethod.Attributes = (property.GetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            }
            if (property.SetMethod != null && (property.SetMethod.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                property.SetMethod.Attributes = (property.SetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            }
        }
        */

        // 可以根据需要添加对事件的处理或其他成员
    }

    /// <summary>
    /// 修补地形系统
    /// </summary>
    /// <param name="assembly"></param>
    private static void TerrainSystemPatch(AssemblyDefinition assembly)
    {
        // 获取TerrainSystem Type
        var terrainSystem = assembly.MainModule.GetType("Game.Simulation.TerrainSystem");

        // 日志输出其他修补逻辑开始
        Logger.LogInfo($"地形系统正在修补{terrainSystem}...");

        if (terrainSystem != null)
        {
            /// 处理字段；TerrainSystem.kDefaultMapSize；不一定很必要，双保险以防内联；
            FieldDefinition kMapSizeField = terrainSystem.Fields.First(f => f.Name == "kDefaultMapSize");
            // 移除readonly修饰符(暂未赋值，int2)
            kMapSizeField.Attributes &= ~FieldAttributes.InitOnly;
            Logger.LogInfo($"地形系统修补字段{kMapSizeField}...");

            /// 修补静态构造函数TerrainSystem.kDefaultMapSize初始化
            /// 实为修补Finalization方法(仅被该方法实际调用，其他方法间接调用，Harmony方式仅用修补该方法即可)
            // 获取TerrainSystem.cctor;
            MethodDefinition terrainsys_cctor = terrainSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");
            Logger.LogInfo($"修补cctor {terrainsys_cctor}...");

            if (terrainsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in terrainsys_cctor.Body.Instructions)
                {
                    // kDefaultMapSize;
                    // 可考虑列举多种可能性57344/229376以免误将已修复dll重复修补而不生效
                    if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                    {
                        ins.Operand = TERRAIN_NEW_SIZE;
                        Logger.LogInfo("TerrainSystem kDefautMapSize set to " + ins.Operand);
                    }

                    /// 测试中!!!change heightmap resolution to 16kx16k or 8kx8k greysale from 4k； 设为8k时地形正常/水系统模拟不完整； 设为16k时崩溃
                    //if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 4096)
                    //{
                    //    ins.Operand = 16384;
                    //   ins.Operand = 8192;
                    //}

                    /// optional!!!
                    // kDefaultHeightScale; // 高度缩放，可在Editor中调整，无需修改
                    // if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 4096f)
                    // {
                    //ins.Operand = 8192f;
                    //Console.WriteLine("TerrainSys_DefaultHeightScale set to " + ins.Operand);
                    // }
                }
                // Add new instructions or logic as needed
            }
            Logger.LogInfo($"{terrainsys_cctor} 修补完成！");

            //TerrainSystem 2 public Methods;
            foreach (MethodDefinition method in terrainSystem.Methods)
            {
                //GetTerrainBounds;ref by 1 system
                if (method.Name == "GetTerrainBounds")
                {
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                        {
                            ins.Operand = TERRAIN_NEW_SIZE;
                            Logger.LogInfo("GetTerrainBounds set to " + ins.Operand);
                        }
                    }
                    // Add new instructions or logic as needed
                    Logger.LogInfo($"{method} 修补完成！");
                }

                //GetHeightData;ref by 大量 systems!!!;
                if (method.Name == "GetHeightData")
                {
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                        {
                            ins.Operand = TERRAIN_NEW_SIZE;
                            Logger.LogInfo("GetTerrainBounds set to " + ins.Operand);
                        }
                    }
                    // Add new instructions or logic as needed
                    Logger.LogInfo($"{method} 修补完成！");
                }

                ///测试中!!!影响TerrainMinMax Init;未知作用！
                /*
                if (method.Name == "FinalizeTerrainData")
                {
                    // Modify the content of the static constructor
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        //TerrainMinMaxInit;
                        if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 1024)
                        {
                            ins.Operand = 4096;
                        }

                        if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 512)
                        {
                            ins.Operand = 2048;
                        }
                    }
                    // Add new instructions or logic as needed
                    logSource.LogInfo($"target method {method} for patching");
                }*/
            }
        }

        ModuleDefinition module = assembly.MainModule;
        //防御性防止内联；
        CustomAttribute nonVerAttr = new(module.ImportReference(typeof(System.Runtime.CompilerServices.MethodImplAttribute).GetConstructor(new[] { typeof(MethodImplOptions) })));
        nonVerAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(typeof(MethodImplOptions)), (int)(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)));
        terrainSystem.CustomAttributes.Add(nonVerAttr);
    }

    /// <summary>
    /// 修补水模拟系统
    /// </summary>
    /// <param name="assembly"></param>
    /// 
    private static void WaterSystemPatch(AssemblyDefinition assembly)
    {
        // 获取WaterSystem Type
        var waterSystem = assembly.MainModule.GetType("Game.Simulation.WaterSystem");

        // 日志输出其他修补逻辑开始
        Logger.LogInfo($"水系统正在修补{waterSystem}...");

        if (waterSystem != null)
        {
            /// 处理字段；不一定很必要，双保险以防内联；
            // WaterSystem.kMapSize字段修改；
            FieldDefinition kMapSizeField = waterSystem.Fields.First(f => f.Name == "kMapSize");
            // 移除readonly修饰符并赋值
            kMapSizeField.Attributes &= ~FieldAttributes.InitOnly;
            kMapSizeField.InitialValue = BitConverter.GetBytes(CELL_NEW_SIZE);
            Logger.LogInfo($"正在修补字段WaterSystem.kMapSize为 ...{kMapSizeField.Constant}");

            // WaterSystem.kCellSize字段修改；
            FieldDefinition kCellSizeField = waterSystem.Fields.First(f => f.Name == "kCellSize");
            // 移除readonly修饰符 
            kCellSizeField.Attributes &= ~FieldAttributes.InitOnly;
            kCellSizeField.InitialValue = BitConverter.GetBytes(7f * CELL_FACTOR);
            Logger.LogInfo($"水系统正在修补字段WaterSystem.kCellSize为 ...{kCellSizeField.Constant}");

            /// 修补静态构造函数WaterSystem.kMapSize初始化
            // 获取WaterSystem.cctor;
            MethodDefinition watersys_cctor = waterSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (watersys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in watersys_cctor.Body.Instructions)
                {
                    // water mapsize;
                    if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 14336)
                    {
                        ins.Operand = CELL_NEW_SIZE;
                        Logger.LogInfo("Water kMapSize cctor set to " + ins.Operand);
                    }
                    // water cellsize;
                    if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 7f)
                    {
                        ins.Operand = 7f * CELL_FACTOR;
                        Logger.LogInfo("Water kCellSize cctor set to " + ins.Operand);
                    }
                }
                // Add new instructions or logic as needed
            }

            Logger.LogInfo($"水系统 {watersys_cctor} 修补完成！");
        }
        ModuleDefinition module = assembly.MainModule;
        //防御性防止内联；
        CustomAttribute nonVerAttr = new(module.ImportReference(typeof(System.Runtime.CompilerServices.MethodImplAttribute)
    .GetConstructor(new[] { typeof(MethodImplOptions) })));
        nonVerAttr.ConstructorArguments.Add(new CustomAttributeArgument(
            module.ImportReference(typeof(MethodImplOptions)),
            (int)(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)));
        waterSystem.CustomAttributes.Add(nonVerAttr);
    }// water system

    private static void CellMapSystemPatch(AssemblyDefinition assembly)
    {
        // const string TARGET_FIELD_NAME = "kMapSize";

        // 获取WaterSystem Type
        var baseType = assembly.MainModule.GetType("Game.Simulation.CellMapSystem`1");

        // 日志输出其他修补逻辑开始
        Logger.LogInfo($"CellMap系统获取到泛型基类{baseType.Name}...");

        if (baseType != null)
        {
            // 查找泛型基类静态构造函数；若版本更新后不存在则必须修补所有调用方法ldsfld；
            MethodDefinition cctor = baseType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
            if (cctor == null || !cctor.HasBody)
            {
                Logger.LogWarning($"CellMap系统 '{baseType}'静态构造函数未找到！");
                return;
            }
            Logger.LogInfo($"CellMap系统 {baseType}已找到静态构造函数，正在处理IL...");

            /*
            // Get the ILProcessor for the static constructor
            ILProcessor ilProcessor = cctor.Body.GetILProcessor();
            List<Instruction> instructions = cctor.Body.Instructions.ToList(); // Work with a copy or list
            bool patched = false;

            // Iterate through instructions to find where kMapSize is assigned
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction instruction = instructions[i];

                // Look for storing the value into our static field (stsfld)
                if (instruction.OpCode == OpCodes.Stsfld &&
                    instruction.Operand is FieldReference fieldRef &&
                    fieldRef.Name == TARGET_FIELD_NAME &&
                    fieldRef.DeclaringType.GetElementType().FullName == baseType.FullName) // Compare full names of the type definitions
                {
                    Logger.LogInfo($"Found 'stsfld {TARGET_FIELD_NAME}' at index {i}.");

                    // Now, look *backwards* from this instruction to find where
                    // the *original* value (e.g., 14336) was loaded onto the stack.
                    // This is typically an ldc.i4 instruction immediately before the stsfld.
                    if (i > 0)
                    {
                        Instruction previousInstruction = instructions[i - 1];

                        // Check if the previous instruction loaded an integer constant
                        if (previousInstruction.OpCode == OpCodes.Ldc_I4)
                        {
                            // Found the likely original value load!
                            int originalValue = (int)previousInstruction.Operand;
                            Logger.LogInfo($"  Previous instruction is 'ldc.i4 {originalValue}'. Replacing it.");

                            // Create the new instruction to load the new value
                            Instruction newLoadInstruction = ilProcessor.Create(OpCodes.Ldc_I4, CELL_NEW_SIZE);

                            // Replace the old load instruction with the new one
                            ilProcessor.Replace(previousInstruction, newLoadInstruction);

                            // We have successfully patched the initialization value.
                            patched = true;
                            Logger.LogInfo($"  Successfully replaced load instruction. New value {CELL_NEW_SIZE} will be stored.");
                            break; // Assume only one initialization in .cctor
                        }
                        else
                        {
                            Logger.LogWarning($"  Instruction before 'stsfld {TARGET_FIELD_NAME}' was not the expected 'ldc.i4'. IL Structure might be different. Cannot patch value reliably. Instruction was: {previousInstruction}");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"  'stsfld {TARGET_FIELD_NAME}' was the first instruction? Unexpected IL structure.");
                    }
                    // If patching failed, break to avoid potential errors if structure is unexpected
                    if (!patched) break;
                }
            } // End for loop

            if (!patched)
            {
                Logger.LogWarning($"xiubu'{TARGET_FIELD_NAME}' in the .cctor of '{baseType}'.");
            }
            else
            {
                Logger.LogInfo($"Successfully patched {baseType} .cctor.");
                // MonoMod loader might automatically handle recalculating offsets/maxstack,
                // but sometimes manual calls are needed if doing complex IL manipulation.
                // For simple replacement, it's often automatic.
            }
            */

            /// 双保险处理基类及所有封闭类型
            /// 
            ProcessType(baseType);
            Logger.LogInfo($"CellMap系统基类处理完成！ {baseType.FullName} ");

            foreach (var type in assembly.MainModule.Types
                .Where(t => IsClosedSubtype(t, baseType)))
            {
                ProcessType(type);
                Logger.LogInfo($"CellMap系统处理封闭类型完成！ {type.FullName} ");
            }

            static bool IsClosedSubtype(TypeDefinition type, TypeDefinition baseType)
            {
                var current = type;
                while (current != null)
                {
                    if (current.BaseType?.Resolve() == baseType && !current.HasGenericParameters)
                        return true;
                    current = current.BaseType?.Resolve();
                }
                return false;
            }

            static void ProcessType(TypeDefinition type)
            {
                Logger.LogInfo($"CellMap系统正在处理类型 {type.FullName} ");

                foreach (var field in type.Fields.Where(f =>
                    f.Name == "kMapSize" &&
                    f.IsStatic &&
                    f.IsInitOnly))
                {
                    // 修改字段默认值
                    // 移除initonly限制（重要！）
                    field.IsInitOnly = false;
                    field.Constant = CELL_NEW_SIZE;
                    Logger.LogInfo($"CellMap系统处理封闭类型 {type.FullName} 字段 {field.Name} 变更为 {field.Constant} ");

                    // 确保存在静态构造函数 
                    var cctor = type.Methods.FirstOrDefault(m => m.Name == ".cctor");
                    if (cctor == null)
                    {
                        Logger.LogInfo($"CellMap系统未找到静态构造函数 {type.FullName} ,创建新的.cctor ");
                        cctor = new MethodDefinition(
                            ".cctor",
                            MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                            type.Module.TypeSystem.Void);
                        type.Methods.Add(cctor);
                        cctor.Body = new MethodBody(cctor);
                        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    }

                    // 修改/添加赋值指令 
                    var processor = cctor.Body.GetILProcessor();
                    var targetInstruction = cctor.Body.Instructions
                        .FirstOrDefault(i => i.OpCode == OpCodes.Ldc_I4 && (int)i.Operand == 14336);

                    if (targetInstruction != null)
                    {
                        targetInstruction.Operand = CELL_NEW_SIZE;
                        Logger.LogInfo($"IL指令修改: {targetInstruction.OpCode}  => {targetInstruction.Operand}");
                    }
                    else // 无原始赋值则插入新指令 
                    {
                        var returnInstruction = cctor.Body.Instructions.Last();
                        processor.InsertBefore(returnInstruction, Instruction.Create(OpCodes.Ldc_I4, CELL_NEW_SIZE));
                        processor.InsertBefore(returnInstruction, Instruction.Create(OpCodes.Stsfld, field));
                    }
                    Logger.LogInfo($"CellMap系统处理封闭类型 {type.FullName} 字段 {field.Name} 变更为 {field.Constant} ");
                }
            }



            ModuleDefinition module = assembly.MainModule;
            //防御性防止内联；
            CustomAttribute nonVerAttr = new(
               module.ImportReference(
                   typeof(System.Runtime.CompilerServices.MethodImplAttribute)
                   .GetConstructor(new[] { typeof(MethodImplOptions) })));
            nonVerAttr.ConstructorArguments.Add(new CustomAttributeArgument(
                module.ImportReference(typeof(MethodImplOptions)),
                (int)(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)));
            baseType.CustomAttributes.Add(nonVerAttr);
        }

        // Fallback/Alternative: Method to patch all ldsfld usages (more complex)
        // public static void PatchAllUsages(AssemblyDefinition assembly) { ... }


    }// CellMapSystem


    ///
    /// Burst Job Replacer
    /// 

    // --- 1. 补丁列表 ---  (已移至末尾!)
    // --- 2. 修补目标程序集 --- (已合并方法)
    // --- 3. Patch()调用具体修补逻辑 --- (已合并到Patch方法)
    // --- 4. 具体的修补逻辑 (辅助方法) ---
    // 这个方法包含了之前示例中的核心 Mono.Cecil 修改代码，但现在是参数化的。
    private static void ApplySinglePatch(AssemblyDefinition assembly, JobPatchTarget target)
    {
        MethodDefinition targetMethod = null;
        TypeReference replacementJobTypeRef = null;
        TypeReference originalJobTypeRef = null;
        AssemblyDefinition customJobAssembly = null;
        // --- CHANGE HERE: Use DefaultAssemblyResolver type ---
        DefaultAssemblyResolver resolver = null;

        try
        {
            // --- Find Target Type and Method ---
            var targetType = assembly.MainModule.GetType(target.TargetTypeName);
            if (targetType == null) {/*... log error and return ...*/ return; }
            targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == target.TargetMethodName); // Add param check if needed
            if (targetMethod == null) {/*... log error and return ...*/ return; }

            // --- Load and Import Replacement Job Type ---
            string customJobDllPath = FindCustomJobDllPath(target.ReplacementJobAssemblyName); // 传递程序集名
            if (customJobDllPath == null)
            {
                Logger.LogError($"Could not find replacement job assembly '{target.ReplacementJobAssemblyName}.dll'. Skipping patch for {target.TargetTypeName}.{target.TargetMethodName}");
                return;
            }

            // --- Configure the Assembly Resolver ---
            resolver = new DefaultAssemblyResolver();

            // --- AddSearchDirectory calls will now work ---
            string customJobDir = Path.GetDirectoryName(customJobDllPath);
            if (Directory.Exists(customJobDir))
            {
                resolver.AddSearchDirectory(customJobDir); // Now valid
                Logger.LogInfo($"Resolver: Added custom job directory: {customJobDir}");
            }
            if (Directory.Exists(Paths.ManagedPath))
            {
                resolver.AddSearchDirectory(Paths.ManagedPath); // Now valid
                Logger.LogInfo($"Resolver: Added game managed path: {Paths.ManagedPath}");
            }
            // ... (Add other search directories: BepInExAssemblyDirectory, PluginPath) ...

            // --- Read the Assembly using the Configured Resolver ---
            var readerParams = new ReaderParameters { ReadWrite = false, AssemblyResolver = resolver };
            customJobAssembly = AssemblyDefinition.ReadAssembly(customJobDllPath, readerParams);
            // ... (Log success) ...

            // --- Find and Import Replacement Job Type ---
            var replacementJobTypeDefinition = customJobAssembly.MainModule.GetType(target.ReplacementJobFullName);
            if (replacementJobTypeDefinition == null)
            {
                Logger.LogError($"Replacement job type '{target.ReplacementJobFullName}' not found in {customJobAssembly.Name.Name}. Skipping patch.");
                customJobAssembly.Dispose();
                return;
            }
            replacementJobTypeRef = assembly.MainModule.ImportReference(replacementJobTypeDefinition);
            Logger.LogInfo($"Imported replacement job type: {replacementJobTypeRef.FullName}");


            // --- Find Original Job Type Reference ---
            originalJobTypeRef = FindOriginalJobTypeReference(targetMethod, target.OriginalJobFullName);
            if (originalJobTypeRef == null) {/*... log error and return ...*/ return; }
            Logger.LogInfo($"Found original job type reference: {originalJobTypeRef.FullName}");


            // --- Process Method Body (IL Manipulation) ---
            var ilProcessor = targetMethod.Body.GetILProcessor();
            var instructions = targetMethod.Body.Instructions; // Get instructions collection
            targetMethod.Body.SimplifyMacros(); // Essential for predictable offsets/indices

            bool modified = false;

            // 1. Modify Local Variable Definitions (High Priority)
            foreach (var variable in targetMethod.Body.Variables)
            {
                if (variable.VariableType.FullName == originalJobTypeRef.FullName)
                {
                    Logger.LogInfo($"Changing local variable type from '{variable.VariableType.FullName}' to '{replacementJobTypeRef.FullName}'");
                    variable.VariableType = replacementJobTypeRef;
                    modified = true;
                }
            }

            // 2. Modify Instructions
            for (int i = 0; i < instructions.Count; i++) // Use index loop for potential insertion/replacement
            {
                var instruction = instructions[i];

                // Patch initobj
                if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeRef && typeRef.FullName == originalJobTypeRef.FullName)
                {
                    Logger.LogInfo($"Replacing initobj target type '{typeRef.FullName}' with '{replacementJobTypeRef.FullName}'");
                    instruction.Operand = replacementJobTypeRef;
                    modified = true;
                }
                // Patch Call/Callvirt instructions for Schedule variants
                else if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                {
                    if (instruction.Operand is MethodReference methodRef && methodRef.Name.Contains("Schedule")) // Broad check for "Schedule" variants
                    {
                        // Case A: Static Generic Extension Method Call (e.g., IJobParallelForExtensions.Schedule<T>)
                        if (methodRef is GenericInstanceMethod genericMethodRef)
                        {
                            bool needsReplacement = false;
                            var newGenericInstance = new GenericInstanceMethod(genericMethodRef.ElementMethod); // Base generic definition

                            foreach (var arg in genericMethodRef.GenericArguments)
                            {
                                if (arg.FullName == originalJobTypeRef.FullName)
                                {
                                    newGenericInstance.GenericArguments.Add(replacementJobTypeRef); // Replace with new job type
                                    needsReplacement = true;
                                }
                                else
                                {
                                    newGenericInstance.GenericArguments.Add(arg); // Keep other args
                                }
                            }

                            if (needsReplacement)
                            {
                                Logger.LogInfo($"Found static generic Schedule call: {genericMethodRef.FullName}");
                                var newMethodRef = assembly.MainModule.ImportReference(newGenericInstance);
                                Logger.LogInfo($"Replacing call target with: {newMethodRef.FullName}");
                                instruction.Operand = newMethodRef;
                                modified = true;
                                Logger.LogDebug($"Patched instruction at offset IL_{instruction.Offset:X4}");
                            }
                        }
                        // Case B: Instance Method Call (e.g., jobData.Schedule(...) )
                        else if (methodRef.HasThis) // It's an instance method
                        {
                            // Check if the call *was originally* on the old Job type.
                            // This helps confirm we're looking at the right kind of instance call.
                            if (methodRef.DeclaringType.FullName == originalJobTypeRef.FullName)
                            {
                                // **Optimistic Approach:** Assume changing the local variable type is enough.
                                // Log this potential instance call for awareness/debugging.
                                Logger.LogInfo($"Detected instance Schedule call originally targeting {methodRef.FullName}. Assuming it will resolve to {replacementJobTypeRef.Name}::{methodRef.Name} due to local variable type change. No direct IL modification applied to this call instruction.");
                                Logger.LogDebug($"Instance call instruction at offset IL_{instruction.Offset:X4}");

                                // **Optional/Advanced "Pessimistic" Approach (Uncomment/Implement if needed):**
                                // If the optimistic approach fails, you'd need to find the *correct* corresponding
                                // Schedule method on the *new* job type (replacementJobTypeRef) with a matching
                                // or compatible signature, import it, and replace instruction.Operand.
                                // This requires careful signature matching.
                                // Example placeholder:
                                // MethodReference newInstanceMethodRef = FindMatchingInstanceMethod(replacementJobTypeRef, methodRef);
                                // if (newInstanceMethodRef != null) {
                                //     instruction.Operand = assembly.MainModule.ImportReference(newInstanceMethodRef);
                                //     modified = true;
                                //     _logSource.LogInfo($"Explicitly replaced instance call target with: {newInstanceMethodRef.FullName}");
                                // } else {
                                //     _logSource.LogError($"Could not find matching instance method for {methodRef.FullName} on type {replacementJobTypeRef.FullName}. Patch might fail!");
                                // }
                            }
                            // else: Log if an instance Schedule call targets something else, might be noise or relevant?
                            // else {
                            //    _logSource.LogDebug($"Ignoring instance Schedule call targeting non-original job type: {methodRef.FullName}");
                            // }
                        }
                        // Case C: Non-Generic Static Schedule Call? (Less common for job scheduling itself)
                        // else { /* Handle if necessary */ }
                    }
                }
                // Patch stfld (Store Field) if Job Structs differ significantly (Essential if fields changed name/type)
                else if (instruction.OpCode == OpCodes.Stfld && instruction.Operand is FieldReference fieldRef)
                {
                    // Check if the field belongs to the original job type
                    if (fieldRef.DeclaringType.FullName == originalJobTypeRef.FullName)
                    {
                        // **ASSUMPTION:** We assume JobD has a field with the *same name*.
                        string fieldName = fieldRef.Name;
                        Logger.LogDebug($"Detected stfld for original job field: {fieldRef.FullName}");

                        // Find the corresponding field in the new job type
                        var replacementJobTypeDef = replacementJobTypeRef.Resolve(); // Resolve to definition to find fields
                        FieldDefinition newFieldDef = replacementJobTypeDef?.Fields.FirstOrDefault(f => f.Name == fieldName);

                        if (newFieldDef != null)
                        {
                            // Import the FieldReference for the field in the *new* job type
                            var newFieldRef = assembly.MainModule.ImportReference(newFieldDef);

                            // Check if operand needs changing (it always should if the declaring type changed)
                            if (instruction.Operand != newFieldRef)
                            {
                                Logger.LogInfo($"Replacing stfld target from {fieldRef.FullName} to {newFieldRef.FullName}");
                                instruction.Operand = newFieldRef;
                                modified = true;
                                Logger.LogDebug($"Patched stfld instruction at offset IL_{instruction.Offset:X4}");
                            }
                        }
                        else
                        {
                            // Field with the same name not found in the new job! This is a problem.
                            Logger.LogError($"Field '{fieldName}' found in original job '{originalJobTypeRef.Name}' but NOT FOUND in replacement job '{replacementJobTypeRef.Name}'. Assignment will likely fail or be incorrect! Manual patch adjustment needed.");
                            // Consider throwing an exception or adding more robust handling/configuration.
                        }
                    }
                }

            } // End instruction loop

            if (modified)
            {
                targetMethod.Body.OptimizeMacros(); // Optimize after changes
                Logger.LogInfo($"Successfully applied modifications to method: {targetMethod.FullName} for target job {target.OriginalJobFullName}");
            }
            else
            {
                Logger.LogWarning($"No modifications were applied for target {target.TargetTypeName}.{target.TargetMethodName}. Check if the original job type or Schedule calls were found, or if patching logic needs adjustment.");
            }
        }
        catch (AssemblyResolutionException are)
        {
            Logger.LogError($"AssemblyResolutionException during patch for {target.TargetTypeName}.{target.TargetMethodName}: {are.Message}");
            Logger.LogError($"Failed to resolve: {are.AssemblyReference?.FullName ?? "Unknown Assembly"}");
            Logger.LogError($"Resolver Search Directories:");
            // --- Adjusted access to GetSearchDirectories ---
            if (resolver != null)
            {
                foreach (var dir in resolver.GetSearchDirectories())
                {
                    Logger.LogError($"  - {dir}");
                }
            }
            Logger.LogError($"Stack Trace: {are.StackTrace}");
            // ... (Context logging) ...
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying patch for {target.TargetTypeName}.{target.TargetMethodName}: {ex}");
            // Optional: Log details about the state if possible
            if (targetMethod != null) Logger.LogError($"Error occurred while processing method: {targetMethod.FullName}");
            if (originalJobTypeRef != null) Logger.LogError($"Original Job Type: {originalJobTypeRef.FullName}");
            if (replacementJobTypeRef != null) Logger.LogError($"Replacement Job Type: {replacementJobTypeRef.FullName}");
        }
        finally
        {
            // --- Dispose the resolver and the loaded assembly ---
            // Using resolver directly is fine now as it implements IDisposable
            resolver?.Dispose();
            customJobAssembly?.Dispose();
            Logger.LogDebug($"Disposed resolver and custom job assembly for target {target.OriginalJobFullName}.");
            // Dispose custom job assembly if loaded earlier (ensure this is handled correctly in the loading part)
            // customJobAssembly?.Dispose(); should be handled where it's loaded
        }
    }    

    // Optional Helper for Pessimistic Instance Call Patching (Complex)
    // private static MethodReference FindMatchingInstanceMethod(TypeReference newJobTypeRef, MethodReference originalMethodRef)
    // {
    //    // Resolve newJobTypeRef to TypeDefinition
    //    // Iterate through methods of the new type definition
    //    // Match name (e.g., "Schedule")
    //    // Match parameters (count, types - this is the tricky part, need to import/compare parameter types)
    //    // Match return type
    //    // Return the found MethodDefinition (which will be implicitly converted/imported later)
    //    return null; // Placeholder
    // }



    // --- Helper Methods (unchanged from previous example, but might need adaptation) ---
    private static string FindCustomJobDllPath(string replacementAssemblyName)
    {
        // Example: Assume it's in a subfolder named 'YourMod' within the BepInEx plugins directory
        string pluginsPath = Paths.PluginPath; // BepInEx provided path
        string expectedPath = Path.Combine(pluginsPath, "MapExtPDX", $"{replacementAssemblyName}.dll"); // Use parameter
        if (File.Exists(expectedPath))
        {
            return expectedPath;
        }
        Logger.LogWarning($"Custom job DLL '{replacementAssemblyName}.dll' not found at expected plugin location: {expectedPath}");

        // Fallback: Check patcher's directory
        string patcherDir = Path.GetDirectoryName(typeof(PreloaderPatcher).Assembly.Location);
        expectedPath = Path.Combine(patcherDir, $"{replacementAssemblyName}.dll");
        if (File.Exists(expectedPath))
        {
            Logger.LogInfo($"Found '{replacementAssemblyName}.dll' in patcher directory: {patcherDir}");
            return expectedPath;
        }
        Logger.LogWarning($"Custom job DLL '{replacementAssemblyName}.dll' not found in patcher directory either.");

        return null;
    }

    private static TypeReference FindOriginalJobTypeReference(MethodDefinition method, string originalJobFullName)
    {
        // 1. Check local variables
        foreach (var variable in method.Body.Variables)
        {
            if (variable.VariableType.FullName == originalJobFullName)
            {
                return variable.VariableType;
            }
        }
        // 2. Check generic arguments in method calls
        foreach (var instruction in method.Body.Instructions)
        {
            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is GenericInstanceMethod genericMethodRef)
            {
                foreach (var arg in genericMethodRef.GenericArguments)
                {
                    if (arg.FullName == originalJobFullName)
                    {
                        return arg;
                    }
                }
            }
        }
        // 3. Check initobj operand
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeRef && typeRef.FullName == originalJobFullName)
            {
                return typeRef;
            }
        }
        return null;
    }


    ///
    /// Burst Job 替换列表
    ///
    public class JobPatchTarget
    {
        public string TargetAssemblyName { get; set; } // e.g., "Game"
        public string TargetTypeName { get; set; }     // e.g., "Game.Simulation.AvailabilityInfoToGridSystem"
        public string TargetMethodName { get; set; }   // e.g., "OnUpdate"
        public string OriginalJobFullName { get; set; } // e.g., "Game.Simulation.AvailabilityInfoToGridSystem/AvailabilityInfoToGridJob" (注意嵌套类型用'/')
        public string ReplacementJobAssemblyName { get; set; } // e.g., "MapExtPDX(burst job AOT库)" (无后缀名 .dll)
        public string ReplacementJobFullName { get; set; } // e.g., "MapExtPDX.MyCustomJob" // 自定义库中未使用嵌套则用"."
        public bool IsParallelFor { get; set; } // Schedule标志位

        // public bool IsParallelFor { get; set; }
        // Add other flags if needed (e.g., IsJobChunk, Schedule variant type)

        // 可以添加更多标志来辅助定位 Schedule 方法或处理特殊情况
        // public bool IsParallelFor { get; set; } = true; // 默认为 IJobParallelFor
        // public string ScheduleMethodHint { get; set; } = "Schedule"; // 帮助识别 Schedule 调用

        // 构造函数或其他方法可以简化创建
        public JobPatchTarget(string targetType, string targetMethod, string originalJob, string replacementAsm, string replacementJob, string targetAsm = "Game")
        {
            TargetAssemblyName = targetAsm;
            TargetTypeName = targetType;
            TargetMethodName = targetMethod;
            OriginalJobFullName = originalJob;
            ReplacementJobAssemblyName = replacementAsm;
            ReplacementJobFullName = replacementJob;
        }
    }


    // --- 1. 定义所有需要应用的补丁目标 ---
    // 这个列表在这里初始化，包含了所有你想要修改的地方。
    private static readonly List<JobPatchTarget> PatchTargets = new()
    {
        new JobPatchTarget(
                targetType: "Game.Simulation.AirPollutionSystem",
                targetMethod: "OnUpdate",
                originalJob: "Game.Simulation.AirPollutionSystem/AirPollutionMoveJob", // 嵌套类型用 /
                replacementAsm: "MapExtPDX", // 替换 Job 所在的程序集名
                replacementJob: "MapExtPDX.AirPollutionMoveJob" // 替换 Job 的完整类型名
            ),
        // 补丁目标：修改 AvailabilityInfoToGridSystem.OnUpdate
        new JobPatchTarget(
                targetType: "Game.Simulation.AvailabilityInfoToGridSystem",
                targetMethod: "OnUpdate",
                originalJob: "Game.Simulation.AvailabilityInfoToGridSystem/AvailabilityInfoToGridJob", // 嵌套类型用 /
                replacementAsm: "MapExtPDX", // 替换 Job 所在的程序集名
                replacementJob: "MapExtPDX.AvailabilityInfoToGridJob" // 替换 Job 的完整类型名
            ),



            // --- 在这里添加更多的补丁目标 ---
            // 例如，如果要修改另一个 System 的另一个 Job：
            // new JobPatchTarget(
            //     targetAsm: "Game.Simulation.dll", // 假设在同一个程序集
            //     targetType: "Game.Simulation.AnotherSystem",
            //     targetMethod: "AnotherMethodToPatch",
            //     originalJob: "Game.Simulation.AnotherSystem/OriginalJobX",
            //     replacementAsm: "YourMod.CustomJobs", // 可以是同一个，也可以是不同的自定义程序集
            //     replacementJob: "YourMod.CustomJobs.MyCustomJobX"
            // ),
            // 例如，如果目标在不同的程序集：
            // new JobPatchTarget(
            //     targetAsm: "Game.Rendering.dll",
            //     targetType: "Game.Rendering.RenderingSystem",
            //     targetMethod: "OnUpdate",
            //     originalJob: "Game.Rendering.RenderingSystem/RenderingJob",
            //     replacementAsm: "YourMod.CustomJobs",
            //     replacementJob: "YourMod.CustomJobs.MyRenderingJob"
            // )
        };
}
