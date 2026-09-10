using System;
using System.IO;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
using MaxToolbars.Toobars;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml.Linq;
namespace NDToolsBox
{
    /// <summary>
    /// NDListBox.xaml 的交互逻辑
    /// </summary>
    public partial class NDListBox : UserControl
    {
        public NDListBoxViewModle _itemlist;
        private object draggedItem;
        private int insertionIndex;
        private float list_box_item_height = 16f;//每个元素的高度
        private bool _saveInitialized;

        public NDListBox()
        {
            InitializeComponent();

            _itemlist = new NDListBoxViewModle();
            base.DataContext = _itemlist;

            this.Loaded += OnUserControlLoaded;  // 订阅 Loaded 事件
        }
        private void OnUserControlLoaded(object sender, RoutedEventArgs e)
        {
            // Loaded 事件触发时，XAML 已解析完成，Name 已赋值
            if (!_saveInitialized && !string.IsNullOrEmpty(this.Name))
            {
                InitWithSaveName(this.Name);
            }
            else if (!_saveInitialized)
            {
                _itemlist.NewItemsTools();
                _saveInitialized = true;
            }
        }

        /// <summary>
        /// 按稳定 Id 初始化保存路径，并尝试从 xml 加载列表项。
        /// </summary>
        public void InitWithSaveName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            _saveInitialized = true;
            _itemlist.SetSaveName(name);
            _itemlist.LoadOrNewItems();
            base.DataContext = _itemlist;
        }

