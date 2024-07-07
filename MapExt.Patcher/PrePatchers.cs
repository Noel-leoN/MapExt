using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil.Cil;
//using System.ComponentModel;
//using Mono.Cecil.Rocks;
//using System;

namespace MapExtPreloader
{
    internal class PrePatchers
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };

        public static ManualLogSource logSource;
        public static void Initialize()
        {
            logSource = Logger.CreateLogSource("ExtMapLog");
        }

        // Patches the assemblies
        public static void Patch(AssemblyDefinition assembly)
        {
            // Patcher code here
            var assemblyNameWithDll = $"{assembly.Name}.dll";

            logSource.LogInfo($"Received assembly {assemblyNameWithDll} for patching");

            ModuleDefinition module = assembly.MainModule;
            //foreach (TypeDefinition type in module.Types)
            //{
                //if (!type.IsAbstract)
                //    continue;
                //Console.WriteLine(type.FullName);
                //logSource.LogInfo($"Received assembly module {type.FullName} for patching");
            //}

            ///TerrainSystem;
            TypeDefinition terrainSystem = module.GetType("Game.Simulation","TerrainSystem");
            logSource.LogInfo($"target class {terrainSystem} for patching");

            //var terrainsys_cctor = terrainSystem.Methods.Single(m => m.IsConstructor);

            // Using Mono.Cecil to manipulate an existing static constructor
            
            //TerrainSystem .cctor;
            MethodDefinition terrainsys_cctor = terrainSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (terrainsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in terrainsys_cctor.Body.Instructions)
                {
                    //kDefaultMapSize;
                    if(ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                    {
                        ins.Operand = 57344f;
                    }
                    ///optional!!!
                    //kDefaultHeightScale;
                    if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 4096f)
                    {
                        ins.Operand = 8192f;
                    }

                    ///test!!!change to 16kx16k greysale from 4k
                    //if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 4096)
                    //{
                    //    ins.Operand = 16384;
                    //}

                }
                // Add new instructions or logic as needed
            }
            logSource.LogInfo($"target method {terrainsys_cctor} for patching");

            //TerrainSystem 2 public Methods;
            foreach (MethodDefinition method in terrainSystem.Methods)
            {
                //Console.WriteLine(method.Name);
                //GetTerrainBounds;referenced by 1 system
                if (method.Name == "GetTerrainBounds")

                //if (terrainsys_bounds != null)
                {
                    // Modify the content of the static constructor
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                        {
                            ins.Operand = 57344f;
                        }
                    }
                    // Add new instructions or logic as needed
                    logSource.LogInfo($"target method {method} for patching");
                }

                //GetHeightData;referenced by a lot of systems
                if (method.Name == "GetHeightData")
                {
                    // Modify the content of the static constructor
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                        {
                            ins.Operand = 57344f;
                        }
                    }
                    // Add new instructions or logic as needed
                    logSource.LogInfo($"target method {method} for patching");
                }

                ///not sure!!!影响TerrainMinMax Init;
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

            ///AreaToolSystem;debug only for burst-disable-compile playmode
            //var areaToolSystem = module.GetType("Game.Tools", "AreaToolSystem");
            //logSource.LogInfo($"target class {areaToolSystem} for patching");

            //foreach (MethodDefinition method in areaToolSystem.Methods)
            //{
                //Console.WriteLine(method.Name);
                /*
                if (method.Name == "x")
                {
                    // Modify the content of the static constructor
                    foreach (Instruction ins in xxxxx.Body.Instructions)
                    {
                        //;
                        if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 14336)
                        {
                            ins.Operand = 57344;
                        }
                    
                    }
                    // Add new instructions or logic as needed
                }

                logSource.LogInfo($"target method {   } for patching");
                */
            //}

            ///WaterSystem;
            var waterSystem = module.GetType("Game.Simulation","WaterSystem");
            logSource.LogInfo($"target class {waterSystem} for patching");
            
            MethodDefinition watersys_cctor = waterSystem.Methods.FirstOrDefault(w => w.Name == ".cctor");

            if (watersys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in watersys_cctor.Body.Instructions)
                {
                    //water mapsize;
                    if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 14336)
                    {
                        ins.Operand = 57344;
                    }
                    //water cellsize;
                    if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 7f)
                    {
                        ins.Operand = 28f;
                    }
                }
                // Add new instructions or logic as needed
            }

            logSource.LogInfo($"target method {watersys_cctor} for patching");

            ///降低水模拟速率以提高性能，似乎不起作用；
            /*
            MethodDefinition watersys_simgpu = waterSystem.Methods.FirstOrDefault(n => n.Name == ".Simulate");
            if (watersys_simgpu != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in watersys_simgpu.Body.Instructions)
                {
                    //lower water speed;
                    if (ins.OpCode.Name == "ldc.i4.4" )
                    {
                        ins.OpCode = OpCodes.Ldc_I4_8;
                    }
                    if (ins.OpCode.Name == "div")
                    {
                        ins.OpCode = OpCodes.Mul;
                    }

                }
                // Add new instructions or logic as needed
            }*/

            ///CellMapSystem;
            ///
            ///
            TypeDefinition cellmapSystem = module.GetType("Game.Simulation", "CellMapSystem`1");
            logSource.LogInfo($"target class {cellmapSystem} for patching");

            MethodDefinition cellmapsys_cctor = cellmapSystem.Methods.FirstOrDefault(c => c.Name == ".cctor");

            if (cellmapsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in cellmapsys_cctor.Body.Instructions)
                {
                    if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 14336)
                    {
                        ins.Operand = 57344;
                    }
                    
                    //logSource.LogInfo($"cellmapsize modified to {ins.Operand} ");
                    
                }
                logSource.LogInfo($"target method {cellmapsys_cctor} for patching");
                // Add new instructions or logic as needed
            }

        }//prepatch method;

    }//patcher class;

}//namespace;

///
///提示：BepInEx cfg设置为dump Assembly,运行prepatch获得修改后的dll,并引用作为编译pdx.mod的依赖项，以大量减少pdx.mod代码，以避免在pdx引用原版cellmapsize进行burst job compile；
///(不建议加入自定义cellmapsystem作为基类编译，以避免修改引用mapsize的非cellmap派生子系统）;