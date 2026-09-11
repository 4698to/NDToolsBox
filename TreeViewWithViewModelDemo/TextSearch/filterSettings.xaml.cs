using Autodesk.Max;
using Autodesk.Max.IAnimLayerControlManager;
using ManagedServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NDToolsBox
{
    /// <summary>
    /// filterSettings.xaml 的交互逻辑
    /// </summary>
    public partial class filterSettings : Window
    {
        public filterSettings()
        {
            InitializeComponent();
            getdata();

        }
        public void getdata()
        {
            filter_a.IsChecked = Properties.Settings.Default.filter_a;
            filter_b.IsChecked = Properties.Settings.Default.filter_b;
            filter_c.IsChecked = Properties.Settings.Default.filter_c;

            string a = Properties.Settings.Default.filter_a_text;
            string b = Properties.Settings.Default.filter_b_text;
            string c = Properties.Settings.Default.filter_c_text;
            
            if (!string.IsNullOrEmpty(a))
            {
                filter_a_text.Text = a;
            }
            if (!string.IsNullOrEmpty(b))
            {
                filter_b_text.Text = b;
            }
            if (!string.IsNullOrEmpty(c))
            {
                filter_c_text.Text = c;
            }
            

            filter_back.IsChecked = Properties.Settings.Default.filter_back;

        }
        public void save()
        {
            Properties.Settings.Default.filter_a = (bool)filter_a.IsChecked;
            Properties.Settings.Default.filter_b = (bool)filter_b.IsChecked;
            Properties.Settings.Default.filter_c = (bool)filter_c.IsChecked;

            Properties.Settings.Default.filter_a_text = filter_a_text.Text;
            Properties.Settings.Default.filter_b_text = filter_b_text.Text;
            Properties.Settings.Default.filter_c_text = filter_c_text.Text;

            Properties.Settings.Default.filter_back = (bool)filter_back.IsChecked;

            Properties.Settings.Default.Save();
        }
        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            //save();
            this.Close();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            save();
            

            this.Close();

        }

        private void filter_a_text_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptsUtilities.EnableAccelerators();
        }

        private void filter_a_text_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptsUtilities.DisableAccelerators();

        }

        private void btn_import_Click(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = AppSDK.GetMaxHWND();
            string file_path = ScriptsUtilities.GetDefaultSelSetFileName();
            string initia_path = ScriptsUtilities.GetDefaultSelSetDirectory();

            FileDialogFilterList fileDialogFilterList = new ManagedServices.FileDialogFilterList("xml (*.xml)|*.xml|");

            if (PathSDK.DoMaxOpenDialog(hwnd, "import selName xml", ref file_path, ref initia_path, fileDialogFilterList))
            {
                string fullPath = ScriptsUtilities.CombineDialogPath(file_path, initia_path);
                if (File.Exists(fullPath))
                {
                    ScriptsUtilities.ImportSeleSet(fullPath);
                }
                else
                {
                    MessageBox.Show(
                        $"导入失败：文件不存在\n{fullPath}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = AppSDK.GetMaxHWND();
            string file_path = ScriptsUtilities.GetDefaultSelSetFileName();
            string initia_path = ScriptsUtilities.GetDefaultSelSetDirectory();

            FileDialogFilterList fileDialogFilterList = new ManagedServices.FileDialogFilterList("xml (*.xml)|*.xml|");

            if (PathSDK.DoMaxSaveAsDialog(hwnd, "export selName xml", ref file_path, ref initia_path, fileDialogFilterList))
            {
                string fullPath = ScriptsUtilities.EnsureXmlExtension(
                    ScriptsUtilities.CombineDialogPath(file_path, initia_path));
                ScriptsUtilities.ExportSeleSet(fullPath);
            }
        }
    }
    
}
