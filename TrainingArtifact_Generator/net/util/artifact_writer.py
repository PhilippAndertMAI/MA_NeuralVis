import io

import numpy as np
from kaitaistruct import KaitaiStream

from net.functions.activations import ActivationType

from libraries.schemas.training_artifact import TrainingArtifact
from net.functions.losses import LossType

from net.layers.dense_layer import DenseLayer
from net.util.sample_wrapper import SampleWrapper
from net.util.statistics_util import iterative_average


class ArtifactWriter:
    def __init__(self, path: str):
        self.path = path
        self.artifact = TrainingArtifact()

        self.artifact.training_steps = None
        self.n_bytes = 0

    def initialize(self, num_features: int, num_samples: int, num_layers: int, num_epochs: int, num_epoch_indices: int, epoch_indices: [int], learning_rate: float, layers: [DenseLayer], loss_type: LossType):
        self._set_header(num_features, num_samples, num_layers, num_epochs, num_epoch_indices, epoch_indices, learning_rate, loss_type)

        self.artifact.metrics = self.artifact.InputMetrics(None, self.artifact, self.artifact._root)
        self.artifact.metrics.avg_feature_values = np.zeros(shape=num_features, dtype=np.float32)
        self.artifact.metrics.num_correct_predictions = np.zeros(shape=num_epoch_indices * num_samples, dtype=np.int)
        self.artifact.initial_states = self._create_layer_states(layers, self.artifact)

        self._calc_bytes(layers)

    def _set_header(self, num_features: int, num_samples: int, num_layers: int, num_epochs: int, num_epoch_indices: int, epoch_indices: [int], learning_rate: float, loss_type: LossType):
        header = self.artifact.MainHeader(None, self.artifact, self.artifact._root)

        header.num_features = num_features
        header.num_samples = num_samples
        header.num_layers = num_layers
        header.num_epochs = num_epochs
        header.num_epoch_indices = num_epoch_indices
        header.epoch_indices = epoch_indices
        header.learning_rate = np.float32(learning_rate)
        header.loss_function = TrainingArtifact.LossFunctions(loss_type.value)

        header._check()

        self.artifact.header = header

    def _calc_bytes(self, layers: [DenseLayer]):
        # TODO: this seems tedious to correct if kaitai schema changes. try to get types programmatically

        # header bytes
        header_bytes = 2 + 2 + self.artifact.header.num_epoch_indices * 2 + 2 + 2 + 1 + 4 + 1

        # input metrics bytes
        input_metrics_bytes = 4 * self.artifact.header.num_features + 2 * self.artifact.header.num_epoch_indices * self.artifact.header.num_samples

        # layer_state bytes
        layer_state_bytes = 0
        for layer in layers:
            layer_state_bytes += 1 + 2 + 4 + layer.biases.size * 1 + layer.weights.size * 1 + layer.biases.size * 4

        # training_step bytes
        n_steps = self.artifact.header.num_epoch_indices * self.artifact.header.num_samples
        training_step_bytes = n_steps * (2 + 2 + 2 + layer_state_bytes + 4)

        self.n_bytes = header_bytes + input_metrics_bytes + layer_state_bytes + training_step_bytes

    def _create_layer_state(self, layer: DenseLayer, parent):
        layer_state = self.artifact.LayerState(None, parent, parent._root)

        layer_state.activation_function = TrainingArtifact.ActivationFunctions(layer.activation_type.value)

        # normalize float values into 1 byte range and cast to u1
        layer_state.biases = (layer.biases * 255).astype(np.uint8)
        layer_state.weights = (layer.weights * 255).astype(np.uint8)

        # call numpy.ravel() to store as 1D array
        layer_state.biases = layer_state.biases.ravel()
        layer_state.weights = layer_state.weights.ravel()

        layer_state.num_biases = layer_state.biases.shape[0]
        layer_state.num_weights = layer_state.weights.shape[0]

        layer_state.avg_neuron_activations = layer.avg_neuron_activations.ravel()

        layer_state._check()

        return layer_state

    def write_step(self, skip_step: bool, epoch_index: int, sample_index: int, n_samples: int, input: SampleWrapper, predicted_index: int, layers: list[DenseLayer], avg_error: float, correct_predictions: int):
        if skip_step or epoch_index not in self.artifact.header.epoch_indices:
            return

        if self.artifact.training_steps is None:
            if self.artifact.header is None:
                raise Exception("Header was empty during initialization of training steps. Please set header info first.")
            else:
                self.artifact.training_steps = np.ndarray(
                    shape=self.artifact.header.num_epoch_indices * self.artifact.header.num_samples,
                    dtype=TrainingArtifact.TrainingStep
                )

        self.artifact.metrics.avg_feature_values = iterative_average(
            self.artifact.metrics.avg_feature_values,
            input.data,
            (epoch_index * n_samples) + sample_index + 1
        )

        # check if last writing step to ravel the nd array (as input needs to be stored 1-dimensionally)
        if self.artifact.header.epoch_indices[self.artifact.header.num_epoch_indices - 1] == epoch_index and \
            sample_index == n_samples - 1:
            self.artifact.metrics.avg_feature_values = self.artifact.metrics.avg_feature_values[0].ravel()

        training_step = self.artifact.TrainingStep(None, self.artifact, self.artifact._root)

        training_step.epoch_index = epoch_index
        training_step.input_index = input.index_abs
        training_step.predicted_index=predicted_index
        training_step.layer_states = self._create_layer_states(layers, training_step)
        training_step.avg_loss = np.float32(avg_error)

        num_epochs_already_stored = self.artifact.header.epoch_indices.index(epoch_index)
        step_index = (num_epochs_already_stored * n_samples) + sample_index
        self.artifact.training_steps[step_index] = training_step

        self.artifact.metrics.num_correct_predictions[step_index] = correct_predictions

    def _create_layer_states(self, layers: [DenseLayer], parent):
        return [(lambda l: self._create_layer_state(l, parent))(l) for l in layers]

    def write(self):
        self.artifact._check()
        io_ = KaitaiStream(io.BytesIO(bytearray(self.n_bytes)))
        self.artifact._write(io_)
        output = io_.to_byte_array()
        with open(self.path, "wb") as f:
            f.write(output)
