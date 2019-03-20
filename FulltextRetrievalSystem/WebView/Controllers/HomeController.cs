using log4net;
using WebCommon;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace WebView.Controllers
{
    /// <summary>
    /// 总体查询的controller，允许匿名查询
    /// </summary>
    [AllowAnonymous]

    public class HomeController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 判断一个字符串是否为url
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsUrl(string str)
        {
            try
            {
                string Url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                return Regex.IsMatch(str, Url);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 返回的结果展示给view
        /// </summary>
        /// <param name="search">检索词</param>
        /// <param name="filter">停用词</param>
        /// <returns></returns>
       [ValidateInput(false)]

        public ActionResult Result(string search, string filter, string page)
        {
            TimeSpan ts;
            List<XapianLogic.SearchResult> XapAns;
            bool urlflag = false;
            string searchbackup = search;
            //角色等级不同
            switch (filter)
            {
                case null:
                    if (User.IsInRole("Admin") || User.IsInRole("Sec"))
                    {
                        filter = "5";       //管理员和等级不同用户检索就是全部可见的
                    }
                    else
                    {
                        filter = "0";       //非管理员检索结果不可见等级不同
                    }
                    break;
                case "0":
                    if (User.IsInRole("Admin") || User.IsInRole("Sec"))
                    {
                        filter = "5";       //管理员和等级不同用户检索就是全部可见的
                    }
                    break;
                case "1":
                    break;
                case "2":
                    break;
                case "3":
                    break;
                case "4":
                case "5":
                    if (!User.IsInRole("Admin") && !User.IsInRole("Sec"))
                    {
                        log.Warn("用户" + User.Identity.Name + "恶意访问！！");
                        filter = "0";
                    }
                    break;
                default:
                    filter = "0";
                    break;
            }
            //分页
            if (page == null)
            {
                page = "0";
            }
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            if (string.IsNullOrEmpty(search))
            {
                //如果没有词检索就返回
                return RedirectToAction("Index");
            }
            else
            {
                if (IsUrl(search))
                {
                    urlflag = true;
                    search = Regex.Replace(search, @"/", " ");
                }
               
                //分词处理
                ViewBag.SearchWord = searchbackup;
                XapianLogic xl = new XapianLogic();
                uint num = 0;
                if (urlflag)
                {
                    xl.SearchReturn(searchbackup, int.Parse(page), int.Parse(filter),"1980/01/01", out num, out XapAns, out ts);
                }
                else
                {
                    //如果是Ftp和共享文件夹的来源，支持用+fileextension：excel、word、ppt、pdf、txt、html，查找过滤
                    //if ((filter == "2" || filter == "3")&&search.Contains("+fileextension"))
                    if (search.Contains("+fileextension"))
                    {
                        int pos = search.IndexOf("+fileextension");     //若有使用扩展名检索，则筛选扩展名
                        string extension = search.Substring(pos + 15, search.Length - pos - 15);
                        string searchkeyword = search.Substring(0, pos);
                        xl.SearchReturn(searchkeyword, int.Parse(page), int.Parse(filter), extension, out num, out XapAns, out ts); //带有扩展名检索
                    }
                    else
                    {
                        xl.SearchReturn(search, int.Parse(page), int.Parse(filter), "0", out num, out XapAns, out ts);  //无扩展名检索
                    }
                }
                

                if (num == 0)
                {
                    //如果没有检索到结果
                    ViewBag.ZeroCheck = "0";
                    TempData["Zero"] = "内容未检索到！";
                    ViewBag.Ansnum = 0;
                    ViewBag.PageCount = 0;
                    ViewBag.time = ts;
                    return View();
                }
                else
                {
                    ViewBag.AnsNum = num;
                }
                //检索到则返回结果
                ViewBag.WebContent = XapAns;
                ViewBag.PageCount = (uint)Math.Ceiling(num / 10.0);
                //检索所用时间
                ViewBag.time = ts;
                return View();
            }
        }

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}