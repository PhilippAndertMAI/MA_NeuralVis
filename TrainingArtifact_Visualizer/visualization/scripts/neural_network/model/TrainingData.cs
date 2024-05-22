using Godot;
using System;
using NumSharp;

public class TrainingData
{
	public NDArray Data { get; }
	public NDArray Target { get; }

	public TrainingData(NDArray data, NDArray target) => (Data, Target) = (data, target);
}
