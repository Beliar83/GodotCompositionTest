#if TOOLS
using Godot;

namespace GodotComposition.addons.entityeditor;

[Tool]
public partial class EntityEditorPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
	}
}
#endif
