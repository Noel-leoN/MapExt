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

            ///新方法1；增加泛型实例化；
            //cellmapsys method remake;
            //获取字段定义；0为kmapsize
            FieldDefinition cellmapsysfield = cellmapSystemType.Fields[0];
            //debug;列举字段；
            Console.WriteLine("Field type: " + cellmapsysfield.FieldType.FullName + " " + cellmapsysfield.Name);
            //获取泛型参数定义；
            GenericParameter tg = cellmapSystemType.GenericParameters[0];
            //debug;列举泛型参数；
            Console.WriteLine("Parameters type: " + tg.FullName);
            //实例化泛型类；
            GenericInstanceType of_tg = new GenericInstanceType(cellmapSystemType);
            //指定实例化泛型类的参数；
            of_tg.GenericArguments.Add(tg);
            //获取实例化泛型类的字段引用；
            FieldReference field_of_tg = new FieldReference(cellmapsysfield.Name, cellmapsysfield.FieldType) { DeclaringType = of_tg };
            //获取所有方法定义；..cctor前面已定义；
            //MethodDefinition method_GetData = cellmapSystemType.Methods[4];
            //MethodDefinition method_GetCellCenter1 = cellmapSystemType.Methods[7];
            //MethodDefinition method_GetCellCenter2 = cellmapSystemType.Methods[9];
            //Console.WriteLine("Method type: " + method_GetData.FullName);
            //Console.WriteLine("Method type: " + method_GetCellCenter1.FullName);
            //Console.WriteLine("Method type: " + method_GetCellCenter2.FullName);
            //获取方法IL;
            //实例化中修改cctor；
            if (cellmapsys_cctor != null)
            {
                ILProcessor ilProcessor0 = cellmapsys_cctor.Body.GetILProcessor();
                Instruction ldci4 = cellmapsys_cctor.Body.Instructions.FirstOrDefault(i => i.OpCode.Code == OpCodes.Ldc_I4.Code);
                Instruction stsfld = cellmapsys_cctor.Body.Instructions.FirstOrDefault(i
=> i.OpCode.Code == OpCodes.Stsfld.Code);
                ilProcessor0.Replace(ldci4, ilProcessor0.Create(OpCodes.Ldc_I4,
                    57344));
                ilProcessor0.Replace(stsfld, ilProcessor0.Create(OpCodes.Stsfld,
                    field_of_tg));
                //此处是否更改为Ldsfld;
                Console.WriteLine("cctor: " + cellmapsys_cctor.Body.Instructions.First());
                Console.WriteLine("cctor: " + ldci4.Operand);
                logSource.LogInfo($"target method {cellmapsys_cctor} for patching");
                // Add new instructions or logic as needed
            }

        }//prepatch method;

    }//patcher class;

}//namespace;

///
///提示：BepInEx cfg设置为dump Assembly,运行prepatch获得修改后的dll,并引用作为编译pdx.mod的依赖项，以大量减少pdx.mod代码，避免pdx引用原版cellmapsize进行burst job compile；
///(不建议加入自定义cellmapsystem作为基类编译，以避免修改引用mapsize的非cellmap派生子系统);