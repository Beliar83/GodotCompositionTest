using Arch.Core;
using Godot;
using Godot.Collections;

namespace GodotComposition;

[GlobalClass]
[Tool]
public abstract partial class Component : Resource
{
    // We make the component have this, as Entity.Add is a generic and needs to know the actual type.
    public abstract void AddToEntity(Entity entity);
    
    protected virtual void InternalGetPropertyList(Array<Dictionary> properties)
    {
    }

    protected virtual Variant? InternalGet(StringName property)
    {
        return null;
    }

    protected virtual bool InternalSet(StringName property, Variant value)
    {
        return false;
    }

    /// <inheritdoc />
    public override Array<Dictionary> _GetPropertyList()
    {
        Array<Dictionary> properties = base._GetPropertyList() ?? new Array<Dictionary>();
        InternalGetPropertyList(properties);
        return properties;
    }

    /// <inheritdoc />
    public override Variant _Get(StringName property)
    {
        return InternalGet(property) ?? base._Get(property);
    }

    /// <inheritdoc />
    public override bool _Set(StringName property, Variant value)
    {
        return InternalSet(property, value) || base._Set(property, value);
    }
}