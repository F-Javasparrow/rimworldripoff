@tool
extends Panel

@export var reset : bool = false

@onready var UI = $".."

var buttons = []
var current_buildable : String = ""

var columnCount : int = 2

func _ready():
	LoadButtons()
	for button in buttons:
		button.connect("pressed", OnButtonPressed.bind(button))

func _process(delta):
	if reset:
		reset = false
		LoadButtons()
		ArrangeSelf()
		ArrangeButtons()
		
		
func OnButtonPressed(button : Button):
	OpenSubmenu(button.text)
	
func OnButtonPressedBuildable(buildable):
	UI.BeginPlacing(buildable)
	
func OnButtonPressedOrder(order):
	UI.BeginOrdering(order)
	
func CloseMenus():
	self.set_visible(false)
	current_buildable = ""
	for child in get_children():
		if child is Panel:
			child.set_visible(false)
			for i in range(child.get_child_count()):
				child.get_children()[0].queue_free()
	
func OpenSubmenu(menu):
	if menu == current_buildable:
		find_child("pnl_buildable").set_visible(false)
		current_buildable = ""
	else:
		current_buildable = menu
		find_child("pnl_buildable").set_visible(true)
		match menu:
			"Structure":
				LoadArchitectBuildableMenu()
			"Orders":
				LoadOrdersMenu()
				
func LoadArchitectBuildableMenu():
	var pnl_buildable = find_child("pnl_buildable")
	
	for i in range(pnl_buildable.get_child_count()):
		pnl_buildable.get_child(i).queue_free()
		
	var buildables = UI.itemManager.constructionPrototypes# ["Cancel", "Wall", "Door", "Fence", "and another thing"]	
	
	for i in range(len(buildables)):
		var button = Button.new()
		pnl_buildable.add_child(button)
		button.text = ItemManager._path_to_name(buildables[i].get_path())
		var buttonSize = pnl_buildable.size.x / 20
		button.position = Vector2(buttonSize * i, pnl_buildable.size.y - buttonSize)
		button.size = Vector2(buttonSize, buttonSize)
		button.add_theme_font_size_override("font_size", 10)
		
		button.connect("pressed", OnButtonPressedBuildable.bind(buildables[i]))
		
func LoadOrdersMenu():
	var pnl_buildable = find_child("pnl_buildable")
	
	for i in range(pnl_buildable.get_child_count()):
		pnl_buildable.get_child(i).queue_free()
		
	var orders = Task.Orders.keys()
	
	for i in range(len(orders)):
		var button = Button.new()
		pnl_buildable.add_child(button)
		button.text = orders[i]
		var buttonSize = pnl_buildable.size.x / 20
		button.position = Vector2(buttonSize * i, pnl_buildable.size.y - buttonSize)
		button.size = Vector2(buttonSize, buttonSize)
		button.add_theme_font_size_override("font_size", 10)
		
		button.connect("pressed", OnButtonPressedOrder.bind(i))
				
		
		
func LoadButtons():
	buttons = []
	for child in get_children():
		if child is Button:
			buttons.append(child)
	

func ArrangeSelf():
	anchor_left = 0
	anchor_right = 0.2
	anchor_bottom = 1 - UI.buttonHeight
	
	var rows = (len(buttons)+1)/columnCount
	
	anchor_top = anchor_bottom - UI.buttonHeight * rows
	
	offset_bottom = 0
	offset_top = 0
	offset_left = 0
	offset_right = 0

func ArrangeButtons():
	var rows = (len(buttons)+1)/columnCount
	
	for i in range(len(buttons)):
		var column = i%columnCount
		var row = i / columnCount
		
		buttons[i].anchor_top = row * 1/float(rows)
		buttons[i].anchor_bottom = 1/float(rows) + row * 1/float(rows)
		buttons[i].anchor_left = column * 1/float(columnCount)
		buttons[i].anchor_right = 1/float(columnCount) + column * 1/float(columnCount)
		
		buttons[i].offset_bottom = 0
		buttons[i].offset_top = 0
		buttons[i].offset_left = 0
		buttons[i].offset_right = 0
