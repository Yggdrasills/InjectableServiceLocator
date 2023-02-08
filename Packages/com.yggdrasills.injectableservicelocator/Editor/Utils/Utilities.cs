using System.IO;
using System.Linq;

using InjectableServiceLocator.Editor.Patchers;
using InjectableServiceLocator.Editor.PostProcessing;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using Unity.CompilationPipeline.Common.ILPostProcessing;

using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace InjectableServiceLocator.Editor.Utils
{
    public static class Utilities
    {
        public static AssemblyDefinition LoadAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray()),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider()
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);

            return assemblyDefinition;
        }
        
        public static MethodDefinition GetOrCreateMethodProcessor(string methodName, TypeReference methodType,
            Collection<MethodDefinition> classMethods)
        {
            var methodDefinition = FindDefinition(methodName, methodType, classMethods);

            if (methodDefinition != null)
            {
                return methodDefinition;
            }
            
            methodDefinition = CreateDefinition(methodName, MethodAttributes.Private, methodType);
            classMethods.Add(methodDefinition);

            return methodDefinition;
        }

        private static MethodDefinition FindDefinition(string methodName, TypeReference methodType,
            Collection<MethodDefinition> classMethods)
        {
            return classMethods.FirstOrDefault(method => method.Name.Equals(methodName) && method.ReturnType == methodType);
        }

        private static MethodDefinition CreateDefinition(string methodName, MethodAttributes methodAttribute, TypeReference type)
        {
            var methodDefinition = new MethodDefinition(methodName, methodAttribute, type);

            var methodProcessor = methodDefinition.Body.GetILProcessor();

            methodProcessor.Append(Instruction.Create(OpCodes.Ret));

            return methodDefinition;
        }
    }
}