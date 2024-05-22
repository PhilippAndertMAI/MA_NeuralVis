extends Node3D


var selected_neuron: Node3D

var camera: Camera3D

@export var rotation_threshold_px = 8

@export_flags_3d_physics var neuron_collision_mask
@export_flags_3d_physics var dendrite_collision_mask

var delta_rotation: float = 0

var _selected_entities: Array[Node3D] = []

var _network_manager: Node
var _activity_map_manager: Node

var _ui_entered: bool = false
var _app_guide_visible: bool = false

func _ready():
	add_user_signal("selected_neuron", [
		{ "name": "neuron", "type": Node3D }
	])
	add_user_signal("selected_entities_changed")
	
	camera = get_viewport().get_camera_3d()
	
	get_node("/root/VisualizationManager/PlayerController/CamParent/OrbitCamera") \
		.connect("camera_rotated", set_delta_rotation_speed)
		
	_activity_map_manager = get_node("/root/VisualizationManager/ActivityMapManager")
		
func initialize(gui: Node):
	gui.connect("ui_entered_exited", func (val): _ui_entered = val)
	gui.connect("appguide_toggled_visibility", func (val): _app_guide_visible = val)

func _input(event):
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
			if delta_rotation >= rotation_threshold_px:
				Input.set_mouse_mode(Input.MOUSE_MODE_HIDDEN)
	if event is InputEventMouseButton:
		if event.is_action_released("left_click"):
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
			# check against amount that the camera was moved since last left click (/hold)
			if delta_rotation < rotation_threshold_px and \
				not _ui_entered and not _app_guide_visible:
				delta_rotation = 0
				check_collision(event)
			delta_rotation = 0
	elif event is InputEventKey:
		if event.is_action_pressed("focus_element"):
			if selected_neuron != null:
				# neuron not in focus
				camera.set_anchor_pos(selected_neuron.position)
				camera.reset_distance(8)
					
		if event.is_action_pressed("unfocus_reset", true):
			if _app_guide_visible:
				return
			# no neuron selected
			if selected_neuron == null:
				camera.set_anchor_pos(camera.center)
				camera.reset_distance()
			else:
				unfocus_neuron()
			
func set_delta_rotation_speed(rotation_speed: float):
	delta_rotation += rotation_speed
			
func check_collision(event):
	if _ui_entered:
		return
	if _activity_map_manager.call("GetIsVisible"):
		_activity_map_manager.call("ToggleVisibility")
	var from = camera.project_ray_origin(event.position)
	var to = from + camera.project_ray_normal(event.position) * 100
	
	var space_state: PhysicsDirectSpaceState3D = get_world_3d().direct_space_state
	
	var neuron_query = PhysicsRayQueryParameters3D.create(from, to)
	neuron_query.collide_with_areas = true
	neuron_query.collision_mask = neuron_collision_mask
	var neuron_result: Dictionary = space_state.intersect_ray(neuron_query)
	
	var dendrite_query = PhysicsRayQueryParameters3D.create(from, to)
	dendrite_query.collide_with_areas = true
	dendrite_query.collision_mask = dendrite_collision_mask
	var dendrite_result: Dictionary = space_state.intersect_ray(dendrite_query)
	
	# if nothing is currently selected, prefer neuron clicks over dendrite clicks (if overlapping)
	if _selected_entities.is_empty():
		if not neuron_result.is_empty():
			handle_collision_result(neuron_result)
		elif not dendrite_result.is_empty():
			handle_collision_result(dendrite_result)
		else:
			unfocus_neuron()
	else:
		# if anything is selected, prefer closest object (as per default)
		var neuron_dist = 0.0 if neuron_result.is_empty() \
			else from.distance_to(neuron_result.collider.position)
		var dendrite_dist = 0.0 if dendrite_result.is_empty() \
			else from.distance_to(dendrite_result.collider.position)
		if neuron_dist > dendrite_dist:
			handle_collision_result(neuron_result)
		elif dendrite_dist > neuron_dist:
			handle_collision_result(dendrite_result)
		else:
			unfocus_neuron()
		
	

func handle_collision_result(result):
	var collider_parent: Node3D = result.collider.get_parent()
	var neuron: Node3D
	if not collider_parent.name.contains('dendrite'):
		neuron = collider_parent
	else:
		var dendrite: Node3D = collider_parent
		var layer_idx = dendrite.call("GetLayerIndex")
		var neuron_idx = dendrite.call("GetEndNeuronIndex")
		if (_network_manager == null):
			_network_manager = get_node("/root/VisualizationManager/NetworkManager")
		neuron = _network_manager.call("GetNeuron", layer_idx, neuron_idx)
		
	if selected_neuron == null:
		selected_neuron = neuron
	elif selected_neuron == neuron and camera.get_anchor_pos() == selected_neuron.position:
		selected_neuron.call("SetSelected", false, 1.0, 0)
		clear_selected_entities()
		selected_neuron = null
	elif selected_neuron == neuron and camera.get_anchor_pos() != selected_neuron.position:
		# refocus neuron because camera isn't focussed on it
		camera.set_anchor_pos(selected_neuron.position)
		camera.reset_distance(8)
		return
	else:
		selected_neuron.call("SetSelected", false, 1.0, 0)
		clear_selected_entities()
		selected_neuron = neuron
		
	if selected_neuron != null:
		selected_neuron.call("SetSelected", true, 1.0, 0)
		emit_signal("selected_neuron", selected_neuron)			

func get_selected_neuron():
	return selected_neuron

func add_selected_entity(node: Node3D):
	_selected_entities.append(node)
	emit_signal("selected_entities_changed")
	
func get_selected_entities():
	return _selected_entities
	
func remove_selected_entity(node: Node3D):
	if node in _selected_entities:
		_selected_entities.remove_at(_selected_entities.find(node))
		emit_signal("selected_entities_changed")
	
func clear_selected_entities():
	_selected_entities.clear()
	selected_neuron = null
	emit_signal("selected_entities_changed")
	
func _emit_unfocus_event():
	var unfocus = InputEventAction.new()
	unfocus.action = "unfocus_reset"
	unfocus.pressed = true
	Input.parse_input_event(unfocus)

func unfocus_neuron():
	if selected_neuron != null:
		selected_neuron.call("SetSelected", false, 1.0, 0)
		clear_selected_entities()
		selected_neuron = null
