using Godot;
using NumSharp;
using System;
using System.Collections.Generic;
using Kaitai;
using System.Linq;

public partial class TrainingStepper : Node
{
	// other manager nodes
	private TrainingArtifactWrapper _tartWrapper;
	private NetworkManager _networkManager;
	private DendriteManager _dendriteManager;

	// stepping logic
	private int _currentTrainingStep = 0;
	private int _currentSubStep = -2;
	private int _numSubSteps;
	private bool _areInputNeuronsSet = false;

	// propagation data & logic
	private List<TrainingArtifact.TrainingStep> _trainingSteps;
	private TrainingData _trainingData;
	private TrainingArtifact.LayerState[] _initialLayerStates;
	private NDArray[] _cachedForwardOutput;


	public override void _Ready()
	{
		AddCustomSignals();
		GetManagerNodes();

		// stepping logic
		int numlayers = _networkManager.NumLayersTotal;
		_numSubSteps = (numlayers - 1) * 2;

		// propagation data & logic
		_trainingData = _tartWrapper.TrainingData;
		_trainingSteps = _tartWrapper.TrainingArtifact.TrainingSteps;
		_initialLayerStates = _tartWrapper.TrainingArtifact.InitialStates.ToArray();
		_cachedForwardOutput = new NDArray[numlayers - 1];
	}

	public void ShowCurrentStep()
	{
		if (_currentSubStep < 0)
		{
			return;
		}

		// check if in backward or forward prop range
		if (_currentSubStep > (_numSubSteps / 2) - 1)
		{
			int backwardPropIndex = _currentSubStep - _numSubSteps / 2;
			PropagateBackward(_currentTrainingStep, backwardPropIndex);
		}
		else
		{
			int forwardPropIndex = _currentSubStep;
			PropagateForward(_currentTrainingStep, forwardPropIndex);
		}
	}

	/// <summary>
	/// See Decrement()
	/// </summary>
	/// <returns></returns>
	public bool Increment()
	{
		// TODO: guard against training step overflow
		if (_currentTrainingStep == GetNumStoredTrainingSteps() - 1 &&
			_currentSubStep == (_numSubSteps - 1))
		{
			return false;
		}

		// initial setting of input neurons
		if (_currentSubStep == -2)
		{
			EmitSkippedSignal(_currentTrainingStep);
			SetInputNeurons(_currentTrainingStep);
		}

		_currentSubStep++;
		if (_currentSubStep > (_numSubSteps - 1))
		{
			_currentTrainingStep++;
			EmitSkippedSignal(_currentTrainingStep);
			SetInputNeurons(_currentTrainingStep);

			_currentSubStep = -1;
		}

		EmitSignal("stepped", _currentSubStep);

		return true;
	}

	/// <summary>
	/// Decrement substep and training step index.
	/// </summary>
	/// <returns>True or false, depending on whether or not the guard statement preventing an underflow is hit.</returns>
	public bool Decrement()
	{
		// guard against training step underflow
		if (_currentTrainingStep == 0 && _currentSubStep <= 0)
		{
			return false;
		}

		_currentSubStep--;
		if (_currentSubStep < -1)
		{
			_currentTrainingStep--;
			EmitSkippedSignal(_currentTrainingStep);

			SetInputNeurons(_currentTrainingStep);
			ReDoPropagation(_currentTrainingStep);

			_currentSubStep = _numSubSteps - 1;
		}

		EmitSignal("stepped", _currentSubStep);

		return true;
	}

