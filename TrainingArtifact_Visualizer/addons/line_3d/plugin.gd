@tool
extends EditorPlugin


func _enter_tree():
	var line_3d = preload("res://addons/line_3d/Line3D.gd")
	var icon = preload("res://addons/line_3d/Line3D-gd4.svg")
	add_custom_type("Line3D", "Path3D", line_3d, icon)
	

func _exit_tree():
	remove_custom_type("Line3D")
