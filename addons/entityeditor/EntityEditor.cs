using Godot;

namespace GodotCompositionTest.addons.entityeditor;

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