	/// <summary>
	/// Skip through x number of training steps in either direction. Resets substeps.
	/// </summary>
	/// <param name="magnitude">Indicates both the amount and direction of training steps to skip.</param>
	public void FastForwardTrainingSteps(int magnitude)
	{
		_currentSubStep = 0;
		_currentTrainingStep += magnitude;
		// guard against underflow
		if (_currentTrainingStep < 0)
		{
			_currentTrainingStep = 0;
		}
		// guard against overflow
		if (_currentTrainingStep > GetNumStoredTrainingSteps() - 1)
		{
			_currentTrainingStep = GetNumStoredTrainingSteps() - 1;
		}

		// re-do previous forward steps
		if (_currentTrainingStep > 0)
		{
			int previousTrainingStep = _currentTrainingStep - 1;
			ReDoPropagation(previousTrainingStep);
		}
		else
		{
			ReDoPropagation(0);
		}
		
		SetInputNeurons(_currentTrainingStep);

		EmitSignal("stepped", _currentSubStep);
		EmitSkippedSignal(_currentTrainingStep);
		
	}

	public void EmitSkippedSignal(int currentTrainingStepIndex) {
		TrainingStepWrapper trainingStep = _tartWrapper.TrainingStep(currentTrainingStepIndex);
		int nSamples = _tartWrapper.TrainingArtifact.Header.NumSamples;
		
		int samplesPassed = trainingStep.EpochIndex * nSamples;

		int storedTrainingStepIndex = samplesPassed + (currentTrainingStepIndex % nSamples);
		
		EmitSignal("skipped", currentTrainingStepIndex, storedTrainingStepIndex, trainingStep.EpochIndex);
		EmitSignal("predicted", -1, false);
	}

	public void ReDoPropagation(int trainingStepIndex)
	{	
		for (int i = 0; i < _numSubSteps / 2; i++)
		{
			PropagateForward(trainingStepIndex, i, false);
		}
		for (int i = 0; i < _numSubSteps / 2; i++)
		{
			PropagateBackward(trainingStepIndex, i, false);
		}
	}

