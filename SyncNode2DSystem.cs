using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

namespace GodotComposition;

public partial class SyncNode2DSystem : BaseSystem<World, float>
{
    /// <inheritdoc />
    public SyncNode2DSystem(World world) : base(world)
    { }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SyncNode(ref Node2D node)
    {
        if (node.Instance is not null)
        {
            node.Instance.Position = node.Position;
        }
    }
}
