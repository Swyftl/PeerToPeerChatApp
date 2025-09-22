extends Button

func _ready() -> void:
	_loadTheme()
	
func _loadTheme():
	var config := ConfigFile.new()
	var save_path = "user://theme.cfg"
	
	var err = config.load(save_path)
	if err != OK:
		print("no save was detected for a theme")
	
	var theme_path = config.get_value("theme", "path", "")
	
	_changeTheme(theme_path)
	

func _on_pressed() -> void:
	var filePicker = FileDialog.new()
	
	filePicker.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	filePicker.add_filter("*.res", "Themes")
	filePicker.add_filter("*.tres", "Themes with resources")
	
	add_child(filePicker)
	filePicker.visible = true
	
	filePicker.file_selected.connect(_changeTheme)
	
func _changeTheme(file):
	if file != "":
		$"..".theme = load(file)
		_saveTheme(file)
	
func _saveTheme(file):
	var config := ConfigFile.new()
	var savePath = "user://theme.cfg"
	
	config.set_value("theme", "path", file)
	
	var err = config.save(savePath)
	if err != OK:
		push_error("Failed to save theme")
