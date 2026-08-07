using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace NDToolsBox
{
    public class CopyItemCommand : ICommand
    {
        public readonly FamilyTreeViewModel _familyTree;

        public CopyItemCommand(FamilyTreeViewModel tool)
        {
            this._familyTree = tool;
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
            if (parameter.GetType() == typeof(PersonViewModel))
            {
                PersonViewModel item = parameter as PersonViewModel;
                if (!string.IsNullOrEmpty(item.Path))
                {
                    ScriptsUtilities.print(item.Path);
                    System.Windows.Forms.Clipboard.SetText(item.Path, System.Windows.Forms.TextDataFormat.UnicodeText);
                }
               
            }
        }
    }
    /// <summary>
    /// This is the view-model of the UI.  It provides a data source
    /// for the TreeView (the FirstGeneration property), a bindable
    /// SearchText property, and the SearchCommand to perform a search.
    /// </summary>
    public class FamilyTreeViewModel: INotifyPropertyChanged
    {
        #region Data
        private const string SearchResults = "搜索结果";

        //readonly ReadOnlyCollection<PersonViewModel> _firstGeneration;
        public ObservableCollection<PersonViewModel> _firstGeneration;
        //private PersonViewModel matchesPersonRoot;
        readonly PersonViewModel _rootPerson;
        readonly ICommand _searchCommand;
        readonly ICommand _openitemHelp;

        IEnumerator<PersonViewModel> _matchingPeopleEnumerator;
        string _searchText = String.Empty;
        private string _message ;

        private string _toolsName;
        private string _toolsVersion;
        public float remoteVersion=0;
        private bool _isupdata;
        private bool GoodMatches;
        #endregion // Data

        #region Constructor

        private CopyItemCommand _copyItemCommand;

        public FamilyTreeViewModel(Person rootPerson)
        {

            _rootPerson = new PersonViewModel(rootPerson);

            //_firstGeneration = new ReadOnlyCollection<PersonViewModel>(
            //   new PersonViewModel[] 
            //  { 
            //    _rootPerson 
            //});
            

            _firstGeneration = new ObservableCollection<PersonViewModel>
            (
                (from p in rootPerson.Children select new PersonViewModel(p)).ToList()
            );

            _searchCommand = new SearchFamilyTreeCommand(this);
            _openitemHelp = new OpenItemHelpCommand(this);
            _copyItemCommand = new CopyItemCommand(this);

            message = rootPerson.message;

        }
        public async Task downloadVersion()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = null;
            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(3);
                    HttpResponseMessage response = await client.GetAsync(string.Concat(WebAddress.serverName1,WebAddress.serverVersion));

                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    float.TryParse(responseBody, out remoteVersion);
                }
            }
            catch { }
        }
        
        public string GetMaxUiBackgroundColor
        {
            get { return ScriptsUtilities.GetMaxBackgroundColor_Str(); }

        }
        public string GetMaxTextColor
        {
            get { return ScriptsUtilities.mGetColor(0); }
            
        }
        public string GetThemeColor
        {
            get { return ScriptsUtilities.mGetColor(5); }
            
        }
        public string GetTreeBackGroundColor
        {
            get { return ScriptsUtilities.mGetColor(6); }
        }
        public string GetBorderColor
        { 
            get { return ScriptsUtilities.mGetColor(7); }

        }
        public string GetMouseItmeColor
        {
            get { return ScriptsUtilities.mGetColor(8); }
            

        }
        public string GetSecondaryTextColor
        {
            get { return ScriptsUtilities.mGetColor(9); }

        }
        public CopyItemCommand GCopyItemCommand
        {
            get { return _copyItemCommand; }

        }
        
        public bool IsUpdata
        {
            get { return _isupdata; }
            set
            {
                _isupdata = value;
                this.OnPropertyChanged("IsUpdata");
            }
        }
        public string toolsName
        {
            get { return _toolsName; }
            set
            {
                _toolsName = value;
                this.OnPropertyChanged("toolsName");
            }
        }
        public string toolsVersion
        {
            get { return _toolsVersion; }
            set
            {
                _toolsVersion = value;
                this.OnPropertyChanged("toolsVersion");
            }
        }
        public string message {
            get { return _message; }
            set { _message = value;
                this.OnPropertyChanged("message");
            }
        }

        #endregion // Constructor

        #region Properties

        #region FirstGeneration

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<PersonViewModel> FirstGeneration
        {
            get { return _firstGeneration; }
            set {
                _firstGeneration = value;
                Console.WriteLine("set FirstGeneration Changed");
                this.OnPropertyChanged("FirstGeneration");
            }
        }

        #endregion // FirstGeneration


        public ICommand OpenItemHelp
        {
            get { return _openitemHelp; }
        }
        private class OpenItemHelpCommand : ICommand
        {
            readonly FamilyTreeViewModel _familyTree;
            public OpenItemHelpCommand(FamilyTreeViewModel familyTree)
            {
                _familyTree = familyTree;
            }
            public bool CanExecute(object parameter)
            {
                return true;
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because
                // this command never raises the event, and
                // not using the WeakEvent pattern here can
                // cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _familyTree.OpenHelpWeb();
            }
        }
        void OpenHelpWeb()
        {

        }
        #region SearchCommand

        /// <summary>
        /// Returns the command used to execute a search in the family tree.
        /// </summary>
        public ICommand SearchCommand
        {
            get { return _searchCommand; }
        }

        private class SearchFamilyTreeCommand : ICommand
        {
            readonly FamilyTreeViewModel _familyTree;

            public SearchFamilyTreeCommand(FamilyTreeViewModel familyTree)
            {
                _familyTree = familyTree;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because
                // this command never raises the event, and
                // not using the WeakEvent pattern here can
                // cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _familyTree.PerformSearch();
            }
        }

        #endregion // SearchCommand

        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value == _searchText)
                    return;

                _searchText = value;

                _matchingPeopleEnumerator = null;
                this.OnPropertyChanged("SearchText");
            }
        }

        #endregion // SearchText

        #endregion // Properties

        #region Search Logic

        void PerformSearch()
        {
            if (_searchText.Length < 1)
            {
                RemoveSearchMatches();
                return;
            }

            if (_matchingPeopleEnumerator == null || !_matchingPeopleEnumerator.MoveNext())
                this.VerifyMatchingPeopleEnumerator();

            //var person = _matchingPeopleEnumerator.Current;
            var person = _firstGeneration[0];

            if (person == null)
            {
                return;
            }
            // Ensure that this person is in view.
            //父节点展开
            if (person.Parent != null)
            {
                //person.Parent.IsExpanded = true;
            }
            //自己展开
            if (person.IsGrouping)
            {
                //person.IsExpanded = true;
            }

            
           // person.IsExpanded = true;
            //person.IsSelected = true;

        }
        public void RemoveSearchMatches()
        {
            if (GoodMatches)
            {
                if (_firstGeneration[0].NameContainsText(SearchResults))
                    _firstGeneration.RemoveAt(0);
                GoodMatches = false;
            }
        }
        void VerifyMatchingPeopleEnumerator()
        {
            PersonViewModel searchNode;
            
            var matches = this.FindMatches(_searchText, _rootPerson);
            //Console.WriteLine("VerifyMatchingPeopleEnumerator " + _searchText);
            _matchingPeopleEnumerator = matches.GetEnumerator();

            List<PersonViewModel> matchesPerson = matches.ToList();
            
            //如果还有上次的搜索结果
            if (GoodMatches)
            {
                searchNode = _firstGeneration[0];
            }
            else { 
                //搜索结果
                Person mr = new Person();
                mr.Name = SearchResults;
                mr.IsGrouping = true;
                
                mr.HelpUrl = "";
                mr.Path = "";
                searchNode = new PersonViewModel(mr);
                _firstGeneration.Insert(0, searchNode);
            }

            searchNode.Name = $"{SearchResults} ({matchesPerson.Count})";
            searchNode.Children = new ObservableCollection<PersonViewModel>(matchesPerson);
            GoodMatches = true;

            if (!_matchingPeopleEnumerator.MoveNext())
            {
                System.Windows.MessageBox.Show(
                    "No matching names were found.",
                    "Try Again",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
            }
        }

        IEnumerable<PersonViewModel> FindMatches(string searchText, PersonViewModel person)
        {
            if (person.NameContainsText(searchText) || person.MessageContainsText(searchText))
            {
                if (!person.IsGrouping)
                {
                    yield return person;
                }
            }

            foreach (PersonViewModel child in person.Children)
            {
                foreach (PersonViewModel match in this.FindMatches(searchText, child))
                {
                    yield return match;
                }
            }
        }

        #endregion // Search Logic
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                Console.WriteLine("tree view model PropertyChanged is null");
            }
        }
    }
}