using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XapianPart
{
    /// <summary>
    /// Xapian检索
    /// </summary>
    public class Search
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private uint VALUE_SOURCE = 0;          //文件来源
      //private uint VALUE_AHREF = 1;           //原链接
      //private uint VALUE_LOCALINK = 2;        //本地连接(cache用)
      //private uint VALUE_TITLE = 3;           //标题
        private uint VALUE_TIME = 4;            //加入时间
        private uint VALUE_SECLEVEL = 5;        //等级&来源筛选
        private uint VALUE_EXTENSION = 6;       //扩展名
      //private uint VALUE_HASHCODE = 7;        //检索hash

        /// <summary>
        /// 检索，带有等级不同标记和时间，用于推送
        /// </summary>
        /// <param name="dbname">数据库路径</param>
        /// <param name="querystr">检索语句</param>
        /// <param name="valuefilter">等级不同标记</param>
        /// <param name="addtime">添加时间</param>
        /// <returns>成返回的检索结果</returns>
        public Xapian.MSet Query(string dbname, string querystr, string valuefilter, string addtime)
        {
            try
            {
                Xapian.Database database;
                database = new Xapian.Database(dbname);
                Xapian.StringValueRangeProcessor svr = new Xapian.StringValueRangeProcessor(VALUE_SECLEVEL, "Sec", true);   //等级的前缀
                Xapian.StringValueRangeProcessor svrt = new Xapian.StringValueRangeProcessor(VALUE_TIME, "Addt", true);     //时间的前缀
                Xapian.Enquire enquire = new Xapian.Enquire(database);
                //设置检索的前缀
                Xapian.QueryParser qp = new Xapian.QueryParser();
                if (querystr.Contains(@"//")|| querystr.Contains(@"\\"))        //判断是否为http://、ftp:// 或者共享文件夹\\ip
                {
                    qp.AddPrefix("", "A");
                }
                else
                {
                    qp.AddPrefix("", "");
                    qp.AddPrefix("", "C");
                    qp.AddPrefix("", "T");
                    //检索语句分词（非检索分词）
                    ChineseSeg cs = new ChineseSeg();
                    querystr = cs.JiebaSegnotSearch(querystr);
                }
                
                qp.SetDatabase(database);
                qp.SetDefaultOp(Xapian.Query.op.OP_ELITE_SET);
                qp.AddValuerangeprocessor(svr);
                qp.AddValuerangeprocessor(svrt);
                qp.SetStemmingStrategy(Xapian.QueryParser.stem_strategy.STEM_NONE);

                uint flags = (uint)(Xapian.QueryParser.feature_flag.FLAG_BOOLEAN |
                    Xapian.QueryParser.feature_flag.FLAG_PHRASE |
                    Xapian.QueryParser.feature_flag.FLAG_LOVEHATE |
                    Xapian.QueryParser.feature_flag.FLAG_BOOLEAN_ANY_CASE |
                    Xapian.QueryParser.feature_flag.FLAG_WILDCARD |
                    Xapian.QueryParser.feature_flag.FLAG_PURE_NOT);

                DateTime DateTimestart = DateTime.Now;
                string str = "";
                //判断等级不同类型，0为不接触等级不同，添加过滤
                if (valuefilter == "0")
                {
                    str = querystr + " Sec0..0.5";
                }
                else
                {
                    str = querystr;
                }
                //设置时间过滤
                str = str + @" Addt" + addtime + @".." + DateTimestart.ToString("yyyy/MM/dd");
                Xapian.Query query = qp.ParseQuery(str, flags);
                Console.WriteLine("query is" + query.GetDescription() + "\n");
                //开始检索
                enquire.SetQuery(query);
                //返回结果
                Xapian.MSet XapAns = enquire.GetMSet(0, int.MaxValue);
                return XapAns;
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return null;
            }
        }

        /// <summary>
        /// 根据等级不同级别和后缀类型筛选，用于正文检索
        /// </summary>
        /// <param name="dbname">数据库名</param>
        /// <param name="querystr">检索词</param>
        /// <param name="secsource">等级不同级别</param>
        /// <param name="filetype">文本后缀名</param>
        /// <returns></returns>
        public Xapian.MSet Query(string dbname, string querystr, int secsource, string filetype)
        {
            try
            {
                Xapian.Database database;
                database = new Xapian.Database(dbname);
                Xapian.StringValueRangeProcessor svr = new Xapian.StringValueRangeProcessor(VALUE_SECLEVEL, "Sec", true);   //等级的前缀
                Xapian.StringValueRangeProcessor sou = new Xapian.StringValueRangeProcessor(VALUE_SOURCE, "Sou", true);     //来源前缀
                Xapian.StringValueRangeProcessor ext = new Xapian.StringValueRangeProcessor(VALUE_EXTENSION, "Ext", true);  //扩展名前缀
                Xapian.Enquire enquire = new Xapian.Enquire(database);
                //设置检索的前缀
                Xapian.QueryParser qp = new Xapian.QueryParser();
                qp.AddPrefix("", "");
                qp.AddPrefix("", "C");
                qp.AddPrefix("", "T");
                qp.SetDatabase(database);
                qp.SetDefaultOp(Xapian.Query.op.OP_ELITE_SET);
                qp.AddValuerangeprocessor(svr);
                qp.AddValuerangeprocessor(ext);
                qp.AddValuerangeprocessor(sou);
                qp.SetStemmingStrategy(Xapian.QueryParser.stem_strategy.STEM_NONE);

                uint flags = (uint)(Xapian.QueryParser.feature_flag.FLAG_BOOLEAN |
                    Xapian.QueryParser.feature_flag.FLAG_PHRASE |
                    Xapian.QueryParser.feature_flag.FLAG_LOVEHATE |
                    Xapian.QueryParser.feature_flag.FLAG_BOOLEAN_ANY_CASE |
                    Xapian.QueryParser.feature_flag.FLAG_WILDCARD |
                    Xapian.QueryParser.feature_flag.FLAG_PURE_NOT);

                string str = "";
                //过滤等级不同
                switch (secsource)
                {
                    case 0:     
                        str = querystr + " Sec0..0.5";      //无等级
                        break;
                    case 1:
                        str = querystr + " Sou1..1.5";      //web来源
                        break;
                    case 2:
                        str = querystr + " Sou2..2.5";      //Ftp来源
                        break;
                    case 3:
                        str = querystr + " Sou3..3.5";      //Share来源
                        break;
                    case 4:
                        str = querystr + " Sou4..4.5";      //等级不同来源
                        break;
                    default:
                        str = querystr;                     //所有
                        break;
                }

                //过滤后缀
                switch (filetype)
                {
                    case "ppt":
                        str = str + " Ext1..1.5";
                        break;
                    case "word":
                        str = str + " Ext2..2.5";
                        break;
                    case "excel":
                        str = str + " Ext3..3.5";
                        break;
                    case "pdf":
                        str = str + " Ext5..5.5";
                        break;
                    case "txt":
                        str = str + " Ext4..4.5";
                        break;
                    case "html":
                        str = str + " Ext6..6.5";
                        break;
                    default:
                        break;
                }

                Xapian.Query query = qp.ParseQuery(str, flags);
                Console.WriteLine("query is" + query.GetDescription() + "\n");
                //开始检索
                enquire.SetQuery(query);
                Xapian.MSet XapAns = enquire.GetMSet(0, int.MaxValue);
                //返回结果
                return XapAns;
            }
            catch (Exception e)
            {
                log.Error("Message"+e.Message);
                log.Error("InnerException"+e.InnerException);
                log.Error("StackTrace"+e.StackTrace);
                Console.Error.WriteLine("Exception: " + e.ToString());
                return null;
            }
        }
    }
}
