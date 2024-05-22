import numpy as np
from enum import Enum
from net.functions.base_function import BaseFunction


class LossType(Enum):
    MSE = 0
    CROSS_E = 1


def _mse(y_true, y_pred):
    return np.mean(np.power(y_true - y_pred, 2))


def _mse_prime(y_true, y_pred):
    return 2 * (y_pred - y_true) / y_true.size


def _cross_entropy(y_true, y_pred):
    n_classes = 3
    """
    Calculate categorical cross-entropy loss.

    Parameters:
        y_true (numpy array): True labels, one-hot encoded. Shape (num_samples, num_classes).
        y_pred (numpy array): Predicted probabilities, shape (num_samples, num_classes).

    Returns:
        float: Categorical cross-entropy loss.
    """
    epsilon = 1e-15  # To prevent division by zero
    y_pred = np.clip(y_pred, epsilon, 1 - epsilon)  # Clip predicted values to avoid log(0) errors
    loss = -np.sum(y_true * np.log(y_pred)) / n_classes
    return loss


def _cross_entropy_prime(y_true, y_pred):
    n_classes = 3
    """
    Calculate the derivative of categorical cross-entropy loss with respect to y_pred.

    Parameters:
        y_true (numpy array): True labels, one-hot encoded. Shape (num_samples, num_classes).
        y_pred (numpy array): Predicted probabilities, shape (num_samples, num_classes).

    Returns:
        numpy array: Derivative of categorical cross-entropy loss with respect to y_pred.
                     Shape (num_samples, num_classes).
    """
    epsilon = 1e-15  # To prevent division by zero
    y_pred = np.clip(y_pred, epsilon, 1 - epsilon)  # Clip predicted values to avoid log(0) errors
    dloss = - y_true / (y_pred * n_classes)
    return dloss


losses = {
    LossType.MSE: BaseFunction(_mse, _mse_prime),
    LossType.CROSS_E: BaseFunction(_cross_entropy, _cross_entropy_prime)
}
