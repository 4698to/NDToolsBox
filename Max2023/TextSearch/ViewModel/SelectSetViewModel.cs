using Autodesk.Max.Plugins;
using System;
using System.Windows.Input;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Diagnostics;

namespace NDToolsBox
{
    public class SetNameList
    {
        public List<SetNameObject> NamedSelSet;

    }
    public class Node
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }
        
        [XmlAttribute("GUID")]
        public string GUID { get; set; }
        [XmlIgnore]
        public INode node { get; set; }
        public Node()
        { 
        }
        public Node(string n)
        {
            this.Name = n;
        }
        public Node(INode n)
        {
            this.Name = n.Name;
            this.node = n;
            
        }
    }
    [XmlRoot("SelSetItem")]
    public class SetNameObject
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Count")]
        public int Count { get { return Nodes.Count; } }

        [XmlArrayItem(Type = typeof(Node))]
        public List<Node> Nodes { get; set; }

        public SetNameObject()
        {
            this.Nodes = new List<Node>();
        }
        public SetNameObject(string name )
        {
            Name = name;
            this.Nodes = new List<Node>();
        }
    }
    public class NameSet 
    {
        public string name;
        public int index;
        public string color_;
        public NameSet(string n , int i) {
            name = n;
            index = i;
        }
        public NameSet(string n, int i,string c)
        {
            name = n;
            index = i;
            color_ = c;
        }
    }
    public class SelectSetItem: INotifyPropertyChanged
    { 
        private string name_;
        private int index_;
        private string color_;
        private bool _isedit = false;
        
        public SelectSetItem()
        { 

        }
        public SelectSetItem(string name)
        {
            name_ = name;

        }
        public SelectSetItem(string name,int index)
        {
            name_ = name ;
            index_ = index;

        }
        public SelectSetItem(string name, int index,string color)
        {
            name_ = name;
            index_ = index;
            color_ = color;

        }
        public bool IsEdit
        {
            get { return _isedit; }

            set
            {
                _isedit = value;
                this.OnPropertyChanged("IsEdit");
            }
        }
        
        public string GetMaxUiBackgroundColor
        {
            get { return ScriptsUtilities.GetMaxBackgroundColor_Str(); }
        }
        public string GetMaxTextColor
        {
            get { return "#00020f"; }
            //get { return ScriptsUtilities.mGetColor(0); }
        }
        public string Name { 
            get { return name_; }
            set {  name_ = value; this.OnPropertyChanged("Name"); }
        }
        public int Index {
            get { return index_; }
            set {
                index_ = value;
                this.OnPropertyChanged("Index");
            }
        
        }
        public string Color
        {
            get { return color_; }
            set {  color_ = value; }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("tree view model PropertyChanged is null");
        }

    }
    //删除选择集
    public class Edit_RemoveCommand : ICommand
    {
        public Edit_RemoveCommand()
        {

        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    ScriptsUtilities.print(sel_index.ToString());
                }
            }
            if (parameter.GetType() == typeof(string))
            {
                ScriptsUtilities.RemoveSeleSet(parameter.ToString());
            }
        }
    }
    //从选择集中移除选中物体
    public class Edit_RemoveNodeCommand : ICommand
    {
        public Edit_RemoveNodeCommand()
        {

        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    //ScriptsUtilities.print(sel_index.ToString());
                    ScriptsUtilities.AddNodeToSeleSet(sel_index,false);
                }
            }
            if (parameter.GetType() == typeof(string))
            {
                ScriptsUtilities.print(parameter.ToString());
            }
        }
    }
    //创建新选择集
    public class Edit_AddCommand : ICommand
    {
        public Edit_AddCommand()
        {
           
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    //ScriptsUtilities.print(sel_index.ToString());
                    //ScriptsUtilities.AddNodeToSeleSet(sel_index,true);
                    
                }
            }
            if (parameter.GetType() == typeof(string))
            { 
                ScriptsUtilities.NewSeleSet();
                //ScriptsUtilities.print(parameter.ToString());
            }
        }
    }

    //给选择集里添加选中物体
    public class Edit_AddNodeCommand : ICommand
    {
        public Edit_AddNodeCommand()
        {

        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    //ScriptsUtilities.print(sel_index.ToString());
                    ScriptsUtilities.AddNodeToSeleSet(sel_index,true);

                }
            }
            if (parameter.GetType() == typeof(string))
            {
                //ScriptsUtilities.print(parameter.ToString());
            }
        }
    }
    //导入选择集
    public class Edit_ImportCommand : ICommand
    {
        public Edit_ImportCommand()
        {

        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    //ScriptsUtilities.print(sel_index.ToString());

                }
            }
            if (parameter.GetType() == typeof(string))
            {
                //ScriptsUtilities.print(parameter.ToString());
                ScriptsUtilities.ImportSeleSet();

            }
        }
    }


    //导出选择集
    public class Edit_ExportCommand : ICommand
    {
        public Edit_ExportCommand()
        {

        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter.GetType() == typeof(int))
            {
                int sel_index = (int)parameter;
                if (sel_index >= 0)
                {
                    //ScriptsUtilities.print(sel_index.ToString());

                }
            }
            if (parameter.GetType() == typeof(string))
            {
                //ScriptsUtilities.print(parameter.ToString());
                ScriptsUtilities.ExportSeleSet();
            }
        }
    }
    internal class SelectSetViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SelectSetItem> _items;

        private ICommand edit_AddNodeCommand;
        private ICommand edit_AddCommand;

        private ICommand edit_RemoveNodeCommand;
        private ICommand edit_RemoveCommand;
        private ICommand edit_Import;
        private ICommand edit_Export;

        public SelectSetViewModel()
        {
            _items = new ObservableCollection<SelectSetItem>();
        }

        public string GetMaxUiBackgroundColor
        {
            get { return ScriptsUtilities.GetMaxBackgroundColor_Str(); }
        }
        public ICommand ImportCommand
        {
            get
            {
                if (edit_Import == null)
                {
                    edit_Import = new Edit_ImportCommand();
                }
                return edit_Import;
            }
        }
        public ICommand ExportCommand
        {
            get
            {
                if (edit_Export == null)
                {
                    edit_Export = new Edit_ExportCommand();
                }
                return edit_Export;
            }
        }
        public ICommand AddCommand
        {
            get {
                if (edit_AddCommand == null)
                {
                    edit_AddCommand = new Edit_AddCommand();
                }
                return edit_AddCommand; 
            }
        }
        public ICommand RemoveCommand
        {
            get
            {
                if (edit_RemoveCommand == null)
                {
                    edit_RemoveCommand = new Edit_RemoveCommand();
                }
                return edit_RemoveCommand;
            }
        }
        public ICommand RemoveNodeCommand
        {
            get
            {
                if (edit_RemoveNodeCommand == null)
                {
                    edit_RemoveNodeCommand = new Edit_RemoveNodeCommand();
                }
                return edit_RemoveNodeCommand;
            }
        }
        public ICommand AddNodeCommand
        {
            get {
                if (edit_AddNodeCommand == null)
                {
                    edit_AddNodeCommand = new Edit_AddNodeCommand();
                }
                return edit_AddNodeCommand; }
        }

        public string GetMaxTextColor
        {
            get { return ScriptsUtilities.mGetColor(0); }
        }
        public string GetButtonColor_A
        {
            get { return ScriptsUtilities.color_lib[1]; }

            //get { return ScriptsUtilities.mGetColor(1); }
        }
        public string GetButtonColor_B
        {
            get { return ScriptsUtilities.mGetColor(2); }
        }
        public string GetButtonColor_C
        {
            get { return ScriptsUtilities.mGetColor(3); }
        }
        public string GetButtonColor_D
        {
            get { return ScriptsUtilities.mGetColor(4); }
        }
        public string GetThemeColor
        {
            get { return ScriptsUtilities.mGetColor(5); }

        }
        public void FindColor(ref NameSet item)
        {
            foreach (SelectSetItem i in _items)
            {
                if (item.name == i.Name)
                {
                    item.color_ = i.Color;
                    return;
                }
            }
        }
        
        public ObservableCollection<SelectSetItem> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                this.OnPropertyChanged("Items");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("tree view model PropertyChanged is null");
        }

    }
}
