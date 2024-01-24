using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using GodotComposition.Data;
using Node2D = GodotComposition.Data.Node2D;

namespace GodotComposition;

public partial class MovementSystem : BaseSystem<World, float>
{
    /// <inheritdoc />
    public MovementSystem(World world) : base(world) { }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveNode([Data] ref float delta, ref Node2D node2D, ref Velocity2D velocity)
    {
        if (node2D.Instance is null)
        {
            return;
        }

        node2D.Position += velocity.Velocity * delta;
    }

}
