extends MarginContainer
class_name NetworkStatsLayersTab


@export var loss_fn_display: LabelledDataDisplay
@export var learning_rate_display: LabelledDataDisplay
@export var epochs_display: LabelledDataDisplay
@export var num_samples_display: LabelledDataDisplay

@export var layers_container: VBoxContainer

var _network_manager: Node
var _training_stepper: Node


func initialize(network_manager: Node, training_stepper: Node):
	_network_manager = network_manager
	_training_stepper = training_stepper
	
	loss_fn_display.override_format("%s")
	epochs_display.override_format("%d")
	num_samples_display.override_format("%d")
	
	init_global_stats()
	
	clear_dummy_layers()
	init_layers()
	
func init_global_stats():
	var loss_fn_name: String = _network_manager.call("GetLossFunctionName")
	var learning_rate: float = _network_manager.call("GetLearningRate")
	var num_epochs: int = _training_stepper.call("GetNumEpochs")
	var num_samples: int = _training_stepper.call("GetNumSamples")

	loss_fn_display.set_data(loss_fn_name)
	learning_rate_display.set_data(learning_rate)
	epochs_display.set_data(num_epochs)
	num_samples_display.set_data(num_samples)

func clear_dummy_layers():
	for child in layers_container.get_children():
		layers_container.remove_child(child)
		child.queue_free()
		
func init_layers():
	var layer_stats_scene: PackedScene = preload("res://gui/elements/LayerStats.tscn")
	var num_layers = _network_manager.call("GetNumLayers")
	for layer_index in range(0, num_layers):
		var layer_stats: LayerStats = layer_stats_scene.instantiate()
		layers_container.add_child(layer_stats)
		layer_stats.initialize(_network_manager, layer_index)
