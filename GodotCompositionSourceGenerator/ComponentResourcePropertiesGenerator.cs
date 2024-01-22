using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Godot.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotCompositionSourceGenerator;

[Generator]
public class ComponentResourcePropertiesGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> componentTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "GodotComposition.ComponentAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) => (INamedTypeSymbol)syntaxContext.TargetSymbol);
        
        
        context.RegisterSourceOutput(componentTypes.Combine(context.CompilationProvider), Generate);
    }

    private void Generate(SourceProductionContext context, (INamedTypeSymbol componentType, Compilation compilation) values)
    {
        INamedTypeSymbol componentType = values.componentType;
        Compilation compilation = values.compilation;
        var typeCache = new MarshalUtils.TypeCache(compilation);

        AttributeData componentAttribute = componentType.GetAttributes(
        ).Single(a =>
            a.AttributeClass?.ContainingNamespace.Name == "GodotComposition" &&
            a.AttributeClass?.Name == "ComponentAttribute");

        if (componentAttribute.AttributeConstructor is null) return;

        var internalComponentType = (INamedTypeSymbol)componentAttribute.ConstructorArguments.First().Value!;

        // ITypeSymbol internalComponentType = componentAttribute.AttributeConstructor.TypeArguments.First();
        AttributeData? internalComponentAttribute = internalComponentType.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "Component" && a.AttributeClass?.ContainingNamespace.Name == "GodotComposition.Components");
        bool exportAllProperties;
        List<IPropertySymbol> members = internalComponentType.GetMembers().OfType<IPropertySymbol>().ToList();
        if (internalComponentAttribute is not null)
        {
            exportAllProperties = (bool)internalComponentAttribute.ConstructorArguments.First().Value!;
        }
        else
        {
            exportAllProperties = members.All(p =>
                p.GetAttributes().All(a => 
                    a.AttributeClass?.Name != "ComponentPropertyAttribute" ||
                    a.AttributeClass?.ContainingNamespace.Name != "GodotComposition.Components"
                    ));
        }
        
        const string internalName = "internalComponent";
        
        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine("using Godot;");
        sourceBuilder.AppendLine("using Godot.Collections;");
        sourceBuilder.AppendLine("using System.Diagnostics;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {componentType.ContainingNamespace.Name};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public partial class {componentType.Name}");
        sourceBuilder.AppendLine("{");
        if (internalComponentType.TypeKind == TypeKind.Struct)
        {
            sourceBuilder.AppendLine($"\tprivate global::{internalComponentType.ContainingNamespace.Name}.{internalComponentType.Name} {internalName} = new();");
        }
        else
        {
            sourceBuilder.AppendLine($"\tprivate readonly global::{internalComponentType.ContainingNamespace.Name}.{internalComponentType.Name} {internalName} = new();");
        }
        sourceBuilder.AppendLine();
        
        
        var nameBuilder = new StringBuilder();
        var getPropertyListBuilder = new StringBuilder();
        var getBuilder = new StringBuilder();
        var setBuilder = new StringBuilder();
        
        getPropertyListBuilder.AppendLine("\tprotected override void InternalGetPropertyList(Array<Dictionary> properties)");
        getPropertyListBuilder.AppendLine("\t{");
        getPropertyListBuilder.AppendLine();

        getBuilder.AppendLine("\tprotected override Variant? InternalGet(StringName property)");
        getBuilder.AppendLine("\t{");

        setBuilder.AppendLine("\tprotected override bool InternalSet(StringName property, Variant value)");
        setBuilder.AppendLine("\t{");

        foreach (IPropertySymbol propertySymbol in members)
        {
            bool exportProperty = 
                exportAllProperties || 
                propertySymbol.GetAttributes().Any(a => 
                    a.AttributeClass?.Name == "ComponentPropertyAttribute" &&
                    a.AttributeClass?.ContainingNamespace.Name == "GodotComposition.Components");
            if (exportProperty)
            {
                var name = $"{propertySymbol.Name}Name";
                
                nameBuilder.AppendLine($"\tprivate static readonly StringName {name} = new (\"{propertySymbol.Name}\");");

                MarshalType? marshalType = MarshalUtils.ConvertManagedTypeToMarshalType(propertySymbol.Type, typeCache);

                if (marshalType is null)
                {
                    // TODO: Report
                    continue;
                }

                VariantType? variantType = MarshalUtils.ConvertMarshalTypeToVariantType(marshalType.Value);
                
                if (variantType is null)
                {
                    // TODO: Report
                    continue;
                }

                getPropertyListBuilder.AppendLine("\t\t{");
                getPropertyListBuilder.AppendLine("\t\t\tvar propertyData = new Dictionary();");
                getPropertyListBuilder.AppendLine($"\t\t\tpropertyData[\"name\"] = {name};");
                getPropertyListBuilder.AppendLine($"\t\t\tpropertyData[\"type\"] = {(int)variantType};");
                getPropertyListBuilder.AppendLine("\t\t\tpropertyData[\"usage\"] = (int)PropertyUsageFlags.Default;");
                // TODO: Hint and hint string (Either use Export Attribute from Godot, or a separate attribute)
                getPropertyListBuilder.AppendLine("\t\t\tpropertyData[\"hint\"] = (int)PropertyHint.None;");
                getPropertyListBuilder.AppendLine("\t\t\tpropertyData[\"hint_string\"] = \"\";");
                getPropertyListBuilder.AppendLine("\t\t\tproperties.Add(propertyData);");
                getPropertyListBuilder.AppendLine("\t\t}");
                
                getBuilder.AppendLine($"\t\tif (property == {name})");
                getBuilder.AppendLine("\t\t{");
                getBuilder.AppendLine($"\t\t\treturn {internalName}.{propertySymbol.Name};");
                getBuilder.AppendLine("\t\t}");

                setBuilder.AppendLine($"\t\tif (property == {name})");
                setBuilder.AppendLine("\t\t{");
                string conversionCall;
                switch (variantType)
                {
                    case VariantType.Bool:
                        conversionCall = "AsBool()";
                        break;
                    case VariantType.Int:
                        conversionCall = propertySymbol.Type.SpecialType switch
                        {
                            SpecialType.System_Int16 => "AsInt16()",
                            SpecialType.System_Int32 => "AsInt32()",
                            SpecialType.System_Int64 => "AsInt32()",
                            SpecialType.System_UInt16 => "AsUInt16()",
                            SpecialType.System_UInt32 => "AsUInt32()",
                            SpecialType.System_UInt64 => "AsUInt32()",
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                        break;
                    case VariantType.Float:
                        conversionCall = propertySymbol.Type.SpecialType switch
                        {
                            SpecialType.System_Single => "AsSingle()",
                            SpecialType.System_Double => "AsDouble()",
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                        break;
                    case VariantType.String:
                        conversionCall = "AsString()";
                        break;
                    case VariantType.Vector2:
                        conversionCall = "AsVector2()";
                        break;
                    case VariantType.Vector2I:
                        conversionCall = "AsVector2I()";
                        break;
                    case VariantType.Rect2:
                        conversionCall = "AsRect2()";
                        break;
                    case VariantType.Rect2I:
                        conversionCall = "AsRect2I()";
                        break;
                    case VariantType.Vector3:
                        conversionCall = "AsVector3()";
                        break;
                    case VariantType.Vector3I:
                        conversionCall = "AsVector3I()";
                        break;
                    case VariantType.Transform2D:
                        conversionCall = "AsTransform2D";
                        break;
                    case VariantType.Vector4:
                        conversionCall = "AsVector4()";
                        break;
                    case VariantType.Vector4I:
                        conversionCall = "AsVector4I()";
                        break;
                    case VariantType.Plane:
                        conversionCall = "AsPlane()";
                        break;
                    case VariantType.Quaternion:
                        conversionCall = "AsQuaternion()";
                        break;
                    case VariantType.Aabb:
                        conversionCall = "AsAabb()";
                        break;
                    case VariantType.Basis:
                        conversionCall = "AsBasis()";
                        break;
                    case VariantType.Transform3D:
                        conversionCall = "AsTransform3D()";
                        break;
                    case VariantType.Projection:
                        conversionCall = "AsProjection()";
                        break;
                    case VariantType.Color:
                        conversionCall = "AsColor()";
                        break;
                    case VariantType.StringName:
                        conversionCall = "AsStringName()";
                        break;
                    case VariantType.NodePath:
                        conversionCall = "AsNodePath()";
                        break;
                    case VariantType.Rid:
                        conversionCall = "AsRid()";
                        break;
                    case VariantType.Object:
                        conversionCall = $"As<global::{propertySymbol.Type.ContainingNamespace.Name}.{propertySymbol.Type.Name}>()";
                        break;
                    case VariantType.Callable:
                        conversionCall = "AsCallable()";
                        break;
                    case VariantType.Signal:
                        conversionCall = "AsSignal()";
                        break;
                    case VariantType.Dictionary:
                    {
                        if (propertySymbol.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
                        {
                            conversionCall =
                                $"AsGodotDictionary<{string.Join(',', namedTypeSymbol.TypeArguments.Select(t => t.Name))}>()";
                        }
                        else
                        {
                            conversionCall = "AsGodotDictionary()";
                        }
                    }
                        break;
                    case VariantType.Array:
                    {
                        if (propertySymbol.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
                        {
                            conversionCall =
                                $"AsGodotArray<{string.Join(',', namedTypeSymbol.TypeArguments.Select(t => t.Name))}>()";
                        }
                        else
                        {
                            conversionCall = "AsGodotArray()";
                        }
                    }
                        break;
                    case VariantType.PackedByteArray:
                        conversionCall = "AsByteArray()";
                        break;
                    case VariantType.PackedInt32Array:
                        conversionCall = "AsInt32Array()";
                        break;
                    case VariantType.PackedInt64Array:
                        conversionCall = "AsInt64Array()";
                        break;
                    case VariantType.PackedFloat32Array:
                        conversionCall = "AsFloat32Array()";
                        break;
                    case VariantType.PackedFloat64Array:
                        conversionCall = "AsFloat64Array()";
                        break;
                    case VariantType.PackedStringArray:
                        conversionCall = "AsStringArray()";
                        break;
                    case VariantType.PackedVector2Array:
                        conversionCall = "AsVector2Array()";
                        break;
                    case VariantType.PackedVector3Array:
                        conversionCall = "AsVector3Array()";
                        break;
                    case VariantType.PackedColorArray:
                        conversionCall = "AsColorArray()";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (internalComponentType.TypeKind == TypeKind.Struct)
                {
                    setBuilder.AppendLine($"\t\t\t{internalName} = {internalName} with {{ {propertySymbol.Name} = value.{conversionCall} }};");                    
                }
                else
                {
                    setBuilder.AppendLine($"\t\t\t{internalName}.{propertySymbol.Name} = value.{conversionCall};");
                }
                
                // setBuilder.AppendLine("\t\t\t");
                setBuilder.AppendLine("\t\treturn true;");
                setBuilder.AppendLine("\t\t}");
                
                
            }
        }
        
        getPropertyListBuilder.AppendLine();
        getPropertyListBuilder.AppendLine("\t}");

        getBuilder.AppendLine("\t\treturn null;");
        getBuilder.AppendLine("\t}");
        
        setBuilder.AppendLine("\t\treturn false;");
        setBuilder.AppendLine("\t}");
        
        sourceBuilder.Append(nameBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.Append(getPropertyListBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.Append(getBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.Append(setBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("}");

        context.AddSource(componentType.Name, sourceBuilder.ToString().Replace("\t", "    "));
    }
}
