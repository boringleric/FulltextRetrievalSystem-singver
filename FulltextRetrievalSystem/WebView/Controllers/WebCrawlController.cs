using log4net;
using WebCommon;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，Web爬虫 Config的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class WebCrawlController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: WebCrawl
        ConfigOperation _co = new ConfigOperation();
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalCrawlConfig"].ToString();  //载入预设的配置路径

        public ActionResult Index()
        {
            ViewBag.PageName = "WebURI管理";
            ViewBag.PageDescription = "管理WebURI信息";
            string path =  Path.GetDirectoryName(CrawlConfigValue);
            if (Directory.Exists(path) == false)//如果不存在就创建文件夹
            {
                Directory.CreateDirectory(path);
            }
            List<ConfigOperation.WebStruct> WebList;
            _co.ShowWebXml(CrawlConfigValue, out WebList);
            ViewBag.WebList = WebList;
            return View();
        }
        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建WebURI";
            ViewBag.PageDescription = "创建WebURI详情";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult Create(string Link,string NickName)
        {
            ViewBag.PageName = "创建WebURI";
            ViewBag.PageDescription = "创建WebURI详情";

            if (Link != "" && Link.Contains("http"))
            {
                List<ConfigOperation.WebStruct> WebList;
                _co.ShowWebXml(CrawlConfigValue, out WebList);      //先调用全部信息
                foreach (var item in WebList)
                {
                    if (item.Link == Link)                          //查看有没有写重的
                    {
                        ModelState.AddModelError("", "链接错误！请重新填写！");
                        
                            return View();
                                                 
                    }
                }
                int ret = _co.CreateWebNode(CrawlConfigValue, NickName, Link);  //一切通过就新增

                if (ret != 1)
                {
                    //失败
                    ModelState.AddModelError("", "新增错误！请重新填写！");
                    
                        return View();
                   
                }
                else
                {
                    log.Info("管理员创建WebURI：" + Link);
                    return RedirectToAction("Index");   //成功
                }
                
            }
            else
            {
                ModelState.AddModelError("", "创建失败！请重新填写！");
                log.Info("管理员创建WebURI失败：" + Link);
            }
            return View();
        }

        //异步读取角色编辑
        // GET: /Roles/Edit/Admin

        public ActionResult Edit(string NetCount)
        {
            ViewBag.PageName = "编辑WebURI";
            ViewBag.PageDescription = "编辑WebURI详情";
            string NickName;
            string Link;
            int ret = _co.ShowWebXmlSingleNode(CrawlConfigValue, int.Parse(NetCount), out NickName,out Link); //展示单条记录
            if (ret == 0)
            {
                return HttpNotFound();
            }

            ViewBag.Link = Link;
            ViewBag.NickName = NickName;
            return View();
        }
        //异步写入角色编辑
        // POST: /Roles/Edit/5
        [HttpPost]

        [ValidateAntiForgeryToken]
        public ActionResult Edit(string NetCount, string LinkEdit,string NickNameEdit)
        {
            ViewBag.PageName = "编辑WebURI";
            ViewBag.PageDescription = "编辑WebURI详情";
            //验证是否满足条件
            if (LinkEdit != ""&& LinkEdit.Contains("http"))
            {
                int ret = _co.UpdateWebNode(CrawlConfigValue, int.Parse(NetCount), NickNameEdit, LinkEdit); //没问题则保存
                log.Info("管理员编辑WebURI：" + LinkEdit);
                return RedirectToAction("Index");   //成功
            }
            log.Info("管理员编辑WebURI失败：" + LinkEdit);
            return RedirectToAction("Index");
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult Delete(string NetCount,string Link)
        {
            ViewBag.PageName = "删除WebURI";
            ViewBag.PageDescription = "删除WebURI详情";
            ViewBag.Link = Link;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string NetCount, string Link)
        {
            ViewBag.PageName = "删除WebURI";
            ViewBag.PageDescription = "删除WebURI详情";
            ViewBag.Link = Link;

            int ret = _co.DeleteWebNode(CrawlConfigValue, int.Parse(NetCount)); //删除记录

            if (ret != 1)
            {
                //失败
                log.Info("管理员删除WebURI失败：" + Link);
                return View();
            }
            else
            {
                log.Info("管理员删除WebURI：" + Link);
                return RedirectToAction("Index");   //成功
            }
        }
    }
}