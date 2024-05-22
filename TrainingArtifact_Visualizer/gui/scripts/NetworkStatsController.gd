extends TabContainer

@export var stats_tabs_current: NetworkStatsTab
@export var stats_tabs_final: NetworkStatsTabFinal
@export var stats_tab_layers: NetworkStatsLayersTab

var _training_stepper: Node
var _selection_controller: Node
var _network_manager: Node
var _ui_controller: Node

var _current_substep_index: int = 0
var _current_stored_trainingstep_index: int = 0
var _num_substeps: int

func initialize(ui_controller: Node):
	_training_stepper = get_node("/root/VisualizationManager/TrainingStepper")
	_selection_controller = get_node("/root/VisualizationManager/PlayerController/SelectionController")
	_network_manager = get_node("/root/VisualizationManager/NetworkManager")
	_ui_controller = ui_controller
	
	_num_substeps = _training_stepper.call("GetNumSubSteps")
	
	_init_stats()
	
	_training_stepper.connect("stepped", _handle_stepped)
	_training_stepper.connect("skipped", _handle_skipped)
	_ui_controller.connect("fast_forward_btn_clicked", _handle_fast_forward_clicked)
	_ui_controller.connect("epoch_clicked", _handle_fast_forward_clicked)

func _handle_stepped(current_substep: int):
	_current_substep_index = current_substep
	if _current_substep_index == float(_num_substeps) / 2 - 1:
		_update_current_stats()
	
func _handle_skipped(_idx1: int, stored_trainingstep_index: int, _idx3: int):
	_current_stored_trainingstep_index = stored_trainingstep_index
	
func _handle_fast_forward_clicked():
	_current_substep_index = 0
	_update_current_stats()
	
func _init_stats():
	stats_tabs_current.initialize(_training_stepper)
	stats_tabs_final.initialize(_training_stepper)
	
	stats_tab_layers.initialize(_network_manager, _training_stepper)
	
	stats_tabs_current.update_stats(_current_substep_index, _current_stored_trainingstep_index)
	var final_trainingstep_index: int = _training_stepper.call("GetNumStoredTrainingSteps") - 1
	stats_tabs_final.set_stats(final_trainingstep_index)

func _update_current_stats():	
	stats_tabs_current.update_stats(_current_substep_index, _current_stored_trainingstep_index)
	
