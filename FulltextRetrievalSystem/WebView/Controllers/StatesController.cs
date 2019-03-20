using log4net;
using System.Reflection;
using System.Threading;
using System.Web.Mvc;
using WebCommon;
using System.Configuration;
using System.IO;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，所有状态的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class StatesController : Controller
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: States
        public ActionResult AllState()
        {
            ViewBag.PageName = "控制台";
            ViewBag.PageDescription = "功能提示";
            
            return View();
        }

        public ActionResult WebCrawlConf()
        {
            ViewBag.PageName = "Web爬虫维护";
            ViewBag.PageDescription = "查看";
            log.Info("管理员维护web爬虫");
            return RedirectToAction("Index", "WebCrawl");
        }
       
        public ActionResult FtpCrawlConf()
        {
            ViewBag.PageName = "Ftp爬虫维护";
            ViewBag.PageDescription = "查看";
            log.Info("管理员维护ftp爬虫");
            return RedirectToAction("Index", "FtpCrawl");
        }
        public ActionResult ShareCrawlConf()
        {
            ViewBag.PageName = "共享文件夹爬虫维护";
            ViewBag.PageDescription = "查看";
            log.Info("管理员维护共享文件夹爬虫");
            return RedirectToAction("Index", "ShareCrawl");
        }
        public ActionResult XapianConf()
        {
            string localdb = ConfigurationManager.AppSettings["LocalXapianSaveAddr"].ToString();
            ViewBag.PageName = "Xapian数据库配置";
            ViewBag.PageDescription = "查看";
            if (Directory.Exists(localdb) == false)//如果不存在就创建文件夹
            {
                Directory.CreateDirectory(localdb);
            }
            //展示数据库内容
            ViewBag.DBName = localdb;
            XapianManage xm = new XapianManage();
            XapianManage.XapManDBStats Dbstates = new XapianManage.XapManDBStats();
            xm.Show_db_stat(localdb, out Dbstates);         //调用数据库函数
            ViewBag.DBSize = Dbstates.DBSize;               //数据库大小
            ViewBag.DocAveLength = Dbstates.DocAveLength;   //数据库平均长度
            ViewBag.DocCount = Dbstates.DocCount;           //数据库文档计数
            ViewBag.DocLastId = Dbstates.DocLastId;         //数据库最后一个文章id

            return View();
        }


        public ActionResult PageResolveConf()
        {
            ViewBag.PageName = "网页解析配置";
            ViewBag.PageDescription = "查看并修改网页解析配置";
            log.Info("管理员配置网页解析");
            return RedirectToAction("Index", "PageResolve");
        }

        public ActionResult UsersManagement()
        {
            ViewBag.PageName = "用户维护";
            ViewBag.PageDescription = "维护用户信息";
            log.Info("管理员维护用户信息");
            return RedirectToAction("Index","UsersAdmin");
        }
        public ActionResult RolesManagement()
        {
            ViewBag.PageName = "角色管理";
            ViewBag.PageDescription = "管理角色信息";
            log.Info("管理员维护角色管理信息");
            return RedirectToAction("Index", "RolesAdmin");
        }
        public ActionResult UsrSubsManagement()
        {
            ViewBag.PageName = "订阅管理";
            ViewBag.PageDescription = "查看并修改您的订阅";
            return RedirectToAction("Index", "Subscribe");
        }
        public ActionResult UserProfile()
        {
            ViewBag.PageName = "个人资料";
            ViewBag.PageDescription = "查看并修改您的资料";
            return RedirectToAction("ChangePassword", "Manage");
        }
    }
}