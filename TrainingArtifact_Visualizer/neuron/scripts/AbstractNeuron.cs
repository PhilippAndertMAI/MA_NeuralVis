using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract partial class AbstractNeuron : Node3D
{
	// index of neuron in its layer
	protected int _neuronIndex;

	protected byte _intensity = 0;

	protected Sprite3D _baseSprite;
	protected Sprite3D _lightUpSprite;
	protected Sprite3D _selectedSprite;

	protected Label3D _valueLabel;
	protected Color _valueLabelBaseColor = Colors.White;
	protected string _valueLabelFormat = "0.00";
	protected string _valueLabelDiffFormat = "+#0.00;-#0.00;0.00";

	private Gradient _baseGradient;

	protected float _lightUpDuration = 1f;

	protected TweenManager _tweenManager;

	private Node _selectionController;

	protected bool _muted = false;

	public int NeuronIndex { get { return _neuronIndex; } }

	public Color[] BaseColorRange { get; set; } =
	{
		new Color(0, 0, 0),
		new Color(1, 1, 1)
	};

	[Export]
	public Gradient SelectionGradient;

	[Export]
	public float StrengthFalloffSpeed = 0.1f;

	protected Dictionary<string, Tween> _tweensDict = new Dictionary<string, Tween>();

	private Vector3 _baseScale;

	private bool _previouslyMuted = false;

	public override void _Ready()
	{
		_tweenManager = new TweenManager();
		AddChild(_tweenManager);

		_selectionController = GetNode<Node>("/root/VisualizationManager/PlayerController/SelectionController");
		_selectionController.Connect("selected_entities_changed", Callable.From(CheckMute));

		_baseSprite = GetNode<Sprite3D>("./BaseSprite");
		_selectedSprite = GetNode<Sprite3D>("./SelectedSprite");
		_selectedSprite.Visible = false;

		_valueLabel = GetNode<Label3D>("./ValueLabel");
		_valueLabel.Visible = false;

		_baseGradient = new Gradient();
		_baseGradient.InterpolationMode = Gradient.InterpolationModeEnum.Linear;
		_baseGradient.SetColor(0, BaseColorRange[0]);
		_baseGradient.SetColor(1, BaseColorRange[1]);

		_baseSprite.AlphaCut = SpriteBase3D.AlphaCutMode.Disabled;
		_baseSprite.RenderPriority = 0;
		_baseSprite.Visible = true;

		_baseScale = _baseSprite.Scale;
	}

	public virtual void Initialize(byte value, int neuronIndex) { }

	public virtual void Initialize(byte bias, byte[] weights, int layerIndex, int neuronIndex) { }

	public abstract void SetIntensity(byte intensity, bool showUpdate = false);

	public virtual void SetSelected(bool val, float selectionStrength, int selectionDirection)
	{
		_selectedSprite.Visible = val;
		_selectedSprite.Modulate = SelectionGradient.Sample(selectionStrength);

		_valueLabel.Visible = val;

		if (val)
		{
			_selectionController.Call("add_selected_entity", this);
		}
		else
		{
			_selectionController.Call("clear_selected_entities");
		}
	}

	protected void ModulateBaseColor(byte intensity)
	{
		float t = (float)intensity / byte.MaxValue;
		Color color = _baseGradient.Sample(t);
		color *= 1.5f;
		color.A = _muted ? 0.3f : 1.0f;
		_baseSprite.Modulate = color;
	}

	protected void UpdateValueLabel(byte value)
	{
		_valueLabel.Text = (value / 255.0f).ToString(_valueLabelFormat);
	}

	protected virtual void UpdateValueLabel()
	{
		UpdateValueLabel(_intensity);
	}

	protected void LightUp(byte value)
	{
		float t = (float)value / byte.MaxValue;
		TweenConfig config = new TweenConfig();
		config.TweenKey = "forwardPassScale";
		config.Object = _baseSprite;
		config.Property = "scale";
		config.FinalVal = _baseScale;
		config.Duration = _lightUpDuration;

		_tweenManager.ResetTweens(config);
		_baseSprite.Scale *= (1 + t);
	}

	protected virtual void LightUp(byte value, Color color)
	{
		LightUp(value);

		TweenConfig config = new TweenConfig();
		config.TweenKey = "forwardModulate";
		config.Object = _baseSprite;
		config.Property = "modulate";
		config.FinalVal = _baseSprite.Modulate;
		config.Duration = _lightUpDuration;

		_tweenManager.ResetTweens(config);

		color.A = _muted ? 0.3f : 1.0f;
		_baseSprite.Modulate = color;
	}

	public void CheckHideDendrites()
	{
		if (this is LearnableNeuron)
		{
			foreach (var dendrite in ((LearnableNeuron)this).ActiveDendrites.Values)
			{
				dendrite.Visible = !_muted;
				dendrite.SetCollidable(!_muted);
				dendrite.UpdateValueLabel();
			}
		}
	}

	public virtual void CheckMute() {
		Godot.Collections.Array<Node3D> selectedEntities = (Godot.Collections.Array<Node3D>)_selectionController.Call("get_selected_entities");
		if (selectedEntities.Count > 0 && !selectedEntities.Contains(this))
		{
			_baseSprite.Modulate = new Color(_baseSprite.Modulate, 0.3f);
			_muted = true;
		}
		else
		{
			_baseSprite.Modulate = new Color(_baseSprite.Modulate, 1.0f);
			_muted = false;
			UpdateValueLabel();
		}
		CheckHideDendrites();
	}

	// ----------- used to determine class in gdscript -----------
	public string GetClassName()
	{
		return GetType().Name;
	}
}
