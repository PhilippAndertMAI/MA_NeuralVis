extends MarginContainer
class_name NeuronStatsTab


@export var neuron_label: Label

@export var avg_activations_display: LabelledDataDisplay
@export var bias_display: LabelledDataDisplay
@export var avg_weight_display: LabelledDataDisplay

@export var latest_raw_input_display: LabelledDataDisplay
@export var latest_weighted_input_display: LabelledDataDisplay
@export var latest_activation_display: LabelledDataDisplay

func update_stats(neuron: Node3D):
	# i.e. InputNeuron does not have activations
	var neuron_types = ["LearnableNeuron", "OutputNeuron"]
	if not neuron.call("GetClassName") in neuron_types:
		return
	
	_update_header(neuron)
	_update_data(neuron)

func _update_header(neuron: Node):
	var layer_index: int = neuron.call("GetLayerIndex")
	var neuron_index: int = neuron.call("GetNeuronIndex")
	if neuron.call("GetClassName") == "OutputNeuron":
		neuron_label.text = "Neuron %03d @ Output Layer" % (neuron_index + 1)
	else:
		neuron_label.text = "Neuron %03d @ Layer %02d" % [neuron_index + 1, layer_index + 1]

func _update_data(neuron: Node):
	var avg_activation: float = neuron.call("GetAvgActivation")
	var current_bias: float = neuron.call("GetBias") as float / 255
	var current_avg_weight: float = neuron.call("GetAverageWeightAsFloat")
	
	var latest_raw_input = neuron.call("GetLatestInput")
	var latest_weighted_input = neuron.call("GetLatestWeightedInput")
	var latest_activation = neuron.call("GetLatestActivation")
	
	avg_activations_display.set_data(avg_activation)
	bias_display.set_data(current_bias)
	avg_weight_display.set_data(current_avg_weight)
	
	latest_raw_input_display.set_data(latest_raw_input)
	latest_weighted_input_display.set_data(latest_weighted_input)
	latest_activation_display.set_data((latest_activation as float) / 255)
