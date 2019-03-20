using log4net;
using WebCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebView.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PageResolveController : Controller
    {
        // GET: PageResolve
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string CrawlConfigValue = ConfigurationManager.AppSettings["PageResolveConfig"].ToString(); //载入预设
        ConfigOperation _co = new ConfigOperation();
        private static string linkbackup = "";
        public ActionResult Index()
        {
            ViewBag.PageName = "Html规则管理";
            ViewBag.PageDescription = "管理Html规则信息";
            List<ConfigOperation.RulesStruct> RuleList;
            _co.ShowHtmlNode(CrawlConfigValue, out RuleList);   //获取html规则信息
            ViewBag.RuleList = RuleList;
            return View();
        }
        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建Html规则";
            ViewBag.PageDescription = "创建Html规则详情";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult Create(string Link, string Rules)
        {
            ViewBag.PageName = "创建Html规则";
            ViewBag.PageDescription = "创建Html规则详情";
            if (Link != "" && Rules != "")
            {
                //若满足不为空条件
                int ret = _co.CreateHtmlNode(CrawlConfigValue, Link, Rules);    //创建规则

                if (ret != 1)
                {
                    log.Info("管理员创建Html规则失败：" + Link + "规则：" + Rules);
                    //失败
                    return View();
                }
                else
                {
                    log.Info("管理员创建Html规则："+Link+ "规则："+Rules);
                    return RedirectToAction("Index");
                }

            }
            return RedirectToAction("Index");
        }

        //异步读取角色编辑
        // GET: /Roles/Edit/Admin

        public ActionResult Edit(string link)
        {
            ViewBag.PageName = "编辑Html规则";
            ViewBag.PageDescription = "编辑Html规则";
            string rules;
            _co.ShowHtmlSingleNode(CrawlConfigValue, link, out rules);      //展示单条信息
            linkbackup = link;
            ViewBag.Link = link;
            ViewBag.Rules = rules;

            return View();
        }
        //异步写入角色编辑
        // POST: /Roles/Edit/5
        [HttpPost]

        [ValidateAntiForgeryToken]
        public ActionResult Edit(string LinkEdit, string RulesEdit)
        {
            ViewBag.PageName = "编辑Html规则";
            ViewBag.PageDescription = "编辑Html规则";

            if (LinkEdit != "" && RulesEdit != "")
            {
                int ret = _co.UpdateHtmlNode(CrawlConfigValue, linkbackup,LinkEdit, RulesEdit);     //更新html节点信息
                if (ret != 1)
                {
                    //失败
                    log.Info("管理员编辑Html规则失败：" + LinkEdit + "规则：" + RulesEdit);
                    return View();
                }
                else
                {
                    log.Info("管理员编辑Html规则：" + LinkEdit + "规则：" + RulesEdit);
                    return RedirectToAction("Index");   //成功
                }

            }
            return View();
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult Delete(string Link)
        {
            ViewBag.PageName = "删除Html规则";
            ViewBag.PageDescription = "删除Html规则";
            ViewBag.Link = Link;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string Link)
        {
            ViewBag.PageName = "删除Html规则";
            ViewBag.PageDescription = "删除Html规则";
            ViewBag.Link = Link;

            int ret = _co.DeleteHtmlNode(CrawlConfigValue, Link);   //删除html节点

            if (ret != 1)
            {
                //失败
                log.Info("管理员删除Html规则失败：" + Link);
                return View();
            }
            else
            {
                log.Info("管理员删除Html规则：" + Link);
                return RedirectToAction("Index");   //成功
            }

        }
    }
}