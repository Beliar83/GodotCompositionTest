using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using Microsoft.FSharp.Core;

namespace GodotComposition;

public partial class SyncNode2DSystem : BaseSystem<World, float>
{
    /// <inheritdoc />
    public SyncNode2DSystem(World world) : base(world)
    { }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SyncNode(ref Data.Node2D node)
    {
        if (FSharpOption<Node2D>.get_IsSome(node.Instance))
        {
            node.Instance.Value.Position = node.Position;
        }
    }
}
