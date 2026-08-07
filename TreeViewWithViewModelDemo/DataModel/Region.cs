using System.Collections.Generic;

namespace NDToolsBox
{
    public class Region
    {
        public Region(string regionName)
        {
            this.RegionName = regionName;
        }

        public string RegionName { get; private set; }

        readonly List<State> _states = new List<State>();
        public List<State> States
        {
            get { return _states; }
        }
    }
}