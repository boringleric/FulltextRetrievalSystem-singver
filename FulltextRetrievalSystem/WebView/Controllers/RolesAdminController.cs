using WebView.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using log4net;

namespace WebView.Controllers
{
    /// <summary>
    /// 只限管理员使用，用户角色的增删改
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class RolesAdminController : Controller
    {
        // GET: RolesAdmin
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ActionResult Index()
        {
            ViewBag.PageName = "角色管理";
            ViewBag.PageDescription = "管理角色信息";
            return View(RoleManager.Roles);//显示角色清单
        }

        public RolesAdminController()
        {
        }

        public RolesAdminController(ApplicationUserManager userManager,
            ApplicationRoleManager roleManager)
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
            set
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
        //异步读取角色详情
        // GET: /Roles/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ViewBag.PageName = "角色详情";
            ViewBag.PageDescription = "管理角色详情";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var role = await RoleManager.FindByIdAsync(id);
            // 读取角色内的用户列表。
            var users = new List<ApplicationUser>();
            foreach (var user in UserManager.Users.ToList())
            {
                if (await UserManager.IsInRoleAsync(user.Id, role.Name))    //判断是否在角色中
                {
                    users.Add(user);
                }
            }
            ViewBag.Users = users;
            ViewBag.UserCount = users.Count();
            return View(role);
        }

        //读取角色创建
        // GET: /Roles/Create
        public ActionResult Create()
        {
            ViewBag.PageName = "创建角色";
            ViewBag.PageDescription = "创建角色详情";
            return View();
        }

        //异步写入角色创建
        // POST: /Roles/Create
        [HttpPost]
        public async Task<ActionResult> Create(RoleViewModel roleViewModel)
        {
            ViewBag.PageName = "创建角色";
            ViewBag.PageDescription = "创建角色详情";
            if (ModelState.IsValid)
            {
                var role = new IdentityRole(roleViewModel.Name);
                var roleresult = await RoleManager.CreateAsync(role);   //新增角色
                if (!roleresult.Succeeded)
                {
                    log.Warn("管理员添加用户角色失败：" + role.Name);
                    ModelState.AddModelError("", roleresult.Errors.First());
                    return View();
                }
                log.Warn("管理员添加用户角色："+role.Name);
                return RedirectToAction("Index");
            }
            return View();
        }

        //异步读取角色编辑
        // GET: /Roles/Edit/Admin
        public async Task<ActionResult> Edit(string id)
        {
            ViewBag.PageName = "编辑角色";
            ViewBag.PageDescription = "编辑角色详情";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null)
            {
                return HttpNotFound();
            }
            RoleViewModel roleModel = new RoleViewModel { Id = role.Id, Name = role.Name };
            return View(roleModel);
        }

        //异步写入角色编辑
        // POST: /Roles/Edit/5
        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Name,Id")] RoleViewModel roleModel)
        {
            ViewBag.PageName = "编辑角色";
            ViewBag.PageDescription = "编辑角色详情";
            if (ModelState.IsValid)
            {
                var role = await RoleManager.FindByIdAsync(roleModel.Id);   //查找角色
                role.Name = roleModel.Name;
                await RoleManager.UpdateAsync(role);                        //更新角色
                log.Warn("管理员更改用户角色：" + role.Name);
                return RedirectToAction("Index");
            }
            log.Warn("管理员更改用户角色失败！");
            return View();
        }

        //异步读取角色删除
        // GET: /Roles/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            ViewBag.PageName = "删除角色";
            ViewBag.PageDescription = "删除角色详情";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null)
            {
                return HttpNotFound();
            }
            return View(role);
        }

        //异步写入角色删除
        // POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id, string deleteUser)
        {
            ViewBag.PageName = "删除角色";
            ViewBag.PageDescription = "删除角色详情";
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                var role = await RoleManager.FindByIdAsync(id);     //查找角色
                if (role == null)
                {
                    return HttpNotFound();
                }
                IdentityResult result;
                if (deleteUser != null)
                {
                    result = await RoleManager.DeleteAsync(role);
                }
                else
                {
                    result = await RoleManager.DeleteAsync(role);
                }
                if (!result.Succeeded)
                {
                    log.Warn("管理员删除用户角色失败：" + role.Name);
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }
                log.Warn("管理员删除用户角色：" + role.Name);
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}