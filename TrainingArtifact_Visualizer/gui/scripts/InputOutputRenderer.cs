using Godot;
using NumSharp;
using System;

public partial class InputOutputRenderer : Node
{
	private TrainingArtifactWrapper _tartWrapper;
	private TrainingStepper _trainingStepper;

	[Export]
	public TextureRect inputTextureRect;
	[Export]
	public Label outputLabel;

	private ImageTexture _inputTexture;

	private int _dataWidth;
	private Image.Format _imageFormat = Image.Format.L8;

	public override void _Ready()
	{
		_tartWrapper = GetNode<TrainingArtifactWrapper>("/root/VisualizationManager/TrainingArtifactWrapper");
		_trainingStepper = GetNode<TrainingStepper>("/root/VisualizationManager/TrainingStepper");

		InitializeInputImage();
		_trainingStepper.Connect("skipped", new Callable(this, nameof(HandleSkipped)));

		outputLabel.Text = "";
		_trainingStepper.Connect("predicted", new Callable(this, nameof(HandlePredicted)));
	}

	private void InitializeInputImage()
	{
		_inputTexture = new ImageTexture();

		Image inputImage = new Image();

		int dataWidthSquared = _tartWrapper.TrainingData.Data.Shape[2];
		float dataWidth = Mathf.Sqrt(dataWidthSquared);
		if (dataWidth - (int)dataWidth != 0)
		{
			throw new Exception("Input data not arrangeable in a square");
		}

		_dataWidth = (int)dataWidth;
		NDArray zeroes = np.zeros(shape: new Shape(dataWidthSquared), dtype: NPTypeCode.Byte.AsType());
		inputImage.SetData(_dataWidth, _dataWidth, false, _imageFormat, zeroes.ToByteArray());

		_inputTexture.SetImage(inputImage);

		inputTextureRect.Texture = _inputTexture;
	}

	private void HandlePredicted(int predictedIndex, bool isCorrect)
	{
		if (predictedIndex == -1)
		{
			outputLabel.Text = "";
			return;
		}
		outputLabel.Text = "predicted: " + predictedIndex + ", correct: " + isCorrect;
	}

	private void HandleSkipped(int currentTrainingStepIndex, int _, int __)
	{
		UpdateInputTexture(currentTrainingStepIndex);
	}

	private void UpdateInputTexture(int currentTrainingStepIndex)
	{
		int inputIndex = _tartWrapper.TrainingStep(currentTrainingStepIndex).InputIndex;
		NDArray newData = _tartWrapper.TrainingData.Data[inputIndex][0];
		newData *= 255.0f;
		newData = newData.astype(NPTypeCode.Byte);

		Image newImage = new Image();
		newImage.SetData(_dataWidth, _dataWidth, false, _imageFormat, newData.ToByteArray());
		ImageTexture newTexture = new ImageTexture();
		newTexture.SetImage(newImage);

		inputTextureRect.Texture = newTexture;
	}

}
