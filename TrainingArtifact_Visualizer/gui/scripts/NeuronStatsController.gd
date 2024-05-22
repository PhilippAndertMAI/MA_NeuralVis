extends TabContainer

@export var stats_tabs_current: NeuronStatsTab
@export var stats_tabs_final: NeuronStatsTabFinal

var _training_stepper: Node
var _selection_controller: Node
var _networkManager: Node

func _ready():
	_training_stepper = get_node("/root/VisualizationManager/TrainingStepper")
	_selection_controller = get_node("/root/VisualizationManager/PlayerController/SelectionController")
	_networkManager = get_node("/root/VisualizationManager/NetworkManager")
	
	_init_stats()
	
	_selection_controller.connect("selected_neuron", _handle_selected_neuron)
	_training_stepper.connect("stepped", _handle_stepped)
	_training_stepper.connect("skipped", _handle_skipped)


func _handle_selected_neuron(neuron: Node):
	_update_current_stats()
	
	var final_trainingstep_index: int = _training_stepper.call("GetNumStoredTrainingSteps") - 1
	stats_tabs_final.update_stats(neuron, final_trainingstep_index)

func _handle_stepped(_current_substep: int):
	_update_current_stats()
	
func _handle_skipped(_idx1: int, _idx2: int, _idx3: int):
	_update_current_stats()
	
func _init_stats():
	var selected_neuron: Node3D = _selection_controller.get_selected_neuron()
	if selected_neuron == null:
		return
	# i.e. InputNeuron does not have activations
	var neuron_types = ["LearnableNeuron", "OutputNeuron"]
	if not selected_neuron.call("GetClassName") in neuron_types:
		return
	
	stats_tabs_current.update_stats(selected_neuron)
	
	var final_trainingstep_index: int = _training_stepper.call("GetNumStoredTrainingSteps") - 1
	stats_tabs_final.update_stats(selected_neuron, final_trainingstep_index)

func _update_current_stats():
	var selected_neuron: Node3D = _selection_controller.get_selected_neuron()
	if selected_neuron == null:
		return
	# i.e. InputNeuron does not have activations
	var neuron_types = ["LearnableNeuron", "OutputNeuron"]
	if not selected_neuron.call("GetClassName") in neuron_types:
		return
	
	stats_tabs_current.update_stats(selected_neuron)
	
