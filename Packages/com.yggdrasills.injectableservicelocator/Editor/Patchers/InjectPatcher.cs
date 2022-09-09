using System;
using System.Linq;
using System.Reflection;

using InjectableServiceLocator.Editor.Utils;
using InjectableServiceLocator.Services;
using InjectableServiceLocator.Services.Attributes;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace InjectableServiceLocator.Editor.Patchers
{
    public class InjectPatcher : IPatcher
    {
        public void ApplyPatch(ModuleDefinition moduleDefinition, Assembly assemblyDefinition)
        {
            foreach (var type in moduleDefinition.Types)
            {
                foreach (var field in type.Fields)
                {
                    var assemblyType = assemblyDefinition.GetType(field.FieldType.FullName);

                    if (!HasInjectAttribute(field))
                        continue;

                    var setFieldInstruction = Instruction.Create(OpCodes.Stfld, field);

                    ApplyPatch("Start", moduleDefinition, type.Methods, "Get", assemblyType, setFieldInstruction);
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
            var callInstanceInstruction = Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(getInstanceMethod));
            var virtualInstruction = Instruction.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(genericMethod));

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