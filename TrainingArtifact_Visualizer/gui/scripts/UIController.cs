using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UIController : CanvasLayer
{
	TrainingStepper _trainingStepper;
	TrainingArtifactWrapper _tartWrapper;
	PlaybackController _playbackController;
	private Node _selectionController;
	ActivityMapManager _activityMapManager;

	[ExportGroup("App Guide")]
	[Export]
	Button buttonToggleAppGuide;

	[ExportGroup("Acitivty Map")]
	[Export]
	Button buttonToggleActivityMap;
	[Export]
	HSlider strengthSlider;
	[Export]
	HSlider sizeSlider;

	[ExportGroup("NetworkStats")]
	[Export]
	Button buttonToggleNetworkStats;

	[ExportGroup("Playback Buttons")]
	[Export]
	Button buttonPrev;
	[Export]
	Button buttonNext;
	[Export]
	Button buttonFastRewind;
	[Export]
	Button buttonFastForward;
	[Export]
	Button buttonPlayPause;
	[Export]
	HSlider playbackSpeedSlider;

	[ExportGroup("Indicator Buttons")]
	[Export]
	Button stepIndicatorButtonPrev;
	[Export]
	Button stepIndicatorButtonNext;
	[Export]
	Button stepIndicatorButtonCurrent;

	private List<Button> _epochControls = new List<Button>();

	public void SetEpochControls(Button[] epochControls)
	{
		_epochControls = epochControls.ToList();
	}

	// mouse "echo" params
	double _echoDelay = 0.5f;	// time after which echo begins
	double _echoRate = 0.033f;   // repeat rate of echo
	double _echoDelta = 0.0f;
	double _echoRateCounter = 0.0f;

	// button states
	bool _buttonPrevDown = false;
	bool _buttonPrevPressed = false;

	bool _buttonNextDown = false;
	bool _buttonNextPressed = false;

	bool _buttonFastRewindDown = false;
	bool _buttonFastRewindPressed = false;

	bool _buttonFastForwardDown = false;
	bool _buttonFastForwardPressed = false;

	private Timer _exitUISignalTimer;

	private PanelContainer _appGuide;
	private TabContainer _neuronStats;
	private MarginContainer _neuronStatsMargin;
	public TabContainer networkStats;

	public override void _Ready()
	{
		_trainingStepper = GetNode<TrainingStepper>("/root/VisualizationManager/TrainingStepper");
		_tartWrapper = GetNode<TrainingArtifactWrapper>("/root/VisualizationManager/TrainingArtifactWrapper");
		_playbackController = GetNode<PlaybackController>("/root/VisualizationManager/PlaybackController");
		_selectionController = GetNode<Node>("/root/VisualizationManager/PlayerController/SelectionController");
		_activityMapManager = GetNode<ActivityMapManager>("/root/VisualizationManager/ActivityMapManager");

		AddUserSignal("ui_entered_exited", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "has_entered" },
				{ "type", (int)Variant.Type.Bool }
			}
		});

		AddUserSignal("fast_forward_btn_clicked");

		AddUserSignal("epoch_clicked");

		AddUserSignal("networkstats_toggled_visibility", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "is_visible" },
				{ "type", (int)Variant.Type.Bool }
			}
		});

		AddUserSignal("appguide_toggled_visibility", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "is_visible" },
				{ "type", (int)Variant.Type.Bool }
			}
		});

		_exitUISignalTimer = new Timer();
		_exitUISignalTimer.OneShot = true;
		_exitUISignalTimer.Connect("timeout", Callable.From(() => EmitSignal("ui_entered_exited", false)));
		AddChild(_exitUISignalTimer);

		playbackSpeedSlider.ValueChanged += PlaybackSliderCallback;

		strengthSlider.ValueChanged += StrengthSliderCallback;
		sizeSlider.ValueChanged += SizeSliderCallback;

		_neuronStats = GetNode<TabContainer>("./MarginContainerNeuronStats/NeuronStats");
		SetNeuronStatsMenuVisibility(false);
		_selectionController.Connect("selected_entities_changed", Callable.From(ToggleNeuronStatsMenu));

		_neuronStatsMargin = GetNode<MarginContainer>("./MarginContainerNetworkStats");
		_appGuide = GetNode<PanelContainer>("./AppGuidePanelContainer");
		SetAppGuideVisibility(false);
		networkStats = _neuronStatsMargin.GetNode<TabContainer>("./NetworkStats");
		SetNetworkStatsMenuVisibility(false);
		buttonToggleNetworkStats.Call("initialize_icon");

		networkStats.Call("initialize", this);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			// handle buttons with custom echo-repeat logic
			if (@event.IsActionPressed("left_click"))
			{
				HandleMousePressed(@event);
			}

			// handle buttons with default logic
			bool isPlaying = _playbackController.GetIsPlaying();
			HandleButtonLogic(buttonPlayPause, Callable.From(() => _playbackController.SetIsPlaying(!isPlaying)));
			HandleButtonLogic(stepIndicatorButtonPrev, Callable.From(StepPrev));
			HandleButtonLogic(stepIndicatorButtonNext, Callable.From(StepNext));
			HandleButtonLogic(stepIndicatorButtonCurrent, Callable.From(StepCurrent));
			HandleButtonLogic(buttonToggleActivityMap, Callable.From(ToggleActivityMap));
			HandleButtonLogic(buttonToggleNetworkStats, Callable.From(ToggleNetworkStatsMenu));
			HandleButtonLogic(buttonToggleAppGuide, Callable.From(ToggleAppGuide));
			for (int i = 0; i < _epochControls.Count; i++) { HandleButtonLogic(_epochControls[i], Callable.From(() => SkipEpoch(i))); }

			HandleSliderLogic(playbackSpeedSlider);
			HandleSliderLogic(strengthSlider);
			HandleSliderLogic(sizeSlider);
			HandleNeuronStatsLogic();
			HandleNetworkStatsLogic();
		}

		if (@event.IsActionReleased("left_click"))
		{
			HandleMouseReleased();
		}
	}

	public override void _Process(double delta)
	{
		_echoDelta += delta;

		bool resetDelta =
			HandleEchoButtonLogic(ref _buttonPrevDown, ref _buttonPrevPressed, Callable.From(StepPrev), _echoDelta) &&
			HandleEchoButtonLogic(ref _buttonNextDown, ref _buttonNextPressed, Callable.From(StepNext), _echoDelta) &&
			HandleEchoButtonLogic(ref _buttonFastRewindDown, ref _buttonFastRewindPressed, Callable.From(FastRewind), _echoDelta) &&
			HandleEchoButtonLogic(ref _buttonFastForwardDown, ref _buttonFastForwardPressed, Callable.From(FastForward), _echoDelta);

		if (resetDelta)
		{
			_echoDelta = 0.0f;
		}
	}

	private void HandleMousePressed(InputEvent @event)
	{
		if (_playbackController.GetIsPlaying())
		{
			return;
		}

		var pos = buttonNext.GetGlobalMousePosition();
		if (buttonPrev.GetGlobalRect().HasPoint(pos))
		{
			_buttonPrevDown = true;
			EmitSignal("ui_entered_exited", true);
		}
		if (buttonNext.GetGlobalRect().HasPoint(pos))
		{
			_buttonNextDown = true;
			EmitSignal("ui_entered_exited", true);
		}
		if (buttonFastRewind.GetGlobalRect().HasPoint(pos))
		{
			_buttonFastRewindDown = true;
			EmitSignal("ui_entered_exited", true);
		}
		if (buttonFastForward.GetGlobalRect().HasPoint(pos))
		{
			_buttonFastForwardDown = true;
			EmitSignal("ui_entered_exited", true);
		}
	}

	private void HandleMouseReleased()
	{
		_buttonPrevDown = false;
		_buttonNextDown = false;
		_buttonFastRewindDown = false;
		_buttonFastForwardDown = false;

		_exitUISignalTimer.Start(0.2f);
	}

	private void HandleButtonLogic(Button button, Callable callback)
	{
		if (button.ButtonPressed)
		{
			callback.Call();
		}
		var pos = button.GetGlobalMousePosition();
		if (button.GetGlobalRect().HasPoint(pos))
		{
			EmitSignal("ui_entered_exited", true);
		}
	}

	private bool HandleEchoButtonLogic(ref bool buttonDown, ref bool buttonPressed, Callable callback, double delta)
	{
		// press logic
		if (buttonDown && !buttonPressed)
		{
			buttonPressed = true;
		}
		
		// repeat logic
		if (buttonDown)
		{
			if (delta > _echoDelay)
			{
				_echoRateCounter += (delta - _echoDelay);
				if (_echoRateCounter > _echoRate)
				{
					_echoRateCounter = 0.0f;
					callback.Call();
				}
			}
		}
		
		// release logic
		if (!buttonDown && buttonPressed)
		{
			buttonPressed = false;
			callback.Call();
		}

		// whether to reset the echo counting
		return !buttonDown;
	}

	private void HandleSliderLogic(Slider slider)
	{
		var pos = slider.GetGlobalMousePosition();
		if (slider.GetGlobalRect().HasPoint(pos))
		{
			EmitSignal("ui_entered_exited", true);
		}
		
		// rest of slider logic is handled via signal callback method
	}

	private void HandleNeuronStatsLogic()
	{
		if (!_neuronStats.Visible)
		{
			return;
		}

		var pos = _neuronStats.GetGlobalMousePosition();
		if (_neuronStats.GetGlobalRect().HasPoint(pos))
		{
			EmitSignal("ui_entered_exited", true);
		}

		// rest of stats logic is handled in child scripts
	}

	private void HandleNetworkStatsLogic()
	{
		if (!networkStats.Visible)
		{
			return;
		}

		var pos = networkStats.GetGlobalMousePosition();
		if (networkStats.GetGlobalRect().HasPoint(pos))
		{
			EmitSignal("ui_entered_exited", true);
		}

		// rest of stats logic is handled in child scripts
	}

	private void PlaybackSliderCallback(double value)
	{
		_playbackController.SetPlaybackRate(value);
	}

	private void StepPrev()
	{
		_playbackController.SetIsPlaying(false);

		if (_trainingStepper.Decrement()) {
			_trainingStepper.ShowCurrentStep();
		}
	}

	private void StepNext()
	{
		_playbackController.SetIsPlaying(false);

		if (_trainingStepper.Increment())
		{
			_trainingStepper.ShowCurrentStep();
		}
	}

	private void FastRewind()
	{
		_trainingStepper.FastForwardTrainingSteps(-5);
		EmitSignal("fast_forward_btn_clicked");
	}

	private void FastForward()
	{
		_trainingStepper.FastForwardTrainingSteps(5);
		EmitSignal("fast_forward_btn_clicked");
	}

	private void StepCurrent()
	{
		_playbackController.SetIsPlaying(false);

		_trainingStepper.ShowCurrentStep();
	}

	private void ToggleNeuronStatsMenu()
	{
		AbstractNeuron selectedNeuron = _selectionController.Call("get_selected_neuron").As<AbstractNeuron>();

		if (selectedNeuron != null && (selectedNeuron is LearnableNeuron || selectedNeuron is OutputNeuron)) {
			SetNeuronStatsMenuVisibility(true);
		}
		else
		{
			SetNeuronStatsMenuVisibility(false);
		}
	}

	public void ToggleAppGuide()
	{
		SetAppGuideVisibility(!_appGuide.Visible);

		EmitSignal("appguide_toggled_visibility", _appGuide.Visible);
	}

	private void SetNeuronStatsMenuVisibility(bool visible)
	{
		_neuronStats.Visible = visible;
	}

	private void SetAppGuideVisibility(bool visible)
	{
		_appGuide.Visible = visible;
	}

	private void ToggleNetworkStatsMenu()
	{
		SetNetworkStatsMenuVisibility(!networkStats.Visible);
	}

	private void SetNetworkStatsMenuVisibility(bool visible)
	{
		EmitSignal("networkstats_toggled_visibility", visible);
		networkStats.Visible = visible;
		_neuronStatsMargin.MouseFilter = visible ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Ignore;
	}

	private void ToggleActivityMap()
	{
		_activityMapManager.ToggleVisibility();
	}

	private void StrengthSliderCallback(double value)
	{
		_activityMapManager.Strength = (float)value;
	}

	private void SizeSliderCallback(double value)
	{
		_activityMapManager.Size = (float)value;
	}

	private void SkipEpoch(int epochButtonIndex)
	{
		int numSamples = _tartWrapper.TrainingArtifact.Header.NumSamples;
		int epochStartTrainingStepIndex = epochButtonIndex * numSamples;
		int trainingStepDiff = epochStartTrainingStepIndex - _trainingStepper.GetCurrentTrainingStep();

		_trainingStepper.FastForwardTrainingSteps(trainingStepDiff);

		EmitSignal("epoch_clicked");
	}
}
