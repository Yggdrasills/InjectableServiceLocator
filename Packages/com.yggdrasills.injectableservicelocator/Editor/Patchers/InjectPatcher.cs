using System;
using System.Linq;
using System.Reflection;

using InjectableServiceLocator.Editor.Utils;
using InjectableServiceLocator.Services;
using InjectableServiceLocator.Services.Attributes;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using Unity.CompilationPipeline.Common.ILPostProcessing;

using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace InjectableServiceLocator.Editor.Patchers
{
    internal class InjectPatcher : IPatcher
    {
        private readonly ModuleDefinition _module;
        private readonly ICompiledAssembly _compiledAssembly;

        internal InjectPatcher(ModuleDefinition module, ICompiledAssembly compiledAssembly)
        {
            _module = module;
            _compiledAssembly = compiledAssembly;
        }

        void IPatcher.ApplyPatch()
        {
            var assemblyDefinition = Assembly.Load(_compiledAssembly.InMemoryAssembly.PeData);

            foreach (var type in _module.Types)
            {
                foreach (var field in type.Fields)
                {
                    var assemblyType = assemblyDefinition.GetType(field.FieldType.FullName);

                    if (!HasInjectAttribute(field))
                        continue;

                    var setFieldInstruction = Instruction.Create(OpCodes.Stfld, field);

                    ApplyPatch("Start", _module, type.Methods, "Get", assemblyType, setFieldInstruction);
                }
            }
        }

        private bool HasInjectAttribute(ICustomAttributeProvider field)
        {
            if (!field.HasCustomAttributes)
                return false;

            var hasInjectAttribute = field.CustomAttributes
                .Any(attribute => attribute.AttributeType.Name.Contains(nameof(InjectAttribute)));

            return hasInjectAttribute;
        }

        private void ApplyPatch(string methodName, ModuleDefinition moduleDefinition,
            Collection<MethodDefinition> methods, string serviceLocatorMethod,
            Type assemblyType, params Instruction[] additionalInstructions)
        {
            var getInstanceMethod = typeof(ServiceLocator).GetMethod("get_Current");
            var resolveGenericMethod = typeof(ServiceLocator).GetMethod(serviceLocatorMethod);

            var ilProcessor = Utilities.GetOrCreateMethodProcessor(methodName,
                    moduleDefinition.TypeSystem.Void, methods)
                .Body.GetILProcessor();

            var processorInstruction = ilProcessor.Body.Instructions[0];
            var genericMethod = resolveGenericMethod?.MakeGenericMethod(assemblyType);

            var loadArg = Instruction.Create(OpCodes.Ldarg_0);
            var callInstanceInstruction =
                Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(getInstanceMethod));
            var virtualInstruction =
                Instruction.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(genericMethod));

            ilProcessor.InsertBefore(processorInstruction, loadArg);
            ilProcessor.InsertBefore(processorInstruction, callInstanceInstruction);
            ilProcessor.InsertBefore(processorInstruction, virtualInstruction);

            for (int i = 0; i < additionalInstructions.Length; i++)
            {
                ilProcessor.InsertBefore(processorInstruction, additionalInstructions[i]);
            }
        }
    }
}