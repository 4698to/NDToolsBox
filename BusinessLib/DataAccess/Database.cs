using System;
using System.IO;
using System.Xml;

namespace BusinessLib
{
    /// <summary>
    /// A data source that provides raw data objects.  In a real
    /// application this class would make calls to a database.
    /// </summary>
    public static class Database
    {
        //3dsMax的安装路径
        private static string rootpath;

        #region GetRegions

        public static Region[] GetRegions()
        {
            return new Region[]
            {
                new Region("Northeast"),
                new Region("Midwest")
            };
        }

        #endregion // GetRegions

        #region GetStates

        public static State[] GetStates(Region region)
        {
            switch (region.RegionName)
            {
                case "Northeast":
                    return new State[]
                    {
                        new State("Connecticut"),
                        new State("New York")
                    };

                case "Midwest":
                    return new State[]
                    {
                        new State("Indiana")
                    };
            }

            return null;
        }

        #endregion // GetStates

        #region GetCities

        public static City[] GetCities(State state)
        {
            switch (state.StateName)
            {
                case "Connecticut":
                    return new City[]
                    {
                        new City("Bridgeport"),
                        new City("Hartford"),
                        new City("New Haven")
                    };

                case "New York":
                    return new City[]
                    {
                        new City("Buffalo"),
                        new City("New York"),
                        new City("Syracuse")
                    };

                case "Indiana":
                    return new City[]
                    {
                        new City("Evansville"),
                        new City("Fort Wayne"),
                        new City("Indianapolis"),
                        new City("South Bend")
                    };
            }

            return null;
        }

        #endregion // GetCities

        #region GetFamilyTree

