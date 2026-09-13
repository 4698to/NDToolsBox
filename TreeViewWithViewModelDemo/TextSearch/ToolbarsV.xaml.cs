using MaxToolbars.Toobars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    
    /// <summary>侧边工具栏宽度分档。</summary>
    public enum ToolbarWidthTier
    {
        /// <summary>默认窄栏：单列，按钮约 64。</summary>
        Default = 0,
        /// <summary>单列加宽：按钮最宽 OneColumnButtonMaxWidth。</summary>
        OneColumnWide = 1,
        /// <summary>双列：两列约 64 宽按钮。</summary>
        TwoColumn = 2
    }

    /// <summary>
    /// ToolbarsV.xaml 的交互逻辑
    /// </summary>
    public partial class ToolbarsV : UserControl
    {
        /// <summary>
        /// 「1 列宽」档：单个脚本按钮的最大宽度（像素）。
        /// 内容区更宽时按钮不再继续拉长，避免单列按钮过扁。
        /// </summary>
        private const double OneColumnButtonMaxWidth = 250;

        /// <summary>
        /// 侧边栏停靠宽度上限（像素）。
        /// 作用于本控件与宿主窗口的 MaxWidth；用户用分隔条拉宽时不会超过此值。
        /// </summary>
        private const double DockConstraintsPanelMax = 280;

        /// <summary>
        /// 「默认」档停靠最小宽度（像素）：单列窄栏。
        /// 切换到该档时写入 MinWidth，并可作为强制收窄时的目标宽度。
        /// </summary>
        private const double DockConstraintsPanelMinDefault = 96;

        /// <summary>
        /// 「1 列宽」档停靠最小宽度（像素）：单列加宽，容纳更宽按钮（见 OneColumnButtonMaxWidth）。
        /// </summary>
        private const double DockConstraintsPanelMinOneColumnWide = 96;

        /// <summary>
        /// 「2 列」档停靠最小宽度（像素）：双列按钮并排所需的最小栏宽。
        /// </summary>
        private const double DockConstraintsPanelMinTwoColumn = 168;

        public static readonly DependencyProperty ScriptButtonWidthProperty =
            DependencyProperty.Register(
                "ScriptButtonWidth",
                typeof(double),
                typeof(ToolbarsV),
                new FrameworkPropertyMetadata(64.0));

        /// <summary>脚本列表按钮宽度（随宽度分档变化）。</summary>
        public double ScriptButtonWidth
        {
            get { return (double)GetValue(ScriptButtonWidthProperty); }
            set { SetValue(ScriptButtonWidthProperty, value); }
        }

        private toolbarsViewModle _itemlist;
        private object draggedItem;
        private int insertionIndex;
        private int RightButtonDown_item_index = -1;
        private float list_box_item_height = 16f;//每个元素的高度

        private MyCallbackRangeChange m_rangechange;
        private GlobalDelegates.Delegate5 m_deleg;

        private ToolBarTabsConfig _tabsConfig;
        private bool _suppressTabSelection;
        private const string BuiltinAnimKey = "anim";
        private const string BuiltinRigKey = "rig";
        private const string TabWidthKeyResource = "TabWidthKey";
        /// <summary>列表相对内容面板的左边距（与动画 Tab 一致）。</summary>
        private const double ContentLeftInset = 2;
        /// <summary>列表相对内容面板的右边距（与动画 Tab ListBox Margin 右 2 一致）。</summary>
        private const double ContentRightInset = 2;
        /// <summary>内容面板相对左侧 Tab 条的外边距；右侧不加，与动画 Tab dockpanel Margin=8,0,0,0 一致。</summary>
        private const double PanelLeftGutter = 8;
        /// <summary>左侧竖排 Tab 条大约占用宽度（用于回退测量）。</summary>
        private const double TabStripApproxWidth = 24;
        /// <summary>窄按钮列表（SolidItems）最多列数。</summary>
        private const int SolidItemsMaxColumns = 4;
        /// <summary>窄按钮单格最小宽度，用于按可用宽度推算列数。</summary>
        private const double SolidItemsMinCellWidth = 32;
        /// <summary>内置 Tab（动画/绑定）的栏宽分档；自定义 Tab 存在 CustomToolbarTab.WidthTier。</summary>
        private readonly Dictionary<string, ToolbarWidthTier> _builtinWidthTiers =
            new Dictionary<string, ToolbarWidthTier>(StringComparer.OrdinalIgnoreCase);
        /// <summary>侧栏内容区上次有效宽度（切 Tab 首帧 ActualWidth 常未就绪时作回退）。</summary>
        private double _lastContentWidth;
        /// <summary>合并同一帧内多次 Schedule，避免拖拽改宽时连刷。</summary>
        private int _layoutRefreshGeneration;
        private double _lastAppliedAvail = -1;
        private ToolbarWidthTier _lastAppliedSelectedTier;
        private TabItem _lastAppliedSelectedTab;

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
            LoadBuiltinWidthTiers();
            AttachBuiltinTabHeaderMenus();
            ApplyDockConstraints(GetWidthTier(GetSelectedContentTab()), forceHostWidth: false);
            ScheduleRefreshScriptButtonLayout();
            Loaded += ToolbarsV_Loaded;
            // 只监听控件自身宽度；勿再挂 dock/rig SizeChanged，否则一次拖拽会触发多次全量刷新
            SizeChanged += ToolbarsV_SizeChanged;
        }

        private void ToolbarsV_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureTabStripAboveContent();
            ScheduleRefreshScriptButtonLayout();
        }

        /// <summary>
        /// Tab 头负边距会伸进内容列；把 HeaderPanel 提到内容之上，避免 Tab 被盖住。
        /// </summary>
        private void EnsureTabStripAboveContent()
        {
            if (MainTabControl == null)
            {
                return;
            }
            FrameworkElement header = FindNamedDescendant(MainTabControl, "HeaderPanel");
            if (header != null)
            {
                Panel.SetZIndex(header, 10);
            }
            FrameworkElement content = FindNamedDescendant(MainTabControl, "ContentPanel");
            if (content != null)
            {
                Panel.SetZIndex(content, 0);
                content.ClipToBounds = true;
            }
        }

        private static FrameworkElement FindNamedDescendant(DependencyObject parent, string name)
        {
            if (parent == null)
            {
                return null;
            }
            int n = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var fe = child as FrameworkElement;
                if (fe != null && string.Equals(fe.Name, name, StringComparison.Ordinal))
                {
                    return fe;
                }
                FrameworkElement nested = FindNamedDescendant(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private void ToolbarsV_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                // 拖拽改宽：只排队延迟刷新，避免每像素同步全量遍历
                ScheduleRefreshScriptButtonLayout(immediate: false);
            }
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
                MinWidth = 74,
                Margin = new Thickness(PanelLeftGutter, 0, 0, 0),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
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
                Tag = tabData,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
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
            Dispatcher.BeginInvoke(new Action(() => ScheduleRefreshScriptButtonLayout(immediate: true)),
                System.Windows.Threading.DispatcherPriority.Loaded);
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

        /// <summary>
        /// 内置 Tab（动画/绑定）标题右键：仅「栏宽」。自定义 Tab 在 Insert 时已挂完整菜单。
        /// </summary>
        private void AttachBuiltinTabHeaderMenus()
        {
            if (MainTabControl == null)
            {
                return;
            }
            foreach (var obj in MainTabControl.Items)
            {
                var tabItem = obj as TabItem;
                if (tabItem == null || ReferenceEquals(tabItem, AddTabItem))
                {
                    continue;
                }
                if (tabItem.Tag is CustomToolbarTab)
                {
                    continue;
                }
                if (tabItem.Resources.Contains("customTabHeaderContextMenu"))
                {
                    continue;
                }
                string key = null;
                if (ReferenceEquals(tabItem.Content, dockpanel))
                {
                    key = BuiltinAnimKey;
                }
                else if (ReferenceEquals(tabItem.Content, rig_dockpanel))
                {
                    key = BuiltinRigKey;
                }
                if (key != null)
                {
                    tabItem.Resources[TabWidthKeyResource] = key;
                }
                var menu = new ContextMenu
                {
                    Background = (Brush)FindResource("MaxUiBackgroundColor"),
                    Foreground = (Brush)FindResource("MaxTextColor")
                };
                menu.Items.Add(CreateWidthTierSubMenu(tabItem));
                tabItem.Resources["customTabHeaderContextMenu"] = menu;
                tabItem.PreviewMouseRightButtonUp += CustomTabItem_PreviewMouseRightButtonUp;
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
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateWidthTierSubMenu(tabItem));
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

            TabItem selected = GetSelectedContentTab();
            if (selected != null)
            {
                ApplyDockConstraints(GetWidthTier(selected), forceHostWidth: false);
                // 切 Tab：先刷一次（可用缓存宽），再延迟刷真实 Arrange 结果
                ScheduleRefreshScriptButtonLayout(immediate: true);
            }
        }

        /// <summary>
        /// 合并刷新：同一轮拖拽/连点只保留最新一次延迟回调，避免 Loaded+Idle 叠成 3N 次全量布局。
        /// </summary>
        /// <param name="immediate">true=同步先刷（切 Tab）；false=仅延迟（拖拽改宽）。</param>
        private void ScheduleRefreshScriptButtonLayout(bool immediate = true)
        {
            int gen = ++_layoutRefreshGeneration;
            if (immediate)
            {
                RefreshScriptButtonLayout();
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (gen != _layoutRefreshGeneration)
                {
                    return;
                }
                RefreshScriptButtonLayout();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (gen != _layoutRefreshGeneration)
                {
                    return;
                }
                RefreshScriptButtonLayout();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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
            menu.Items.Add(new Separator());
            // 「+」上的栏宽作用于当前选中的内容 Tab
            menu.Items.Add(CreateWidthTierSubMenu(null));
            menu.PlacementTarget = AddTabItem;
            menu.IsOpen = true;
        }

        private TabItem GetSelectedContentTab()
        {
            if (MainTabControl == null)
            {
                return null;
            }
            var sel = MainTabControl.SelectedItem as TabItem;
            if (sel == null || ReferenceEquals(sel, AddTabItem))
            {
                return null;
            }
            return sel;
        }

        private System.Windows.Controls.MenuItem CreateWidthTierSubMenu(TabItem targetTab)
        {
            var widthMenu = new System.Windows.Controls.MenuItem
            {
                Header = "栏宽",
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            widthMenu.SubmenuOpened += (s, e) =>
                PopulateWidthTierMenu(widthMenu, targetTab ?? GetSelectedContentTab());
            PopulateWidthTierMenu(widthMenu, targetTab ?? GetSelectedContentTab());
            return widthMenu;
        }

        private void PopulateWidthTierMenu(System.Windows.Controls.MenuItem widthMenu, TabItem targetTab)
        {
            ToolbarWidthTier current = GetWidthTier(targetTab);
            widthMenu.Items.Clear();
            widthMenu.Items.Add(CreateWidthTierMenuItem("默认（单列）", ToolbarWidthTier.Default, targetTab, current));
            widthMenu.Items.Add(CreateWidthTierMenuItem("1 列宽（按钮至 120）", ToolbarWidthTier.OneColumnWide, targetTab, current));
            widthMenu.Items.Add(CreateWidthTierMenuItem("2 列", ToolbarWidthTier.TwoColumn, targetTab, current));
        }

        private System.Windows.Controls.MenuItem CreateWidthTierMenuItem(
            string header, ToolbarWidthTier tier, TabItem targetTab, ToolbarWidthTier current)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = header,
                IsCheckable = true,
                IsChecked = current == tier,
                Background = (Brush)FindResource("MaxUiBackgroundColor"),
                BorderThickness = new Thickness(0)
            };
            item.Click += (s, args) =>
            {
                TabItem tab = targetTab ?? GetSelectedContentTab();
                if (tab != null)
                {
                    ApplyWidthTier(tab, tier, true);
                }
            };
            return item;
        }

        private ToolbarWidthTier GetWidthTier(TabItem tab)
        {
            if (tab == null)
            {
                return ToolbarWidthTier.Default;
            }
            var custom = tab.Tag as CustomToolbarTab;
            if (custom != null)
            {
                return NormalizeTier(custom.WidthTier);
            }
            string key = GetBuiltinWidthKey(tab);
            ToolbarWidthTier tier;
            if (!string.IsNullOrEmpty(key) && _builtinWidthTiers.TryGetValue(key, out tier))
            {
                return tier;
            }
            return ToolbarWidthTier.Default;
        }

        private static ToolbarWidthTier NormalizeTier(int value)
        {
            if (Enum.IsDefined(typeof(ToolbarWidthTier), value))
            {
                return (ToolbarWidthTier)value;
            }
            return ToolbarWidthTier.Default;
        }

        private string GetBuiltinWidthKey(TabItem tab)
        {
            if (tab == null)
            {
                return null;
            }
            if (tab.Resources.Contains(TabWidthKeyResource))
            {
                return tab.Resources[TabWidthKeyResource] as string;
            }
            if (ReferenceEquals(tab.Content, dockpanel))
            {
                return BuiltinAnimKey;
            }
            if (ReferenceEquals(tab.Content, rig_dockpanel))
            {
                return BuiltinRigKey;
            }
            return null;
        }

        private void SetWidthTier(TabItem tab, ToolbarWidthTier tier, bool save)
        {
            if (tab == null)
            {
                return;
            }
            var custom = tab.Tag as CustomToolbarTab;
            if (custom != null)
            {
                custom.WidthTier = (int)tier;
                if (save)
                {
                    SaveTabsConfig();
                }
                return;
            }
            string key = GetBuiltinWidthKey(tab);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _builtinWidthTiers[key] = tier;
            if (save)
            {
                SaveBuiltinWidthTiers();
            }
        }

        /// <summary>
        /// 为指定 Tab 应用栏宽分档（每 Tab 独立）。选中该 Tab 时同步停靠最小宽度。
        /// </summary>
        private void ApplyWidthTier(TabItem tab, ToolbarWidthTier tier, bool save)
        {
            SetWidthTier(tab, tier, save);
            if (tab != null && ReferenceEquals(MainTabControl.SelectedItem, tab))
            {
                ApplyDockConstraints(tier, forceHostWidth: true);
            }
            _lastAppliedAvail = -1; // 强制下一帧按新档位重算
            ScheduleRefreshScriptButtonLayout(immediate: true);
        }

        private void ApplyDockConstraints(ToolbarWidthTier tier, bool forceHostWidth)
        {
            MinWidth = GetDockConstraintsPanelMin(tier);
            MaxWidth = DockConstraintsPanelMax;
            ClearValue(WidthProperty);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            try
            {
                Window host = Window.GetWindow(this);
                if (host != null)
                {
                    host.MinWidth = MinWidth;
                    host.MaxWidth = DockConstraintsPanelMax;
                    if (forceHostWidth)
                    {
                        host.Width = MinWidth;
                    }
                }
            }
            catch { }
        }

        private static double GetDockConstraintsPanelMin(ToolbarWidthTier tier)
        {
            switch (tier)
            {
                case ToolbarWidthTier.OneColumnWide:
                    return DockConstraintsPanelMinOneColumnWide;
                case ToolbarWidthTier.TwoColumn:
                    return DockConstraintsPanelMinTwoColumn;
                default:
                    return DockConstraintsPanelMinDefault;
            }
        }

        private static void GetLayoutMetrics(ToolbarWidthTier tier, double avail, out int cols, out double btnW)
        {
            cols = tier == ToolbarWidthTier.TwoColumn ? 2 : 1;
            const double colGap = 4;
            btnW = Math.Max(48, (avail - colGap * cols) / cols);
            if (tier == ToolbarWidthTier.OneColumnWide && btnW > OneColumnButtonMaxWidth)
            {
                btnW = OneColumnButtonMaxWidth;
            }
        }

        /// <summary>
        /// 自适应核心：只刷新当前选中 Tab + 底部栏（隐藏 Tab 在选中时再刷）。
        /// </summary>
        private void RefreshScriptButtonLayout()
        {
            TabItem selected = GetSelectedContentTab();
            ToolbarWidthTier selectedTier = GetWidthTier(selected);
            double selectedAvail = MeasureTabContentWidth(selected);

            // 宽度/档位/选中项未变则跳过（拖拽末帧与延迟回调常重复）
            if (ReferenceEquals(selected, _lastAppliedSelectedTab)
                && selectedTier == _lastAppliedSelectedTier
                && Math.Abs(selectedAvail - _lastAppliedAvail) < 0.5)
            {
                return;
            }
            _lastAppliedSelectedTab = selected;
            _lastAppliedSelectedTier = selectedTier;
            _lastAppliedAvail = selectedAvail;

            int selectedCols;
            double selectedBtnW;
            GetLayoutMetrics(selectedTier, selectedAvail, out selectedCols, out selectedBtnW);
            ScriptButtonWidth = selectedBtnW;

            ApplyBottomBarLayout(selectedAvail);

            if (selected != null)
            {
                ApplyTabContentLayout(selected, selectedTier);
            }
        }

        /// <summary>
        /// 底部 Speed/时间/导入：始终 1 列。
        /// 左右与上方列表对齐：Tab 条之后套 PanelLeftGutter+ContentLeftInset / ContentRightInset。
        /// </summary>
        private void ApplyBottomBarLayout(double availWidth)
        {
            double w = Math.Max(48, availWidth);

            // TabControl 总宽 - 内容 ActualWidth ≈ Tab条 + dockpanel.Margin.Left（已含 PanelLeftGutter），勿再加一遍 8
            double left = TabStripApproxWidth + PanelLeftGutter + ContentLeftInset;
            TabItem selected = GetSelectedContentTab();
            var content = selected != null ? selected.Content as FrameworkElement : null;
            if (MainTabControl != null && content != null && content.IsVisible && content.ActualWidth > 20
                && MainTabControl.ActualWidth > content.ActualWidth)
            {
                left = MainTabControl.ActualWidth - content.ActualWidth + ContentLeftInset;
            }

            if (BottomBarPanel != null)
            {
                BottomBarPanel.Margin = new Thickness(
                    left,
                    BottomBarPanel.Margin.Top,
                    ContentRightInset,
                    BottomBarPanel.Margin.Bottom);
                BottomBarPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            ApplyBottomBarItemWidth(MySpeedButton, w);
            ApplyBottomBarItemWidth(sp_start, w);
            ApplyBottomBarItemWidth(sp_end, w);
            ApplyBottomBarItemWidth(importA, w);
        }

        private static void ApplyBottomBarItemWidth(FrameworkElement fe, double width)
        {
            if (fe == null)
            {
                return;
            }
            BindingOperations.ClearBinding(fe, FrameworkElement.WidthProperty);
            BindingOperations.ClearBinding(fe, FrameworkElement.MaxWidthProperty);
            fe.MinWidth = 48;
            fe.Width = width;
            fe.MaxWidth = width;
            fe.HorizontalAlignment = HorizontalAlignment.Left;
            // 单列末项：无额外右边距（与列表末列 ColumnGap=0 一致）
            double top = fe.Margin.Top > 0 ? fe.Margin.Top : 1;
            double bottom = fe.Margin.Bottom > 0 ? fe.Margin.Bottom : 1;
            fe.Margin = new Thickness(0, top, 0, bottom);
        }

        private void ApplyTabContentLayout(TabItem tab, ToolbarWidthTier tier)
        {
            if (tab == null)
            {
                return;
            }
            int cols = tier == ToolbarWidthTier.TwoColumn ? 2 : 1;
            var host = tab.Content as FrameworkElement;
            double avail = MeasureHostContentWidth(host);
            if (ReferenceEquals(tab.Content, dockpanel))
            {
                ApplyListLayout(MyListBox, cols, tier, avail);
                ApplySolidItemsListLayout(avail);
                ApplyAnimUtilityButtonsLayout(avail);
                if (dockpanel != null)
                {
                    ApplyNdListBoxesLayout(dockpanel, cols, tier, avail);
                }
                return;
            }
            if (ReferenceEquals(tab.Content, rig_dockpanel))
            {
                if (rig_dockpanel != null)
                {
                    ApplyNdListBoxesLayout(rig_dockpanel, cols, tier, avail);
                }
                return;
            }
            var content = tab.Content as DependencyObject;
            if (content != null)
            {
                ApplyNdListBoxesLayout(content, cols, tier, avail);
            }
        }

        private double MeasureTabContentWidth(TabItem tab)
        {
            return MeasureHostContentWidth(tab != null ? tab.Content as FrameworkElement : null);
        }

        private double MeasureHostContentWidth(FrameworkElement panel)
        {
            if (panel == null)
            {
                panel = dockpanel ?? rig_dockpanel;
            }
            if (panel != null)
            {
                // 仅在尚未量到有效宽度时强制 UpdateLayout（同步布局很贵）
                if (panel.ActualWidth <= 20)
                {
                    panel.UpdateLayout();
                }
                if (panel.IsVisible && panel.ActualWidth > 20)
                {
                    double w = Math.Max(48, panel.ActualWidth - ContentLeftInset - ContentRightInset);
                    _lastContentWidth = w;
                    return w;
                }
            }

            // 切 Tab 首帧：用当前已选中且已布局的内容，或上次侧栏改宽后的有效宽度
            TabItem selected = GetSelectedContentTab();
            var selectedPanel = selected != null ? selected.Content as FrameworkElement : null;
            if (selectedPanel != null && !ReferenceEquals(selectedPanel, panel))
            {
                if (selectedPanel.ActualWidth <= 20)
                {
                    selectedPanel.UpdateLayout();
                }
                if (selectedPanel.IsVisible && selectedPanel.ActualWidth > 20)
                {
                    double w = Math.Max(48, selectedPanel.ActualWidth - ContentLeftInset - ContentRightInset);
                    _lastContentWidth = w;
                    return w;
                }
            }
            if (_lastContentWidth > 20)
            {
                return _lastContentWidth;
            }

            if (MainTabControl != null && MainTabControl.ActualWidth > 30)
            {
                return Math.Max(48, MainTabControl.ActualWidth - TabStripApproxWidth - PanelLeftGutter - ContentLeftInset - ContentRightInset);
            }
            double fallback = ActualWidth;
            if (double.IsNaN(fallback) || fallback < 40)
            {
                fallback = MinWidth;
            }
            return Math.Max(48, fallback - TabStripApproxWidth - PanelLeftGutter - ContentLeftInset - ContentRightInset);
        }

        private void ApplyNdListBoxesLayout(DependencyObject parent, int columns, ToolbarWidthTier tier, double availWidth)
        {
            if (parent == null)
            {
                return;
            }
            int n = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var nd = child as NDListBox;
                if (nd != null)
                {
                    // 始终用宿主测得的 availWidth；勿用未显示/未刷新的 nd.ActualWidth（切 Tab 首帧会偏）
                    ListBox inner = nd.FindName("MyListBox") as ListBox ?? FindVisualChild<ListBox>(nd);
                    ApplyListLayout(inner, columns, tier, availWidth);
                }
                else
                {
                    ApplyNdListBoxesLayout(child, columns, tier, availWidth);
                }
            }
        }

        /// <summary>列与列之间的间距；末列不加，保证 1 列/2 列右缘对齐。</summary>
        private const double ColumnGap = 4;

        private static void ApplyListLayout(ListBox listBox, int columns, ToolbarWidthTier tier, double availWidth)
        {
            if (listBox == null)
            {
                return;
            }

            int cols = Math.Max(1, columns);
            listBox.ClearValue(FrameworkElement.WidthProperty);
            listBox.ClearValue(FrameworkElement.MaxWidthProperty);
            listBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            listBox.Margin = new Thickness(
                ContentLeftInset,
                listBox.Margin.Top,
                ContentRightInset,
                listBox.Margin.Bottom);

            double gridW = Math.Max(48, availWidth);
            double cellW = gridW / cols;

            var grid = FindVisualChild<UniformGrid>(listBox);
            if (grid != null)
            {
                grid.Columns = cols;
                grid.Width = gridW;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
            }

            int count = listBox.Items != null ? listBox.Items.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var container = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (container == null)
                {
                    continue;
                }
                container.ClearValue(FrameworkElement.WidthProperty);
                container.ClearValue(FrameworkElement.MinWidthProperty);
                container.ClearValue(FrameworkElement.MaxWidthProperty);
                container.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                // 只有非末列留 ColumnGap；末列贴齐网格右缘，1 列与 2 列右边距一致
                bool lastCol = (i % cols) == (cols - 1);
                double gap = lastCol ? 0 : ColumnGap;
                double btnW = Math.Max(48, cellW - gap);
                if (tier == ToolbarWidthTier.OneColumnWide && btnW > OneColumnButtonMaxWidth)
                {
                    btnW = OneColumnButtonMaxWidth;
                }
                ApplyScriptButtonChrome(container, btnW, gap);
            }
        }

        /// <summary>
        /// 动画页窄按钮列表：按可用宽度均分，最多 SolidItemsMaxColumns 列；不写死 Width=30。
        /// </summary>
        private void ApplySolidItemsListLayout(double availWidth)
        {
            if (MyItemsControl == null)
            {
                return;
            }

            double gridW = Math.Max(48, availWidth);
            int cols = Math.Max(1, Math.Min(SolidItemsMaxColumns, (int)(gridW / SolidItemsMinCellWidth)));
            double cellW = gridW / cols;

            MyItemsControl.ClearValue(FrameworkElement.WidthProperty);
            MyItemsControl.ClearValue(FrameworkElement.MaxWidthProperty);
            MyItemsControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            MyItemsControl.Margin = new Thickness(
                ContentLeftInset,
                MyItemsControl.Margin.Top,
                ContentRightInset,
                MyItemsControl.Margin.Bottom);

            var grid = FindVisualChild<UniformGrid>(MyItemsControl);
            if (grid != null)
            {
                grid.Columns = cols;
                grid.Width = gridW;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
            }

            int count = MyItemsControl.Items != null ? MyItemsControl.Items.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var container = MyItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (container == null)
                {
                    continue;
                }
                container.ClearValue(FrameworkElement.WidthProperty);
                container.ClearValue(FrameworkElement.MinWidthProperty);
                container.ClearValue(FrameworkElement.MaxWidthProperty);
                container.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                bool lastCol = (i % cols) == (cols - 1);
                double gap = lastCol ? 0 : ColumnGap;
                double btnW = Math.Max(24, cellW - gap);
                ApplySolidItemButtonChrome(container, btnW, gap);
            }
        }

        /// <summary>
        /// 动画页底部工具区：固定 2 列，边距/宽度与上方列表相同（末列无 ColumnGap）。
        /// </summary>
        private void ApplyAnimUtilityButtonsLayout(double availWidth)
        {
            // 用 FindName，避免各 Max* 工程未重编 XAML 时 .g.cs 缺字段导致 CS0103
            var panel = FindName("AnimUtilityPanel") as StackPanel;
            if (panel != null)
            {
                panel.Margin = new Thickness(
                    ContentLeftInset,
                    panel.Margin.Top,
                    ContentRightInset,
                    panel.Margin.Bottom);
                panel.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            ApplyFixedUniformGridLayout(FindName("AnimUtilityGridLink") as UniformGrid, 2, availWidth, 24);
            ApplyFixedUniformGridLayout(FindName("AnimUtilityGridVisA") as UniformGrid, 2, availWidth, 24);
            ApplyFixedUniformGridLayout(FindName("AnimUtilityGridVisB") as UniformGrid, 2, availWidth, 24);
        }

        private static void ApplyFixedUniformGridLayout(UniformGrid grid, int columns, double availWidth, double minButtonWidth)
        {
            if (grid == null)
            {
                return;
            }

            int cols = Math.Max(1, columns);
            double gridW = Math.Max(48, availWidth);
            double cellW = gridW / cols;

            grid.Columns = cols;
            grid.Width = gridW;
            grid.HorizontalAlignment = HorizontalAlignment.Left;

            int index = 0;
            foreach (UIElement child in grid.Children)
            {
                var fe = child as FrameworkElement;
                if (fe == null)
                {
                    index++;
                    continue;
                }

                bool lastCol = (index % cols) == (cols - 1);
                double gap = lastCol ? 0 : ColumnGap;
                double btnW = Math.Max(minButtonWidth, cellW - gap);

                BindingOperations.ClearBinding(fe, FrameworkElement.WidthProperty);
                BindingOperations.ClearBinding(fe, FrameworkElement.MaxWidthProperty);
                fe.MinWidth = minButtonWidth;
                fe.Width = btnW;
                fe.MaxWidth = btnW;
                fe.HorizontalAlignment = HorizontalAlignment.Left;

                double top = fe.Margin.Top > 0 ? fe.Margin.Top : 1;
                double bottom = fe.Margin.Bottom > 0 ? fe.Margin.Bottom : 1;
                fe.Margin = new Thickness(0, top, gap, bottom);
                index++;
            }
        }

        private static void ApplySolidItemButtonChrome(DependencyObject parent, double buttonWidth, double rightMargin)
        {
            if (parent == null)
            {
                return;
            }
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var btn = child as System.Windows.Controls.Button;
                if (btn != null && btn.Name == "MyItemsControlButon")
                {
                    BindingOperations.ClearBinding(btn, FrameworkElement.WidthProperty);
                    BindingOperations.ClearBinding(btn, FrameworkElement.MaxWidthProperty);
                    btn.MinWidth = 24;
                    btn.Width = buttonWidth;
                    btn.MaxWidth = buttonWidth;
                    btn.HorizontalAlignment = HorizontalAlignment.Left;
                    double top = btn.Margin.Top > 1 ? btn.Margin.Top : 1;
                    btn.Margin = new Thickness(0, top, rightMargin, 1);
                }
                else
                {
                    ApplySolidItemButtonChrome(child, buttonWidth, rightMargin);
                }
            }
        }

        /// <summary>
        /// 同一列/同一档内按钮等宽；宽度来自内容区均分，左边距仍由面板 gutter 保证。
        /// </summary>
        private static void ApplyScriptButtonChrome(DependencyObject parent, double buttonWidth, double rightMargin)
        {
            if (parent == null)
            {
                return;
            }
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var btn = child as System.Windows.Controls.Button;
                if (btn != null && btn.Name == "MyListBoxButton")
                {
                    BindingOperations.ClearBinding(btn, FrameworkElement.WidthProperty);
                    BindingOperations.ClearBinding(btn, FrameworkElement.MaxWidthProperty);
                    btn.MinWidth = 48;
                    btn.Width = buttonWidth;
                    btn.MaxWidth = buttonWidth;
                    btn.HorizontalAlignment = HorizontalAlignment.Left;
                    double top = btn.Margin.Top > 0 ? btn.Margin.Top : 1;
                    btn.Margin = new Thickness(0, top, rightMargin, 2);
                }
                else
                {
                    ApplyScriptButtonChrome(child, buttonWidth, rightMargin);
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }
            int n = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var match = child as T;
                if (match != null)
                {
                    return match;
                }
                match = FindVisualChild<T>(child);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        private void LoadBuiltinWidthTiers()
        {
            _builtinWidthTiers.Clear();
            _builtinWidthTiers[BuiltinAnimKey] = ToolbarWidthTier.Default;
            _builtinWidthTiers[BuiltinRigKey] = ToolbarWidthTier.Default;
            try
            {
                string path = WebAddress.ToolBarWidthTierConfig;
                if (!File.Exists(path))
                {
                    return;
                }
                string text = File.ReadAllText(path).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                // 兼容旧版：整文件一个数字 = 全局分档，迁移到内置 Tab
                int legacy;
                if (int.TryParse(text, out legacy) && Enum.IsDefined(typeof(ToolbarWidthTier), legacy))
                {
                    var tier = (ToolbarWidthTier)legacy;
                    _builtinWidthTiers[BuiltinAnimKey] = tier;
                    _builtinWidthTiers[BuiltinRigKey] = tier;
                    return;
                }
                string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    int n;
                    if (string.IsNullOrEmpty(key) || !int.TryParse(val, out n) ||
                        !Enum.IsDefined(typeof(ToolbarWidthTier), n))
                    {
                        continue;
                    }
                    _builtinWidthTiers[key] = (ToolbarWidthTier)n;
                }
            }
            catch
            {
            }
        }

        private void SaveBuiltinWidthTiers()
        {
            try
            {
                if (!Directory.Exists(WebAddress.apppath))
                {
                    Directory.CreateDirectory(WebAddress.apppath);
                }
                var sb = new StringBuilder();
                foreach (var kv in _builtinWidthTiers)
                {
                    sb.Append(kv.Key).Append('=').Append((int)kv.Value).AppendLine();
                }
                File.WriteAllText(WebAddress.ToolBarWidthTierConfig, sb.ToString());
            }
            catch
            {
            }
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
            SizeChanged -= ToolbarsV_SizeChanged;
            _layoutRefreshGeneration++;
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
