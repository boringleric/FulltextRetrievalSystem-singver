using System;
using System.Collections.Generic;
using XapianPart;
using System.Web;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Collections;

namespace WebCommon
{
    public class XapianLogic
    {
        private Search _xps = new Search();
        private Index _idx = new Index();
        private ChineseSeg _cs = new ChineseSeg();
        private string localdb = ConfigurationManager.AppSettings["LocalXapianSaveAddr"].ToString();
        public struct SearchResult
        {
            public string ahref;                        //原链接
            public string ahrefencode;                  //转码后原链接（Ftp用）
            public string link;                         //本地连接
            public string title;                        //标题
            public string snippet;                      //快照
            public string allcontent;                   //所有内容（等级不同库用）
        }
        
        public struct AddSec
        {
            public string id;
            public string title;
            public string content;
            public string seclevel;
        }
        /// <summary>
        /// ftp链接转码用
        /// </summary>
        /// <param name="url">ftp链接</param>
        /// <returns>转码后的ftp链接</returns>
        protected string UrlEncode(string url)
        {
            byte[] bs = Encoding.GetEncoding("GB2312").GetBytes(url);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bs.Length; i++)
            {
                if (bs[i] < 128)
                    sb.Append((char)bs[i]);
                else
                {
                    sb.Append("%" + bs[i++].ToString("x").PadLeft(2, '0'));
                    sb.Append("%" + bs[i].ToString("x").PadLeft(2, '0'));
                }
            }
            return sb.ToString();
        }


