using System.Linq;
using Arch.Core;
using Godot;
using Godot.Collections;

namespace GodotComposition;

[Tool]
[GlobalClass]
public partial class ECSEntity : Node2D
{
	private World? world;
	public Entity? Entity { get; private set; }
	
	[Export] public Array<Component> Components { get; set; } = new();

	public ECSEntity()
	{
		if (Engine.IsEditorHint())
		{
			SetNotifyTransform(true);
		}
	}
	
	/// <inheritdoc />
	public override void _Notification(int what)
	{
		if (what == NotificationParented && IsNodeReady())
		{
			UpdateWorld();
		}

		if (what == NotificationTransformChanged)
		{
			Node2DComponent? node2DComponent = Components.OfType<Node2DComponent>().SingleOrDefault();

			if (node2DComponent is not null)
			{
				node2DComponent.Position = Position;				
			}
		}
			
	}

	/// <inheritdoc />
	public override void _Ready()
	{
		UpdateWorld();
	}

	private void UpdateWorld()
	{
		if (Engine.IsEditorHint()) return;
		
		var ecsWorld = GetParentOrNull<ECSWorld>();
		if (ecsWorld is null)
		{
			return;
		}


		object[] components = System.Array.Empty<object>();
		if (world is not null && Entity is not null)
		{
			components = world.GetAllComponents(Entity.Value);
			world.Destroy(Entity.Value);
		}

		world = ecsWorld.World;
		Entity = world.Create(components);
	}
}