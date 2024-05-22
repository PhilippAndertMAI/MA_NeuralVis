import numpy
from keras.datasets import mnist
from sklearn.model_selection import train_test_split

from net.my_network import MyNetwork
from net.layers.dense_layer import DenseLayer
from sklearn import datasets
from net.functions.activations import ActivationType as ActTypes
from net.functions.losses import LossType

import numpy as np
from keras.utils import np_utils, normalize
import logging

from net.util.sample_wrapper import SampleWrapper

logging.basicConfig(level=logging.NOTSET)


"""
# load data
iris = datasets.load_iris()

X_raw, y_raw = iris.data, iris.target
X_raw = normalize(X_raw, axis=0)
X_raw = np.array([arr.reshape(1, -1) for arr in X_raw])
y_raw = np_utils.to_categorical(y_raw, dtype=float)

X_raw.dtype = float
y_raw.dtype = float

np.savez_compressed("training.data.npz", X=X_raw)
np.savez_compressed("training.target.npz", y=y_raw)
"""


# load MNIST from server
# TODO: get validation set and also validate at the end
(X_train_raw, y_train_raw), (X_test_raw, y_test_raw) = mnist.load_data()
# take smaller size (1200 samples = 2%)
X_train_raw = X_train_raw[:150]
y_train_raw = y_train_raw[:150]
X_test_raw = X_test_raw[:150]
y_test_raw = y_test_raw[:150]


# reshape, normalize, encode y
# train
X_train_raw = X_train_raw.reshape(X_train_raw.shape[0], 1, 28*28)
X_train_raw = X_train_raw.astype('float64')
X_train_raw = X_train_raw / 255.0
y_train_raw = np_utils.to_categorical(y_train_raw)
# test
X_test_raw = X_test_raw.reshape(X_test_raw.shape[0], 1, 28*28)
X_test_raw = X_test_raw.astype('float64')
X_test_raw = X_test_raw / 255.0
y_test_raw = np_utils.to_categorical(y_test_raw)

# prepare for saving
# setting to other types of float may mess with the sample size (i.e. halve it)
X_train_raw.dtype = np.float_
y_train_raw.dtype = np.single
X_test_raw.dtype = np.float_
y_test_raw.dtype = np.single

# save training data
np.savez("training.data.npz", X=X_train_raw)
np.savez("training.target.npz", y=y_train_raw)


x_train = [(lambda i: SampleWrapper(X_train_raw[i], i) )(i) for i in range(X_train_raw.shape[0])]
y_train = [(lambda i: SampleWrapper(y_train_raw[i], i) )(i) for i in range(y_train_raw.shape[0])]
x_test = [(lambda i: SampleWrapper(X_train_raw[i], i) )(i) for i in range(X_test_raw.shape[0])]
y_test = [(lambda i: SampleWrapper(y_train_raw[i], i) )(i) for i in range(y_test_raw.shape[0])]

# no need to extract test data from train set if loaded beforehand from server
#x_train, x_test, y_train, y_test = train_test_split(X, y, test_size=0.2)


# network
num_epochs = 33
epoch_indices = [0, 8, 16, 24, 32]
net = MyNetwork(record_artifact=True, artifact_path="artifact.tart", epoch_indices=epoch_indices)
net.add(DenseLayer(28*28, 100, ActTypes.TANH))
net.add(DenseLayer(100, 50, ActTypes.TANH))
net.add(DenseLayer(50, 10, ActTypes.TANH))


# train
net.use(LossType.MSE)
net.fit(x_train, y_train, epochs=num_epochs, learning_rate=0.1)


# test
out = net.predict(x_test[0:3])
print("\n")
print("predicted values : ")
print(out, end="\n")
print("true values : ")
print(y_test[0].data, y_test[1].data, y_test[2].data)

# write artifact
net.write_artifact()
