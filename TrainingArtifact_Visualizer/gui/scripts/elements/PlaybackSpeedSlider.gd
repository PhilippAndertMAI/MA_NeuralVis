extends HSlider


func _ready():
	var playback_controller = get_node("/root/VisualizationManager/PlaybackController")
	
	min_value = playback_controller.get("minPlaybackSpeed")
	max_value = playback_controller.get("maxPlaybackSpeed")
