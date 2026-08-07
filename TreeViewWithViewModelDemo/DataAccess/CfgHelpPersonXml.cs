using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MaxToolbars.Toobars;

namespace NDToolsBox
{
    public class CfgHelpPersonXml
    {
        public static bool TestExtension(string path)
        {
            if (Path.HasExtension(path))
            {
                string ext = Path.GetExtension(path);
                if (ext.Equals(".mse")) { 
                    return true;
                }
                if (ext.Equals(".ms"))
                {
                    return true;
                }
            }
            return false;
        }
        public static string GetAbout(string path)
        {
            string aboutfile = Path.ChangeExtension(path, ".txt");
            if (File.Exists(aboutfile))
            {
                try
                {
                    return (File.ReadAllText(aboutfile, Encoding.UTF8));
                }
                catch {
                    return "";
                }

            }
            return "";
        }
        public static void GetPathFiles(ref Person Proot,string root)
        {
            //string root = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\Resources";

            if (!string.IsNullOrEmpty(root))
            {
                if (Directory.Exists(root))
                {

                    string[] Child_level = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
                    foreach (string i in Child_level)
                    {
                        //ScriptsUtilities.print(i);

                        Person p = new Person();
                        p.Name = Path.GetFileName(i);
                        p.Path = i; ;
                        p.exist = true;
                        string[] rootfile = Directory.GetFiles(i);
                        foreach (string f in rootfile)
                        {
                            if (TestExtension(f))
                            {
                                Person file = new Person(f);
                                file.message = GetAbout(f);
                                file.exist = true;
                                file.setName();
                                p.Children.Add(file);
                            }
                        }
                        p.setName();
                        Proot.Children.Add(p);
                    }
                }
            }
            else {
                ScriptsUtilities.print($"No Exists {root}");
            }
        }
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeToXmlString(object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static T DeserializeFromXmlString<T>(string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// 让每个都重新设置下名字
        /// </summary>
        /// <param name="p"></param>
        public static void SetNewName(Person p, string max_root)
        {
            if (!p.IsGrouping)
            {
                if (Path.HasExtension(p.SubPath))
                {
                    p.ExtensionType = Path.GetExtension(p.SubPath);
                }
                if (p.RootType == null)
                {
                    p.RootType = "MaxRoot";
                }
                if (p.RootType.Equals("MaxRoot"))
                {
                    p.Path = Path.Combine(max_root, p.StartupFolder, p.SubPath);


                }
                if (p.RootType.Equals("ApplicationPlugins"))
                { 
                    p.Path = Path.Combine(WebAddress.apppath, p.StartupFolder, p.SubPath);

                }
                p.exist = File.Exists(p.Path);
            }
            else {
                p.exist = true; //如果是分类，必须是存在的
            }
            foreach (Person ip in p.Children)
            {
                SetNewName(ip, max_root);
            }
            p.setName();

        }
        public static void SaveXml(object obj,string xml_path)
        {
            string xmlString = SerializeToXmlString(obj);
            System.IO.File.WriteAllText(xml_path, xmlString);

        }
        public static toolbarsViewModle ReadToolBarItem(string xml_path)
        {
            if (File.Exists(xml_path))
            {
                string xmltext = System.IO.File.ReadAllText(xml_path, new System.Text.UTF8Encoding(false));
                try
                {
                    toolbarsViewModle tt = DeserializeFromXmlString<toolbarsViewModle>(xmltext);
                    if (tt != null)
                    {
                        return tt;
                    }
                }
                catch {
                    return null;
                }
            }
            return null;
        }
        public static Person ReadToolListXml(string _Path, string max_root)
        {
            //string _Path = @"G:\Git_NDBox\天晴动作组脚本工具v4.47For2015\天晴盒子-脚本清单.xml";
            if (File.Exists(_Path))
            {
                string xmltext = System.IO.File.ReadAllText(_Path, new System.Text.UTF8Encoding(false));
                try
                {
                    Person body_information = DeserializeFromXmlString<Person>(xmltext);

                    //body_information.Name = "0";//版本号
                    //body_information.Path = "工具栏上的名字";//工具栏上的名字
                    //设置显示名字和设置max根路径
                    SetNewName(body_information, max_root);


                    return body_information;
                }
                catch {

                    Person body_information = new Person();
                    body_information.Name = "0";//版本号
                    body_information.Path = "缺失工具清单";//工具栏上的名字
                    
                    body_information.message = $"工具配置错误 -> 无法读取： {_Path} ";
                    ScriptsUtilities.print(body_information.message);

                    return body_information;

                };
            }
            else
            { 
                
                Person body_information = new Person();
                body_information.Name = "0";//版本号
                body_information.Path = "缺失工具清单";//工具栏上的名字
                
                body_information.message = $"工具配置错误 -> 不存在： {_Path} ";
                ScriptsUtilities.print(body_information.message);
                
                return body_information;
               
            }
        }
    }
}

