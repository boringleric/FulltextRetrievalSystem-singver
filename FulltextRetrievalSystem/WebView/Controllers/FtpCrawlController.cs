using log4net;
using WebCommon;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using System.Xml;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，Ftp Config的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class FtpCrawlController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: FtpCrawl      
        ConfigOperation _co = new ConfigOperation();
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalCrawlConfig"].ToString(); //载入预设的配置路径

        public ActionResult Index()
        {
            ViewBag.PageName = "FtpURI管理";
            ViewBag.PageDescription = "管理FtpURI信息";
            string path = Path.GetDirectoryName(CrawlConfigValue);
            if (Directory.Exists(path) == false)//如果不存在就创建文件夹
            {
                Directory.CreateDirectory(path);
            }
            List<ConfigOperation.FtpStruct> FtpList;
            List<string> Banlist;
            _co.ShowFtpXml(CrawlConfigValue, out FtpList);      //展示ftp链接表
            _co.ShowBanNode(CrawlConfigValue, out Banlist);     //展示ftp禁用表
            ViewBag.FtpList = FtpList;
            ViewBag.BanList = Banlist;
            return View();
        }
        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建FtpURI";
            ViewBag.PageDescription = "创建FtpURI详情";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult Create(string Link, string NickName, string UserName, string Password)
        {
            ViewBag.PageName = "创建FtpURI";
            ViewBag.PageDescription = "创建FtpURI详情";
            List<ConfigOperation.FtpStruct> FtpList;
            _co.ShowFtpXml(CrawlConfigValue, out FtpList);  //载入全部连接，检查是否重复
            if (Link != "" && UserName != "")
            {
                //先检查是否满足条件
                foreach (var item in FtpList)
                {
                    //再检查是否重复
                    if (item.Link == Link)
                    {
                        ModelState.AddModelError("", "创建失败！请重新填写！");
                        return View();
                    }
                }
                int ret = _co.CreateFtpNode(CrawlConfigValue, Link, NickName, UserName, Password);  //没问题就创建

                if (ret != 1)
                {
                    //失败
                    ModelState.AddModelError("", "创建失败！请重新填写！");
                    log.Info("管理员创建FtpURI失败：" + Link + " UserName：" + UserName + " Password:" + Password);
                    return View();
                }
                else
                {
                    log.Info("管理员创建FtpURI：" + Link + " UserName：" + UserName+ " Password:"+Password);
                    return RedirectToAction("Index");   
                }

            }
            ModelState.AddModelError("", "创建失败！请重新填写！");
            log.Info("管理员创建FtpURI失败：" + Link + " UserName：" + UserName + " Password:" + Password);
            return View();
        }

        //异步读取角色编辑
        // GET: /Roles/Edit/Admin

        public ActionResult Edit(string FtpCount)
        {
            ViewBag.PageName = "编辑FtpURI";
            ViewBag.PageDescription = "编辑FtpURI详情";
            string username;
            string link;
            string password;
            string nickname;
            _co.ShowFtpXmlSingleNode(CrawlConfigValue,int.Parse(FtpCount),out link,out nickname,out username,out password);   //展示ftp节点

            if (FtpCount == null)
            {
                return HttpNotFound();
            }

            ViewBag.Link = link;
            ViewBag.NickName = nickname;
            ViewBag.Password = password;
            ViewBag.UserName = username;
            return View();
        }
        //异步写入角色编辑
        // POST: /Roles/Edit/5
        [HttpPost]

        [ValidateAntiForgeryToken]
        public ActionResult Edit(string FtpCount, string LinkEdit, string NickNameEdit, string UserNameEdit, string PasswordEdit)
        {
            ViewBag.PageName = "编辑FtpURI";
            ViewBag.PageDescription = "编辑FtpURI详情";

            if (LinkEdit != "" && NickNameEdit != "")
            {
                int ret = _co.UpdateFtpNode(CrawlConfigValue, int.Parse(FtpCount), LinkEdit, NickNameEdit, UserNameEdit, PasswordEdit); //若编辑没问题，就进行修改
                log.Info("管理员编辑FtpURI：" + LinkEdit + " UserName：" + UserNameEdit + " Password:" + PasswordEdit);
                return RedirectToAction("Index");   //成功
            }
            log.Info("管理员编辑FtpURI失败：" + LinkEdit + " UserName：" + UserNameEdit + " Password:" + PasswordEdit);
            return RedirectToAction("Index");
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult Delete(string FtpCount,string Link)
        {
            ViewBag.PageName = "删除FtpURI";
            ViewBag.PageDescription = "删除FtpURI详情";
            ViewBag.Link = Link;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string FtpCount, string Link)
        {
            ViewBag.PageName = "删除FtpURI";
            ViewBag.PageDescription = "删除FtpURI详情";
            ViewBag.Link = FtpCount;

            int ret = _co.DeleteFtpNode(CrawlConfigValue, int.Parse(FtpCount));     //删除一个节点

            if (ret != 1)
            {
                //失败
                log.Info("管理员删除FtpURI失败：" + Link);
                return View();
            }
            else
            {
                log.Info("管理员删除FtpURI：" + Link);
                return RedirectToAction("Index");   //成功
            }

        }

        //读取角色创建
        // GET: /Roles/Create
        public ActionResult CreateBan()
        {
            ViewBag.PageName = "设置Ftp禁爬";
            ViewBag.PageDescription = "设置Ftp禁爬文件";
            return View();
        }
        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public ActionResult CreateBan(string Link)
        {
            ViewBag.PageName = "设置Ftp禁爬";
            ViewBag.PageDescription = "设置Ftp禁爬文件";
            List<string> BanList;
            _co.ShowBanNode(CrawlConfigValue, out BanList);     //读取禁爬文件夹列表
            if (Link != "")
            {
                foreach (var item in BanList)
                {
                    if (item == Link)
                    {
                        return View();
                    }
                }
                int ret = _co.CreateBanNode(CrawlConfigValue, Link);    //创建禁止爬虫文件夹

                if (ret != 1)
                {
                    //失败
                    log.Info("管理员创建FtpBan失败：" + Link);
                    return View();
                }
                else
                {
                    log.Info("管理员创建FtpBan：" + Link);
                    return RedirectToAction("Index");
                }

            }
            log.Info("管理员创建FtpBan失败：" + Link);
            return View();
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public ActionResult DeleteBan(string item)
        {
            ViewBag.PageName = "删除Ftp禁爬文件夹";
            ViewBag.PageDescription = "删除Ftp禁爬文件夹详情";
            ViewBag.Link = item;
            return View();
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("DeleteBan")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBanConfirmed(string item)
        {
            ViewBag.PageName = "删除Ftp禁爬文件夹";
            ViewBag.PageDescription = "删除Ftp禁爬文件夹详情";
            ViewBag.Link = item;

            int ret = _co.DeleteBanNode(CrawlConfigValue, item);    //删除禁止爬虫文件夹

            if (ret != 1)
            {
                //失败
                log.Info("管理员删除FtpBan失败：" + item);
                return View();
            }
            else
            {
                log.Info("管理员删除FtpBan：" + item);
                return RedirectToAction("Index");   //成功
            }

        }
    }
}