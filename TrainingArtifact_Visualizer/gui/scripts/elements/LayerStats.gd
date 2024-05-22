extends VBoxContainer
class_name LayerStats

@export var header: Label
@export var dimensions_display: LabelledDataDisplay
@export var act_fn_display: LabelledDataDisplay


func _ready():
	dimensions_display.override_format("%s")
	act_fn_display.override_format("%s")

func initialize(network_manager: Node, layer_index: int):
	var num_layers = network_manager.call("GetNumLayers")

	var input_neurons: int = 0 if layer_index == 0 else network_manager.call("GetNeuronsPerLayer", layer_index)
	var output_neurons: int = 0 if layer_index == num_layers - 1 else network_manager.call("GetNeuronsPerLayer", layer_index + 1)
	
	var layer_type: String = "Hidden"
	if input_neurons == 0:
		layer_type = "Input"
	elif output_neurons == 0:
		layer_type = "Output"
	var label: String = "Layer " + str(layer_index + 1) + " (" + layer_type + ")"
	
	var dimensions: String = str(input_neurons)
	dimensions += " x " + str(output_neurons)
	
	var act_fn: String = "n/a" if layer_index == 0 else network_manager.call("GetActivationFunctionName", layer_index - 1)
	
	header.text = label
	dimensions_display.set_data(dimensions)
	act_fn_display.set_data(act_fn)
