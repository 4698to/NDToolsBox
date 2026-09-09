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
        public SelectSetToolBar()
        {
            InitializeComponent();
            _itemlist = new SelectSetViewModel();
            UpSelSet();
            
            base.DataContext = _itemlist;
            
            
            

#if M2015 || M2016
            SelSetName_deleg = new GlobalDelegates.Delegate5(SelSetName_Delegate6_Callback);
#else
            SelSetName_deleg = new GlobalDelegates.Delegate5(SelSetName_Delegate5_Callback);
#endif
            
        }
        public void try_set_widow_size(System.Windows.Size size)
        {
            base.UpdateLayout();

            if (size.Width > 50)
            {
                base.Width = size.Width + 160d;
            }
            else
            {
                base.Width = 170d;
            }
            base.Height = 40d;
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
        }
        private void SelSetName_Delegate5_Callback(IntPtr param0, INotifyInfo param1)
        {
            UpSelSet();
        }
        private void SelSetName_Delegate6_Callback(IntPtr param0, IntPtr param1)
        {
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
    }
}
