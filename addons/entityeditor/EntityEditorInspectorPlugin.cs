using Godot;

namespace GodotComposition.addons.entityeditor;

public partial class EntityEditorInspectorPlugin : EditorInspectorPlugin
{
    /// <inheritdoc />
    public override bool _CanHandle(GodotObject @object)
    {
        return @object is Entity;
    }

    /// <inheritdoc />
    public override void _ParseBegin(GodotObject @object)
    {
        AddCustomControl(new EntityEditor());       
    }
}
