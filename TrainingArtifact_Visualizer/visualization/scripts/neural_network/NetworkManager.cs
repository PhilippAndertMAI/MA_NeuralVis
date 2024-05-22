using Godot;
using Kaitai;
using System;
using System.Collections.Generic;
using System.Linq;
using NumSharp;

public partial class NetworkManager : Node3D
{
	TrainingArtifactWrapper _tartWrapperNode;

	private DendriteManager _dendriteManager;

	private int _numLayersTotal;
	private int[] _neuronsPerLayer;

	private List<AbstractNeuron>[] _neurons;
	private InputNeuron _dummyNeuron;		// used by ActivityMapManager to select an invisible neuron (and mute rest)

	// TODO: get spatial parameters from some config (autoload/singleton?)
	private int _neuronOffset = 1;
	private int _layerOffset = 10;

	public int NeuronOffset { get { return _neuronOffset; } }
	public int LayerOffset { get { return _layerOffset; } }

	public int NumLayersTotal {  get { return _numLayersTotal; } }
	public int[] NeuronsPerLayer { get {  return _neuronsPerLayer; } }
	
	public AbstractNeuron GetNeuron(int layerIndex, int neuronIndex)
	{
		return GetNeuron<AbstractNeuron>(layerIndex, neuronIndex);
	}

	public T GetNeuron<T>(int layerIndex, int neuronIndex)
	{
		AbstractNeuron neuron = _neurons[layerIndex][neuronIndex];
		try
		{
			return (T)(object)neuron;
		}
		catch (Exception)
		{
			throw new InvalidCastException();
		}
	}

	public override void _Ready()
	{
		_tartWrapperNode = GetNode<TrainingArtifactWrapper>("/root/VisualizationManager/TrainingArtifactWrapper");
		_dendriteManager = GetNode<DendriteManager>("/root/VisualizationManager/DendriteManager");

		TrainingArtifact artifact = _tartWrapperNode.TrainingArtifact;
		SetMetadata(artifact);

		InstantiateNeurons();
		InitializeNeuronData();

		_dendriteManager.Initialize(this, _tartWrapperNode);

		ArrangeNeurons();
		ArrangeDendrites();
	}

	private void InstantiateNeurons()
	{
		_neurons = Enumerable.Range(0, _numLayersTotal)
					.Select(_ => new List<AbstractNeuron>())
					.ToArray();

		PackedScene inputNeuronScene = GD.Load<PackedScene>("res://neuron/InputNeuron.tscn");
		PackedScene learnableNeuronScene = GD.Load<PackedScene>("res://neuron/LearnableNeuron.tscn");
		PackedScene outputNeuronScene = GD.Load<PackedScene>("res://neuron/OutputNeuron.tscn");

		for (int layerIndex = 0; layerIndex < _numLayersTotal; layerIndex++)
		{
			for (int neuronIndex = 0; neuronIndex < _neuronsPerLayer[layerIndex]; neuronIndex++)
			{
				AbstractNeuron neuron;
				// instantiate input neurons
				if (layerIndex == 0)
				{
					
					neuron = inputNeuronScene.Instantiate<InputNeuron>();
				}
				// instantiate output neurons
				else if (layerIndex == _numLayersTotal - 1)
				{
					neuron = outputNeuronScene.Instantiate<OutputNeuron>();
				}
				else
				{
					neuron = learnableNeuronScene.Instantiate<LearnableNeuron>();
				}

				neuron.Name = "neuron" + layerIndex + "_" + neuronIndex;
				_neurons[layerIndex].Add(neuron);

				AddChild(neuron);
				neuron.Owner = GetTree().Root;
			}
		}

		_dummyNeuron = inputNeuronScene.Instantiate<InputNeuron>();
		_dummyNeuron.Visible = false;
		_dummyNeuron.Name = "DummyNeuron";
		AddChild(_dummyNeuron);
	}

	/// <summary>
	/// Initialize neurons starting from first hidden layer
	/// </summary>
	private void InitializeNeuronData()
	{
		// initialize input neurons
		for (int neuronIndex = 0; neuronIndex < _neuronsPerLayer[0]; neuronIndex++)
		{
			InputNeuron neuron = GetNeuron<InputNeuron>(0, neuronIndex);
			neuron.Initialize(0, neuronIndex);
		}

		// initialize neurons
		TrainingArtifact artifact = _tartWrapperNode.TrainingArtifact;
		for (int layerIndex = 1; layerIndex < _numLayersTotal; layerIndex++)
		{
			TrainingArtifact.LayerState layerState = artifact.InitialStates[layerIndex - 1];

			NDArray weights = np.array(layerState.Weights);
			weights = weights.reshape(_neuronsPerLayer[layerIndex - 1], _neuronsPerLayer[layerIndex]);

			for (int neuronIndex = 0; neuronIndex < _neuronsPerLayer[layerIndex];  neuronIndex++)
			{
				byte neuronBias = layerState.Biases[neuronIndex];
				// transpose weights as we are reading the data "right to left" instead of left to right,
				// meaning that we draw the dendrites from the right layer neurons to the left
				byte[] neuronWeights = weights.T.GetData(neuronIndex).ToArray<byte>();

				float avgActivation = layerState.AvgNeuronActivations[neuronIndex];

				LearnableNeuron neuron = GetNeuron<LearnableNeuron>(layerIndex, neuronIndex);
				neuron.Initialize(neuronBias, neuronWeights, layerIndex, neuronIndex);
				neuron.SetAvgActivation(avgActivation);
			}
		}
	}

