using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NDToolsBox
{
    /// <summary>
    /// 自定义 Tab 内容：一份 xml 中 Items 的每一项渲染为 Expander + ListBox（NDListBox）。
    /// </summary>
    public partial class NDCustomTabLists : UserControl
    {
        private CustomTabListsViewModle _tabLists;

        public NDCustomTabLists()
        {
            InitializeComponent();
        }

        public CustomTabListsViewModle TabLists
        {
            get { return _tabLists; }
        }

        public void InitWithSaveName(string listId)
        {
            string path = string.IsNullOrEmpty(listId)
                ? null
                : Path.Combine(WebAddress.apppath, listId + ".xml");
            if (_tabLists != null)
            {
                _tabLists.Reloaded -= RebuildSections;
            }
            _tabLists = CustomTabListsViewModle.LoadOrCreate(path);
            _tabLists.Reloaded += RebuildSections;
            DataContext = _tabLists;
            RebuildSections();
        }

        /// <summary>
        /// 用已有 ViewModel 初始化（导入后刷新）。
        /// </summary>
        public void InitWithViewModel(CustomTabListsViewModle model)
        {
            if (model == null)
            {
                return;
            }
            if (_tabLists != null)
            {
                _tabLists.Reloaded -= RebuildSections;
            }
            _tabLists = model;
            _tabLists.Reloaded += RebuildSections;
            _tabLists.AttachOwners();
            DataContext = _tabLists;
            RebuildSections();
        }

        private void RebuildSections()
        {
            RootPanel.Children.Clear();
            if (_tabLists == null || _tabLists.Items == null)
            {
                return;
            }
            for (int i = 0; i < _tabLists.Items.Count; i++)
            {
                NDListBoxViewModle section = _tabLists.Items[i];
                if (section == null)
                {
                    continue;
                }
                section.OwnerTab = _tabLists;
                var listBox = new NDListBox();
                if (i > 0)
                {
                    listBox.Margin = new Thickness(0, 5, 0, 0);
                }
                listBox.InitWithViewModel(section);
                RootPanel.Children.Add(listBox);
            }
        }

        private void Root_DragEnter(object sender, DragEventArgs e)
        {
            SetDropEffects(e);
        }

        private void Root_DragOver(object sender, DragEventArgs e)
        {
            SetDropEffects(e);
        }

        private static void SetDropEffects(DragEventArgs e)
        {
            if (e == null)
            {
                return;
            }
            if (e.Data != null &&
                (e.Data.GetDataPresent(DataFormats.UnicodeText) || e.Data.GetDataPresent(DataFormats.FileDrop)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Root_Drop(object sender, DragEventArgs e)
        {
            AcceptDrop(e);
        }

        /// <summary>
        /// Tab 空白区 / 父级转发的拖放入口。
        /// </summary>
        public void AcceptDrop(DragEventArgs e)
        {
            NDListBox target = FindDropTargetListBox(e);
            if (target == null)
            {
                if (e != null)
                {
                    e.Handled = true;
                }
                return;
            }
            bool onListBox = IsDescendantOf(e.OriginalSource as DependencyObject, target);
            target.AcceptDrop(e, onListBox ? (int?)null : -1);
        }

        private NDListBox FindDropTargetListBox(DragEventArgs e)
        {
            var src = e.OriginalSource as DependencyObject;
            NDListBox hit = FindAncestor<NDListBox>(src);
            if (hit != null)
            {
                return hit;
            }
            if (RootPanel.Children.Count == 0)
            {
                return null;
            }
            return RootPanel.Children[RootPanel.Children.Count - 1] as NDListBox;
        }

        private static bool IsDescendantOf(DependencyObject child, DependencyObject ancestor)
        {
            DependencyObject current = child;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
