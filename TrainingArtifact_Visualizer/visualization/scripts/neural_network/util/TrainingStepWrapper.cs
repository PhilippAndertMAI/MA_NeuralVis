using Godot;
using Kaitai;
using System;
using NumSharp;
using System.Collections.Generic;

public class TrainingStepWrapper
{
	public ushort EpochIndex { get; set; }
	public ushort InputIndex { get; set; }

	public List<TrainingArtifact.LayerState> LayerStates { get; set; }

	public ushort PredictedIndex { get; set; }

	public float AvgLoss { get; set; }

	public TrainingStepWrapper(TrainingArtifact.TrainingStep trainingStep)
	{
		EpochIndex = trainingStep.EpochIndex;
		InputIndex = trainingStep.InputIndex;
		LayerStates = trainingStep.LayerStates;
		PredictedIndex = trainingStep.PredictedIndex;
		AvgLoss = trainingStep.AvgLoss;
	}

	public byte[] GetBiases(int layerIndex)
	{
		return LayerStates[layerIndex].Biases.ToArray();
	}

	public byte GetBiases(int layerIndex, int neuronIndex)
	{
		return LayerStates[layerIndex].Biases[neuronIndex];
	}

	public NDArray GetWeights(int layerIndex)
	{
		return np.array(LayerStates[layerIndex].Weights);
	}

		public NDArray GetWeights(int layerIndex, params int[] shape)
	{
		NDArray weights = np.array(LayerStates[layerIndex].Weights);
		weights = weights.reshape(shape[0], shape[1]);
		return weights;
	}

	public TrainingArtifact.ActivationFunctions GetActivationFunctions(int layerIndex)
	{
		return LayerStates[layerIndex].ActivationFunction;
	}
}
