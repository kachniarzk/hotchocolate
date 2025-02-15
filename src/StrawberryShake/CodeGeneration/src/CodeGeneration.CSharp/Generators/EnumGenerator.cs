using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EnumGenerator : CodeGenerator<EnumTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumTypeDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.Name;
            path = null;

            EnumBuilder enumBuilder = EnumBuilder
                .New()
                .SetComment(descriptor.Documentation)
                .SetName(descriptor.RuntimeType.Name)
                .SetUnderlyingType(descriptor.UnderlyingType);

            foreach (EnumValueDescriptor element in descriptor.Values)
            {
                enumBuilder.AddElement(element.RuntimeValue, element.Value, element.Documentation);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(enumBuilder)
                .Build(writer);
        }
    }
}
