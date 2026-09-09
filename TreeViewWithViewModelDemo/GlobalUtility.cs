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
        GlobalDelegates.Delegate5 m_SystemStartupNotifyDelegate;

        // 菜单是否已补齐当前可装选项（避免 File>New / 主题切换触发的 PostNew 反复拆建菜单）
        private bool _menusInstalled;

        // 与 Action.ButtonText / MenuText 对齐（Max 未设 UseCustomTitle 时按此显示）
        private const string ActionTextFloat = "天晴盒子";
        private const string ActionTextFloatDockAlt = "天晴盒子-Dock";
        private const string ActionTextToolsBar = "侧边工具栏";

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

#if M2015 || M2016
        // Max 2015/2016：Delegate5 = (IntPtr, IntPtr)
        private void SystemStartupNotifyHandler(IntPtr param0, IntPtr param1)
        {
            // PostNew 常在 GUP.Start 注册前回调已结束；SystemStartup 补一次
            ScriptsUtilities.print("SystemStartupNotifyHandler - ");
            TryInstallMenusOnce();
        }
#else
        // Max 2017+：Delegate5 = (IntPtr, INotifyInfo)
        private void SystemStartupNotifyHandler(IntPtr objPtr, INotifyInfo infoPtr)
        {
            ScriptsUtilities.print("SystemStartupNotifyHandler - ");
            TryInstallMenusOnce();
        }
