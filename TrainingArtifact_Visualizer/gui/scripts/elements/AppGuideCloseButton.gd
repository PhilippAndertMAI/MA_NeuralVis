extends Node

@export var style: StyleBoxFlat

var time: float


func _process(delta):
	time += delta
	
	style.border_color.a = sin(time * 2.5)
