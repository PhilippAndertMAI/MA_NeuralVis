meta:
  id: training_artifact
  title: Training Artifact (neural network training process data)
  file-extension: tart
  tags:
    - ml
    - 3d
  endian: le
doc: |
  "Artifact" of the backpropagation process of a neural network representing the slices of its entire training process.
  To be used for visualizing said process in i.e. a 3D environment.
seq:
  - id: header
    type: main_header
  - id: initial_states
    type: layer_state
    repeat: expr
    repeat-expr: _root.header.num_layers
  - id: metrics
    type: input_metrics
  - id: training_steps
    type: training_step
    repeat: eos

enums:
  activation_functions:
    0: relu
    1: softmax
    2: tanh
  loss_functions:
    0: mse
    1: cross_e

types:
  main_header:
    seq:
      - id: num_epochs
        doc: |
          Number of total training epochs used for training the network.
        type: u2
      - id: num_epoch_indices
        doc: |
          Number of epochs stored in the training artifact.
        type: u2
      - id: epoch_indices
        doc: |
          Indices of the stored epochs, as not all epochs may be stored.
        type: u2
        repeat: expr
        repeat-expr: num_epoch_indices
      - id: num_samples
        doc: |
          Number of training samples PER EPOCH used for training the network.
        type: u2
      - id: num_features
        doc: |
          Number of features per sample.
        type: u2
      - id: num_layers
        doc: |
          Number of layers.
        type: u1
      - id: learning_rate
        doc: |
          Learning rate.
        type: f4
      - id: loss_function
        type: u1
        enum: loss_functions

  input_metrics:
    seq:
      - id: avg_feature_values
        doc: |
          Average value of each feature calculated until the last training iteration. |
          Stored in higher float precision in order to be more precise in selecting relevant features.
        type: f4
        repeat: expr
        repeat-expr: _root.header.num_features
      - id: num_correct_predictions
        doc: |
          Number of correct predictions total at each training step.
        type: u2
        repeat: expr
        repeat-expr: _root.header.num_epoch_indices * _root.header.num_samples

  layer_state:
    seq:
      - id: activation_function
        doc: |
          Activation function used in this layer.
        type: u1
        enum: activation_functions
      - id: num_biases
        doc: |
          Number of biases, which equals the number of neurons in the layer.
        type: u2
      - id: num_weights
        doc: |
          Number of weights in this layer (number of inputs * number of neurons/outputs).
        type: u4
      - id: biases
        doc: |
          Biases for this layer with values in [0, 255]
        repeat: expr
        repeat-expr: num_biases
        type: u1
      - id: weights
        doc: |
          Weights for this layer with values in [0, 255]
        type: u1
        repeat: expr
        repeat-expr: num_weights
      - id: avg_neuron_activations
        doc: |
          Average value of activations per neuron in this layer.
        type: f4
        repeat: expr
        repeat-expr: num_biases

  training_step:
    seq:
      - id: epoch_index
        doc: |
          Index of the epoch this training step belongs to.
        type: u2
      - id: input_index
        doc: |
          Index referencing the sample used
        type: u2
      - id: predicted_index
        doc: |
          Target index that was predicted with this input.
        type: u2
      - id: layer_states
        doc: |
          Final state of layer after a full backpropagation.
        type: layer_state
        repeat: expr
        repeat-expr: _root.header.num_layers
      - id: avg_loss
        doc: |
          Average loss calculcated at each sample in the training.
        type: f4