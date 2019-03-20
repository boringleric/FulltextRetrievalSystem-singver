using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using WinSCP;


namespace CrawlPart
{
    /// <summary>
    /// Ftp爬取Winscp控制类
    /// </summary>
    public class FtpCrawl
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //载入预设的Ftp缓存路径
        private string ConfigValue = ConfigurationManager.AppSettings["LocalFtpSaveAddr"].ToString();
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalCrawlConfig"].ToString();
        //设置临时变量
        private static List<string> str;
        private static List<string> strfinal = new List<string>();  //缓存文件夹名
        public string Host;     //主机IP地址
        public string User;     //登录用户名
        public string Psw;      //登录密码
        public bool flag = true;  //爬虫结束标志

        public Queue<string> QFtpAdd = new Queue<string>();
        public Queue<string> QFtpUpdate = new Queue<string>();
        public Queue<string> QFtpDel = new Queue<string>();
        public Queue<string> QFtpChk = new Queue<string>();

        #region Ftp下载内容检查及下载
        /// <summary>
        /// 检查扩展名，选择会解析的下载
        /// </summary>
        /// <param name="extension">扩展名</param>
        /// <returns>返回是否解析</returns>
        private bool checkextension(string extension)
        {
            //检查扩展名
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
        /// 检查文件夹是否在禁用列表里
        /// </summary>
        /// <param name="dirname">文件夹名称</param>
        /// <returns>是否包含</returns>
        private bool checkbanlist(string dirname)
        {
            if (strfinal.Contains(dirname))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 下载选择的内容
        /// </summary>
        /// <param name="session">初始session</param>
        /// <param name="dir">远程路径</param>
        /// <param name="localdir">本地路径</param>
        private void GetFilesSelected(Session session, string dir, string localdir)
        {
            try
            {
                RemoteDirectoryInfo directory = session.ListDirectory(dir);
                //对每一个文件做判断，如果是文件夹则进行递归（内存问题考虑！！！）
                foreach (RemoteFileInfo fileInfo in directory.Files)
                {
                    //如果是上一级链接不考虑
                    if (fileInfo.IsParentDirectory || checkbanlist(fileInfo.FullName))
                    {
                        continue;
                    }
                    //判断是否为文件
                    switch (fileInfo.FileType)
                    {
                        //文件夹就进行递归
                        case 'D':
                            GetFilesSelected(session, fileInfo.FullName, localdir);
                            break;
                        //文件会选择提取
                        default:
                            string strdirtmp = localdir + dir;
                            //取出扩展名
                            string filexten = Path.GetExtension(fileInfo.Name);
                            //若不在扩展范围就下载同名处理
                            if (checkextension(filexten))
                            {
                                if (Directory.Exists(strdirtmp) == false)//如果不存在就创建file文件夹
                                {
                                    Directory.CreateDirectory(strdirtmp);
                                }

                                //若不存在就下载下来文件
                                if (!File.Exists(strdirtmp + @"\" + fileInfo.Name))
                                {
                                    TransferOptions transferOptions = new TransferOptions();
                                    transferOptions.TransferMode = TransferMode.Automatic;
                                    TransferOperationResult transferResult;
                                    //组带有文件名的路径
                                    //string strtmp = dir + @"/" + fileInfo.Name;
                                    //transferResult = session.GetFiles(strtmp, strdirtmp + @"\", false, transferOptions);    //传输文件
                                    transferResult = session.GetFiles(session.EscapeFileMask(fileInfo.FullName), strdirtmp + @"\", false, transferOptions);    //传输文件

                                    string correction = strdirtmp.Replace("/", "\\");   //统一"/"为一个格式，避免后期检索路径错误
                                    Console.WriteLine("新增文件" + correction + @"\" + fileInfo.Name);
                                    log.Info("新增文件" + correction + @"\" + fileInfo.Name); //打日志
                                    QFtpAdd.Enqueue(correction + @"\" + fileInfo.Name); //加入新增队列

                                    transferResult.Check();
                                }
                                else
                                {
                                    //若存在本地
                                    FileInfo flocinfo = new FileInfo(strdirtmp + @"\" + fileInfo.Name);
                                    if (flocinfo.Length != fileInfo.Length) //检查文件长度判断是否需要重新下载
                                    {
                                        TransferOptions transferOptions = new TransferOptions();
                                        transferOptions.TransferMode = TransferMode.Automatic;
                                        TransferOperationResult transferResult;
                                        //若需要就重新下载
                                        string strtmp = dir + @"/" + fileInfo.Name;
                                        transferResult = session.GetFiles(strtmp, strdirtmp + @"\", false, transferOptions);
                                        string correction = strdirtmp.Replace("/", "\\");
                                        log.Info("更新文件" + correction + @"\" + fileInfo.Name);
                                        QFtpUpdate.Enqueue(correction + @"\" + fileInfo.Name); //加入更新队列

                                        transferResult.Check();
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                //不在解析范围，只需要创建一个同名文件，而且内容是源文件的长度
                                if (Directory.Exists(strdirtmp) == false)//如果不存在就创建file文件夹
                                {
                                    Directory.CreateDirectory(strdirtmp);
                                }

                                if (File.Exists(strdirtmp + @"\" + fileInfo.Name + @".rntf"))
                                {
                                    //若文件存在，检查是否一致
                                    StreamReader sr = new StreamReader(strdirtmp + @"\" + fileInfo.Name + @".rntf", Encoding.Default);
                                    string line = sr.ReadLine().ToString();
                                    if (fileInfo.Length == long.Parse(line))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        //信息不一致，重置信息
                                        sr.Close();
                                        FileStream fs = new FileStream(strdirtmp + @"\" + fileInfo.Name + @".rntf", FileMode.Open, FileAccess.Write);
                                        //清空文件!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                        fs.SetLength(0);
                                        StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                                        string writeString = fileInfo.Length.ToString();
                                        sw.WriteLine(writeString);
                                        sw.Close(); //关闭文件
                                        string correction = strdirtmp.Replace("/", "\\");

                                        log.Info("更新文件" + correction + @"\" + fileInfo.Name + ".rntf"); //打更新日志
                                        QFtpUpdate.Enqueue(correction + @"\" + fileInfo.Name + ".rntf");  //加入更新队列
                                    }
                                }
                                else
                                {
                                    //若文件不存在，新建文件
                                    FileStream fs = new FileStream(strdirtmp + @"\" + fileInfo.Name + ".rntf", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                    StreamWriter sw = new StreamWriter(fs);
                                    sw.WriteLine(fileInfo.Length.ToString());   //写入远程文件大小
                                    sw.Close(); //关闭文件
                                    string correction = strdirtmp.Replace("/", "\\");
                                    Console.WriteLine("新增文件" + correction + @"\" + fileInfo.Name + ".rntf");
                                    log.Info("新增文件" + correction + @"\" + fileInfo.Name + ".rntf");
                                    QFtpAdd.Enqueue(correction + @"\" + fileInfo.Name + ".rntf"); //加入新增队列
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Ftp错误：" + e.Message);
                Console.WriteLine("Ftp错误：" + e.Message);
            }
        }

        /// <summary>
        /// 设置Ftp下载，调用递归下载函数
        /// </summary>
        /// <param name="Host">Ftp地址</param>
        /// <param name="User">用户名</param>
        /// <param name="Psw">密码</param>
        public void StartCrawl()
        {
            try
            {
                // 开始建立session
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = Host,
                    UserName = User,
                    Password = Psw,
                };

                using (Session session = new Session())
                {
                    session.ReconnectTimeInMilliseconds = 5;
                    session.Open(sessionOptions);
                    log.Info("Ftp建立连接：" + Host + @" " + User);
                    //在xml中读取禁用文件夹配置信息
                    XmlOperation xo = new XmlOperation();
                    xo.ShowBanXml(CrawlConfigValue, out str);
                    foreach (var item in str)
                    {
                        //取出并依次加入禁用列表缓存
                        string stra = item.Replace(Host, "");
                        strfinal.Add(stra);
                    }
                    string getdir = ConfigValue;
                    string localdir = getdir + Host;
                    if (Directory.Exists(localdir) == false)//如果不存在就创建file文件夹
                    {
                        Directory.CreateDirectory(localdir);
                    }
                    //执行取文件函数
                    GetFilesSelected(session, "/", localdir);

                }
                flag = false;
            }
            catch (Exception e)
            {
                log.Error("Ftp错误：" + e.Message);
                Console.WriteLine("Error: {0}", e);
                flag = false;
            }
        }

        #endregion

        #region 删除文件检查
        /// <summary>
        /// 检查文件夹是否含有要被删的
        /// </summary>
        /// <param name="strdir">文件夹本地路径</param>
        /// <param name="session">远程Session</param>
        /// <param name="Host">Ftp地址</param>
        private void CheckDirectory(string strdir, Session session, string Host)
        {
            DirectoryInfo theFolder = new DirectoryInfo(strdir);
            string localdir = ConfigValue;    //获得本地的文件夹们
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                string name, remote, finalstr;
                string filexten = Path.GetExtension(NextFile.Name); //扩展名检查
                if (filexten == ".rntf")
                {
                    name = NextFile.FullName;
                    remote = name.Replace(localdir + Host, "");
                    finalstr = remote.Replace(@"\", @"/");
                    finalstr = finalstr.Replace(".rntf", "");
                }
                else
                {
                    name = NextFile.FullName;
                    remote = name.Replace(localdir + Host, "");
                    finalstr = remote.Replace(@"\", @"/");
                }
                //统一文件名和路径名，到远程服务器检测是否存在
                if (session.FileExists(finalstr))
                {
                    continue;   //存在就不用管
                }
                else
                {
                    //不存在删除本地文件
                    File.Delete(name);
                    log.Info("删除Ftp文件" + name);
                    QFtpDel.Enqueue(name);
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //若文件夹已经是空文件夹，删除该文件夹
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空Ftp文件夹" + NextFolder.FullName);
                    Directory.Delete(NextFolder.FullName);
                    continue;
                }
                else
                {
                    CheckDirectory(NextFolder.FullName, session, Host);         //递归检查
                }
            }
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        /// <param name="Host">Ftp地址</param>
        /// <param name="User">用户名</param>
        /// <param name="Psw">密码</param>
        public void CheckExist()
        {
            try
            {
                // 创建session
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = Host,
                    UserName = User,
                    Password = Psw,
                };

                using (Session session = new Session())
                {
                    session.Open(sessionOptions);
                    string localdir = ConfigValue;    //获得本地的文件夹们
                    DirectoryInfo theFolder = new DirectoryInfo(localdir + Host + @"\");
                    log.Info("Ftp建立连接：" + Host + @" " + User);
                    //获取文件夹内的文件
                    foreach (FileInfo NextFile in theFolder.GetFiles())
                    {
                        string name, remote, finalstr;
                        string filexten = Path.GetExtension(NextFile.Name); //检查扩展名
                        if (filexten == ".rntf")
                        {
                            name = NextFile.FullName;
                            remote = name.Replace(localdir + Host, "");
                            finalstr = remote.Replace(@"\", @"/");
                            finalstr = finalstr.Replace(".rntf", "");
                        }
                        else
                        {
                            name = NextFile.FullName;
                            remote = name.Replace(localdir + Host, "");
                            finalstr = remote.Replace(@"\", @"/");
                        }
                        //在统一链接之后检查远程文件是否存在
                        if (session.FileExists(finalstr))
                        {
                            continue;
                        }
                        else
                        {
                            //不存在则删除
                            QFtpDel.Enqueue(name);
                            File.Delete(name);
                            log.Info("删除Ftp文件" + name);

                        }
                    }
                    //遍历文件夹
                    foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
                    {
                        if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                        {
                            log.Info("删除空Ftp文件夹" + NextFolder.FullName);
                            Directory.Delete(NextFolder.FullName);
                            continue;
                        }
                        else
                        {
                            CheckDirectory(NextFolder.FullName, session, Host);
                        }
                    }
                    flag = false;
                }
            }
            catch (Exception e)
            {
                log.Error("Ftp错误：" + e.Message);
                Console.WriteLine("Error: {0}", e);
                flag = false;
            }
        }
        #endregion

        #region 本地未索引文件检查
        /// <summary>
        /// 检查本地文件夹
        /// </summary>
        /// <param name="strdir">文件夹路径</param>
        private void CheckDirectory(string strdir)
        {
            DirectoryInfo theFolder = new DirectoryInfo(strdir);
            //遍历文件
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                //将文件加入检查队列
                QFtpChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    //若是空文件夹，删除
                    log.Info("删除空Ftp文件夹" + NextFolder.FullName);
                    Directory.Delete(NextFolder.FullName);
                    continue;
                }
                else
                {
                    CheckDirectory(NextFolder.FullName);         //递归检查
                }
            }
        }
        /// <summary>
        /// 检查本地是否有未索引的文件
        /// </summary>
        public void Check()
        {
            string localdir = ConfigValue;    //获得本地的文件夹们
            DirectoryInfo theFolder = new DirectoryInfo(localdir + Host + @"\");
            if (theFolder.Exists == false)
            {
                return;
            }
            log.Info("Ftp文件检查：" + Host + @" " + User);

            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                //将文件加入检查队列
                QFtpChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //空文件夹删除
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空Ftp文件夹" + NextFolder.FullName);
                    Directory.Delete(NextFolder.FullName);
                    continue;
                }
                else
                {
                    CheckDirectory(NextFolder.FullName);
                }
            }
            flag = false;
        }
        #endregion

    }
}
