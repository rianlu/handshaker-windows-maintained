using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static int Main(string[] args)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(args[1]);
        var parameters = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = true };
        var assembly = AssemblyDefinition.ReadAssembly(args[0], parameters);
        var module = assembly.MainModule;
        var helper = AssemblyDefinition.ReadAssembly(args[2]);
        var source = helper.MainModule.Types.First(type => type.Name == "ProjectHome").Methods.First(method => method.Name == "Attach");
        var attach = module.ImportReference(source);
        var preference = module.Types.First(type => type.FullName == "HandShaker.Modules.Preference.PreferenceView");
        var ctor = preference.Methods.First(method => method.IsConstructor && !method.IsStatic);
        var processor = ctor.Body.GetILProcessor();
        var ret = ctor.Body.Instructions.Last(instruction => instruction.OpCode == OpCodes.Ret);
        processor.InsertBefore(ret, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(ret, processor.Create(OpCodes.Call, attach));
        assembly.Write();
        assembly.Dispose();
        Console.WriteLine("project home button attached");
        return 0;
    }
}
