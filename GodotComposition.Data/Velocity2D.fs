namespace GodotComposition.Data

open Components
open Godot

[<Component>]
[<Struct>]
type Velocity2D = {
    mutable Velocity : Vector2
}