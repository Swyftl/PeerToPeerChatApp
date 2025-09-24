extends MenuButton

var popup

func _ready() -> void:
	popup = $".".get_popup()
	popup.id_pressed.connect(_popup_pressed)
	_loadTheme()

func _on_pressed() -> void:
	var filePicker = FileDialog.new()
	
	filePicker.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	filePicker.add_filter("*.res", "Themes")
	filePicker.add_filter("*.tres", "Themes with resources")
	
	filePicker.use_native_dialog = true
	filePicker.access = 2
	
	add_child(filePicker)
	filePicker.visible = true
	
	filePicker.file_selected.connect(_changeTheme)

func _popup_pressed(id:int):
	if id == 0:
		_on_pressed()
	elif id == 1:
		_removeTheme()

func _loadTheme():
	var config := ConfigFile.new()
	var save_path = "user://theme.cfg"
	
	var err = config.load(save_path)
	if err != OK:
		print("no save was detected for a theme")
	
	var theme_path = config.get_value("theme", "path", "")
	
	_changeTheme(theme_path)
	
func _changeTheme(file):
	if file != "":
		if load(file) != null:
			$"..".theme = load(file)
			_saveTheme(file)
		else:
			_saveTheme("")
			
func _saveTheme(file):
	var config := ConfigFile.new()
	var savePath = "user://theme.cfg"
	
	config.set_value("theme", "path", file)
	
	var err = config.save(savePath)
	if err != OK:
		push_error("Failed to save theme")
		
func _removeTheme():
	var config:= ConfigFile.new()
	var savePath = "user://theme.cfg"
	
	config.set_value("theme", "path", "")
	
	var err = config.save(savePath)
	if err != OK:
		push_error("Failed to save theme")
		
	$"..".theme = null
