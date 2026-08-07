using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using ActionItem = Autodesk.Max.Plugins.ActionItem;
using Autodesk.Max;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace NDToolsBox
{
    //这种入口不可停靠，但是有图标，拖到工具栏上不能保存UI
    class NDBoxActionItem : ActionItem
    {
        private TextSearchDemoControl wpf;
        private Window dialog;
        private WindowInteropHelper windowHandle;
        private double height;
        private double width;
        private string icon;
        public override bool ExecuteAction()
        {
            if (wpf == null)
            {
                wpf = new TextSearchDemoControl();
            }

            if (dialog == null || !dialog.IsVisible)
            {
                dialog = new System.Windows.Window();
                //dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;

                dialog.Top = 20;//ScriptsUtilities.GetDialogLocationt("top");
                dialog.Left = 50; //ScriptsUtilities.GetDialogLocationt("left");

                height = 300;//ScriptsUtilities.GetDialogLocationt("height");
                width = 480; //ScriptsUtilities.GetDialogLocationt("width");
                if (height > 0.0d)
                {
                    dialog.Height = height;

                }
                else {
                    dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                }
                if (width > 0.0d)
                {
                    dialog.Width = width;
                }
                //dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                //dialog.Closing
                dialog.Closed += new EventHandler(Close);
                //dialog.ResizeMode = System.Windows.ResizeMode.NoResize;
                dialog.Content = wpf;
                dialog.MinWidth = wpf.MinWidth;
                dialog.MaxWidth = wpf.MaxWidth;
                windowHandle = new System.Windows.Interop.WindowInteropHelper(dialog);
                windowHandle.Owner = ManagedServices.AppSDK.GetMaxHWND();

                

                ManagedServices.AppSDK.ConfigureWindowForMax(dialog);
                icon = ScriptsUtilities.iconUri();
                if (icon != null)
                {
                    Uri iconUri = new Uri(icon);
                    dialog.Icon = BitmapFrame.Create(iconUri);
                }
                dialog.Show();
                try_set_widow_pos();
            }
            return true;
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
            get {return true; }
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
                saveSettings();
            }
        }
        private void saveSettings()
        {
            
            Properties.Settings.Default.Box_L = dialog.Left;
            Properties.Settings.Default.Box_T = dialog.Top;
            Properties.Settings.Default.Box_W = dialog.Width;
            Properties.Settings.Default.Box_H = dialog.Height;
            Properties.Settings.Default.Save();
        }
        private void try_set_widow_pos()
        {
            double top_ = 50.0d;
            double left_ = 50.0d;
            double w_ = 480d;
            double h_ = 300d;
            try
            {
                top_ = Properties.Settings.Default.Box_T;
                left_ = Properties.Settings.Default.Box_L;
                w_ = Properties.Settings.Default.Box_W;
                h_ = Properties.Settings.Default.Box_H;
            }
            catch (Exception ex)
            {

            }

            if (top_ > SystemParameters.PrimaryScreenHeight | top_ <= 50.0d)
            {
                top_ = SystemParameters.PrimaryScreenHeight * 0.5d;
            }
            if (left_ > SystemParameters.PrimaryScreenWidth | left_ <= 20.0d)
            {
                left_ = 80.0d;
            }

            if (h_ > SystemParameters.PrimaryScreenHeight | h_ <= 300.0d)
            {
                h_ = 300d;
            }
            if (w_ > SystemParameters.PrimaryScreenWidth | w_ <= 480.0d)
            {
                w_ = 480.0d;
            }
            dialog.Height = h_;
            dialog.Width = w_;

            dialog.Top = top_;
            dialog.Left = left_;

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

        public override int Id_ => 4698;

        public override string ButtonText
        {
            get { 
                return "天晴盒子";
                //return "NDBox-Float"; 
            }
        }

        public override string MenuText
        {
            get { 
                return "天晴盒子";
                //return "NDBox-Float";
            }
        }

        public override string DescriptionText
        {
            get { return "NDBox - MenuItem"; }
        }

        public override string CategoryText
        {
            //get { return "NDTools-MenuItem"; }
            get { return "A-NDTools-Float"; }

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
}
