using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using ManagedServices;

namespace NDToolsBox
{
    public static class MaxComm
    {
        public static IGlobal global
        {
            get { return GlobalInterface.Instance; }
        }
        static IInterface13 ip
        {
            get{ return global.COREInterface13; }
        }
        public static void print(string str)
        {
            global.TheListener.EditStream.Printf(string.Format("{0}\n", str));
            global.TheListener.EditStream.Flush();
        }
        
        /// <summary>
        /// 3ds Max 安装根目录。对应 Max SDK MaxDirectory.MaxSysRootDir（索引 20）。
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
    }
}
