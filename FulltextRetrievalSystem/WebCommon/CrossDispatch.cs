using System;
using System.Collections.Generic;
using XapianPart;
using System.Configuration;
using log4net;
using System.IO;

namespace WebCommon
{
    public class CrossDispatch
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string ConfigValue = ConfigurationManager.AppSettings["LocalXapianSaveAddr"].ToString();

        Queue<List<Index.xapIndex>> qladdxap = new Queue<List<Index.xapIndex>>();   //加入xapian的预处理List队列
        Queue<List<Index.xapIndex>> qlupdxap = new Queue<List<Index.xapIndex>>();   //更新xapian的预处理list队列
        Queue<List<string>> qldelxap = new Queue<List<string>>();   //删除xapian的预处理list队列

        List<Index.xapIndex> listxpadd = new List<Index.xapIndex>();    //加入xapian的预处理list
        List<Index.xapIndex> listxpupd = new List<Index.xapIndex>();    //更新xapian的预处理list
        List<string> listxpdel = new List<string>();        //删除xapian的list

        string add;
        string upd;
        string del;
        /// <summary>
        /// 新增/更新爬虫队列监控线程
        /// </summary>
        public void MonitorCrawl()
        {
            Index newxpindex = new Index();
            int addc = 0;
            int updc = 0;
            try
            {
                while (true)
                {
                    //监控队列
                    if (CrawlLogic.qsAdd.Count == 0 && CrawlLogic.qsUpd.Count == 0)     //若队列为空
                    {
                        if (CrawlLogic.lxffin.Count == CrawlLogic.lxfcount &&
                            CrawlLogic.lxsfin.Count == CrawlLogic.lxscount &&
                            CrawlLogic.lxwfin.Count == CrawlLogic.lxwcount &&
                            listxpadd.Count == 0 && listxpupd.Count == 0)
                        {
                            //检查是否所有队列都处理完了，都处理完成就退出
                            log.Info("爬虫队列监控线程执行结束");
                            return;
                        }
                        if (listxpadd.Count != 0)
                        {
                            log.Info("插入数据库");
                            //否则取出list插入数据库处理
                            Console.WriteLine("插入数据库");
                            int ret = newxpindex.insertIndex(ConfigValue, listxpadd);
                            listxpadd.RemoveAll(it => true);
                        }
                        if (listxpupd.Count != 0)
                        {
                            //否则取出list更新数据库处理
                            log.Info("更新数据库");
                            Console.WriteLine("更新数据库");
                            int ret = newxpindex.updateDocument(ConfigValue, listxpadd);
                            listxpupd.RemoveAll(it => true);
                        }
                    }
                    else
                    {
                        if (CrawlLogic.qsAdd.Count != 0)
                        {
                            while (CrawlLogic.qsAdd.Count != 0)
                            {
                                Console.WriteLine("新增队列:" + (++addc));
                                add = CrawlLogic.qsAdd.Dequeue();

                                if (add != null)
                                {
                                    Index.xapIndex inx = new Index.xapIndex();
                                    if (!File.Exists(add))
                                    {
                                        log.Error("文件下载错误！ "+ add);
                                        Console.WriteLine("文件下载错误！ " + add);
                                        continue;
                                    }
                                    inx = GetInformation(add);                  //获取这个地址的文件对应的信息
                                    Console.WriteLine(inx.link);
                                    listxpadd.Add(inx);
                                    if (listxpadd.Count == 100)
                                    {
                                        log.Info("插入数据库");
                                        //计算100个文档就取出list插入数据库处理
                                        Console.WriteLine("插入数据库");
                                        int ret = newxpindex.insertIndex(ConfigValue, listxpadd);
                                        listxpadd.RemoveAll(it => true);
                                    }
                                }
                            }
                        }
                        if (CrawlLogic.qsUpd.Count != 0)
                        {
                            Console.WriteLine("更新队列:" + (++updc));
                            upd = CrawlLogic.qsUpd.Dequeue();
                            if (add != null)
                            {
                                Index.xapIndex inx = new Index.xapIndex();
                                inx = GetInformation(add);                  //获取这个地址的文件对应的信息
                                listxpupd.Add(inx);
                                if (listxpupd.Count == 100)
                                {
                                    log.Info("更新数据库");
                                    //计算100个文档就更新list插入数据库处理
                                    Console.WriteLine("更新数据库");
                                    int ret = newxpindex.updateDocument(ConfigValue, listxpadd);
                                    listxpupd.RemoveAll(it => true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("爬虫队列监控线程错误！"+ e.Message);
                Console.WriteLine("爬虫队列监控线程错误！" + e.Message);
                return;
            }

        }
        /// <summary>
        /// 删除爬虫队列监控线程
        /// </summary>
        public void MonitorDel()
        {
            Index newxpindex = new Index();
            int i = 0;
            while (true)
            {
                //监控队列
                if (CrawlLogic.qsDel.Count == 0)
                {
                    if (CrawlLogic.lxffin.Count == CrawlLogic.lxfcount &&
                        CrawlLogic.lxsfin.Count == CrawlLogic.lxscount &&
                        CrawlLogic.lxwfin.Count == CrawlLogic.lxwcount &&
                        listxpdel.Count==0)
                    {
                        //检查是否所有队列都处理完了，都处理完成就退出
                        break;
                    }
                    if (listxpdel.Count != 0)
                    {
                        //否则取出list做删除数据库处理
                        int ret = newxpindex.delDocument(ConfigValue, listxpdel);
                        listxpdel.RemoveAll(it => true);
                    }
                }
                else
                {
                    Console.WriteLine("删除队列:" + (++i));

                    del = CrawlLogic.qsDel.Dequeue();
                    if (del != null)
                    {
                        listxpdel.Add(del.GetHashCode().ToString());
                        if (listxpdel.Count == 100)
                        {
                            //取出list做删除数据库处理，满100条处理一次
                            int ret = newxpindex.delDocument(ConfigValue, listxpdel);
                            listxpdel.RemoveAll(it => true);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 检查队列监控线程
        /// </summary>
        public void MonitorCheck()
        {
            Index newxpindex = new Index();
            int i = 0;
            while (true)
            {
                //监控队列
                if (CrawlLogic.qsChk.Count == 0)
                {
                    if (listxpadd.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        //防止一直不传数据降低效率
                        int ret = newxpindex.checkandinsertIndex(ConfigValue, listxpadd);
                        listxpadd.RemoveAll(it => true);
                    }
                }
                else
                {
                    Console.WriteLine("检查队列:"+ (++i));
                    add = CrawlLogic.qsChk.Dequeue();
                    if (add != null)
                    {
                        Index.xapIndex inx = new Index.xapIndex();
                        inx = GetInformation(add);                  //获取这个地址的文件对应的信息
                        listxpadd.Add(inx);
                        if (listxpadd.Count == 100)
                        {
                            //推list做检查数据库处理，满100条处理一次
                            int ret = newxpindex.checkandinsertIndex(ConfigValue, listxpadd);
                            listxpadd.RemoveAll(it => true);
                        }
                    }
                }
            }           
        }
        /// <summary>
        /// 获得文件的所有消息
        /// </summary>
        /// <param name="str">文件路径</param>
        /// <returns></returns>
        private Index.xapIndex GetInformation(string str)
        {
            Index.xapIndex inx = new Index.xapIndex();
            inx.link = str;
            string[] strcutmp = str.Split('\\');
            FileResolve fr = new FileResolve();
            //获得文件的内容，来源，文件名，链接，扩展名
            fr.OpenFile(str, out inx.content, out inx.source, out inx.title, out inx.ahref, out inx.extension);
            inx.seclevel = 0;                       //文件的访问级别
            inx.hashcode = str.GetHashCode().ToString();

            return inx;
        }
    }
}
