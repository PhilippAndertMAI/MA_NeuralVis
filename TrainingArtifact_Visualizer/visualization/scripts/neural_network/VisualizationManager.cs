using Godot;
using System;
using Kaitai;
using NumSharp;
using System.Runtime.CompilerServices;

public partial class VisualizationManager : Node
{
	private NetworkManager _networkManager;
	private DendriteManager _dendriteManager;

	private string _dataPath = "res://data/input/training.data.npz";
	private string _targetPath = "res://data/input/training.target.npz";
	private string _artifactPath = "res://data/training/artifact_balanced.tart";

	private string _inputDataName = "training.data.npz";
	private string _inputTargetName = "training.target.npz";

	private TrainingStepper _trainingStepper;

	private PlaybackController _playbackController;

	public NetworkManager NetworkManager { get { return _networkManager; } }

	public DendriteManager DendriteManager { get { return _dendriteManager; } }

	public override void _Ready()
	{
		TrainingArtifactWrapper tartWrapperNode = new TrainingArtifactWrapper();
		tartWrapperNode.Name = "TrainingArtifactWrapper";
		tartWrapperNode.SetPaths(_artifactPath, _dataPath, _targetPath);
		tartWrapperNode.Initialize();
		AddChild(tartWrapperNode);

		DendriteManager dendriteManagerNode = new DendriteManager();
		dendriteManagerNode.Name = "DendriteManager";
		_dendriteManager = dendriteManagerNode;
		AddChild(dendriteManagerNode);

		NetworkManager networkManagerNode = new NetworkManager();
		networkManagerNode.Name = "NetworkManager";
		_networkManager = networkManagerNode;
		AddChild(networkManagerNode);

		TrainingStepper stepperNode = new TrainingStepper();
		stepperNode.Name = "TrainingStepper";
		_trainingStepper = stepperNode;
		AddChild(stepperNode);

		ActivityMapManager activityMapManager = GetNode<ActivityMapManager>("./ActivityMapManager");
		activityMapManager.Initialize();

		PlaybackController playbackControllerNode = new PlaybackController();
		playbackControllerNode.Name = "PlaybackController";
		playbackControllerNode.Initialize(_trainingStepper);
		_playbackController = playbackControllerNode;
		AddChild(playbackControllerNode);

		PackedScene guiScene = GD.Load<PackedScene>("res://gui/Gui.tscn");
		UIController gui = guiScene.Instantiate<UIController>();
		gui.Name = "GUI";
		AddChild(gui);

		_playbackController.ConnectGuiSignals(gui);

		GetNode<Node>("./PlayerController/CamParent/OrbitCamera").Call("initialize", gui);
		GetNode<Node>("./PlayerController/SelectionController").Call("initialize", gui);

		gui.ToggleAppGuide();
	}
}
