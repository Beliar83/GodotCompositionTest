using System;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using Node2D = Godot.Node2D;

namespace GodotComposition;

[Tool]
[GlobalClass]
public partial class ECSWorld : Node {
	
	public Arch.Core.World World { get; }
	private readonly Group<float> systems;

	// We only need the World and systems in the actual game
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ECSWorld()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		World = World.Create();
		systems = new Group<float>(
			new MovementSystem(World),
			new SyncNode2DSystem(World)
			);
	}
	
	/// <inheritdoc />
	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			foreach (ECSEntity entity in GetChildren().OfType<ECSEntity>())
			{
				foreach (Component entityComponent in entity.Components)
				{
					if (entity.Entity.HasValue)
					{
						entityComponent.AddToEntity(entity.Entity.Value);
					}
				}
			}
		}
	}

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		foreach (ECSEntity entity in GetChildren().OfType<ECSEntity>())
		{
			Node instances = entity.GetNodeOrNull("_Instances");
			if (instances is null)
			{
				instances = new Node
				{
					Name = "_Instances",
				};
				entity.AddChild(instances);
			}
			if (!entity.Components.Any() || entity.Components.All(c => c == null)) continue;
			foreach (Node2DComponent node2DComponent in entity.Components.OfType<Node2DComponent>())
			{
				if (node2DComponent.Template is null) continue;
				if (node2DComponent.Instance != null)
				{
					continue;
				}
				
				var instance = node2DComponent.Template.Instantiate<Node2D>();
				instances.AddChild(instance);
				node2DComponent.InternalComponent = node2DComponent.InternalComponent with { Instance = instance };
				if (Engine.IsEditorHint())
				{
					continue;
				}

				if (entity.Entity?.Has<Components.Node2D>() ?? false)
				{
					entity.Entity?.Set(node2DComponent.InternalComponent);
				}
				else
				{
					entity.Entity?.Add(node2DComponent.InternalComponent);
				}
			}
		}
	}

	/// <inheritdoc />
	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint())
		{
			foreach (ECSEntity entity in GetChildren().OfType<ECSEntity>())
			{
				Velocity2DComponent? velocity2DComponent =
					entity.Components.OfType<Velocity2DComponent>().FirstOrDefault();

				foreach (Node2DComponent node2DComponent in entity.Components.OfType<Node2DComponent>())
				{
					if (node2DComponent.Instance is null) continue;
					node2DComponent.Instance.Position = node2DComponent.Position;
					if (!Engine.IsEditorHint() && velocity2DComponent is not null)
					{
						node2DComponent.Instance.Position += velocity2DComponent.Velocity;
					}
					
					entity.Position = node2DComponent.Position;
				}
			}
		}
		else
		{
			systems.BeforeUpdate((float)delta);
			systems.Update(0);
		}
	}
	

}
