using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CrawlPart
{
    /// <summary>
    /// xml操作类
    /// </summary>
    public class XmlOperation
    {
        public struct WebStruct
        {
            public int NetCount;
            public string NickName;
            public string Link;
        }

        public struct FtpStruct
        {
            public int FtpCount;
            public string NickName;
            public string Link;
            public string UserName;
            public string UserPassword;
        }

        public struct ShareStruct
        {
            public int ShareCount;
            public string NickName;
            public string StartIP;
            public string EndIP;
        }

        #region 初始化
        /// <summary>
        /// 爬虫相关信息存在config当中，需要初始化config
        /// </summary>
        /// <param name="path">config文件路径</param>
        public void InitialConfig(string path)
        {
            //判断是否存在文件
            if (File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length == 0)
                {
                    //存在但没有内容就初始化设置
                    try
                    {
                        var xDoc = new XDocument(new XElement("Crawl",
                                               new XElement("Web"),
                                               new XElement("Ftp"),
                                               new XElement("Share")));

                        //默认是缩进格式化的xml，而无须格式化设置
                        xDoc.Save(path);
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e);
                    }
                }

            }
            else
            {
                //不存在就初始化设置
                try
                {
                    var xDoc = new XDocument(new XElement("Crawl",
                                           new XElement("Web"),
                                           new XElement("Ftp"),
                                           new XElement("Share")));

                    //默认是缩进格式化的xml，而无须格式化设置
                    xDoc.Save(path);
                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                }
            }
        }
        #endregion

        #region 插入Net节点
        /// <summary>
        /// 插入Net节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="NickName">代号</param>
        /// <param name="Link">链接</param>
        /// <returns></returns>
        public int InsertWebNode(string path, string NickName, string Link)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Net")
                                                    select target;

                XElement node1 = targetNodes.LastOrDefault();
                if (node1 == null)
                {
                    //无数据插入
                    XElement newNode = new XElement("Net", new XAttribute("NickName", NickName),
                        new XElement("NetCount", 1),
                        new XElement("Link", Link));
                    rootNode.Element("Web").Add(newNode);
                }
                else
                {
                    //有数据新增
                    XElement a = (XElement)node1.FirstNode;
                    int value = int.Parse(a.Value);
                    XElement newNode = new XElement("Net", new XAttribute("NickName", NickName),
                        new XElement("NetCount", ++value),
                        new XElement("Link", Link));
                    rootNode.Element("Web").Add(newNode);
                }
                //保存节点
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 修改Net节点
        /// <summary>
        /// 修改Net节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="Link">爬虫根路径</param>
        /// <param name="NickName">路径代号</param>
        /// <returns></returns>
        public int UpdateWebNode(string path, int NetCount, string NickName, string Link)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Net")
                                                    where target.Element("NetCount").Value == NetCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();
                //更新数据
                node.Attribute("NickName").SetValue(NickName);
                node.Element("Link").SetValue(Link);
                //保存
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        #endregion

        #region 显示Net数据
        /// <summary>
        /// 显示Net全部数据
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes">节点信息</param>
        /// <returns></returns>
        public int ShowWebXml(string path, out List<WebStruct> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("Net")
                                              select target;
                targetNodes = new List<WebStruct>();
                foreach (var item in Nodes)
                {
                    //取值并重置
                    WebStruct ns = new WebStruct();
                    string Link = item.Element("Link").ToString();
                    Link = Link.Replace("</Link>", "");
                    Link = Link.Replace("<Link>", "");
                    ns.Link = Link;

                    string NetCount = item.Element("NetCount").ToString();
                    NetCount = NetCount.Replace("</NetCount>", "");
                    NetCount = NetCount.Replace("<NetCount>", "");
                    ns.NetCount = int.Parse(NetCount);

                    string NickName = item.Attribute("NickName").ToString();
                    NickName = NickName.Replace("\"", "");
                    NickName = NickName.Replace("NickName=", "");
                    ns.NickName = NickName;

                    targetNodes.Add(ns);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        /// <summary>
        /// 显示节点部分信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="NetCount">指定节点</param>
        /// <param name="NickName">link代号</param>
        /// <param name="Link">web链接</param>
        /// <returns></returns>
        public int ShowWebXml(string path, int NetCount, out string NickName, out string Link)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Net")
                                                    where target.Element("NetCount").Value == NetCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                Link = node.Element("Link").ToString();
                Link = Link.Replace("</Link>", "");
                Link = Link.Replace("<Link>", "");

                NickName = node.Attribute("NickName").ToString();
                NickName = NickName.Replace("\"", "");
                NickName = NickName.Replace("NickName=", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        #endregion

        #region 删除Net节点
        /// <summary>
        /// 删除Net节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="NetCount">Net节点号</param>
        /// <returns></returns>
        public int DeleteWebNode(string path, int NetCount)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Net")
                                                    where target.Element("NetCount").Value == NetCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                //将获得的节点集合中的每一个节点依次从它相应的父节点中删除
                targetNodes.Remove();
                //保存对xml的更改操作
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 插入Ftp节点
        /// <summary>
        /// 插入Ftp节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="Nickname">Ftp代号</param>
        /// <param name="Link">Ftp路径</param>
        /// <param name="UserName">Ftp用户名</param>
        /// <param name="UserPassword">Ftp密码</param>
        /// <returns></returns>
        public int InsertFtpNode(string path, string Nickname, string Link, string UserName, string UserPassword)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Ftp子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("FtpFile")
                                                    select target;

                XElement node1 = targetNodes.LastOrDefault();
                if (node1 == null)
                {
                    XElement newNode = new XElement("FtpFile", new XAttribute("NickName", Nickname),
                        new XElement("FtpCount", 1),
                        new XElement("Link", Link),
                        new XElement("UserName", UserName),
                        new XElement("UserPassword", UserPassword));
                    rootNode.Element("Ftp").Add(newNode);
                }
                else
                {
                    XElement a = (XElement)node1.FirstNode;
                    int value = int.Parse(a.Value);
                    XElement newNode = new XElement("FtpFile", new XAttribute("NickName", Nickname),
                        new XElement("FtpCount", ++value),
                        new XElement("Link", Link),
                        new XElement("UserName", UserName),
                        new XElement("UserPassword", UserPassword));
                    rootNode.Element("Ftp").Add(newNode);
                }
                //保存节点
                rootNode.Save(path);
            }
            catch (Exception)
            {

                throw;
            }
            return 1;
        }
        #endregion

        #region 修改Ftp节点
        /// <summary>
        /// 更新Ftp信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="FtpCount">Ftp计数</param>
        /// <param name="NickName">Ftp代号</param>
        /// <param name="Link">Ftp链接</param>
        /// <param name="UserName">Ftp用户名</param>
        /// <param name="UserPassword">Ftp密码</param>
        /// <returns></returns>
        public int UpdateFtpNode(string path, int FtpCount, string NickName, string Link, string UserName, string UserPassword)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("FtpFile")
                                                    where target.Element("FtpCount").Value == FtpCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();
                //更新数据
                node.Attribute("NickName").SetValue(NickName);
                node.Element("Link").SetValue(Link);
                node.Element("UserName").SetValue(UserName);
                node.Element("UserPassword").SetValue(UserPassword);
                //保存
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 显示Ftp数据
        /// <summary>
        /// 显示Ftp全部数据
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="targetNodes">返回信息</param>
        /// <returns></returns>
        public int ShowFtpXml(string path, out List<FtpStruct> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("FtpFile")
                                              select target;
                targetNodes = new List<FtpStruct>();
                foreach (var item in Nodes)
                {
                    //取值并重置
                    FtpStruct ns = new FtpStruct();
                    string Link = item.Element("Link").ToString();
                    Link = Link.Replace("</Link>", "");
                    Link = Link.Replace("<Link>", "");
                    ns.Link = Link;

                    string UserName = item.Element("UserName").ToString();
                    UserName = UserName.Replace("</UserName>", "");
                    UserName = UserName.Replace("<UserName>", "");
                    ns.UserName = UserName;

                    string UserPassword = item.Element("UserPassword").ToString();
                    UserPassword = UserPassword.Replace("</UserPassword>", "");
                    UserPassword = UserPassword.Replace("<UserPassword>", "");
                    ns.UserPassword = UserPassword;

                    string FtpCount = item.Element("FtpCount").ToString();
                    FtpCount = FtpCount.Replace("</FtpCount>", "");
                    FtpCount = FtpCount.Replace("<FtpCount>", "");
                    ns.FtpCount = int.Parse(FtpCount);

                    string NickName = item.Attribute("NickName").ToString();
                    NickName = NickName.Replace("\"", "");
                    NickName = NickName.Replace("NickName=", "");
                    ns.NickName = NickName;

                    targetNodes.Add(ns);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        /// <summary>
        /// 返回指定节点的信息
        /// </summary>
        /// <param name="path">config位置</param>
        /// <param name="FtpCount">指定的Ftp节点</param>
        /// <param name="Link">Ftp链接</param>
        /// <param name="NickName">Ftp代号</param>
        /// <param name="UserName">Ftp用户名</param>
        /// <param name="UserPassword">Ftp密码</param>
        public int ShowFtpXml(string path, int FtpCount, out string Link, out string NickName, out string UserName, out string UserPassword)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("FtpFile")
                                                    where target.Element("FtpCount").Value == FtpCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                Link = node.Element("Link").ToString();
                Link = Link.Replace("</Link>", "");
                Link = Link.Replace("<Link>", "");

                UserName = node.Element("UserName").ToString();
                UserName = UserName.Replace("</UserName>", "");
                UserName = UserName.Replace("<UserName>", "");

                UserPassword = node.Element("UserPassword").ToString();
                UserPassword = UserPassword.Replace("</UserPassword>", "");
                UserPassword = UserPassword.Replace("<UserPassword>", "");

                NickName = node.Attribute("NickName").ToString();
                NickName = NickName.Replace("\"", "");
                NickName = NickName.Replace("NickName=", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 删除Ftp节点
        /// <summary>
        /// 删除Ftp节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="FtpCount">Ftp代号</param>
        /// <returns></returns>
        public int DeleteFtpNode(string path, int FtpCount)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("FtpFile")
                                                    where target.Element("FtpCount").Value == FtpCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                //将获得的节点集合中的每一个节点依次从它相应的父节点中删除
                targetNodes.Remove();
                //保存对xml的更改操作
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 插入Share节点
        /// <summary>
        /// 插入Share节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="Nickname">IP代号</param>
        /// <param name="StartIP">起始IP</param>
        /// <param name="EndIP">终止IP</param>
        /// <returns></returns>
        public int InsertShareNode(string path, string Nickname, string StartIP, string EndIP)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Ftp子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("ShareFile")
                                                    select target;

                XElement node1 = targetNodes.LastOrDefault();
                if (node1 == null)
                {
                    XElement newNode = new XElement("ShareFile", new XAttribute("NickName", Nickname),
                        new XElement("ShareCount", 1),
                        new XElement("StartIP", StartIP),
                        new XElement("EndIP", EndIP));
                    rootNode.Element("Share").Add(newNode);
                }
                else
                {
                    XElement a = (XElement)node1.FirstNode;
                    int value = int.Parse(a.Value);
                    XElement newNode = new XElement("ShareFile", new XAttribute("NickName", Nickname),
                        new XElement("ShareCount", ++value),
                        new XElement("StartIP", StartIP),
                        new XElement("EndIP", EndIP));
                    rootNode.Element("Share").Add(newNode);
                }
                //保存节点
                rootNode.Save(path);
            }
            catch (Exception)
            {

                throw;
            }
            return 1;
        }
        #endregion

        #region 修改Share节点
        /// <summary>
        /// 修改Share节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="StartIP">起始IP</param>
        /// <param name="EndIP">终止IP</param>
        /// <param name="NickName">IP代号</param>
        /// <returns></returns>
        public int UpdateShareNode(string path, int ShareCount, string NickName, string StartIP, string EndIP)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("ShareFile")
                                                    where target.Element("ShareCount").Value == ShareCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();
                //更新数据
                node.Attribute("NickName").SetValue(NickName);
                node.Element("StartIP").SetValue(StartIP);
                node.Element("EndIP").SetValue(EndIP);
                //保存
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 显示Share数据
        /// <summary>
        /// 显示Share数据
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes">返回的节点数据</param>
        /// <returns></returns>
        public int ShowShareXml(string path, out List<ShareStruct> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("ShareFile")
                                              select target;
                targetNodes = new List<ShareStruct>();
                foreach (var item in Nodes)
                {
                    //取值并重置
                    ShareStruct ns = new ShareStruct();
                    string StartIP = item.Element("StartIP").ToString();
                    StartIP = StartIP.Replace("</StartIP>", "");
                    StartIP = StartIP.Replace("<StartIP>", "");
                    ns.StartIP = StartIP;

                    string ShareCount = item.Element("ShareCount").ToString();
                    ShareCount = ShareCount.Replace("</ShareCount>", "");
                    ShareCount = ShareCount.Replace("<ShareCount>", "");
                    ns.ShareCount = int.Parse(ShareCount);

                    string EndIP = item.Element("EndIP").ToString();
                    EndIP = EndIP.Replace("</EndIP>", "");
                    EndIP = EndIP.Replace("<EndIP>", "");
                    ns.EndIP = EndIP;

                    string NickName = item.Attribute("NickName").ToString();
                    NickName = NickName.Replace("\"", "");
                    NickName = NickName.Replace("NickName=", "");
                    ns.NickName = NickName;

                    targetNodes.Add(ns);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }

        /// <summary>
        /// 显示指定Share数据
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="ShareCount">指定节点</param>
        /// <param name="NickName">网段代号</param>
        /// <param name="StartIP">起始IP</param>
        /// <param name="EndIP">结束IP</param>
        public int ShowShareXml(string path, int ShareCount, out string NickName, out string StartIP, out string EndIP)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("ShareFile")
                                                    where target.Element("ShareCount").Value == ShareCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                StartIP = node.Element("StartIP").ToString();
                StartIP = StartIP.Replace("</StartIP>", "");
                StartIP = StartIP.Replace("<StartIP>", "");

                EndIP = node.Element("EndIP").ToString();
                EndIP = EndIP.Replace("</EndIP>", "");
                EndIP = EndIP.Replace("<EndIP>", "");

                NickName = node.Attribute("NickName").ToString();
                NickName = NickName.Replace("\"", "");
                NickName = NickName.Replace("NickName=", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 删除Share节点
        /// <summary>
        /// 删除Share节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="ShareCount">共享文件夹计数</param>
        /// <returns></returns>
        public int DeleteShareNode(string path, int ShareCount)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("ShareFile")
                                                    where target.Element("ShareCount").Value == ShareCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                //将获得的节点集合中的每一个节点依次从它相应的父节点中删除
                targetNodes.Remove();
                //保存对xml的更改操作
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 插入ftpban节点
        /// <summary>
        /// 插入ftpban节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="banfile">Ftpban文件夹</param>
        /// <returns></returns>
        public int InsertBanNode(string path, string banfile)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);
                XElement newNode = new XElement("FtpBan", new XElement("BanFile", banfile));
                rootNode.Add(newNode);

                //保存节点
                rootNode.Save(path);
            }
            catch (Exception)
            {

                throw;
            }
            return 1;
        }
        #endregion

        #region 删除Ftpban节点
        /// <summary>
        /// 删除Ftpban节点
        /// </summary>
        /// <param name="path">config所在文件</param>
        /// <param name="FtpCount">Ftpban</param>
        /// <returns></returns>
        public int DeleteBanNode(string path, string Ftpban)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("FtpBan")
                                                    where target.Element("BanFile").Value == Ftpban
                                                    select target;
                XElement node = targetNodes.First();

                //将获得的节点集合中的每一个节点依次从它相应的父节点中删除
                targetNodes.Remove();
                //保存对xml的更改操作
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 显示Ftpban数据
        /// <summary>
        /// 显示Ftpban全部数据
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="targetNodes">返回信息</param>
        /// <returns></returns>
        public int ShowBanXml(string path, out List<string> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下FtpBan子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("FtpBan")
                                              select target;
                targetNodes = new List<string>();
                foreach (var item in Nodes)
                {
                    if (item.IsEmpty)
                    {
                        return 1;
                    }
                    //取值并重置
                    string Link = item.Element("BanFile").ToString();
                    Link = Link.Replace("</BanFile>", "");
                    Link = Link.Replace("<BanFile>", "");
  
                    targetNodes.Add(Link);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }

        #endregion
    }
}
