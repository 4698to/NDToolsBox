using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace NDToolsBox
{
    /// <summary>
    /// A simple data transfer object (DTO) that contains raw data about a person.
    /// </summary>
    /// 
    [Serializable]
    public class Person
    {
        /*readonly List<Person> _children = new List<Person>();
        public IList<Person> Children
        {
            get { return _children; }
        }*/
        public List<Person> Children;
        public int GetCount()
        {
            if (IsGrouping)
            {
                int icount = Children.Count;
                foreach (var i in Children)
                {
                    if (i.IsGrouping) { icount -= 1; }
                    icount += i.GetCount();
                }
                return icount;
            }
            else
            {
                return 0;
            }

        }
        public string Name ;

        public string DisplayName;
        public void setName()
        {
            if (IsGrouping)
            {
                DisplayName = string.Concat(Name, " (", GetCount(), ")");
            }
            else {
                DisplayName = Name;
            }
        } //DisplayName
        public string StartupFolder;
        public string SubPath;

        public string Path;
        
        public bool exist;

        public string RootType;
        public string ExtensionType;
        public bool IsGrouping;
        public string message;
        [XmlIgnore]
        public bool hasHelp
        {
            get {
                if (string.IsNullOrEmpty(HelpUrl)) { return false; } else { return true; }
            }
        }
        public string HelpUrl;

        public Person()
        {
            IsGrouping = true;
           
            Children = new List<Person>();
        }
        public Person(string path)
        { 
            this.Path = path;
            this.Name = System.IO.Path.GetFileName(path);
            this.ExtensionType = System.IO.Path.GetExtension(path);
            this.IsGrouping = false;
            this.Children = new List<Person>();
        }
    }
}