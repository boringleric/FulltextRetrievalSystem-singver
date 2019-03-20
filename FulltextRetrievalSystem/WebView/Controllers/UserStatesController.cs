using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebView.Controllers
{
    /// <summary>
    /// 限用户使用，个人信息的修改
    /// </summary>
    [Authorize]
    public class UserStatesController : Controller
    {
        // GET: UserStates
        public ActionResult UserCenter()
        {
            ViewBag.PageName = "控制台";
            ViewBag.PageDescription = "查看所有信息";
            return View();
        }

        public ActionResult UsrSubsManagement()
        {
            ViewBag.PageName = "订阅管理";
            ViewBag.PageDescription = "管理订阅内容";
            return RedirectToAction("Index", "Subscribe");
        }

        public ActionResult UserProfile()
        {
            ViewBag.PageName = "个人信息管理";
            ViewBag.PageDescription = "修改个人信息";
            return RedirectToAction("ChangePassword", "Manage");
        }
    }
}