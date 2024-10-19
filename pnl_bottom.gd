@tool
extends Panel

@export var reset : bool = false

@onready var UI = $".."

var buttons = []

func _ready():
	LoadButtons()
	for button in buttons:
		button.connect("pressed", OnButtonPressed.bind(button))
	

func _process(delta):
	if reset:
		reset = false
		LoadButtons()
		ArrangeButtons()

func OnButtonPressed(button: Button):
	OpenSubmenu(button.text)
	
func CloseMenus():
	pass
	
func OpenSubmenu(menu : String):
	print(menu)
	for submenu in get_parent().get_children():
		if submenu != self:
			if menu == submenu.name:
				if submenu.is_visible():
					submenu.set_visible(false)
				else:
					submenu.set_visible(true)
			else :
				submenu.set_visible(false)
						
func ArrangeButtons():
	var div : float = float(1) / len(buttons)
	
	for i in range(len(buttons)):
		buttons[i].anchor_left = div * i
		buttons[i].anchor_right = (div * i) + div
		buttons[i].anchor_top = 0
		buttons[i].anchor_bottom = 1
		
		buttons[i].offset_bottom = 0
		buttons[i].offset_top = 0
		buttons[i].offset_left = 0
		buttons[i].offset_right = 0
	
func LoadButtons():
	buttons = []
	for child in get_children():
		if child is Button:
			buttons.append(child)
