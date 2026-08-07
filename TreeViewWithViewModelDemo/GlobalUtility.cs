using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using Autodesk.Max.IQuadMenuContext;
using Autodesk.Max.Plugins;
using ManagedServices;

namespace NDToolsBox
{
    class GlobalUtility : GUP
    {
        IIMenu menu;
        IIMenuItem menuItem;
        IIMenuItem menuItemNDBoxFloat;
        IIMenuItem menuItemNDBoxDock;
        IIMenuItem menuItemNDBoxToolsBar;
        IIMenuItem menuItemNDBoxSelectSetToolsBar;
        uint idActionTable = 0;
        IActionTable actionTable;//选择集工具的
        IActionTable actionTable_ANDTools;
        IActionCallback actionCallback;
        GlobalDelegates.Delegate5 m_SystemStartupDelegate;
        GlobalDelegates.Delegate5 m_SystemStartupDelegate_2015;

        private void MenuSystemStartupHandler(IntPtr objPtr, INotifyInfo infoPtr)
        {
            InstallMenus();
        }

        private void MenuSystemStartupHandler_2015(IntPtr param0, IntPtr param1) 
        {
            ScriptsUtilities.print("MenuSystemStartupHandler_2015 - " );

            InstallMenus();
        }
        public override void Stop()
        {
            try
            {

                if (actionTable != null)
                {
                    ScriptsUtilities.global.COREInterface.ActionManager.DeactivateActionTable(actionCallback, idActionTable);
                }
                // Clean up menu
                if (menu != null)
                {
                    ScriptsUtilities.global.COREInterface.MenuManager.UnRegisterMenu(menu);
                    ScriptsUtilities.global.ReleaseIMenu(menu);
                    ScriptsUtilities.global.ReleaseIMenuItem(menuItemNDBoxFloat);
                    ScriptsUtilities.global.ReleaseIMenuItem(menuItemNDBoxSelectSetToolsBar);
                    ScriptsUtilities.global.ReleaseIMenuItem(menuItem);
                    menu = null;
                    menuItem = null;
                }
            }
            catch { 
            }
        }
        public static IActionItem GetActionItem(string action_name,string tb_name)
        {
            IIActionManager actionManager = ScriptsUtilities.ip4.ActionManager;
            for (int i = 0; i < actionManager.NumActionTables; i++)
            {
                IActionTable tb = actionManager.GetTable(i);
                if (tb.Name.Equals(tb_name))
                { 
                    for (int o = 0; o < tb.Count; o++)
                    {
                        ScriptsUtilities.print($"IActionTable {o} -> {tb[o].ButtonText}");
                        if (tb[o].ButtonText.Equals(action_name))
                        {
                            return (tb[o]);
                        }
                    }
                }
            }
            return null;
        }
        public IActionTable GetActionTable_ANDTools()
        {
            IIActionManager actionManager = ScriptsUtilities.ip4.ActionManager;
            for (int i = 0; i < actionManager.NumActionTables; i++)
            {
                IActionTable tb = actionManager.GetTable(i);
                if (tb.Name.Equals("A-NDTools"))
                {
                    
                     return (tb);
                }
            }
            return null;
        }
        public override uint Start
        {
            get
            {
                IIActionManager actionManager = ScriptsUtilities.ip4.ActionManager;

                // Set up global actions
                idActionTable = (uint)actionManager.NumActionTables;
                string actionTableName = "A-NDTools-Float";
                //string actionTableName = "A-NDTools";
                

#if M2022 || M2023 || M2024
                actionTable = ScriptsUtilities.global.ActionTable.Create(idActionTable, 0, actionTableName);
#else
                actionTable = ScriptsUtilities.global.ActionTable.Create(idActionTable, 0, ref actionTableName);
#endif

#if M2015 || M2016 || M2017 || M2018 || M2019 || M2020 
                actionTable.AppendOperation(new NDBoxActionItem());//不可停靠
#endif
                actionTable.AppendOperation(new SelectSetActionItem());
                actionCallback = new NDBoxActionCallback();
                //注册成新的 ActionTable
                actionManager.RegisterActionTable(actionTable);
                actionManager.ActivateActionTable(actionCallback as ActionCallback, idActionTable);
                // Set up menus
#if M2015 || M2016
                //InstallMenus();
                ScriptsUtilities.print("SystemNotificationCode.SystemPostNew - ");

                m_SystemStartupDelegate_2015 = new GlobalDelegates.Delegate5(MenuSystemStartupHandler_2015);
                
                ScriptsUtilities.global.RegisterNotification(m_SystemStartupDelegate_2015, null, SystemNotificationCode.SystemPostNew);
#else
                m_SystemStartupDelegate = new GlobalDelegates.Delegate5(MenuSystemStartupHandler);
                ScriptsUtilities.global.RegisterNotification(m_SystemStartupDelegate, null, SystemNotificationCode.SystemPostNew);
                
#endif
                //InstallMenus();
                //actionTable_ANDTools = GetActionTable_ANDTools();
                //AddMenus();
                return 0;
            }
        }
        private void AddMenus()
        {
            IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;


            // Set up menu
            menu = menuManager.FindMenu("NDBox");
            // 如果已经有了，就移除掉
            if (menu != null)
            {
                menuManager.UnRegisterMenu(menu);
                ScriptsUtilities.global.ReleaseIMenu(menu);
                menu = null;
            }
            // Main menu
            menu = ScriptsUtilities.global.IMenu;
            menu.Title = "NDBox";
            menuManager.RegisterMenu(menu, 0);

            //actionTable = GetActionTable_ANDTools();
            if (actionTable_ANDTools != null)
            {
                for (int i = 0; i < actionTable_ANDTools.Count; i++)
                {
                    IIMenuItem mi = ScriptsUtilities.global.IMenuItem;
                    mi.Title = actionTable_ANDTools[i].MenuText; //"&Open NDBox-Dock";
                    mi.ActionItem = actionTable_ANDTools[i];
                    menu.AddItem(mi, -1);
                }
            }
            for (int i=0; i < actionTable.Count; i++)
            {
                menuItemNDBoxDock = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxDock.Title = actionTable[i].MenuText; //"&Open NDBox-Dock";
                menuItemNDBoxDock.ActionItem = actionTable[i];
                menu.AddItem(menuItemNDBoxDock, -1);

                ScriptsUtilities.print($"{i} , {actionTable[i].MenuText}");

            }
            menuItem = ScriptsUtilities.global.IMenuItem;
            menuItem.SubMenu = menu;
            menuManager.MainMenuBar.AddItem(menuItem, -1);

            //ScriptsUtilities.ip4.MenuManager.UpdateMenuBar();
            ScriptsUtilities.global.COREInterface.MenuManager.UpdateMenuBar();
        }
        private void InstallMenus()
        {
            IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;

            // Set up menu
            menu = menuManager.FindMenu("NDBox");
            // 如果已经有了，就移除掉
            if (menu != null)
            {
                menuManager.UnRegisterMenu(menu);
                ScriptsUtilities.global.ReleaseIMenu(menu);
                menu = null;
            }
           

            // Main menu
            menu = ScriptsUtilities.global.IMenu;
            menu.Title = "NDBox";
            menuManager.RegisterMenu(menu, 0);

            // Launch option

            //IActionItem item = GetActionItem("NDBox-Dock"); // "NDBox-Dock"
            IActionItem item = GetActionItem("天晴盒子", "A-NDTools-Dock"); // "NDBox-Dock"
            if (item != null) { 
                //ScriptsUtilities.print($"NDBox-Dock IActionItem -> {item.MenuText}");
                menuItemNDBoxDock = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxDock.Title = "&Open NDBox-Dock";
                menuItemNDBoxDock.ActionItem = item;
                menu.AddItem(menuItemNDBoxDock, 0);
            }
            else { 
                ScriptsUtilities.print($"No find -> NDBox-Dock IActionItem");
            }

            //盒子和选择集工具 两个不停靠工具的菜单 
            if (actionTable != null && actionTable.Count > 0)
            {
                menuItemNDBoxFloat = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxFloat.Title = "&Open NDBox-Float";
                menuItemNDBoxFloat.ActionItem = actionTable[0];
                menu.AddItem(menuItemNDBoxFloat, -1);

                menuItemNDBoxSelectSetToolsBar = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxSelectSetToolsBar.Title = "&Open NameSel-Float";
                menuItemNDBoxSelectSetToolsBar.ActionItem = actionTable[1];
                menu.AddItem(menuItemNDBoxSelectSetToolsBar, -1);

            }
            IActionItem baritem = GetActionItem("侧边工具栏", "A-NDTools-Dock");
            if (baritem != null)
            {
                menuItemNDBoxToolsBar = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxToolsBar.Title = "&Open ToolsBar";
                menuItemNDBoxToolsBar.ActionItem = baritem;
                menu.AddItem(menuItemNDBoxToolsBar, -1);
            }

            //这种需要停靠外框
            /*IActionItem selectsetBar = GetActionItem("SelectSet-Dock");
            if (selectsetBar != null)
            {
                menuItemNDBoxSelectSetToolsBar = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxSelectSetToolsBar.Title = "&Open SelectSetToolsBar";
                menuItemNDBoxSelectSetToolsBar.ActionItem = selectsetBar;
                menu.AddItem(menuItemNDBoxSelectSetToolsBar, -1);
            }*/

            menuItem = ScriptsUtilities.global.IMenuItem;
            menuItem.SubMenu = menu;
            menuManager.MainMenuBar.AddItem(menuItem, -1);

            //ScriptsUtilities.ip4.MenuManager.UpdateMenuBar();
            ScriptsUtilities.global.COREInterface.MenuManager.UpdateMenuBar();
        }
    }
}
