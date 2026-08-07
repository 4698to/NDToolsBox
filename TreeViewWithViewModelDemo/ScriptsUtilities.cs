using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Autodesk.Max;
using Autodesk.Max.IColorManager;
using ManagedServices;
using Autodesk.Max.LightscapeLight;
using Autodesk.Max.IAnimLayerControlManager;
using System.Xml.Serialization;
using System.Xml;
using System.Windows.Interop;
namespace NDToolsBox
{
    // 2015 - 2021
    public static class XmlUtilites
    {
        public static string SerializeToXmlString(object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }
        public static T DeserializeFromXmlString<T>(string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }
        //反序列化 对象 list
        public static List<T> DeserializeXmlToListT<T>(string xmlFile ,ref string msg)
        {
            List<T> dataList = new List<T>();
            try
            {
                using (StreamReader reader = new StreamReader(xmlFile, System.Text.Encoding.UTF8))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<T>));
                    var tempList = serializer.Deserialize(reader) as List<T>;
                    if (tempList != null && tempList.Any())
                    {
                        dataList = new List<T>(tempList);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                ScriptsUtilities.print(ex.Message);
            }
            return dataList;
        }
        //序列化 对象 list
        public static void SerializeListTToXml<T>(List<T> dataList, string xmlFileName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<T>));
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = System.Text.Encoding.UTF8;
                var xmlWriter = XmlWriter.Create(xmlFileName, settings);
                serializer.Serialize(xmlWriter, dataList);
                xmlWriter.Close();

                MessageBox.Show(
                "导出完成 ",
                "OK",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
                MessageBox.Show(
                $"导出错误 {ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
            }
        }

    }
    public static class ScriptsUtilities
    {
        private static Ini config;
        private static string[] Mouse_ColorTheme_Dark = new string[] { "#3b3939", "#5f8ac1", "#444444" };
        private static string[] Mouse_ColorTheme_Light = new string[] { "#a5a5a5", "#fffdfd", "#b9b9b9" };

        private static string[] colorTheme_dark = new string[] { "#ffffff", "#646464", "#646464", "#646464", "#646464", "#bbff00", "#252525", "#444444", "#5a5a5a", "#6d8323" };
        private static string[] colorTheme_light = new string[] { "#00020f", "#e1e1e1", "#e1e1e1 ", "#e1e1e1", "#e1e1e1", "#6d83fa", "#a0a0a0", "#bfbfbf", "#eeeeee", "#6d83fa" };

        //private static string[] colorTheme_light = new string[] { "#00020f", "#72d6ff", "#27e8dd ", "#FF79B123", "#8c96cb", "#6d83fa", "#a0a0a0", "#bfbfbf", "#eeeeee", "#6d83fa" };
        public static void GetMaxLanguage()
        { 
         
        }
        public static string[] color_lib = new string[] {
            "#bdbdbc","#676767","#c35c4d","#f3e068","#4ba062","#4b9f5f","#489dae","#afa0df"
        };
        //导入选择集xml 
        public static void ImportSeleSet(string xmlFile)
        {
            ExecuteMAXScriptScript($"NDNamedSelSetsToolsInit.load_xml @\"{xmlFile}\"");
        }
        public static void ExportSeleSet(string xmlFile)
        {
            ExecuteMAXScriptScript($"NDNamedSelSetsToolsInit.save_xml @\"{xmlFile}\"");
        }
        //导入选择集xml 
        public static void ImportSeleSet()
        {
            ExecuteMAXScriptScript($"NDNamedSelSetsToolsInit._import()");
        }
        //导出选择集xml
        public static void ExportSeleSet()
        {
            ExecuteMAXScriptScript($"NDNamedSelSetsToolsInit._export()");
        }
        public static List<SetNameObject> GetSeteNames()
        {
            //https://www.cnblogs.com/Fred1987/p/18606119
            List<SetNameObject> NamedSelSet = new List<SetNameObject>();
            try
            {
                NameSet[] s = GetSeleSetNames();
                //https://www.cnblogs.com/Fred1987/p/18606119
                
                foreach (NameSet i in s)
                {
#if M2015 || M2016 || M2017 || M2018 || M2019
                    IINodeTab Nodes = global.NodeTab.Create();
#else
                    IINodeTab Nodes = global.INodeTab.Create();//m2020,2021
#endif
                    SetNameObject SetSel = new SetNameObject(i.name);

                    global.INamedSelectionSetManager.Instance.GetNamedSelSetList(Nodes, i.index);
                    for (int t = 0; t < Nodes.Count; t++)
                    {
#if M2015 || M2016
                        SetSel.Nodes.Add(new Node(Nodes[(IntPtr)t].Name));
#else
                        SetSel.Nodes.Add(new Node(Nodes[t].Name));
#endif
                    }
                    NamedSelSet.Add(SetSel);

                }
            }
            catch (Exception ex)
            {

            }
            return NamedSelSet;
        }
        public static void SeteToXMl(List<SetNameObject> NamedSelSet, string xmlFile)
        {
            XmlUtilites.SerializeListTToXml<SetNameObject>(NamedSelSet, xmlFile);
        }
        public static void SerializeListTToXml(string xmlFile)
        {
            try
            {
                
                //https://www.cnblogs.com/Fred1987/p/18606119
                List<SetNameObject> NamedSelSet = GetSeteNames();
                //XmlUtilites.SerializeListTToXml<SetNameObject>(NamedSelSet, xmlFile);
                SeteToXMl(NamedSelSet, xmlFile);

            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }

        }

        //读取 xml 反序列化选择集
        public static void DeserializeXml(string xmlFile)
        {
            string msg = "";

            List<SetNameObject> deserializedObjectList = XmlUtilites.DeserializeXmlToListT<SetNameObject>(xmlFile,ref msg);
            if (deserializedObjectList.Count < 1)
            {
                MessageBox.Show(
                $"导入错误 -< {msg}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
                return;
            }
            int index = 0;
            for (int i = 0; i < deserializedObjectList.Count; i++)
            {
                SetNameObject SetSel = deserializedObjectList[i];
                ScriptsUtilities.NewSeleSetName(SetSel,ref index);
            }
            MessageBox.Show(
                $"导入完成 ,数量->{index}",
                "OK",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
        }
        //读写选择集 xml 重新创建 
        public static void NewSeleSetName(SetNameObject SetSel,ref int index )
        {
            try
            {
                if (SetSel.Nodes.Count < 1)
                {
                    ScriptsUtilities.print($"no nodes -> {SetSel.Name}");
                    return;
                }
#if M2015 || M2016 || M2017 || M2018 || M2019
                IINodeTab Nodes = global.NodeTab.Create();
#else
                IINodeTab Nodes = global.INodeTab.Create();//m2020,2021
#endif
                
                for (int i = 0; i < SetSel.Nodes.Count; i++)
                {
                    IINode n = ip.GetINodeByName(SetSel.Nodes[i].Name);
                    
                    if (n != null)
                    {
                        Nodes.AppendNode(n, false, 1);
                    }
                }
                if (Nodes.Count < 1)
                {
                    return;
                }
                string set_name = SetSel.Name;
                global.INamedSelectionSetManager.Instance.RemoveNamedSelSet(ref set_name);

                if (global.INamedSelectionSetManager.Instance.AddNewNamedSelSet(Nodes, ref set_name))
                {
                    index += 1;
                    //ScriptsUtilities.print($"AddNewNamedSelSet -{set_name} , {Nodes.Count}");
                }
                else
                {
                    //ScriptsUtilities.print("AddNewNamedSelSet -> ");
                }
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }

        }

        public static List<string> get_color_random(int indexCount)
        {
            List<string> color_ = new List<string>();

            Random random_ = new Random();

            float d = (float)indexCount / 3.0f;
            int sel_count = (int)Math.Ceiling(d);

            for (int i = 0; i < sel_count; i++)
            {
                int id = random_.Next(8);

                for (int o = 0; o < 3; o++)
                {
                    color_.Add(color_lib[id]);
                }
            }
            return color_;
        }
        public static List<string> GetFilterNames()
        { 
            List<string> FilterString = new List<string>();
            try
            {
                bool a = Properties.Settings.Default.filter_a;
                bool b = Properties.Settings.Default.filter_b;
                bool c = Properties.Settings.Default.filter_c;

                string at = Properties.Settings.Default.filter_a_text;
                if (a & !string.IsNullOrEmpty(at))
                {
                    FilterString.Add(at);
                }

                string bt = Properties.Settings.Default.filter_b_text;
                if (b & !string.IsNullOrEmpty(bt))
                {
                    FilterString.Add(bt);
                }

                string ct = Properties.Settings.Default.filter_c_text;
                if (c & !string.IsNullOrEmpty(ct))
                {
                    FilterString.Add(ct);
                }
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }

            return FilterString;
        }
        public static bool FindNameFilter(string name, List<string> Filter)
        {
            for (int i = 0; i < Filter.Count; i++)
            {
                if (name.IndexOf(Filter[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

            }
            return false;
        }
        public static NameSet[] GetSeleSetNames()
        {
            int num = global.INamedSelectionSetManager.Instance.NumNamedSelSets;
            //获取过滤词
            List<string> nameFilter = GetFilterNames();
            
            //是否反向过滤
            bool isBack = Properties.Settings.Default.filter_back;
            List<NameSet> SetItems = new List<NameSet>();
            
            if (nameFilter.Count > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    string n = global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i);
                    if (!isBack)
                    {
                        if (FindNameFilter(n, nameFilter))
                        {
                            SetItems.Add(new NameSet(n, i));
                        }
                    }
                    else
                    {
                        if (!FindNameFilter(n, nameFilter))
                        {
                            SetItems.Add(new NameSet(n, i));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    string n = global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i);
                    SetItems.Add(new NameSet(n, i));

                }
            }
            List<string> sel_color = get_color_random(SetItems.Count);
            for (int i = 0; i < SetItems.Count; i++)
            {
                SetItems[i].color_ = sel_color[i];
            }

            return SetItems.ToArray();
        }
        public static void SeleSet(int index, bool selectmore)
        {
            //global.INamedSelectionSetManager.Instance
            //IGenericNamedSelSetList iset = global.GenericNamedSelSetList.Create();
            try
            {
#if M2015 || M2016 || M2017 || M2018 || M2019
                IINodeTab tp = global.NodeTab.Create();
#else
            IINodeTab tp = global.INodeTab.Create();//m2020,2021
#endif

                global.INamedSelectionSetManager.Instance.GetNamedSelSetList(tp, index);

                if (!selectmore)
                {
                    ip4.ClearNodeSelection(true);
                }
                ip4.SelectNodeTab(tp, true, true);
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }
        }
        public static void RemoveSeleSet(string name)
        {
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    string refname = name;
                    global.INamedSelectionSetManager.Instance.RemoveNamedSelSet(ref refname);
                }
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }

        }
        public static void AddNodeToSeleSet(int index,bool is_add)
        {
            try
            {
                int Select_count = ip.SelNodeCount;
                if (Select_count == 0)
                {
                    return;
                }
#if M2015 || M2016 || M2017 || M2018 || M2019
                IINodeTab tp = global.NodeTab.Create();
#else
                IINodeTab tp = global.INodeTab.Create();//m2020,2021
#endif
                if (global.INamedSelectionSetManager.Instance.GetNamedSelSetList(tp, index))
                {
                    if (tp.Count < 1)
                    {
                        return;
                    }
                    if (is_add)
                    {
                        for (int i = 0; i < Select_count; i++)
                        {
                            IINode snode = ip.GetSelNode(i);
                            tp.AppendNode(snode, false, 1);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Select_count; i++)
                        {
                            IINode snode = ip.GetSelNode(i);
                            tp.RemoveNode(snode);
                        }
                    }

                    string set_name = global.INamedSelectionSetManager.Instance.GetNamedSelSetName(index);

                    global.INamedSelectionSetManager.Instance.RemoveNamedSelSet(ref set_name);

                    global.INamedSelectionSetManager.Instance.AddNewNamedSelSet(tp, ref set_name);
                    //ip4.SelectNodeTab(tp, true, true);
                }
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }
        }

        public static void NewSeleSet()
        {
            ExecuteMAXScriptScript("NDNamedSelSetsToolsInit.add_new_sele_set();");
        }
        public static int AnimStart()
        {
            return (ip4.AnimRange.Start / global.TicksPerFrame);
        }
        public static int AnimEnd()
        {
            return (ip4.AnimRange.End / global.TicksPerFrame);
        }
        public static void LinkNodeAlign()
        {
            string mxs = $"{GetMaxRoot()}scripts\\NDTools\\TimeLine_Tools\\LinkNodeAlign.ms";
            FileinMxs(mxs);
        }
        public static void LinkNode()
        {
            string mxs = $"{GetMaxRoot()}scripts\\NDTools\\TimeLine_Tools\\LinkNode.ms";
            FileinMxs(mxs);
        }
        public static void HideCSBip(bool hide_state)
        {
            if (hide_state)
            {
                ExecuteMAXScriptScript("(CS_All=for i in objects where (classof i==Biped_Object ) collect i;hide CS_All)");
            }
            else { 
                ExecuteMAXScriptScript("(CS_All=for i in objects where (classof i==Biped_Object ) collect i;unhide CS_All)");
            }

        }
        public static void HideBone(bool hide_state)
        {
            if (hide_state)
            {
                ExecuteMAXScriptScript("(Bn_All=for i in objects where (classof i==BoneGeometry ) collect i;hide Bn_All)");
            }
            else
            {
                ExecuteMAXScriptScript("(Bn_All=for i in objects where (classof i==BoneGeometry ) collect i;unhide Bn_All)");
            }

        }
        public static void MaxBoxModeSelected()
        {
            ExecuteMAXScriptScript("max box mode selected");
        }
        public static void hideByCategory(int type,bool hide_state)
        {
            if (type.Equals(1))
            {
                ExecuteMAXScriptScript($"hideByCategory.geometry = {hide_state.ToString()}");
            }
            if (type.Equals(2))
            {
                ExecuteMAXScriptScript($"hideByCategory.shapes = {hide_state.ToString()}");
            }
            if (type.Equals(16))
            {
                ExecuteMAXScriptScript($"hideByCategory.helpers = {hide_state.ToString()}");
            }
            if (type.Equals(256))
            {
                ExecuteMAXScriptScript($"hideByCategory.Bones = {hide_state.ToString()}");
            }
            if (type.Equals(4))
            {
                ExecuteMAXScriptScript($"hideByCategory.lights = {hide_state.ToString()}");
            }
            if (type.Equals(8))
            {
                ExecuteMAXScriptScript($"hideByCategory.cameras = {hide_state.ToString()}");
            }
            //return ip4.HideByCategoryFlags;
            //ip4.SetHideByCategoryFlags
        }
        //private static string ToHex(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static string GetMaxBackgroundColor_Str()
        {
            return GetMouseOverColor(2);

            //return ToHex(GetMaxColor(GuiColors.Background));
        }
/*        public static System.Drawing.Color GetMaxColor(GuiColors type)
        {
            System.Drawing.Color bg_color;
#if M2015 || M2016
            bg_color = global.ColorManager.GetColor(type);
#else
            bg_color = global.ColorManager.GetColor(GuiColors.Background,Autodesk.Max.IColorManager.State.Normal);
#endif
            
            //System.Windows.Media.Color max_color = System.Windows.Media.Color.FromArgb(bg_color.A, bg_color.R, bg_color.G, bg_color.B);
            return bg_color;

        }*/

        public static string GetMouseOverColor(int type)
        {
            //暗黑
            if (global.ColorManager.AppFrameColorTheme == AppFrameColorTheme.DarkTheme)
            {
                return Mouse_ColorTheme_Dark[type];
            }
            //灰白
            else
            {
                return Mouse_ColorTheme_Light[type];
            }

        }


        public static string mGetColor(int type)
        {
            //System.Windows.Media.Color max_color;
            //暗黑
            if (global.ColorManager.AppFrameColorTheme == AppFrameColorTheme.DarkTheme)
            {
                //max_color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorTheme_dark[type]);
                return colorTheme_dark[type];
            }
            //灰白
            else
            {
                //max_color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorTheme_light[type]);
                return colorTheme_light[type];
            }
        }

        public static string GetNDBoxMxsCommitScriptName(string commit)
        {
            if (commit.StartsWith("--NDDrop"))
            {
                string[] commits = commit.Split(';');
                if (commits.Length > 2)
                {
                    return commits[1];
                }
            }
            return string.Empty;

        }
        public static void ExecuteMAXScriptScript(string commit)
        {
            bool commandState;
#if M2019 || M2020 || M2021
            commandState = global.ExecuteMAXScriptScript(commit, true, null,false);
#else
            commandState = global.ExecuteMAXScriptScript(commit, true, null);
#endif
            if (commandState)
            {
                print($"{commit} -> OK");
            }
            else
            {
                MessageBox.Show(
                $"脚本错误 -< {commit}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
                print($"出错 {commit}");
            }
        }
        public static void FileinMxs(string mxs_path)
        {
            if (File.Exists(mxs_path))
            {
                string ext = Path.GetExtension(mxs_path);
                if (ext.Equals(".ms") || ext.Equals(".mse"))
                {
                    bool commandState = FileinScriptEx(mxs_path);
                    if (commandState)
                    {
                        print($"{mxs_path} -> OK");
                    }
                    else
                    {
                        MessageBox.Show(
                        $"脚本错误 -< {mxs_path}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                        );
                        print($"出错 {mxs_path}");
                    }
                }
            }
            else {
                MessageBox.Show(
                        "此资源不存在.",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                        );
                print($"no exists : {mxs_path}");

            }
        }
        public static void ImportFBX(string fbxpath)
        {
            if (File.Exists(fbxpath))
            {
                if (!global.COREInterface13.ImportFromFile(fbxpath, false, null))
                {
                    MessageBox.Show($"文件错误 -< {fbxpath}","导入失败",MessageBoxButton.OK,MessageBoxImage.Information);
                }

            }

        }
        /// <summary>
        /// Maxscript filein 执行脚本 
        /// </summary>
        /// <param name="Command"></param>
        /// <returns></returns>
        public static bool FileinScriptEx(string Command)
        {
            string error_message = "";
            //IntPtr ErrorPtr = Marshal.StringToHGlobalAnsi(error_message);
            return ScriptsUtilities.global.FileinScriptEx(Command, error_message);
        }
        public static string iconUri()
        {
            string icon = Path.Combine(GetMaxRoot(), "UI_ln\\Icons\\NDBoxMao_24i.bmp");
            if (File.Exists(icon))
            {
                return icon;
            }
            return null;
        }
        public static double GetDialogLocationt(string key) 
        {
            if (config == null)
            {
                return 0.0d;
            }
            else
            {
                double value_d = 0.0d;
                string value = config.GetValue(key, "NDBox");
                double.TryParse(value, out value_d);
                return value_d;
            }
        }
        public static void SaveDialogLocation(string key, string value)
        {
            if (config != null)
            {
                config.WriteValue(key, "NDBox", value);
            }
           
        }
        
        public static void Save()
        {
            if (config != null)
            {
                config.Save();
            }
        }
        public static IGlobal global
        {
            get { return GlobalInterface.Instance; }
        }
        public static IInterface13 ip
        {
            get { return global.COREInterface13; }
        }
        public static IInterface14 ip4
        {
            get
            {
                return global.COREInterface14;
            }
        }
        public static IInterface8 ip8
        {
            get
            {
                return global.COREInterface8;
            }
        }
        public static IClass_ID Class_ID
        {
            get { return global.Class_ID.Create(0x267a19d5, 0xb196507); }

        }

        public static void print(string str)
        {
            global.TheListener.EditStream.Printf(string.Format("{0}\n", str));
            global.TheListener.EditStream.Flush();
        }

        public static string GetMaxRoot()
        {
            //"D:\Program Files\Autodesk\3ds Max 2016\"
            return ip.GetDir(20); ;
        }

        public static void DisableAccelerators()
        {
            AppSDK.DisableAccelerators();
        }

        public static void EnableAccelerators()
        {
            AppSDK.EnableAccelerators();
        }
        public static void AssemblyMain()
        {
            ip4.AddClass(new Descriptor());
            if (config == null)
            {
                config = new Ini(ip.MAXIniFile);
            }
        }
        public static void AssemblyInitializationCleanup()
        {

        }

        public static void AssemblyShutdown()
        {

        }
        public static void PlayAnim(bool isplay)
        {
            if (isplay)
            {
                ExecuteMAXScriptScript("playAnimation immediateReturn:true");
            }
            else { 
                ExecuteMAXScriptScript("stopAnimation()");
            }
        }
        public static void timeConfiguration(int index)
        {
            //ip4.PlaybackSpeed = index;
            ExecuteMAXScriptScript($"timeConfiguration.playbackSpeed = {index}");
        }
    }
}
