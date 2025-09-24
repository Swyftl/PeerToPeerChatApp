extends MenuButton

var popup

func _ready() -> void:
	popup = $".".get_popup()
	popup.id_pressed.connect(_onPressed)
	
func _onPressed(id:int):
	# Check which was pressed, and if its the right button
	# Do the corresponding thing
	
	if id == 0:
		$"../../ChatOutput".add_theme_font_size_override("font_size", get_theme_font_size("font_size")+1)
	elif id == 1:
		$"../../ChatOutput".add_theme_font_size_override("font_size", get_theme_font_size("font_size")-1)
