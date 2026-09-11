
using NDToolsBox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml.Serialization;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace MaxToolbars.Toobars
{
    public class AddMarginItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public AddMarginItemCommand(toolbarsViewModle tool)
        {
            _toolViewModel = tool;
        }
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                toolbarItemViewModle item = parameter as toolbarItemViewModle;
                if (item != null) { item.Space = !item.Space; }
            }
        }
    }
    public class RemoveItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;
        public event EventHandler CanExecuteChanged;
        public RemoveItemCommand(toolbarsViewModle tool)
        {
            _toolViewModel = tool;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
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
                    if (_toolViewModel.SolidItems.Contains(item))
                    {
                        _toolViewModel.SolidItems.Remove(item);
                    }
                }
            }
        }
    }
    public class CopyItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public CopyItemCommand(toolbarsViewModle tool)
        {
            this._toolViewModel = tool;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                ToolbarItemClipboard.Copy(parameter as toolbarItemViewModle);
            }
        }
    }
    public class PasetItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public PasetItemCommand(toolbarsViewModle tool)
        {
            this._toolViewModel = tool;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(toolbarItemViewModle))
            {
                ToolbarItemClipboard.PasteOnto(parameter as toolbarItemViewModle);
            }
        }
    }
    public class SetItemSpacingCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public SetItemSpacingCommand(toolbarsViewModle tool)
        {
            _toolViewModel = tool;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
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
            CfgHelpPersonXml.SaveXml(_toolViewModel, WebAddress.ToolBarItemConfig);
        }
    }

    public class EditItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public EditItemCommand(toolbarsViewModle tool)
        {
            this._toolViewModel = tool;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
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
                    item.GetEditCommitText(),
                    item.ToolTip);
                if (values == null)
                {
                    return;
                }
                item.Name = values.Name;
                item.Commit = values.Commit;
                // 编辑以 Commit 为准：有脚本内容时清掉 Path，避免点击仍优先跑旧路径而忽略编辑
                if (!string.IsNullOrWhiteSpace(values.Commit))
                {
                    item.Path = string.Empty;
                }
                item.ToolTip = values.ToolTip;
                item.IsEdit = false;
            }
        }
    }

    public class SaveItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public SaveItemCommand(toolbarsViewModle tool)
        {
            this._toolViewModel = tool;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            CfgHelpPersonXml.SaveXml(_toolViewModel, WebAddress.ToolBarItemConfig);
        }
    }

    public class ReloadItemCommand : ICommand
    {
        public readonly toolbarsViewModle _toolViewModel;

        public ReloadItemCommand(toolbarsViewModle tool)
        {
            this._toolViewModel = tool;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            _toolViewModel.ReloadFromXml();
        }
    }

    public class toolbarItemViewModle : INotifyPropertyChanged
    {

        private string name;
        private string path;
        private string commit;
        private string tooltip;
        public toolbarItemViewModle()
        {
            tooltip = "";
            name = "";
            path = "";
            commit = "";
        }
        public toolbarItemViewModle(string names )
        {
            this.name = names;
            this.tooltip = names;
        }
        private bool _isedit = false;
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
        [XmlIgnore]
        public bool IsEdit {
            get { return _isedit; }

            set {  _isedit = value; 
                this.OnPropertyChanged("IsEdit");
            }
        }

        private bool _space = false;
        public bool Space
        {
            get { return _space; }
            set
            {
                _space = value;
                this.OnPropertyChanged("Space");
            }
        }

        public string Path 
        {
            get { return path; }
            set {
                path = value;
                this.OnPropertyChanged("Path");

            }
        }
        public string Commit
        {
            get { return commit; }
            set {
                commit = value;
                this.OnPropertyChanged("Commit");
            }
        }
        public string Name
        {
            get { return name; }
            set { name = value;
                this.OnPropertyChanged("Name");
            }
        }
        public string ToolTip
        {
            get {
                if (this.tooltip.Length > 0)
                {
                    return this.tooltip;
                }
                else { 
                    return this.name;
                }
                
            }
            set {
                this.tooltip = value;
                this.OnPropertyChanged("ToolTip");

            }
        }

        /// <summary>
        /// 配置中实际存储的 ToolTip（不含回退到 Name）。
        /// </summary>
        [XmlIgnore]
        public string StoredToolTip
        {
            get { return this.tooltip ?? string.Empty; }
        }

        public void ApplyFields(string name, string path, string commit, string toolTip, bool space)
        {
            Name = name ?? string.Empty;
            Path = path ?? string.Empty;
            Commit = commit ?? string.Empty;
            this.tooltip = toolTip ?? string.Empty;
            this.OnPropertyChanged("ToolTip");
            Space = space;
            IsEdit = false;
        }

        /// <summary>
        /// 编辑对话框展示的脚本：优先 Commit；若为空且有 Path，则按扩展名生成执行命令。
        /// </summary>
        public string GetEditCommitText()
        {
            if (!string.IsNullOrEmpty(commit))
            {
                return commit;
            }
            if (!string.IsNullOrEmpty(path))
            {
                return BuildScriptCommit(path);
            }
            return string.Empty;
        }

        /// <summary>
        /// 按扩展名生成侧栏按钮的执行脚本：.ms/.mse → filein；.py → python.ExecuteFile。
        /// </summary>
        public static string BuildScriptCommit(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }
            string ext = System.IO.Path.GetExtension(filePath);
            if (string.Equals(ext, ".py", StringComparison.OrdinalIgnoreCase))
            {
                return $"python.ExecuteFile @\"{filePath}\"";
            }
            // .ms / .mse 及其它默认按 MaxScript filein
            return $"filein @\"{filePath}\"";
        }

        /// <summary>
        /// 解析黏贴/拖入的纯文本：路径按扩展名；#NDDrop / --NDPy 为 Python；--NDDrop 及其它为 MaxScript。
        /// </summary>
        public static bool TryResolveScriptText(string text, out string name, out string path, out string commit)
        {
            name = null;
            path = string.Empty;
            commit = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            string asPath = trimmed.Trim('"');
            if (File.Exists(asPath))
            {
                path = asPath;
                commit = BuildScriptCommit(asPath);
                name = System.IO.Path.GetFileNameWithoutExtension(asPath);
                return true;
            }

            if (TryParseTaggedScript(trimmed, out name, out commit))
            {
                path = string.Empty;
                return true;
            }

            // 未标记：整段当作 MaxScript Commit
            name = ScriptsUtilities.GetNDBoxMxsCommitScriptName(trimmed);
            if (string.IsNullOrEmpty(name))
            {
                name = LooksLikePythonInvoke(trimmed) ? "Py" : "Mxs";
            }
            commit = trimmed;
            path = string.Empty;
            return true;
        }

        /// <summary>
        /// #NDDrop;名称; / --NDPy;名称; → Python 源码（包装为 python.Execute）
        /// --NDDrop;名称; → MaxScript（整段作为 Commit，与历史行为一致）
        /// </summary>
        private static bool TryParseTaggedScript(string text, out string name, out string commit)
        {
            name = null;
            commit = null;
            int lineEnd = text.IndexOfAny(new[] { '\r', '\n' });
            string firstLine = lineEnd >= 0 ? text.Substring(0, lineEnd) : text;
            string body = lineEnd >= 0 ? text.Substring(lineEnd).TrimStart('\r', '\n') : string.Empty;

            if (firstLine.StartsWith("#NDDrop;", StringComparison.Ordinal)
                || firstLine.StartsWith("--NDPy;", StringComparison.Ordinal))
            {
                name = ParseTaggedName(firstLine);
                if (string.IsNullOrEmpty(body))
                {
                    body = RemainderAfterName(firstLine);
                }
                commit = WrapPythonAsMaxScript(body);
                if (string.IsNullOrEmpty(name))
                {
                    name = "Py";
                }
                return true;
            }

            if (firstLine.StartsWith("--NDDrop;", StringComparison.Ordinal))
            {
                name = ParseTaggedName(firstLine);
                commit = text;
                return true;
            }

            return false;
        }

        private static string ParseTaggedName(string firstLine)
        {
            string[] parts = firstLine.Split(';');
            if (parts.Length > 1)
            {
                return parts[1] ?? string.Empty;
            }
            return string.Empty;
        }

        private static string RemainderAfterName(string firstLine)
        {
            int first = firstLine.IndexOf(';');
            if (first < 0)
            {
                return string.Empty;
            }
            int second = firstLine.IndexOf(';', first + 1);
            if (second < 0 || second + 1 >= firstLine.Length)
            {
                return string.Empty;
            }
            return firstLine.Substring(second + 1).Trim();
        }

        private static bool LooksLikePythonInvoke(string text)
        {
            string t = text.TrimStart();
            return t.StartsWith("python.ExecuteFile", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("python.Execute", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将 Python 源码包装为可在 Commit 中执行的 MaxScript。
        /// 若已是 python.Execute / ExecuteFile，则原样返回。
        /// </summary>
        public static string WrapPythonAsMaxScript(string pythonCode)
        {
            if (string.IsNullOrEmpty(pythonCode))
            {
                return "python.Execute \"\"";
            }
            string trimmed = pythonCode.TrimStart();
            if (LooksLikePythonInvoke(trimmed))
            {
                return pythonCode;
            }
            string escaped = pythonCode
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\\n");
            return "python.Execute \"" + escaped + "\"";
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
    public class toolbarsViewModle : INotifyPropertyChanged
    {

        private ObservableCollection<toolbarItemViewModle> _items;

        private ObservableCollection<toolbarItemViewModle> solid_items;

        /*private bool _geometry;
        private bool _lights;
        private bool _cameras;
        private bool _helpers;
        private bool _bone;*/
        private int start;
        private int end;

        private SaveItemCommand _SaveCommand;
        private ReloadItemCommand _ReloadCommand;
        private EditItemCommand _EditItemCommand;
        private RemoveItemCommand _removerItemCommand;
        private CopyItemCommand _copyItemCommand;
        private PasetItemCommand _pasetItemCommand;
        private AddMarginItemCommand _addMarginItemCommand;
        private SetItemSpacingCommand _setItemSpacingCommand;
        private int _itemMarginTop = 1;
        private int _itemMarginBottom = 1;

        public toolbarsViewModle()
        {
            _items = new ObservableCollection<toolbarItemViewModle>();solid_items = new ObservableCollection<toolbarItemViewModle>();
            _SaveCommand = new SaveItemCommand(this);
            _ReloadCommand = new ReloadItemCommand(this);
            _EditItemCommand = new EditItemCommand(this);
            _removerItemCommand = new RemoveItemCommand(this);
            _copyItemCommand = new CopyItemCommand(this);
            _pasetItemCommand = new PasetItemCommand(this);
            _addMarginItemCommand = new AddMarginItemCommand(this);
            _setItemSpacingCommand = new SetItemSpacingCommand(this);

        }

        /// <summary>
        /// 从 ToolBarItem 配置 xml 重新加载列表（保留当前 ViewModel 与命令绑定）。
        /// </summary>
        public void ReloadFromXml()
        {
            toolbarsViewModle loaded = CfgHelpPersonXml.ReadToolBarItem(WebAddress.ToolBarItemConfig);
            if (loaded == null)
            {
                NewItemsTools();
                NewSolidItems();
                this.OnPropertyChanged("Items");
                this.OnPropertyChanged("SolidItems");
                this.OnPropertyChanged("ItemRowMargin");
                return;
            }
            ItemMarginTop = loaded.ItemMarginTop;
            ItemMarginBottom = loaded.ItemMarginBottom;
            Items = loaded.Items != null
                ? loaded.Items
                : new ObservableCollection<toolbarItemViewModle>();
            SolidItems = loaded.SolidItems != null
                ? loaded.SolidItems
                : new ObservableCollection<toolbarItemViewModle>();
            if (Items.Count < 1)
            {
                NewItemsTools();
                this.OnPropertyChanged("Items");
            }
            if (SolidItems.Count < 1)
            {
                NewSolidItems();
                this.OnPropertyChanged("SolidItems");
            }
        }
        public void NewItemsTools()
        { 
            _items = new ObservableCollection<toolbarItemViewModle>();
            _items.Add(new toolbarItemViewModle("将脚本"));
            _items.Add(new toolbarItemViewModle("拖拽到"));
            _items.Add(new toolbarItemViewModle("这里"));
        }
        public void NewSolidItems() 
        {
            solid_items = new ObservableCollection<toolbarItemViewModle>();
            solid_items.Add(new toolbarItemViewModle("这"));
            solid_items.Add(new toolbarItemViewModle("也"));
            solid_items.Add(new toolbarItemViewModle("可"));
            solid_items.Add(new toolbarItemViewModle("以"));
        }
        public void NewTools()
        {
            if (_items.Count < 1)
            { 
                NewItemsTools();
            }
            if (solid_items.Count < 1)
            {
                NewSolidItems();
            }
        }
        public void Set_Items_Margin_Up()
        {
            int count = Items.Count / 5;
            //ScriptsUtilities.print(count.ToString());
            for (int i = 0; i < count; i++)
            { 
                int id =(i+1)*5;
                if (id < Items.Count)
                {
                    if (Items[id] != null)
                    {
                        Items[id].Space = true;
                    }
                }
            }
        }
        public AddMarginItemCommand AddMarginCommand
        {
            get { return _addMarginItemCommand; }
        }
        public SetItemSpacingCommand SetItemSpacingCommand
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
        public System.Windows.Thickness ItemRowMargin
        {
            get { return new System.Windows.Thickness(0, ItemMarginTop, 0, ItemMarginBottom); }
        }
        public PasetItemCommand GPasetItemCommand
        {
            get { return _pasetItemCommand; }
        }
        public CopyItemCommand GCopyItemCommand
        {
            get { return _copyItemCommand; }
        
        }
        public SaveItemCommand GSaveItemCommand
        {
            get { return _SaveCommand; }
        }
        public ReloadItemCommand GReloadItemCommand
        {
            get { return _ReloadCommand; }
        }
        public EditItemCommand GEditItemCommand
        {
            get { return _EditItemCommand; }
        }
        public RemoveItemCommand RemoverItem
        {
            get { return _removerItemCommand; }
        }
        [XmlIgnore]
        public int TimeEnd {

            get { return end; }
            set { 
                end = value;
                this.OnPropertyChanged("TimeEnd");
            }
        }
        [XmlIgnore]
        public int TimeStart {

            get { return start; } 
            set
            {
                start = value;
                this.OnPropertyChanged("TimeStart");
            }
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
            get {return ScriptsUtilities.mGetColor(1); }
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

        public void AddNewCommitItem(ObservableCollection<toolbarItemViewModle> items , string commit, int index, string name )
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
        public void AddNewCommitItem(string commit, int index,string name )
        {
            //bool isfind = _items.Any<toolbarItemViewModle>(p => p.Commit.Equals(commit));
            if (!FindFilePath(commit))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(string.Concat("script-", index + 1 ));
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
        public void AddNewFileItem(string file_path,int index)
        {
            //这玩意在 3dsMax 中无法执行
            //bool isfind = _items.Any<toolbarItemViewModle>(p => p.Path.Equals(file_path));

            if (!FindFilePath(file_path))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(System.IO.Path.GetFileNameWithoutExtension(file_path));
                item.Path = file_path;
                item.Commit = toolbarItemViewModle.BuildScriptCommit(file_path);
                if (index >= 0 && index < _items.Count)
                {
                    _items.Insert(index, item);
                }
                else {
                    _items.Add(item);
                }
            }

        }
        public void AddNewFileItem(ObservableCollection<toolbarItemViewModle> items , string file_path, int index)
        {
            if (!FindFilePath(items, file_path))
            {
                toolbarItemViewModle item = new toolbarItemViewModle(System.IO.Path.GetFileNameWithoutExtension(file_path));
                item.Path = file_path;
                item.Commit = toolbarItemViewModle.BuildScriptCommit(file_path);
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
        public bool FindFilePath(ObservableCollection<toolbarItemViewModle> items,string file_path)
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
        



        public ObservableCollection<toolbarItemViewModle> SolidItems
        {
            get { return solid_items; }
            set {
                solid_items = value;
                this.OnPropertyChanged("SolidItems");
            }
        }
        public ObservableCollection<toolbarItemViewModle> Items
        {
            get { return _items; }
            set { _items = value; 
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

    /// <summary>
    /// 剪贴板中的完整按钮快照（与纯 Path/Commit 文本区分）。
    /// </summary>
    [XmlRoot("ToolbarItem")]
    public class ToolbarItemClipboardData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Commit { get; set; }
        public string ToolTip { get; set; }
        public bool Space { get; set; }
    }

    public static class ToolbarItemClipboard
    {
        private const string Prefix = "NDToolsBox.ToolbarItem.v1\n";

        public static void Copy(toolbarItemViewModle item)
        {
            if (item == null)
            {
                return;
            }
            var data = new ToolbarItemClipboardData
            {
                Name = item.Name ?? string.Empty,
                Path = item.Path ?? string.Empty,
                Commit = item.Commit ?? string.Empty,
                ToolTip = item.StoredToolTip,
                Space = item.Space
            };
            var serializer = new XmlSerializer(typeof(ToolbarItemClipboardData));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                Clipboard.SetText(Prefix + writer.ToString(), TextDataFormat.UnicodeText);
            }
        }

        /// <summary>
        /// 将剪贴板内容应用到目标按钮。优先完整按钮快照；否则按旧逻辑粘贴 Path 或 Commit。
        /// </summary>
        public static bool PasteOnto(toolbarItemViewModle target)
        {
            if (target == null || !Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                return false;
            }
            string text = Clipboard.GetText(TextDataFormat.UnicodeText);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.StartsWith(Prefix, StringComparison.Ordinal))
            {
                string xml = text.Substring(Prefix.Length);
                try
                {
                    var serializer = new XmlSerializer(typeof(ToolbarItemClipboardData));
                    using (var reader = new StringReader(xml))
                    {
                        var data = serializer.Deserialize(reader) as ToolbarItemClipboardData;
                        if (data == null)
                        {
                            return false;
                        }
                        target.ApplyFields(data.Name, data.Path, data.Commit, data.ToolTip, data.Space);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            // 兼容：外部粘贴文件路径 / MaxScript / Python 文本
            if (!toolbarItemViewModle.TryResolveScriptText(text, out string name, out string path, out string commit))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(path))
            {
                // 路径黏贴：更新 Path/Commit，保留原显示名（若有）
                string keepName = string.IsNullOrEmpty(target.Name) ? name : target.Name;
                target.ApplyFields(keepName, path, commit, target.StoredToolTip, target.Space);
                return true;
            }

            target.ApplyFields(name, string.Empty, commit, target.StoredToolTip, target.Space);
            return true;
        }
    }
}