        /// <summary>
        /// 使用已有 ViewModel（自定义 Tab 多段列表中的一段）。
        /// </summary>
        public void InitWithViewModel(NDListBoxViewModle model)
        {
            if (model == null)
            {
                return;
            }
            _saveInitialized = true;
            _itemlist = model;
            base.DataContext = _itemlist;
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
            CommandManager.InvalidateRequerySuggested();
        }
        private void MyListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText) || (e.Data.GetDataPresent(DataFormats.FileDrop)))
            //if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            //e.Handled = true;

        }
        private void MyListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                foreach (toolbarItemViewModle i in _itemlist.Items)
                {
                    i.IsEdit = false;
                }
                
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button bt = sender as System.Windows.Controls.Button;
            toolbarItemViewModle item = (toolbarItemViewModle)bt.DataContext;
            if (item.Path != null && (!string.IsNullOrEmpty(item.Path)))
            {
                ScriptsUtilities.FileinMxs(item.Path);
            }
            else if (!string.IsNullOrEmpty(item.Commit))
            {
                ScriptsUtilities.ExecuteMAXScriptScript(item.Commit);
            }
        }
        private void GetFiles(ref List<string> lists, string[] files)
        {
            foreach (string item in files)
            {
                if (File.Exists(item))
                {
                    if (System.IO.Path.GetExtension(item).Equals(".ms") || System.IO.Path.GetExtension(item).Equals(".mse"))
                    {
                        //Console.WriteLine(item);
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
            //拖拽 ndbox 中的脚本项
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                var str = e.Data.GetData(DataFormats.UnicodeText);
                if (str != null)
                {
                    if (File.Exists((string)str))
                    {
                        _itemlist.AddNewFileItem((string)str, item_index);
                    }
                    else
                    {
                        _itemlist.AddNewCommitItem((string)str, item_index, ScriptsUtilities.GetNDBoxMxsCommitScriptName((string)str));
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
    }
    public class NDListBoxItemCommand : ICommand
    {
        public readonly NDListBoxViewModle _toolViewModel;
        public NDListBoxItemCommand()
        {
        }

        public NDListBoxItemCommand(NDListBoxViewModle tool)
        {
            _toolViewModel = tool;
        }
        public event EventHandler CanExecuteChanged;

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public virtual void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(NDListBoxViewModle))
            {
                NDListBoxViewModle item = parameter as NDListBoxViewModle;
                if (item != null) 
                {
                }
            }
        }
    }
    /// <summary>
    /// 设置按钮上上下的 边距
    /// </summary>
    public class NDListBoxAddItemMarginCommand : NDListBoxItemCommand
    {
        public NDListBoxAddItemMarginCommand()
        { 
        }
        public NDListBoxAddItemMarginCommand(NDListBoxViewModle tool):base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (item != null)
                {
                    if (item != null) { item.Space = !item.Space; }
                }
            }
        }
    }
    public class NDListBoxRemoveItemCommand : NDListBoxItemCommand
    {
        public NDListBoxRemoveItemCommand()
        { 
        }
        public NDListBoxRemoveItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (item != null)
                {
                    if (_toolViewModel.Items.Contains(item))
                    {
                        _toolViewModel.Items.Remove(item);
                    }
                }
            }
        }
    }

    public class NDListBoxCopyItemCommand : NDListBoxItemCommand
    {
        public NDListBoxCopyItemCommand()
        { 
        
        }
        public NDListBoxCopyItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                ToolbarItemClipboard.Copy(parameter as toolbarItemViewModle);
            }
        }
    }
    public class NDListBoxPasetItemCommand : NDListBoxItemCommand
    {
        public NDListBoxPasetItemCommand()
        {

        }
        public NDListBoxPasetItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                ToolbarItemClipboard.PasteOnto(parameter as toolbarItemViewModle);
            }
        }
    }
    public class NDListBoxSetItemSpacingCommand : NDListBoxItemCommand
    {
        public NDListBoxSetItemSpacingCommand()
        {
        }
        public NDListBoxSetItemSpacingCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            SpacingValues values = UiPrompt.PromptSpacing(
                "设置上下间距",
                _toolViewModel.ItemMarginTop,
                _toolViewModel.ItemMarginBottom);
            if (values == null)
            {
                return;
            }
            _toolViewModel.ItemMarginTop = values.Top;
            _toolViewModel.ItemMarginBottom = values.Bottom;
            if (_toolViewModel.OwnerTab != null)
            {
                _toolViewModel.OwnerTab.SaveToXml();
            }
            else if (_toolViewModel.GSaveItemCommand != null && !string.IsNullOrEmpty(_toolViewModel.GSaveItemCommand.Save_Path))
            {
                CfgHelpPersonXml.SaveXml(_toolViewModel, _toolViewModel.GSaveItemCommand.Save_Path);
            }
        }
    }

    public class NDListBoxEditItemCommand : NDListBoxItemCommand
    {
        public NDListBoxEditItemCommand()
        { 
        
        }
        public NDListBoxEditItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (item == null)
                {
                    return;
                }
                ButtonEditValues values = UiPrompt.PromptButtonEdit(
                    "编辑按钮",
                    item.Name,
                    item.Commit,
                    item.ToolTip);
                if (values == null)
                {
                    return;
                }
                item.Name = values.Name;
                item.Commit = values.Commit;
                item.ToolTip = values.ToolTip;
                item.IsEdit = false;
            }
        }
    }

    public class NDListBoxSaveItemCommand : NDListBoxItemCommand
    {
        public string Save_Path;
        public NDListBoxSaveItemCommand()
        { 
        }
        public NDListBoxSaveItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (_toolViewModel.OwnerTab != null)
            {
                _toolViewModel.OwnerTab.SaveToXml();
                return;
            }
            CfgHelpPersonXml.SaveXml(_toolViewModel, Save_Path);
        }
    }

    public class NDListBoxReloadItemCommand : NDListBoxItemCommand
    {
        public NDListBoxReloadItemCommand()
        {
        }
        public NDListBoxReloadItemCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override void Execute(object parameter)
        {
            if (_toolViewModel.OwnerTab != null)
            {
                _toolViewModel.OwnerTab.ReloadFromXml();
                return;
            }
            _toolViewModel.ReloadFromXml();
        }
    }

    /// <summary>
    /// 在当前自定义 Tab 下新增一组 Expander + ListBox（仅 OwnerTab 时可用）。
    /// </summary>
    public class NDListBoxAddSectionCommand : NDListBoxItemCommand
    {
        public NDListBoxAddSectionCommand()
        {
        }
        public NDListBoxAddSectionCommand(NDListBoxViewModle tool) : base(tool)
        {
        }
        public override bool CanExecute(object parameter)
        {
            return _toolViewModel != null && _toolViewModel.OwnerTab != null;
        }
        public override void Execute(object parameter)
        {
            if (_toolViewModel != null && _toolViewModel.OwnerTab != null)
            {
                _toolViewModel.OwnerTab.AddSection();
            }
        }
    }

    /// <summary>
    /// 自定义 Tab 整页配置：Items 中每一项对应一组 Expander + ListBox。
    /// </summary>
    public class CustomTabListsViewModle : INotifyPropertyChanged
    {
        private ObservableCollection<NDListBoxViewModle> _items;

        [XmlIgnore]
        public string SavePath { get; set; }

        public event Action Reloaded;

        public CustomTabListsViewModle()
        {
            _items = new ObservableCollection<NDListBoxViewModle>();
        }

        [XmlArray("Items")]
        [XmlArrayItem("NDListBoxViewModle")]
        public ObservableCollection<NDListBoxViewModle> Items
        {
            get { return _items; }
            set
            {
                _items = value ?? new ObservableCollection<NDListBoxViewModle>();
                OnPropertyChanged("Items");
            }
        }

        public void AttachOwners()
        {
            if (_items == null)
            {
                return;
            }
            foreach (NDListBoxViewModle section in _items)
            {
                if (section != null)
                {
                    section.OwnerTab = this;
                    if (section.Items == null)
                    {
                        section.NewItemsTools();
                    }
                }
            }
        }

        public void SaveToXml()
        {
            if (string.IsNullOrEmpty(SavePath))
            {
                return;
            }
            CfgHelpPersonXml.SaveXml(this, SavePath);
        }

        public void ReloadFromXml()
        {
            CustomTabListsViewModle loaded = LoadOrCreate(SavePath);
            Items = loaded.Items;
            AttachOwners();
            RaiseReloaded();
        }

        /// <summary>
        /// 新增一组 Expander + ListBox，写入配置并刷新界面。
        /// </summary>
        public void AddSection()
        {
            if (_items == null)
            {
                _items = new ObservableCollection<NDListBoxViewModle>();
            }
            var section = new NDListBoxViewModle();
            section.NewItemsTools();
            section.OwnerTab = this;
            _items.Add(section);
            OnPropertyChanged("Items");
            SaveToXml();
            RaiseReloaded();
        }

        private void RaiseReloaded()
        {
            if (Reloaded != null)
            {
                Reloaded();
            }
        }

        public static CustomTabListsViewModle CreateDefault(string savePath)
        {
            var vm = new CustomTabListsViewModle { SavePath = savePath };
            var section = new NDListBoxViewModle();
            section.NewItemsTools();
            section.OwnerTab = vm;
            vm.Items.Add(section);
            return vm;
        }

        /// <summary>
        /// 加载自定义 Tab 配置；兼容：
        /// - CustomTabListsViewModle（多段 Expander）
        /// - 旧版单列表 NDListBoxViewModle
        /// - 同一 NDListBoxViewModle 下多个并列 &lt;Items&gt;（每段一组按钮 → 多个 Expander）
        /// - 旧版 CustomTab_*_1/2/3.xml 三列文件 → 合并为多段
        /// </summary>
        public static CustomTabListsViewModle LoadOrCreate(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                return CreateDefault(savePath);
            }

            string unifiedPath = GetUnifiedListPath(savePath);

            CustomTabListsViewModle loaded = TryLoadMultiFile(unifiedPath);
            if (loaded != null)
            {
                return loaded;
            }
            if (!string.Equals(unifiedPath, savePath, StringComparison.OrdinalIgnoreCase))
            {
                loaded = TryLoadMultiFile(savePath);
                if (loaded != null)
                {
                    return loaded;
                }
            }

            // 旧三列 _1/_2/_3 并存时优先合并
            loaded = TryLoadLegacyColumnFiles(savePath);
            if (loaded != null)
            {
                return loaded;
            }

            if (File.Exists(savePath))
            {
                loaded = TryLoadNdListBoxMultipleItemsGroups(savePath);
                if (loaded != null)
                {
                    return loaded;
                }

                loaded = TryLoadSingleAsOneSection(savePath);
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return CreateDefault(unifiedPath);
        }

        /// <summary>CustomTab_xxx_1.xml → CustomTab_xxx.xml</summary>
        public static string GetUnifiedListPath(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                return savePath;
            }
            string dir = System.IO.Path.GetDirectoryName(savePath);
            string name = System.IO.Path.GetFileNameWithoutExtension(savePath);
            if (string.IsNullOrEmpty(name))
            {
                return savePath;
            }
            int us = name.LastIndexOf('_');
            if (us > 0 && us < name.Length - 1)
            {
                string suffix = name.Substring(us + 1);
                int n;
                if (int.TryParse(suffix, out n) && n >= 1 && n <= 99)
                {
                    string prefix = name.Substring(0, us);
                    return System.IO.Path.Combine(dir ?? string.Empty, prefix + ".xml");
                }
            }
            return savePath;
        }

        public static string ReadAllTextDetectEncoding(string path)
        {
            using (var reader = new StreamReader(path, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static CustomTabListsViewModle TryLoadMultiFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }
            try
            {
                string xmltext = ReadAllTextDetectEncoding(path);
                CustomTabListsViewModle multi = CfgHelpPersonXml.DeserializeFromXmlString<CustomTabListsViewModle>(xmltext);
                if (multi != null && multi.Items != null && multi.Items.Count > 0)
                {
                    // 避免把「单列表误反序列化成空壳多段」：段里应是按钮列表模型
                    multi.SavePath = path;
                    multi.AttachOwners();
                    return multi;
                }
            }
            catch
            {
            }
            return null;
        }

        private static CustomTabListsViewModle TryLoadSingleAsOneSection(string path)
        {
            try
            {
                string xmltext = ReadAllTextDetectEncoding(path);
                NDListBoxViewModle single = CfgHelpPersonXml.DeserializeFromXmlString<NDListBoxViewModle>(xmltext);
                if (single == null)
                {
                    return null;
                }
                string unified = GetUnifiedListPath(path);
                var multi = new CustomTabListsViewModle { SavePath = unified };
                if (single.Items == null || single.Items.Count == 0)
                {
                    single.NewItemsTools();
                }
                single.OwnerTab = multi;
                multi.Items.Add(single);
                return multi;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析根为 NDListBoxViewModle、含多个并列 &lt;Items&gt; 的文件（用户手写多段）。
        /// </summary>
        private static CustomTabListsViewModle TryLoadNdListBoxMultipleItemsGroups(string path)
        {
            try
            {
                string xmltext = ReadAllTextDetectEncoding(path);
                XDocument doc = XDocument.Parse(xmltext);
                XElement root = doc.Root;
                if (root == null || root.Name.LocalName != "NDListBoxViewModle")
                {
                    return null;
                }
                List<XElement> groups = root.Elements().Where(e => e.Name.LocalName == "Items").ToList();
                if (groups.Count <= 1)
                {
                    return null;
                }

                string unified = GetUnifiedListPath(path);
                var multi = new CustomTabListsViewModle { SavePath = unified };
                XElement[] shared = root.Elements()
                    .Where(e => e.Name.LocalName != "Items")
                    .ToArray();
                foreach (XElement group in groups)
                {
                    var wrapper = new XElement(root.Name, shared, new XElement(group));
                    NDListBoxViewModle section =
                        CfgHelpPersonXml.DeserializeFromXmlString<NDListBoxViewModle>(wrapper.ToString());
                    if (section == null)
                    {
                        continue;
                    }
                    if (section.Items == null || section.Items.Count == 0)
                    {
                        section.NewItemsTools();
                    }
                    section.OwnerTab = multi;
                    multi.Items.Add(section);
                }
                if (multi.Items.Count == 0)
                {
                    return null;
                }
                multi.AttachOwners();
                try
                {
                    multi.SaveToXml();
                }
                catch
                {
                }
                return multi;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 合并 CustomTab_xxx_1.xml / _2.xml / _3.xml 为多段；SavePath 改为 CustomTab_xxx.xml。
        /// </summary>
        private static CustomTabListsViewModle TryLoadLegacyColumnFiles(string savePath)
        {
            string dir = System.IO.Path.GetDirectoryName(savePath);
            string name = System.IO.Path.GetFileNameWithoutExtension(savePath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            string prefix = name;
            int us = name.LastIndexOf('_');
            if (us > 0)
            {
                string suffix = name.Substring(us + 1);
                int n;
                if (int.TryParse(suffix, out n) && n >= 1 && n <= 99)
                {
                    prefix = name.Substring(0, us);
                }
            }

            string col1 = System.IO.Path.Combine(dir, prefix + "_1.xml");
            if (!File.Exists(col1))
            {
                return null;
            }
            // 至少还要有一列兄弟文件，才走「旧三列合并」；否则交给单文件逻辑
            bool hasSibling = false;
            for (int i = 2; i <= 9; i++)
            {
                if (File.Exists(System.IO.Path.Combine(dir, prefix + "_" + i + ".xml")))
                {
                    hasSibling = true;
                    break;
                }
            }
            if (!hasSibling)
            {
                return null;
            }

            string unified = System.IO.Path.Combine(dir, prefix + ".xml");
            var multi = new CustomTabListsViewModle { SavePath = unified };
            for (int i = 1; i <= 20; i++)
            {
                string colPath = System.IO.Path.Combine(dir, prefix + "_" + i + ".xml");
                if (!File.Exists(colPath))
                {
                    break;
                }

                CustomTabListsViewModle fromGroups = TryLoadNdListBoxMultipleItemsGroups(colPath);
                if (fromGroups != null && fromGroups.Items != null && fromGroups.Items.Count > 0)
                {
                    foreach (NDListBoxViewModle s in fromGroups.Items)
                    {
                        if (s == null)
                        {
                            continue;
                        }
                        s.OwnerTab = multi;
                        multi.Items.Add(s);
                    }
                    continue;
                }

                CustomTabListsViewModle one = TryLoadSingleAsOneSection(colPath);
                if (one != null && one.Items != null)
                {
                    foreach (NDListBoxViewModle s in one.Items)
                    {
                        if (s == null)
                        {
                            continue;
                        }
                        s.OwnerTab = multi;
                        multi.Items.Add(s);
                    }
                }
            }

            if (multi.Items.Count == 0)
            {
                return null;
            }
            multi.AttachOwners();
            try
            {
                multi.SaveToXml();
            }
            catch
            {
            }
            return multi;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class NDListBoxViewModle : INotifyPropertyChanged
    {

        private ObservableCollection<toolbarItemViewModle> _items;
        private NDListBoxSaveItemCommand _SaveCommand;
        private NDListBoxReloadItemCommand _ReloadCommand;
        private NDListBoxAddSectionCommand _AddSectionCommand;
        private NDListBoxEditItemCommand _EditItemCommand;
        private NDListBoxRemoveItemCommand _removerItemCommand;
        private NDListBoxCopyItemCommand _copyItemCommand;
        private NDListBoxPasetItemCommand _pasetItemCommand;
        private NDListBoxAddItemMarginCommand _addMarginItemCommand;
        private NDListBoxSetItemSpacingCommand _setItemSpacingCommand;
        private int _itemMarginTop = 1;
        private int _itemMarginBottom = 1;
        private string _header = "...";

        [XmlIgnore]
        private string _name;

        /// <summary>
        /// 所属自定义 Tab 整页配置；非空时保存/刷新写整份 CustomTabLists xml。
        /// </summary>
        [XmlIgnore]
        public CustomTabListsViewModle OwnerTab { get; set; }

        public NDListBoxViewModle()
        {
            _items = new ObservableCollection<toolbarItemViewModle>();
            _SaveCommand = new NDListBoxSaveItemCommand(this);
            _SaveCommand.Save_Path = $@"{WebAddress.apppath}\{_name}.xml";
            _ReloadCommand = new NDListBoxReloadItemCommand(this);
            _AddSectionCommand = new NDListBoxAddSectionCommand(this);

            _EditItemCommand = new NDListBoxEditItemCommand(this);
            _removerItemCommand = new NDListBoxRemoveItemCommand(this);
            _copyItemCommand = new NDListBoxCopyItemCommand(this);
            _pasetItemCommand = new NDListBoxPasetItemCommand(this);
            _addMarginItemCommand = new NDListBoxAddItemMarginCommand(this);
            _setItemSpacingCommand = new NDListBoxSetItemSpacingCommand(this);
        }

        /// <summary>
        /// Expander 标题；默认 "..."。
        /// </summary>
        public string Header
        {
            get { return string.IsNullOrEmpty(_header) ? "..." : _header; }
            set
            {
                _header = value;
                this.OnPropertyChanged("Header");
            }
        }
        public void SetSaveName(string n)
        { 
            _name = n;
            _SaveCommand.Save_Path = $@"{WebAddress.apppath}\{_name}.xml";
        }

        public void LoadOrNewItems()
        {
            ReloadFromXml(false);
        }

        /// <summary>
        /// 从 xml 重新加载列表；force 时即使文件为空也覆盖内存中的当前项。
        /// </summary>
        public void ReloadFromXml(bool force = true)
        {
            string path = _SaveCommand != null ? _SaveCommand.Save_Path : null;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    string xmltext = File.ReadAllText(path, new UTF8Encoding(false));
                    NDListBoxViewModle loaded = CfgHelpPersonXml.DeserializeFromXmlString<NDListBoxViewModle>(xmltext);
                    if (loaded != null)
                    {
                        ItemMarginTop = loaded.ItemMarginTop;
                        ItemMarginBottom = loaded.ItemMarginBottom;
                        if (loaded.Items != null && (force || loaded.Items.Count > 0))
                        {
                            Items = loaded.Items;
                            if (Items == null || Items.Count == 0)
                            {
                                NewItemsTools();
                            }
                            return;
                        }
                    }
                }
                catch
                {
                }
            }
            if (force || Items == null || Items.Count == 0)
            {
                NewItemsTools();
            }
        }

        public void NewItemsTools()
        {
            _items = new ObservableCollection<toolbarItemViewModle>();
            _items.Add(new toolbarItemViewModle("将脚本"));
            _items.Add(new toolbarItemViewModle("拖拽到"));
            _items.Add(new toolbarItemViewModle("这里"));
            this.OnPropertyChanged("Items");
        }
        public void AddNewCommitItem(ObservableCollection<toolbarItemViewModle> items, string commit, int index, string name)
        {
            //bool isfind = items.Any<toolbarItemViewModle>(p => p.Commit.Equals(commit));
            if (!FindFilePath(items, commit))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(string.Concat("script-", index + 1));
                item.Commit = commit;

                if (name != null)
                {
                    item.Name = name;
                }

                if (index >= 0 && index < items.Count)
                {
                    items.Insert(index, item);
                }
                else
                {
                    items.Add(item);
                }
            }
        }
        public void AddNewCommitItem(string commit, int index, string name)
        {
            //bool isfind = _items.Any<toolbarItemViewModle>(p => p.Commit.Equals(commit));
            if (!FindFilePath(commit))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(string.Concat("script-", index + 1));
                item.Commit = commit;

                if (name != null)
                {
                    item.Name = name;
                }

                if (index >= 0 && index < _items.Count)
                {
                    _items.Insert(index, item);
                }
                else
                {
                    _items.Add(item);
                }
            }
        }
        /// <summary>
        /// 拖拽来的文件路径添加
        /// </summary>
        /// <param name="file_path"></param>
        public void AddNewFileItem(string file_path, int index)
        {
            //这玩意在 3dsMax 中无法执行
            //bool isfind = _items.Any<toolbarItemViewModle>(p => p.Path.Equals(file_path));

            if (!FindFilePath(file_path))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(System.IO.Path.GetFileNameWithoutExtension(file_path));
                item.Path = file_path;
                if (index >= 0 && index < _items.Count)
                {
                    _items.Insert(index, item);
                }
                else
                {
                    _items.Add(item);
                }
            }

        }
        public void AddNewFileItem(ObservableCollection<toolbarItemViewModle> items, string file_path, int index)
        {
            if (!FindFilePath(items, file_path))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(System.IO.Path.GetFileNameWithoutExtension(file_path));
                item.Path = file_path;
                if (index >= 0 && index < items.Count)
                {
                    items.Insert(index, item);
                }
                else
                {
                    items.Add(item);
                }
            }

        }
        public bool FindFilePath(ObservableCollection<toolbarItemViewModle> items, string file_path)
        {
            foreach (toolbarItemViewModle item in items)
            {
                if (item.Path != null && item.Path.Equals(file_path))
                {
                    return true;
                }
                if (item.Commit != null && item.Commit.Equals(file_path))
                {
                    return true;
                }
            }
            return false;
        }
        public bool FindFilePath(string file_path)
        {
            foreach (toolbarItemViewModle item in _items)
            {
                if (item.Path != null && item.Path.Equals(file_path))
                {
                    return true;
                }
                if (item.Commit != null && item.Commit.Equals(file_path))
                {
                    return true;
                }
            }
            return false;
        }

        public NDListBoxAddItemMarginCommand AddMarginCommand
        {
            get { return _addMarginItemCommand; }
        }
        public NDListBoxSetItemSpacingCommand SetItemSpacingCommand
        {
            get { return _setItemSpacingCommand; }
        }
        public int ItemMarginTop
        {
            get { return _itemMarginTop; }
            set
            {
                if (_itemMarginTop == value)
                {
                    return;
                }
                _itemMarginTop = value;
                this.OnPropertyChanged("ItemMarginTop");
                this.OnPropertyChanged("ItemRowMargin");
            }
        }
        public int ItemMarginBottom
        {
            get { return _itemMarginBottom; }
            set
            {
                if (_itemMarginBottom == value)
                {
                    return;
                }
                _itemMarginBottom = value;
                this.OnPropertyChanged("ItemMarginBottom");
                this.OnPropertyChanged("ItemRowMargin");
            }
        }
        [XmlIgnore]
        public Thickness ItemRowMargin
        {
            get { return new Thickness(0, ItemMarginTop, 0, ItemMarginBottom); }
        }
        public NDListBoxPasetItemCommand GPasetItemCommand
        {
            get { return _pasetItemCommand; }
        }
        public NDListBoxCopyItemCommand GCopyItemCommand
        {
            get { return _copyItemCommand; }

        }
        public NDListBoxSaveItemCommand GSaveItemCommand
        {
            get { return _SaveCommand; }
        }
        public NDListBoxReloadItemCommand GReloadItemCommand
        {
            get { return _ReloadCommand; }
        }
        public NDListBoxAddSectionCommand GAddSectionCommand
        {
            get { return _AddSectionCommand; }
        }
        public NDListBoxEditItemCommand GEditItemCommand
        {
            get { return _EditItemCommand; }
        }
        public NDListBoxRemoveItemCommand RemoverItem
        {
            get { return _removerItemCommand; }
        }
        public string GetThemeColor
        {
            get { return ScriptsUtilities.mGetColor(5); }
        }
        public string GetMaxUiBackgroundColor
        {
            get { return ScriptsUtilities.GetMaxBackgroundColor_Str(); }
        }
        public string GetMaxTextColor
        {
            get { return ScriptsUtilities.mGetColor(0); }
        }
        public string GetButtonColor_A
        {
            get { return ScriptsUtilities.mGetColor(1); }
        }
        public string GetButtonColor_B
        {
            get { return ScriptsUtilities.mGetColor(2); }
        }
        public string GetButtonColor_C
        {
            get { return ScriptsUtilities.mGetColor(3); }
        }
        public string GetButtonColor_D
        {
            get { return ScriptsUtilities.mGetColor(4); }
        }
        public string GetMouseOverColor
        {
            get { return ScriptsUtilities.GetMouseOverColor(0); }
        }
        public string GetMousePressedColor
        {
            get { return ScriptsUtilities.GetMouseOverColor(1); }
        }
        public ObservableCollection<toolbarItemViewModle> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                this.OnPropertyChanged("Items");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("tree view model PropertyChanged is null");
        }
    }
}
