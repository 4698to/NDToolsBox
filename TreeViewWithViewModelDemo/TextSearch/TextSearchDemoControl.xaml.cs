
using System.Windows.Controls;
using System.Windows.Input;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Collections.Generic;
using MaxToolbars.Toobars;


namespace NDToolsBox
{
    public partial class TextSearchDemoControl : UserControl
    {
        public FamilyTreeViewModel _familyTree;
        //public string message;

        private static int serverType = 1; // 1 是用腾讯服务器，2 是公司内网
        private Ini config;


        //工具盒子显示的工具列表
        public List<string> ToolsLists ;
        
        public string MaxRoot; 
        public TextSearchDemoControl()
        {
            InitializeComponent();
            config = new Ini(WebAddress.iniConfig);

            
            //如果无法链接服务器将无法正常显示

            /*if (WebAddress.pingIp(serverType))
            {
                message = string.Concat(message, "网络链接正常 ");
                MaxComm.print("网络链接正常 ");
                //如果网络正常，就下载 脚本名单
                try
                {
                    WebAddress.downloadScriptList(WebAddress.dataModelFile, serverType);
                }
                catch {
                    MaxComm.print($"下载错误 {WebAddress.dataModelFile}");
                }
            }
            else
            {
                message = string.Concat(message, "无法链接服务器");
                MaxComm.print("无法链接服务器 ");
            }*/
            //message += "NDToolsBox Test 2015";

            MaxRoot = ScriptsUtilities.GetMaxRoot();

            //Person rootPerson = Database.GetRootPathTree(ScriptsUtilities.GetMaxRoot(), WebAddress.dataModelFile);
            //Person rootPerson = CfgHelpPerson.Readjson(WebAddress.dataModelFile, MaxRoot);

            Person rootPerson = CfgHelpPersonXml.ReadToolListXml(WebAddress.dataModelFileXml, MaxRoot);
            
            GetToolsList(ref rootPerson);

            if (!Directory.Exists(WebAddress.Resources))
            {
                Directory.CreateDirectory(WebAddress.Resources);
            }

            CfgHelpPersonXml.GetPathFiles(ref rootPerson,WebAddress.Resources);
            GetResourcesPath(ref rootPerson);


             // Create UI-friendly wrappers around the 
             // raw data objects (i.e. the view-model).
             _familyTree = new FamilyTreeViewModel(rootPerson);


            // Let the UI bind to the view-model.
            base.DataContext = _familyTree;
            //显示的版本号
            //_familyTree.toolsVersion = rootPerson.Name;

            _familyTree.toolsVersion = GetInstallVersion.ToString();

            //显示的工具名字
            _familyTree.toolsName = rootPerson.Path;
            _familyTree.message = rootPerson.message;

            //message = "message";

            // 启动时远程版本检测/下载已暂时关闭
            //CheckRemoteVersionAsync();
            
        }
        /*
        private async void CheckRemoteVersionAsync()
        {
            await _familyTree.downloadVersion();
            float localVersion;
            if (float.TryParse(_familyTree.toolsVersion, out localVersion)
                && localVersion < _familyTree.remoteVersion)
            {
                _familyTree.IsUpdata = true;
            }
        }
        */
        public void GetToolsList(ref Person rootPerson)
        {
            int count;
            if (!int.TryParse(config.GetValue("Count", "ToolsList", "0"), out count))
            {
                count = 0;
            }
            for (int i = 0; i < count; i++)
            {
                string path = config.GetValue(i.ToString(), "ToolsList",string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    Person tools_person = CfgHelpPersonXml.ReadToolListXml(path, MaxRoot);
                    if (tools_person.GetCount() > 0) 
                    {
                        rootPerson.Children.AddRange(tools_person.Children);
                    }
                }
            }
        }
        public void GetResourcesPath(ref Person p)
        {
            int pathcount;
            if (!int.TryParse(config.GetValue("Count","Resources",  "0"), out pathcount))
            {
                pathcount = 0;
            }
            for (int i = 0; i < pathcount; i++)
            {
                string path = config.GetValue(i.ToString(), "Resources", string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    CfgHelpPersonXml.GetPathFiles(ref p, path);
                }
            }

        }
        /// <summary>
        /// 获取当前安装的版本号
        /// </summary>
        public float GetInstallVersion
        {
            get
            {
                string v = config.GetValue("Version", "NDBoxDownload", "0");
                float result;
                return float.TryParse(v, out result) ? result : 0f;
            }
            set
            {
                config.WriteValue("Version", "NDBoxDownload", value.ToString());
                config.Save();
            }
        }
        void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _familyTree.SearchCommand.Execute(null);    
        }
        //搜索按钮
        public void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button b = sender as Button;
            //Console.WriteLine($"Click {b.Name}, Mode {b.ClickMode.ToString()}");
        }
        public void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            //https://blog.csdn.net/qq_41569198/article/details/106504976
            PersonViewModel item = e.NewValue as PersonViewModel;
            if (item != null)
            {
                _familyTree.message = item.message;
            }
            PersonViewModel item_old = e.OldValue as PersonViewModel;
            if (item_old != null && !item_old.Equals(item) )
            {
                item_old.CommandState = "";
            }
        }
        public void TvDepartment_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            //鼠标双击
            TreeView tv = (TreeView)sender;
            PersonViewModel item = (PersonViewModel)tv.SelectedValue;
            if (!item.IsGrouping)
            {
                if (File.Exists(item.Path) && item.ExtensionType != null)
                {
                    if (item.ExtensionType.Equals(".mse") || item.ExtensionType.Equals(".ms"))
                    {

                        bool commandState = ScriptsUtilities.FileinScriptEx(item.Path);
                        if (!commandState)
                        {
                            ScriptsUtilities.print($"出错{item.Path}-> {item.ExtensionType}");
                        }
                        else
                        {
                            ScriptsUtilities.print($"{item.Path}-> {item.ExtensionType}");
                        }
                    }
                    else {
                        MessageBox.Show(
                        "此类型资源还不支持.",
                        "抱歉",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                        );
                        ScriptsUtilities.print($"{item.Path} -> {item.ExtensionType}");
                    }
                }
                else
                {
                    MessageBox.Show(
                        "此资源不存在.",
                        "更新试试",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                        );
                    ScriptsUtilities.print($"no exists : {item.Path} -> {item.ExtensionType}");
                }
            }
        }

        private void help_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = (Button)e.Source;

            PersonViewModel item = (PersonViewModel)button.DataContext;

            if (item == null)
            {
                ScriptsUtilities.print("help_Click");
                return;
            }
            //打开鼠标提示中的网页链接
            //Process.Start(new ProcessStartInfo(button.ToolTip.ToString()));
            if (!string.IsNullOrEmpty(item.HelpUrl))
            {
                ScriptsUtilities.print(item.HelpUrl);
                //Process.Start(new ProcessStartInfo(item.HelpUrl));
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.HelpUrl));
                }
                catch (Exception ex)
                {
                    ScriptsUtilities.print($"打开帮助失败: {ex.Message}");
                    MessageBox.Show($"无法打开帮助链接:\n{item.HelpUrl}", "帮助", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            else {
               
            }
            //ScriptsUtilities.print($" HelpUrl {item.Name}");

            //Console.WriteLine(button.ToolTip);
            //https://blog.csdn.net/qq_36651243/article/details/130698988
            //https://blog.csdn.net/weixin_30770495/article/details/95357619

        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)e.Source;
            if (textbox.Text.Length == 0)
            {
                //Console.WriteLine("no search");
            }

            //Console.WriteLine(e);
        }

        private void textclear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var bt =(Button)sender;
            //FamilyTreeViewModel tr = (FamilyTreeViewModel)bt.DataContext;
            if (_familyTree.SearchText.Length > 0)
            {
                _familyTree.SearchText = string.Empty;
                _familyTree.RemoveSearchMatches();
            }
            else {

            }

        }

        private void searchTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptsUtilities.DisableAccelerators();
        }

        private void searchTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //AppSDK.EnableAccelerators();
            ScriptsUtilities.EnableAccelerators();
        }
        [DllImport("kernel32.dll")]
        public static extern int WinExec(string programPath, int operType);
        private void versionname_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //打开下载程序
            if (File.Exists(WebAddress.UpdataExE))
            {
                var result = WinExec(WebAddress.UpdataExE, 5);
            }
            else {
                MessageBox.Show($"未找到 {WebAddress.UpdataExE}");
            }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        //I'd double check this constant, just in case
        static uint WM_CLOSE = 0x10;

        private void CloseContainingWindow(System.Windows.Media.Visual visual)
        {
            // Find the containing HWND for the Visual in question
            HwndSource wpfHandle = PresentationSource.FromVisual(this) as HwndSource;
            if (wpfHandle == null)
            {
                throw new Exception("Could not find Window handle");
            }

            // Trace up the window chain, to find the ultimate parent
            IntPtr hWindow = wpfHandle.Handle;
            //while (true)
            //{
            //    IntPtr parentHWindow = GetParent(hWindow);
            //    if (parentHWindow == (IntPtr)0) break;
            //    hWindow = parentHWindow;
            //}

            // Now send the containing window a close message
            SendMessage(hWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private void close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.CloseContainingWindow(ndbox);
            //Application.Current.Shutdown();
        }

        private void ndbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine(e.Source);
        }

        private void TreeView_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;
            if (tvi == null || e.Handled) return;

            var vm = tvi.DataContext as PersonViewModel;
            if (vm == null || !vm.IsGrouping) return;

            // 仅响应用户点击分组；避免与代码侧自动展开互相打架
            tvi.IsExpanded = !tvi.IsExpanded;
            tvi.IsSelected = false;
            e.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {

        }
        /// <summary>
        /// 拖拽出box 到外面的字符串
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = e.OriginalSource;
            if (item == null) return;
            var context = item as TextBlock;
            if (context != null)
            {
                var person = context.DataContext as PersonViewModel;
                if (person != null && !person.IsGrouping)
                {
                    string data = $"--NDDrop;{person.Name};\r\n{toolbarItemViewModle.BuildScriptCommit(person.Path)}";
                    string dataFormat = System.Windows.DataFormats.UnicodeText;
                    System.Windows.DataObject dataObject = new System.Windows.DataObject(dataFormat, data);
                    System.Windows.DragDropEffects dde = DragDrop.DoDragDrop(context, dataObject, System.Windows.DragDropEffects.Copy);
                }
            }
        }

        private void setting_Click(object sender, RoutedEventArgs e)
        {
#if M2015 || M2016 || M2017 || M2018
            ResetMaxCUI.Reset();
#endif

        }

        private void liName_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock item = e.OriginalSource as TextBlock;
            if (item != null)
            {
                PersonViewModel i = item.DataContext as PersonViewModel;
                if (i != null)
                {
                    /*if (!string.IsNullOrEmpty(i.message))
                    {
                        ScriptsUtilities.print(i.message);
                    }*/
                    _familyTree.message = i.message;
                }
            }
        }
    }
}