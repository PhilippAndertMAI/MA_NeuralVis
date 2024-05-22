using Godot;
using NumSharp.Extensions;
using System;
using System.Collections.Generic;

public partial class Dendrite : Node3D
{
	// position in neural network
	private int _layerIndex;
	// the end neuron is the neuron associated with the weight of this dendrite
	private int _endNeuronIndex;
	// index of neuron connected to the end neuron (which is always in the previous layer)
	private int _startNeuronIndex;

	private byte _weight = 0;

	private Vector3 _startPos;
	private Vector3 _endPos;

	private Node3D _line3D;
	private Node3D _selectedLine3D;

	private float _backpropLightUpDuration = 1f;

	private Label3D _valueLabel;
	private Color _valueLabelBaseColor = Colors.White;
	private string _valueLabelFormat = "0.00";
	private string _valueLabelDiffFormat = "+#0.00;-#0.00;0.00";

	private CollisionShape3D _collisionShape3d;

	private Gradient _baseGradient;

	private Gradient _pulseGradient;
	private float[] _pulseOffsets;

	private float _pulseSpeed = 25f;

	private StandardMaterial3D _material;

	private float _strengthFalloffSpeed = 0.1f;

	private TweenManager _tweenManager;

	private NetworkManager _networkManager;

	private float _colliderThickness = 0.15f;

	public int LayerIndex { get { return _layerIndex; } }
	public int EndNeuronIndex { get { return _endNeuronIndex; } }
	public int StartNeuronIndex { get { return _startNeuronIndex; } }

	// index of the neuron in the next layer that this neuron (end neuron) is connected to via a dendrite
	public int ConnectedNeuronIndex { get; set; } = -1; // is set when another dendrite connects from a higher layer

	public byte Weight { get { return _weight; } }

	[Export]
	public Gradient BackpropGradient;

	[Export]
	public Gradient SelectionGradient;

	public override void _Ready()
	{
		_tweenManager = new TweenManager();
		AddChild(_tweenManager);

		_baseGradient = new Gradient();
		_baseGradient.InterpolationMode = Gradient.InterpolationModeEnum.Cubic;
		_baseGradient.SetColor(0, Colors.Black);
		_baseGradient.SetColor(1, Colors.White);

		_pulseGradient = new Gradient();
		_pulseGradient.InterpolationMode = Gradient.InterpolationModeEnum.Constant;

		// offsets to be interpolated when "sending" a pulse
		_pulseOffsets = new float[]
		{
			0.0f,
			0.4f,
			0.5f,
			0.6f,
			1
		};
		// initialize pulse gradient out-of-bounds and set to all-white
		// points 1 and 2
		_pulseGradient.SetColor(0, Colors.White);
		_pulseGradient.SetOffset(0, _pulseOffsets[0]);
		_pulseGradient.SetColor(1, Colors.White);
		_pulseGradient.SetOffset(1, _pulseOffsets[1]);
		// points 3 to 5
		_pulseGradient.AddPoint(_pulseOffsets[2], Colors.White);
		_pulseGradient.AddPoint(_pulseOffsets[3], Colors.White);
		_pulseGradient.AddPoint(_pulseOffsets[4], Colors.White);

		// assign gradient to texture
		GradientTexture1D gradientTexture = new GradientTexture1D();
		gradientTexture.UseHdr = true;
		gradientTexture.Gradient = _pulseGradient;

		_line3D = GetNode<Node3D>("./Line3D");
		_selectedLine3D = GetNode<Node3D>("./SelectedLine3D");
		_selectedLine3D.Visible = false;
		_material = _line3D.Get("custom_material").As<StandardMaterial3D>();
		_material.AlbedoTextureForceSrgb = true;
		_material.AlbedoTexture = gradientTexture;
		_material.Uv1Offset = new Vector3(1f, 0, 0);

		_valueLabel = GetNode<Label3D>("./ValueLabel");
		_valueLabel.Visible = false;
	}

