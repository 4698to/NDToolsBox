using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Policy;

namespace NDToolsBox
{
    public class CfgHelpPerson
    {
       
        /// <summary>
        /// 让每个都重新设置下名字
        /// </summary>
        /// <param name="p"></param>
        public static void SetNewName(Person p,string max_root)
        {
            if (!p.IsGrouping)
            {
                p.Path = Path.Combine(max_root ?? "", p.StartupFolder ?? "", p.SubPath ?? "");
            }
                foreach (Person ip in p.Children)
                {
                    SetNewName(ip, max_root);
                }
                p.setName();
            
        }
        public static Person Readjson(string _Path, string max_root)
        {
            //string _Path = @"G:\Git_NDBox\天晴动作组脚本工具v4.47For2015\天晴盒子-脚本清单.json";
            if (File.Exists(_Path))
            {
                string jsontext = System.IO.File.ReadAllText(_Path, new System.Text.UTF8Encoding(false));

                JToken Jview_body = JToken.Parse(jsontext);

                Person body_information = Jview_body.ToObject<Person>();
                body_information.Name = "0";//版本号
                body_information.Path = "";//工具栏上的名字
                
                SetNewName(body_information, max_root);

                return body_information;
            }
            else
            {
                Person body_information = new Person();
                body_information.Name = "0";//版本号
                body_information.Path = "缺失工具清单";//工具栏上的名字
                return body_information;

            }
        }
    }
}
