iGlobal = (dotnetClass "Autodesk.Max.GlobalInterface").Instance

ip4 = iGlobal.COREInterface14
menuManager = ip4.MenuManager

ctionManager = ip4.ActionManager
print ctionManager.NumActionTables
showMethods ctionManager

ctionManager.GetActionContext
--for i = 1 to (ctionManager.NumActionTables - 1) do 
for i = 1 to (100) do 
(
    --tp = actionManager.GetTable i 
    ac = ctionManager.GetActionContext i 
    if ac != undefined do ( 
    print(ac.Name)
    )
) 

/* new_menu = iGlobal.IMenu
new_menu.Title__IMenuElement = "NDBoxTT" 

--showMethods new_menu
--showProperties new_menu
new_menuItem = iGlobal.IMenuItem;
new_menuItem.SubMenu = new_menu;

menuManager.MainMenuBar.AddItem new_menuItem -1;
 */


/* ndbox_menu = menuManager.FindMenu "NDBox"

print ndbox_menu.NumItems
showProperties ndbox_menu */
