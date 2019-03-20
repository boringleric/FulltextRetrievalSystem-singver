using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using log4net;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;


namespace CrawlPart
{
    /// <summary>
    /// 共享文件夹抓取
    /// </summary>
    public class ShareCrawl
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //载入预设的共享文件夹缓存路径
        private string ConfigValue = ConfigurationManager.AppSettings["LocalShareSaveAddr"].ToString();

        public string startip;  //起始ip
        public string endip;    //结束ip
        public bool flag = true;  //爬虫结束标志

        public Queue<string> QShareAdd = new Queue<string>();
        public Queue<string> QShareUpdate = new Queue<string>();
        public Queue<string> QShareDel = new Queue<string>();
        public Queue<string> QShareChk = new Queue<string>();

        //外部调用
        [DllImport("Netapi32.dll", SetLastError = true)]
        static extern int NetApiBufferFree(IntPtr Buffer);
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetShareEnum(
             StringBuilder ServerName,
             int level,
             ref IntPtr bufPtr,
             uint prefmaxlen,
             ref int entriesread,
             ref int totalentries,
             ref int resume_handle
             );
        //定义共享文件夹相关结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_1
        {
            public string shi1_netname;
            public uint shi1_type;
            public string shi1_remark;
            public SHARE_INFO_1(string sharename, uint sharetype, string remark)
            {
                this.shi1_netname = sharename;
                this.shi1_type = sharetype;
                this.shi1_remark = remark;
            }
            public override string ToString()
            {
                return shi1_netname;
            }
        }


