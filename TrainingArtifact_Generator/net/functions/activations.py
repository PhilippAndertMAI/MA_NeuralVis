import numpy as np
from enum import Enum
from net.functions.base_function import BaseFunction


class ActivationType(Enum):
    RELU = 0
    SOFTMAX = 1
    TANH = 2


def _relu(x):
    return np.maximum(0, x)


def _relu_prime(x):
    return np.where(x > 0, 1, 0)


def _tanh(x):
    return np.tanh(x)


def _tanh_prime(x):
    return 1 - np.tanh(x) ** 2


def _softmax(x):
    # Avoid numerical instability by subtracting the maximum value from each element
    # before exponentiating. This ensures the denominator of softmax does not blow up.
    max_x = np.max(x)
    exp_x = np.exp(x - max_x)

    # Add a constant term to avoid underflow issues
    epsilon = 1e-8
    exp_x += epsilon

    return exp_x / np.sum(exp_x)


def _softmax_prime(x):
    s = _softmax(x)
    N = len(x)
    d_softmax = np.zeros((N, N))
    for i in range(N):
        for j in range(N):
            d_softmax[i, j] = s[i] * (int(i == j) - s[j])
    return d_softmax


activations = {
    ActivationType.RELU: BaseFunction(_relu, _relu_prime),
    ActivationType.TANH: BaseFunction(_tanh, _tanh_prime),
    ActivationType.SOFTMAX: BaseFunction(_softmax, _softmax_prime)
}