	/// <summary>
	/// Initialize a single dendrite.
	/// </summary>
	/// <param name="layerIndex">The layer the dendrite ENDS in.</param>
	/// <param name="endNeuronIndex">The index of the neuron (in its layer) that the dendrite ends in (whose weight it corresponds with).</param>
	/// <param name="startNeuronIndex">The index of the neuron that this dendrite connects with the end neuron.</param>
	public void Initialize(int layerIndex, int endNeuronIndex, int startNeuronIndex)
	{
		_layerIndex = layerIndex;
		_endNeuronIndex = endNeuronIndex;
		_startNeuronIndex = startNeuronIndex;

		NetworkManager networkManager = GetNode<VisualizationManager>("/root/VisualizationManager").NetworkManager;

		AbstractNeuron endNeuron = networkManager.GetNeuron(layerIndex, endNeuronIndex);
		AbstractNeuron startNeuron = networkManager.GetNeuron(layerIndex - 1, startNeuronIndex);

		// 
		_strengthFalloffSpeed = endNeuron.StrengthFalloffSpeed;

		_startPos = startNeuron.Position;
		_endPos = endNeuron.Position;

		_line3D.Call("set_points", new Vector3[] { _startPos, _endPos });

		Vector3 midPoint = (_startPos - _endPos) / 2.0f;
		midPoint.Y += _valueLabel.Position.Y;
		_valueLabel.Position = midPoint;

		SetupCollisionShape();
	}

	private void SetupCollisionShape()
	{
		MeshInstance3D box = new MeshInstance3D();
		box.Visible = false;
		box.Mesh = new BoxMesh();

		Vector3 forward = (_startPos - _endPos).Normalized();
		Vector3 up_temp = new Vector3(0, 1, 0);
		Vector3 right = forward.Cross(up_temp).Normalized();
		Vector3 up = right.Cross(forward).Normalized();

		box.Basis = new Basis(right, up, forward);

		float distance = _endPos.DistanceTo(_startPos);
		(box.Mesh as BoxMesh).Size = new Vector3(_colliderThickness, _colliderThickness, distance);
		box.Translate(new Vector3(0, 0, (distance / 2)));

		Area3D area3d = GetNode<Area3D>("./Area3D");
		CollisionShape3D collisionShape3d = area3d.GetChild(0) as CollisionShape3D;
		area3d.AddChild(box);
		collisionShape3d.MakeConvexFromSiblings();

		_collisionShape3d = collisionShape3d;
	}

	public void SetWeight(byte weight)
	{
		_weight = weight;
		
		if (_valueLabel.Visible)
		{
			UpdateValueLabel(weight);
		}

		float t = (float)weight / byte.MaxValue;

		Color color = _baseGradient.Sample(t);
		color.A = Mathf.Min(0.5f, t);
		_material.AlbedoColor = color;
	}

	private void UpdateValueLabel(byte value)
	{
		_valueLabel.Text = (value / 255.0f).ToString(_valueLabelFormat);
	}

	public void UpdateValueLabel()
	{
		UpdateValueLabel(_weight);
	}

	public void DoPulseForward(byte value)
	{
		DoPulse(value, true);
	}

	public void DoPulseBackward(byte value)
	{
		int diff = _weight - value;
		float diff_normalized = (diff + 1.0f) / 2.0f;	// where a negative diff equals < 0.5

		Color color = BackpropGradient.Sample(diff_normalized);

		LightUp(value, color);

		if (_valueLabel.Visible)
		{
			DoValueLabelTween(diff, color);
		}
	}

	private void DoValueLabelTween(int diff, Color color)
	{
		TweenConfig valueConfig = new TweenConfig();
		string prevText = _valueLabel.Text;
		valueConfig.TweenKey = "valuelabel_tween";
		valueConfig.Callback = Callable.From(() => _valueLabel.Text = prevText);
		valueConfig.Duration = _backpropLightUpDuration;
		string diffStr = (diff / 255.0f).ToString(_valueLabelDiffFormat);
		valueConfig.Method = (_) => _valueLabel.Text = diffStr;
		_valueLabel.Text = diffStr;

		TweenConfig valueColorConfig = new TweenConfig();
		valueColorConfig.TweenKey = "valuelabel_color_tween";
		valueColorConfig.Callback = Callable.From(() => _valueLabel.Modulate = _valueLabelBaseColor);
		valueColorConfig.Duration = _backpropLightUpDuration;
		valueColorConfig.Object = _valueLabel;
		valueColorConfig.Property = "modulate";
		valueColorConfig.FinalVal = _valueLabelBaseColor;
		_valueLabel.Modulate = color;

		_tweenManager.ResetTweens(valueConfig, valueColorConfig);
	}

