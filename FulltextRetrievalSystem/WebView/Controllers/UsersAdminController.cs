using WebView.Models;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebView.Configuration;
using System.Configuration;
using log4net;
using System.Text.RegularExpressions;
using System;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，用户信息的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class UsersAdminController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: UsersAdmin
        public async Task<ActionResult> Index()
        {
            ViewBag.PageName = "用户维护";
            ViewBag.PageDescription = "查看用户信息";
            return View(await UserManager.Users.ToListAsync());
        }

        public UsersAdminController()
        {
        }

        public UsersAdminController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        //异步读取用户详情
        //GET: /Users/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ViewBag.PageName = "用户详情";
            ViewBag.PageDescription = "查看用户详情";
            //用户为空时返回400错误
            if (id == null)
            {
                log.Error("用户为空!");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //按Id查找用户
            var user = await UserManager.FindByIdAsync(id);
            ViewBag.RoleNames = await UserManager.GetRolesAsync(user.Id);
            return View(user);
        }

        //异步读取用户创建
        //GET:/Users/Create
        public async Task<ActionResult> Create()
        {
            //读取角色列表
            ViewBag.PageName = "创建用户";
            ViewBag.PageDescription = "创建用户信息";
            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            return View();
        }
        //
        //异步写入用户创建
        // POST: /Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel, params string[] selectedRoles)
        {
            ViewBag.PageName = "创建用户";
            ViewBag.PageDescription = "创建用户信息";
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = userViewModel.Email, Email = userViewModel.Email };
                var adminresult = await UserManager.CreateAsync(user, userViewModel.Password);

                //创建用户成功
                if (adminresult.Succeeded)
                {
                    var mailConfig = (MailConfig)ConfigurationManager.GetSection("application/mail");
                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking this link: <a href=\"" + callbackUrl + "\">link</a>，或者复制此链接输入到您的浏览器地址栏" + callbackUrl);
                    //确认邮件
                    if (selectedRoles != null)
                    {
                        var result = await UserManager.AddToRolesAsync(user.Id, selectedRoles);
                        if (!result.Succeeded)
                        {
                            ModelState.AddModelError("", result.Errors.First());
                            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
                            return View();
                        }
                    }
                }
                else
                {
                    log.Warn("创建用户失败：用户Email：" + userViewModel.Email);
                    ModelState.AddModelError("", adminresult.Errors.First());
                    ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
                    return View();
                }
                log.Warn("创建用户成功：用户Email："+ userViewModel.Email);
                return RedirectToAction("Index");
            }
            ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
            return View();
        }

        //读取用户编辑
        // GET: /Users/Edit/1
        public async Task<ActionResult> Edit(string id)
        {
            ViewBag.PageName = "用户编辑";
            ViewBag.PageDescription = "编辑用户信息";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            var userRoles = await UserManager.GetRolesAsync(user.Id);   //读取用户角色
            return View(new EditUserViewModel()
            {
                Id = user.Id,
                Email = user.Email,
                RolesList = RoleManager.Roles.ToList().Select(x => new SelectListItem()
                {
                    Selected = userRoles.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                })
            });
        }
        //
        //写入用户编辑
        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Email,Id")]EditUserViewModel editUser, params string[] selectedRole)
        {
            ViewBag.PageName = "用户编辑";
            ViewBag.PageDescription = "编辑用户信息";
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(editUser.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }
                user.UserName = editUser.Email;
                user.Email = editUser.Email;

                var userRoles = await UserManager.GetRolesAsync(user.Id);
                selectedRole = selectedRole ?? new string[] { };

                //将用户添加到指定的角色
                var result = await UserManager.AddToRolesAsync(user.Id, selectedRole.Except(userRoles).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }

                //将用户从指定的角色中删除
                result = await UserManager.RemoveFromRolesAsync(user.Id, userRoles.Except(selectedRole).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }

                log.Warn("编辑用户：用户ID：" + editUser.Id);
                return RedirectToAction("Index");
            }
            log.Warn("编辑用户失败：用户ID：" + editUser.Id);
            ModelState.AddModelError("", "操作失败。");
            return View();
        }

        //读取用户删除
        // GET: /Users/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            ViewBag.PageName = "用户删除";
            ViewBag.PageDescription = "删除用户信息";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
        //
        //写入角色删除
        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            ViewBag.PageName = "用户删除";
            ViewBag.PageDescription = "删除用户信息";
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    return HttpNotFound();
                }
                var result = await UserManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    log.Warn("删除用户失败：用户：" + user);
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }
                log.Warn("删除用户：" + user);
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}