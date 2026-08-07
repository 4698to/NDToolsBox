using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;

namespace BusinessLib
{
    public static class WebAddress
    {
        public static string serverIP = "101.34.112.80";
        //public static string serverIP = "192.168.251.94";
        public static int Port = 8019;
        //公司内网
        //public static string serverName = $"http://{serverIP}:{Port}/download?fileid=";
        public static string serverName = $"sundaybox.cc/downloadfiles?fileid=";


        //服务器上的版本号文件，每次启动下载该文件检测是否有版本更新
        public static string serverVersion = "updateBox.txt";

        //服务上的脚本名单，仅仅是名单
        public static string scriptContent = "NDToolsList.xml";
        public static string dataModelFile = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\NDToolsList.xml";

        //更新程序 
        public static string UpdataExE = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\NDDownload.exe";
        //服务器上的内容列表
        public static string contentList = "Install_version_full.xml";
        public static string contentLocal = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\Install_version_full.xml";

        //本地的配置文件
        public static string iniConfig = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\config.ini";

        //根目录
        public static string apppath = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox";

        public static bool pingServer()
        {
            using (Ping pingsender = new Ping())
            {
                PingReply reply = pingsender.Send($"http://{serverIP}", 50);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }
        //检测内网的服务器是否正常
        public static bool pingIp()
        {
            return true;

            //return WebAddress.checkPortEnable(serverIP, Port);
            //return ping("sundaybox.cc");
        }
        /// <summary>
        /// telnet port 
        /// </summary>
        /// <param name="_ip"></param>
        /// <param name="_port"></param>
        /// <returns></returns>
        public static bool checkPortEnable(string _ip, int _port)
        {
            //将IP和端口替换成为你要检测的
            string ipAddress = _ip;
            int portNum = _port;
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint point = new IPEndPoint(ip, portNum);

            bool _portEnable = false;
            try
            {
                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(point);
                    //Console.WriteLine("连接{0}成功!", point);
                    sock.Close();

                    _portEnable = true;
                }
            }
            catch (SocketException e)
            {
                //Console.WriteLine("连接{0}失败", point);
                _portEnable = false;
            }
            return _portEnable;
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
        public static void downloadScriptList(string loaclfile)
        {
            string url = string.Concat(serverName, Path.GetFileName(loaclfile));
            //先创建文件夹
            Directory.CreateDirectory(Path.GetDirectoryName(loaclfile));
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
