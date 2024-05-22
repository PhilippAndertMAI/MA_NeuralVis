extends Camera3D

# External var
@export var SCROLL_SPEED: float = 10 # Speed when use scroll mouse
@export var ZOOM_SPEED: float = 5 # Speed use when is_zoom_in or is_zoom_out is true
@export var DEFAULT_DISTANCE: float = 20 # Default distance of the Node
@export var ROTATE_SPEED: float = 10
@export var MOVE_SPEED: float = 10
@export var ANCHOR_NODE_PATH: NodePath
@export var MOUSE_ZOOM_SPEED: float = 10

# Event var
var _rotation_speed: Vector2
var _move_speed: Vector2
var _scroll_speed: float
var _touches: Dictionary
var _old_touche_dist: float
# Use to add posibility to updated zoom with external script
var is_zoom_in: bool
var is_zoom_out: bool

# Transform var
var _rotation: Vector3
var _distance: float
var _anchor_node: Node3D
var center: Vector3

var _ui_entered: bool = false
var _appguide_visible: bool = false

func _ready():
	_distance = DEFAULT_DISTANCE
	_anchor_node = self.get_node(ANCHOR_NODE_PATH)
	_reset_rotation()
	
	add_user_signal("camera_rotated", [
		{ "name": "rotation_speed", "type": TYPE_FLOAT }
	])

func initialize(gui: Node):
	gui.connect("appguide_toggled_visibility", handle_appguide_visible)
	gui.connect("ui_entered_exited", handle_ui_entered_exited)

func handle_ui_entered_exited(val):
	_ui_entered = val
	_scroll_speed = 0

func handle_appguide_visible(val):
	_appguide_visible = val
	_scroll_speed = 0

func _process(delta: float):
	if is_zoom_in:
		_scroll_speed = -1 * ZOOM_SPEED
	if is_zoom_out:
		_scroll_speed = 1 * ZOOM_SPEED
	_process_transformation(delta)

func _process_transformation(delta: float):
	if _ui_entered or _appguide_visible:
		_move_speed = Vector2()
		_rotation_speed = Vector2()
		return
	
	if !(_move_speed.x == 0 && _move_speed.y == 0):
		var move_speed_3d = Vector3()
		# Assuming _move_speed is a Vector2 representing mouse movement
		move_speed_3d.x = -_move_speed.x
		move_speed_3d.y = _move_speed.y
		move_speed_3d.z = 0

		# Obtain the camera's basis without the y-rotation
		var basis_vec = Vector3(1, 1, 1)
		if _anchor_node.rotation_degrees.y > 0:
			basis_vec.y = _anchor_node.rotation_degrees.y
		basis_vec = basis_vec.normalized()
		var camera_basis = Basis(basis_vec, 0)

		# Use the camera's basis to transform the movement vector
		move_speed_3d = camera_basis * move_speed_3d

		# Move the camera based on the transformed movement vector
		_anchor_node.translate(move_speed_3d * delta * MOVE_SPEED)
		_move_speed = Vector2()	
	
	_reset_rotation
	# Update rotation
	_rotation.x += -_rotation_speed.y * delta * ROTATE_SPEED
	_rotation.y += -_rotation_speed.x * delta * ROTATE_SPEED
	if _rotation.x < -PI/2:
		_rotation.x = -PI/2
	if _rotation.x > PI/2:
		_rotation.x = PI/2
		
	var rotation_speed: float = _rotation_speed.length()
	if rotation_speed > 0:
		emit_signal("camera_rotated", rotation_speed)
		
	_rotation_speed = Vector2()
	
	# Update distance
	_distance += _scroll_speed * delta
	if _distance < 0:
		_distance = 0
	_scroll_speed = 0
	
	self.set_identity()
	self.translate_object_local(Vector3(0,0,_distance))
	##_anchor_node.set_identity()
	_anchor_node.transform.basis = Basis(Quaternion.from_euler(_rotation))

func _input(event):
	if event is InputEventScreenDrag:
		_process_touch_rotation_event(event)
	elif event is InputEventMouseMotion:
		_process_mouse_motion_event(event)
	elif event is InputEventMouseButton:
		_process_mouse_scroll_event(event)
	elif event is InputEventScreenTouch:
		_process_touch_zoom_event(event)

func _process_mouse_motion_event(e: InputEventMouseMotion):
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_rotation_speed = e.relative
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE):
		_move_speed = e.relative

func _process_mouse_scroll_event(e: InputEventMouseButton):
	if e.button_index == MOUSE_BUTTON_WHEEL_UP:
		_scroll_speed = -1 * SCROLL_SPEED
	elif e.button_index == MOUSE_BUTTON_WHEEL_DOWN:
		_scroll_speed = 1 * SCROLL_SPEED

func _process_touch_rotation_event(e: InputEventScreenDrag):
	if _touches.has(e.index):
		_touches[e.index] = e.position
	if _touches.size() == 2:
		var _keys = _touches.keys()
		var _pos_finger_1 = _touches[_keys[0]] as Vector2
		var _pos_finger_2 = _touches[_keys[1]] as Vector2
		var _dist = _pos_finger_1.distance_to(_pos_finger_2)
		if _old_touche_dist != -1:
			_scroll_speed = (_dist - _old_touche_dist) * MOUSE_ZOOM_SPEED
		_old_touche_dist = _dist
	elif _touches.size() < 2:
		_old_touche_dist = -1
		_rotation_speed = e.relative
	
func _process_touch_zoom_event(e: InputEventScreenTouch):
	if e.pressed:
		if not _touches.has(e.index):
			_touches[e.index] = e.position
	else:
		if _touches.has(e.index):	
			# warning-ignore:return_value_discarded
			_touches.erase(e.index)

func _reset_rotation():
	_rotation = _anchor_node.transform.basis.get_rotation_quaternion().get_euler()

func reset_distance(distance = DEFAULT_DISTANCE):
	_distance = distance

func set_anchor_pos(pos: Vector3):
	_anchor_node.position = pos
	_reset_rotation()
	
func get_anchor_pos():
	return _anchor_node.position
	
func set_center(center: Vector3):
	self.center = center
	set_anchor_pos(center)
