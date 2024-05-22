extends MarginContainer
class_name AppGuideItem

var item_resource: AppGuideItemResource

var item_desc_container: RichTextLabel

var button: Button

func _ready():
	button = get_child(0)
	button.text = "Item"
	
	button.pressed.connect(activate)

func instantiate(resource: AppGuideItemResource, desc_container: RichTextLabel):
	item_resource = resource
	item_desc_container = desc_container
	
	button.text = item_resource.item_name

func activate():
	item_desc_container.text = item_resource.item_desc
