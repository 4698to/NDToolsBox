using Autodesk.Max;
using Autodesk.Max.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using UiViewModels.Actions;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Windows.Controls;

namespace NDToolsBox
{
    public class SelectSetActionItem : ActionItem
    {
        private SelectSetToolBar dialog;
        private WindowInteropHelper windowHandle;
        private double height;
        private double width;
        public Graphics g = Graphics.FromHwnd(IntPtr.Zero);

        private string icon;
        public override bool ExecuteAction()
        {
            

            if (dialog == null || !dialog.IsVisible)
            {
                dialog = new SelectSetToolBar();
                dialog.WindowStyle = WindowStyle.None;
                //dialog.ResizeMode = ResizeMode.CanMinimize;
                //dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;


                try_set_widow_pos();

                //dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;


                dialog.AllowsTransparency = true;
                dialog.Background = new SolidColorBrush(Colors.Transparent);


                //dialog.MouseMove += new System.Windows.Input.MouseEventHandler(dockpanel_MouseMove);
                //鼠标右键点击
                dialog.MouseDown += new System.Windows.Input.MouseButtonEventHandler(dockpanel_MouseDown);

                dialog.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(dockpanel_DoubleClick);
                dialog.AllowDrop = true;

                
                //dialog.Closing
                dialog.Closed += new EventHandler(Close);

                //dialog.ResizeMode = System.Windows.ResizeMode.NoResize;
                
                windowHandle = new System.Windows.Interop.WindowInteropHelper(dialog);
                windowHandle.Owner = ManagedServices.AppSDK.GetMaxHWND();

                dialog.MyListBoxCloseButton.Click += new RoutedEventHandler(close_dialog);
                dialog.MyListBoxSettingsButton.Click += new RoutedEventHandler(openSettings_dialog);
                dialog.MyListBoxResetButton.Click += new RoutedEventHandler(ResetSelSet);

                ManagedServices.AppSDK.ConfigureWindowForMax(dialog);
                icon = ScriptsUtilities.iconUri();
                if (icon != null)
                {
                    Uri iconUri = new Uri(icon);
                    dialog.Icon = BitmapFrame.Create(iconUri);
                }

                dialog.UpSelSet();

                dialog.RegisterNamedSelSet();
               
                dialog.Show();
                System.Windows.Size p = dialog.MyListBox.RenderSize;
                p.Height = dialog.MyListBox.RenderSize.Height / g.DpiY;
                p.Width = dialog.MyListBox.RenderSize.Width / g.DpiX;
                //try_set_widow_size(MyListBox.RenderSize);
                dialog.try_set_widow_size(p);
                
                //dialog.try_set_widow_size(dialog.MyListBox.RenderSize);
            }
            return true;
        }
        private void try_set_widow_size(System.Windows.Size size)
        {
            if (size.Width > 50)
            {
                dialog.Width = size.Width + 160d;
            }
            else { 
                dialog.Width = 170d;
            }
            dialog.Height = 40d;
        }
        private void try_set_widow_pos()
        {
            double top_ = 10.0d / g.DpiY;
            double left_ = 20.0d / g.DpiX;
            try
            {
                top_ = Properties.Settings.Default.Top;
                left_ = Properties.Settings.Default.Left;
            }
            catch (Exception ex)
            {

            }

            /*if (width > SystemParameters.PrimaryScreenWidth | width <= 50.0d )
            {
                width = 150.0d;
            }
            
            if (height > SystemParameters.PrimaryScreenHeight | height <= 20.0d)
            {
                height = 50.0d; 
            }*/
            if (top_ > SystemParameters.PrimaryScreenHeight | top_ <= 50.0d / g.DpiY)
            {
                top_ = SystemParameters.PrimaryScreenHeight * 0.5d / g.DpiY ;
            }
            if (left_ > SystemParameters.PrimaryScreenWidth | left_ <= 10.0d / g.DpiX)
            {
                left_ = 80.0d / g.DpiX;
            }
             
                //dialog.Width = width;
                dialog.Height = 40d / g.DpiY;
                dialog.Top = top_/g.DpiY;
                dialog.Left = left_/g.DpiX;
            
           
             //dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
             //dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
        }
        public void ResetSelSet(object sender, EventArgs e)
        {
            if (dialog != null)
            {
                dialog.UpSelSet();
                dialog.UpdateLayout();

                dialog.try_set_widow_size(dialog.MyListBox.RenderSize);
            }
        }
        private void openSettings_dialog(object sender, EventArgs e)
        {

            if (dialog._filterWidow == null || !dialog._filterWidow.IsVisible)
            {
                dialog._filterWidow = new filterSettings();
                System.Windows.Point m = Mouse.GetPosition(null);
                System.Windows.Point new_m = dialog.MyListBoxSettingsButton.PointToScreen(m);

                dialog._filterWidow.Left = SystemParameters.PrimaryScreenWidth * 0.5d;
                dialog._filterWidow.Top = SystemParameters.PrimaryScreenHeight * 0.5d;
                dialog._filterWidow.btn_ok.Click += new RoutedEventHandler(ResetSelSet);
                dialog._filterWidow.btn_import.Click += new RoutedEventHandler(ResetSelSet);

                dialog._filterWidow.Owner = dialog;

                dialog._filterWidow.Show();
            }
            
        }
        private void saveSettings()
        {
            Properties.Settings.Default.MainRestoreBounds = dialog.RestoreBounds;
            Properties.Settings.Default.Left = dialog.Left;
            Properties.Settings.Default.Top = dialog.Top;
            Properties.Settings.Default.Width = dialog.Width;
            Properties.Settings.Default.Height = dialog.Height;
            Properties.Settings.Default.Save();
        }
        private void close_dialog(object sender, EventArgs e)
        {
            if (dialog != null)
            {
                dialog.Close();
                saveSettings();
            }
        }
        private void dockpanel_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dialog != null)
            {
                saveSettings();
                dialog.Close();
            }
        }
        private void dockpanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Window w = sender as System.Windows.Window;

            if (e.ChangedButton == MouseButton.Left)
            {
               
                try
                {
                    w.DragMove();
                }
                catch (Exception) { }
            }
            /*else {
                if (dialog.ResizeMode.Equals(ResizeMode.CanMinimize))
                {
                    //w.WindowStyle = WindowStyle.SingleBorderWindow;
                    dialog.ResizeMode = ResizeMode.CanResizeWithGrip;
                }
                else {
                    dialog.ResizeMode = ResizeMode.CanMinimize;
                }
            }*/


        }
        private void dockpanel_MouseMove(object sender, EventArgs e)
        {
            //ScriptsUtilities.print("text move");
        }
        public override IMaxIcon Icon
        {
            get { return ScriptsUtilities.global.MaxBmpFileIcon.Create("NDBoxMao", 1); }
        }
        public override IMaxIcon IconImp
        {
            get { return Icon; }
        }
        public override bool Icon_
        {
            get { return true; }
        }

        public override int IconIndex
        {
            get { return 1; }
        }
        /*public override void Dispose()
        {
            Cleanup();
            base.Dispose();
        }*/

        public void Close(object sender, EventArgs e)
        {
            if (dialog != null)
            {
                dialog.UnRegisterNamedSelSet();
            }
           
            //Cleanup();
        }

        private void Cleanup()
        {
            //ScriptsUtilities.ip.PushPrompt("Cleanup!");

            /*ScriptsUtilities.SaveDialogLocation("top", dialog.Top.ToString());
            ScriptsUtilities.SaveDialogLocation("left", dialog.Left.ToString());
            ScriptsUtilities.SaveDialogLocation("width", dialog.Width.ToString());
            ScriptsUtilities.SaveDialogLocation("height", dialog.Height.ToString());

            ScriptsUtilities.Save();*/
        }

        public override int Id_ => 1;

        public override string ButtonText
        {
            get { 
                return "选择集工具条";
                //return "SelSet-Float";
            }
        }

        public override string MenuText
        {
            get { 
                return "选择集工具条";
                //return "SelSet-Float";
            }
        }

        public override string DescriptionText
        {
            get { return "SelSet-MenuItem-Float"; }
        }
        
        public override string CategoryText
        {
            get { return "A-NDTools-Float"; }
            //get { return "A-NDTools"; }


        }

        public override bool IsChecked_
        {
            get { return false; }
        }

        public override bool IsItemVisible
        {
            get { return true; }
        }

        public override bool IsEnabled_
        {
            get { return true; }
        }
    }


    /*
    public class SelectSetCuiDock: CuiDockableContentAdapter
    {
        
        public override string ActionText
        {
            get { return "SelectSet Window"; }
        }
        public override string Category
        {
            get { return InternalCategory; }
        }
        public override string ButtonText
        {
            get
            {
                return "SelectSet-Dock";
            }
        }
        public override string WindowTitle
        {
            get { return InternalActionText; }
        }
        public override string InternalActionText
        {
            get { return "SelectSetBar-Dock"; }
        }
        public override string MenuText
        {
            get { return InternalActionText; }
        }
        public override string InternalCategory
        {
            get { return "A-NDTools"; }
        }


        public override Type ContentType
        {
            get { return typeof(SelectSetToolBar); }
        }

        public override object CreateDockableContent()
        {

            return new SelectSetToolBar();
        }


        public override DockStates.Dock DockingModes
        {
            get
            {
                return DockStates.Dock.Bottom | DockStates.Dock.Top | DockStates.Dock.Floating;
            }

        }
        public override string ObjectName
        {
            get
            {
                return "ndbox_setectSetbar";
            }
        }
        public override bool DocksMaximized
        {
            get
            {
                return true;

            }
        }

        public override bool IsMainContent
        {
            get
            {
                return true;
            }
        }
    }
    */
}
