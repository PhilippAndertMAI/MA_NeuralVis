extends Button


@export var offset = 0;


var training_stepper: Node

var set_input_templ: String
var forward_prop_templ: String
var backward_prop_templ: String

var num_substeps
var num_training_steps: int


func _ready():
	training_stepper = get_node("/root/VisualizationManager/TrainingStepper")
	
	set_input_templ = "Set Input"
	forward_prop_templ = "Forward Prop %s/%s"
	backward_prop_templ = "Backward Prop %s/%s"
	
	num_substeps = training_stepper.call("GetNumSubSteps")
	num_training_steps = training_stepper.call("GetNumStoredTrainingSteps")
	
	change_text(-2)
	training_stepper.connect("stepped", change_text)
	
	
func change_text(current_substep: int):	
	match offset:
		-1: # display prev step
			change_text_prev(current_substep)
		0: # display current step
			change_text_current(current_substep)
		1: # display next step
			change_text_next(current_substep)
		
func change_text_current(current_substep: int):
	# check if set input
	if current_substep == -1:
		self.text = set_input_templ
	else:
		if current_substep < 0:
			self.text = "/"
		# check if forward prop
		elif current_substep < num_substeps / 2:
			# get forward prop index
			var forward_index: int = current_substep
			self.text = forward_prop_templ % [forward_index + 1, num_substeps / 2]
		else:
			# get backprop index
			var backprop_index: int = current_substep - num_substeps / 2
			self.text = backward_prop_templ % [backprop_index + 1, num_substeps / 2]
			
func change_text_prev(current_substep: int):
	var prev_step = current_substep + offset
	if prev_step < -1:
		prev_step = num_substeps - 1
	
	var current_trainingstep = training_stepper.call("GetCurrentTrainingStep")
	
	# check if set input
	if prev_step == -1:
		self.text = set_input_templ
	# check if forward prop
	elif prev_step < num_substeps / 2:
		# get forward prop index
		var forward_index: int = prev_step
		self.text = forward_prop_templ % [forward_index + 1, num_substeps / 2]
	else:
		# check for underflow
		if prev_step == num_substeps - 1 && current_trainingstep == 0:
			self.text = "/"
		else:
			# get backprop index
			var backprop_index: int = prev_step - num_substeps / 2
			self.text = backward_prop_templ % [backprop_index + 1, num_substeps / 2]


func change_text_next(current_substep: int):
	var next_step = current_substep + offset
	if next_step > num_substeps - 1:
		next_step = -1
		
	# check if set input
	if next_step == -1:
		if training_stepper.call("GetCurrentTrainingStep") == num_training_steps - 1:
			self.text = "/"
		else:
			self.text = set_input_templ
	# check if forward prop
	elif next_step < num_substeps / 2:
		# get forward prop index
		var forward_index: int = next_step
		self.text = forward_prop_templ % [forward_index + 1, num_substeps / 2]
	else:
		# get backprop index
		var backprop_index: int = next_step - num_substeps / 2
		self.text = backward_prop_templ % [backprop_index + 1, num_substeps / 2]
		
func _pressed():
	if offset != 0:
		release_focus()
