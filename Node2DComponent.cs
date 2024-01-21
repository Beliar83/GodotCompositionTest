using System.ComponentModel.DataAnnotations;
using Godot;
using Godot.Collections;
using Test;
using InternalNode2Component = Test.Node2DComponent;

namespace GodotCompositionTest;

[Component(typeof(InternalNode2Component))]
[GlobalClass]
[Tool]
public partial class Node2DComponent : Component
{
}
