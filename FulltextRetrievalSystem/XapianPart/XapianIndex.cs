using log4net;
using System;
using System.Collections.Generic;
using System.Web;

namespace XapianPart
{
    /// <summary>
    /// Xapian 索引，更新，删除
    /// </summary>
    public class Index
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public struct xapIndex
        {
            public string content;         //内容
            public string title;           //标题
            public string ahref;           //原链接
            public string link;            //本地链接
            public int source;             //数据来源
            public int seclevel;           //安全级别 0:全部 >=1以上为等级
            public int extension;          //扩展名
            public string hashcode;        //文件路径哈希值（检索用）
        }

        private uint VALUE_SOURCE = 0;           //文件来源
        private uint VALUE_AHREF = 1;            //原链接
        private uint VALUE_LOCALINK = 2;         //本地连接(cache用)
        private uint VALUE_TITLE = 3;            //标题
        private uint VALUE_TIME = 4;             //加入时间
        private uint VALUE_SECLEVEL = 5;         //等级
        private uint VALUE_EXTENSION = 6;        //扩展名
        private uint VALUE_HASHCODE = 7;         //检索hash

        /// <summary>
        /// 插入一篇文章
        /// </summary>
        /// <param name="dbname">数据库名字</param>
        /// <param name="index">要插入的文章结构体</param>
        /// <returns>是否成功，成功为1，失败为0</returns>
        public int insertIndex(string dbname, xapIndex index)
        {
            ChineseSeg cs = new ChineseSeg();
            //操作索引
            try
            {
                Xapian.WritableDatabase database;
                database = new Xapian.WritableDatabase(dbname, Xapian.Xapian.DB_CREATE_OR_OPEN);

                Xapian.TermGenerator indexer = new Xapian.TermGenerator();
                Xapian.Document doc = new Xapian.Document();
                doc.SetData(HttpUtility.HtmlEncode(index.content));             //设置负载域
                DateTime DateTimestart = DateTime.Now;
                doc.AddValue(VALUE_TIME, DateTimestart.ToString("yyyy/MM/dd")); //插入时间
                doc.AddValue(VALUE_AHREF, index.ahref);                         //原文链接
                doc.AddValue(VALUE_LOCALINK, index.link);                       //本地链接
                doc.AddValue(VALUE_TITLE, index.title);                         //文章标题
                doc.AddValue(VALUE_SOURCE, index.source.ToString());            //扩展类型
                doc.AddValue(VALUE_SECLEVEL, index.seclevel.ToString());        //等级
                doc.AddValue(VALUE_EXTENSION, index.extension.ToString());      //扩展名
                doc.AddValue(VALUE_HASHCODE, index.hashcode);                   //hash

                indexer.SetDocument(doc);
                indexer.SetStemmingStrategy(Xapian.TermGenerator.stem_strategy.STEM_NONE);  //设置不解析策略

                string strcut = cs.JiebaSeg(index.content);             //内容分词（检索分词策略）
                string titlecut = cs.JiebaSeg(index.title);             //标题分词（检索分词策略）

                indexer.IndexText(strcut, 1, "C");                      //设置内容前缀
                indexer.IndexText(titlecut, 1, "T");                    //设置标题前缀
                indexer.IndexText(index.hashcode, 1, "Q");              //设置文档名hash
                indexer.IndexText(index.ahref, 1, "A");                 //设置链接前缀（用于推送文件夹订阅）
                database.AddDocument(doc);                              //加入数据库

                database.Commit();                                      //提交数据库
                database.Close();                                       //关闭数据库
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                Environment.Exit(1);
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// 插入好几篇文章
        /// </summary>
        /// <param name="dbname">数据库名字</param>
        /// <param name="list">要插入的文章结构体列表</param>
        /// <returns>是否成功，成功为1，失败为0</returns>
        public int insertIndex(string dbname, List<xapIndex> list)
        {
            ChineseSeg cs = new ChineseSeg();
            string DBName = dbname;

            //操作索引
            try
            {
                Xapian.WritableDatabase database;
                database = new Xapian.WritableDatabase(DBName, Xapian.Xapian.DB_CREATE_OR_OPEN);

                foreach (var item in list)
                {
                    Console.WriteLine("插入数据："+item.title);
                    Xapian.TermGenerator indexer = new Xapian.TermGenerator();
                    Xapian.Document doc = new Xapian.Document();
                    doc.SetData(HttpUtility.HtmlEncode(item.content));              //设置负载域

                    DateTime DateTimestart = DateTime.Now;
                    doc.AddValue(VALUE_TIME, DateTimestart.ToString("yyyy/MM/dd")); //插入时间
                    doc.AddValue(VALUE_AHREF, item.ahref);                          //原文链接
                    doc.AddValue(VALUE_LOCALINK, item.link);                        //本地链接
                    doc.AddValue(VALUE_TITLE, item.title);                          //文章标题
                    doc.AddValue(VALUE_SOURCE, item.source.ToString());             //来源类型
                    doc.AddValue(VALUE_SECLEVEL, item.seclevel.ToString());         //等级
                    doc.AddValue(VALUE_EXTENSION, item.extension.ToString());       //扩展名
                    doc.AddValue(VALUE_HASHCODE, item.hashcode);                    //hash

                    indexer.SetDocument(doc);
                    indexer.SetStemmingStrategy(Xapian.TermGenerator.stem_strategy.STEM_NONE);  //设置不解析策略

                    string strcut = cs.JiebaSeg(item.content);      //中文分词
                    string titlecut = cs.JiebaSeg(item.title);      //中文分词

                    indexer.IndexText(strcut, 1, "C");              //设置内容前缀
                    indexer.IndexText(titlecut, 1, "T");            //设置标题前缀
                    indexer.IndexText(item.hashcode, 1, "Q");       //设置文档名hash
                    indexer.IndexText(item.ahref, 1, "A");         //设置链接前缀（用于推送文件夹订阅）

                    database.AddDocument(doc);                      //加入数据库
                }
                database.Commit();                                  //提交数据库
                database.Close();                                   //关闭数据库
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// 检查是否存在该文档否则插入数据库
        /// </summary>
        /// <param name="dbname">数据库名字</param>
        /// <param name="list">要插入的文章结构体列表</param>
        /// <returns>是否成功，成功为1，失败为0</returns>
        public int checkandinsertIndex(string dbname, List<xapIndex> list)
        {
            ChineseSeg cs = new ChineseSeg();
            string DBName = dbname;

            //操作索引
            try
            {
                Xapian.WritableDatabase database;
                database = new Xapian.WritableDatabase(DBName, Xapian.Xapian.DB_CREATE_OR_OPEN);

                foreach (var item in list)
                {

                    Xapian.Enquire enquire = new Xapian.Enquire(database);
                    //设置检索的前缀
                    Xapian.QueryParser qp = new Xapian.QueryParser();
                    qp.SetDatabase(database);
                    qp.SetDefaultOp(Xapian.Query.op.OP_ELITE_SET);
                    qp.SetStemmingStrategy(Xapian.QueryParser.stem_strategy.STEM_NONE);
                    //要检查的是hash值
                    string querystr = item.hashcode;
                    qp.AddPrefix("", "Q");  //hash前缀为Q
                    Xapian.Query query = qp.ParseQuery(querystr);
                    Console.WriteLine("query is" + query.GetDescription() + "\n");
                    //开始检索
                    enquire.SetQuery(query);
                    //返回结果
                    Xapian.MSet XapAns = enquire.GetMSet(0, int.MaxValue);
                    if (XapAns == null || XapAns.Size() == 0)                           //如果没有结果就新增
                    {
                        Xapian.TermGenerator indexer = new Xapian.TermGenerator();
                        Xapian.Document doc = new Xapian.Document();
                        doc.SetData(HttpUtility.HtmlEncode(item.content));              //设置负载域

                        DateTime DateTimestart = DateTime.Now;
                        doc.AddValue(VALUE_TIME, DateTimestart.ToString("yyyy/MM/dd")); //插入时间
                        doc.AddValue(VALUE_AHREF, item.ahref);                          //原文链接
                        doc.AddValue(VALUE_LOCALINK, item.link);                        //本地链接
                        doc.AddValue(VALUE_TITLE, item.title);                          //文章标题
                        doc.AddValue(VALUE_SOURCE, item.source.ToString());             //来源类型
                        doc.AddValue(VALUE_SECLEVEL, item.seclevel.ToString());         //等级
                        doc.AddValue(VALUE_EXTENSION, item.extension.ToString());       //扩展名
                        doc.AddValue(VALUE_HASHCODE, item.hashcode);                    //hash

                        indexer.SetDocument(doc);
                        indexer.SetStemmingStrategy(Xapian.TermGenerator.stem_strategy.STEM_NONE);  //设置不解析策略

                        string strcut = cs.JiebaSeg(item.content);
                        string titlecut = cs.JiebaSeg(item.title);

                        indexer.IndexText(strcut, 1, "C");              //设置内容前缀
                        indexer.IndexText(titlecut, 1, "T");            //设置标题前缀
                        indexer.IndexText(item.hashcode, 1, "Q");       //设置文档名hash
                        indexer.IndexText(item.ahref, 1, "A");         //设置链接前缀（用于推送文件夹订阅）

                        database.AddDocument(doc);                      //加入数据库
                    }                    
                }
                database.Commit();                                      //提交数据库
                database.Close();                                       //关闭数据库
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// 更新一篇文章列表
        /// </summary>
        /// <param name="dbname">数据库路径</param>
        /// <param name="list">文章列表</param>
        /// <returns>是否成功，成功为1，失败为0</returns>
        public int updateDocument(string dbname, List<xapIndex> list)
        {
            ChineseSeg cs = new ChineseSeg();
            string DBName = dbname;
            try
            {
                Xapian.WritableDatabase database;
                database = new Xapian.WritableDatabase(DBName, Xapian.Xapian.DB_CREATE_OR_OPEN);

                foreach (var item in list)
                {
                    Xapian.Enquire enquire = new Xapian.Enquire(database);
                    //设置检索的前缀
                    Xapian.QueryParser qp = new Xapian.QueryParser();
                    qp.SetDatabase(database);
                    qp.SetDefaultOp(Xapian.Query.op.OP_ELITE_SET);
                    qp.SetStemmingStrategy(Xapian.QueryParser.stem_strategy.STEM_NONE);
                    //通过hash查找文章
                    string querystr = item.hashcode;
                    qp.AddPrefix("", "Q");  //hash前缀为Q
                    Xapian.Query query = qp.ParseQuery(querystr);
                    Console.WriteLine("query is" + query.GetDescription() + "\n");
                    //开始检索
                    enquire.SetQuery(query);
                    //返回结果
                    Xapian.MSet XapAns = enquire.GetMSet(0, int.MaxValue);
                    for (Xapian.MSetIterator iter = XapAns.Begin(); iter != XapAns.End(); ++iter)
                    {
                        Xapian.Document iterdoc = iter.GetDocument();
                        if (iterdoc.GetValue(VALUE_HASHCODE) != item.hashcode)              //以防出现hash筛选错误
                        {
                            continue;
                        }
                        else
                        {
                            uint docid = iter.GetDocId();                                   //获取唯一id
                            Xapian.Document doc = new Xapian.Document();
                            Xapian.TermGenerator indexer = new Xapian.TermGenerator();
                            doc.SetData(HttpUtility.HtmlEncode(item.content));              //设置负载域

                            DateTime DateTimestart = DateTime.Now;
                            doc.AddValue(VALUE_TIME, DateTimestart.ToString("yyyy/MM/dd")); //插入时间
                            doc.AddValue(VALUE_AHREF, item.ahref);                          //原文链接
                            doc.AddValue(VALUE_LOCALINK, item.link);                        //本地链接
                            doc.AddValue(VALUE_TITLE, item.title);                          //文章标题
                            doc.AddValue(VALUE_SOURCE, item.source.ToString());             //来源类型
                            doc.AddValue(VALUE_SECLEVEL, item.seclevel.ToString());         //等级
                            doc.AddValue(VALUE_EXTENSION, item.extension.ToString());       //扩展名
                            doc.AddValue(VALUE_HASHCODE, item.hashcode);                    //hash

                            indexer.SetDocument(doc);
                            indexer.SetStemmingStrategy(Xapian.TermGenerator.stem_strategy.STEM_NONE);  //设置不解析策略

                            string strcut = cs.JiebaSeg(item.content);
                            string titlecut = cs.JiebaSeg(item.title);

                            indexer.IndexText(strcut, 1, "C");          //设置内容前缀
                            indexer.IndexText(titlecut, 1, "T");        //设置标题前缀
                            indexer.IndexText(item.hashcode, 1, "Q");   //设置文档名hash
                            indexer.IndexText(item.ahref, 1, "A");      //设置链接前缀（用于推送文件夹订阅）

                            database.ReplaceDocument(docid, doc);       //替换文档
                           
                        }
                    }
                }
                database.Commit();                                      //提交数据库
                database.Close();                                       //关闭数据库
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// 在数据库中删除一篇文档
        /// </summary>
        /// <param name="dbname">数据库名</param>
        /// <param name="hashcodelist">文章路径hashcode列表</param>
        /// <returns>是否成功，成功为1，失败为0</returns>
        public int delDocument(string dbname, List<string> hashcodelist)
        {
            string DBName = dbname;
            try
            {
                Xapian.WritableDatabase database;
                database = new Xapian.WritableDatabase(DBName, Xapian.Xapian.DB_CREATE_OR_OPEN);
                foreach (var item in hashcodelist)
                {
                    Xapian.Enquire enquire = new Xapian.Enquire(database);
                    //设置检索的前缀
                    Xapian.QueryParser qp = new Xapian.QueryParser();
                    qp.SetDatabase(database);
                    qp.SetDefaultOp(Xapian.Query.op.OP_ELITE_SET);
                    qp.SetStemmingStrategy(Xapian.QueryParser.stem_strategy.STEM_NONE);
                    //检索hash值
                    string querystr = item;
                    qp.AddPrefix("", "Q");  //hash前缀为Q
                    Xapian.Query query = qp.ParseQuery(querystr);
                    Console.WriteLine("query is" + query.GetDescription() + "\n");
                    //开始检索
                    enquire.SetQuery(query);
                    //返回结果
                    Xapian.MSet XapAns = enquire.GetMSet(0, int.MaxValue);
                    var a = XapAns.Size();
                    for (Xapian.MSetIterator iter = XapAns.Begin(); iter != XapAns.End(); ++iter)
                    {
                        Xapian.Document iterdoc = iter.GetDocument();   
                        if (iterdoc.GetValue(VALUE_HASHCODE) != item)   //防止hash检查出错
                        {
                            continue;
                        }
                        else
                        {
                            uint docid = iter.GetDocId();               //获取唯一id
                            database.DeleteDocument(docid);             //删除文档                            
                        }
                    }
                }
                database.Commit();                                      //提交数据库       
                database.Close();                                       //关闭数据库
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return 0;
            }

            return 1;
        }
    }
}
