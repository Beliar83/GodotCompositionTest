using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class Entity : Node
{
	[Export] public Array<GodotCompositionTest.Component> Components { get; set; } = new();
	
}
