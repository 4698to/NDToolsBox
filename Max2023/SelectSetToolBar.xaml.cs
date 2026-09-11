using Autodesk.Max;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NDToolsBox
{
    /// <summary>
    /// SelectSetToolBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectSetToolBar : Window
    {
        private SelectSetViewModel _itemlist;
        private GlobalDelegates.Delegate5 SelSetName_deleg;

        public filterSettings _filterWidow;
        private bool _adjustingSize;

        public SelectSetToolBar()
        {
            InitializeComponent();
            _itemlist = new SelectSetViewModel();
            UpSelSet();
            
            base.DataContext = _itemlist;

            this.SizeChanged += SelectSetToolBar_SizeChanged;

#if M2015 || M2016
            SelSetName_deleg = new GlobalDelegates.Delegate5(SelSetName_Delegate6_Callback);
#else
            SelSetName_deleg = new GlobalDelegates.Delegate5(SelSetName_Delegate5_Callback);
#endif
            
        }

        private void SelectSetToolBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_adjustingSize || !e.WidthChanged)
            {
                return;
            }
            // 用户拖宽/拖窄后按新宽度重新换行并适配高度
            AdjustHeightToContent();
        }

        /// <summary>
        /// 按当前窗口宽度让选择集按钮自动换行，并增高窗口以容纳多行。
        /// </summary>
        public void try_set_widow_size(System.Windows.Size size)
        {
            if (Width < MinWidth || double.IsNaN(Width))
            {
                Width = 520;
            }
            // 不再随按钮数量无限加宽；固定/保留当前宽度，超出部分换行
            AdjustHeightToContent();
        }

        private void AdjustHeightToContent()
        {
            if (_adjustingSize)
            {
                return;
            }
            _adjustingSize = true;
            try
            {
                UpdateLayout();
                // Grid 左右边距各 6，工具区与列表间距 8
                const double sidePad = 6;
                const double toolsGap = 8;
                double toolsW = dockpanel.ActualWidth > 1 ? dockpanel.ActualWidth : 120;
                double wrapW = Math.Max(80, ActualWidth - toolsW - sidePad * 2 - toolsGap - 8);
                MyListBox.MaxWidth = wrapW;
                MyListBox.Width = wrapW;
                UpdateLayout();

                double contentH = MyListBox.DesiredSize.Height;
                if (contentH < 26)
                {
                    contentH = 26;
                }
                // 上下边距 4+4 + Border
                double newH = contentH + 8 + 8;
                if (newH < MinHeight)
                {
                    newH = MinHeight;
                }
                Height = newH;
            }
            finally
            {
                _adjustingSize = false;
            }
        }
        public void RegisterNamedSelSet()
        {
            if (SelSetName_deleg != null)
            {
                ScriptsUtilities.global.RegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetCreated);
                ScriptsUtilities.global.RegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetRenamed);
                ScriptsUtilities.global.RegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetDeleted);
                ScriptsUtilities.global.RegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetPreModify);
                ScriptsUtilities.global.RegisterNotification(SelSetName_deleg, null, SystemNotificationCode.FilePostOpen);

            }
            ScriptsUtilities.SelSetListChanged -= OnSelSetListChanged;
            ScriptsUtilities.SelSetListChanged += OnSelSetListChanged;
        }
        public void UnRegisterNamedSelSet()
        {
            if (SelSetName_deleg != null)
            {
                ScriptsUtilities.global.UnRegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetCreated);
                ScriptsUtilities.global.UnRegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetRenamed);
                ScriptsUtilities.global.UnRegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetDeleted);
                ScriptsUtilities.global.UnRegisterNotification(SelSetName_deleg, null, SystemNotificationCode.FilePostOpen);
                ScriptsUtilities.global.UnRegisterNotification(SelSetName_deleg, null, SystemNotificationCode.NamedSelSetPreModify);

            }
            ScriptsUtilities.SelSetListChanged -= OnSelSetListChanged;
        }
        private void OnSelSetListChanged()
        {
            UpSelSet();
        }
        private void SelSetName_Delegate5_Callback(IntPtr param0, INotifyInfo param1)
        {
            if (ScriptsUtilities.SuspendSelSetToolbarRefresh)
            {
                return;
            }
            UpSelSet();
        }
        private void SelSetName_Delegate6_Callback(IntPtr param0, IntPtr param1)
        {
            if (ScriptsUtilities.SuspendSelSetToolbarRefresh)
            {
                return;
            }
            UpSelSet();
        }
        public void UpSelSet()
        {
            
            NameSet[] setNames = ScriptsUtilities.GetSeleSetNames();
                for (int i=0;i< setNames.Length;i++)
                {
                    _itemlist.FindColor(ref setNames[i]);
                    //_itemlist.Items.Add(new SelectSetItem(item.name, item.index, item.color_));
                    //_itemlist.AddNewItem(item);
                }
                _itemlist.Items.Clear();
            foreach (NameSet item in setNames)
            {
                _itemlist.Items.Add(new SelectSetItem(item.name, item.index, item.color_));
            }
            // WPF RenderSize 已是 DIP
            try_set_widow_size(MyListBox.RenderSize);


        }

        private void MyListBoxButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button bt = sender as System.Windows.Controls.Button;
            SelectSetItem item = (SelectSetItem)bt.DataContext;

            if (Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down)
            {
                ScriptsUtilities.SeleSet(item.Index,true);
            }
            else { 
                ScriptsUtilities.SeleSet(item.Index,false);
            }


        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            /*if (e.Key == Key.Return || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                foreach (SelectSetItem i in _itemlist.Items)
                {
                    i.IsEdit = false;
                }
                
            }*/
        }
        private void MyListBoxAddButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptsUtilities.ExecuteMAXScriptScript("NDNamedSelSetsToolsInit.add_new_sele_set()");
        }
        private void MyListBoxCloseButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyListBoxResetButton_Click(object sender, RoutedEventArgs e)
        {
            //UpSelSet();

        }

        private void MyListBoxSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            

        }

        private void ToolbarCloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 复用关闭按钮上由 SelectSetCuiDock 挂接的 Click 处理
            MyListBoxCloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ToolbarSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 复用设置按钮上由 SelectSetCuiDock 挂接的 Click 处理
            MyListBoxSettingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }
}
