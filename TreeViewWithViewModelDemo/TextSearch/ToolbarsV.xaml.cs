using MaxToolbars.Toobars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.Max.Plugins;
using Autodesk.Max;
//using XiaFControl.Controls;
using ManagedServices;
using CSharpUtilities;
using NDToolsBox;
using System.Collections.ObjectModel;
namespace NDToolsBox.TextSearch
{
    /// <summary>
    /// ToolbarsV.xaml 的交互逻辑
    /// </summary>
    public partial class ToolbarsV : UserControl
    {
        private toolbarsViewModle _itemlist;
        private object draggedItem;
        private int insertionIndex;
        private int RightButtonDown_item_index = -1;
        private float list_box_item_height = 16f;//每个元素的高度

        private MyCallbackRangeChange m_rangechange;
        private GlobalDelegates.Delegate5 m_deleg;

        private ToolBarTabsConfig _tabsConfig;
        private bool _suppressTabSelection;

        public ToolbarsV()
        {
            
            InitializeComponent();
            SetMaxUiColor();

            

            _itemlist = CfgHelpPersonXml.ReadToolBarItem(WebAddress.ToolBarItemConfig);

            if (_itemlist == null)
            {
                _itemlist = new toolbarsViewModle();
                _itemlist.NewTools();
            }
            else {
                _itemlist.NewTools();
                _itemlist.Set_Items_Margin_Up();
            }
            _itemlist.TimeEnd = ScriptsUtilities.AnimEnd();
            _itemlist.TimeStart = ScriptsUtilities.AnimStart();

            base.DataContext = _itemlist;

            
            sp_start.EnterSpinMode();

            sp_start.ValueChanged += sp_start_ValueChanged;
            sp_end.ValueChanged += sp_end_ValueChanged;

            m_rangechange = new MyCallbackRangeChange();
#if M2015 || M2016
            m_deleg = new GlobalDelegates.Delegate5(Test_Delegate6_Callback);
#else
            m_deleg = new GlobalDelegates.Delegate5(Test_Delegate5_Callback);
#endif

            ScriptsUtilities.global.RegisterNotification(m_deleg, null, SystemNotificationCode.TimerangeChange);

            this.Unloaded += ToolbarsV_Unloaded;

            LoadCustomTabs();
        }

        private void LoadCustomTabs()
        {
            _suppressTabSelection = true;
            try
            {
                LoadCustomTabsCore();
            }
            finally
            {
                _suppressTabSelection = false;
            }
        }

        private void LoadCustomTabsCore()
        {
            _tabsConfig = CfgHelpPersonXml.ReadToolBarTabs(WebAddress.ToolBarTabsConfig);
            if (_tabsConfig == null)
            {
                _tabsConfig = new ToolBarTabsConfig();
            }
            bool migrated = false;
            foreach (CustomToolbarTab tab in _tabsConfig.Tabs)
            {
                if (tab == null || string.IsNullOrEmpty(tab.Id))
                {
                    continue;
                }
                string beforeListId = tab.ListId;
                tab.Normalize();
                if (string.IsNullOrEmpty(tab.Header))
                {
                    tab.Header = "自定义";
                }
                InsertCustomTabItem(tab, MainTabControl.Items.Count - 1);
                if (!string.Equals(beforeListId, tab.ListId, StringComparison.Ordinal))
                {
                    migrated = true;
                }
            }
            if (migrated)
            {
                SaveTabsConfig();
            }
        }

