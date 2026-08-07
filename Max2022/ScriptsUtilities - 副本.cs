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
        /*public static void serializePack(PackViewModel body_model, string path, bool is_new_version)
        {
            string xmlString = SerializeToXmlString(body_model);
            System.IO.File.WriteAllText(path, xmlString);
        }*/
    }
    // 2022 - 2024
    public static class ScriptsUtilities
    {
        private static Ini config;
        private static string[] colorTheme_dark = new string[] { "#ffffff", "#60c8f3", "#27e8dd ", "#FF79B123", "#8c96cb", "#bbff00", "#252525", "#444444", "#5a5a5a", "#6d8323" };
        private static string[] colorTheme_light = new string[] { "#00020f", "#72d6ff", "#27e8dd ", "#FF79B123", "#8c96cb", "#6d83fa", "#a0a0a0", "#bfbfbf", "#eeeeee", "#6d83fa" };

        public static string[] color_lib = new string[] {
            "#bdbdbc","#676767","#c35c4d","#f3e068","#4ba062","#4b9f5f","#489dae","#afa0df"
        };
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
                            SetItems.Add(new NameSet(global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i), i));
                        }
                    }
                    else
                    {
                        if (!FindNameFilter(n, nameFilter))
                        {
                            SetItems.Add(new NameSet(global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i), i));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    string n = global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i);
                    SetItems.Add(new NameSet(global.INamedSelectionSetManager.Instance.GetNamedSelSetName(i), i));

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

            IINodeTab tp = global.INodeTab.Create();
            global.INamedSelectionSetManager.Instance.GetNamedSelSetList(tp, index);

            if (!selectmore)
            {
                ip4.ClearNodeSelection(true);
            }
            ip4.SelectNodeTab(tp, true, true);

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

        private static string ToHex(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        public static string GetMaxBackgroundColor_Str()
        {
            return ToHex(GetMaxColor(GuiColors.Background));
        }
        public static System.Drawing.Color GetMaxColor(GuiColors type)
        {
            System.Drawing.Color bg_color;
#if M2015 || M2016
            bg_color = global.ColorManager.GetColor(type);
#else
            bg_color = global.ColorManager.GetColor(GuiColors.Background, Autodesk.Max.IColorManager.State.Normal);
#endif
            return bg_color;

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

        public static string GetMaxRoot()
        {
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

    }
}
