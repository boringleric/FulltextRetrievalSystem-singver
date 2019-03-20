using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.IO;
using System.Text;
using System.Linq;
using DotMaysWind.Office;
using System.Collections.Generic;
using AngleSharp.Parser.Html;
using System.Configuration;
using log4net;

namespace XapianPart
{
    public class FileResolve
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //载入预设的html页面提取方法
        private string ConfigValue = ConfigurationManager.AppSettings["PageResolveConfig"].ToString();

        #region 文本打开与查找

        /// <summary>
        /// 找到文件的原链接
        /// </summary>
        /// <param name="file">本地文件链接</param>
        /// <param name="ahref">该文件的原来链接</param>
        /// <returns>返回来源</returns>
        private int FindSource(string file, out string ahref)
        {
            string[] strcut = file.Split('\\');
            ahref = "";
            //根据文件来源获取原链接
            switch (strcut[2])
            {
                case "Web":
                    int posweb = file.IndexOf("Web\\");
                    return 1;
                case "Ftp":
                    int posftp = file.IndexOf("Ftp\\");
                    ahref = @"ftp://" + file.Substring(posftp + 4, file.Length - posftp - 4); //构造ftp链接
                    return 2;
                case "Share":
                    int posshare = file.IndexOf(strcut[3]);
                    ahref = @"\\" + strcut[3] +@"\"+ file.Substring(posshare + strcut[3].Length + 1, file.Length - posshare - 1 - strcut[3].Length); //构造共享文件夹链接
                    return 3;
                default:
                    return 0;
            }
        }
        /// <summary>
        /// 打开文件内容
        /// </summary>
        /// <param name="file">本地文件链接</param>
        /// <param name="strans">文件正文</param>
        /// <param name="source">文件来源，web，ftp，share</param>
        /// <param name="filename">文件名</param>
        /// <param name="ahref">该文件的原来链接</param>
        /// <returns></returns>
        public int OpenFile(string file, out string strans,
            out int source, out string filename, out string ahref, out int ext)
        {
            strans = ""; filename = ""; ahref = ""; source = 0;
           string extension = "";
            ext = 0;
            if (!string.IsNullOrEmpty(file))
            {
                extension = System.IO.Path.GetExtension(file);                  //获得文件扩展名
                filename = System.IO.Path.GetFileNameWithoutExtension(file);    //获得文件名
                source = FindSource(file, out ahref);                           //获得文件源和链接
                switch (extension)
                {
                    //根据后缀提取文件内容
                    case ".ppt":
                    case ".pptx":
                        ext = 1;
                        PptWordExtract(file, out strans);
                        break;
                    case ".doc":
                    case ".docx":
                        ext = 2;
                        PptWordExtract(file, out strans);
                        break;
                    case ".xls":
                    case ".xlsx":
                        ext = 3;
                        ExcelExtract(file, out strans);
                        break;
                    case ".txt":
                        ext = 4;
                        TxtExtract(file, out strans);
                        break;
                    case ".pdf":
                        ext = 5;
                        PdfExtract(file, out strans);
                        break;
                    case ".html":
                        ext = 6;
                        HtmlExtract(file, out strans, out ahref);
                        break;
                    case ".rntf":
                        ext = 0;
                        string real = file.Replace(extension, "");
                        int pos = real.LastIndexOf(".");
                        string finalexten = real.Substring(pos, real.Length - pos);
                        ahref = ahref.Replace(extension, "");
                        break;
                    default:
                        break;
                }
            }
            return 0;
        }

        #endregion

