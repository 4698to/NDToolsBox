using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace NDToolsBox
{
    /// <summary>
    /// 用户自定义工具条 Tab 元数据（列表按钮内容仍按 ListIds 各自存 xml）。
    /// </summary>
    public class CustomToolbarTab
    {
        public string Id { get; set; }
        public string Header { get; set; }

        [XmlArray("ListIds")]
        [XmlArrayItem("ListId")]
        public List<string> ListIds { get; set; }

        public CustomToolbarTab()
        {
            ListIds = new List<string>();
        }

        public static CustomToolbarTab CreateNew(string header)
        {
            string id = Guid.NewGuid().ToString("N");
            var tab = new CustomToolbarTab
            {
                Id = id,
                Header = header
            };
            tab.ListIds.Add("CustomTab_" + id + "_1");
            tab.ListIds.Add("CustomTab_" + id + "_2");
            tab.ListIds.Add("CustomTab_" + id + "_3");
            return tab;
        }
    }

    public class ToolBarTabsConfig
    {
        [XmlArray("Tabs")]
        [XmlArrayItem("Tab")]
        public List<CustomToolbarTab> Tabs { get; set; }

        public ToolBarTabsConfig()
        {
            Tabs = new List<CustomToolbarTab>();
        }
    }

    /// <summary>
    /// 简单文本输入弹窗（Tab 重命名、按钮改名共用）。
    /// </summary>
    public static class UiPrompt
    {
        public static string PromptText(string title, string prompt, string defaultValue)
        {
            var window = new Window
            {
                Title = title,
                Width = 320,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };
            var panel = new StackPanel { Margin = new Thickness(12) };
            panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) });
            var box = new TextBox { Text = defaultValue ?? string.Empty, Margin = new Thickness(0, 0, 0, 12) };
            panel.Children.Add(box);
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var ok = new Button { Content = "确定", Width = 70, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "取消", Width = 70, IsCancel = true };
            string result = null;
            ok.Click += (s, e) => { result = box.Text; window.DialogResult = true; };
            cancel.Click += (s, e) => { window.DialogResult = false; };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            window.Content = panel;
            box.SelectAll();
            box.Focus();
            bool? dialog = window.ShowDialog();
            return dialog == true ? result : null;
        }

        /// <summary>
        /// 设置按钮上下间距。取消返回 null。
        /// </summary>
        public static SpacingValues PromptSpacing(string title, int top, int bottom)
        {
            var window = new Window
            {
                Title = title,
                Width = 320,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };
            var panel = new StackPanel { Margin = new Thickness(12) };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            row.Children.Add(new TextBlock
            {
                Text = "上间距：",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            });
            var topBox = new TextBox
            {
                Text = top.ToString(),
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            row.Children.Add(topBox);
            row.Children.Add(new TextBlock
            {
                Text = "下间距：",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            });
            var bottomBox = new TextBox
            {
                Text = bottom.ToString(),
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(bottomBox);
            panel.Children.Add(row);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var ok = new Button { Content = "确定", Width = 70, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "取消", Width = 70, IsCancel = true };
            SpacingValues result = null;
            ok.Click += (s, e) =>
            {
                int t, b;
                if (!int.TryParse(topBox.Text.Trim(), out t) || !int.TryParse(bottomBox.Text.Trim(), out b))
                {
                    System.Windows.MessageBox.Show("请输入整数间距。", title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (t < 0 || b < 0)
                {
                    System.Windows.MessageBox.Show("间距不能为负数。", title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = new SpacingValues { Top = t, Bottom = b };
                window.DialogResult = true;
            };
            cancel.Click += (s, e) => { window.DialogResult = false; };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            window.Content = panel;
            topBox.SelectAll();
            topBox.Focus();
            bool? dialog = window.ShowDialog();
            return dialog == true ? result : null;
        }
    }

    public class SpacingValues
    {
        public int Top { get; set; }
        public int Bottom { get; set; }
    }
}
