using Godot;

namespace GodotComposition.Components;

[Component]
public record struct Node2DComponent([property:ComponentProperty]PackedScene Template, [property:ComponentProperty]Vector2 Position, Node2D Instance);