        /// <summary>
        /// 清空已有自定义 Tab 后从 ToolBarTabs.xml 重新加载；返回匹配 listId 的 TabItem（可空）。
        /// </summary>
        private TabItem ReloadCustomTabs(string selectListId = null)
        {
            _suppressTabSelection = true;
            TabItem selected = null;
            try
            {
                // 只移除自定义 Tab（Tag 为 CustomToolbarTab），保留「动画」「绑定」「+」等内置页
                for (int i = MainTabControl.Items.Count - 1; i >= 0; i--)
                {
                    var item = MainTabControl.Items[i] as TabItem;
                    if (item != null && item.Tag is CustomToolbarTab)
                    {
                        MainTabControl.Items.RemoveAt(i);
                    }
                }

                LoadCustomTabsCore();

                if (!string.IsNullOrEmpty(selectListId))
                {
                    foreach (TabItem item in MainTabControl.Items)
                    {
                        var data = item != null ? item.Tag as CustomToolbarTab : null;
                        if (data != null &&
                            string.Equals(data.ListId, selectListId, StringComparison.OrdinalIgnoreCase))
                        {
                            selected = item;
                            MainTabControl.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            finally
            {
                _suppressTabSelection = false;
            }
            return selected;
        }

        private TabItem InsertCustomTabItem(CustomToolbarTab tabData, int insertIndex)
        {
            var panel = new Grid
            {
                Width = 74,
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                AllowDrop = true,
                MinHeight = 120
            };
            panel.GotKeyboardFocus += dockpanel_GotKeyboardFocus;
            panel.LostKeyboardFocus += dockpanel_LostKeyboardFocus;
            panel.IsEnabledChanged += dockpanel_IsEnabledChanged;
            panel.DragEnter += CustomTabPanel_DragEnter;
            panel.DragOver += CustomTabPanel_DragOver;
            panel.Drop += CustomTabPanel_Drop;

            tabData.Normalize();
            var lists = new NDCustomTabLists();
            lists.InitWithSaveName(tabData.ListId);
            lists.HorizontalAlignment = HorizontalAlignment.Stretch;
            lists.VerticalAlignment = VerticalAlignment.Stretch;
            // 旧 ListId（*_1）合并迁移后，同步为统一文件名（无 _1 后缀）
            if (lists.TabLists != null && !string.IsNullOrEmpty(lists.TabLists.SavePath))
            {
                string canonical = System.IO.Path.GetFileNameWithoutExtension(lists.TabLists.SavePath);
                if (!string.IsNullOrEmpty(canonical) &&
                    !string.Equals(tabData.ListId, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    tabData.ListId = canonical;
                }
            }
            panel.Children.Add(lists);

            var tabItem = new TabItem
            {
                Header = string.IsNullOrEmpty(tabData.Header) ? "自定义" : tabData.Header,
                Width = 14,
                Height = 60,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Style = (Style)FindResource("TabItemStyle"),
                Background = (Brush)FindResource("ButtonColor_B"),
                Content = panel,
                Tag = tabData
            };
            // 不要设 tabItem.ContextMenu：Content 是 TabItem 逻辑子级，右键列表时会弹出 Tab 的「重命名/删除」而不是完整 dynamicContextMenu
            ContextMenu headerMenu = CreateCustomTabContextMenu(tabItem);
            tabItem.Resources["customTabHeaderContextMenu"] = headerMenu;
            tabItem.PreviewMouseRightButtonUp += CustomTabItem_PreviewMouseRightButtonUp;

            if (insertIndex < 0 || insertIndex > MainTabControl.Items.Count)
            {
                MainTabControl.Items.Add(tabItem);
            }
            else
            {
                MainTabControl.Items.Insert(insertIndex, tabItem);
            }
            return tabItem;
        }

        private void CustomTabPanel_DragEnter(object sender, DragEventArgs e)
        {
            SetToolbarDropEffects(e);
        }

        private void CustomTabPanel_DragOver(object sender, DragEventArgs e)
        {
            SetToolbarDropEffects(e);
        }

        private void CustomTabPanel_Drop(object sender, DragEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null || panel.Children.Count == 0)
            {
                return;
            }
            var lists = panel.Children[0] as NDCustomTabLists;
            if (lists != null)
            {
                lists.AcceptDrop(e);
            }
        }

        private ContextMenu CreateCustomTabContextMenu(TabItem tabItem)
        {
            var menu = new ContextMenu
            {
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                Foreground = (Brush)FindResource("MaxTextColor")
            };
            var rename = new System.Windows.Controls.MenuItem
            {
                Header = "重命名",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            rename.Click += (s, e) => RenameCustomTab(tabItem);
            var delete = new System.Windows.Controls.MenuItem
            {
                Header = "删除",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            delete.Click += (s, e) => DeleteCustomTab(tabItem);
            var export = new System.Windows.Controls.MenuItem
            {
                Header = "导出 CustomTab…",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            export.Click += (s, e) => ExportCustomTab(tabItem);
            menu.Items.Add(rename);
            menu.Items.Add(delete);
            menu.Items.Add(new Separator());
            menu.Items.Add(export);
            return menu;
        }

        private void CustomTabItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tabItem = sender as TabItem;
            if (tabItem == null)
            {
                return;
            }
            // 仅标题区域在 TabItem 视觉树内；内容在 ContentPresenter 下，不应打开 Tab 菜单
            var src = e.OriginalSource as DependencyObject;
            if (src == null || !IsVisualDescendantOf(src, tabItem))
            {
                return;
            }
            var menu = tabItem.Resources["customTabHeaderContextMenu"] as ContextMenu;
            if (menu == null)
            {
                return;
            }
            menu.PlacementTarget = tabItem;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private static bool IsVisualDescendantOf(DependencyObject child, DependencyObject ancestor)
        {
            DependencyObject current = child;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void SaveTabsConfig()
        {
            if (_tabsConfig == null)
            {
                _tabsConfig = new ToolBarTabsConfig();
            }
            CfgHelpPersonXml.SaveToolBarTabs(_tabsConfig, WebAddress.ToolBarTabsConfig);
        }

        private string NextCustomTabHeader()
        {
            int n = 1;
            if (_tabsConfig != null && _tabsConfig.Tabs != null)
            {
                n = _tabsConfig.Tabs.Count + 1;
            }
            return "自定义" + n;
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTabSelection || MainTabControl == null || AddTabItem == null)
            {
                return;
            }
            // 「+」仅作入口，不允许保持选中；真正新建走左键 Preview
            if (ReferenceEquals(MainTabControl.SelectedItem, AddTabItem))
            {
                _suppressTabSelection = true;
                try
                {
                    if (MainTabControl.Items.Count > 1)
                    {
                        MainTabControl.SelectedIndex = Math.Max(0, MainTabControl.Items.Count - 2);
                    }
                }
                finally
                {
                    _suppressTabSelection = false;
                }
            }
        }

        private void AddTabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CreateNewCustomTab();
        }

        private void AddTabItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var menu = new ContextMenu
            {
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                Foreground = (Brush)FindResource("MaxTextColor")
            };
            var create = new System.Windows.Controls.MenuItem
            {
                Header = "新建 Tab",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            create.Click += (s, args) => CreateNewCustomTab();
            var import = new System.Windows.Controls.MenuItem
            {
                Header = "导入 CustomTab…",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            import.Click += (s, args) => ImportCustomTabFromFile();
            menu.Items.Add(create);
            menu.Items.Add(import);
            menu.PlacementTarget = AddTabItem;
            menu.IsOpen = true;
        }

        private void CreateNewCustomTab()
        {
            if (_tabsConfig == null)
            {
                _tabsConfig = new ToolBarTabsConfig();
            }
            _suppressTabSelection = true;
            try
            {
                CustomToolbarTab tabData = CustomToolbarTab.CreateNew(NextCustomTabHeader());
                if (_tabsConfig.Tabs == null)
                {
                    _tabsConfig.Tabs = new System.Collections.Generic.List<CustomToolbarTab>();
                }
                _tabsConfig.Tabs.Add(tabData);
                SaveTabsConfig();

                int insertIndex = MainTabControl.Items.IndexOf(AddTabItem);
                TabItem newItem = InsertCustomTabItem(tabData, insertIndex);
                MainTabControl.SelectedItem = newItem;
            }
            finally
            {
                _suppressTabSelection = false;
            }
        }

        /// <summary>
        /// 导入 CustomTab：先校验 xml 合法，再注册 ToolBarTabs，最后整表重新加载并选中。
        /// ListId = 文件名；文件不在安装目录时按原名复制过去。
        /// </summary>
        private void ImportCustomTabFromFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "导入 CustomTab",
                Filter = "CustomTab XML (*.xml)|*.xml|All files (*.*)|*.*",
                CheckFileExists = true,
                InitialDirectory = WebAddress.apppath
            };
            if (dlg.ShowDialog() != true)
            {
                return;
            }

            string srcPath = dlg.FileName;
            string listId = System.IO.Path.GetFileNameWithoutExtension(srcPath);
            string validateError;
            CustomTabListsViewModle loaded =
                CustomTabListsViewModle.TryValidateAndLoadForImport(srcPath, out validateError);
            if (loaded == null)
            {
                System.Windows.MessageBox.Show(
                    "CustomTab XML 不合法，未注册。\n" + (validateError ?? "未知错误"),
                    "导入 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_tabsConfig == null)
            {
                _tabsConfig = new ToolBarTabsConfig();
            }
            if (_tabsConfig.Tabs == null)
            {
                _tabsConfig.Tabs = new System.Collections.Generic.List<CustomToolbarTab>();
            }

            // 已注册：仍重新加载并选中，保证 UI 与磁盘一致
            CustomToolbarTab already = null;
            for (int i = 0; i < _tabsConfig.Tabs.Count; i++)
            {
                CustomToolbarTab t = _tabsConfig.Tabs[i];
                if (t != null && string.Equals(t.ListId, listId, StringComparison.OrdinalIgnoreCase))
                {
                    already = t;
                    break;
                }
            }
            if (already != null)
            {
                EnsureListXmlInAppPath(srcPath, listId);
                ReloadCustomTabs(listId);
                System.Windows.MessageBox.Show(
                    "该 CustomTab 已在 ToolBarTabs 中注册，已重新加载。",
                    "导入 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!EnsureListXmlInAppPath(srcPath, listId))
            {
                return;
            }

            // 再校验安装目录中的最终文件（复制后）
            string destPath = System.IO.Path.Combine(WebAddress.apppath, listId + ".xml");
            string destError;
            if (CustomTabListsViewModle.TryValidateAndLoadForImport(destPath, out destError) == null)
            {
                System.Windows.MessageBox.Show(
                    "安装目录中的列表文件校验失败，未注册。\n" + (destError ?? "未知错误"),
                    "导入 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string header = UiPrompt.PromptText("导入 CustomTab", "Tab 显示名称：", listId);
            if (string.IsNullOrWhiteSpace(header))
            {
                return;
            }
            header = header.Trim();

            var tabData = new CustomToolbarTab
            {
                Id = Guid.NewGuid().ToString("N"),
                Header = header,
                ListId = listId
            };
            _tabsConfig.Tabs.Add(tabData);
            SaveTabsConfig();

            ReloadCustomTabs(listId);
        }

        /// <summary>
        /// 若源文件不在安装目录，复制为 {apppath}\{listId}.xml。失败弹窗并返回 false。
        /// </summary>
        private bool EnsureListXmlInAppPath(string srcPath, string listId)
        {
            string destPath = System.IO.Path.Combine(WebAddress.apppath, listId + ".xml");
            string apppathFull = System.IO.Path.GetFullPath(WebAddress.apppath)
                .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string srcDirFull = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(srcPath) ?? string.Empty)
                .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            if (string.Equals(srcDirFull, apppathFull, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            try
            {
                if (!Directory.Exists(WebAddress.apppath))
                {
                    Directory.CreateDirectory(WebAddress.apppath);
                }
                File.Copy(srcPath, destPath, true);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "复制到安装目录失败：\n" + ex.Message,
                    "导入 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private void ExportCustomTab(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }
            var tabData = tabItem.Tag as CustomToolbarTab;
            if (tabData == null)
            {
                return;
            }
            tabData.Normalize();
            string srcPath = System.IO.Path.Combine(WebAddress.apppath, tabData.ListId + ".xml");
            if (!File.Exists(srcPath))
            {
                System.Windows.MessageBox.Show(
                    "未找到列表配置：\n" + srcPath,
                    "导出 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出 CustomTab",
                Filter = "CustomTab XML (*.xml)|*.xml",
                FileName = tabData.ListId + ".xml",
                AddExtension = true,
                DefaultExt = ".xml"
            };
            if (dlg.ShowDialog() != true)
            {
                return;
            }
            try
            {
                File.Copy(srcPath, dlg.FileName, true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "导出失败：\n" + ex.Message,
                    "导出 CustomTab",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // XAML 资源菜单保留兼容；实际自定义 Tab 使用闭包绑定到 TabItem
        private void CustomTabRename_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var menu = menuItem != null ? menuItem.Parent as ContextMenu : null;
            var tabItem = menu != null ? menu.PlacementTarget as TabItem : null;
            RenameCustomTab(tabItem);
        }

        private void CustomTabDelete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var menu = menuItem != null ? menuItem.Parent as ContextMenu : null;
            var tabItem = menu != null ? menu.PlacementTarget as TabItem : null;
            DeleteCustomTab(tabItem);
        }

        private void RenameCustomTab(TabItem tabItem)
        {
            if (tabItem == null)
            {
                return;
            }
            var tabData = tabItem.Tag as CustomToolbarTab;
            if (tabData == null)
            {
                return;
            }

            string newName = UiPrompt.PromptText("重命名 Tab", "请输入新名称：", tabData.Header);
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }
            newName = newName.Trim();
            tabData.Header = newName;
            tabItem.Header = newName;
            SaveTabsConfig();
        }

        private void DeleteCustomTab(TabItem tabItem)
        {
            if (tabItem == null || MainTabControl == null)
            {
                return;
            }
            var tabData = tabItem.Tag as CustomToolbarTab;
            if (tabData == null)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show(
                "确定删除 Tab「" + tabData.Header + "」及其脚本列表配置？",
                "删除 Tab",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (_tabsConfig != null && _tabsConfig.Tabs != null)
            {
                _tabsConfig.Tabs.RemoveAll(t => t != null && t.Id == tabData.Id);
            }
            SaveTabsConfig();

            foreach (string listId in tabData.ConfigFileNames())
            {
                try
                {
                    string path = System.IO.Path.Combine(WebAddress.apppath, listId + ".xml");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                }
            }

            _suppressTabSelection = true;
            try
            {
                int removeIndex = MainTabControl.Items.IndexOf(tabItem);
                MainTabControl.Items.Remove(tabItem);
                if (MainTabControl.Items.Count > 1)
                {
                    int selectIndex = Math.Max(0, Math.Min(removeIndex, MainTabControl.Items.Count - 2));
                    MainTabControl.SelectedIndex = selectIndex;
                }
            }
            finally
            {
                _suppressTabSelection = false;
            }
        }

        private void ToolbarsV_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= ToolbarsV_Unloaded;
            if (m_deleg != null)
            {
                ScriptsUtilities.global.UnRegisterNotification(m_deleg, null, SystemNotificationCode.TimerangeChange);
                m_deleg = null;
            }
        }

        private void Test_Delegate5_Callback(IntPtr param0, INotifyInfo param1)
        { 
            //ScriptsUtilities.print("Test_Delegate5_Callback");
            _itemlist.TimeStart = ScriptsUtilities.ip4.AnimRange.Start / ScriptsUtilities.global.TicksPerFrame;
            _itemlist.TimeEnd = ScriptsUtilities.ip4.AnimRange.End / ScriptsUtilities.global.TicksPerFrame;
        }
        private void Test_Delegate6_Callback(IntPtr param0, IntPtr param1)
        {
            /*INotifyInfo inf = ScriptsUtilities.global.NotifyInfo.Marshal(param1);
            ScriptsUtilities.print(inf.CallParam.ToString());*/
            _itemlist.TimeStart = ScriptsUtilities.ip4.AnimRange.Start / ScriptsUtilities.global.TicksPerFrame;
            _itemlist.TimeEnd = ScriptsUtilities.ip4.AnimRange.End / ScriptsUtilities.global.TicksPerFrame;

        }

       
        
        private void SetMaxUiColor()
        {
            //this.Resources["MaxTextColor"] = new SolidColorBrush(ScriptsUtilities.GetTextColor());
            //this.Resources["MaxUiBackgroundColor"] = new SolidColorBrush(ScriptsUtilities.GetMaxBackgroundColor());
            
            //不管用
            //this.Resources["MaxUiBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ScriptsUtilities.GetMaxBackgroundColor_Str()));

            /*this.Resources["ButtonColor_A"] = new SolidColorBrush(ScriptsUtilities.mGetColor(1));
            this.Resources["ButtonColor_B"] = new SolidColorBrush(ScriptsUtilities.mGetColor(2));
            this.Resources["ButtonColor_C"] = new SolidColorBrush(ScriptsUtilities.mGetColor(3));
            this.Resources["ButtonColor_D"] = new SolidColorBrush(ScriptsUtilities.mGetColor(4));*/
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button bt = sender as System.Windows.Controls.Button;
            toolbarItemViewModle item = (toolbarItemViewModle)bt.DataContext;
            if (item.Path != null && (!string.IsNullOrEmpty(item.Path)))
            {
                ScriptsUtilities.FileinMxs(item.Path);
            } else if (!string.IsNullOrEmpty(item.Commit))
            {
                ScriptsUtilities.ExecuteMAXScriptScript(item.Commit);
            }
        }
        private void SelSetButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button bt = sender as System.Windows.Controls.Button;
            SelectSetItem item = (SelectSetItem)bt.DataContext;
            //ScriptsUtilities.print(item.Name);
            if (System.Windows.Input.Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down)
            {
                ScriptsUtilities.SeleSet(item.Index, true);
            }
            else
            {
                ScriptsUtilities.SeleSet(item.Index, false);
            }

        }
        private void MyListBox_DragEnter(object sender, DragEventArgs e)
        {
            SetToolbarDropEffects(e);
        }

        private void MyListBox_DragOver(object sender, DragEventArgs e)
        {
            SetToolbarDropEffects(e);
        }

        private static void SetToolbarDropEffects(DragEventArgs e)
        {
            if (e == null)
            {
                return;
            }
            if (e.Data != null &&
                (e.Data.GetDataPresent(DataFormats.UnicodeText) || e.Data.GetDataPresent(DataFormats.FileDrop)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private int HitListBox(Point pos)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(MyListBox, pos);
            if (hitTestResult != null && hitTestResult.VisualHit is FrameworkElement element)
            {
                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>(element);

                if (listBoxItem != null)
                {
                    draggedItem = listBoxItem.DataContext;
                    insertionIndex = MyListBox.Items.IndexOf(draggedItem);
                    return insertionIndex;
                }
            }
            return -1;
        }
        private int HitItemsControl(Point pos)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(MyItemsControl, pos);
            if (hitTestResult != null && hitTestResult.VisualHit is FrameworkElement element)
            {
                Button item = FindAncestor<Button>(element);
                if (item != null)
                {
                    int index = MyItemsControl.Items.IndexOf(item.DataContext);
                    return index;
                }
            }
            return -1;
        }
        private void GetFiles(ref List<string> lists, string[] files)
        {
            foreach (string item in files)
            {
                if (File.Exists(item))
                {
                    string ext = System.IO.Path.GetExtension(item);
                    if (string.Equals(ext, ".ms", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ext, ".mse", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ext, ".py", StringComparison.OrdinalIgnoreCase))
                    {
                        lists.Add(item);
                    }
                }
            }
        }
        /// <summary>
        /// 拖拽过来事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyListBox_Drop(object sender, DragEventArgs e)
        {

            Point pos = e.GetPosition((UIElement)sender);
            

            int item_index = this.HitListBox(pos);
            ScriptsUtilities.print(item_index.ToString());

            //拖拽脚本文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> script_file = new List<string>();
                this.GetFiles(ref script_file, files);
                ScriptsUtilities.print(script_file.Count.ToString());

                if (item_index >= 0)
                {
                    for (int i = 0; i < script_file.Count; i++)
                    {
                        
                        _itemlist.AddNewFileItem(script_file[i], item_index + i);
                    }
                }
                else
                {
                    item_index = (int)(pos.Y / (list_box_item_height + 2.0f));
                    if (item_index <= _itemlist.Items.Count)
                    {
                        for (int i = 0; i < script_file.Count; i++)
                        {
                            _itemlist.AddNewFileItem(script_file[i], item_index + i);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < script_file.Count; i++)
                        {
                            _itemlist.AddNewFileItem(script_file[i], -1);
                        }
                    }

                }

            }
            //拖拽 ndbox 中的脚本项 / 纯文本
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                var str = e.Data.GetData(DataFormats.UnicodeText);
                if (str != null
                    && toolbarItemViewModle.TryResolveScriptText((string)str, out string name, out string path, out string commit))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        _itemlist.AddNewFileItem(path, item_index);
                    }
                    else
                    {
                        _itemlist.AddNewCommitItem(commit, item_index, name);
                    }
                }
            }
        }
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        private void MyListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// 右键编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void dynamicSpeedContextMenu_Opened(object sender, RoutedEventArgs e)
        { 
        
        }
        private void dynamicContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;
            if (menu == null)
            {
                return;
            }
            var target = menu.PlacementTarget as FrameworkElement;
            if (target != null)
            {
                menu.DataContext = target.DataContext;
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                foreach (toolbarItemViewModle i in _itemlist.Items)
                {
                    i.IsEdit = false; 
                }

                foreach (toolbarItemViewModle i in _itemlist.SolidItems)
                {
                    i.IsEdit = false;
                }
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CfgHelpPersonXml.SaveXml(_itemlist, WebAddress.ToolBarItemConfig);

        }

        private void dockpanel_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptsUtilities.DisableAccelerators();
        }

        private void dockpanel_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptsUtilities.EnableAccelerators();
        }

        private void Button_Click_link(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.LinkNode();
        }
        private void Button_Click_Align(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.LinkNodeAlign();
        }
        private bool GetToggleButtonState(object sender)
        {
            System.Windows.Controls.Primitives.ToggleButton bt = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (bt != null)
            {
                if (bt.IsChecked == true) {
                    return true;
                }
            }
            return false;
        }
        private void SethideByCategory(int type, object sender)
        {
            ScriptsUtilities.hideByCategory(type, GetToggleButtonState(sender));
        }
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton bt = sender as System.Windows.Controls.Primitives.ToggleButton;

            toolbarItemViewModle item = (toolbarItemViewModle)bt.DataContext;
        }

        private void ToggleButton_Checked_Mesh(object sender, RoutedEventArgs e)
        {
            SethideByCategory(1, sender);
        }
        private void ToggleButton_Checked_Shape(object sender, RoutedEventArgs e)
        {
            SethideByCategory(2, sender);
        }
        private void ToggleButton_Checked_Help(object sender, RoutedEventArgs e)
        {
            SethideByCategory(16, sender);

        }
        private void ToggleButton_Checked_Bone(object sender, RoutedEventArgs e)
        {
            SethideByCategory(256, sender);

        }
        private void ToggleButton_Checked_Light(object sender, RoutedEventArgs e)
        {
            SethideByCategory(4, sender);

        }

        private void ToggleButton_Checked_Camera(object sender, RoutedEventArgs e)
        {
            SethideByCategory(8, sender);
        }

        private void ToggleButton_Checked_bip(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.HideCSBip(GetToggleButtonState(sender));
        }
        private void ToggleButton_Checked_onlyBone(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.HideBone(GetToggleButtonState(sender));
        }

        private void ToggleButton_Checked_box(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.ExecuteMAXScriptScript("max box mode selected");
        }

        private void dockpanel_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScriptsUtilities.print("dockpanel_IsEnabledChanged f");
        }

        
        private void sp_start_ValueChanged(object sender, EventArgs e)
        {
                
            IInterval animtimeline = ScriptsUtilities.ip4.AnimRange;
            
            int new_start = (int)sp_start.Value;
            

            int old_start = animtimeline.Start / ScriptsUtilities.global.TicksPerFrame;
            int end = animtimeline.End / ScriptsUtilities.global.TicksPerFrame;

            if (new_start < end)
            {
                if (new_start != old_start)
                {
                    animtimeline.Start = new_start * ScriptsUtilities.global.TicksPerFrame;
                    ScriptsUtilities.ip4.AnimRange = animtimeline;
                }
            }
            else {
                sp_start.Value = end - 1;
            }
        }
        private void sp_end_ValueChanged(object sender, EventArgs e)
        {
            IInterval animtimeline = ScriptsUtilities.ip4.AnimRange;

            int new_end = (int)sp_end.Value;

            int old_end = animtimeline.End / ScriptsUtilities.global.TicksPerFrame;
            int start = animtimeline.Start / ScriptsUtilities.global.TicksPerFrame;

            if (new_end > start)
            {
                if (new_end != old_end)
                {
                    animtimeline.End = new_end * ScriptsUtilities.global.TicksPerFrame;
                    ScriptsUtilities.ip4.AnimRange = animtimeline;
                }
            }
            else
            {
                sp_end.Value = start + 1;
            }
        }

        private void inport_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //e.Effects = DragDropEffects.Copy;
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;

        }

        private void inport_Drop(object sender, DragEventArgs e)
        {
            //拖拽脚本文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> script_file = new List<string>();
                foreach (string item in files)
                {
                    if (File.Exists(item))
                    {
                        if (System.IO.Path.GetExtension(item).Equals(".fbx") || System.IO.Path.GetExtension(item).Equals(".FBX"))
                        {
                            //Console.WriteLine(item);
                            ScriptsUtilities.ImportFBX(item);
                        }

                    }
                }
            }
        }

        private void ItemsControl_Drop(object sender, DragEventArgs e)
        {
            Point pos = e.GetPosition((UIElement)sender);
            
            int item_index = this.HitItemsControl(pos);
            
            //拖拽脚本文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> script_file = new List<string>();
                this.GetFiles(ref script_file, files);
                if (item_index >= 0)
                {
                    for (int i = 0; i < script_file.Count; i++)
                    {
                        _itemlist.AddNewFileItem(_itemlist.SolidItems, script_file[i], item_index + i);
                    }
                    _itemlist.Set_Items_Margin_Up();
                }
            }
            //拖拽 ndbox 中的脚本项 / 纯文本
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                var str = e.Data.GetData(DataFormats.UnicodeText);
                if (str != null
                    && toolbarItemViewModle.TryResolveScriptText((string)str, out string name, out string path, out string commit))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        _itemlist.AddNewFileItem(_itemlist.SolidItems, path, item_index);
                    }
                    else
                    {
                        _itemlist.AddNewCommitItem(_itemlist.SolidItems, commit, item_index, name);
                    }
                    _itemlist.Set_Items_Margin_Up();
                }
            }
        }
        
