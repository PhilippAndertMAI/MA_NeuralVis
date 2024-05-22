extends VBoxContainer


func _ready():
	var activity_map_manager = get_node("/root/VisualizationManager/ActivityMapManager")
	
	_set_visibility(activity_map_manager.call("GetIsVisible"))
	activity_map_manager.connect("toggled_visibility", _set_visibility)


func _set_visibility(val):
	visible = val
