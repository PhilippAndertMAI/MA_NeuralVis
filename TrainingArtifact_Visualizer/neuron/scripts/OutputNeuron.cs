using Godot;
using System;

public partial class OutputNeuron : LearnableNeuron
{
	private Label3D _outputLabel;

	public override void _Ready()
	{
		base._Ready();

		_outputLabel = GetNode<Label3D>("./OutputLabel");
	}

	public override void Initialize(byte bias, byte[] weights, int layerIndex, int neuronIndex)
	{
		base.Initialize(bias, weights, layerIndex, neuronIndex);

		_outputLabel.Text = neuronIndex.ToString();
	}

	public override void CheckMute()
	{
		base.CheckMute();

		_tweenManager.FinishTween("labelModulate");

		_outputLabel.Modulate = new Color(_outputLabel.Modulate, _muted ? 0.1f : 1.0f);
		_outputLabel.OutlineModulate = new Color(_outputLabel.OutlineModulate, _muted ? 0.1f : 1.0f);
	}

	public void LightUpLabel(Color color)
	{
		TweenConfig modulateConfig = new TweenConfig();
		modulateConfig.TweenKey = "labelModulate";
		modulateConfig.Object = _outputLabel;
		modulateConfig.Property = "modulate";
		modulateConfig.FinalVal = new Color(Colors.White, _muted ? 0.1f : 1.0f);
		modulateConfig.Duration = _lightUpDuration;

		TweenConfig scaleConfig = new TweenConfig();
		scaleConfig.TweenKey = "labelScale";
		scaleConfig.Object = _outputLabel;
		scaleConfig.Property = "scale";
		scaleConfig.FinalVal = new Vector3(1.0f, 1.0f, 1.0f);
		scaleConfig.Duration = _lightUpDuration;

		_tweenManager.ResetTweens(modulateConfig, scaleConfig);

		_outputLabel.Modulate = color;
		_outputLabel.Scale *= 2f;
	}
}
