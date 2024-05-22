# This is a generated file! Please edit source .ksy file and use kaitai-struct-compiler to rebuild
# type: ignore

import kaitaistruct
from kaitaistruct import ReadWriteKaitaiStruct, KaitaiStream, BytesIO
from enum import Enum


if getattr(kaitaistruct, 'API_VERSION', (0, 9)) < (0, 9):
    raise Exception("Incompatible Kaitai Struct Python API: 0.9 or later is required, but you have %s" % (kaitaistruct.__version__))

class TrainingArtifact(ReadWriteKaitaiStruct):
    """"Artifact" of the backpropagation process of a neural network representing the slices of its entire training process.
    To be used for visualizing said process in i.e. a 3D environment.
    """

    class ActivationFunctions(Enum):
        relu = 0
        softmax = 1
        tanh = 2

    class LossFunctions(Enum):
        mse = 0
        cross_e = 1
    def __init__(self, _io=None, _parent=None, _root=None):
        self._io = _io
        self._parent = _parent
        self._root = _root if _root else self

    def _read(self):
        self.header = TrainingArtifact.MainHeader(self._io, self, self._root)
        self.header._read()
        self.initial_states = []
        for i in range(self._root.header.num_layers):
            _t_initial_states = TrainingArtifact.LayerState(self._io, self, self._root)
            _t_initial_states._read()
            self.initial_states.append(_t_initial_states)

        self.metrics = TrainingArtifact.InputMetrics(self._io, self, self._root)
        self.metrics._read()
        self.training_steps = []
        i = 0
        while not self._io.is_eof():
            _t_training_steps = TrainingArtifact.TrainingStep(self._io, self, self._root)
            _t_training_steps._read()
            self.training_steps.append(_t_training_steps)
            i += 1



    def _fetch_instances(self):
        pass
        self.header._fetch_instances()
        for i in range(len(self.initial_states)):
            pass
            self.initial_states[i]._fetch_instances()

        self.metrics._fetch_instances()
        for i in range(len(self.training_steps)):
            pass
            self.training_steps[i]._fetch_instances()



    def _write__seq(self, io=None):
        super(TrainingArtifact, self)._write__seq(io)
        self.header._write__seq(self._io)
        for i in range(len(self.initial_states)):
            pass
            self.initial_states[i]._write__seq(self._io)

        self.metrics._write__seq(self._io)
        for i in range(len(self.training_steps)):
            pass
            if self._io.is_eof():
                raise kaitaistruct.ConsistencyError(u"training_steps", self._io.size() - self._io.pos(), 0)
            self.training_steps[i]._write__seq(self._io)

        if not self._io.is_eof():
            raise kaitaistruct.ConsistencyError(u"training_steps", self._io.size() - self._io.pos(), 0)


    def _check(self):
        pass
        if self.header._root != self._root:
            raise kaitaistruct.ConsistencyError(u"header", self.header._root, self._root)
        if self.header._parent != self:
            raise kaitaistruct.ConsistencyError(u"header", self.header._parent, self)
        if (len(self.initial_states) != self._root.header.num_layers):
            raise kaitaistruct.ConsistencyError(u"initial_states", len(self.initial_states), self._root.header.num_layers)
        for i in range(len(self.initial_states)):
            pass
            if self.initial_states[i]._root != self._root:
                raise kaitaistruct.ConsistencyError(u"initial_states", self.initial_states[i]._root, self._root)
            if self.initial_states[i]._parent != self:
                raise kaitaistruct.ConsistencyError(u"initial_states", self.initial_states[i]._parent, self)

        if self.metrics._root != self._root:
            raise kaitaistruct.ConsistencyError(u"metrics", self.metrics._root, self._root)
        if self.metrics._parent != self:
            raise kaitaistruct.ConsistencyError(u"metrics", self.metrics._parent, self)
        for i in range(len(self.training_steps)):
            pass
            if self.training_steps[i]._root != self._root:
                raise kaitaistruct.ConsistencyError(u"training_steps", self.training_steps[i]._root, self._root)
            if self.training_steps[i]._parent != self:
                raise kaitaistruct.ConsistencyError(u"training_steps", self.training_steps[i]._parent, self)


    class MainHeader(ReadWriteKaitaiStruct):
        def __init__(self, _io=None, _parent=None, _root=None):
            self._io = _io
            self._parent = _parent
            self._root = _root

        def _read(self):
            self.num_epochs = self._io.read_u2le()
            self.num_epoch_indices = self._io.read_u2le()
            self.epoch_indices = []
            for i in range(self.num_epoch_indices):
                self.epoch_indices.append(self._io.read_u2le())

            self.num_samples = self._io.read_u2le()
            self.num_features = self._io.read_u2le()
            self.num_layers = self._io.read_u1()
            self.learning_rate = self._io.read_f4le()
            self.loss_function = KaitaiStream.resolve_enum(TrainingArtifact.LossFunctions, self._io.read_u1())


        def _fetch_instances(self):
            pass
            for i in range(len(self.epoch_indices)):
                pass



        def _write__seq(self, io=None):
            super(TrainingArtifact.MainHeader, self)._write__seq(io)
            self._io.write_u2le(self.num_epochs)
            self._io.write_u2le(self.num_epoch_indices)
            for i in range(len(self.epoch_indices)):
                pass
                self._io.write_u2le(self.epoch_indices[i])

            self._io.write_u2le(self.num_samples)
            self._io.write_u2le(self.num_features)
            self._io.write_u1(self.num_layers)
            self._io.write_f4le(self.learning_rate)
            self._io.write_u1(self.loss_function.value)


        def _check(self):
            pass
            if (len(self.epoch_indices) != self.num_epoch_indices):
                raise kaitaistruct.ConsistencyError(u"epoch_indices", len(self.epoch_indices), self.num_epoch_indices)
            for i in range(len(self.epoch_indices)):
                pass



    class InputMetrics(ReadWriteKaitaiStruct):
        def __init__(self, _io=None, _parent=None, _root=None):
            self._io = _io
            self._parent = _parent
            self._root = _root

        def _read(self):
            self.avg_feature_values = []
            for i in range(self._root.header.num_features):
                self.avg_feature_values.append(self._io.read_f4le())

            self.num_correct_predictions = []
            for i in range((self._root.header.num_epoch_indices * self._root.header.num_samples)):
                self.num_correct_predictions.append(self._io.read_u2le())



        def _fetch_instances(self):
            pass
            for i in range(len(self.avg_feature_values)):
                pass

            for i in range(len(self.num_correct_predictions)):
                pass



        def _write__seq(self, io=None):
            super(TrainingArtifact.InputMetrics, self)._write__seq(io)
            for i in range(len(self.avg_feature_values)):
                pass
                self._io.write_f4le(self.avg_feature_values[i])

            for i in range(len(self.num_correct_predictions)):
                pass
                self._io.write_u2le(self.num_correct_predictions[i])



        def _check(self):
            pass
            if (len(self.avg_feature_values) != self._root.header.num_features):
                raise kaitaistruct.ConsistencyError(u"avg_feature_values", len(self.avg_feature_values), self._root.header.num_features)
            for i in range(len(self.avg_feature_values)):
                pass

            if (len(self.num_correct_predictions) != (self._root.header.num_epoch_indices * self._root.header.num_samples)):
                raise kaitaistruct.ConsistencyError(u"num_correct_predictions", len(self.num_correct_predictions), (self._root.header.num_epoch_indices * self._root.header.num_samples))
            for i in range(len(self.num_correct_predictions)):
                pass



    class LayerState(ReadWriteKaitaiStruct):
        def __init__(self, _io=None, _parent=None, _root=None):
            self._io = _io
            self._parent = _parent
            self._root = _root

        def _read(self):
            self.activation_function = KaitaiStream.resolve_enum(TrainingArtifact.ActivationFunctions, self._io.read_u1())
            self.num_biases = self._io.read_u2le()
            self.num_weights = self._io.read_u4le()
            self.biases = []
            for i in range(self.num_biases):
                self.biases.append(self._io.read_u1())

            self.weights = []
            for i in range(self.num_weights):
                self.weights.append(self._io.read_u1())

            self.avg_neuron_activations = []
            for i in range(self.num_biases):
                self.avg_neuron_activations.append(self._io.read_f4le())



        def _fetch_instances(self):
            pass
            for i in range(len(self.biases)):
                pass

            for i in range(len(self.weights)):
                pass

            for i in range(len(self.avg_neuron_activations)):
                pass



        def _write__seq(self, io=None):
            super(TrainingArtifact.LayerState, self)._write__seq(io)
            self._io.write_u1(self.activation_function.value)
            self._io.write_u2le(self.num_biases)
            self._io.write_u4le(self.num_weights)
            for i in range(len(self.biases)):
                pass
                self._io.write_u1(self.biases[i])

            for i in range(len(self.weights)):
                pass
                self._io.write_u1(self.weights[i])

            for i in range(len(self.avg_neuron_activations)):
                pass
                self._io.write_f4le(self.avg_neuron_activations[i])



        def _check(self):
            pass
            if (len(self.biases) != self.num_biases):
                raise kaitaistruct.ConsistencyError(u"biases", len(self.biases), self.num_biases)
            for i in range(len(self.biases)):
                pass

            if (len(self.weights) != self.num_weights):
                raise kaitaistruct.ConsistencyError(u"weights", len(self.weights), self.num_weights)
            for i in range(len(self.weights)):
                pass

            if (len(self.avg_neuron_activations) != self.num_biases):
                raise kaitaistruct.ConsistencyError(u"avg_neuron_activations", len(self.avg_neuron_activations), self.num_biases)
            for i in range(len(self.avg_neuron_activations)):
                pass



    class TrainingStep(ReadWriteKaitaiStruct):
        def __init__(self, _io=None, _parent=None, _root=None):
            self._io = _io
            self._parent = _parent
            self._root = _root

        def _read(self):
            self.epoch_index = self._io.read_u2le()
            self.input_index = self._io.read_u2le()
            self.predicted_index = self._io.read_u2le()
            self.layer_states = []
            for i in range(self._root.header.num_layers):
                _t_layer_states = TrainingArtifact.LayerState(self._io, self, self._root)
                _t_layer_states._read()
                self.layer_states.append(_t_layer_states)

            self.avg_loss = self._io.read_f4le()


        def _fetch_instances(self):
            pass
            for i in range(len(self.layer_states)):
                pass
                self.layer_states[i]._fetch_instances()



        def _write__seq(self, io=None):
            super(TrainingArtifact.TrainingStep, self)._write__seq(io)
            self._io.write_u2le(self.epoch_index)
            self._io.write_u2le(self.input_index)
            self._io.write_u2le(self.predicted_index)
            for i in range(len(self.layer_states)):
                pass
                self.layer_states[i]._write__seq(self._io)

            self._io.write_f4le(self.avg_loss)


        def _check(self):
            pass
            if (len(self.layer_states) != self._root.header.num_layers):
                raise kaitaistruct.ConsistencyError(u"layer_states", len(self.layer_states), self._root.header.num_layers)
            for i in range(len(self.layer_states)):
                pass
                if self.layer_states[i]._root != self._root:
                    raise kaitaistruct.ConsistencyError(u"layer_states", self.layer_states[i]._root, self._root)
                if self.layer_states[i]._parent != self:
                    raise kaitaistruct.ConsistencyError(u"layer_states", self.layer_states[i]._parent, self)




