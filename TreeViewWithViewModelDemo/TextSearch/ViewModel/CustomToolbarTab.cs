using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace NDToolsBox
{
    /// <summary>
    /// 用户自定义工具条 Tab：每个 Tab 一份列表配置（对应一个 MyListBox / NDListBox xml）。
    /// </summary>
    public class CustomToolbarTab
    {
        public string Id { get; set; }
        public string Header { get; set; }

        /// <summary>
        /// 本 Tab 唯一列表配置名（保存为 {ListId}.xml）。
        /// </summary>
        public string ListId { get; set; }

        /// <summary>
        /// 旧版三列 ListIds，仅用于反序列化兼容；保存时不再写出。
        /// </summary>
        [XmlArray("ListIds")]
        [XmlArrayItem("ListId")]
        public List<string> ListIds { get; set; }

        public CustomToolbarTab()
        {
        }

        public static CustomToolbarTab CreateNew(string header)
        {
            string id = Guid.NewGuid().ToString("N");
            return new CustomToolbarTab
            {
                Id = id,
                Header = header,
                ListId = "CustomTab_" + id
            };
        }

        /// <summary>
        /// 补齐 ListId；旧配置若只有 ListIds，取第一列。
        /// </summary>
        public void Normalize()
        {
            if (string.IsNullOrEmpty(ListId))
            {
                if (ListIds != null)
                {
                    for (int i = 0; i < ListIds.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(ListIds[i]))
                        {
                            ListId = ListIds[i];
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(ListId) && !string.IsNullOrEmpty(Id))
                {
                    ListId = "CustomTab_" + Id;
                }
            }
        }

        public bool ShouldSerializeListIds()
        {
            return false;
        }

        /// <summary>
        /// 删除本 Tab 时应收掉的 xml 名（含旧三列遗留文件）。
        /// </summary>
        public IEnumerable<string> ConfigFileNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(ListId))
            {
                names.Add(ListId);
            }
            if (ListIds != null)
            {
                foreach (string id in ListIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        names.Add(id);
                    }
                }
            }
            return names;
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
        /// 编辑按钮显示名 / Commit / ToolTip。取消返回 null；确定时 Name 不能为空。
        /// </summary>
        public static ButtonEditValues PromptButtonEdit(string title, string name, string commit, string toolTip)
        {
            var window = new Window
            {
                Title = title,
                Width = 480,
                Height = 360,
                MinWidth = 360,
                MinHeight = 280,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResizeWithGrip,
                ShowInTaskbar = false
            };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock
            {
                Text = "显示名称：",
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(nameLabel, 0);
            root.Children.Add(nameLabel);

            var nameBox = new TextBox
            {
                Text = name ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(nameBox, 1);
            root.Children.Add(nameBox);

            var commitLabel = new TextBlock
            {
                Text = "Commit（MaxScript）：",
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(commitLabel, 2);
            root.Children.Add(commitLabel);

            var commitBox = new TextBox
            {
                Text = commit ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10),
                MinHeight = 80
            };
            Grid.SetRow(commitBox, 3);
            root.Children.Add(commitBox);

            var tipLabel = new TextBlock
            {
                Text = "ToolTip：",
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(tipLabel, 4);
            root.Children.Add(tipLabel);

            var tipBox = new TextBox
            {
                Text = toolTip ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(tipBox, 5);
            root.Children.Add(tipBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var ok = new Button { Content = "确定", Width = 70, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "取消", Width = 70, IsCancel = true };
            ButtonEditValues result = null;
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    System.Windows.MessageBox.Show("显示名称不能为空。", title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = new ButtonEditValues
                {
                    Name = nameBox.Text.Trim(),
                    Commit = commitBox.Text ?? string.Empty,
                    ToolTip = tipBox.Text ?? string.Empty
                };
                window.DialogResult = true;
            };
            cancel.Click += (s, e) => { window.DialogResult = false; };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(buttons, 6);
            root.Children.Add(buttons);

            window.Content = root;
            nameBox.SelectAll();
            nameBox.Focus();
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

    public class ButtonEditValues
    {
        public string Name { get; set; }
        public string Commit { get; set; }
        public string ToolTip { get; set; }
    }
}
