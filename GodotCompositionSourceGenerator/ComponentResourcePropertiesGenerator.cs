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

        AttributeData? internalComponentAttribute = internalComponentType.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "Component" && a.AttributeClass?.ContainingNamespace.Name == "Components");
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
                    a.AttributeClass?.ContainingNamespace.Name != "Components"
                    ));
        }
        
        
        var sourceBuilder = new StringBuilder();

        var internalComponentTypeString =
            $"global::{internalComponentType.ContainingNamespace.ToDisplayString()}.{internalComponentType.Name}";

        var usings = new HashSet<string>
        {
            "Godot",
            "Godot.Collections",
            "Arch.Core",
            "Arch.Core.Extensions",
        };
        
        const string internalName = "InternalComponent";
        bool isStruct = internalComponentType.TypeKind == TypeKind.Struct;
        
        // TODO: Add fields as properties
        
        var nameBuilder = new StringBuilder();
        var getPropertyListBuilder = new StringBuilder();
        var getBuilder = new StringBuilder();
        var setBuilder = new StringBuilder();

        var propertyBuilder = new StringBuilder();
        
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
                    a.AttributeClass?.ContainingNamespace.Name == "Components");
            var typeNameBuilder = new StringBuilder();
            var isOption = false;
            ITypeSymbol? optionType = null; 
            typeNameBuilder.Append(
                $"{propertySymbol.Type.ContainingNamespace.ToDisplayString()}.{propertySymbol.Type.Name}");

            
            
            if (propertySymbol.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
            {
                typeNameBuilder.Append('<');

                if (namedTypeSymbol.Name == "FSharpOption" &&
                    namedTypeSymbol.ContainingNamespace.ToDisplayString() == "Microsoft.FSharp.Core")
                {
                    isOption = true;
                    usings.Add("Microsoft.FSharp.Core");
                    optionType = namedTypeSymbol.TypeArguments.First();
                }

                IEnumerable<string> types = namedTypeSymbol.TypeArguments.Select(ta => $"{ta.ContainingNamespace.ToDisplayString()}.{ta.Name}");

                typeNameBuilder.Append(string.Join(", ", types));
                    
                typeNameBuilder.Append('>');
            }

            if (exportProperty)
            {
                var name = $"{propertySymbol.Name}Name";
                
                nameBuilder.AppendLine($"\tprivate static readonly StringName {name} = new (\"{propertySymbol.Name}\");");

                MarshalType? marshalType = MarshalUtils.ConvertManagedTypeToMarshalType(isOption ? optionType! : propertySymbol.Type, typeCache);

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
                if (isOption)
                {
                    getBuilder.AppendLine($"\t\t\tif ({typeNameBuilder}.get_IsSome({propertySymbol.Name}))");
                    getBuilder.AppendLine("\t\t\t{");
                    getBuilder.AppendLine($"\t\t\t\treturn {propertySymbol.Name}.Value;");
                    getBuilder.AppendLine("\t\t\t}");
                    getBuilder.AppendLine("\t\t\telse");
                    getBuilder.AppendLine("\t\t\t{");
                    getBuilder.AppendLine("\t\t\t\tnew Variant();");
                    getBuilder.AppendLine("\t\t\t}");
                }
                else
                {
                    getBuilder.Append($"\t\t\treturn {propertySymbol.Name}");
                    if (propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        getBuilder.Append(" ?? new Variant()");
                    }
                    getBuilder.AppendLine(";");
                }
                getBuilder.AppendLine("\t\t}");

                setBuilder.AppendLine($"\t\tif (property == {name})");
                setBuilder.AppendLine("\t\t{");
                string conversionCall;
                ITypeSymbol propertyType = isOption ? optionType! : propertySymbol.Type;
                switch (variantType)
                {
                    case VariantType.Bool:
                        conversionCall = "AsBool()";
                        break;
                    case VariantType.Int:
                        conversionCall = propertyType.SpecialType switch
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
                        conversionCall = propertyType.SpecialType switch
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
                        conversionCall = $"As<global::{propertyType.ContainingNamespace.Name}.{propertyType.Name}>()";
                        break;
                    case VariantType.Callable:
                        conversionCall = "AsCallable()";
                        break;
                    case VariantType.Signal:
                        conversionCall = "AsSignal()";
                        break;
                    case VariantType.Dictionary:
                    {
                        if (propertyType is INamedTypeSymbol { IsGenericType: true } dictionaryTypeSymbol)
                        {
                            conversionCall =
                                $"AsGodotDictionary<{string.Join(',', dictionaryTypeSymbol.TypeArguments.Select(t => t.Name))}>()";
                        }
                        else
                        {
                            conversionCall = "AsGodotDictionary()";
                        }
                    }
                        break;
                    case VariantType.Array:
                    {
                        if (propertyType is INamedTypeSymbol { IsGenericType: true } arrayTypeSymbol)
                        {
                            conversionCall =
                                $"AsGodotArray<{string.Join(',', arrayTypeSymbol.TypeArguments.Select(t => t.Name))}>()";
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

                setBuilder.Append($"\t\t\t{propertySymbol.Name}");
                if (isOption)
                {
                    setBuilder.Append($" = new {typeNameBuilder}(value.{conversionCall})");
                }
                else
                {
                    setBuilder.Append($" = value.{conversionCall}");
                }
                
                setBuilder.AppendLine(";");
                
                setBuilder.AppendLine("\t\t\treturn true;");
                setBuilder.AppendLine("\t\t}");

            }

            propertyBuilder.Append(
                $"\tpublic {typeNameBuilder}");
            propertyBuilder.Append(
                $"{(propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "")} ");
            propertyBuilder.AppendLine(
                $"{propertySymbol.Name}");
            propertyBuilder.AppendLine("\t{");
            propertyBuilder.AppendLine($"\t\tget => {internalName}.{propertySymbol.Name};");;
            if (isStruct)
            {
                propertyBuilder.AppendLine(
                    $"\t\tset => {internalName} = {internalName} with {{ {propertySymbol.Name} = value }};");
            }
            else
            {
                propertyBuilder.AppendLine($"\t\tset => {internalName}.{propertySymbol.Name} = value;");
            }
            propertyBuilder.AppendLine("\t}");
            
        }
        
        getPropertyListBuilder.AppendLine();
        getPropertyListBuilder.AppendLine("\t}");

        getBuilder.AppendLine("\t\treturn null;");
        getBuilder.AppendLine("\t}");
        
        setBuilder.AppendLine("\t\treturn false;");
        setBuilder.AppendLine("\t}");

        foreach (string @using in usings)
        {
            sourceBuilder.AppendLine($"using {@using};");
        }
        
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {componentType.ContainingNamespace.ToDisplayString()};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public partial class {componentType.Name}");
        sourceBuilder.AppendLine("{");
        if (isStruct)
        {
            sourceBuilder.AppendLine($"\tinternal {internalComponentTypeString} {internalName} {{ get; set;}} = new();");
        }
        else
        {
            sourceBuilder.AppendLine($"\tinternal {internalComponentTypeString} {internalName} {{ get; }}  = new();");
        }
        sourceBuilder.AppendLine();        sourceBuilder.Append(nameBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.Append(propertyBuilder);
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("\tpublic override void AddToEntity(Entity entity)");
        sourceBuilder.AppendLine("\t{");
        sourceBuilder.AppendLine("\t\tentity.Add(InternalComponent);");
        sourceBuilder.AppendLine("\t}");
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
