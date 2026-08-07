using System.Collections.Generic;

namespace BusinessLib
{
    /// <summary>
    /// A simple data transfer object (DTO) that contains raw data about a person.
    /// </summary>
    public class Person
    {
        readonly List<Person> _children = new List<Person>();
        public IList<Person> Children
        {
            get { return _children; }
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public string RootType { get; set; }
        public string ext { get; set; }
        public bool IsGrouping { get; set; }
        public string message { get; set; }
        public bool hasHelp
        {
            get; set;
        }
        public string HelpUrl { get; set; }
    }
}