        public static Person GetFamilyTree()
        {
            // In a real app this method would access a database.
            return new Person
            {
                Name = "David Weatherbeam",
                IsGrouping = true,
                message = "In a real app this method would access a database.",
                Children =
                {
                    new Person
                    {
                        Name="Alberto Weatherbeam",
                        IsGrouping = true,
                        message = "Alberto Weatherbeam.",

                        Children=
                        {
                            new Person
                            {
                                Name="Zena Hairmonger",
                                IsGrouping = true,

                                Children=
                                {
                                    new Person
                                    {
                                        Name="Sarah Applifunk",
                                        message = "Sarah Applifunk",
                                        HelpUrl = ""

                                    }
                                }
                            },
                            new Person
                            {
                                Name="Jenny van Machoqueen",
                                IsGrouping = true,

                                Children=
                                {
                                    new Person
                                    {
                                        Name="Nick van Machoqueen",
                                        HelpUrl = "www.baidu.com"

                                    },
                                    new Person
                                    {
                                        Name="Matilda Porcupinicus",
                                        HelpUrl = "www.baidu.com"

                                    },
                                    new Person
                                    {
                                        Name="Bronco van Machoqueen",
                                        HelpUrl = "www.baidu.com"

                                    }
                                }
                            }
                        }
                    },
                    new Person
                    {
                        Name="Komrade Winkleford",
                        IsGrouping = true,

                        Children=
                        {
                            new Person
                            {
                                Name="Maurice Winkleford",
                                IsGrouping = true,

                                Children=
                                {
                                    new Person
                                    {
                                        Name="Divinity W. Llamafoot",
                                        HelpUrl = "www.baidu.com"

                                    }
                                }
                            },
                            new Person
                            {
                                Name="Komrade Winkleford, Jr.",
                                IsGrouping = true,

                                Children=
                                {
                                    new Person
                                    {
                                        Name="Saratoga Z. Crankentoe",
                                        HelpUrl = "www.baidu.com"

                                    },
                                    new Person
                                    {
                                        Name="Excaliber Winkleford",
                                        HelpUrl = "www.baidu.com"

                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion // GetFamilyTree

        #region GetRootPathTree 
        public static Person GetRootPathTree(string root, string xmlfile)
        {
            //string[] files = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
            rootpath = root;

            XmlDocument xml = new XmlDocument();
            if (File.Exists(xmlfile))
            {
                xml.Load(xmlfile);
            }

            //获取根据节点
            XmlNode rootnode = xml.SelectSingleNode("checkResult");
            //tree 根节点
            Person rootParent = NewRootPerson();
            if (rootnode == null)
            {
                return rootParent;
            }

            if (rootnode.Attributes != null)
            {
                XmlAttribute ver = rootnode.Attributes["Version"];
                XmlAttribute name = rootnode.Attributes["Name"];
                if (ver != null) rootParent.Name = ver.Value;
                if (name != null) rootParent.Path = name.Value;
            }

            //遍历根节点的分类节点
            for (int i = 0 ; i < rootnode.ChildNodes.Count ; i++)
            {
                XmlNode item = rootnode.ChildNodes[i];
                //分类节点
                GetClassNodeItemChilders(item, ref rootParent);
            }

            return rootParent;
        }
        public static Person NewRootPerson()
        {
            Person Node = new Person();
            Node.IsGrouping = true;
            return Node;
        }
        public static void SetPath(ref Person per)
        {
            if (string.IsNullOrEmpty(per.RootType) || string.IsNullOrEmpty(per.Path))
            {
                return;
            }
            string relative = per.Path.TrimStart('\\');
            if (per.RootType.Equals("MaxRoot"))
            {
                per.Path = Path.Combine(rootpath ?? "", relative);
            }
            else if (per.RootType.Equals("ApplicationPlugins"))
            {
                per.Path = Path.Combine(WebAddress.apppath, relative);
            }
        }
        private static string SafeInnerText(XmlNode parent, string childName)
        {
            if (parent == null) return "";
            XmlNode n = parent.SelectSingleNode(childName);
            return n != null ? (n.InnerText ?? "") : "";
        }
        private static string SafeAttr(XmlNode item, string attrName)
        {
            if (item == null || item.Attributes == null) return "";
            XmlAttribute a = item.Attributes[attrName];
            return a != null ? (a.Value ?? "") : "";
        }
        public static Person NewXmlNodePerson(XmlNode item,ref Person Parent)
        {
            Person Node = new Person();
            Node.IsGrouping = true;
            Node.hasHelp = false;

            //分类节点
            if (item.Name.Equals("class"))
            {
                XmlNode nameAttr = item.Attributes != null ? item.Attributes.GetNamedItem("Name") : null;
                Node.Name = nameAttr != null ? nameAttr.Value : "";
                Node.HelpUrl = "";
                Node.Path = "";
            }
            else
            {
                //普通节点显示的名字
                XmlNode id_node = item.SelectSingleNode("display");
                if (id_node != null)
                    Node.Name = id_node.InnerText;
               
                Node.message = SafeInnerText(item, "about");

                string url = SafeInnerText(item, "url");
                if (url.Length > 3)
                {
                    Node.HelpUrl = url;
                    Node.hasHelp = true;
                }
                else {
                    Node.HelpUrl = "";
                }

                Node.RootType = SafeAttr(item, "rootpath");
                Node.ext = SafeAttr(item, "ext");

                Node.Path = SafeInnerText(item, "path");
                SetPath(ref Node);

                Node.IsGrouping = false;

                //普通节点
            }
            if (Parent != null)
            {
                Parent.Children.Add(Node);
            }
            return Node;
        }
        //传入一个分节点，然后分解他的子节点
        public static void GetClassNodeItemChilders(XmlNode classNode,ref Person Parent)
        {
            //先创建分类节点自己
            Person SelfNode = NewXmlNodePerson(classNode, ref Parent);
           
            //然后遍历该分类节点的子级
            foreach (XmlNode item in classNode.ChildNodes)
            {
                if (item.Name.Equals("class"))
                {
                    GetClassNodeItemChilders(item, ref SelfNode);
                }
                if (item.Name.Equals("item"))
                {
                    NewXmlNodePerson(item,ref SelfNode);
                }
            }
        }
        #endregion  
    }
}