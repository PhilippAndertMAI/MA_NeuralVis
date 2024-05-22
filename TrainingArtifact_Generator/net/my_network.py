import logging

from net.util.artifact_writer import ArtifactWriter
from net.functions.losses import losses, LossType
from net.util.sample_wrapper import SampleWrapper
import numpy as np

from net.util.statistics_util import iterative_average


class MyNetwork:
    def __init__(self, record_artifact:bool=False, artifact_path:str= '', epoch_indices=None):
        self.layers = []

        self.loss_type = None
        self.loss = None

        self.record_artifact = record_artifact
        self._artifact_writer = ArtifactWriter(artifact_path)
        self.epoch_indices = epoch_indices

    def add(self, layer):
        # append layer to list and convert to numpy array (required by kaitai serializer)
        # TODO: refactor :>
        layers = []
        for l in self.layers:
            layers.append(l)
        layers.append(layer)
        layers = np.array(layers)
        self.layers = layers

    def use(self, loss_name: LossType):
        self.loss_type = loss_name
        self.loss = losses[loss_name]

    def predict(self, inputs_: [SampleWrapper]):
        n_samples = len(inputs_)
        result = []

        for i in range(n_samples):
            # forward pass
            output = inputs_[i].data
            for layer in self.layers:
                output = layer.forward_propagation(output)
            result.append(output)

        return result

    def fit(self, x_train: [SampleWrapper], y_train: [SampleWrapper], epochs: int, learning_rate: float):
        n_samples = len(x_train)

        if self.record_artifact:
            self._artifact_writer.initialize(
                num_features=x_train[0].data.size,
                num_samples=n_samples,
                num_layers=len(self.layers),
                num_epochs=epochs,
                num_epoch_indices=len(self.epoch_indices),
                epoch_indices=self.epoch_indices,
                learning_rate=learning_rate,
                layers=self.layers,
                loss_type=self.loss_type
            )

        correct_predictions = 0
        for i in range(epochs):
            err_display = 0
            it_err_display = 0
            for j in range(n_samples):
                # forward propagation
                output = x_train[j].data
                z = 0
                for index, layer in enumerate(self.layers):
                    z, output = layer.forward_propagation(output)

                # compute loss (for display purpose only)
                err_val_display = self.loss(y_train[j].data, output)
                err_display += err_val_display
                it_err_display = iterative_average(cur_avg=it_err_display, val=err_val_display, n=i*j+j+1)

                # count correct prediction for tart
                predicted_index = int(np.argmax(output[0]))
                correct_index = int(np.argmax(y_train[j].data))
                if predicted_index == correct_index:
                    correct_predictions += 1

                # backward propagation
                error = self.loss.prime(y_train[j].data, output) * self.layers[len(self.layers) - 1].activation.prime(z)
                for layer in reversed(self.layers):
                    error = layer.backward_propagation(error, learning_rate)

                self._artifact_writer.write_step(skip_step=not self.record_artifact,
                                                 epoch_index=i,
                                                 sample_index=j,
                                                 n_samples=n_samples,
                                                 input=x_train[j],
                                                 predicted_index=int(np.argmax(output[0])),
                                                 layers=self.layers,
                                                 avg_error=it_err_display,
                                                 correct_predictions=correct_predictions)

            # calculate average error on all samples
            err_display /= n_samples
            #print('epoch %d/%d   error=%f' % (i + 1, epochs, err_display))
            print('epoch %d/%d   iterative error=%f' % (i + 1, epochs, it_err_display))
            #print('epoch %d/%d   iterative error diff=%f' % (i + 1, epochs, err_display - it_err_display))

    def write_artifact(self):
        if self.record_artifact:
            self._artifact_writer.write()