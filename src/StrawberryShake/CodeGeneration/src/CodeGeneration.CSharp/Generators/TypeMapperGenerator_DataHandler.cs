using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        private const string _dataParameterName = "data";

        private void AddDataHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            method
                .AddParameter(_dataParameterName)
                .SetType(namedTypeDescriptor.ParentRuntimeType!
                    .ToString()
                    .MakeNullable(!isNonNullable));
            method
                .AddParameter(_snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot);

            if (!isNonNullable)
            {
                method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
            }

            const string returnValue = nameof(returnValue);

            method.AddCode($"{namedTypeDescriptor.RuntimeType.Name} {returnValue} = default!;");
            method.AddEmptyLine();

            GenerateIfForEachImplementedBy(
                method,
                namedTypeDescriptor,
                o => GenerateDataInterfaceIfClause(o, isNonNullable, returnValue));

            method.AddCode($"return {returnValue};");

            AddRequiredMapMethods(
                _dataParameterName,
                namedTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }

        private IfBuilder GenerateDataInterfaceIfClause(
            ObjectTypeDescriptor objectTypeDescriptor,
            bool isNonNullable,
            string variableName)
        {
            ICode ifCondition = MethodCallBuilder
                .Inline()
                .SetMethodName(
                    _dataParameterName.MakeNullable(!isNonNullable),
                    "__typename",
                    nameof(string.Equals))
                .AddArgument(objectTypeDescriptor.Name.AsStringToken())
                .AddArgument(TypeNames.OrdinalStringComparison);

            if (!isNonNullable)
            {
                ifCondition = NullCheckBuilder
                    .New()
                    .SetCondition(ifCondition)
                    .SetSingleLine()
                    .SetDetermineStatement(false)
                    .SetCode("false");
            }

            MethodCallBuilder constructorCall = MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(objectTypeDescriptor.RuntimeType.Name);

            foreach (PropertyDescriptor prop in objectTypeDescriptor.Properties)
            {
                var propAccess = $"{_dataParameterName}.{prop.Name}";
                if (prop.Type.IsEntityType() || prop.Type.IsDataType())
                {
                    constructorCall.AddArgument(BuildMapMethodCall(_dataParameterName, prop, true));
                }
                else
                {
                    constructorCall
                        .AddArgument(
                            NullCheckBuilder
                                .Inline()
                                .SetCondition(propAccess)
                                .SetCode(ExceptionBuilder.Inline(TypeNames.ArgumentNullException)));
                }
            }

            return IfBuilder
                .New()
                .SetCondition(ifCondition)
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(variableName)
                    .SetRighthandSide(constructorCall));
        }
    }
}
