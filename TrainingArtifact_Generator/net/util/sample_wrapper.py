import numpy as np
from dataclasses import dataclass

@dataclass
class SampleWrapper:
    """Wrapper class around the input data into the neural network.

    Attributes:
        data: Numpy array representation of the data
        index_abs: Absolute index of the data in relation to the original dataset
    """
    data: np.ndarray = None
    index_abs: int = -1
