extends Button


var pathVisOn: CompressedTexture2D = preload("res://gui/icons/visibility_on.png")
var pathVisOff: CompressedTexture2D = preload("res://gui/icons/visibility_off.png")


func initialize_icon():
	var ui_controller = get_node("/root/VisualizationManager/GUI")
	var network_stats: TabContainer = ui_controller.get("networkStats")

	change_icon(network_stats.visible)
	ui_controller.connect("networkstats_toggled_visibility", change_icon)

func change_icon(_is_visible):
	self.icon = pathVisOff if _is_visible else pathVisOn
