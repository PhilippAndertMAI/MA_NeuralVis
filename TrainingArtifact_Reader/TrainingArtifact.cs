// This is a generated file! Please edit source .ksy file and use kaitai-struct-compiler to rebuild

using System.Collections.Generic;

namespace Kaitai
{

    /// <summary>
    /// &quot;Artifact&quot; of the backpropagation process of a neural network representing the slices of its entire training process.
    /// To be used for visualizing said process in i.e. a 3D environment.
    /// </summary>
    public partial class TrainingArtifact : KaitaiStruct
    {
        public static TrainingArtifact FromFile(string fileName)
        {
            return new TrainingArtifact(new KaitaiStream(fileName));
        }


        public enum ActivationFunctions
        {
            Relu = 0,
            Softmax = 1,
            Tanh = 2,
        }

        public enum LossFunctions
        {
            Mse = 0,
            CrossE = 1,
        }
        public TrainingArtifact(KaitaiStream p__io, KaitaiStruct p__parent = null, TrainingArtifact p__root = null) : base(p__io)
        {
            m_parent = p__parent;
            m_root = p__root ?? this;
            _read();
        }
        private void _read()
        {
            _header = new MainHeader(m_io, this, m_root);
            _initialStates = new List<LayerState>();
            for (var i = 0; i < M_Root.Header.NumLayers; i++)
            {
                _initialStates.Add(new LayerState(m_io, this, m_root));
            }
            _metrics = new InputMetrics(m_io, this, m_root);
            _trainingSteps = new List<TrainingStep>();
            {
                var i = 0;
                while (!m_io.IsEof) {
                    _trainingSteps.Add(new TrainingStep(m_io, this, m_root));
                    i++;
                }
            }
        }
        public partial class MainHeader : KaitaiStruct
        {
            public static MainHeader FromFile(string fileName)
            {
                return new MainHeader(new KaitaiStream(fileName));
            }

            public MainHeader(KaitaiStream p__io, TrainingArtifact p__parent = null, TrainingArtifact p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _numEpochs = m_io.ReadU2le();
                _numEpochIndices = m_io.ReadU2le();
                _epochIndices = new List<ushort>();
                for (var i = 0; i < NumEpochIndices; i++)
                {
                    _epochIndices.Add(m_io.ReadU2le());
                }
                _numSamples = m_io.ReadU2le();
                _numFeatures = m_io.ReadU2le();
                _numLayers = m_io.ReadU1();
                _learningRate = m_io.ReadF4le();
                _lossFunction = ((TrainingArtifact.LossFunctions) m_io.ReadU1());
            }
            private ushort _numEpochs;
            private ushort _numEpochIndices;
            private List<ushort> _epochIndices;
            private ushort _numSamples;
            private ushort _numFeatures;
            private byte _numLayers;
            private float _learningRate;
            private LossFunctions _lossFunction;
            private TrainingArtifact m_root;
            private TrainingArtifact m_parent;

            /// <summary>
            /// Number of total training epochs used for training the network.
            /// </summary>
            public ushort NumEpochs { get { return _numEpochs; } }

            /// <summary>
            /// Number of epochs stored in the training artifact.
            /// </summary>
            public ushort NumEpochIndices { get { return _numEpochIndices; } }

            /// <summary>
            /// Indices of the stored epochs, as not all epochs may be stored.
            /// </summary>
            public List<ushort> EpochIndices { get { return _epochIndices; } }

            /// <summary>
            /// Number of training samples PER EPOCH used for training the network.
            /// </summary>
            public ushort NumSamples { get { return _numSamples; } }

            /// <summary>
            /// Number of features per sample.
            /// </summary>
            public ushort NumFeatures { get { return _numFeatures; } }

            /// <summary>
            /// Number of layers.
            /// </summary>
            public byte NumLayers { get { return _numLayers; } }

            /// <summary>
            /// Learning rate.
            /// </summary>
            public float LearningRate { get { return _learningRate; } }
            public LossFunctions LossFunction { get { return _lossFunction; } }
            public TrainingArtifact M_Root { get { return m_root; } }
            public TrainingArtifact M_Parent { get { return m_parent; } }
        }
        public partial class InputMetrics : KaitaiStruct
        {
            public static InputMetrics FromFile(string fileName)
            {
                return new InputMetrics(new KaitaiStream(fileName));
            }

            public InputMetrics(KaitaiStream p__io, TrainingArtifact p__parent = null, TrainingArtifact p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _avgFeatureValues = new List<float>();
                for (var i = 0; i < M_Root.Header.NumFeatures; i++)
                {
                    _avgFeatureValues.Add(m_io.ReadF4le());
                }
                _numCorrectPredictions = new List<ushort>();
                for (var i = 0; i < (M_Root.Header.NumEpochIndices * M_Root.Header.NumSamples); i++)
                {
                    _numCorrectPredictions.Add(m_io.ReadU2le());
                }
            }
            private List<float> _avgFeatureValues;
            private List<ushort> _numCorrectPredictions;
            private TrainingArtifact m_root;
            private TrainingArtifact m_parent;

            /// <summary>
            /// Average value of each feature calculated until the last training iteration. |
            /// Stored in higher float precision in order to be more precise in selecting relevant features.
            /// </summary>
            public List<float> AvgFeatureValues { get { return _avgFeatureValues; } }

