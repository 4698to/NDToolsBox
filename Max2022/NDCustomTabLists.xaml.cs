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
    }
}
