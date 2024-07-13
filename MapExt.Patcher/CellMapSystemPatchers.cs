using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using System;


namespace MapExtPreloader
{
    internal class CellMapSystemPrePatchers
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };

        public static ManualLogSource logSource;
        public static void Initialize()
        {
            logSource = BepInEx.Logging.Logger.CreateLogSource("MapExtLog");
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

            ///
            //获取CellMapSystem<T>;
            TypeDefinition cellmapSystemType = module.GetType("Game.Simulation", "CellMapSystem`1");
            logSource.LogInfo($"target class {cellmapSystemType} for patching");

            ///改造CellMaSystem<T>；
            //optional: make cellmapsys fields public for easy harmony;
            foreach (var field in cellmapSystemType.Fields)
            {
                field.IsPublic = true;

                logSource.LogInfo($"cellmap field {field} patched");
            }

            //foreach (MethodDefinition method in cellmapSystemType.Methods)
            //{
                //打印所有CellMap派生类的方法
            //    Console.WriteLine("  " + method.FullName);
            //}

            ///
            //Phase 1: 静态修改cctor；CellMapSystem<T>.kMapSize;
            ///
            MethodDefinition cellmapsys_cctor = cellmapSystemType.Methods.FirstOrDefault(c => c.Name == ".cctor");
            if (cellmapsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in cellmapsys_cctor.Body.Instructions)
                {
                    if (ins.OpCode == OpCodes.Ldc_I4 && (int)ins.Operand == 14336)
                    {
                        ins.Operand = 57344;

                        Console.WriteLine("  L_{0}: {1} {2}", ins.Offset.ToString("x4"), ins.OpCode.Name, ins.Operand is String ? String.Format("\"{0}\"", ins.Operand) : ins.Operand);
                    }
                    //if (ins.OpCode == OpCodes.Stsfld)
                    //{
                    //    ins.Operand = field_of_tg;//instance reference;

                    //   Console.WriteLine("  L_{0}: {1} {2}", ins.Offset.ToString("x4"), ins.OpCode.Name, ins.Operand is String ? String.Format("\"{0}\"", ins.Operand) : ins.Operand);
                    //}
                    Console.WriteLine("cellmapsys cctor: " + cellmapsys_cctor.Body.Instructions.First());
                    //logSource.LogInfo($"cellmapsize modified to {ins.Operand} "); 
                }
                logSource.LogInfo($"target method {cellmapsys_cctor} patched.");
                // Add new instructions or logic as needed

            }//cellmap block;

        }//prepatch method;

    }//patcher class;

}//namespace;

///
///提示：BepInEx cfg设置为dump Assembly,运行prepatch获得修改后的dll,并引用作为编译pdx.mod的依赖项，以大量减少pdx.mod代码，避免pdx引用原版cellmapsize进行burst job compile；
///(不建议加入自定义cellmapsystem作为基类编译，以避免修改引用mapsize的非cellmap派生子系统);