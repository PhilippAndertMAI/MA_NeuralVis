extends HBoxContainer
class_name LabelledDataDisplay

var _info: TextureRect
var _data: Label

var _overridden_format: String

func _ready():
	_info = get_node("./MarginContainerInfo/Info")
	_data = get_node("./Data")
	
	if tooltip_text.is_empty():
		_info.visible = false
	else:
		_info.tooltip_text = tooltip_text
		
func _input(event):
	if event is InputEventMouseMotion:
		pass
		
func override_format(format: String):
	_overridden_format = format
		
func set_data(value):
	if not _overridden_format.is_empty():
		_data.text = _overridden_format % value
	else:
		_data.text = "%.2f" % value

func set_data_with_format(format, value):
	_data.text = format % value
