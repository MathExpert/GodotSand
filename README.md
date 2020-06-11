# GodotSand
A basic falling sand game implementation for Godot in C#

This could be used as a template for anyone who wants to implement something similar in their own Godot game.

Current version is very simple, supports only sand, water and wall pixel types, and no special optimizations.
I am not planning to develop this further, as this was just a tiny test project to understand how this type of games is implemented.

The implementation is using a single `Sprite`, for which a reference to its `Image` is stored.
The painting is done using `set_pixel` method on the `Image`. I've tried using GDScript (Godot 3.2), but the performance was very poor
for some reason, even in an extremely simple example. For this reason I am using C# here, which results in a much better performance. That being said,
I don't have much experience with C#, so the code may have some obvious common C# practices/style violations.

Some obvious next steps in further development could be:

* optimize the code by working with a `ByteArray` directly instead (I haven't tested if it boosts performance though)
* add dirty boxing (see the Noita talk in the [References](References))
* use a 2D grid of `Sprite`s, and process them in multiple threads, again, as in the Noita talk

# References

* [Noita](https://noitagame.com/) game and [Noita talk](https://www.youtube.com/watch?v=prXuyMCgbTc)
* [Sandspiel](https://sandspiel.club/), [Making Sandspiel](https://maxbittker.com/making-sandspiel/), and its [source code](https://github.com/MaxBittker/sandspiel)
* A more advanced Godot C# [implementation](https://www.reddit.com/r/godot/comments/f6s141/made_a_falling_sand_engine_for_my_alchemy_game/),
  unfortunately without the source code (although the author might share if you ask nicely?). The thread has a few pieces of advice though,
  and even a helpful comment on how to use GDNative.
* [Recreating Noita's Sand Simulation in C and OpenGL](https://www.youtube.com/watch?v=VLZjd_Y1gJ8)
* [WebGL Fluid simulation](https://github.com/PavelDoGreat/WebGL-Fluid-Simulation). Not directly connected, but could be used for nice visual effects (and is indeed used in Sandspiel to simulate the wind).
* [2D Liquid simulator with cellular automaton in Godot Engine](https://www.youtube.com/watch?v=nF7cdUVgvNc). A very interesting approach
  that also shows the amount of water in each "pixel", allowing for a smoother look.
* [How To Make a “Falling Sand” Style Water Simulation](https://w-shadow.com/blog/2009/09/29/falling-sand-style-water-simulation/)