	public void AddCustomSignals()
	{
		AddUserSignal("stepped", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "step" },
				{ "type", (int)Variant.Type.Int }
			}
		});

		AddUserSignal("skipped", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "currentTrainingStepIndex" },
				{ "type", (int)Variant.Type.Int }
			},
			new Godot.Collections.Dictionary()
			{
				{ "name", "storedTrainingStepIndex" },
				{ "type", (int)Variant.Type.Int }
			},
			new Godot.Collections.Dictionary()
			{
				{ "name", "trainingStepEpochIndex" },
				{ "type", (int)Variant.Type.Int }
			}
		});

		AddUserSignal("predicted", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "predicted_index" },
				{ "type", (int)Variant.Type.Int }
			},
			new Godot.Collections.Dictionary()
			{
				{ "name", "is_correct" },
				{ "type", (int)Variant.Type.Int }
			},
		});
	}

	private void GetManagerNodes()
	{
		_tartWrapper = GetNode<TrainingArtifactWrapper>("/root/VisualizationManager/TrainingArtifactWrapper");
		_networkManager = GetNode<NetworkManager>("/root/VisualizationManager/NetworkManager");
		_dendriteManager = GetNode<DendriteManager>("/root/VisualizationManager/DendriteManager");
	}

	private void SetInputNeurons(int trainingStepIndex)
	{
		GD.Print("setting neurons\n");

		var trainingStep = _tartWrapper.TrainingStep(trainingStepIndex);
		int inputIndex = trainingStep.InputIndex;
		NDArray inputData = _trainingData.Data[inputIndex];

		for (int i = 0; i < _networkManager.NeuronsPerLayer[0]; i++)
		{
			// TODO: write proper wrapper around data to avoid having to index 0
			_networkManager.GetNeuron(0, i).SetIntensity(inputData[0][i], showUpdate: false);
		}
		_areInputNeuronsSet = true;
	}

	static NDArray BytesToNormalized(NDArray x)
	{
		var result = x.astype(np.float64);
		result = (x - np.min(x)) / (np.max(x) - np.min(x));
		result -= 0.5;
		return result;
	}

	private void PropagateForward(int trainingStepIndex, int forwardPropIndex, bool showUpdate = true)
	{
		GD.Print("training step ", trainingStepIndex);
		GD.Print("forward index ", forwardPropIndex);
		GD.Print("\n");

		var trainingStep = _tartWrapper.TrainingStep(trainingStepIndex);

		// get input
		int inputIndex = trainingStep.InputIndex;
		GD.Print(inputIndex);
		NDArray inputData = _trainingData.Data[inputIndex];
		NDArray data;
		if (forwardPropIndex == 0)
		{
			data = inputData;
		}
		else
		{
			data = _cachedForwardOutput[forwardPropIndex - 1];
		}

		// do forward prop
		// get values for calculation

		NDArray biases = trainingStep.GetBiases(forwardPropIndex);
		int curNeuronCount = _networkManager.NeuronsPerLayer[forwardPropIndex + 1];
		int prevNeuronCount = _networkManager.NeuronsPerLayer[forwardPropIndex];
		NDArray weights = trainingStep.GetWeights(forwardPropIndex, prevNeuronCount, curNeuronCount);

		//data = MapRangeToByte(data);

		// do forward calc (with byte values, as this is only for visuals and the true training is stored in the artifact anyways)

		NDArray weights_float = BytesToNormalized(weights);
		NDArray biases_float = BytesToNormalized(biases);
		NDArray data_float = BytesToNormalized(data);
		NDArray z = np.dot(data_float, weights_float) + biases_float;
		z = BytesToNormalized(z);
		var activationType = trainingStep.GetActivationFunctions(forwardPropIndex);
		NDArray output = ActivationFunctionUtil.Activate(z, activationType);
		output *= 255;

		//output = output.astype(np.@byte);
		//NDArray output = ActivationFunctions.Activate(z, activationFunction);

		_cachedForwardOutput[forwardPropIndex] = output;

		

		for (int i = 0; i < _networkManager.NeuronsPerLayer[forwardPropIndex + 1]; i++)
		{
			LearnableNeuron neuron = _networkManager.GetNeuron<LearnableNeuron>(forwardPropIndex + 1, i);

			// set avg activations for neuron
			TrainingArtifact.LayerState layerState = _trainingSteps[trainingStepIndex].LayerStates[neuron.LayerIndex - 1];
			float[] avgActivations = layerState.AvgNeuronActivations.ToArray();
			neuron.SetAvgActivation(avgActivations[neuron.NeuronIndex]);
			neuron.SetLatestRawInput((float)data_float[0][neuron.NeuronIndex].GetValue<double>());
			neuron.SetLatestWeightedInput((float)z[0][neuron.NeuronIndex].GetValue<double>());
			neuron.SetLatestActivation(output[0][neuron.NeuronIndex]);

			if (!showUpdate)
			{
				continue;
			}

			if (forwardPropIndex == (_numSubSteps / 2) - 1)
			{
				if (i == _trainingSteps[trainingStepIndex].PredictedIndex)
				{
					Color predictedColor;
					bool isCorrect;
					if (np.argmax(_trainingData.Target[_trainingSteps[trainingStepIndex].InputIndex]) == _trainingSteps[trainingStepIndex].PredictedIndex)
					{
						predictedColor = new Color(0, 1, 0);
						isCorrect = true;
					}
					else
					{
						predictedColor = new Color(1, 0, 0);
						isCorrect = false;
					}
					neuron.LightUpForward(output[0][i], predictedColor);
					(neuron as OutputNeuron).LightUpLabel(predictedColor);

					EmitSignal("predicted", _trainingSteps[trainingStepIndex].PredictedIndex, isCorrect);
				}
				else
				{
					neuron.LightUpForward(output[0][i]);
				}
			}
			else
			{
				neuron.LightUpForward(output[0][i]);
			}
		}
	}

	private void PropagateBackward(int trainingStepIndex, int backwardPropIndex, bool showUpdate = true)
	{
		GD.Print("training step ", trainingStepIndex);
		GD.Print("backward index ", backwardPropIndex);
		GD.Print("\n");

		// -------------------- Fetch and display backwards propagation data --------------------

		// get index at which layer the backpropagation is happening right now
		int layerIndex = (_numSubSteps / 2) - 1 - backwardPropIndex;

		TrainingArtifact.LayerState[] prevLayerStates;

		int prevTrainingStepIndex = trainingStepIndex - 1;

		if (prevTrainingStepIndex > 0)
		{
			prevLayerStates = _trainingSteps[prevTrainingStepIndex].LayerStates.ToArray();
		}
		else
		{
			prevLayerStates = _initialLayerStates;		
		}


		// fetch data of the layer
		TrainingArtifact.LayerState prevLayerState = prevLayerStates[layerIndex];

		UpdateLayerNeurons(layerIndex, prevLayerState, showUpdate: false);

		// get layer states of the next training step
		TrainingArtifact.LayerState[] layerStates = _trainingSteps[trainingStepIndex].LayerStates.ToArray();
		// fetch data of the layer
		TrainingArtifact.LayerState layerState = layerStates[layerIndex];

		UpdateLayerNeurons(layerIndex, layerState, showUpdate);

		// if backwards prop. is at its last step of this training iteration
		if (backwardPropIndex == (_numSubSteps / 2) - 1)
		{
			_dendriteManager.UpdateDendriteStrands();
		}
	}

	private void UpdateLayerNeurons(int layerIndex, TrainingArtifact.LayerState layerState, bool showUpdate = true)
	{
		// retrieve weights of all neurons in this layer
		NDArray weights = np.array(layerState.Weights);
		int curNeuronCount = _networkManager.NeuronsPerLayer[layerIndex + 1];
		int prevNeuronCount = _networkManager.NeuronsPerLayer[layerIndex];
		weights = weights.reshape(curNeuronCount, prevNeuronCount);

		// iterate over neurons in the layer where the data needs to be updated (the layer after that)
		for (int i = 0; i < _networkManager.NeuronsPerLayer[layerIndex + 1]; i++)
		{
			// retrieve bias and weight of this singular neuron
			byte bias = layerState.Biases[i];
			byte[] neuronWeights = weights.GetData(i).ToArray<byte>();

			LearnableNeuron neuron = (LearnableNeuron)_networkManager.GetNeuron(layerIndex + 1, i);

			neuron.SetIntensity(bias, showUpdate);
			neuron.SetWeights(neuronWeights, showUpdate);
		}
	}

	// ---------- called (not exclusively) by GD scripts ----------

	public int GetCurrentTrainingStep()
	{
		return _currentTrainingStep;
	}

	public int GetNumSubSteps()
	{
		return _numSubSteps;
	}

	public int GetNumStoredTrainingSteps()
	{
		return _trainingSteps.Count;
	}

	public int GetNumActualTotalTrainingSteps()
	{
		return _tartWrapper.TrainingArtifact.Header.NumEpochs * _tartWrapper.TrainingArtifact.Header.NumSamples;
	}

	public int[] GetEpochIndices()
	{
		List<int> epochIndices = new List<int>();
		foreach (ushort index in _tartWrapper.TrainingArtifact.Header.EpochIndices)
		{
			epochIndices.Add(index);
		}
		return epochIndices.ToArray();
	}

	public int GetNumSamples()
	{
		return _tartWrapper.TrainingArtifact.Header.NumSamples;
	}

	public int GetNumEpochs()
	{
		return _tartWrapper.TrainingArtifact.Header.NumEpochs;
	}

	public float GetCurrentAvgLoss()
	{
		return GetAvgLoss(_currentTrainingStep);
	}

	public float GetAvgLoss(int trainingStepIndex)
	{
		return _tartWrapper.TrainingStep(trainingStepIndex).AvgLoss;
	}

	public int GetCurrentNumCorrectPredictions()
	{
		return GetNumCorrectPredictions(_currentTrainingStep);
	}

	public int GetNumCorrectPredictions(int trainingStepIndex)
	{
		return _tartWrapper.TrainingArtifact.Metrics.NumCorrectPredictions[trainingStepIndex];
	}
}
