extends Panel

var targetItem = null

func _ready():
	set_visible(false)
	
	
func _process(delta):
	if targetItem != null:
		SelectItem(targetItem)

func SelectItem(item):
	targetItem = item
	if item == null:
		set_visible(false)
	else:
		set_visible(true)
		SetItemName(item.GetName())
		SetItemCount(item.GetCount())
	
func SetItemName(itemName : String):
	get_node("lbl_item_name").text = itemName
	
func SetItemCount(itemCount):
	get_node("lbl_item_amount").text = str(itemCount)	
	
	
func CloseMenus():
	pass