            /// <summary>
            /// Number of correct predictions total at each training step.
            /// </summary>
            public List<ushort> NumCorrectPredictions { get { return _numCorrectPredictions; } }
            public TrainingArtifact M_Root { get { return m_root; } }
            public TrainingArtifact M_Parent { get { return m_parent; } }
        }
        public partial class LayerState : KaitaiStruct
        {
            public static LayerState FromFile(string fileName)
            {
                return new LayerState(new KaitaiStream(fileName));
            }

            public LayerState(KaitaiStream p__io, KaitaiStruct p__parent = null, TrainingArtifact p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _activationFunction = ((TrainingArtifact.ActivationFunctions) m_io.ReadU1());
                _numBiases = m_io.ReadU2le();
                _numWeights = m_io.ReadU4le();
                _biases = new List<byte>();
                for (var i = 0; i < NumBiases; i++)
                {
                    _biases.Add(m_io.ReadU1());
                }
                _weights = new List<byte>();
                for (var i = 0; i < NumWeights; i++)
                {
                    _weights.Add(m_io.ReadU1());
                }
                _avgNeuronActivations = new List<float>();
                for (var i = 0; i < NumBiases; i++)
                {
                    _avgNeuronActivations.Add(m_io.ReadF4le());
                }
            }
            private ActivationFunctions _activationFunction;
            private ushort _numBiases;
            private uint _numWeights;
            private List<byte> _biases;
            private List<byte> _weights;
            private List<float> _avgNeuronActivations;
            private TrainingArtifact m_root;
            private KaitaiStruct m_parent;

            /// <summary>
            /// Activation function used in this layer.
            /// </summary>
            public ActivationFunctions ActivationFunction { get { return _activationFunction; } }

            /// <summary>
            /// Number of biases, which equals the number of neurons in the layer.
            /// </summary>
            public ushort NumBiases { get { return _numBiases; } }

            /// <summary>
            /// Number of weights in this layer (number of inputs * number of neurons/outputs).
            /// </summary>
            public uint NumWeights { get { return _numWeights; } }

            /// <summary>
            /// Biases for this layer with values in [0, 255]
            /// </summary>
            public List<byte> Biases { get { return _biases; } }

            /// <summary>
            /// Weights for this layer with values in [0, 255]
            /// </summary>
            public List<byte> Weights { get { return _weights; } }

            /// <summary>
            /// Average value of activations per neuron in this layer.
            /// </summary>
            public List<float> AvgNeuronActivations { get { return _avgNeuronActivations; } }
            public TrainingArtifact M_Root { get { return m_root; } }
            public KaitaiStruct M_Parent { get { return m_parent; } }
        }
        public partial class TrainingStep : KaitaiStruct
        {
            public static TrainingStep FromFile(string fileName)
            {
                return new TrainingStep(new KaitaiStream(fileName));
            }

            public TrainingStep(KaitaiStream p__io, TrainingArtifact p__parent = null, TrainingArtifact p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _epochIndex = m_io.ReadU2le();
                _inputIndex = m_io.ReadU2le();
                _predictedIndex = m_io.ReadU2le();
                _layerStates = new List<LayerState>();
                for (var i = 0; i < M_Root.Header.NumLayers; i++)
                {
                    _layerStates.Add(new LayerState(m_io, this, m_root));
                }
                _avgLoss = m_io.ReadF4le();
            }
            private ushort _epochIndex;
            private ushort _inputIndex;
            private ushort _predictedIndex;
            private List<LayerState> _layerStates;
            private float _avgLoss;
            private TrainingArtifact m_root;
            private TrainingArtifact m_parent;

            /// <summary>
            /// Index of the epoch this training step belongs to.
            /// </summary>
            public ushort EpochIndex { get { return _epochIndex; } }

            /// <summary>
            /// Index referencing the sample used
            /// </summary>
            public ushort InputIndex { get { return _inputIndex; } }

            /// <summary>
            /// Target index that was predicted with this input.
            /// </summary>
            public ushort PredictedIndex { get { return _predictedIndex; } }

            /// <summary>
            /// Final state of layer after a full backpropagation.
            /// </summary>
            public List<LayerState> LayerStates { get { return _layerStates; } }

            /// <summary>
            /// Average loss calculcated at each sample in the training.
            /// </summary>
            public float AvgLoss { get { return _avgLoss; } }
            public TrainingArtifact M_Root { get { return m_root; } }
            public TrainingArtifact M_Parent { get { return m_parent; } }
        }
        private MainHeader _header;
        private List<LayerState> _initialStates;
        private InputMetrics _metrics;
        private List<TrainingStep> _trainingSteps;
        private TrainingArtifact m_root;
        private KaitaiStruct m_parent;
        public MainHeader Header { get { return _header; } }
        public List<LayerState> InitialStates { get { return _initialStates; } }
        public InputMetrics Metrics { get { return _metrics; } }
        public List<TrainingStep> TrainingSteps { get { return _trainingSteps; } }
        public TrainingArtifact M_Root { get { return m_root; } }
        public KaitaiStruct M_Parent { get { return m_parent; } }
    }
}