        #region 文本正文提取
        /// <summary>
        /// excel正文提取
        /// </summary>
        /// <param name="strname">输入文本路径</param>
        /// <param name="strout">输出解析文本</param>
        /// <returns></returns>
        public int ExcelExtract(string strname, out string strout)
        {
            strout = "";
            using (FileStream stream = File.OpenRead(strname))
            {
                IWorkbook workbook;
                if (System.IO.Path.GetExtension(strname) == ".xls")
                {
                    workbook = new HSSFWorkbook(stream);
                }
                else
                {
                    workbook = new XSSFWorkbook(stream);
                }

                // HSSFWorkbook excel = new HSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);
                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    if (sheet.GetRow(i) == null)
                    {
                        continue;
                    }
                    foreach (ICell cell in sheet.GetRow(i).Cells)
                    {
                        /*
                         * Excel数据Cell有不同的类型，当我们试图从一个数字类型的Cell读取出一个字符串并写入数据库时，就会出现Cannot get a text value from a numeric cell的异常错误。
                         * 解决办法：先设置Cell的类型，然后就可以把纯数字作为String类型读进来了
                         */
                        strout += cell;
                       // Console.WriteLine(cell);
                    }
                   // Console.WriteLine("*");
                }
            }
            return 0;
        }
        /// <summary>
        /// txt正文提取
        /// </summary>
        /// <param name="path">输入文本路径</param>
        /// <param name="strout">输出解析文本</param>
        /// <returns></returns>
        public int TxtExtract(string path, out string strout)
        {
            strout = "";
            FileInfo fi = new FileInfo(path);
            if (fi.Length>=(1024*1024))
            {
                log.Error("txt文件太大，不读" + path);
                return 0;
            }
            StreamReader sr = new StreamReader(path, Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                strout += line;
            }
            return 0;
        }
        /// <summary>
        /// pdf正文提取
        /// </summary>
        /// <param name="filepath">输入文本路径</param>
        /// <param name="strout">输出解析文本</param>
        /// <returns></returns>
        public int PdfExtract(string filepath, out string strout)
        {
            strout = "";
            StringBuilder text = new StringBuilder();

            if (File.Exists(filepath))
            {
                try
                {
                    PdfReader pdfReader = new PdfReader(filepath);
                    //按页码读取
                    for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                    {
                        ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                        string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);
                        //转码
                        currentText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                        text.Append(currentText);
                    }
                    pdfReader.Close();
                    strout = text.ToString();
                }
                catch (Exception e)
                {
                    log.Error("pdf文件出错：" + filepath + e.Message);
                    Console.WriteLine("pdf文件出错：" + filepath + e.Message);
                }                
            }            
            return 0;
        }
        /// <summary>
        /// html正文提取（借助解析规则）
        /// </summary>
        /// <param name="uris">输入文本路径</param>
        /// <param name="strout">输出解析文本</param>
        /// <param name="ahref">输出原链接</param>
        /// <returns></returns>
        public int HtmlExtract(string uris, out string strout, out string ahref)
        {
            strout = "";
            string cnblogsHtml;
            using (var reader = new StreamReader(uris))
            {
                cnblogsHtml = reader.ReadToEnd();
            }

            var parser = new HtmlParser();
            var document = parser.Parse(cnblogsHtml);

            var cell = document.QuerySelector("p");         //提取预设源链接
            if (cell!=null)
            {
                ahref = cell.TextContent;
            }
            else
            {
                ahref = "";
                log.Info("不在范围内的html，不读："+ uris);
                return 0;
            }
            HtmlExtract he = new HtmlExtract();
            List<HtmlExtract.RulesStruct> lhr = new List<HtmlExtract.RulesStruct>();
            HtmlExtract.RulesStruct lr = new HtmlExtract.RulesStruct();
            he.ShowWebXml(ConfigValue, out lhr);
            string[] strcut = uris.Split('\\');
            string findtitle = strcut[3];
            lr = lhr.Find(item => item.Link == findtitle);  //在规则库中查找匹配文本解析规则
            //替换错误的词汇
            string rules = lr.Rules;
            if (rules != null)
            {
                //过滤错误的链接
                rules.Replace("&gt;", ">");
                rules.Replace("&lt;", "<");
                rules.Replace("&quot;", "\"");
                rules.Replace("&amp;", "&");
                var cells1 = document.QuerySelectorAll(rules);
                //获得结果
                var titles1 = cells1.Select(m => m.TextContent);
                foreach (var item in titles1)
                {
                    //Console.WriteLine(item);
                    if (item != "")
                    {
                        strout += item;
                    }
                }
            }          
            return 0;
        }

        #region PPT&WORD 解析
        /// <summary>
        /// pptword解析
        /// </summary>
        /// <param name="data">输入文本路径</param>
        /// <param name="strout">输出解析文本</param>
        public void PptWordExtract(string data, out string strout)
        {
            IOfficeFile _file;
            strout = "";
            string ans = "";

            //检查文件数据
            if (File.Exists(data))
            {
                try
                {
                    _file = OfficeFileFactory.CreateOfficeFile(data);
                    ans = string.Format((_file == null ? "Failed to open \"{0}\"." : ""), data);

                    ShowSummary(_file.SummaryInformation);
                    strout = ShowContent(_file);                    //展示文件内容
                    ShowSummary(_file.DocumentSummaryInformation);
                }
                catch (Exception ex)
                {
                    ans = string.Format("Error: {0}", ex.Message);
                    Console.WriteLine(ans);
                }
            }
        }
        /// <summary>
        /// 展示文件内容
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <returns>文件内容</returns>
        private string ShowContent(IOfficeFile file)
        {
            string ans2 = "";
            if (file is IWordFile)
            {
                IWordFile wordFile = file as IWordFile;
                ans2 = wordFile.ParagraphText;
            }
            else if (file is IPowerPointFile)
            {
                IPowerPointFile pptFile = file as IPowerPointFile;
                ans2 = pptFile.AllText;
            }
            else
            {
                ans2 = string.Format("无法在此文件中提取数据.");
            }
            return ans2;
        }
        /// <summary>
        /// 展示文件摘要
        /// </summary>
        /// <param name="dictionary">文件路径</param>
        private void ShowSummary(Dictionary<string, string> dictionary)
        {
            string ans1 = "";
            if (dictionary == null)
            {
                ans1 = string.Format("此文件非微软office文件格式.");
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                sb.AppendFormat("[{0}]={1}", pair.Key, pair.Value);
                sb.AppendLine();
            }

            ans1 = sb.ToString();
        }
        
        #endregion

        #endregion
    }
}
