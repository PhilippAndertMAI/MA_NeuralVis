using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TweenManager : Node
{
	private Dictionary<string, Tween> _tweenDict;

	public TweenManager()
	{
		_tweenDict = new Dictionary<string, Tween>();
	}

	public void ResetTweens(params TweenConfig[] tweenConfig)
	{
		foreach (var config in tweenConfig)
		{
			Tween tween;
			if (_tweenDict.TryGetValue(config.TweenKey, out tween))
			{
				tween.CustomStep(config.Duration * 2);
				tween.Kill();
			}
			tween = GetTree().CreateTween();
			if (config.Method != null)
			{
				tween.TweenMethod(Callable.From(config.Method), config.InitialVal, config.FinalVal, config.Duration);
			}
			else
			{
				tween.TweenProperty(config.Object, config.Property, config.FinalVal, config.Duration);
			}
			if (config.Callback != null)
			{
				tween.TweenCallback((Callable)config.Callback);
			}
			_tweenDict[config.TweenKey] = tween;
		}
	}

	public void FinishTween(string tweenKey)
	{
		Tween tween;
		if (_tweenDict.TryGetValue(tweenKey, out tween))
		{
			tween.CustomStep(10f);
			tween.Kill();
		}
	}

	/// <summary>
	/// Hacky solution to not instantly delete a node while a tween is running.
	/// </summary>
	/// <param name="config"></param>
	public void DeleteAfterTweens(Node nodeToDelete)
	{
		// TODO: actually append to tween that will run the longest
		if (_tweenDict.Count > 0)
		{
			Tween tween = _tweenDict.Values.ToArray()[0];
			if (tween.IsRunning() && tween.IsValid())
			{
				tween.Chain()
				.TweenCallback(Callable.From(() => nodeToDelete.QueueFree()));
			}
			else
			{
				nodeToDelete.QueueFree();
			}
		}
		else
		{
			nodeToDelete.QueueFree();
		}
	}

	public void KillAll()
	{
		foreach (var tween in _tweenDict.Values)
		{
			tween.Kill();
		}
	}
}
