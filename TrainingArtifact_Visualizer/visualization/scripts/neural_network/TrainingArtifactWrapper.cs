using Godot;
using Kaitai;
using NumSharp;
using System;
using TrainingArtifactReaderNS;

public partial class TrainingArtifactWrapper : Node
{
	string _artifactPath;
	string _dataPath;
	string _targetPath;

	public TrainingArtifact TrainingArtifact { get; set; }

	public TrainingData TrainingData { get; set; }

	public int NumTrainingStepsStored { get { return TrainingArtifact.TrainingSteps.Count; } }

	public TrainingStepWrapper TrainingStep(int trainingStepIndex) {
		return new TrainingStepWrapper(TrainingArtifact.TrainingSteps[trainingStepIndex]);
	}

	public void SetPaths(string artifactPath, string dataPath, string targetPath)
	{
		_artifactPath = artifactPath;
		_dataPath = dataPath;
		_targetPath = targetPath;
	}

	public void Initialize()
	{
		TrainingArtifact = LoadArtifact();
		TrainingData = LoadData();
	}

	private TrainingArtifact LoadArtifact()
	{
		var tartBytes = FileAccess.GetFileAsBytes(_artifactPath);

		TrainingArtifactReader reader = new TrainingArtifactReader();
		return reader.FromBytes(tartBytes);
	}

	private TrainingData LoadData()
	{
		try
		{
			var dataNpzBytes = FileAccess.GetFileAsBytes(_dataPath);
			var targetNpzBytes = FileAccess.GetFileAsBytes(_targetPath);

			var dataNpz = np.Load_Npz<double[,,]>(dataNpzBytes);
			var targetNpz = np.Load_Npz<float[,]>(targetNpzBytes);
			NDArray data = dataNpz["X.npy"];
			NDArray target = targetNpz["y.npy"];
			return new TrainingData(data, target);
		}
		catch (Exception)
		{
			GD.PrintErr("Could not load training artifact.");
		}
		return null;
	}
}
