using log4net;
using CrawlPart;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace WebCommon
{
    public class CrawlLogic
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //创建一系列的委托调用
        delegate int WebCrawlDelegate(string Link);
        delegate int FtpCrawlDelegate(string Link, string name, string psw);
        delegate int ShareCrawlDelegate(string StartIP, string EndIP);
        delegate int CheckDelegate();

        XmlOperation _xo = new XmlOperation();

        public static Queue<string> qsAdd = new Queue<string>();    //新增队列
        public static Queue<string> qsUpd = new Queue<string>();    //更新队列
        public static Queue<string> qsDel = new Queue<string>();    //删除队列
        public static Queue<string> qsChk = new Queue<string>();    //检查队列

        public static List<string> lxsfin = new List<string>();     //共享文件夹爬虫结束列表
        public static List<string> lxffin = new List<string>();     //ftp爬虫结束列表
        public static List<string> lxwfin = new List<string>();     //网页爬虫结束列表

        public static int lxscount;
        public static int lxfcount;
        public static int lxwcount;

        List<XmlOperation.ShareStruct> lxs;     //共享文件夹结构体列表
        List<XmlOperation.FtpStruct> lxf;       //ftp结构体列表
        List<XmlOperation.WebStruct> lxw;       //web结构体列表

        private delegate int ShareMethodCaller(string startIP,string endIP);//定义个代理 
        #region 爬虫
        /// <summary>
        /// 启动所有爬虫的线程
        /// </summary>
        /// <param name="path">config路径</param>
        public void Build(string path)
        {
            log.Info("启动所有控制爬虫的线程");
            //从config中载入各个节点信息
            _xo.ShowShareXml(path, out lxs);
            lxscount = lxs.Count;
            _xo.ShowFtpXml(path, out lxf);
            lxfcount = lxf.Count;
            _xo.ShowWebXml(path, out lxw);
            lxwcount = lxw.Count;
            //如果共享文件夹有内容
            if (lxs.Count != 0)
            {
                foreach (var item in lxs)
                {
                    //建立委托
                    ShareCrawlDelegate myDelegate = new ShareCrawlDelegate(ShareCrawlStart);
                    //启动共享文件夹网段爬虫线程
                    myDelegate.BeginInvoke(item.StartIP, item.EndIP, new AsyncCallback(ShareCrawlCompleted), null);
                }
            }
            //如果Ftp有内容
            if (lxf.Count != 0)
            {
                foreach (var item in lxf)
                {
                    //建立委托
                    FtpCrawlDelegate myDelegate = new FtpCrawlDelegate(FtpCrawlStart);
                    //启动Ftp网段爬虫线程
                    myDelegate.BeginInvoke(item.Link, item.UserName, item.UserPassword, new AsyncCallback(FtpCrawlCompleted), null);
                }
            }
            //如果Web爬虫有内容
            if (lxw.Count != 0)
            {
                foreach (var item in lxw)
                {
                    //建立委托
                    WebCrawlDelegate myDelegate = new WebCrawlDelegate(WebCrawlStart);
                    //启动Web网段爬虫线程
                    myDelegate.BeginInvoke(item.Link, new AsyncCallback(WebCrawlCompleted), null);
                }
            }
        }
        /// <summary>
        /// 启动web爬虫线程
        /// </summary>
        /// <param name="Link">要爬的链接</param>
        /// <returns></returns>
        private static int WebCrawlStart(string Link)
        {
            WebCrawl wc = new WebCrawl();
            wc.link = Link;
            log.Info("启动web爬虫的线程"+Link);
            Thread tr = new Thread(wc.StartCrawl);  //启动子线程
            tr.Start();
            while (true)
            {
                if (!wc.flag)   //监控是否爬虫完成
                {
                    if (wc.QWebAdd.Count != 0)  //若有新增
                    {
                        qsAdd.Enqueue(wc.QWebAdd.Dequeue());    //取出加入处理队列
                    }
                    else
                    {
                        lxwfin.Add(Link);   //完全爬完就加入结束列表并结束线程
                        log.Info("web爬虫的线程结束" + Link);
                        return 1;
                    }
                }
                else
                {
                    if (wc.QWebAdd.Count != 0)  //若有新增
                    {
                        qsAdd.Enqueue(wc.QWebAdd.Dequeue()); //取出加入处理队列
                    }
                }
            }
        }
        /// <summary>
        /// 启动web爬虫线程
        /// </summary>
        /// <param name="name">Ftp链接</param>
        /// <param name="user">Ftp登录名</param>
        /// <param name="psw">Ftp密码</param>
        /// <returns></returns>
        private static int FtpCrawlStart(string name, string user, string psw)
        {
            FtpCrawl fc = new FtpCrawl();
            fc.Host = name;
            fc.User = user;
            fc.Psw = psw;
            log.Info("启动Ftp爬虫的线程" + name);
            Thread tr = new Thread(fc.StartCrawl);  //启动子线程
            tr.Start();
            while (true)
            {
                if (!fc.flag)   //监控是否爬虫完成
                {
                    if (fc.QFtpAdd.Count != 0 || fc.QFtpUpdate.Count != 0)  //若有新增或更新
                    {
                        if (fc.QFtpAdd.Count != 0)
                        {
                            qsAdd.Enqueue(fc.QFtpAdd.Dequeue()); //取出加入新增队列
                        }
                        else
                        {
                            qsUpd.Enqueue(fc.QFtpUpdate.Dequeue()); //取出加入更新队列
                        }
                    }
                    else
                    {
                        lxffin.Add(name);   //完全爬完就加入结束列表并结束线程
                        log.Info("Ftp爬虫的线程结束" + name);
                        return 1;
                    }
                }
                else
                {
                    if (fc.QFtpAdd.Count != 0 || fc.QFtpUpdate.Count != 0)  //若有新增或更新
                    {
                        if (fc.QFtpAdd.Count != 0)
                        {
                            qsAdd.Enqueue(fc.QFtpAdd.Dequeue());    //取出加入新增队列
                        }
                        else
                        {
                            qsUpd.Enqueue(fc.QFtpUpdate.Dequeue()); //取出加入更新队列
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 启动web爬虫线程
        /// </summary>
        /// <param name="startip">起始ip</param>
        /// <param name="endip">结束ip</param>
        /// <returns></returns>
        private static int ShareCrawlStart(string startip, string endip)
        {
            ShareCrawl sc = new ShareCrawl();
            sc.startip = startip;
            sc.endip = endip;
            log.Info("启动共享文件夹爬虫的线程" + startip);
            Thread tr = new Thread(sc.StartCrawl);   //启动子线程
            tr.Start();
            while (true)
            {
                if (!sc.flag)   //监控是否爬虫完成
                {
                    if (sc.QShareAdd.Count != 0 || sc.QShareUpdate.Count != 0)  //若有新增或更新
                    {
                        if (sc.QShareAdd.Count != 0)
                        {
                            qsAdd.Enqueue(sc.QShareAdd.Dequeue());  //取出加入新增队列
                        }
                        else
                        {
                            qsUpd.Enqueue(sc.QShareUpdate.Dequeue()); //取出加入更新队列
                        }
                    }
                    else
                    {
                        lxsfin.Add(startip);    //完全爬完就加入结束列表并结束线程
                        log.Info("共享文件夹爬虫的线程结束" + startip);
                        return 1;
                    }
                }
                else
                {
                    if (sc.QShareAdd.Count != 0 || sc.QShareUpdate.Count != 0)  //若有新增或更新
                    {
                        if (sc.QShareAdd.Count != 0)
                        {
                            qsAdd.Enqueue(sc.QShareAdd.Dequeue());  //取出加入新增队列
                        }
                        else
                        {
                            qsUpd.Enqueue(sc.QShareUpdate.Dequeue()); //取出加入更新队列
                        }
                    }
                }
            }
        }
        private static void WebCrawlCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            WebCrawlDelegate myDelegate = (WebCrawlDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        private static void FtpCrawlCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            FtpCrawlDelegate myDelegate = (FtpCrawlDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        private static void ShareCrawlCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            ShareCrawlDelegate myDelegate = (ShareCrawlDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        #endregion

        #region 删除检查
        /// <summary>
        /// 启动删除线程
        /// </summary>
        /// <param name="path">config路径</param>
        public void Delete(string path)
        {
            log.Info("启动删除线程");
            //从config中载入各个节点信息
            _xo.ShowShareXml(path, out lxs);
            lxscount = lxs.Count;
            _xo.ShowFtpXml(path, out lxf);
            lxfcount = lxf.Count;

            foreach (var item in lxs)
            {
                //建立委托
                ShareCrawlDelegate myDelegate = new ShareCrawlDelegate(ShareDelStart);
                //启动共享文件夹本地与远程同步删除检查线程
                myDelegate.BeginInvoke(item.StartIP, item.EndIP, new AsyncCallback(ShareDelCompleted), null);
            }

            foreach (var item in lxf)
            {
                //建立委托
                FtpCrawlDelegate myDelegate = new FtpCrawlDelegate(FtpDelStart);
                //启动Ftp本地与远程同步删除检查线程
                myDelegate.BeginInvoke(item.Link, item.UserName, item.UserPassword, new AsyncCallback(FtpDelCompleted), null);
            }
        }
        /// <summary>
        /// Ftp本地与远程同步删除检查线程
        /// </summary>
        /// <param name="name">Ftp链接</param>
        /// <param name="user">Ftp登录名</param>
        /// <param name="psw">Ftp密码</param>
        /// <returns></returns>
        private static int FtpDelStart(string name, string user, string psw)
        {
            FtpCrawl fc = new FtpCrawl();
            fc.Host = name;
            fc.User = user;
            fc.Psw = psw;
            log.Info("启动Ftp本地与远程同步删除检查线程"+ name);
            Thread tr = new Thread(fc.CheckExist); //启动子线程
            tr.Start();
            while (true)
            {
                if (!fc.flag) //监控是否爬虫完成
                {
                    if (fc.QFtpDel.Count != 0)  //若有删除
                    {
                        qsDel.Enqueue(fc.QFtpDel.Dequeue());    //取出加入删除队列
                    }
                    else
                    {
                        lxffin.Add(name);   //完全爬完就加入结束列表并结束线程
                        log.Info("Ftp本地与远程同步删除检查线程结束" + name);
                        return 1;
                    }
                }
                else
                {
                    if (fc.QFtpDel.Count != 0)  //若有删除
                    {
                        qsDel.Enqueue(fc.QFtpDel.Dequeue());    //取出加入删除队列
                    }
                }
            }
        }
        /// <summary>
        /// 共享文件夹本地与远程同步删除检查线程
        /// </summary>
        /// <param name="startip">起始ip</param>
        /// <param name="endip">结束ip</param>
        /// <returns></returns>
        private static int ShareDelStart(string startip, string endip)
        {
            ShareCrawl sc = new ShareCrawl();
            sc.startip = startip;
            sc.endip = endip;
            log.Info("启动共享文件夹本地与远程同步删除检查线程" + startip);
            Thread tr = new Thread(sc.CheckExist); //启动子线程
            tr.Start();
            while (true)
            {
                if (!sc.flag)
                {
                    if (sc.QShareDel.Count != 0)    //若有删除
                    {
                        qsDel.Enqueue(sc.QShareDel.Dequeue());  //取出加入删除队列
                    }
                    else
                    {
                        lxsfin.Add(startip);    //完全爬完就加入结束列表并结束线程
                        log.Info("共享文件夹本地与远程同步删除检查线程结束" + startip);
                        return 1;
                    }
                }
                else
                {
                    if (sc.QShareDel.Count != 0)    //若有删除
                    {
                        qsDel.Enqueue(sc.QShareDel.Dequeue());  //取出加入删除队列
                    }
                }
            }
        }
        private static void FtpDelCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            FtpCrawlDelegate myDelegate = (FtpCrawlDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        private static void ShareDelCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            ShareCrawlDelegate myDelegate = (ShareCrawlDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        #endregion

        #region 本地数据库同步检查


        /// <summary>
        /// 启动本地数据库同步检查线程
        /// </summary>
        public void Check()
        {
            log.Info("启动本地数据库同步检查线程");
            //建立委托
            CheckDelegate myshareDelegate = new CheckDelegate(ShareCheckStart);
            myshareDelegate.BeginInvoke(new AsyncCallback(ShareCheckCompleted), null);

            //建立委托
            CheckDelegate myftpDelegate = new CheckDelegate(FtpCheckStart);
            myftpDelegate.BeginInvoke(new AsyncCallback(FtpCheckCompleted), null);

            //建立委托
            CheckDelegate mywebDelegate = new CheckDelegate(WebCheckStart);
            mywebDelegate.BeginInvoke(new AsyncCallback(WebCheckCompleted), null);
        }
        /// <summary>
        /// 启动web本地数据库同步检查线程
        /// </summary>
        /// <returns></returns>
        private static int WebCheckStart()
        {
            WebCrawl wc = new WebCrawl();
            log.Info("启动web本地数据库同步检查线程");
            Thread tr = new Thread(wc.Check); //启动子线程
            tr.Start();
            while (true)
            {
                if (!wc.flag)   //若未结束
                {
                    if (wc.QWebChk.Count != 0)  //取出加入检查队列
                    {
                        qsChk.Enqueue(wc.QWebChk.Dequeue());    
                    }
                    else
                    {
                        log.Info("web本地数据库同步检查线程结束");
                        return 1;
                    }
                }
                else
                {
                    if (wc.QWebChk.Count != 0)  //取出加入检查队列
                    {
                        qsChk.Enqueue(wc.QWebChk.Dequeue());
                    }
                }
            }
        }
        /// <summary>
        /// 启动Ftp本地数据库同步检查线程
        /// </summary>
        /// <returns></returns>
        private static int FtpCheckStart()
        {
            FtpCrawl fc = new FtpCrawl();
            log.Info("启动Ftp本地数据库同步检查线程");
            Thread tr = new Thread(fc.Check); //启动子线程
            tr.Start();
            while (true)
            {
                if (!fc.flag)   //若未结束
                {
                    if (fc.QFtpChk.Count != 0) //取出加入检查队列
                    {
                        qsChk.Enqueue(fc.QFtpChk.Dequeue());
                    }
                    else
                    {
                        log.Info("Ftp本地数据库同步检查线程结束");
                        return 1;
                    }
                }
                else
                {
                    if (fc.QFtpChk.Count != 0)  //取出加入检查队列
                    {
                        qsChk.Enqueue(fc.QFtpChk.Dequeue());
                    }
                }
            }
        }
        /// <summary>
        /// 启动共享文件夹本地数据库同步检查线程
        /// </summary>
        /// <returns></returns>
        private static int ShareCheckStart()
        {
            ShareCrawl sc = new ShareCrawl();
            log.Info("启动共享文件夹本地数据库同步检查线程");
            Thread tr = new Thread(sc.Check); //启动子线程
            tr.Start();
            while (true)
            {
                if (!sc.flag)   //若未结束
                {
                    if (sc.QShareChk.Count != 0)    //取出加入检查队列
                    {
                        qsChk.Enqueue(sc.QShareChk.Dequeue());
                    }
                    else
                    {
                        log.Info("共享文件夹本地数据库同步检查线程结束");
                        return 1;
                    }
                }
                else
                {
                    if (sc.QShareChk.Count != 0)    //取出加入检查队列
                    {
                        qsChk.Enqueue(sc.QShareChk.Dequeue());
                    }
                }
            }
        }
        private static void WebCheckCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            CheckDelegate myDelegate = (CheckDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        private static void FtpCheckCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            CheckDelegate myDelegate = (CheckDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }
        private static void ShareCheckCompleted(IAsyncResult result)
        {
            //获取委托对象，调用EndInvoke方法获取运行结果
            AsyncResult _result = (AsyncResult)result;
            CheckDelegate myDelegate = (CheckDelegate)_result.AsyncDelegate;
            int data = myDelegate.EndInvoke(_result);
        }

        #endregion
    }
}
