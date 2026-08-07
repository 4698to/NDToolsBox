using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace NDToolsBox
{
    public class ResetMaxCUI
    {
        public static void Reset()
        {
            for (int i = 0; i < 4; i++)
            {
                int max = 2015 + i;
                //高版本Max 没有这个配置文件
                SetCUiFile(max);
            }
        }
        public static void SetCUiFile(int max)
        {
            //拿到用户 的 \AppData\Local
            string appdata = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string CUIUs = $"{appdata}\\Autodesk\\3dsMax\\{max} - 64bit\\ENU\\en-US\\UI\\MaxManaged.cuix";
            string CUIZh = $"{appdata}\\Autodesk\\3dsMax\\{max} - 64bit\\CHS\\zh-CN\\UI\\MaxManaged.cuix";

            RemoveNDBox(CUIUs);
            RemoveNDBox(CUIZh);
        }
        public static void RemoveNDBox(string CUIfile)
        {
            if (File.Exists(CUIfile))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(CUIfile);
                XmlNodeList Windows = xml.SelectNodes("ADSK_CUI/CUIWindows/Window");
                foreach (XmlNode item in Windows)
                {
                    if (item.Attributes["name"].Value.Contains("NDBox-Dock"))
                    {
                        item.ParentNode.RemoveChild(item);
                        break;
                    }
                }
                xml.Save(CUIfile);
                Console.WriteLine($"save -> {CUIfile}");
            }
            else
            {
                //Console.WriteLine($"No existe -> {CUIfile}");
            }
        }
    }
}
