using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NDToolsBox
{
    /// <summary>
    /// A UI-friendly wrapper around a Person object.
    /// </summary>
    public class PersonViewModel : INotifyPropertyChanged
    {
        #region Data

        //readonly ReadOnlyCollection<PersonViewModel> _children;
        public ObservableCollection<PersonViewModel> _children;

        readonly PersonViewModel _parent;
        public Person _person;

        bool _isExpanded;
        bool _isSelected;
        string _isCommandState;
        #endregion // Data

        #region Constructors

        public PersonViewModel(Person person) : this(person, null)
        {
            if (person != null)
            {
                person.setName();
            }
        }
        private void Getchildren()
        {
            _children = new ObservableCollection<PersonViewModel>(
                    (from child in _person.Children
                     select new PersonViewModel(child, this) )
                     .ToList<PersonViewModel>());
        }
        private PersonViewModel(Person person, PersonViewModel parent)
        {
            _person = person;
            _parent = parent;

            _children = new ObservableCollection<PersonViewModel>(
                    (from child in _person.Children
                     select new PersonViewModel(child, this) )
                     .ToList<PersonViewModel>());
        }

        #endregion // Constructors

        #region Person Properties

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
        public ObservableCollection<PersonViewModel> Children
        {
            get {
                //Getchildren();
                return _children; }
            set { _children = value;
                this.OnPropertyChanged("Children");
            }

        }
        public string CommandState
        {
            get { return _isCommandState; }
            set {
                _isCommandState = value;
                this.OnPropertyChanged("CommandState");
            }
        }
        public string ExtensionType
        {
            get { return _person.ExtensionType; }
            set { _person.ExtensionType = value;
                this.OnPropertyChanged("ext");
            }
        }
        public string Name
        {
            get {
                //if (IsGrouping) { return _person.DisplayName; } else { return _parent.Name; }
                return _person.DisplayName;
            }
            set { _person.DisplayName = value;
                this.OnPropertyChanged("Name");
            }
        }

        public bool IsGrouping
        {
            get { return _person.IsGrouping;}
            set { 
                _person.IsGrouping = value;
                this.OnPropertyChanged("IsGrouping");
            }
        }
        public string message
        {
            get { return _person.message;}
            set { 
                _person.message = value;
                this.OnPropertyChanged("message");
            }
        }
        public string HelpUrl
        {
            get { return _person.HelpUrl;}
            set {
                _person.HelpUrl = value;
                this.OnPropertyChanged("HelpUrl");
            }
        }
        public bool hasHelp
        {
            get { return _person.hasHelp; }
        }
        public string Path
        {
            get { return _person.Path; }
        }
        public bool Exist
        {
            get { return _person.exist; }
            set {
                _person.exist = value;
                this.OnPropertyChanged("Exist");
            }
        }
        public string StartupFolder
        {
            get { return _parent.StartupFolder; }
        }
        public string SubPath
        {
            get { return _parent.SubPath; }
        }
        #endregion // Person Properties

        #region Presentation Members

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;
            }
        }

        #endregion // IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }
        public bool MessageContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.message))
                return false;

            return this.message.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }
        #endregion // NameContainsText

        #region Parent

        public PersonViewModel Parent
        {
            get { return _parent; }
        }

        #endregion // Parent

        #endregion // Presentation Members        

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            //Console.WriteLine(propertyName);
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine($"{propertyName} viewModel PropertyChanged null ");
        }

        #endregion // INotifyPropertyChanged Members
    }
}