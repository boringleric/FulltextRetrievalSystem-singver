using log4net;
using Microsoft.AspNet.Identity;
using PushFunction;
using WebView.WebPushFunction;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using System.Xml;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，个人订阅的增删改
    /// </summary>
    [Authorize]
    public class SubscribeController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Subscribe
        UserSubscribe _us = new UserSubscribe();
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalSubsSaveAddr"].ToString();     //载入预设的配置路径
        public struct SubsStruct
        {
            public string SearchWord;
            public string AddTime;
        }
        public ActionResult Index()
        {
            ViewBag.PageName = "个人订阅管理";
            ViewBag.PageDescription = "管理订阅信息";
            List<UserSubscribe.SubStruct> lus;
            if (!System.IO.Directory.Exists(CrawlConfigValue))
            {
                System.IO.Directory.CreateDirectory(CrawlConfigValue);
            }
            string userid = User.Identity.GetUserId();
            string str = CrawlConfigValue + userid + @".config";
            _us.ShowSubXml(str, out lus);                           //展示订阅信息
            ViewBag.SubList = lus;
            return View();
        }
        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建订阅";
            ViewBag.PageDescription = "创建订阅详情";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult Create(string SearchWord)
        {
            ViewBag.PageName = "创建订阅";
            ViewBag.PageDescription = "创建订阅详情";
            if (SearchWord != null)
            {
                string userid = User.Identity.GetUserId();
                string str = CrawlConfigValue + userid + @".config";
                _us.InsertSubNode(str, SearchWord); //插入节点
                return RedirectToAction("Index");   //成功
            }
            return View();
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult Delete(string SubCount, string SearchWord)
        {
            ViewBag.PageName = "删除订阅";
            ViewBag.PageDescription = "删除订阅详情";
            ViewBag.SearchWord = SearchWord;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string SubCount, string SearchWord)
        {
            ViewBag.PageName = "删除订阅";
            ViewBag.PageDescription = "删除订阅详情";
            ViewBag.SearchWord = SearchWord;
            if (SearchWord == null)
            {
                return RedirectToAction("Index");
            }
            if (SearchWord != "")
            {
                string userid = User.Identity.GetUserId();
                string str = CrawlConfigValue + userid + @".config";
                _us.DeleteSubNode(str, int.Parse(SubCount));    //删除节点
                return RedirectToAction("Index");   //成功
            }
            return View();
        }
    }
}