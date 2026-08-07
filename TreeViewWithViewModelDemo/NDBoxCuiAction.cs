using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiViewModels.Actions;
using Autodesk.Max;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;

// 没有图标但是可以拖到工具栏上并保存UI
namespace NDToolsBox
{
    public class NDBoxCuiAction : CuiActionCommandAdapter
    {
        private TextSearchDemoControl wpf;
        private Window dialog;
        private WindowInteropHelper windowHandle;
        private double height;
        private double width;
        private string icon;
        public override string ActionText
        {
            get { return InternalActionText; }
        }

        public override string Category
        {
            get { return InternalCategory; }
        }

        public override void Execute(object parameter)
        {
            //ScriptsUtilities.ip.PushPrompt("Yeeehaaaaaaaaaa!");
            if (wpf == null)
            {
                wpf = new TextSearchDemoControl();
            }

            if (dialog == null || !dialog.IsVisible)
            {
                dialog = new System.Windows.Window();
                dialog.Top = 20;//ScriptsUtilities.GetDialogLocationt("top");
                dialog.Left = 20; //ScriptsUtilities.GetDialogLocationt("left");

                height = 300;//ScriptsUtilities.GetDialogLocationt("height");
                width = 480; //ScriptsUtilities.GetDialogLocationt("width");
                if (height > 0.0d)
                {
                    dialog.Height = height;
                }
                else
                {
                    dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                }
                if (width > 0.0d)
                {
                    dialog.Width = width;
                }

                //dialog.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                //dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                //dialog.ResizeMode = System.Windows.ResizeMode.NoResize;
                dialog.Closed += new EventHandler(Close);

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
            }
        }
        public void Close()
        {
            if (dialog == null)
            {
                return;
            }
            dialog.Close();
        }
        public void Close(object sender, EventArgs e)
        {
            Cleanup();
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
        public override string InternalActionText
        {
            get { 
                return "天晴盒子";

                //return "NDBox-Float";
            }
        }

        public override string InternalCategory
        {
            get { return "A-NDTools"; }
        }
        public override bool IsChecked
        {
            get { return false; }
        }

        public override bool IsVisible
        {
            get { return true; }
        }

        
    }
}
