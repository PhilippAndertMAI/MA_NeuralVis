using Godot;
using Microsoft.VisualBasic;
using System;
using System.Runtime.InteropServices;

public partial class PlaybackController : Node
{
	private TrainingStepper _trainingStepper;
	private double _deltaTrainingStep = 0;

	// use getter/setter for accessing via gdscript
	private bool _isPlaying = false;

	private double _basePlaybackRate = 1.0f;

	private double _playbackRate;
	[Export]
	public double minPlaybackSpeed = 1.0f;
	[Export]
	public double maxPlaybackSpeed = 5.0f;

	public void SetIsPlaying(bool isPlaying)
	{
		_isPlaying = isPlaying;
		EmitSignal("toggled_is_playing", isPlaying);
	}

	public bool GetIsPlaying()
	{
		return _isPlaying;
	}

	public void SetPlaybackRate(double speed)
	{
		_playbackRate = _basePlaybackRate / speed;
	}

	public override void _Ready()
	{
		AddUserSignal("toggled_is_playing", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "is_playing" },
				{ "type", (int)Variant.Type.Int }
			}
		});

		_playbackRate = _basePlaybackRate;
	}

	public void Initialize(TrainingStepper trainingStepper)
	{
		_trainingStepper = trainingStepper;
	}

	public void ConnectGuiSignals(Node gui)
	{
		gui.Connect("appguide_toggled_visibility", new Callable(this, nameof(HandleAppGuideToggled)));
	}

	private void HandleAppGuideToggled(bool visible)
	{
		if (visible)
		{
			SetIsPlaying(false);
		}
	}

	public override void _Process(double delta)
	{
		if (_isPlaying)
		{
			DoAutoStep(delta, _playbackRate);
		}
	}

	private void DoAutoStep(double delta, double rate)
	{
		_deltaTrainingStep += delta;
		if (_deltaTrainingStep > rate)
		{
			_deltaTrainingStep = 0;
			if (_trainingStepper.Increment())
			{
				_trainingStepper.ShowCurrentStep();
			}
		}
	}
}
