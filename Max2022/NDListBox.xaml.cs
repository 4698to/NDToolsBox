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
        private void dynamicContextMenu_Opened(object sender, RoutedEventArgs e)
        {
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
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (!string.IsNullOrEmpty(item.Path))
                {
                    Clipboard.SetText(item.Path, TextDataFormat.UnicodeText);
                }
                if (!string.IsNullOrEmpty(item.Commit))
                {
                    Clipboard.SetText(item.Commit, TextDataFormat.UnicodeText);
                }
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
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    string Path_or_commit = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (File.Exists(Path_or_commit))
                    {
                        item.Path = Path_or_commit; item.Commit = string.Empty;
                    }
                    else
                    {
                        item.Commit = Path_or_commit; item.Path = string.Empty;
                        string name = ScriptsUtilities.GetNDBoxMxsCommitScriptName(Path_or_commit);
                        if (string.IsNullOrEmpty(name))
                        {
                            item.Name = "Mxs";
                        }
                        else
                        {
                            item.Name = name;
                        }
                    }
                }
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
            if (_toolViewModel.GSaveItemCommand != null && !string.IsNullOrEmpty(_toolViewModel.GSaveItemCommand.Save_Path))
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
                string newName = UiPrompt.PromptText("编辑名字", "请输入按钮显示名称：", item.Name);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return;
                }
                item.Name = newName.Trim();
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
            CfgHelpPersonXml.SaveXml(_toolViewModel, Save_Path);
        }
    }

    public class NDListBoxViewModle : INotifyPropertyChanged
    {

        private ObservableCollection<toolbarItemViewModle> _items;
        private NDListBoxSaveItemCommand _SaveCommand;
        private NDListBoxEditItemCommand _EditItemCommand;
        private NDListBoxRemoveItemCommand _removerItemCommand;
        private NDListBoxCopyItemCommand _copyItemCommand;
        private NDListBoxPasetItemCommand _pasetItemCommand;
        private NDListBoxAddItemMarginCommand _addMarginItemCommand;
        private NDListBoxSetItemSpacingCommand _setItemSpacingCommand;
        private int _itemMarginTop = 1;
        private int _itemMarginBottom = 1;

        [XmlIgnore]
        private string _name;
        public NDListBoxViewModle()
        {
            _items = new ObservableCollection<toolbarItemViewModle>();
            _SaveCommand = new NDListBoxSaveItemCommand(this);
            _SaveCommand.Save_Path = $@"{WebAddress.apppath}\{_name}.xml";

            _EditItemCommand = new NDListBoxEditItemCommand(this);
            _removerItemCommand = new NDListBoxRemoveItemCommand(this);
            _copyItemCommand = new NDListBoxCopyItemCommand(this);
            _pasetItemCommand = new NDListBoxPasetItemCommand(this);
            _addMarginItemCommand = new NDListBoxAddItemMarginCommand(this);
            _setItemSpacingCommand = new NDListBoxSetItemSpacingCommand(this);
        }
        public void SetSaveName(string n)
        { 
            _name = n;
            _SaveCommand.Save_Path = $@"{WebAddress.apppath}\{_name}.xml";
        }

        public void LoadOrNewItems()
        {
            string path = _SaveCommand != null ? _SaveCommand.Save_Path : null;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    string xmltext = File.ReadAllText(path, new UTF8Encoding(false));
                    NDListBoxViewModle loaded = CfgHelpPersonXml.DeserializeFromXmlString<NDListBoxViewModle>(xmltext);
                    if (loaded != null && loaded.Items != null && loaded.Items.Count > 0)
                    {
                        ItemMarginTop = loaded.ItemMarginTop;
                        ItemMarginBottom = loaded.ItemMarginBottom;
                        Items = loaded.Items;
                        return;
                    }
                    if (loaded != null)
                    {
                        ItemMarginTop = loaded.ItemMarginTop;
                        ItemMarginBottom = loaded.ItemMarginBottom;
                    }
                }
                catch
                {
                }
            }
            if (Items == null || Items.Count == 0)
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
