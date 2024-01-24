using Godot;

namespace GodotComposition;

[Component(typeof(GodotComposition.Data.Node2D))]
[GlobalClass]
[Tool]
public partial class Node2DComponent : Component
{

}

// [Component(typeof(GodotComposition.Data.Node2D))]
// [GlobalClass]
// [Tool]
// public partial class Node2DComponentT : Component
// {
//     public void sdf()
//     {
//         var node2D = new Data.Node2D(null, Vector2.One, null);
//         node2D with { Template = null };
//     }
// }