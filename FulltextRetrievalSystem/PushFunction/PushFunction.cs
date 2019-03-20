using System;
using log4net;
using System.Configuration;

namespace PushFunction
{
    /// <summary>
    /// 推送处理方案
    /// </summary>
    public class PushFunction
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //读取预设内容
        private string MailServer = ConfigurationManager.AppSettings["MailServer"].ToString();                      //服务器地址
        private string MailAddr = ConfigurationManager.AppSettings["MailAddr"].ToString();                          //邮件发送者的email
        private string MailPsw = ConfigurationManager.AppSettings["MailPsw"].ToString();                            //邮件发送者的密码
        /// <summary>
        /// 根据用户邮箱和文本内容发送邮件
        /// </summary>
        /// <param name="useremail">用户邮箱</param>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        public int GetInfandPush(string useremail, string content)
        {
            EmailPush email = new EmailPush();
            email.mailFrom = MailAddr;
            email.mailPwd = MailPsw;
            email.isbodyHtml = false;    //是否是HTML
            email.host = MailServer;//如果是QQ邮箱则：smtp:qq.com,依次类推

            email.mailToArray = new string[] { useremail };             //接收者邮箱
            email.mailSubject = DateTime.Now.ToString() + "订阅推送";   //主题
            email.mailBody = content;   //更新内容

            if (email.Send())           //发送邮件
            {
                log.Info(useremail + "发送成功！");
                Console.WriteLine("发送成功！");//发送成功则提示返回当前页面；
            }
            else
            {
                log.Error(useremail + "发送不成功！");
                Console.WriteLine("发送不成功！");
            }
        
            return 1;
        }
    }
}