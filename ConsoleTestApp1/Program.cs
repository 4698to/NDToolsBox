using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDToolsBox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string a = "C:\\ProgramData\\Autodesk\\ApplicationPlugins\\NDToolsBox\\ToolLists\\NDToolsList.xml";
            string b = "C:\\ProgramData\\Autodesk\\ApplicationPlugins\\NDToolsBox";
            Person rootPerson = CfgHelpPersonXml.ReadToolListXml(a, b);
            Console.WriteLine(rootPerson.Name);
        }
    }
}
