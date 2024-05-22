using Godot;
using System;
using System.Runtime.CompilerServices;

public class TweenConfig
{
	public string TweenKey { get; set; }
	public GodotObject Object { get; set; }
	public NodePath Property { get; set; }
	public Action<Variant> Method { get; set; } = null;
	public Callable? Callback { get; set; } = null;
	public Variant InitialVal { get; set; }
	public Variant FinalVal { get; set; }
	public float Duration { get; set; }
}
