extends Button

func _on_pressed() -> void:
	var filePicker = FileDialog.new()
	
	filePicker.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	filePicker.add_filter("*.res", "Themes")
	filePicker.add_filter("*.tres", "Themes with resources")
	
	add_child(filePicker)
	filePicker.visible = true
	
	filePicker.file_selected.connect(_fileSelected)
	
func _fileSelected(file):
	$"..".theme = load(file)
