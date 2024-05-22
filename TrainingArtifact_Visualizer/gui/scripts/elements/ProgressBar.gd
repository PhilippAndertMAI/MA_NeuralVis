extends ProgressBar

var training_stepper: Node

var label: RichTextLabel

var max_display_value: int

func _ready():
	training_stepper = get_node("/root/VisualizationManager/TrainingStepper")
	label = get_node("./MarginContainerLabel/RichTextLabel")
	
	var n_samples: int = training_stepper.call("GetNumSamples")
	var n_epochs: int = len(training_stepper.call("GetEpochIndices"))
	max_display_value = training_stepper.call("GetNumActualTotalTrainingSteps")
	
	self.max_value = n_epochs * n_samples
	
	change_progress(0, 0, 0)
	training_stepper.connect("skipped", change_progress)


func change_progress(current_trainingstep: int, stored_trainingstep: int, _trainingstep_epoch_index: int):
	self.value = current_trainingstep
	label.text = "[center]%s/%s[/center]" % [stored_trainingstep, max_display_value]
