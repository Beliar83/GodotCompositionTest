using System.Linq;
using Godot;

namespace GodotComposition;

[Tool]
[GlobalClass]
public partial class ECSWorld : Node
{
	public Arch.Core.World World { get; } = Arch.Core.World.Create();

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		foreach (Entity entity in GetChildren().OfType<Entity>())
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
				// if (node2DComponent.Template is null) continue;
				// if (node2DComponent.Instance != null)
				// {
				// 	continue;
				// }
				//
				// var instance = node2DComponent.Template.Instantiate<Node2D>();
				// instances.AddChild(instance);
				// node2DComponent.Instance = instance;
			}
		}
	}

	/// <inheritdoc />
	public override void _PhysicsProcess(double delta)
	{
		// foreach (Entity entity in GetChildren().OfType<Entity>())
		// {
		// 	Velocity2DComponent velocity2DComponent = entity.Components.OfType<Velocity2DComponent>().FirstOrDefault();
		//
		// 	foreach (Node2DComponent node2DComponent in entity.Components.OfType<Node2DComponent>())
		// 	{
		// 		if (node2DComponent.Instance is null) continue;
		// 		node2DComponent.Instance.Position = node2DComponent.Position;
		// 		if (!Engine.IsEditorHint() && velocity2DComponent is not null)
		// 		{
		// 			node2DComponent.Instance.Position += velocity2DComponent.Velocity;
		// 		}
		// 		node2DComponent.Position = node2DComponent.Instance.Position;
		// 	}
		// }
	}
}
