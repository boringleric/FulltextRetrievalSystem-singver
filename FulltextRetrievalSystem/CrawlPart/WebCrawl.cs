using Abot.Crawler;
using Abot.Poco;
using AngleSharp.Parser.Html;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

namespace CrawlPart
{
    /// <summary>
    ///  Web爬虫Abot控制类
    /// </summary>
    public class WebCrawl
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //载入预设的web爬虫缓存路径
        private string ConfigValue = ConfigurationManager.AppSettings["LocalWebSaveAddr"].ToString();

        public string link;       //web链接
        public bool flag = true;  //爬虫结束标志

        public Queue<string> QWebAdd = new Queue<string>();
        public Queue<string> QWebChk = new Queue<string>();

        #region web爬虫下载

        /// <summary>
        /// 设置爬虫开始爬行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            log.Info("要爬取的链接 " + pageToCrawl.Uri.AbsoluteUri + " 在页面 " + pageToCrawl.ParentUri.AbsoluteUri);
        }
        /// <summary>
        /// 爬虫判断是否该下载网页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                log.Info("爬取页面失败 " + crawledPage.Uri.AbsoluteUri);
            }
            else
            {
                string[] sArray = crawledPage.Uri.AbsoluteUri.Split('/');
                string localdir = ConfigValue + sArray[2];          //本地存储文件夹
                if (Directory.Exists(localdir) == false)            //如果不存在就创建file文件夹
                {
                    Directory.CreateDirectory(localdir);
                }
                log.Info("爬取页面成功" + crawledPage.Uri.AbsoluteUri);
                //Console.WriteLine("爬取页面成功 {0}", crawledPage.Uri.AbsoluteUri);

                string cnblogsHtml = e.CrawledPage.Content.Text;    //提取Html
                var parser = new HtmlParser();
                var document = parser.Parse(cnblogsHtml);
                var cells = document.QuerySelectorAll("title");     //提取html当中的title
                var titles = cells.Select(m => m.TextContent);
                string strname = "";

                foreach (var item in titles)
                {
                    strname = item.Trim();
                }
                //因为按照title存在本地，因此需要过滤掉不能存储的词
                {
                    strname = strname.Replace(":", "：");
                    strname = strname.Replace("\\", "、");
                    strname = strname.Replace("|", "");
                    strname = strname.Replace("/", "");
                    strname = strname.Replace("?", "？");
                    strname = strname.Replace("<", "《");
                    strname = strname.Replace(">", "》");
                    strname = strname.Replace("*", "");
                    strname = strname.Replace("\"", "");
                    strname = strname.Replace("\v", "");
                    strname = strname.Replace("\a", "");
                    strname = strname.Replace("\b", "");
                    strname = strname.Replace("\t", "");
                    strname = strname.Replace("\n", "");
                    strname = strname.Replace("\f", "");
                }
                //检查是否已经下载过
                if (File.Exists(localdir + @"\" + strname + ".html") == false)
                {
                    try
                    {
                        //若可以就下载，填充数据
                        File.AppendAllText(localdir + @"\" + strname + ".html", @"<p>" + crawledPage.Uri.AbsoluteUri + @"</p>" + e.CrawledPage.Content.Text);

                        QWebAdd.Enqueue(localdir + @"\" + strname + ".html");   //加入新增队列
                    }
                    //由于联调时候的规则出现读写冲突，后续考虑修改
                    catch (Exception d)
                    {
                        log.Error("File被占用 " + d.Message);
                        Console.WriteLine("File被占用", d.Message);
                    }
                }
            }
            if (string.IsNullOrEmpty(crawledPage.Content.Text))
            {
                log.Info("网页无内容 " + crawledPage.Uri.AbsoluteUri);
            }
        }

        /// <summary>
        /// 不爬取这个链接的原因
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            log.Info("不爬取此链接 " + crawledPage.Uri.AbsoluteUri + " 其原因为 " + e.DisallowedReason);
        }
        /// <summary>
        /// 不爬取这个页面的原因
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            log.Info("不爬取此页面 " + pageToCrawl.Uri.AbsoluteUri + " 其原因为 " + e.DisallowedReason);
        }
        /// <summary>
        /// 运行爬虫
        /// </summary>

        public void StartCrawl()
        {
            //设置爬虫
            PoliteWebCrawler crawler = new PoliteWebCrawler();

            //设置爬取条件
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //开始爬取
            CrawlResult result = crawler.Crawl(new Uri(link)); //This is synchronous, it will not go to the next line until the crawl has completed

            //返回结果
            if (result.ErrorOccurred)
            {
                log.Error("链接" + result.RootUri.AbsoluteUri + "出现差错爬取完成:" + result.ErrorException.Message);
                Console.WriteLine("链接 {0} 出现差错爬取完成: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
            }
            else
            {
                log.Info("链接" + result.RootUri.AbsoluteUri + "无差错爬取完成!");
                Console.WriteLine("链接 {0} 无差错爬取完成.", result.RootUri.AbsoluteUri);
            }
            flag = false;
        }
        #endregion

        #region 本地未索引内容检查
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
                QWebChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //空文件夹删之
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空web缓存文件夹" + NextFolder.FullName);
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
            log.Info("开始检查web缓存同步");
            if (theFolder.Exists == false)
            {
                return;
            }
            foreach (FileInfo NextFile in theFolder.GetFiles())
            {
                //加入检查队列
                QWebChk.Enqueue(NextFile.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
            {
                //空文件夹删之
                if (NextFolder.GetDirectories().Length == 0 && NextFolder.GetFiles().Length == 0)
                {
                    log.Info("删除空web缓存文件夹" + NextFolder.FullName);
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
