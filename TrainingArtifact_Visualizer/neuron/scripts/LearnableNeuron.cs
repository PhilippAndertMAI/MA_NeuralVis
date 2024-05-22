using Godot;
using Kaitai;
using NumSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class LearnableNeuron : AbstractNeuron
{
	private TrainingArtifactWrapper _tartWrapper;
	private NetworkManager _networkManager;

	private int _layerIndex;

	private byte _bias;
	private List<byte> _weights;

	private float _avgActivation;
	private float _latestRawInput;
	private float _latestWeightedInput;
	private byte _latestActivation;

	public int LayerIndex { get { return _layerIndex; } }

	public byte Bias { get { return _bias; } }
	public List<byte> Weights { get { return _weights; } }
	
	public float AvgActivation { get { return _avgActivation; } }
	public float LatestRawInput { get { return _latestRawInput; } }
	public float LatestWeightedInput {  get { return _latestWeightedInput; } }
	public byte LatestActivation { get { return _latestActivation; } }

	[Export]
	public Gradient BackpropGradient;

	public Dictionary<int, Dendrite> ActiveDendrites { get; set; }

	public override void _Ready()
	{
		base._Ready();

		_tartWrapper = GetNode<TrainingArtifactWrapper>("/root/VisualizationManager/TrainingArtifactWrapper");
		_networkManager = GetNode<NetworkManager>("/root/VisualizationManager/NetworkManager");

		_weights = new List<byte>();
	}

	public override void Initialize(byte bias, byte[] weights, int layerIndex, int neuronIndex)
	{
		_layerIndex = layerIndex;
		_neuronIndex = neuronIndex;

		ActiveDendrites = new Dictionary<int, Dendrite>();

		SetIntensity(bias);
		SetWeights(weights);
	}

	public override void SetIntensity(byte bias, bool showUpdate = false)
	{
		byte oldBias = _bias;
		byte newBias = bias;

		if (_valueLabel.Visible)
		{
			UpdateValueLabel(bias);
		}

		_intensity = bias;
		_bias = bias;
		ModulateBaseColor(bias);

		if (showUpdate)
		{
			int biasDiff = newBias - oldBias;
			// map diff to range 0..255
			biasDiff = (int)((float)(biasDiff + 255) / (255 + 255) * 255);

			LightUpBackward((byte)biasDiff);
		}
	}

	protected override void UpdateValueLabel()
	{
		UpdateValueLabel(_bias);
	}

	public void SetWeights(byte[] weights, bool showUpdate = false)
	{
		_weights.Clear();
		for (int i = 0; i < weights.Length; i++)
		{
			_weights.Add(weights[i]);

			Dendrite dendrite;
			if (ActiveDendrites.TryGetValue(i, out dendrite))
			{
				byte oldWeight = dendrite.Weight;
				byte newWeight = weights[i];

				dendrite.SetWeight(weights[i]);

				if (showUpdate)
				{
					int weightDiff = newWeight - oldWeight;
					// map diff to range 0..255
					weightDiff = (int)((float)(weightDiff + 255) / (255 + 255) * 255);

					dendrite.DoPulseBackward((byte)weightDiff);
				}

				CheckHideDendrites();
			}
		}
	}

	public void SetAvgActivation(float avgActivation)
	{
		_avgActivation = avgActivation;
	}

	public void SetLatestRawInput(float latestRawInput)
	{
		_latestRawInput = latestRawInput;
	}

	public void SetLatestWeightedInput(float latestWeightedInput)
	{
		_latestWeightedInput = latestWeightedInput;
	}

	public void SetLatestActivation(byte latestActivation)
	{
		_latestActivation = latestActivation;
	}

	/// <summary>
	/// To be used during forward propagation when a predicted neuron should light up
	/// </summary>
	/// <param name="value">Value of activation</param>
	/// <param name="predictedColor">Color of the prediction representing a correct or incorrect prediction</param>
	public void LightUpForward(byte value, Color predictedColor)
	{
		LightUp(value, predictedColor);
		foreach (var dendrite in ActiveDendrites.Values)
		{
			dendrite.DoPulseForward(value);
		}
	}

	/// <summary>
	/// Regular light up method to be used when activations pass through the network
	/// </summary>
	/// <param name="value">Value of activation</param>
	public void LightUpForward(byte value)
	{
		LightUp(value);
		foreach (var dendrite in ActiveDendrites.Values)
		{
			dendrite.DoPulseForward(value);
		}
	}

	public void LightUpBackward(byte value)
	{
		int diff = _bias - value;
		float diff_normalized = (diff + 1.0f) / 2.0f;   // where a negative diff equals < 0.5

		Color color = BackpropGradient.Sample(diff_normalized);

		LightUp(value, color);

		if (_valueLabel.Visible)
		{
			DoValueLabelTween(diff, color);
		}
	}

	protected void DoValueLabelTween(int diff, Color color)
	{
		TweenConfig valueConfig = new TweenConfig();
		string prevText = _valueLabel.Text;
		valueConfig.TweenKey = "valuelabel_tween";
		valueConfig.Callback = Callable.From(() => _valueLabel.Text = prevText);
		valueConfig.Duration = _lightUpDuration;
		string diffStr = (diff / 255.0f).ToString(_valueLabelDiffFormat);
		valueConfig.Method = (_) => _valueLabel.Text = diffStr;
		_valueLabel.Text = diffStr;

		TweenConfig valueColorConfig = new TweenConfig();
		valueColorConfig.TweenKey = "valuelabel_color_tween";
		valueColorConfig.Callback = Callable.From(() => _valueLabel.Modulate = _valueLabelBaseColor);
		valueColorConfig.Duration = _lightUpDuration;
		valueColorConfig.Object = _valueLabel;
		valueColorConfig.Property = "modulate";
		valueColorConfig.FinalVal = _valueLabelBaseColor;
		_valueLabel.Modulate = color;

		_tweenManager.ResetTweens(valueConfig, valueColorConfig);
	}

	public override void SetSelected(bool val, float selectionStrength, int selectionDirection)
	{
		base.SetSelected(val, selectionStrength, selectionDirection);

		foreach (var dendrite in ActiveDendrites.Values)
		{
			dendrite.SetSelected(val, selectionStrength, selectionDirection);
		}
	}

	// ---------- called by GD scripts ----------

	public int GetLayerIndex()
	{
		return _layerIndex;
	}

	public int GetNeuronIndex()
	{
		return _neuronIndex;
	}

	public float GetAvgActivation()
	{
		return AvgActivation;
	}

	public float GetAvgActivation(int trainingStepIndex)
	{
		return _tartWrapper.TrainingStep(trainingStepIndex).LayerStates[LayerIndex - 1].AvgNeuronActivations[NeuronIndex];
	}

	public byte GetBias()
	{
		return Bias;
	}

	public byte GetBias(int trainingStepIndex)
	{
		return _tartWrapper.TrainingStep(trainingStepIndex).LayerStates[LayerIndex - 1].Biases[NeuronIndex];
	}

	public float GetAverageWeightAsFloat()
	{
		float weightSum = 0.0f;
		foreach (byte weight in Weights)
		{
			weightSum += (float)weight / 255;
		}
		return weightSum / Weights.Count;
	}

	public float GetAverageWeightAsFloat(int trainingStepIndex)
	{
		TrainingArtifact.LayerState weightsLayerState = _tartWrapper.TrainingStep(trainingStepIndex).LayerStates[LayerIndex - 1];
		NDArray weights = np.array(weightsLayerState.Weights);
		weights = weights.reshape(_networkManager.NeuronsPerLayer[LayerIndex - 1], _networkManager.NeuronsPerLayer[LayerIndex]);

		float weightSum = 0.0f;
		foreach (byte weight in weights)
		{
			weightSum += (float)weight / 255;
		}
		return weightSum / Weights.Count;
	}

	public float GetLatestInput()
	{
		return LatestRawInput;
	}

	public float GetLatestWeightedInput()
	{
		return LatestWeightedInput;
	}

	public byte GetLatestActivation()
	{
		return LatestActivation;
	}
}
