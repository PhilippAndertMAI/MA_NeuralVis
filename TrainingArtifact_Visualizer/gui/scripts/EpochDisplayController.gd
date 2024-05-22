extends Node

@export var progress_bar: ProgressBar

@export var epoch_scene: PackedScene

@export var first_style: StyleBoxFlat
@export var normal_style: StyleBoxFlat
@export var last_style: StyleBoxFlat
@export var current_style: StyleBoxFlat

var training_stepper: Node
var gui: Node

var epoch_indices: PackedInt32Array
var n_samples: int


func _ready():
	training_stepper = get_node("/root/VisualizationManager/TrainingStepper")
	gui = get_node("/root/VisualizationManager/GUI")
	
	epoch_indices = training_stepper.call("GetEpochIndices")
	n_samples = training_stepper.call("GetNumSamples")
	
	instantiate_epochs()
	
	training_stepper.connect("skipped", set_styles)

func instantiate_epochs():
	# determine width of each epoch
	var progress_bar_width: float = (progress_bar.get_parent() as MarginContainer).custom_minimum_size.x
	var epoch_width: float = progress_bar_width / len(epoch_indices)
	
	var epoch_buttons = []
	
	for i in range(epoch_indices.size()):
		var epoch: Button = epoch_scene.instantiate()
		epoch.custom_minimum_size.x = epoch_width
		epoch_buttons.append(epoch)
		add_child(epoch)
		
		var epoch_index: int = epoch_indices[i]
		var label: Label = epoch.get_node("./Label") as Label
		label.text = "Epoch %s" % (epoch_index + 1)
		
		if i == 0:
			set_stylebox(label, current_style)
		elif i == len(epoch_indices) - 1:
			set_stylebox(label, last_style)
		else:
			set_stylebox(label, normal_style)
		
	gui.call("SetEpochControls", epoch_buttons)
		
	
func set_styles(_current_trainingstep_index: int, _stored_trainingstep_index: int, trainingstep_epoch_index: int):
	var epochs = get_children()
	for i in range(epochs.size()):
		var epoch: Node = epochs[i]
		var label: Label = epoch.get_node("./Label")
		# check if epoch is the current one
		if epoch_indices[i] == trainingstep_epoch_index:
			set_stylebox(label, current_style)
			continue
		
		# set styles according to epoch pos
		if i == 0:
			set_stylebox(label, first_style)
		elif i == len(epoch_indices) - 1:
			set_stylebox(label, last_style)
		else:
			set_stylebox(label, normal_style)

func set_stylebox(control: Control, stylebox: StyleBox):
	if control.has_theme_stylebox_override("normal"):
		control.remove_theme_stylebox_override("normal")
	control.add_theme_stylebox_override("normal", stylebox)
