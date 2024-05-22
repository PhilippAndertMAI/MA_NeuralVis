using Godot;
using Kaitai;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public partial class DendriteManager : Node
{
	private NetworkManager _networkManager;

	private TrainingArtifactWrapper _tartWrapper;

	private PackedScene _dendriteScene;

	private List<LearnableNeuron> _rootNeurons;

	private Node _selectionController;

	private bool _staticStrandsBuilt = false;

	public byte biasThreshold = 0;

	// value of 0 so dendrites stay the same throughout (and dont cause stutter when deleted/newly spawned)
	public byte weightThreshold = 0;

	public bool buildStaticallyFromMetrics = true;

	public int numDendriteStrands = 150;

	public int strandsPerRootNeuron = 1;

	public List<LearnableNeuron> RootNeurons { get {  return _rootNeurons; } }

	public override void _Ready()
	{
		_dendriteScene = GD.Load<PackedScene>("res://neuron/Dendrite.tscn");

		_rootNeurons = new List<LearnableNeuron>();

		_selectionController = GetNode<Node>("/root/VisualizationManager/PlayerController/SelectionController");
	}

	public void Initialize(NetworkManager networkManager, TrainingArtifactWrapper tartWrapper)
	{
		_networkManager = networkManager;

		_tartWrapper = tartWrapper;
	}

	public void BuildDendritesFromIndices(List<List<int>> neuronsToConnectIndices)
	{
		for (int layerIndex = 1; layerIndex < _networkManager.NumLayersTotal; layerIndex++)
		{
			// if a neuron received more than one connection, it should still have only one outgoing connection
			List<int> indicesAlreadyWithOutgoingConnection = new List<int>();

			for (int i = 0; i < neuronsToConnectIndices[layerIndex - 1].Count; i++)
			{
				int dendriteIndex = neuronsToConnectIndices[layerIndex - 1][i];
				if (indicesAlreadyWithOutgoingConnection.Contains(dendriteIndex))
				{
					continue;
				}
				indicesAlreadyWithOutgoingConnection.Add(dendriteIndex);

				int neuronIndex = neuronsToConnectIndices[layerIndex][i];
				LearnableNeuron neuron = _networkManager.GetNeuron<LearnableNeuron>(layerIndex, neuronIndex);

				Dendrite newDendrite = AddDendrite(neuron, dendriteIndex);

				// tell previous neurons' dendrites that they are connected to this one
				if (layerIndex > 1)
				{
					LearnableNeuron prevNeuron = _networkManager.GetNeuron<LearnableNeuron>(layerIndex - 1, newDendrite.StartNeuronIndex);
					foreach (var prevDendriteIndex in prevNeuron.ActiveDendrites.Keys)
					{
						Dendrite prevDendrite;
						prevNeuron.ActiveDendrites.TryGetValue(prevDendriteIndex, out prevDendrite);

						prevDendrite.ConnectedNeuronIndex = newDendrite.EndNeuronIndex;
					}
				}
			}
		}
	}

	public void UpdateDendriteStrands()
	{
		if (!buildStaticallyFromMetrics)
		{
			UpdateRootNeurons();
		}
		else if (!_staticStrandsBuilt)
		{
			var neuronsToConnectIndices = GetNeuronsToConnectIndices();
			BuildDendritesFromIndices(neuronsToConnectIndices);
			_staticStrandsBuilt = true;
		}
	}

	private void UpdateRootNeurons()
	{
		foreach (var neuron in _rootNeurons)
		{
			foreach (var dendriteIndex in neuron.ActiveDendrites.Keys)
			{
				Dendrite dendrite;
				neuron.ActiveDendrites.TryGetValue(dendriteIndex, out dendrite);
				if (dendrite.Weight < weightThreshold)
				{
					// if a neuron is currently selected, neurons need to be de- and reselected in order to avoid headless dendrite strands
					AbstractNeuron selectedNeuron = _selectionController.Call("get_selected_neuron").As<AbstractNeuron>();
					if (selectedNeuron != null)
					{
						selectedNeuron.SetSelected(false, 1.0f, 0);
					}
					RemoveDendriteStrand(neuron, dendriteIndex);
					if (selectedNeuron != null)
					{
						selectedNeuron.SetSelected(true, 1.0f, 0);
					}
				}
			}
		}

		// remove root neurons that might not have dendrites anymore
		_rootNeurons = _rootNeurons.Where(neuron => neuron.ActiveDendrites.Count != 0).ToList();

		int numToSpawn = numDendriteStrands - _rootNeurons.Count;
		for (int i = 0; i < numToSpawn; i++) {
			AddDendriteStrand();
		}
	}

	private List<List<int>> GetNeuronsToConnectIndices()
	{
		// sort avg values of features and keep the feature index
		var avgValAndIndex = _tartWrapper.TrainingArtifact.Metrics.AvgFeatureValues.Select((value, index) => (Value: value, Index: index))
									 .ToArray();
		avgValAndIndex = avgValAndIndex.OrderByDescending(item => item.Value)
								   .ToArray();

		// get top 100 features and store their indices to create dendrites from
		List<int> inputNeuronIndices = new List<int>();
		for (int i = 0; i < numDendriteStrands; i++)
		{
			inputNeuronIndices.Add(avgValAndIndex[i].Index);
		}

		// for non-input layers, choose connecting neurons based on avg weights at final training step
		float avgActivationThresholdsPerLayer = 0.0f;

		List<List<int>> nonInputNeuronIndices = new List<List<int>>();
		for (int layerIndex = 1; layerIndex < _networkManager.NumLayersTotal; layerIndex++)
		{
			nonInputNeuronIndices.Add(new List<int>());

			TrainingStepWrapper finalStep = _tartWrapper.TrainingStep(_tartWrapper.NumTrainingStepsStored - 1);

			int indicesToFind = numDendriteStrands;
			/*
			if (layerIndex == 1)
			{
				indicesToFind = numDendriteStrands;
			}
			else
			{
				int numUniqueNeurons = nonInputNeuronIndices[layerIndex - 2].Distinct().Count();
				indicesToFind = numUniqueNeurons;
			}
			*/

			for (int i = 0; ; i = ++i % _networkManager.NeuronsPerLayer[layerIndex])
			{
				// find suitable neuron
				if (finalStep.LayerStates[layerIndex - 1].AvgNeuronActivations[i] >= avgActivationThresholdsPerLayer)
				{
					nonInputNeuronIndices[layerIndex - 1].Add(i);
					indicesToFind--;
				}

				if (indicesToFind == 0)
				{
					break;
				}
			}
		}

		List<List<int>> resultList = new List<List<int>>();
		resultList.Add(inputNeuronIndices);
		resultList.AddRange(nonInputNeuronIndices);

		return resultList;
	}

	private void AddDendriteStrand()
	{
		// pick a random (end) neuron from first learnable neuron layer
		IEnumerable<int> randomNeuronIndices = GetRandomEnumerable(_networkManager.NeuronsPerLayer[1]);

		// query a random dendrite from that neuron that satisfies the weight threshold
		int dendriteIndex = -1;
		LearnableNeuron rootNeuron = null;

		foreach (var potentialNeuronIndex in randomNeuronIndices)
		{
			LearnableNeuron potentialRootNeuron = _networkManager.GetNeuron<LearnableNeuron>(1, potentialNeuronIndex);
			// guard
			if (_rootNeurons.Contains(potentialRootNeuron))
			{
				continue;
			}

			IEnumerable<int> randomDendriteIndices = GetRandomEnumerable(potentialRootNeuron.Weights.Count);

			foreach (var potentialDendriteIndex in randomDendriteIndices)
			{
				if (potentialRootNeuron.ActiveDendrites.Keys.Contains(potentialDendriteIndex))
				{
					continue;
				}

				if (potentialRootNeuron.Weights[potentialDendriteIndex] > weightThreshold)
				{
					rootNeuron = potentialRootNeuron;
					dendriteIndex = potentialDendriteIndex;
					break;
				}
			}
		}

		_rootNeurons.Add(rootNeuron);
		BuildStrandFromRoot(rootNeuron, dendriteIndex);
	}

	private void BuildStrandFromRoot(LearnableNeuron rootNeuron, int dendriteIndex)
	{
		LearnableNeuron currentNeuron = rootNeuron;
		int currentDendriteIndex = dendriteIndex;

		do
		{
			Dendrite newDendrite = AddDendrite(currentNeuron, currentDendriteIndex);
			// tell previous neurons' dendrites that they are connected to this one
			if (currentNeuron.LayerIndex > 1)
			{
				LearnableNeuron prevNeuron = _networkManager.GetNeuron<LearnableNeuron>(currentNeuron.LayerIndex - 1, newDendrite.StartNeuronIndex);
				foreach (var prevDendriteIndex in prevNeuron.ActiveDendrites.Keys) {
					Dendrite prevDendrite;
					prevNeuron.ActiveDendrites.TryGetValue(prevDendriteIndex, out prevDendrite);

					prevDendrite.ConnectedNeuronIndex = newDendrite.EndNeuronIndex;
				}
			}
		}
		while (TryGetRandomNext(currentNeuron, out currentNeuron, out currentDendriteIndex));
	}

	private Dendrite AddDendrite(LearnableNeuron neuron, int dendriteIndex)
	{
		Dendrite dendrite = _dendriteScene.Instantiate<Dendrite>();
		dendrite.Name = neuron.Name + "_dendrite_" + dendriteIndex;

		neuron.AddChild(dendrite);
		dendrite.Initialize(neuron.LayerIndex, neuron.NeuronIndex, dendriteIndex);
		dendrite.SetWeight(neuron.Weights[dendriteIndex]);

		neuron.ActiveDendrites.Add(dendriteIndex, dendrite);

		neuron.CheckHideDendrites();

		return dendrite;
	}

	private void RemoveDendriteStrand(LearnableNeuron rootNeuron, int dendriteIndex)
	{
		List<Dendrite> dendritesToRemove = new List<Dendrite>();

		Dendrite currentDendrite;
		rootNeuron.ActiveDendrites.TryGetValue(dendriteIndex, out currentDendrite);
		int currentDendriteIndex = dendriteIndex;

		LearnableNeuron currentNeuron = rootNeuron;

		do
		{
			dendritesToRemove.Add(currentDendrite);
		}
		while (TryGetNextForRemoval(currentNeuron, ++currentDendriteIndex, out currentNeuron, out currentDendrite));

		foreach (var dendrite in dendritesToRemove)
		{
			LearnableNeuron neuron = _networkManager.GetNeuron<LearnableNeuron>(dendrite.LayerIndex, dendrite.EndNeuronIndex);
			neuron.ActiveDendrites.Remove(dendrite.StartNeuronIndex);
			dendrite.DeleteAfterTweens();
		}
	}

	private bool TryGetNextForRemoval(LearnableNeuron currentNeuron, int currentDendriteIndex, out LearnableNeuron nextNeuron, out Dendrite nextDendrite)
	{
		nextNeuron = null;

		bool hasDendrite = currentNeuron.ActiveDendrites.TryGetValue(currentDendriteIndex, out nextDendrite);

		// guard
		if (!hasDendrite)
		{
			return false;
		}

		try
		{
			nextNeuron = _networkManager.GetNeuron<LearnableNeuron>(currentNeuron.LayerIndex + 1, nextDendrite.ConnectedNeuronIndex);
		}
		catch (Exception)
		{
			return false;
		}

		return true;
	}

	private bool TryGetRandomNext(LearnableNeuron neuron, out LearnableNeuron nextNeuron, out int nextDendriteIndex)
	{
		nextNeuron = null;
		nextDendriteIndex = -1;

		if (neuron.LayerIndex == _networkManager.NumLayersTotal - 1)
		{	
			return false;
		}

		// similar to begin of root finding, except this time don't look at weight threshold
		// and don't choose a random neuron but choose a random one connected to this neuron

		// get a random neuron fromt he next layer
		IEnumerable<int> randomNeuronIndices = GetRandomEnumerable(_networkManager.NeuronsPerLayer[neuron.LayerIndex + 1]);

		foreach (var potentialNextNeuronIndex  in randomNeuronIndices)
		{
			LearnableNeuron potentialNextNeuron = _networkManager.GetNeuron<LearnableNeuron>(neuron.LayerIndex + 1, potentialNextNeuronIndex);
			
			if (!(potentialNextNeuron.ActiveDendrites.Keys.Contains(nextDendriteIndex)))
			{
				nextDendriteIndex = neuron.NeuronIndex;
				nextNeuron = potentialNextNeuron;
			}
		}

		return nextNeuron != null;
	}

	private IEnumerable<int> GetRandomEnumerable(int count)
	{
		Random rnd = new Random();
		return Enumerable.Range(0, count).OrderBy(r => rnd.Next());
	}
}
