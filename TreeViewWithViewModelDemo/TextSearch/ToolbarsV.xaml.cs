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
            if (e.Data.GetDataPresent(DataFormats.UnicodeText)||(e.Data.GetDataPresent(DataFormats.FileDrop)))
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
                        _itemlist.AddNewCommitItem((string)str, item_index , ScriptsUtilities.GetNDBoxMxsCommitScriptName((string)str));
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

            /*// 获取右键单击的 ListBoxItem
            var item = (System.Windows.Controls.ListBox)dynamicContextMenu.PlacementTarget;

            if (item != null)
            {
                RightButtonDown_item_index = item.SelectedIndex;
                // 在这里处理右键单击 ListBoxItem 的逻辑
                e.Handled = true;
            }
            else
            {
                RightButtonDown_item_index = -1;
            }*/
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
            //拖拽 ndbox 中的脚本项
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                var str = e.Data.GetData(DataFormats.UnicodeText);
                if (str != null)
                {
                    if (File.Exists((string)str))
                    {
                        _itemlist.AddNewFileItem(_itemlist.SolidItems,(string)str, item_index);
                    }
                    else
                    {
                        _itemlist.AddNewCommitItem(_itemlist.SolidItems,(string)str, item_index, ScriptsUtilities.GetNDBoxMxsCommitScriptName((string)str));
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
