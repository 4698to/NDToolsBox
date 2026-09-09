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

        // 菜单是否已完成首次安装（避免 File>New / 主题切换触发的 PostNew 反复拆建菜单）
        private bool _menusInstalled;

        private void MenuSystemStartupHandler(IntPtr objPtr, INotifyInfo infoPtr)
        {
            // 参考 BsKeyTools：UI 重载期间操作菜单会导致低版本 Max 闪退
            TryInstallMenusOnce();
        }

        private void MenuSystemStartupHandler_2015(IntPtr param0, IntPtr param1) 
        {
            ScriptsUtilities.print("MenuSystemStartupHandler_2015 - " );
            TryInstallMenusOnce();
        }

        /// <summary>
        /// 仅在 NDBox 菜单尚不存在时创建；已存在则跳过，防止切换颜色主题等 UI 重载时崩溃。
        /// </summary>
        private void TryInstallMenusOnce()
        {
            try
            {
                IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;
                // 菜单已在：不拆不建（主题切换 / 反复 PostNew 时的关键防护）
                if (menuManager.FindMenu("NDBox") != null)
                {
                    _menusInstalled = true;
                    return;
                }
                InstallMenus();
                _menusInstalled = true;
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print($"TryInstallMenusOnce: {ex.Message}");
            }
        }
        public override void Stop()
        {
            try
            {
#if M2015 || M2016
                if (m_SystemStartupDelegate_2015 != null)
                {
                    ScriptsUtilities.global.UnRegisterNotification(m_SystemStartupDelegate_2015, null, SystemNotificationCode.SystemPostNew);
                    m_SystemStartupDelegate_2015 = null;
                }
#else
                if (m_SystemStartupDelegate != null)
                {
                    ScriptsUtilities.global.UnRegisterNotification(m_SystemStartupDelegate, null, SystemNotificationCode.SystemPostNew);
                    m_SystemStartupDelegate = null;
                }
#endif

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
                _menusInstalled = false;
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

            // 与 InstallMenus 相同：已存在则跳过，避免 UI 重载期间拆建菜单
            menu = menuManager.FindMenu("NDBox");
            if (menu != null)
            {
                return;
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

            // 修复（对齐 BsKeyTools b01c92f）：
            // 切换 Max 颜色主题时会重载 UI；此时若 UnRegister/重建菜单，低版本易闪退。
            // 菜单已存在则直接返回，不再拆除重建。
            menu = menuManager.FindMenu("NDBox");
            if (menu != null)
            {
                return;
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
            // M2015-M2020: [0]=NDBox Float, [1]=SelectSet；M2021+: 仅 [0]=SelectSet
            if (actionTable != null && actionTable.Count > 0)
            {
#if M2015 || M2016 || M2017 || M2018 || M2019 || M2020
                menuItemNDBoxFloat = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxFloat.Title = "&Open NDBox-Float";
                menuItemNDBoxFloat.ActionItem = actionTable[0];
                menu.AddItem(menuItemNDBoxFloat, -1);

                if (actionTable.Count > 1)
                {
                    menuItemNDBoxSelectSetToolsBar = ScriptsUtilities.global.IMenuItem;
                    menuItemNDBoxSelectSetToolsBar.Title = "&Open NameSel-Float";
                    menuItemNDBoxSelectSetToolsBar.ActionItem = actionTable[1];
                    menu.AddItem(menuItemNDBoxSelectSetToolsBar, -1);
                }
#else
                menuItemNDBoxSelectSetToolsBar = ScriptsUtilities.global.IMenuItem;
                menuItemNDBoxSelectSetToolsBar.Title = "&Open NameSel-Float";
                menuItemNDBoxSelectSetToolsBar.ActionItem = actionTable[0];
                menu.AddItem(menuItemNDBoxSelectSetToolsBar, -1);
#endif
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
