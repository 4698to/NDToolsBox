using System.Collections.Generic;

namespace NDToolsBox
{
    public class State
    {
        public State(string stateName)
        {
            this.StateName = stateName;
        }

        readonly List<City> _cities = new List<City>();
        public List<City> Cities
        {
            get { return _cities; }
        } 

        public string StateName { get; private set; }
    }
}