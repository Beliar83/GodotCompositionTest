using Godot;

namespace GodotComposition.addons.entityeditor;

public partial class EntityEditor : VBoxContainer
{
    private MenuButton addComponentButton;

    public EntityEditor()
    {
        addComponentButton = new MenuButton
        {
            Text = "Add Component",
            Flat = false,
        };
    }
}
