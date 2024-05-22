using Godot;
using System;
using System.ComponentModel.DataAnnotations;

public partial class ActivityMapManager : Node3D
{
	private NetworkManager _networkManager;
	private Node _selectionController;

	private PackedScene _activityMapScene;

	private Node3D[] _maps;

	private bool _isVisible = true;

	private float _strength = 0.575f;
	private float _size = 1.9f;

	public float Thickness { get; set; } = 1.0f;
	public float Density { get; set; } = 5.0f;

	[Export, Range(0.0f, 2.0f)]
	public float Strength { get { return _strength; } set { _strength = value; UpdateAllShaders(); } }

	[Export, Range(1.0f, 2.0f)]
	public float Size { get { return _size; } set { _size = value; UpdateAllShaders(); } }

	public override void _Ready()
	{
		
	}

	public void Initialize()
	{
		_networkManager = GetNode<NetworkManager>("/root/VisualizationManager/NetworkManager");
		_activityMapScene = GD.Load<PackedScene>("res://visualization/ActivityMap.tscn");
		_selectionController = GetNode<Node>("/root/VisualizationManager/PlayerController/SelectionController");

		AddUserSignal("toggled_visibility", new Godot.Collections.Array()
		{
			new Godot.Collections.Dictionary()
			{
				{ "name", "visible" },
				{ "type", (int)Variant.Type.Int }
			}
		});

		_maps = new Node3D[_networkManager.NumLayersTotal - 1];
		for (int layerIndex = 1; layerIndex < _networkManager.NumLayersTotal; layerIndex++)
		{
			InitializeActivityMap(layerIndex);
		}
		ToggleVisibility();     // toggle OFF
	}

	private void InitializeActivityMap(int layerIndex)
	{
		ActivityMap activityMap = _activityMapScene.Instantiate<ActivityMap>();
		activityMap.Name = "activityMap_layer" + layerIndex;
		_maps[layerIndex - 1] = activityMap;
		AddChild(activityMap);

		// determine bounds of map
		float minX = 10000;
		float maxX = 0;
		float minY = 10000;
		float maxY = 0;
		Node3D neuron = _networkManager.GetNeuron(layerIndex, 0); ;
		for (int neuronIndex = 0; neuronIndex < _networkManager.NeuronsPerLayer[layerIndex]; neuronIndex++)
		{
			neuron = _networkManager.GetNeuron(layerIndex, neuronIndex);
			if (neuron.Position.X < minX)
			{
				minX = neuron.Position.X;
			}
			if (neuron.Position.X > maxX)
			{
				maxX = neuron.Position.X;
			}
			if (neuron.Position.Y < minY)
			{
				minY = neuron.Position.Y;
			}
			if (neuron.Position.Y > maxY)
			{
				maxY = neuron.Position.Y;
			}
		}

		float posX = minX - Thickness * 0.5f;
		float sizeX = maxX - minX + Thickness;
		float posY = minY - Thickness * 0.5f;
		float sizeY = maxY - minY + Thickness;
		float posZ = neuron.Position.Z - Thickness * 0.5f;
		float sizeZ = Thickness;

		// set bounds
		FogVolume fogVolume = activityMap.GetNode<FogVolume>("./FogVolume");
		fogVolume.GlobalPosition = new Vector3(posX + sizeX * 0.5f, posY + sizeY * 0.5f, posZ + sizeZ * 0.5f);
		fogVolume.Size = new Vector3(sizeX, sizeY, sizeZ);

		activityMap.Initialize(layerIndex, minX, minY);
	}

	public void ToggleVisibility()
	{
		_isVisible = !_isVisible;

		foreach (var map in _maps)
		{
			map.Visible = _isVisible;
		}

		_selectionController.Call("unfocus_neuron");
		
		_networkManager.GetDummyNeuron().SetSelected(_isVisible, 1.0f, 0);

		EmitSignal("toggled_visibility", _isVisible);
	}

	public void UpdateAllShaders()
	{
		foreach (ActivityMap map in _maps)
		{
			map.UpdateShader();
		}
	}

	// ---------- called by gd scripts ----------
	
	public bool GetIsVisible()
	{
		return _isVisible;
	}
}
