using Arch.Core;
using Godot;
using Godot.Collections;

namespace GodotComposition;

[Tool]
[GlobalClass]
public partial class Entity : Node
{
	private World world;
	private Arch.Core.Entity? entity;
	
	[Export] public Array<Component> Components { get; set; } = new();

	/// <inheritdoc />
	public override void _Notification(int what)
	{
		if (what == NotificationParented && IsNodeReady())
		{
			UpdateWorld();
		}
	}

	/// <inheritdoc />
	public override void _Ready()
	{
		UpdateWorld();
	}

	private void UpdateWorld()
	{
		var ecsWorld = GetParentOrNull<ECSWorld>();
		if (ecsWorld is null)
		{
			return;
		}


		object[] components = System.Array.Empty<object>();
		if (world is not null && entity is not null)
		{
			components = world.GetAllComponents(entity.Value);
			world.Destroy(entity.Value);
		}

		world = ecsWorld.World;
		entity = world.Create(components);
	}
}