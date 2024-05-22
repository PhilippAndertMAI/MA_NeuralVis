using Godot;
using Godot.NativeInterop;
using System;

public partial class ActivityMap : Node3D
{
	private TrainingStepper _trainingStepper;
	private NetworkManager _networkManager;
	private ActivityMapManager _activityMapManager;

	private FogVolume _fog;
	private ShaderMaterial _material;
	private ImageTexture3D _texture;

	private int _layerIndex;

	private float _minPosX;
	private float _minPosY;

	private int _width = 64;
	private int _height = 64;
	private int _dimPerNeuron = 16;

	private float _pixelsPerUnit;

	private float _baseRadius = 1.0f;

	public override void _Ready()
	{
		_trainingStepper = GetNode<TrainingStepper>("/root/VisualizationManager/TrainingStepper");
		_networkManager = GetNode<NetworkManager>("/root/VisualizationManager/NetworkManager");
		_activityMapManager = GetParent<ActivityMapManager>();

		_fog = GetNode<FogVolume>("./FogVolume");
		_material = _fog.Material as ShaderMaterial;

		_trainingStepper.Connect("stepped", new Callable(this, nameof(HandleStepped)));
		_trainingStepper.Connect("skipped", new Callable(this, nameof(HandleSkipped)));
	}
	private void HandleStepped(int _)
	{
		UpdateShader();
	}

	private void HandleSkipped(int _, int __, int ___)
	{
		UpdateShader();
	}

	public void Initialize(int layerIndex, float minPosX, float minPosY)
	{
		_layerIndex = layerIndex;

		_minPosX = minPosX;
		_minPosY = minPosY;

		UpdateShader();
	}

	public void UpdateShader(params Variant[] _)
	{
		int nNeurons = _networkManager.NeuronsPerLayer[_layerIndex];
		
		Vector3[] positions = new Vector3[nNeurons];
		float[] biases = new float[nNeurons];
		float[] avgWeights = new float[nNeurons];
		float[] avgActivations = new float[nNeurons];

		for (int neuronIndex = 0; neuronIndex < nNeurons; neuronIndex++)
		{
			LearnableNeuron neuron = _networkManager.GetNeuron<LearnableNeuron>(_layerIndex, neuronIndex);
			positions[neuronIndex] = neuron.Position;
			biases[neuronIndex] = (float)neuron.Bias / 255;
			avgWeights[neuronIndex] = neuron.GetAverageWeightAsFloat();
			avgActivations[neuronIndex] = neuron.GetAvgActivation();
		}

		_material.SetShaderParameter("n_neurons", nNeurons);
		_material.SetShaderParameter("positions", positions);
		_material.SetShaderParameter("fog_width", _fog.Size.X);
		_material.SetShaderParameter("fog_height", _fog.Size.Y);
		_material.SetShaderParameter("min_pos_x", _minPosX);
		_material.SetShaderParameter("min_pos_y", _minPosY);
		_material.SetShaderParameter("thickness", _activityMapManager.Thickness);
		_material.SetShaderParameter("biases", biases);
		_material.SetShaderParameter("avg_weights", avgWeights);
		_material.SetShaderParameter("avg_activations", avgActivations);

		_material.SetShaderParameter("density", _activityMapManager.Density);
		_material.SetShaderParameter("strength", _activityMapManager.Strength);
		_material.SetShaderParameter("size", _activityMapManager.Size);
	}

	private void ApplyTexture(ImageTexture3D texture)
	{
		_material.SetShaderParameter("myTexture", texture);
	}
}
