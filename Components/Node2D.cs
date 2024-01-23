using Godot;

namespace Components;

[Component]
public record struct Node2D([property:ComponentProperty]PackedScene? Template, [property:ComponentProperty]Vector2 Position, Godot.Node2D? Instance);