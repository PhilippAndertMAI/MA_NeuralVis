extends MarginContainer
class_name NetworkStatsTab

@export var loss_display: LabelledDataDisplay
@export var samples_display: LabelledDataDisplay
@export var correct_display: LabelledDataDisplay
@export var false_display: LabelledDataDisplay

var _training_stepper: Node

var _num_substeps: int

func initialize(training_stepper):
	_training_stepper = training_stepper
	
	_num_substeps = _training_stepper.call("GetNumSubSteps")
	
	samples_display.override_format("%d")
	correct_display.override_format("%d")
	false_display.override_format("%d")

func update_stats(current_substep_index: int, stored_trainingstep_index: int):
	_update_data(current_substep_index, stored_trainingstep_index)

func _update_data(current_substep_index: int, stored_trainingstep_index: int):
	if stored_trainingstep_index == 0 and current_substep_index < float(_num_substeps) / 2 - 1:
		loss_display.set_data_with_format("%s", "n/a")
		samples_display.set_data_with_format("%s", "n/a")
		correct_display.set_data_with_format("%s", "n/a")
		false_display.set_data_with_format("%s", "n/a")
		return
	
	var loss = _training_stepper.call("GetCurrentAvgLoss")
	var num_correct = _training_stepper.call("GetCurrentNumCorrectPredictions")
	
	loss_display.set_data(loss)
	samples_display.set_data(stored_trainingstep_index + 1)
	correct_display.set_data(num_correct)
	false_display.set_data(stored_trainingstep_index + 1 - num_correct)
