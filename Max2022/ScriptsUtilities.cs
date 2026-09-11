using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using ManagedServices;
using System.IO;
using System.Windows.Media;
using System.Windows;
using Autodesk.Max.MAXScript;
using Autodesk.Max.IColorManager;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Xml;

namespace NDToolsBox
{

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
        public static List<T> DeserializeXmlToListT<T>(string xmlFile,ref string msg )
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
    // 2022 - 2024
    public static class ScriptsUtilities
    {
        private static Ini config;

        private static string[] Mouse_ColorTheme_Dark = new string[] { "#3b3939", "#5f8ac1", "#444444" };
        private static string[] Mouse_ColorTheme_Light = new string[] { "#a5a5a5", "#fffdfd" , "#b9b9b9" };

        //private static string[] colorTheme_dark = new string[] { "#ffffff", "#60c8f3", "#27e8dd ", "#FF79B123", "#8c96cb", "#bbff00", "#252525", "#444444", "#5a5a5a", "#6d8323" };
        private static string[] colorTheme_dark = new string[] { "#ffffff", "#646464", "#646464", "#646464", "#646464", "#bbff00", "#252525", "#444444", "#5a5a5a", "#6d8323" };

        private static string[] colorTheme_light = new string[] { "#00020f", "#72d6ff", "#27e8dd", "#FF79B123", "#8c96cb", "#6d83fa", "#a0a0a0", "#bfbfbf", "#eeeeee", "#6d83fa" };

        public static string[] color_lib = new string[] {
            "#bdbdbc","#676767","#c35c4d","#f3e068","#4ba062","#4b9f5f","#489dae","#afa0df"
        };

        /// <summary>与 MaxScript getAppData/setAppData 互通的 AppData 键（v4.ms 使用 1001 存 GUID）。</summary>
        public const uint MxsAppDataGuidId = 1001;

        private static readonly uint MxsUtilityClassIdA = 0x4d64858;
        private static readonly uint MxsUtilityClassIdB = 0x16d1751d;

        private static IClass_ID GetMxsAppDataClassId()
        {
            return global.Class_ID.Create(MxsUtilityClassIdA, MxsUtilityClassIdB);
        }

        public static string GetMxsAppData(IINode node, uint id)
        {
            if (node == null)
            {
                return null;
            }
            try
            {
                IAnimatable anim = node as IAnimatable;
                if (anim == null)
                {
                    return null;
                }
                IAppDataChunk chunk = anim.GetAppDataChunk(GetMxsAppDataClassId(), SClass_ID.Utility, id);
                if (chunk == null || chunk.Data == null || chunk.Data.Length == 0)
                {
                    return null;
                }
                return DecodeMxsAppDataBytes(chunk.Data);
            }
            catch (Exception ex)
            {
                print($"GetMxsAppData 失败: {ex.Message}");
                return null;
            }
        }

        public static void SetMxsAppData(IINode node, uint id, string value)
        {
            if (node == null)
            {
                return;
            }
            try
            {
                IAnimatable anim = node as IAnimatable;
                if (anim == null)
                {
                    return;
                }
                IClass_ID cid = GetMxsAppDataClassId();
                anim.RemoveAppDataChunk(cid, SClass_ID.Utility, id);
                if (value == null)
                {
                    return;
                }
                byte[] bytes = Encoding.UTF8.GetBytes(value + "\0");
                anim.AddAppDataChunk(cid, SClass_ID.Utility, id, bytes);
            }
            catch (Exception ex)
            {
                print($"SetMxsAppData 失败: {ex.Message}");
            }
        }

