using Godot;

namespace Test;

[Component]
public record struct Node2DComponent([property:ComponentProperty]PackedScene Template, [property:ComponentProperty]Vector2 Position, Node2D Instance);
