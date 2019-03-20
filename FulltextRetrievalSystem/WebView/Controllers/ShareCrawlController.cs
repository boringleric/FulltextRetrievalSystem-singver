using System.Collections.Generic;
using System.Web.Mvc;
using WebCommon;
using System.Configuration;
using System.Net;
using log4net;
using System.IO;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，Share Config的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ShareCrawlController : Controller
    {
        // GET: ShareCrawl
        ConfigOperation _co = new ConfigOperation();
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalCrawlConfig"].ToString();
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);  //载入预设的配置路径
        public ActionResult Index()
        {
            ViewBag.PageName = "共享文件夹管理";
            ViewBag.PageDescription = "管理共享文件夹信息";
            string path = Path.GetDirectoryName(CrawlConfigValue);
            if (Directory.Exists(path) == false)//如果不存在就创建文件夹
            {
                Directory.CreateDirectory(path);
            }
            List<ConfigOperation.ShareStruct> ShareList;
            _co.ShowShareXml(CrawlConfigValue, out ShareList);  //获取共享文件夹信息
            ViewBag.ShareList = ShareList;
            return View();
        }
        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建共享文件夹网段";
            ViewBag.PageDescription = "创建共享文件夹网段详情";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult Create(string NickName, string StartIP, string EndIP)
        {
            ViewBag.PageName = "创建共享文件夹网段";
            ViewBag.PageDescription = "创建共享文件夹网段详情";
            if (StartIP != "" && EndIP != "")
            {
                IPAddress ipin, ipout;
                //判断是否属于ip范围，是否起始ip比结束ip小
                if (IPAddress.TryParse(StartIP, out ipin)&& IPAddress.TryParse(EndIP, out ipout)&& string.Compare(StartIP, EndIP) <= 0)
                {
                    int ret = _co.CreateShareNode(CrawlConfigValue, NickName, StartIP, EndIP);  //没问题则创建
                    log.Info("管理员创建共享文件夹网段：" + StartIP + " - "+ EndIP);
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "创建失败！请重新填写！");
                    log.Info("管理员创建共享文件夹网段失败：" + StartIP + " - " + EndIP);
                    return View();   
                }
            }
            ModelState.AddModelError("", "创建失败！请重新填写！");
            log.Info("管理员创建共享文件夹网段失败：" + StartIP + " - " + EndIP);
            return View();
        }

        //异步读取角色编辑
        // GET: /Roles/Edit/Admin

        public ActionResult Edit(string ShareCount)
        {
            ViewBag.PageName = "编辑共享文件夹网段";
            ViewBag.PageDescription = "编辑共享文件夹网段详情";

            string nickname;
            string startip;
            string endip;
            _co.ShowShareXmlSingleNode(CrawlConfigValue, int.Parse(ShareCount),out nickname, out startip, out endip);   //读取一个详情

            if (ShareCount == null)
            {
                return HttpNotFound();
            }

            ViewBag.StartIP = startip;
            ViewBag.EndIP = endip;
            ViewBag.NickName = nickname;
            return View();
        }
        //异步写入角色编辑
        // POST: /Roles/Edit/5
        [HttpPost]

        [ValidateAntiForgeryToken]
        public ActionResult Edit(string ShareCount, string StartIPEdit, string EndIPEdit, string NickNameEdit)
        {
            ViewBag.PageName = "编辑共享文件夹网段";
            ViewBag.PageDescription = "编辑共享文件夹网段详情";
            if (StartIPEdit != "" && EndIPEdit != "")
            {
                IPAddress ipin, ipout;
                //判断是否属于ip范围，是否起始ip比结束ip小
                if (IPAddress.TryParse(StartIPEdit, out ipin) && IPAddress.TryParse(EndIPEdit, out ipout)&&string.Compare(StartIPEdit, EndIPEdit) <= 0)
                {               
                    int ret = _co.UpdateShareNode(CrawlConfigValue, int.Parse(ShareCount), NickNameEdit, StartIPEdit, EndIPEdit);   //没问题则编辑成功
                    log.Info("管理员编辑共享文件夹网段：" + StartIPEdit + " - " + EndIPEdit);
                    return RedirectToAction("Index");   //成功
                    
                }
                else
                {
                    log.Info("管理员编辑共享文件夹网段失败：" + StartIPEdit + " - " + EndIPEdit);
                    return RedirectToAction("Index");
                }           
            }
            else
            {
                log.Info("管理员编辑共享文件夹网段失败：" + StartIPEdit + " - " + EndIPEdit);
                return RedirectToAction("Index");
            }
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult Delete(string ShareCount, string StartIP)
        {
            ViewBag.PageName = "删除共享文件夹网段";
            ViewBag.PageDescription = "删除共享文件夹网段详情";
            ViewBag.Link = StartIP;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string ShareCount, string StartIP)
        {
            ViewBag.PageName = "删除共享文件夹网段";
            ViewBag.PageDescription = "删除共享文件夹网段详情";
            ViewBag.Link = StartIP;
            if (ShareCount != "")
            {
                int ret = _co.DeleteShareNode(CrawlConfigValue, int.Parse(ShareCount)); //删除一个节点

                if (ret != 1)
                {
                    //失败
                    log.Info("管理员删除共享文件夹网段失败：" + StartIP);
                    return View();
                }
                else
                {
                    log.Info("管理员删除共享文件夹网段：" + StartIP);
                    return RedirectToAction("Index");   //成功
                }

            }

            return View();
        }
    }
}