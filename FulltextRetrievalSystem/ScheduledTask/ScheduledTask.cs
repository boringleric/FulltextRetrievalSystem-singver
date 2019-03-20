using log4net;
using WebCommon;
using WebView.WebPushFunction;
using System;
using System.Configuration;
using System.Threading;

namespace ScheduledTask
{
    /// <summary>
    /// 计划任务，分为爬虫、删除检查、订阅推送、缓存检查
    /// </summary>
    class ScheduledTask
    {
        ////log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //载入本地爬虫缓存路径
        private static string ConfigValue = ConfigurationManager.AppSettings["LocalCrawlConfig"].ToString();
        /// <summary>
        /// 启动爬虫
        /// </summary>
        /// <returns></returns>
        private static int RunCrawl()
        {
            CrawlLogic cl = new CrawlLogic();
            cl.Build(ConfigValue);                      //启动爬虫线程
            CrossDispatch cd = new CrossDispatch();
            Thread tr = new Thread(cd.MonitorCrawl);    //开始监控爬虫线程
            tr.Start();
            while (true)
            {
                if (tr.ThreadState == ThreadState.Stopped)  //若线程结束，则爬虫结束
                {
                    log.Info(DateTime.Now.ToString() + "爬虫结束");
                    return 1;
                }
            }
        }
        /// <summary>
        /// 启动删除检查
        /// </summary>
        /// <returns></returns>
        private static int RunDelTest()
        {
            CrawlLogic cl = new CrawlLogic();
            cl.Delete(ConfigValue);                     //启动删除线程
            CrossDispatch cd = new CrossDispatch();
            Thread tr = new Thread(cd.MonitorDel);      //开始监控删除线程
            tr.Start();
            while (true)
            {
                if (tr.ThreadState == ThreadState.Stopped)  //若线程结束，则爬虫结束
                {
                    log.Info(DateTime.Now.ToString() + "删除检查结束");
                    return 1;
                }
            }
        }
        /// <summary>
        /// 启动推送
        /// </summary>
        /// <returns></returns>
        private static int RunMail()
        {
            WebPushFunction pf = new WebPushFunction();
            int ret = pf.GetUserSubandPush();           //调用推送函数
            if (ret == 1)
            {
                log.Info(DateTime.Now.ToString() + "订阅推送结束");
            }
            else
            {
                log.Info(DateTime.Now.ToString() + "订阅推送失败");
            }
            return ret;
        }
        /// <summary>
        /// 启动本地未索引检查
        /// </summary>
        /// <returns></returns>
        private static int RunCheck()
        {
            CrawlLogic cl = new CrawlLogic();
            cl.Check();                                 //启动本地检查线程
            CrossDispatch cd = new CrossDispatch();
            Thread tr = new Thread(cd.MonitorCheck);    //开始启动检查线程
            tr.Start();
            while (true)
            {
                if (tr.ThreadState == ThreadState.Stopped)   //若线程结束，则检测结束
                {
                    log.Info(DateTime.Now.ToString() + "本地缓存与数据库检测结束");
                    return 1;
                }
            }           
        }

        static void Main(string[] args)
        {
            int ret = -1;
            switch (args[0])
            {
                //根据预设的内容开始各项检查
                case "startcrawl":
                    log.Info(DateTime.Now.ToString() + "爬虫开始");
                    ret = RunCrawl();
                    break;
                case "startdelete":
                    log.Info(DateTime.Now.ToString() + "删除检查开始");
                    ret = RunDelTest();
                    break;
                case "startmail":
                    log.Info(DateTime.Now.ToString() + "订阅推送开始");
                    ret = RunMail();
                    break;
                case "errorcorrection":
                    log.Info(DateTime.Now.ToString() + "本地缓存与数据库检测开始");
                    ret = RunCheck();
                    break;
                default:
                    break;
            }
            while (true)
            {
                if (ret != -1)
                {
                    break;
                }
            }
        }
    }
}
