extends Button


var pathShow: CompressedTexture2D = preload("res://gui/icons/layers_on.png")
var pathHide: CompressedTexture2D = preload("res://gui/icons/layers_off.png")


func _ready():
	var activity_map_manager = get_node("/root/VisualizationManager/ActivityMapManager")
	
	change_icon(activity_map_manager.call("GetIsVisible"))
	activity_map_manager.connect("toggled_visibility", change_icon)

func change_icon(isVisible):
	self.icon = pathHide if isVisible else pathShow