	private void DoPulse(byte value, bool goingForward)
	{
		float t = (float)value / byte.MaxValue;

		float duration = _startPos.DistanceTo(_endPos) / _pulseSpeed;

		List<TweenConfig> configs = new List<TweenConfig>();

		float t_weight = (float)_weight / byte.MaxValue;
		TweenConfig alphaConfig = new TweenConfig();
		alphaConfig.Method = (val) => UpdateAlpha(val.As<float>());
		alphaConfig.InitialVal = 0f;
		alphaConfig.FinalVal = t_weight;
		alphaConfig.Duration = duration;
		alphaConfig.TweenKey = "line_alpha";

		TweenConfig pulseConfig = new TweenConfig();
		pulseConfig.Method = (val) => UpdatePulse(val.As<float>());
		pulseConfig.Callback = Callable.From(() => _pulseGradient.SetColor(2, Colors.White));
		pulseConfig.InitialVal = goingForward ? 0.5f : -0.5f;
		pulseConfig.FinalVal = goingForward ? -0.5 : 0.5f;
		pulseConfig.Duration = duration;
		pulseConfig.TweenKey = "line_gradient";

		configs.Add(alphaConfig);
		configs.Add(pulseConfig);

		_tweenManager.ResetTweens(configs.ToArray());
	}

	private void LightUp(byte value, Color color)
	{
		float t = (float)value / byte.MaxValue;

		float duration = _startPos.DistanceTo(_endPos) / _pulseSpeed;

		TweenConfig colorConfig = new TweenConfig();
		colorConfig.Method = (val) => UpdateColor(val.AsColor());
		colorConfig.Duration = duration;
		colorConfig.InitialVal = color;
		colorConfig.FinalVal = _material.AlbedoColor;
		colorConfig.TweenKey = "line_color";

		_tweenManager.ResetTweens(colorConfig);
	}

	private void UpdateAlpha(float alpha)
	{
		_material.AlbedoColor = new Color(_material.AlbedoColor, alpha);
	}

	private void UpdateColor(Color color)
	{
		_material.AlbedoColor = color;
	}

	private void UpdatePulse(float offset)
	{
		Color color = _material.AlbedoColor;
		// set color of the "peak" of the pulse
		(_material.AlbedoTexture as GradientTexture1D).Gradient.SetColor(2, Colors.White * 1.5f);

		// move texture
		_material.Uv1Offset = new Vector3(offset, 0, 0);
	}

	public void DeleteAfterTweens()
	{
		_tweenManager.DeleteAfterTweens(this);
	}

	public override void _ExitTree()
	{
		_tweenManager.KillAll();
	}

	public void SetSelected(bool val, float selectionStrength, int selectionDirection)
	{
		_selectedLine3D.Visible = val;
		_valueLabel.Visible = val;
		_selectedLine3D.Call("get_material").As<ShaderMaterial>()
			.SetShaderParameter("base_color", SelectionGradient.Sample(selectionStrength));

		SetNeuronsSelectedInStrand(val, selectionStrength, selectionDirection);
	}

	public void SetNeuronsSelectedInStrand(bool val, float selectionStrength, int selectionDirection)
	{
		if (_networkManager == null)
		{
			_networkManager = GetNode<VisualizationManager>("/root/VisualizationManager").NetworkManager;
		}

		selectionStrength = Mathf.Clamp(selectionStrength - _strengthFalloffSpeed, 0.0f, 1.0f);

		if (selectionDirection <= 0)
		{
			int nextDirection = selectionDirection;
			if (nextDirection == 0)
			{
				nextDirection--;
			}

			// find previous neurons
			if (LayerIndex > 0)
			{
				AbstractNeuron prevNeuron = _networkManager.GetNeuron<AbstractNeuron>(LayerIndex - 1, StartNeuronIndex);
				prevNeuron.SetSelected(val, selectionStrength, nextDirection);
			}
		}
		if (selectionDirection >= 0)
		{
			selectionDirection++;
			
			// find next neurons
			if (LayerIndex < _networkManager.NumLayersTotal - 1)
			{
				AbstractNeuron nextNeuron = _networkManager.GetNeuron<AbstractNeuron>(LayerIndex + 1, ConnectedNeuronIndex);
				nextNeuron.SetSelected(val, selectionStrength, selectionDirection);
			}
		}
	}

	public void SetCollidable(bool val)
	{
		_collisionShape3d.Disabled = !val;
	}

	// -------- used by GD scripts ---------
	public int GetLayerIndex()
	{
		return _layerIndex;
	}

	public int GetEndNeuronIndex()
	{
		return _endNeuronIndex;
	}
}
