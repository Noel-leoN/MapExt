// MapExtPatcher.dll
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// using System.Reflection; // 通常不需要直接用于 Patcher 逻辑

namespace MapExtPatcher
{
    /// <summary>
    /// 定义一个补丁目标的信息。
    /// 每个实例代表一个需要被修改的方法及其相关的 Job 替换信息。
    /// </summary>
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
        public JobPatchTarget( string targetType, string targetMethod, string originalJob, string replacementAsm, string replacementJob, string targetAsm = "Game")
        {
            TargetAssemblyName = targetAsm;
            TargetTypeName = targetType;
            TargetMethodName = targetMethod;
            OriginalJobFullName = originalJob;
            ReplacementJobAssemblyName = replacementAsm;
            ReplacementJobFullName = replacementJob;
        }
    }

    // BepInEx 5 Patcher Entry Point
    public static class PreloaderPatcher
    {
        // 修补程序集名称  
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };

        // 静态日志源，使用BepInEx的ManualLogSource
        private static ManualLogSource _logSource = Logger.CreateLogSource("ReplaceBusrtJobPatcher");

        // --- 1. 定义所有需要应用的补丁目标 ---
        // 这个列表在这里初始化，包含了所有你想要修改的地方。
        private static readonly List<JobPatchTarget> PatchTargets = new List<JobPatchTarget>
        {
            // 第一个补丁目标：修改 AvailabilityInfoToGridSystem.OnUpdate
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

        // --- 2. BepInEx Patcher TargetDLLs ---
        // 这个属性告诉 BepInEx Preloader 哪些程序集加载时需要调用我们的 Patch 方法。
        // 应该包含所有 PatchTargets 中涉及的 TargetAssemblyName。
        /*
        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                // 从 PatchTargets 动态生成列表，避免重复且确保所有目标都被包含
                return PatchTargets.Select(t => t.TargetAssemblyName).Distinct();
            }
        }
        */

        /*
        // --- 3. BepInEx Patcher Patch Method ---
        // 这个方法会被 BepInEx 为 TargetDLLs 中的每个程序集调用一次。
        public static void Patch(ref AssemblyDefinition assembly)
        {
            _logSource.LogInfo($"Processing assembly: {assembly.Name.Name}");
            _logSource.LogInfo($"PatchTargets contains {PatchTargets.Count} items.");

            // 遍历所有定义的补丁目标
            foreach (var target in PatchTargets)
            {
                // 检查当前处理的程序集是否是此补丁目标所指定的程序集
                if (assembly.Name.Name == target.TargetAssemblyName)
                {
                    _logSource.LogInfo($"Attempting patch for target: {target.TargetTypeName}.{target.TargetMethodName}");
                    // 如果匹配，则调用实际执行修改的辅助方法
                    ApplySinglePatch(assembly, target);
                }
                 else
                 {
                //     // 可选：记录跳过不匹配的补丁目标
                     _logSource.LogDebug($"Skipping target '{target.TargetTypeName}.{target.TargetMethodName}' for assembly '{assembly.Name.Name}'");
                 }
            }
        }
        */

        // --- 4. 具体的修补逻辑 (辅助方法) ---
        // 这个方法包含了之前示例中的核心 Mono.Cecil 修改代码，但现在是参数化的。
        private static void ApplySinglePatch(AssemblyDefinition assembly, JobPatchTarget target)
        {
            try
            {
                // --- Find Target Type and Method ---
                var targetType = assembly.MainModule.GetType(target.TargetTypeName);
                if (targetType == null)
                {
                    _logSource.LogError($"Target type '{target.TargetTypeName}' not found in {assembly.Name.Name}. Skipping patch.");
                    return;
                }

                // Find the specific method (需要更健壮的查找，例如考虑参数)
                var targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == target.TargetMethodName /* && CheckParameters(m, expectedParams) */ );
                if (targetMethod == null)
                {
                    _logSource.LogError($"Target method '{target.TargetMethodName}' not found in type '{targetType.FullName}'. Skipping patch.");
                    return;
                }

                // --- Load and Import Replacement Job Type ---
                string customJobDllPath = FindCustomJobDllPath(target.ReplacementJobAssemblyName); // 传递程序集名
                if (customJobDllPath == null)
                {
                    _logSource.LogError($"Could not find replacement job assembly '{target.ReplacementJobAssemblyName}.dll'. Skipping patch for {target.TargetTypeName}.{target.TargetMethodName}");
                    return;
                }

                var resolver = new DefaultAssemblyResolver(); // Consider a more robust resolver
                resolver.AddSearchDirectory(Path.GetDirectoryName(customJobDllPath));
                // Add game/unity paths if needed: resolver.AddSearchDirectory(Paths.ManagedPath);
                var customJobAssembly = AssemblyDefinition.ReadAssembly(customJobDllPath, new ReaderParameters { ReadWrite = false, AssemblyResolver = resolver });

                var replacementJobTypeDefinition = customJobAssembly.MainModule.GetType(target.ReplacementJobFullName);
                if (replacementJobTypeDefinition == null)
                {
                    _logSource.LogError($"Replacement job type '{target.ReplacementJobFullName}' not found in {customJobAssembly.Name.Name}. Skipping patch.");
                    customJobAssembly.Dispose();
                    return;
                }
                var replacementJobTypeRef = assembly.MainModule.ImportReference(replacementJobTypeDefinition);
                _logSource.LogInfo($"Imported replacement job type: {replacementJobTypeRef.FullName}");

                // --- Find Original Job Type Reference ---
                TypeReference originalJobTypeRef = FindOriginalJobTypeReference(targetMethod, target.OriginalJobFullName);
                if (originalJobTypeRef == null)
                {
                    _logSource.LogError($"Could not find reference to original job type '{target.OriginalJobFullName}' in method '{targetMethod.Name}'. Skipping patch.");
                    customJobAssembly.Dispose();
                    return;
                }
                _logSource.LogInfo($"Found original job type reference: {originalJobTypeRef.FullName}");


                // --- Process Method Body (IL Manipulation) ---
                var ilProcessor = targetMethod.Body.GetILProcessor();
                targetMethod.Body.SimplifyMacros();

                bool modified = false; // 跟踪是否有实际修改发生

                // 1. Modify Local Variable Definitions
                foreach (var variable in targetMethod.Body.Variables)
                {
                    if (variable.VariableType.FullName == originalJobTypeRef.FullName)
                    {
                        _logSource.LogInfo($"Changing local variable type from '{variable.VariableType.FullName}' to '{replacementJobTypeRef.FullName}'");
                        variable.VariableType = replacementJobTypeRef;
                        modified = true;
                    }
                }

                // 2. Modify Instructions (initobj, Schedule calls, potentially stfld)
                for (int i = 0; i < targetMethod.Body.Instructions.Count; i++)
                {
                    var instruction = targetMethod.Body.Instructions[i];

                    // Patch initobj
                    if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeRef && typeRef.FullName == originalJobTypeRef.FullName)
                    {
                        _logSource.LogInfo($"Replacing initobj target type '{typeRef.FullName}' with '{replacementJobTypeRef.FullName}'");
                        instruction.Operand = replacementJobTypeRef;
                        modified = true;
                    }
                    // Patch Schedule calls (Generic)
                    else if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is GenericInstanceMethod genericMethodRef && genericMethodRef.Name.Contains("Schedule"))
                    {
                        bool schedulePatched = false;
                        var newGenericInstance = new GenericInstanceMethod(genericMethodRef.ElementMethod); // Start with the base generic definition
                        bool needsReplacement = false;

                        foreach (var arg in genericMethodRef.GenericArguments)
                        {
                            if (arg.FullName == originalJobTypeRef.FullName)
                            {
                                newGenericInstance.GenericArguments.Add(replacementJobTypeRef); // Replace
                                needsReplacement = true;
                            }
                            else
                            {
                                newGenericInstance.GenericArguments.Add(arg); // Keep original
                            }
                        }

                        if (needsReplacement)
                        {
                            _logSource.LogInfo($"Found Schedule call: {genericMethodRef.FullName}");
                            var newMethodRef = assembly.MainModule.ImportReference(newGenericInstance);
                            _logSource.LogInfo($"Replacing call target with: {newMethodRef.FullName}");
                            instruction.Operand = newMethodRef;
                            modified = true;
                            schedulePatched = true;
                        }
                    }
                    // TODO: Add logic for non-generic Schedule calls if necessary
                    // TODO: Add logic for patching 'stfld' instructions if JobD field layout/names differ from JobC.
                    // This requires finding the stfld instruction targeting the old field and replacing its operand
                    // with an imported FieldReference to the corresponding field in JobD.
                }

                if (modified)
                {
                    targetMethod.Body.OptimizeMacros();
                    _logSource.LogInfo($"Successfully applied patch modifications to: {targetMethod.FullName}");
                }
                else
                {
                    _logSource.LogWarning($"No modifications were applied for target {target.TargetTypeName}.{target.TargetMethodName}. Check if the original job type or Schedule calls were found.");
                }

                // Dispose the loaded custom assembly definition
                customJobAssembly.Dispose();

            }
            catch (Exception ex)
            {
                _logSource.LogError($"Error applying patch for {target.TargetTypeName}.{target.TargetMethodName}: {ex}");
            }
        }

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
            _logSource.LogWarning($"Custom job DLL '{replacementAssemblyName}.dll' not found at expected plugin location: {expectedPath}");

            // Fallback: Check patcher's directory
            string patcherDir = Path.GetDirectoryName(typeof(PreloaderPatcher).Assembly.Location);
            expectedPath = Path.Combine(patcherDir, $"{replacementAssemblyName}.dll");
            if (File.Exists(expectedPath))
            {
                _logSource.LogInfo($"Found '{replacementAssemblyName}.dll' in patcher directory: {patcherDir}");
                return expectedPath;
            }
            _logSource.LogWarning($"Custom job DLL '{replacementAssemblyName}.dll' not found in patcher directory either.");

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
    }
}
