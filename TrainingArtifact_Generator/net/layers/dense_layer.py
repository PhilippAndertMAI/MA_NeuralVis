import numpy as np
from net.functions.activations import activations, ActivationType
from net.util.statistics_util import iterative_average


class DenseLayer:
    def __init__(self, input_size: int, output_size: int, activation_type: ActivationType):
        self.weights = np.random.rand(input_size, output_size) - 0.5
        self.biases = np.random.rand(1, output_size) - 0.5

        self.activation_type = activation_type
        self.activation = activations[activation_type]

        # data solely for recording in training artifact
        self.n = 0
        self.avg_neuron_activations = np.zeros(shape=output_size, dtype=np.float32)

        self.input = None
        self.output = None

    def forward_propagation(self, input_data: np.ndarray):
        self.input = input_data
        z = np.dot(self.input, self.weights) + self.biases
        self.output = self.activation(z)

        # used only by training artifact, not relevant to training
        self.n += 1
        self.avg_neuron_activations = iterative_average(self.avg_neuron_activations, self.output, self.n)

        return z, self.output

    def backward_propagation(self, output_error: np.ndarray, learning_rate: float):
        input_error = np.dot(output_error, self.weights.T)
        weights_gradient = np.dot(self.input.T, output_error)

        self.weights -= learning_rate * weights_gradient
        self.biases -= learning_rate * output_error
        return input_error
