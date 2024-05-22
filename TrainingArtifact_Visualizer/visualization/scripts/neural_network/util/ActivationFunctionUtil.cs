using Godot;
using Kaitai;
using NumSharp;
using System;

public static class ActivationFunctionUtil
{
	private static double ManualSum(NDArray array)
	{
		double sum = 0;
		foreach (double element in array.ToArray<double>())
		{
			sum += element;
		}
		return sum;
	}

	public static NDArray ReLU(NDArray input)
	{
		var output = np.maximum(input, 0);
		return output;
	}

	public static NDArray Softmax(NDArray x)
	{
		// Avoid numerical instability by subtracting the maximum value from each element
		// before exponentiating. This ensures the denominator of softmax does not blow up.
		var max_x = np.max(x);
		var exp_x = np.exp(x - max_x);

		// Add a constant term to avoid underflow issues
		var epsilon = 1e-8;
		exp_x += epsilon;

		return exp_x / ManualSum(exp_x);
	}

	public static NDArray Tanh(NDArray input)
	{
		return np.tanh(input);
	}

	public static NDArray Activate(NDArray input, TrainingArtifact.ActivationFunctions activationType)
	{
		if (activationType == TrainingArtifact.ActivationFunctions.Relu)
		{
			return ReLU(input);
		}
		else if (activationType == TrainingArtifact.ActivationFunctions.Softmax)
		{
			return Softmax(input);
		}
		else if (activationType == TrainingArtifact.ActivationFunctions.Tanh)
		{
			return Tanh(input);
		}
		else throw new NotImplementedException();
	}
}