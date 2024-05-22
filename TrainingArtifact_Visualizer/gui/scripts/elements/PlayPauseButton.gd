extends Button


var pathPause: CompressedTexture2D = preload("res://gui/icons/pause.png")
var pathPlay: CompressedTexture2D = preload("res://gui/icons/play.png")

var playback_controller: Node

func _ready():
	playback_controller = get_node("/root/VisualizationManager/PlaybackController")
	
	change_icon(playback_controller.call("GetIsPlaying"))
	playback_controller.connect("toggled_is_playing", change_icon)

func change_icon(isPlaying):
	self.icon = pathPause if isPlaying else pathPlay
