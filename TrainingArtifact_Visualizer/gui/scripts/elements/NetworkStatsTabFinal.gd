extends MarginContainer
class_name NetworkStatsTabFinal

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

func set_stats(final_trainingstep_index: int):
	_set_data(final_trainingstep_index)

func _set_data(final_trainingstep_index: int):	
	var loss = _training_stepper.call("GetAvgLoss", final_trainingstep_index)
	var num_correct = _training_stepper.call("GetNumCorrectPredictions", final_trainingstep_index)
	var samples_total = _training_stepper.call("GetNumActualTotalTrainingSteps")
	
	loss_display.set_data(loss)
	samples_display.set_data(samples_total)
	correct_display.set_data(num_correct)
	false_display.set_data(samples_total - num_correct)