        /// <summary>
        /// 检索结果
        /// </summary>
        /// <param name="query">检索词</param>
        /// <param name="page">分页号</param>
        /// <param name="filter">来源/等级不同过滤</param>
        /// <param name="filetype">类型过滤</param>
        /// <param name="num">返回结果数目</param>
        /// <param name="XapResList">返回检索结果</param>
        /// <param name="ts">返回检索时间</param>
        public void SearchReturn(string query, int page, int filter, string filetype, out uint num, out List<SearchResult> XapResList, out TimeSpan ts)
        {
            DateTime DateTimestart = DateTime.Now;
            DateTime DateTimeend;
            Xapian.MSet xm;
            XapResList = new List<SearchResult>();

            {
                query = query.Replace("\\", "");
                query = query.Replace("/", "");
            }

            string querystr = _cs.JiebaSegnotSearch(query);     //分词

            if (!Directory.Exists(localdb))
            {
                Directory.CreateDirectory(localdb);
            }
            if (filetype == "1980/01/01")
            {
                xm = searchforpush(query, filter, filetype);   //检索
            }
            else
            {
                xm = search(querystr, filter, filetype);   //检索
            }
            
            //若返回不为空
            if (xm != null)
            {
                num = xm.Size();    //结果数目
                int pagecount = 0;
                for (Xapian.MSetIterator iter = xm.Begin(); iter != xm.End(); ++iter)
                {
                    SearchResult sr = new SearchResult();
                    ++pagecount;
                    if (pagecount <= ((page - 1) * 10))     //获得分页
                    {
                        continue;
                    }
                    else
                    {
                        if (XapResList.Count >= 10)         //每页10个结果
                        {
                            break;
                        }

                        Xapian.Document iterdoc = iter.GetDocument();
                        bool ftpflag = false;                             //ftp标记，转码用
                        bool emflag = false;
                        string strcontent = iterdoc.GetData();           //取出正文
                        string strtitle = iterdoc.GetValue(3);           //取出标题 ValueTitle
                        string strahref = iterdoc.GetValue(1);            //取出链接
                        string source = iterdoc.GetValue(0);
                        string strcut = "";
                        int contentlen = strcontent.Length;              //判断正文长度，为下面筛选含有关键词片段做准备
                        uint docid = iter.GetDocId();

                        if (source == "4")
                        {
                            sr.allcontent = strcontent;
                        }
                        if (source == "2")
                        {
                            ftpflag = true;
                            strahref = UrlEncode(strahref);             //若为ftp链接，需要转码
                        }
                        string[] strquerycut = querystr.Split(' ');
                        string emlink = "";
                        List<string> tmp = new List<string>();
                        foreach (var item in strquerycut)
                        {
                            if (item == "e"|| item=="E" || item == "m"||item=="M"||
                                item == "em" || item == "Em" || item == "Em" || item == "EM"||
                                item == "<" || item == ">")
                            {
                                emflag = true;
                                if (emlink!="")
                                {
                                    emlink = emlink + "|"+ item;
                                }
                                else
                                {
                                    emlink = item;
                                }
                            }
                            else
                            {
                                tmp.Add(item);
                            }
                        }
                        HashSet<string> hs = new HashSet<string>(tmp); //此时已经去掉重复的数据保存在hashset中
                        String[] strunique = new String[hs.Count];
                        hs.CopyTo(strunique);
                        
                        int cutlen = strunique.Length;
                        int count = 0;

                        if (emlink!=""&&cutlen==0)
                        {
                            foreach (var item in strquerycut)
                            {
                                //消掉*问号空格
                                if (item == " " || item == "")
                                {
                                    continue;
                                }
                                CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
                                int conpos = Compare.IndexOf(strcontent, item, CompareOptions.IgnoreCase);      //根据位置标红
                                                                                                                //int conpos = strcontent.IndexOf(item);      //根据位置标红
                                if (conpos != -1)
                                {
                                    if (contentlen - conpos > 150 && conpos > 50)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, 200);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }               
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (conpos > 50)
                                    {
                                        ////截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, contentlen - conpos + 50);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (contentlen - conpos > 150)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(0, conpos + 150);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                       
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else
                                    {
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        //不够150的全拿出
                                        strcut = strcontent;
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                }
                                else
                                {
                                    CompareInfo Comparetitle = CultureInfo.InvariantCulture.CompareInfo;
                                    int conpostitle = Comparetitle.IndexOf(strtitle, item, CompareOptions.IgnoreCase);      //根据位置标红
                                    if (conpostitle != -1)
                                    {
                                        if (contentlen > 200)
                                        {
                                            strcut = strcontent.Substring(0, 200);
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                        else
                                        {
                                            strcut = strcontent;
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                    }
                                    else
                                    {
                                        ++count;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //每一个词都查一遍
                            foreach (var item in strunique)
                            {
                                //消掉*问号空格
                                if (item == " " || item == "")
                                {
                                    continue;
                                }
                                CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
                                int conpos = Compare.IndexOf(strcontent, item, CompareOptions.IgnoreCase);      //根据位置标红
                                                                                                                //int conpos = strcontent.IndexOf(item);      //根据位置标红
                                if (conpos != -1)
                                {
                                    if (contentlen - conpos > 150 && conpos > 50)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, 200);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红，大小写不敏感，regex替换法，replace大小写敏感
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (conpos > 50)
                                    {
                                        ////截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, contentlen - conpos + 50);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (contentlen - conpos > 150)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(0, conpos + 150);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else
                                    {
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        //不够150的全拿出
                                        strcut = strcontent;
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                }
                                else
                                {
                                    CompareInfo Comparetitle = CultureInfo.InvariantCulture.CompareInfo;
                                    int conpostitle = Comparetitle.IndexOf(strtitle, item, CompareOptions.IgnoreCase);      //根据位置标红
                                    if (conpostitle != -1)
                                    {
                                        if (contentlen > 200)
                                        {
                                            strcut = strcontent.Substring(0, 200);
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            //strcut = HttpUtility.HtmlEncode(strcut);
                                            for (; count < cutlen; count++)
                                            {
                                                if (strunique[count] == " " || strunique[count] == "")
                                                {
                                                    continue;
                                                }
                                                //标红
                                                strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                                //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            }
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                        else
                                        {
                                            strcut = strcontent;
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            //strcut = HttpUtility.HtmlEncode(strcut);
                                            for (; count < cutlen; count++)
                                            {
                                                if (strunique[count] == " " || strunique[count] == "")
                                                {
                                                    continue;
                                                }
                                                //标红
                                                strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                                //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            }
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                    }
                                    else
                                    {
                                        ++count;
                                    }
                                }
                            }
                        }





                        //找到合适的内容之后返回结果
                        Finally:
                        sr.ahref = iterdoc.GetValue(1);
                        if (ftpflag)                    //判断是否需要转码链接
                        {
                            sr.ahrefencode = strahref; //ftp则使用转码链接
                        }
                        else
                        {
                            sr.ahrefencode = sr.ahref;
                        }
                        sr.link = iterdoc.GetValue(2);
                        sr.title = strtitle;
                        sr.snippet = strcut;
                        XapResList.Add(sr);
                    }
                }
            }
            else
            {
                num = 0;
            }
            DateTimeend = DateTime.Now;
            ts = DateTimeend - DateTimestart;
            ts.TotalMilliseconds.ToString();        //查询时间返回
        }

        static public string ReplaceCntent(string pattern, string Content)
        {
           // string pattern = "e|E|m|M|em|EM|Em|eM|<|>";
            Regex Reg = new Regex(pattern);
            MatchEvaluator evaluator = new MatchEvaluator(ConvertToEM);
            return Reg.Replace(Content, evaluator);
        }
        static public string ConvertToEM(Match m)
        {
            string Letter = string.Empty;
            switch (m.Value)
            {
                case "m":
                    Letter = @"<em>m</em>";
                    break;
                case "e":
                    Letter = @"<em>e</em>";
                    break;
                case "em":
                    Letter = @"<em>em</em>";
                    break;
                case "M":
                    Letter = @"<em>M</em>";
                    break;
                case "E":
                    Letter = @"<em>E</em>";
                    break;
                case "Em":
                    Letter = @"<em>Em</em>";
                    break;
                case "EM":
                    Letter = @"<em>EM</em>";
                    break;
                case "eM":
                    Letter = @"<em>eM</em>";
                    break;
                case "<":
                    Letter = @"<em><</em>";
                    break;
                case ">":
                    Letter = @"<em>></em>";
                    break;
                default:
                    Letter = "";
                    break;
            }
            return Letter;
        }

        /// <summary>
        /// 用于正文检索的查询
        /// </summary>
        /// <param name="query">检索语句</param>
        /// <param name="filter">等级不同/来源</param>
        /// <param name="filetype">文件类型</param>
        /// <returns></returns>
        private Xapian.MSet search(string query, int filter, string filetype)
        {
           return _xps.Query(localdb, query, filter, filetype);
        }
        /// <summary>
        /// 用于推送的查询
        /// </summary>
        /// <param name="query">检索语句</param>
        /// <param name="filter">等级不同与否</param>
        /// <param name="addtime">添加时间</param>
        /// <returns>检索结果</returns>
        public string SearchForPush(string query, int filter, string addtime)
        {
            string str = "";
            Xapian.MSet xm = searchforpush(query, filter, addtime);
            if (!Directory.Exists(localdb))
            {
                Directory.CreateDirectory(localdb);
            }
            if (xm != null)
            {          
                if (xm.Size() == 0)
                {
                    return null;
                }
                else
                {
                    str = "订阅词" + query + "有" + xm.Size().ToString() + "条更新！\n";

                    int i = 0;
                    for (Xapian.MSetIterator iter = xm.Begin(); iter != xm.End(); ++iter)
                    {
                        //构造检索结果
                        Xapian.Document iterdoc = iter.GetDocument();
                        str = str + (++i).ToString() + " 、标题：" + iterdoc.GetValue(3) + "\n 链接："+ iterdoc.GetValue(1) + "\n";
                    }
                }               
            }
            else
            {
                return null;
            }

            return str;
        }
        /// <summary>
        /// 用于推送的查询
        /// </summary>
        /// <param name="query">检索语句</param>
        /// <param name="filter">等级不同与否</param>
        /// <param name="addtime">添加时间</param>
        /// <returns>检索结果的Mset</returns>
        private Xapian.MSet searchforpush(string query, int filter, string addtime)
        {
            return _xps.Query(localdb, query, filter.ToString(), addtime);
        }
    }
}
