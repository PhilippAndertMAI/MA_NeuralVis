extends Button


var playback_controller: Node

func _ready():
	playback_controller = get_node("/root/VisualizationManager/PlaybackController")
	
	disable_button(playback_controller.call("GetIsPlaying"))
	playback_controller.connect("toggled_is_playing", disable_button)

func disable_button(isPlaying):
	self.disabled = isPlaying
	self.mouse_default_cursor_shape = Control.CURSOR_ARROW if isPlaying else Control.CURSOR_POINTING_HAND
