using Godot;

namespace GodotComposition;

public partial class TestComponent : Component
{
	[Export]
	public int Test {get;set;}
}