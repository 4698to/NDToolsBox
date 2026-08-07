using Autodesk.Max.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NDToolsBox
{
    public class MyCallbackRangeChange: TimeChangeCallback
    {
        public MyCallbackRangeChange() { }

        public override void TimeChanged(int t)
        {
            //ScriptsUtilities.print("TimeChanged");
            //ScriptsUtilities.ip4.AnimRange.Start.ToString()
            //throw new NotImplementedException();

        }
    }
}
