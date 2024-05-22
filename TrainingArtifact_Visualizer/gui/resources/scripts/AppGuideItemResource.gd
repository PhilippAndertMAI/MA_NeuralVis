extends Resource
class_name AppGuideItemResource

@export var item_name: String
@export_multiline var item_desc: String


func _init():
	item_name = 'new item'
	item_desc = 'item description'
