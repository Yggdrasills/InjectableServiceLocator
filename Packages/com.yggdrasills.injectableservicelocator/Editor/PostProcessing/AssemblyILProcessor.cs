using System.Collections.Generic;
using System.IO;
using System.Linq;

using InjectableServiceLocator.Editor.Patchers;
using InjectableServiceLocator.Editor.Utils;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace InjectableServiceLocator.Editor.PostProcessing
{
    internal class AssemblyILProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(r => r == "Patcher");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
                return null;

            var assemblyDefinition = Utilities.LoadAssemblyDefinition(compiledAssembly);

            var patchers = new List<IPatcher>()
            {
                new InjectPatcher(assemblyDefinition.MainModule, compiledAssembly),
                new ServicePatcher(assemblyDefinition.MainModule, compiledAssembly)
            };

            foreach (var patcher in patchers)
            {
                patcher.ApplyPatch();
            }

            var (selfReference, selfReferenceIndex) = assemblyDefinition.MainModule.AssemblyReferences
                .Select((x, i) => (x, i))
                .FirstOrDefault(e => e.x.Name == assemblyDefinition.Name.Name);

            if (selfReference != null)
            {
                assemblyDefinition.MainModule.AssemblyReferences.RemoveAt(selfReferenceIndex);
            }

            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDefinition.Write(pe, writerParameters);

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()));
        }
    }
}