        private static string DecodeMxsAppDataBytes(byte[] data)
        {
            int start = 0;
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                start = 3;
            }
            int end = data.Length;
            for (int i = start; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    end = i;
                    break;
                }
            }
            if (end <= start)
            {
                return string.Empty;
            }
            try
            {
                return Encoding.UTF8.GetString(data, start, end - start);
            }
            catch
            {
                return Encoding.Default.GetString(data, start, end - start);
            }
        }

        public static string EnsureNodeGuid(IINode node)
        {
            if (node == null)
            {
                return null;
            }
            string guid = GetMxsAppData(node, MxsAppDataGuidId);
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }
            guid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            SetMxsAppData(node, MxsAppDataGuidId, guid);
            return guid;
        }

        public static string CombineDialogPath(string filePath, string directory)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }
            if (string.IsNullOrWhiteSpace(directory))
            {
                return filePath;
            }
            return Path.Combine(directory, filePath);
        }

        public static string EnsureXmlExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
            if (!path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return path + ".xml";
            }
            return path;
        }

        public static string GetDefaultSelSetFileName()
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(ip.CurFileName);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
            catch
            {
            }
            return "SelSets";
        }

        public static string GetDefaultSelSetDirectory()
        {
            try
            {
                string dir = Path.GetDirectoryName(ip.CurFilePath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    return dir;
                }
            }
            catch
            {
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private static bool TryPickXmlFile(bool openForRead, out string filePath, out string initialDir)
        {
            filePath = GetDefaultSelSetFileName();
            initialDir = GetDefaultSelSetDirectory();
            IntPtr hwnd = AppSDK.GetMaxHWND();
            var filters = new FileDialogFilterList("xml (*.xml)|*.xml|");
            if (openForRead)
            {
                return PathSDK.DoMaxOpenDialog(hwnd, "import selName xml", ref filePath, ref initialDir, filters);
            }
            return PathSDK.DoMaxSaveAsDialog(hwnd, "export selName xml", ref filePath, ref initialDir, filters);
        }

        public static NameSet[] GetAllSeleSetNames()
        {
            int num = global.INamedSelectionSetManager.Instance.NumNamedSelSets;
            List<NameSet> SetItems = new List<NameSet>();
            for (int i = 0; i < num; i++)
            {
                string n = global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i);
                SetItems.Add(new NameSet(n, i));
            }
            return SetItems.ToArray();
        }

        public static List<SetNameObject> GetSeteNames()
        {
            //https://www.cnblogs.com/Fred1987/p/18606119
            List<SetNameObject> NamedSelSet = new List<SetNameObject>();
            try
            {
                NameSet[] s = GetAllSeleSetNames();
                foreach (NameSet i in s)
                {
                    IINodeTab Nodes = global.INodeTab.Create();
                    SetNameObject SetSel = new SetNameObject(i.name);

                    global.INamedSelectionSetManager.Instance.GetNamedSelSetList(Nodes, i.index);
                    for (int t = 0; t < Nodes.Count; t++)
                    {
                        IINode inode = Nodes[t];
                        if (inode == null)
                        {
                            continue;
                        }
                        string guid = EnsureNodeGuid(inode);
                        SetSel.Nodes.Add(new Node(inode.Name, guid));
                    }
                    NamedSelSet.Add(SetSel);

                }
            }
            catch (Exception ex)
            {
                print($"GetSeteNames 失败: {ex.Message}");
            }
            return NamedSelSet;
        }
        public static void SeteToXMl(List<SetNameObject> NamedSelSet ,string xmlFile)
        {
            XmlUtilites.SerializeListTToXml<SetNameObject>(NamedSelSet, xmlFile);
        }
        public static void SerializeListTToXml(string xmlFile)
        {
            try
            {
                //https://www.cnblogs.com/Fred1987/p/18606119
                List<SetNameObject> NamedSelSet = GetSeteNames();
                SeteToXMl(NamedSelSet, xmlFile);    
                //XmlUtilites.SerializeListTToXml<SetNameObject>(NamedSelSet, xmlFile);
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
            }

        }
        //导入选择集xml 
        public static void ImportSeleSet(string xmlFile)
        {
            string fullPath = string.IsNullOrWhiteSpace(xmlFile) || xmlFile.IndexOf('"') >= 0
                ? null
                : xmlFile.Trim();
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                print($"ImportSeleSet 路径无效或不存在: {xmlFile}");
                MessageBox.Show(
                    $"导入失败：文件不存在\n{xmlFile}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            DeserializeXml(fullPath);
        }
        //导出选择集xml
        public static void ExportSeleSet(string xmlFile)
        {
            string fullPath = EnsureXmlExtension(
                string.IsNullOrWhiteSpace(xmlFile) || xmlFile.IndexOf('"') >= 0
                    ? null
                    : xmlFile.Trim());
            if (string.IsNullOrEmpty(fullPath))
            {
                print($"ExportSeleSet 路径无效: {xmlFile}");
                MessageBox.Show(
                    $"导出失败：路径无效\n{xmlFile}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                MessageBox.Show(
                    $"导出失败：目录不存在\n{dir}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            SerializeListTToXml(fullPath);
        }
        //导入选择集xml 
        public static void ImportSeleSet()
        {
            string filePath;
            string initialDir;
            if (!TryPickXmlFile(openForRead: true, out filePath, out initialDir))
            {
                return;
            }
            ImportSeleSet(CombineDialogPath(filePath, initialDir));
        }
        //导出选择集xml
        public static void ExportSeleSet()
        {
            string filePath;
            string initialDir;
            if (!TryPickXmlFile(openForRead: false, out filePath, out initialDir))
            {
                return;
            }
            ExportSeleSet(CombineDialogPath(filePath, initialDir));
        }
        /// <summary>为 true 时忽略 NamedSelSet 通知触发的工具条刷新（批量导入期间）。</summary>
        public static bool SuspendSelSetToolbarRefresh { get; private set; }

        /// <summary>选择集列表批量变更完成（如 XML 导入结束）后通知 UI 刷新一次。</summary>
        public static event Action SelSetListChanged;

        public static void NotifySelSetListChanged()
        {
            Action handler = SelSetListChanged;
            if (handler != null)
            {
                handler();
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

            Dictionary<string, IINode> guidMap = BuildSceneGuidMap();
            int yes = 0;
            int no = 0;
            StringBuilder detail = new StringBuilder();
            SuspendSelSetToolbarRefresh = true;
            try
            {
                for (int i = 0; i < deserializedObjectList.Count; i++)
                {
                    SetNameObject SetSel = deserializedObjectList[i];
                    if (TryCreateSeleSetFromXml(SetSel, guidMap, detail))
                    {
                        yes += 1;
                    }
                    else
                    {
                        no += 1;
                    }
                }
            }
            finally
            {
                SuspendSelSetToolbarRefresh = false;
            }
            // 全部导入完成后再刷新选择集工具条（避免每个 AddNewNamedSelSet 都刷一次）
            NotifySelSetListChanged();

            string summary = $"成功导入：{yes} 个, 失败：{no}";
            print(summary);
            if (detail.Length > 0)
            {
                print(detail.ToString());
            }
            MessageBox.Show(
                summary,
                "打开 Listener 查看详细情况",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
        }

        private static Dictionary<string, IINode> BuildSceneGuidMap()
        {
            var map = new Dictionary<string, IINode>(StringComparer.OrdinalIgnoreCase);
            try
            {
                IINode root = ip.RootNode;
                if (root == null)
                {
                    return map;
                }
                CollectGuidMap(root, map, includeSelf: false);
            }
            catch (Exception ex)
            {
                print($"BuildSceneGuidMap 失败: {ex.Message}");
            }
            return map;
        }

        private static void CollectGuidMap(IINode node, Dictionary<string, IINode> map, bool includeSelf)
        {
            if (node == null)
            {
                return;
            }
            if (includeSelf)
            {
                string guid = GetMxsAppData(node, MxsAppDataGuidId);
                if (!string.IsNullOrEmpty(guid) && !map.ContainsKey(guid))
                {
                    map[guid] = node;
                }
            }
            int count = node.NumberOfChildren;
            for (int i = 0; i < count; i++)
            {
                CollectGuidMap(node.GetChildNode(i), map, includeSelf: true);
            }
        }

        private static IINode FindNodeByGuid(string guid, Dictionary<string, IINode> guidMap)
        {
            if (string.IsNullOrEmpty(guid) || guidMap == null)
            {
                return null;
            }
            IINode found;
            if (guidMap.TryGetValue(guid, out found))
            {
                return found;
            }
            return null;
        }

        private static bool NodeListContains(List<IINode> list, IINode node)
        {
            if (list == null || node == null)
            {
                return false;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (object.ReferenceEquals(list[i], node))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryCreateSeleSetFromXml(SetNameObject SetSel, Dictionary<string, IINode> guidMap, StringBuilder detail)
        {
            try
            {
                if (SetSel == null || string.IsNullOrEmpty(SetSel.Name) || SetSel.Nodes == null || SetSel.Nodes.Count < 1)
                {
                    if (detail != null && SetSel != null)
                    {
                        detail.AppendLine($"no nodes -> {SetSel.Name}");
                    }
                    return false;
                }

                var matched = new List<IINode>();
                int matchedExpected = 0;
                for (int i = 0; i < SetSel.Nodes.Count; i++)
                {
                    Node item = SetSel.Nodes[i];
                    if (item == null)
                    {
                        continue;
                    }
                    string wantGuid = item.GUID;
                    IINode byName = string.IsNullOrEmpty(item.Name) ? null : ip.GetINodeByName(item.Name);

                    if (byName != null)
                    {
                        string nodeGuid = GetMxsAppData(byName, MxsAppDataGuidId);
                        if (!string.IsNullOrEmpty(wantGuid) && string.Equals(nodeGuid, wantGuid, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!NodeListContains(matched, byName))
                            {
                                matched.Add(byName);
                            }
                            matchedExpected += 1;
                        }
                        else
                        {
                            IINode byGuid = FindNodeByGuid(wantGuid, guidMap);
                            if (byGuid != null)
                            {
                                if (!NodeListContains(matched, byGuid))
                                {
                                    matched.Add(byGuid);
                                }
                                matchedExpected += 1;
                            }
                            else if (!NodeListContains(matched, byName))
                            {
                                matched.Add(byName);
                                if (!string.IsNullOrEmpty(wantGuid) && detail != null)
                                {
                                    detail.AppendLine($"{SetSel.Name} GUID无法匹配 {item.Name}");
                                }
                            }
                            else if (detail != null)
                            {
                                detail.AppendLine($"{SetSel.Name} GUID无法匹配 {item.Name}");
                            }
                        }
                    }
                    else
                    {
                        IINode byGuid = FindNodeByGuid(wantGuid, guidMap);
                        if (byGuid != null)
                        {
                            if (!NodeListContains(matched, byGuid))
                            {
                                matched.Add(byGuid);
                            }
                            matchedExpected += 1;
                        }
                        else if (detail != null)
                        {
                            detail.AppendLine($"{SetSel.Name} 无法匹配 {item.Name}");
                        }
                    }
                }

                if (matched.Count < 1)
                {
                    return false;
                }

                IINodeTab Nodes = global.INodeTab.Create();
                for (int i = 0; i < matched.Count; i++)
                {
                    Nodes.AppendNode(matched[i], false, 1);
                }

                string set_name = SetSel.Name;
                global.INamedSelectionSetManager.Instance.RemoveNamedSelSet(ref set_name);
                if (!global.INamedSelectionSetManager.Instance.AddNewNamedSelSet(Nodes, ref set_name))
                {
                    return false;
                }
                return matchedExpected == SetSel.Nodes.Count || matched.Count == SetSel.Nodes.Count;
            }
            catch (Exception ex)
            {
                ScriptsUtilities.print(ex.Message);
                return false;
            }
        }

        //读写选择集 xml 重新创建 
        public static void NewSeleSetName(SetNameObject SetSel,ref int index)
        {
            Dictionary<string, IINode> guidMap = BuildSceneGuidMap();
            if (TryCreateSeleSetFromXml(SetSel, guidMap, null))
            {
                index += 1;
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
        
        //收集场景中的选择集名字
        public static NameSet[] GetSeleSetNames()
        {
            int num = global.INamedSelectionSetManager.Instance.NumNamedSelSets;
            List<string> nameFilter = GetFilterNames();

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
        public static void SeleSet(int index,bool selectmore)
        {
            //global.INamedSelectionSetManager.Instance
            //IGenericNamedSelSetList iset = global.GenericNamedSelSetList.Create();
            try
            {
                IINodeTab tp = global.INodeTab.Create();
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
                IINodeTab tp = global.INodeTab.Create();
                if (global.INamedSelectionSetManager.Instance.GetNamedSelSetList(tp, index))
                {
                    // 空选择集仍允许添加节点；仅移除时列表为空则无需处理
                    if (!is_add && tp.Count < 1)
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
                    else {
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
            else
            {
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
        public static void hideByCategory(int type, bool hide_state)
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
        }
/*        public static System.Drawing.Color GetMaxColor(GuiColors type)
        {
            System.Drawing.Color bg_color;
#if M2015 || M2016
            bg_color = global.ColorManager.GetColor(type);
#else
            bg_color = global.ColorManager.GetColor(GuiColors.Background, Autodesk.Max.IColorManager.State.Normal);
#endif
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
            
            bool commandState = global.ExecuteMAXScriptScript(commit,ScriptSource.NotSpecified, true, null,true);
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
            else
            {
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
            if (!global.COREInterface13.ImportFromFile(fbxpath, false, null))
            {
                MessageBox.Show($"文件错误 -< {fbxpath}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Information);
            }

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
        public static bool FileinScriptEx(string Command)
        {

            ScriptsUtilities.global.FileinScript(Command);
            return true;
            //string error_message = "";
            //IntPtr ErrorPtr = Marshal.StringToHGlobalAnsi(error_message);
            //return MaxComm.global.FileinScriptEx(Command, ErrorPtr,false);

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
        public static IClass_ID Class_ID
        {
            get { return global.Class_ID.Create(0x267a19d5, 0xb196507); }

        }
        public static void print(string str)
        {
            global.TheListener.EditStream.Printf(string.Format("{0}\n", str));
            //global.TheListener.EditStream.Flush();
        }

        /// <summary>
        /// 3ds Max 安装根目录。对应 Max SDK MaxDirectory.MaxSysRootDir（索引 20，2015–2024 一致）。
        /// </summary>
        private const int MaxSysRootDirIndex = 20;

        public static string GetMaxRoot()
        {
            return ip.GetDir(MaxSysRootDirIndex);
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
            else
            {
                ExecuteMAXScriptScript("stopAnimation()");
            }
        }
        public static void timeConfiguration(int index)
        {
            //ip4.PlaybackSpeed = index;
            //ip4.IsAnimPlaying
            ExecuteMAXScriptScript($"timeConfiguration.playbackSpeed = {index}");
        }
    }
}
