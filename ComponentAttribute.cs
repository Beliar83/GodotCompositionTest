using System;

namespace GodotComposition;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute : Attribute
{
    public ComponentAttribute(Type type)
    {
    }
}
