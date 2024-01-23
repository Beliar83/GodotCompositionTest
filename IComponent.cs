namespace GodotComposition;

public interface IComponent<out T>
{
    internal T InternalComponent { get; } 
}