        #region 从远程IP获取文件夹
        const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;
        const int NERR_Success = 0;
        private enum NetError : uint
        {
            NERR_Success = 0,
            NERR_BASE = 2100,
            NERR_UnknownDevDir = (NERR_BASE + 16),
            NERR_DuplicateShare = (NERR_BASE + 18),
            NERR_BufTooSmall = (NERR_BASE + 23),
        }
        private enum SHARE_TYPE : uint
        {
            STYPE_DISKTREE = 0,
            STYPE_PRINTQ = 1,
            STYPE_DEVICE = 2,
            STYPE_IPC = 3,
            STYPE_SPECIAL = 0x80000000,
        }
        /// <summary>
        /// 检查IP的共享文件夹
        /// </summary>
        /// <param name="Server">IP地址</param>
        /// <returns>共享文件夹目录</returns>
        static private ArrayList EnumNetShares(string Server)
        {
            // List<SHARE_INFO_1> ShareInfos = new List<SHARE_INFO_1>();
            ArrayList shareinfo = new ArrayList();
            int entriesread = 0;
            int totalentries = 0;
            int resume_handle = 0;
            int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_1));
            IntPtr bufPtr = IntPtr.Zero;
            StringBuilder server = new StringBuilder(Server);
            //使用NetShareEnum获取远程文件夹共享内容
            int ret = NetShareEnum(server, 1, ref bufPtr, MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
            if (ret == NERR_Success)
            {
                IntPtr currentPtr = bufPtr;
                for (int i = 0; i < entriesread; i++)
                {
                    SHARE_INFO_1 shi1 = (SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_1));
                    if (shi1.shi1_type == 0)//Disk drive类型
                    {
                        shareinfo.Add(shi1.shi1_netname);
                    }

                    //64位系统需要调用64位指针， 使用 ToInt32 会出错，使用ToInt64
                    currentPtr = new IntPtr(currentPtr.ToInt64() + nStructSize);
                }
                NetApiBufferFree(bufPtr);

                return shareinfo;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region IP与字符串转换
        /// <summary>
        /// 输入的ip地址转化为字符串类型
        /// </summary>
        /// <param name="ipCode">ip地址</param>
        /// <returns>ip地址的字符串格式</returns>
        private static string Int2IP(uint ipCode)
        {
            byte a = (byte)((ipCode & 0xFF000000) >> 0x18);
            byte b = (byte)((ipCode & 0x00FF0000) >> 0x10);
            byte c = (byte)((ipCode & 0x0000FF00) >> 0x8);
            byte d = (byte)(ipCode & 0x000000FF);
            string ipStr = string.Format("{0}.{1}.{2}.{3}", a, b, c, d);
            return ipStr;
        }
        /// <summary>
        /// 字符串转为ip地址类型
        /// </summary>
        /// <param name="ipStr">ip地址字符串格式</param>
        /// <returns>ip地址</returns>
        private static uint IP2Int(string ipStr)
        {
            string[] ip = ipStr.Split('.');
            uint ipCode = 0xFFFFFF00 | byte.Parse(ip[3]);
            ipCode = ipCode & 0xFFFF00FF | (uint.Parse(ip[2]) << 0x8);
            ipCode = ipCode & 0xFF00FFFF | (uint.Parse(ip[1]) << 0x10);
            ipCode = ipCode & 0x00FFFFFF | (uint.Parse(ip[0]) << 0x18);
            return ipCode;
        }
        #endregion

        #region 共享文件夹内容取回
        /// <summary>
        /// 测试IP是否在线
        /// </summary>
        /// <param name="ipaddr">IP地址</param>
        private bool TestIP(string ipaddr)
        {
            //设置超时时间  
            int timeout = 120;
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(ipaddr, timeout);   //使用ping来测试是否在线

            if (pingReply.Status == IPStatus.Success)
            {
                log.Info(ipaddr + "在线");
                return true;
                //Console.WriteLine("当前在线，已ping通！");
            }
            else
            {
                log.Error(ipaddr + "不在线");
                return false;
                //Console.WriteLine("不在线，ping不通！");
            }
        }
        /// <summary>
        /// 设置链接的默认格式，可自定义用户名密码
        /// </summary>
        /// <param name="path">共享文件夹所在的ip地址</param>
        /// <param name="userName">共享文件夹的用户名</param>
        /// <param name="passWord">共享文件夹的密码</param>
        /// <param name="connect">建立或释放连接</param>
        /// <returns>链接成功或者失败</returns>
        private static bool connectState(string path, string userName, string passWord, bool connect)
        {
            bool Flag = false;
            //建立进程
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";            //调用cmd
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string dosLine;
                //设置cmd链接方式
                if (connect)
                {
                    //构造建立连接的指令
                    dosLine = @"net use " + path + "ipc$ /User:" + userName + " " + passWord + " /PERSISTENT:YES";
                }
                else
                {
                    //构造释放连接的指令
                    dosLine = @"net use " + path + "ipc$ /del";
                }
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                int tmptime = 0;
                //若链接失败
                while (!proc.HasExited)
                {
                    //若三次链接失败就不再继续了
                    if (++tmptime >= 3)
                    {
                        log.Error("无法连接" + path);
                        return false;
                    }

                    proc.WaitForExit(500);
                }
                //获得链接错误信息
                string errormsg = proc.StandardError.ReadToEnd();
                proc.StandardError.Close();
                if (string.IsNullOrEmpty(errormsg))
                {
                    //没问题就不抛异常
                    Flag = true;
                }
                else
                {
                    log.Error(errormsg);
                    throw new Exception(errormsg);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                Console.WriteLine(ex.Message);
                throw ex;
            }
            finally
            {
                proc.Close();
                proc.Dispose();
            }
            return Flag;
        }
        /// <summary>
        /// 判断扩展名确定是否应该抓取文件
        /// </summary>
        /// <param name="extension">扩展名</param>
        /// <returns>是否在范围内</returns>
        private bool CheckExtension(string extension)
        {
            switch (extension)
            {
                case ".ppt":
                case ".pptx":
                case ".doc":
                case ".docx":
                case ".xls":
                case ".xlsx":
                case ".txt":
                case ".pdf":
                case ".html":
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// 检查文件，若是文件夹则递归调用之
        /// </summary>
        /// <param name="path">远程地址</param>
        /// <param name="strdir">本地地址</param>
        private void ListDirectory(string path, string strdir)
        {
            DirectoryInfo theFolder = new DirectoryInfo(path);
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                if (CheckExtension(NextFile.Extension))
                {
                    //检查文件是否在要解析的范围内
                    FileInfo flocinfo = new FileInfo(strdir + @"\" + NextFile.Name);
                    if (!File.Exists(strdir + @"\" + NextFile.Name) || flocinfo.Length != NextFile.Length) //不在本地或者长度不对
                    {
                        log.Info("下载路径：" + path + "文件名" + NextFile.Name);
                        Transport(NextFile.FullName, strdir + @"\", NextFile.Name); //调用传输函数
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    //不在解析范围内就新建同名文件保存文件长度信息
                    if (File.Exists(strdir + @"\" + NextFile.Name + @".rntf"))
                    {
                        //检查本地是否有同名文件，若有
                        StreamReader sr = new StreamReader(strdir + @"\" + NextFile.Name + @".rntf", Encoding.Default);
                        string line = sr.ReadLine().ToString();
                        if (NextFile.Length == long.Parse(line))
                        {
                            //检查一致
                            continue;
                        }
                        else
                        {
                            //若大小不一致，重新保存
                            sr.Close();
                            FileStream fs = new FileStream(strdir + @"\" + NextFile.Name + @".rntf", FileMode.Open, FileAccess.Write);
                            //清空文件!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            fs.SetLength(0);
                            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                            string writeString = NextFile.Length.ToString();
                            sw.WriteLine(writeString);
                            sw.Close(); //关闭文件
                            log.Info("更新文件" + strdir + @"\" + NextFile.Name + ".rntf");
                            QShareUpdate.Enqueue(strdir + @"\" + NextFile.Name + ".rntf");  //加入更新队列
                        }
                    }
                    else
                    {
                        //若本地没有，则新建文件并写入相关信息
                        FileStream fs = new FileStream(strdir + @"\" + NextFile.Name + ".rntf", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.WriteLine(NextFile.Length.ToString());
                        sw.Close(); //关闭文件
                        log.Info("新增文件" + strdir + @"\" + NextFile.Name + ".rntf");
                        QShareAdd.Enqueue(strdir + @"\" + NextFile.Name + ".rntf"); //加入新增队列
                    }
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                string strtmp = strdir + @"\" + NextFolder.Name;
                if (Directory.Exists(strtmp) == false)//如果不存在就创建file文件夹
                {
                    Directory.CreateDirectory(strtmp);
                }
                ListDirectory(NextFolder.FullName, strtmp); //递归检查文件夹
            }
        }
        /// <summary>
        /// 向远程文件夹保存本地内容，或者从远程文件夹下载文件到本地
        /// </summary>
        /// <param name="src">要保存的文件的路径，如果保存文件到共享文件夹，这个路径就是本地文件路径如：@"D:\1.avi"</param>
        /// <param name="dst">保存文件的路径，不含名称及扩展名</param>
        /// <param name="fileName">保存文件的名称以及扩展名</param>
        private void Transport(string src, string dst, string fileName)
        {
            FileStream inFileStream = new FileStream(src, FileMode.Open);
            bool flag = false;
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }
            dst = dst + fileName;   //构造完整的文件路径
            if (File.Exists(dst))
            {
                //若文件存在就清空文件
                flag = true;
                FileStream fs = new FileStream(dst, FileMode.Open, FileAccess.Write);
                //清空文件!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                fs.SetLength(0);
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine();
                sw.Close(); //关闭文件
            }
            //传输文件
            FileStream outFileStream = new FileStream(dst, FileMode.OpenOrCreate);
            byte[] buf = new byte[inFileStream.Length];
            int byteCount;

            while ((byteCount = inFileStream.Read(buf, 0, buf.Length)) > 0)
            {
                outFileStream.Write(buf, 0, byteCount);
            }
            log.Info("传输文件" + dst);
            inFileStream.Flush();
            inFileStream.Close();
            outFileStream.Flush();
            outFileStream.Close();
            if (flag)
            {
                QShareUpdate.Enqueue(dst);  //加入更新文件队列
            }
            else
            {
                QShareAdd.Enqueue(dst);  //加入新增文件队列
            }
        }
        #endregion

        #region 开始共享文件夹爬虫

        /// <summary>
        /// 开始共享文件夹爬虫(单IP)
        /// </summary>
        /// <param name="sipaddr">ip</param>
        public void StartCrawl(string ipaddr)
        {
            log.Info("开始爬虫，当前IP" + ipaddr);
            bool status = TestIP(ipaddr);                 //判断是否可以链接
            if (status)
            {
                try
                {
                    //创建连接
                    bool ret = connectState(@"\\" + ipaddr + @"\", "everyone", "", true);
                    log.Info("建立连接" + ipaddr);
                    ArrayList alShare = EnumNetShares(@"\\" + ipaddr + @"\");   //获得该ip的共享文件夹列表
                    string[] ShareList = new string[alShare.Count];
                    for (int j = 0; j < alShare.Count; j++)
                    {
                        ShareList[j] = alShare[j].ToString();
                    }
                    if (ShareList != null)
                    {
                        foreach (var item in ShareList)
                        {
                            string straim = @"\\" + ipaddr + @"\" + item;    //拼接ip地址字符串

                            DirectoryInfo theFolder = new DirectoryInfo(straim);
                            string locallink = ConfigValue;
                            string strdir = locallink + ipaddr + @"\" + item;

                            if (Directory.Exists(strdir) == false)          //如果不存在就创建file文件夹
                            {
                                Directory.CreateDirectory(strdir);
                                log.Info("创建文件夹" + strdir);
                            }
                            //遍历共享文件夹，把共享文件夹下的文件取回
                            ListDirectory(straim, strdir);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error("StartCrawl错误，" + e.Message);
                    Console.WriteLine(e.Message);
                }
                bool reles = connectState(@"\\" + ipaddr + @"\", "everyone", "", false);
            }
        }
        /// <summary>
        /// 开始共享文件夹爬虫(IP段)
        /// </summary>
        public void StartCrawl()
        {
            string ipaddr;
            uint lstartip = IP2Int(startip);
            uint lendip = IP2Int(endip);
            for (uint i = lstartip; i <= lendip; i++)       //遍历ip，依次开始爬虫
            {
                ipaddr = Int2IP(i);                         //转换ip
                log.Info("开始爬虫，当前IP" + ipaddr);
                bool status = TestIP(ipaddr);                 //判断是否可以链接
                if (status)
                {
                    try
                    {
                        bool ret = connectState(@"\\" + ipaddr + @"\", "everyone", "", true);
                        log.Info("建立连接" + ipaddr);
                        ArrayList alShare = EnumNetShares(@"\\" + ipaddr + @"\");   //获得该ip的共享文件夹列表
                        string[] ShareList = new string[alShare.Count];
                        for (int j = 0; j < alShare.Count; j++)
                        {
                            ShareList[j] = alShare[j].ToString();
                        }
                        if (ShareList != null)
                        {
                            foreach (var item in ShareList)
                            {
                                string straim = @"\\" + ipaddr + @"\" + item;    //拼接ip地址字符串

                                DirectoryInfo theFolder = new DirectoryInfo(straim);
                                string locallink = ConfigValue;
                                string strdir = locallink + ipaddr + @"\" + item;

                                if (Directory.Exists(strdir) == false)          //如果不存在就创建file文件夹
                                {
                                    Directory.CreateDirectory(strdir);
                                    log.Info("创建文件夹" + strdir);
                                }
                                //遍历共享文件夹，把共享文件夹下的文件取回
                                ListDirectory(straim, strdir);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error("StartCrawl错误，" + e.Message);
                        Console.WriteLine(e.Message);
                        flag = false;
                    }
                    //释放连接
                    bool reles = connectState(@"\\" + ipaddr + @"\", "everyone", "", false);
                }
            }
            flag = false;
        }
        #endregion

        #region 检查远程文件夹并删除本地
        /// <summary>
        /// 删除该文件夹所有内容
        /// </summary>
        /// <param name="path">文件夹路径</param>
        private void DelAllFile(string path)
        {
            DirectoryInfo theFolder = new DirectoryInfo(path);
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                string name = NextFile.FullName;
                File.Delete(name);
                log.Info("删除文件" + name);
                QShareDel.Enqueue(name);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                DelAllFile(NextFolder.FullName); //递归检查文件夹
            }
            Directory.Delete(path);
            log.Info("删除文件夹" + path);
        }
        /// <summary>
        /// 检查文件是否存在，若是文件夹则递归调用之
        /// </summary>
        /// <param name="path">远程地址</param>
        /// <param name="strdir">本地地址</param>
        private void CheckDirectory(string strdir)
        {
            DirectoryInfo theFolder = new DirectoryInfo(strdir);
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                string localdir = ConfigValue;    //获得本地的文件夹们
                string name;
                string filexten = Path.GetExtension(NextFile.Name);
                if (filexten == ".rntf")
                {
                    name = NextFile.FullName;
                    name = name.Replace(".rntf", "");
                }
                else
                {
                    name = NextFile.FullName;
                }
                string remote = name.Replace(localdir, @"\\");
                if (File.Exists(remote))
                {
                    continue;
                }
                else
                {
                    File.Delete(name);
                    log.Info("删除文件" + name);
                    QShareDel.Enqueue(name);
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                string localdir = ConfigValue;    //获得本地的文件夹们
                string name = NextFolder.FullName;
                string remote = name.Replace(localdir, @"\\");
                if (Directory.Exists(remote) == false)//如果不存在就以文件夹为单位删除
                {
                    DelAllFile(name);
                }
                else
                {
                    CheckDirectory(NextFolder.FullName); //递归检查文件夹
                }
            }
        }
        /// <summary>
        /// 检查是否删除
        /// </summary>
        public void CheckExist()
        {
            string ipaddr;
            uint lstartip = IP2Int(startip);
            uint lendip = IP2Int(endip);
            for (uint i = lstartip; i <= lendip; i++)   //遍历ip地址
            {
                ipaddr = Int2IP(i);
                CheckExist(ipaddr); //调用文件检查函数检查是否存在
            }
            flag = false;
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="ipaddr">ip地址</param>
        private void CheckExist(string ipaddr)
        {
            string localdir = ConfigValue;    //获得本地的文件夹们
            DirectoryInfo theFolder = new DirectoryInfo(localdir + ipaddr + @"\");
            if (theFolder.Exists == false)
            {
                return;
            }
            log.Info("开始检查删除,当前IP" + ipaddr);
            bool status = TestIP(ipaddr);                 //判断是否可以链接
            if (status)
            {
                //建立连接
                bool ret = connectState(@"\\" + ipaddr + @"\", "everyone", "", true);
                //遍历文件夹
                foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
                {
                    string name = NextFolder.FullName;
                    string remote = name.Replace(localdir, @"\\");
                    if (!Directory.Exists(remote))
                    {
                        //若远程文件夹不存在，则全部删除
                        DelAllFile(name);
                    }
                    else
                    {
                        //否则递归检查文件夹
                        CheckDirectory(NextFolder.FullName);
                    }
                }
                //释放连接
                bool reles = connectState(@"\\" + ipaddr + @"\", "everyone", "", false);
            }
        }
        #endregion

        #region 共享文件夹本地缓存与数据库检查
        /// <summary>
        /// 递归检查本地文件夹
        /// </summary>
        /// <param name="strdir">文件夹路径</param>
        private void CheckDirectoryforreg(string strdir)
        {
            DirectoryInfo theFolder = new DirectoryInfo(strdir);
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                //加入检查队列
                QShareChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //空文件夹删之
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空share文件夹" + NextFolder.FullName);
                    Directory.Delete(NextFolder.FullName);
                    continue;
                }
                else
                {
                    CheckDirectoryforreg(NextFolder.FullName);         //递归检查
                }
            }
        }
        /// <summary>
        /// 检查本地是否有未索引的
        /// </summary>
        public void Check()
        {
            string localdir = ConfigValue;    //获得本地的文件夹们
            DirectoryInfo theFolder = new DirectoryInfo(localdir);
            if (theFolder.Exists == false)
            {
                return;
            }
            log.Info("开始检查共享文件夹同步");

            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                //加入检查队列
                QShareChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //空文件夹删之
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空share文件夹" + NextFolder.FullName);
                    Directory.Delete(NextFolder.FullName);
                    continue;
                }
                else
                {
                    CheckDirectoryforreg(NextFolder.FullName);  //调用文件夹删除检查
                }
            }
            flag = false;
        }
        #endregion

    }
}
