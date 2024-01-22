namespace GodotComposition.Components;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentAttribute : Attribute
{
    public ComponentAttribute(bool exportAllPublicProperties=true) {}
}
