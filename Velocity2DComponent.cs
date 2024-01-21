using Godot;

namespace GodotCompositionTest;

[GlobalClass]
[Tool]
public partial class Velocity2DComponent : Component
{
    [Export] public Vector2 Velocity { get; set; }
}
