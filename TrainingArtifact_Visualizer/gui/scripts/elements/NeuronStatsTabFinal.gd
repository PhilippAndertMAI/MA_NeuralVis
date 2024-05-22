extends MarginContainer
class_name NeuronStatsTabFinal


@export var neuron_label: Label

@export var avg_activations_display: LabelledDataDisplay
@export var bias_display: LabelledDataDisplay
@export var avg_weight_display: LabelledDataDisplay


func update_stats(neuron: Node3D, final_trainingstep_index: int):
	# i.e. InputNeuron does not have activations
	var neuron_types = ["LearnableNeuron", "OutputNeuron"]
	if not neuron.call("GetClassName") in neuron_types:
		return
	
	_update_header(neuron)
	_update_data(neuron, final_trainingstep_index)

func _update_header(neuron: Node):
	var layer_index: int = neuron.call("GetLayerIndex")
	var neuron_index: int = neuron.call("GetNeuronIndex")
	if neuron.call("GetClassName") == "OutputNeuron":
		neuron_label.text = "Neuron %03d @ Output Layer" % (neuron_index + 1)
	else:
		neuron_label.text = "Neuron %03d @ Layer %02d" % [neuron_index + 1, layer_index + 1]


func _update_data(neuron: Node, final_trainingstep_index: int):
	var avg_activation: float = neuron.call("GetAvgActivation", final_trainingstep_index)
	var current_bias: float = neuron.call("GetBias", final_trainingstep_index) as float / 255
	var current_avg_weight: float = neuron.call("GetAverageWeightAsFloat", final_trainingstep_index)
	
	avg_activations_display.set_data(avg_activation)
	bias_display.set_data(current_bias)
	avg_weight_display.set_data(current_avg_weight)
	
