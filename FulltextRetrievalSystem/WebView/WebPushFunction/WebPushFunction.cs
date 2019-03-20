using System;
using System.Collections.Generic;
using System.Linq;
using WebView.Models;
using WebCommon;
using log4net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Configuration;
using PushFunction;

namespace WebView.WebPushFunction
{
    public class WebPushFunction
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //读取预设内容
        private string CrawlConfigValue = ConfigurationManager.AppSettings["LocalSubsSaveAddr"].ToString();

        public int GetUserSubandPush()
        {
            //用户Email和邮件内容
            string useremail = "";
            string mailcontent = "";
            //在数据库中读取角色与用户信息
            var context = new ApplicationDbContext();          
            List<ApplicationUser> allUsers = context.Users.ToList();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            //获得管理员角色
            ICollection<IdentityUserRole> Adminrole = roleManager.FindByName("Admin").Users;
            //获得等级不同角色
            ICollection<IdentityUserRole> Secrole = roleManager.FindByName("Sec").Users;
            foreach (var item in allUsers)  //轮询用户
            {
                int sec = 0;
                bool flag = false;
                useremail = item.Email;
                if (useremail == "admin@123.com"||item.EmailConfirmed == false)
                //if (item.EmailConfirmed == false)
                {
                    continue;
                }
               
                
                foreach (var rolea in Adminrole)    //是否管理员
                {
                    var find  = string.Compare(rolea.UserId, item.Id);  //依次查询
                    if (find == 0)
                    {
                        sec = 1;        //若管理员，则等级不同
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    foreach (var rolea in Secrole)  //是否等级不同
                    {
                        var find = string.Compare(rolea.UserId, item.Id);   //依次查询
                        if (find == 0)
                        {
                            sec = 1;    //若等级不同，则终止
                            break;
                        }
                    }
                }

                string id = item.Id;
                UserSubscribe us = new UserSubscribe();
                XapianLogic xl = new XapianLogic();
                List<UserSubscribe.SubStruct> lus = new List<UserSubscribe.SubStruct>();
                string str = CrawlConfigValue + id + @".config";
                us.ShowSubXml(str,out lus);         //查看该用户是否有订阅
                if (lus.Count == 0)
                {
                    continue;
                }
                else
                {
                    foreach (var word in lus)
                    {
                        string tmp =  xl.SearchForPush(word.SearchWord, sec, word.AddTime); //检查数据库内容
                        if (tmp!=null || tmp!="")
                        {
                            mailcontent = mailcontent + tmp;    //获取内容更新
                        }
                    }
                }
                us.UpdateSubNodeonlytime(str);      //更新订阅查询时间
                if (mailcontent == "")              //检查是否有内容，无内容不推送
                {
                    Console.WriteLine("no update");
                    continue;
                }
                PushFunction.PushFunction ppf = new PushFunction.PushFunction();
                ppf.GetInfandPush(useremail, mailcontent);  //按照用户邮箱和内容推送
            }
            return 1;
        }
    }
}