using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;
using System.Windows.Input;

namespace NDToolsBox
{
    public static class WebAddress
    {
        //public static string serverIP = "101.34.112.80";
        public static string serverIP = "192.168.251.94";

        public static int Port = 8019;
        //公司内网
        public static string serverName2 = $"http://{serverIP}:{Port}/download?fileid=";
        //public static string serverName1 = "http://sundaybox.cc/downloadfiles?fileid=";
        public static string serverName1 = "http://sundaybox.cc/Test_download?fileid=";//腾讯服务器

        //根目录
        public static string apppath = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox";
        //服务器上的版本号文件，每次启动下载该文件检测是否有版本更新
        public static string serverVersion = "updateBox.txt";

        //服务上的脚本名单，仅仅是名单
        public static string scriptContent = "NDToolsList.json";
        public static string dataModelFile = $@"{apppath}\ToolLists\NDToolsList.json";

        public static string dataModelFileXml = $@"{apppath}\ToolLists\NDToolsList.xml";


        public static string FontFamily = $@"{apppath}\#iconfont"; 
        //更新程序 
        public static string UpdataExE = $@"{apppath}\NDDownload.exe";
        //服务器上的内容列表
        public static string contentList = "InstallBox_version_full.xml";
        public static string contentLocal = $@"{apppath}\InstallBox_version_full.xml";

        //本地的配置文件
        public static string iniConfig = $@"{apppath}\config.ini";

        

        public static string Resources = $@"{apppath}\Resources";


        public static string ToolBarItemConfig = $@"{apppath}\ToolBar.xml";
        public static string ToolBarTabsConfig = $@"{apppath}\ToolBarTabs.xml";
        public static bool pingServer()
        {
            using (Ping pingsender = new Ping())
            {
                PingReply reply = pingsender.Send($"www.sundaybox.cc", 50);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }
        //检测内网的服务器是否正常
        public static bool pingIp(int type)
        {
            switch (type)
            {
                case 0:
                    return true;
                case 1:
                    return WebAddress.pingServer();//腾讯服务器
                case 2:
                    return WebAddress.CheckConnect(serverIP, Port);//公司内网
                case 3:
                    bool intranet = WebAddress.CheckConnect(serverIP, Port);
                    if (intranet)
                    {
                        return intranet;
                    }
                    else {
                        return WebAddress.pingServer();
                    }
            }
            return false; 
        }
        /// <summary>
        /// telnet port 
        /// </summary>
        /// <param name="_ip"></param>
        /// <param name="_port"></param>
        /// <returns></returns>
        /// 检查服务器和端口是否可以连接
        /// </summary>
        /// <param name="ipString">服务器ip</param>
        /// <param name="port">端口</param>
        /// <returns></returns>
        public static bool CheckConnect(string ipString, int port)
        {
            using (System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient()
            { SendTimeout = 200 })
            {
                try
                {
                    IPAddress ip = IPAddress.Parse(ipString);
                    var result = tcpClient.BeginConnect(ip, port, null, null);
                    bool connected = result.AsyncWaitHandle.WaitOne(200);
                    if (connected)
                    {
                        try
                        {
                            tcpClient.EndConnect(result);
                        }
                        catch
                        {
                            return false;
                        }
                        return tcpClient.Connected;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
        public static bool ping(string url)
        {

            using (Ping pingsender = new Ping())
            {
                PingReply reply = pingsender.Send(url, 50);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }
       
        public static void downloadScriptList(string loaclfile,int type)
        {
            string url = string.Concat(serverName1, Path.GetFileName(loaclfile));
            switch (type)
            {
                case 1:
                    url = string.Concat(serverName1, Path.GetFileName(loaclfile));
                    break;
                case 2:
                    url = string.Concat(serverName2, Path.GetFileName(loaclfile));
                    break;
            }
            //先创建文件夹
            Directory.CreateDirectory(Path.GetDirectoryName(loaclfile));
            ScriptsUtilities.print($"download {url} -> {loaclfile}");
            //如果存在
                using (WebClient web = new WebClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3 | (SecurityProtocolType)0x300 | (SecurityProtocolType)0xC00;
                    web.Proxy = null;
                    web.DownloadFile(url, loaclfile);
                }
        }

    }
}
