extends PanelContainer


@export var resources_path: String

@export var scroll_vbox: VBoxContainer
@export var item_desc_container: RichTextLabel

@export var btn_close: Button

@export var item_resources: Array[AppGuideItemResource]

var _gui: Node


func _ready():
	_gui = get_parent()
	btn_close.pressed.connect(_handle_close_clicked)
	
	_instantiate_items()
	
	self.visibility_changed.connect(_handle_visibility_changed)


func _instantiate_items():
	var item_scene: PackedScene = preload("res://gui/elements/AppGuideItem.tscn")
	
	for i in item_resources.size():
		var item_resource = item_resources[i]
		var app_guide_item: AppGuideItem = item_scene.instantiate()
		app_guide_item.name = "item_" + str(i)
		scroll_vbox.add_child(app_guide_item)
		app_guide_item.instantiate(item_resource, item_desc_container)

func _handle_visibility_changed():
	if self.visible:
		_handle_opened()
	
func _handle_opened():
	var first_item: AppGuideItem = scroll_vbox.get_child(0)
	first_item.activate()
	first_item.button.button_pressed = true
	
func _handle_close_clicked():
	self.visible = false
	
	_gui.emit_signal("appguide_toggled_visibility", false)
