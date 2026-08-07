using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GetPathFiles
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string root = @"D:\Program Files\Autodesk\3ds Max 2015\scripts\NDTools";

            string[] dir = Directory.GetDirectories(root,"*",SearchOption.AllDirectories);
            foreach (string i in dir)
            {
                string name = Path.GetFileName(i);
                Console.WriteLine(name);
            }
            string[] rootfile = Directory.GetFiles(root);
            
            foreach (string i in rootfile)
            {
                Console.WriteLine(i);
            }

        }
    }
}
