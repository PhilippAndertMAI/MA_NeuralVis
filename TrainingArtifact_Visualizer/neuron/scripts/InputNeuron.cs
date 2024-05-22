using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InputNeuron : AbstractNeuron
{
	private DendriteManager _dendriteManager;
	private NetworkManager _networkManager;

	public override void Initialize(byte value, int neuronIndex)
	{
		_neuronIndex = neuronIndex;
		SetIntensity(value);
	}

	public override void SetIntensity(byte intensity, bool showUpdate = false)
	{
		_intensity = intensity;

		if (_valueLabel.Visible)
		{
			UpdateValueLabel(intensity);
		}

		ModulateBaseColor(intensity);
	}

	protected override void UpdateValueLabel()
	{
		UpdateValueLabel(_intensity);
	}

	public override void SetSelected(bool val, float selectionStrength, int selectionDirection)
	{
		base.SetSelected(val, selectionStrength, selectionDirection);

		if (selectionDirection != 0)
		{
			return;
		}

		selectionStrength -= StrengthFalloffSpeed;

		if (_dendriteManager == null)
		{
			_dendriteManager = GetNode<VisualizationManager>("/root/VisualizationManager").DendriteManager;
		}
		if (_networkManager == null)
		{
			_networkManager = GetNode<VisualizationManager>("/root/VisualizationManager").NetworkManager;
		}

		List<LearnableNeuron> nextConnectingNeurons = _dendriteManager.RootNeurons.FindAll((LearnableNeuron n) =>
		{
			return n.ActiveDendrites.Values.ToList().Exists((Dendrite d) => d.StartNeuronIndex == NeuronIndex);
		});

		if (nextConnectingNeurons.Count > 0)
		{
			foreach (var neuron in nextConnectingNeurons)
			{
				int nextDirection = 1;
				// find next neuron
				LearnableNeuron nextNeuron = _networkManager.GetNeuron<LearnableNeuron>(1, neuron.NeuronIndex);
				foreach (var dendrite in nextNeuron.ActiveDendrites.Values)
				{
					nextNeuron.SetSelected(val, selectionStrength, nextDirection);
				}
			}
		}
	}
}