	private void ArrangeNeurons()
	{
		

		Vector3[] layerCenters = new Vector3[NumLayersTotal];
		int[] nextSquares = new int[NumLayersTotal];

		for (int i = 0; i < NumLayersTotal; i++)
		{
			nextSquares[i] = (int)Mathf.Ceil(Mathf.Sqrt(NeuronsPerLayer[i]));
			layerCenters[i] = new Vector3(nextSquares[i] * NeuronOffset, nextSquares[i] * NeuronOffset, 0) / 2;
		}

		// calc camera center (center of NN)
		int largestSquareCenter = (int)layerCenters.ToList().MaxBy(v => v.X).X;
		int layersZCenter = LayerOffset * NumLayersTotal / (NumLayersTotal - 1);
		Vector3 center = new Vector3(largestSquareCenter, largestSquareCenter, layersZCenter);
		Node3D camera = GetNode<Node3D>("/root/VisualizationManager/PlayerController/CamParent/OrbitCamera");
		camera.Call("set_center", center);

		// arrange neurons in a square XY plane
		for (int layerIndex = 0; layerIndex < NumLayersTotal; layerIndex++)
		{
			int neuronInRowIndex = 0;
			int yOffset = 0;
			for (int neuronIndex = 0; neuronIndex < NeuronsPerLayer[layerIndex]; neuronIndex++)
			{
				// check if row has been completed (except on output layer)
				if (layerIndex != NumLayersTotal - 1)
				{
					// first neuron should not be affected
					if (neuronIndex > 0)    
					{
						// check if row has been completed
						if (neuronIndex % nextSquares[layerIndex] == 0)
						{
							yOffset += NeuronOffset;
							// reset count for that row
							neuronInRowIndex = 0;
						}
					}
				}
			
				Vector3 neuronPos = new Vector3(neuronInRowIndex * NeuronOffset, yOffset, layerIndex * LayerOffset);

				// move neurons towards largest layer's center (in XY plane)
				int maxSquareIndex = np.argmax(nextSquares);
				if (layerIndex != maxSquareIndex)
				{
					Vector3 from = layerCenters[layerIndex];
					Vector3 to = layerCenters[maxSquareIndex];
					to.Z = 0;
					neuronPos += to - from;
				}

				// rotate around z axis around center point
				Vector3 rotationAxis = new Vector3(0, 0, 1);
				Vector3 commonCenter = layerCenters[maxSquareIndex];
				Vector3 pivot = new Vector3(commonCenter.X, commonCenter.Y, neuronPos.Z);
				Vector3 neuronPosTranslated = neuronPos - pivot;
				Vector3 neuronPosRotated = neuronPosTranslated.Rotated(rotationAxis, Mathf.DegToRad(180));
				neuronPos = neuronPosRotated + pivot;

				// apply position
				GetNeuron(layerIndex, neuronIndex).Position = neuronPos;
				neuronInRowIndex++;
			}
		}
	}

	private void ArrangeDendrites()
	{
		_dendriteManager.UpdateDendriteStrands();
	}

	private void SetMetadata(TrainingArtifact artifact)
	{
		// add 1 for input layer
		int numLayers = artifact.Header.NumLayers + 1;
		int[] neuronsPerLayer = new int[numLayers];

		// input layer's neuron count equals the features of the samples (i.e. pixels of an image)
		neuronsPerLayer[0] = artifact.Header.NumFeatures;

		// get data for remaining layers
		TrainingArtifact.TrainingStep step = artifact.TrainingSteps[0];
		for (int i = 1; i < numLayers; i++)
		{
			/*
			the number of neurons
			(starting from the first hidden layer, which is the first LayerState in each TrainingStep)
			equals the number of biases in this layer
			*/
			TrainingArtifact.LayerState state = step.LayerStates[i - 1];

			neuronsPerLayer[i] = state.NumBiases;
		}

		_numLayersTotal = numLayers;
		_neuronsPerLayer = neuronsPerLayer;
	}

	// ---------- called by gd scripts ----------

	public int GetNumLayers()
	{
		return _numLayersTotal;
	}

	public int GetNeuronsPerLayer(int layerIndex)
	{
		return NeuronsPerLayer[layerIndex];
	}

	public InputNeuron GetDummyNeuron()
	{
		return _dummyNeuron;
	}

	public string GetLossFunctionName()
	{
		return _tartWrapperNode.TrainingArtifact.Header.LossFunction.ToString();
	}

	public string GetActivationFunctionName(int layerIndex)
	{
		return _tartWrapperNode.TrainingArtifact.TrainingSteps[0].LayerStates[layerIndex].ActivationFunction.ToString();
	}

	public float GetLearningRate()
	{
		return _tartWrapperNode.TrainingArtifact.Header.LearningRate;
	}
}
