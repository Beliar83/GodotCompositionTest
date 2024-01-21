using Godot;

namespace GodotCompositionTest;

public partial class TestComponent : GodotCompositionTest.Component
{
	[Export]
	public int Test {get;set;}
}