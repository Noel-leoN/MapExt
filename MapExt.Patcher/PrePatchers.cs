using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using System;

namespace MapExtPreloader
{
    internal class TerrainSystemPrePatchers
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };

        public static ManualLogSource logSource;
        public static void Initialize()
        {
            logSource = Logger.CreateLogSource("MapExtLog");
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
            {
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
                            Console.WriteLine("TerrainSys_MapSize set to " + ins.Operand);
                        }
                        ///optional!!!
                        //kDefaultHeightScale;
                        if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 4096f)
                        {
                            ins.Operand = 8192f;
                            Console.WriteLine("TerrainSys_DefaultHeightScale set to " + ins.Operand);
                        }

                        ///test!!!change heightmap resolution to 16kx16k or 8kx8k greysale from 4k
                        //if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 4096)
                        //{
                        //    ins.Operand = 16384;
                        //    ins.Operand = 8192;(optional)
                        //}

                    }
                    // Add new instructions or logic as needed
                }
                logSource.LogInfo($"target method {terrainsys_cctor} patched");

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
                    logSource.LogInfo($"target method {method} patched");
                }

                //GetHeightData;referenced by a lot of systems!!!;
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
                    logSource.LogInfo($"target method {method} patched");
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
            }

            ///AreaToolSystem;debug only for burst-disable-compile playmode;
            ///效果未明，可能影响地图区块构建；
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
                    if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 23)
                    {
                        ins.Operand = 92;
                    }

                }
                // Add new instructions or logic as needed
            }

            logSource.LogInfo($"target method {   } for patching");
            */
            //}

            ///WaterSystem;
            {
                TypeDefinition waterSystem = module.GetType("Game.Simulation", "WaterSystem");
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

                logSource.LogInfo($"target method {watersys_cctor} patched");


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
            }            

        }//prepatch method;

    }//patcher class;

}//namespace;