#endif

        private static bool IsNdBoxOnMainMenuBar(IIMenuManager menuManager, IIMenu ndMenu)
        {
            if (menuManager == null || ndMenu == null || menuManager.MainMenuBar == null)
                return false;
            IIMenu bar = menuManager.MainMenuBar;
            for (int i = 0; i < bar.NumItems; i++)
            {
                IIMenuItem mi = bar.GetItem(i);
                if (mi == null)
                    continue;
                if (mi.SubMenu != null && object.ReferenceEquals(mi.SubMenu, ndMenu))
                    return true;
                if (mi.SubMenu != null && "NDBox".Equals(mi.SubMenu.Title))
                    return true;
            }
            return false;
        }

        private static bool IsMissingMenuItem(IIMenuItem mi)
        {
            // CUI 持久化后 ActionTable Id 变化，项会显示为 "Missing: <ActionId>"（如 4698 / 1）
            return mi != null && mi.Title != null
                && mi.Title.StartsWith("Missing:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool MenuHasBoundAction(IIMenu ndMenu, string actionButtonText)
        {
            if (ndMenu == null || string.IsNullOrEmpty(actionButtonText))
                return false;
            for (int i = 0; i < ndMenu.NumItems; i++)
            {
                IIMenuItem mi = ndMenu.GetItem(i);
                if (mi == null || IsMissingMenuItem(mi))
                    continue;
                if (actionButtonText.Equals(mi.Title))
                    return true;
                IActionItem ai = mi.ActionItem;
                if (ai != null && actionButtonText.Equals(ai.ButtonText))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 清掉失效绑定（Missing: id），再按当前 ActionTable 重挂。不 UnRegister 整个菜单。
        /// </summary>
        private static void RemoveMissingMenuItems(IIMenu ndMenu)
        {
            if (ndMenu == null)
                return;
            for (int i = ndMenu.NumItems - 1; i >= 0; i--)
            {
                if (IsMissingMenuItem(ndMenu.GetItem(i)))
                    ndMenu.RemoveItem(i);
            }
        }

        /// <summary>
        /// 静默查找 Action，供菜单补齐反复调用（避免刷屏 Listener）。
        /// </summary>
        private static IActionItem FindActionItem(string action_name, string tb_name)
        {
            IIActionManager actionManager = ScriptsUtilities.ip4.ActionManager;
            for (int i = 0; i < actionManager.NumActionTables; i++)
            {
                IActionTable tb = actionManager.GetTable(i);
                if (tb == null || !tb.Name.Equals(tb_name))
                    continue;
                for (int o = 0; o < tb.Count; o++)
                {
                    if (tb[o].ButtonText.Equals(action_name))
                        return tb[o];
                }
            }
            return null;
        }

        /// <summary>
        /// Dock 在不同版本/工程中的 ButtonText、Category 不一致，逐一尝试。
        /// Max2015 工程可能未编入 NDBoxCuiDock，找不到则返回 null（不阻断 Float/侧边栏）。
        /// </summary>
        private static IActionItem FindDockActionItem()
        {
            return FindActionItem("天晴盒子", "A-NDTools-Dock")
                ?? FindActionItem(ActionTextFloatDockAlt, "A-NDTools")
                ?? FindActionItem(ActionTextFloat, "A-NDTools");
        }

        private static IActionItem FindToolsBarActionItem()
        {
            return FindActionItem(ActionTextToolsBar, "A-NDTools-Dock")
                ?? FindActionItem(ActionTextToolsBar, "A-NDTools");
        }

        private static void AddMenuAction(IIMenu ndMenu, IActionItem action, string title, int pos)
        {
            if (ndMenu == null || action == null || MenuHasBoundAction(ndMenu, action.ButtonText))
                return;
            IIMenuItem mi = ScriptsUtilities.global.IMenuItem;
            mi.Title = string.IsNullOrEmpty(title) ? action.MenuText : title;
            mi.UseCustomTitle = true;
            mi.ActionItem = action;
            ndMenu.AddItem(mi, pos);
        }

        /// <summary>
        /// 期望项是否都已有效绑定（不含 Missing:）。
        /// 截图典型半成品：Missing:4698(天晴盒子)、Missing:1(选择集)，仅侧边工具栏可用。
        /// </summary>
        private bool IsNdBoxMenuComplete(IIMenu ndMenu)
        {
            if (ndMenu == null)
                return false;

            for (int i = 0; i < ndMenu.NumItems; i++)
            {
                if (IsMissingMenuItem(ndMenu.GetItem(i)))
                    return false;
            }

#if M2015 || M2016 || M2017 || M2018 || M2019 || M2020
            if (actionTable != null && actionTable.Count > 0
                && !MenuHasBoundAction(ndMenu, actionTable[0].ButtonText))
                return false;
            if (actionTable != null && actionTable.Count > 1
                && !MenuHasBoundAction(ndMenu, actionTable[1].ButtonText))
                return false;
#else
            if (actionTable != null && actionTable.Count > 0
                && !MenuHasBoundAction(ndMenu, actionTable[0].ButtonText))
                return false;
#endif

            IActionItem dock = FindDockActionItem();
            if (dock != null && !MenuHasBoundAction(ndMenu, dock.ButtonText))
                return false;

            IActionItem bar = FindToolsBarActionItem();
            if (bar != null && !MenuHasBoundAction(ndMenu, bar.ButtonText))
                return false;

            return true;
        }

        /// <summary>
        /// 菜单未齐则清 Missing 并补装；已齐则跳过，防止切换颜色主题等 UI 重载时崩溃。
        /// </summary>
        private void TryInstallMenusOnce()
        {
            try
            {
                IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;
                IIMenu existing = menuManager.FindMenu("NDBox");
                // 选项已齐、无 Missing、且已在主菜单栏：不拆不建
                if (IsNdBoxMenuComplete(existing) && IsNdBoxOnMainMenuBar(menuManager, existing))
                {
                    ScriptsUtilities.print("TryInstallMenusOnce: already complete");
                    _menusInstalled = true;
                    return;
                }
                InstallMenus();
                existing = menuManager.FindMenu("NDBox");
                _menusInstalled = IsNdBoxMenuComplete(existing) && IsNdBoxOnMainMenuBar(menuManager, existing);
                ScriptsUtilities.print($"TryInstallMenusOnce: installed={_menusInstalled}");
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
                if (m_SystemStartupNotifyDelegate != null)
                {
                    ScriptsUtilities.global.UnRegisterNotification(m_SystemStartupNotifyDelegate, null, SystemNotificationCode.SystemStartup);
                    m_SystemStartupNotifyDelegate = null;
                }

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

                // PostNew：File>New / 部分 UI 重载；常在 GUP.Start 注册前就已触发，不能只靠它
#if M2015 || M2016
                m_SystemStartupDelegate_2015 = new GlobalDelegates.Delegate5(MenuSystemStartupHandler_2015);
                ScriptsUtilities.global.RegisterNotification(m_SystemStartupDelegate_2015, null, SystemNotificationCode.SystemPostNew);
#else
                m_SystemStartupDelegate = new GlobalDelegates.Delegate5(MenuSystemStartupHandler);
                ScriptsUtilities.global.RegisterNotification(m_SystemStartupDelegate, null, SystemNotificationCode.SystemPostNew);
#endif
                // SystemStartup：启动更晚一拍，补上错过的 PostNew
                m_SystemStartupNotifyDelegate = new GlobalDelegates.Delegate5(SystemStartupNotifyHandler);
                ScriptsUtilities.global.RegisterNotification(m_SystemStartupNotifyDelegate, null, SystemNotificationCode.SystemStartup);

                // Start 时立即装一次（PostNew 已过时的兜底）；已齐则内部直接 return
                ScriptsUtilities.print("GUP.Start -> TryInstallMenusOnce");
                TryInstallMenusOnce();
                return 0;
            }
        }
        private void AddMenus()
        {
            IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;

            menu = menuManager.FindMenu("NDBox");
            RemoveMissingMenuItems(menu);
            if (IsNdBoxMenuComplete(menu))
            {
                return;
            }
            bool createdNew = menu == null;
            if (createdNew)
            {
                menu = ScriptsUtilities.global.IMenu;
                menu.Title = "NDBox";
                menuManager.RegisterMenu(menu, 0);
            }

            //actionTable = GetActionTable_ANDTools();
            if (actionTable_ANDTools != null)
            {
                for (int i = 0; i < actionTable_ANDTools.Count; i++)
                {
                    AddMenuAction(menu, actionTable_ANDTools[i], actionTable_ANDTools[i].MenuText, -1);
                }
            }
            for (int i=0; i < actionTable.Count; i++)
            {
                AddMenuAction(menu, actionTable[i], actionTable[i].MenuText, -1);
                ScriptsUtilities.print($"{i} , {actionTable[i].MenuText}");
            }
            if (createdNew)
            {
                menuItem = ScriptsUtilities.global.IMenuItem;
                menuItem.SubMenu = menu;
                menuManager.MainMenuBar.AddItem(menuItem, -1);
            }

            //ScriptsUtilities.ip4.MenuManager.UpdateMenuBar();
            ScriptsUtilities.global.COREInterface.MenuManager.UpdateMenuBar();
        }
        private void InstallMenus()
        {
            IIMenuManager menuManager = ScriptsUtilities.ip4.MenuManager;

            // 修复（对齐 BsKeyTools b01c92f）：
            // 切换 Max 颜色主题时会重载 UI；此时若 UnRegister/重建菜单，低版本易闪退。
            // 半成品：清 Missing 后按 Action 补绑定，不拆除整个菜单。
            menu = menuManager.FindMenu("NDBox");
            RemoveMissingMenuItems(menu);

            bool createdNew = menu == null;
            if (createdNew)
            {
                menu = ScriptsUtilities.global.IMenu;
                menu.Title = "NDBox";
                menuManager.RegisterMenu(menu, 0);
            }

            if (!IsNdBoxMenuComplete(menu))
            {
                // Dock（可选：Max2015 工程可能未编入 NDBoxCuiDock）
                AddMenuAction(menu, FindDockActionItem(), null, 0);

                //盒子和选择集工具 两个不停靠工具的菜单
                // M2015-M2020: [0]=NDBox Float, [1]=SelectSet；M2021+: 仅 [0]=SelectSet
                if (actionTable != null && actionTable.Count > 0)
                {
#if M2015 || M2016 || M2017 || M2018 || M2019 || M2020
                    AddMenuAction(menu, actionTable[0], null, -1);
                    if (actionTable.Count > 1)
                        AddMenuAction(menu, actionTable[1], null, -1);
#else
                    AddMenuAction(menu, actionTable[0], null, -1);
#endif
                }

                AddMenuAction(menu, FindToolsBarActionItem(), null, -1);
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

            // 菜单壳可能已在 CUI 中，但未挂到主菜单栏 → 仍要 AddItem
            if (createdNew || !IsNdBoxOnMainMenuBar(menuManager, menu))
            {
                menuItem = ScriptsUtilities.global.IMenuItem;
                menuItem.SubMenu = menu;
                menuManager.MainMenuBar.AddItem(menuItem, -1);
            }

            //ScriptsUtilities.ip4.MenuManager.UpdateMenuBar();
            ScriptsUtilities.global.COREInterface.MenuManager.UpdateMenuBar();
            ScriptsUtilities.print($"InstallMenus done, NumItems={menu.NumItems}, onBar={IsNdBoxOnMainMenuBar(menuManager, menu)}");
        }
    }
}