        private void TextBlock_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        public int playSpeedType = 3;
        private void MySpeedButton_Click(object sender, EventArgs e)
        {
            ScriptsUtilities.PlayAnim(false);
            if (MySpeedButton.IsChecked == true)
            {
                ScriptsUtilities.timeConfiguration(playSpeedType);
                ScriptsUtilities.PlayAnim(true);
            }
        }
        private void MySpeedButton_S1_Click(object sender, EventArgs e)
        {
            MySpeedButton.Content = "Speed: 1/4x"; playSpeedType = 1;
            ScriptsUtilities.timeConfiguration(1);

        }
        private void MySpeedButton_S2_Click(object sender, EventArgs e)
        {
            MySpeedButton.Content = "Speed: 1/2x"; playSpeedType = 2;
            ScriptsUtilities.timeConfiguration(2);

        }
        private void MySpeedButton_S3_Click(object sender, EventArgs e)
        {
            MySpeedButton.Content = "Speed: 1x"; playSpeedType = 3;
            ScriptsUtilities.timeConfiguration(3);

        }
        private void MySpeedButton_S4_Click(object sender, EventArgs e)
        {
            MySpeedButton.Content = "Speed: 2x"; playSpeedType = 4;
            ScriptsUtilities.timeConfiguration(4);

        }
        private void MySpeedButton_S5_Click(object sender, EventArgs e)
        {
            MySpeedButton.Content = "Speed: 4x"; playSpeedType = 5;
            ScriptsUtilities.timeConfiguration(5);

        }
    }
}
