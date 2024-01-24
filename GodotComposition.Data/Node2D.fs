namespace GodotComposition.Data

open Components
open Godot

[<Component>]
[<Struct>]
type Node2D = {
    [<ComponentProperty>]
    mutable Template : Option<PackedScene>
    [<ComponentProperty>]
    mutable Position : Vector2
    mutable Instance : Option<Godot.Node2D> 
}    