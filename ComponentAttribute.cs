using System;

namespace GodotCompositionTest;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute : Attribute
{
    public ComponentAttribute(Type type)
    {
    }
}
