using CrawlPart;
using XapianPart;
using System.Collections.Generic;

namespace WebCommon
{
    /// <summary>
    /// 对于一些定义结构体的重新包装
    /// </summary>
    public class ConfigOperation
    {
        public struct WebStruct
        {
            public int NetCount;
            public string NickName;
            public string Link;
        }

        public struct FtpStruct
        {
            public int FtpCount;
            public string NickName;
            public string Link;
            public string UserName;
            public string UserPassword;
        }

        public struct ShareStruct
        {
            public int ShareCount;
            public string NickName;
            public string StartIP;
            public string EndIP;
        }

        public struct RulesStruct
        {
            public string Link;
            public string Rules;
        }

        XmlOperation xmlop = new XmlOperation();

        HtmlExtract he = new HtmlExtract();

        #region CrawlPart 转发处理       
        /// <summary>
        /// 展示共享文件夹config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="num">节点编号</param>
        /// <param name="nickname">输出昵称</param>
        /// <param name="startip">起始ip</param>
        /// <param name="endip">结束ip</param>
        /// <returns></returns>
        public int ShowShareXmlSingleNode(string path, int num, out string nickname, out string startip,  out string endip)
        {        
           return xmlop.ShowShareXml(path, num, out nickname,out startip, out endip);
        }
        /// <summary>
        /// 展示共享文件夹config的全部信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes">所有节点信息</param>
        /// <returns></returns>
        public int ShowShareXml(string path, out List<ShareStruct> targetNodes)
        {
            targetNodes = new List<ShareStruct>();
            List<XmlOperation.ShareStruct> lxs;
            xmlop.ShowShareXml(path, out lxs);
            foreach (var item in lxs)
            {
                ShareStruct ss = new ShareStruct();
                ss.EndIP = item.EndIP;
                ss.NickName = item.NickName;
                ss.ShareCount = item.ShareCount;
                ss.StartIP = item.StartIP;
                targetNodes.Add(ss);
            }
            return 1;
        }
        /// <summary>
        /// 更新共享文件夹config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="num">节点编号</param>
        /// <param name="nickname">昵称</param>
        /// <param name="startip">起始ip</param>
        /// <param name="endip">结束ip</param>
        /// <returns></returns>
        public int UpdateShareNode(string path, int num, string nickname, string startip, string endip)
        {
           return xmlop.UpdateShareNode(path, num, nickname, startip, endip);
        }
        /// <summary>
        /// 删除共享文件夹config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="num">节点编号</param>
        /// <returns></returns>
        public int DeleteShareNode(string path, int num)
        {
            return xmlop.DeleteShareNode(path, num);
        }
        /// <summary>
        /// 创建共享文件夹config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="nickname">代号</param>
        /// <param name="startip">起始ip</param>
        /// <param name="endip">结束ip</param>
        /// <returns></returns>
        public int CreateShareNode(string path, string nickname, string startip, string endip)
        {
           return xmlop.InsertShareNode(path, nickname, startip, endip);
        }
        /// <summary>
        /// 展示Ftpconfig的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="ftpcount">节点编号</param>
        /// <param name="Link">Ftp链接</param>
        /// <param name="nickname">代号</param>
        /// <param name="username">Ftp登录名</param>
        /// <param name="password">Ftp密码</param>
        /// <returns></returns>
        public int ShowFtpXmlSingleNode(string path, int ftpcount, out string Link, out string nickname, out string username, out string password)
        {
           return xmlop.ShowFtpXml(path, ftpcount, out Link, out nickname,out username,out password);
        }
        /// <summary>
        /// 展示Ftpconfig的全部信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes">返回全部config信息</param>
        /// <returns></returns>
        public int ShowFtpXml(string path, out List<FtpStruct> targetNodes)
        {
            targetNodes = new List<FtpStruct>();
            List<XmlOperation.FtpStruct> lfs;
            xmlop.ShowFtpXml(path, out lfs);
            foreach (var item in lfs)
            {
                FtpStruct ss = new FtpStruct();
                ss.UserName = item.UserName;
                ss.NickName = item.NickName;
                ss.UserPassword = item.UserPassword;
                ss.Link = item.Link;
                ss.FtpCount = item.FtpCount;
                targetNodes.Add(ss);
            }
            return 1;
        }
        /// <summary>
        /// 更新Ftpconfig的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="ftpcount">节点编号</param>
        /// <param name="Link">Ftp链接</param>
        /// <param name="nickname">代号</param>
        /// <param name="username">Ftp登录名</param>
        /// <param name="password">Ftp密码</param>
        /// <returns></returns>
        public int UpdateFtpNode(string path, int ftpcount, string Link, string nickname, string username, string password)
        {
           return xmlop.UpdateFtpNode(path, ftpcount, nickname, Link , username, password);
        }
        /// <summary>
        /// 删除Ftpconfig的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="ftpcount">节点编号</param>
        /// <returns></returns>
        public int DeleteFtpNode(string path, int ftpcount)
        {
           return xmlop.DeleteFtpNode(path, ftpcount);
        }
        /// <summary>
        /// 创建Ftpconfig的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="Link">Ftp链接</param>
        /// <param name="nickname">代号</param>
        /// <param name="username">Ftp登录名</param>
        /// <param name="password">Ftp密码</param>
        /// <returns></returns>
        public int CreateFtpNode(string path, string Link, string nickname, string username, string password)
        {
           return xmlop.InsertFtpNode(path, nickname, Link, username, password);
        }
        /// <summary>
        /// 展示网页爬虫config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="webcount">节点编号</param>
        /// <param name="nickname">节点代号</param>
        /// <param name="Link">网页链接</param>
        /// <returns></returns>
        public int ShowWebXmlSingleNode(string path,int webcount,out string nickname, out string Link)
        {
           return xmlop.ShowWebXml(path, webcount, out nickname, out Link);
        }
        /// <summary>
        /// 展示网页爬虫config的全部信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes"></param>
        /// <returns></returns>
        public int ShowWebXml(string path, out List<WebStruct> targetNodes)
        {
            targetNodes = new List<WebStruct>();
            List<XmlOperation.WebStruct> lws;
            xmlop.ShowWebXml(path, out lws);
            foreach (var item in lws)
            {
                WebStruct ss = new WebStruct();
                ss.NetCount = item.NetCount;
                ss.NickName = item.NickName;
                ss.Link = item.Link;
                targetNodes.Add(ss);
            }
            return 1;
        }
        /// <summary>
        /// 更新网页爬虫config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="webcount">节点编号</param>
        /// <param name="nickname">节点代号</param>
        /// <param name="Link">网页链接</param>
        /// <returns></returns>
        public int UpdateWebNode(string path, int webcount, string nickname, string Link)
        {
           return xmlop.UpdateWebNode(path, webcount, nickname, Link);
        }
        /// <summary>
        /// 删除网页爬虫config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="webcount">节点编号</param>
        /// <returns></returns>
        public int DeleteWebNode(string path, int webcount)
        {
           return xmlop.DeleteWebNode(path, webcount);
        }
        /// <summary>
        ///创建网页爬虫config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="nickname">节点代号</param>
        /// <param name="Link">网页链接</param>
        /// <returns></returns>
        public int CreateWebNode(string path, string nickname, string Link)
        {
           return xmlop.InsertWebNode(path, nickname, Link);
        }
        #endregion

        #region Html转发处理
        /// <summary>
        /// 创建Html解析config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="Link">html链接</param>
        /// <param name="Rules">解析规则</param>
        /// <returns></returns>
        public int CreateHtmlNode(string path, string Link, string Rules)
        {
            return he.InsertWebNode(path, Link, Rules);
        }
        /// <summary>
        /// 显示Html解析config的全部信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="targetNodes">返回全部信息</param>
        /// <returns></returns>
        public int ShowHtmlNode(string path, out List<RulesStruct> targetNodes)
        {
            targetNodes = new List<RulesStruct>();
            List<HtmlExtract.RulesStruct> lhe = new List<HtmlExtract.RulesStruct>();
            he.ShowWebXml(path,out lhe);
            foreach (var item in lhe)
            {
                RulesStruct rs = new RulesStruct();
                rs.Link = item.Link;
                rs.Rules = item.Rules;
                targetNodes.Add(rs);
            }
            return 1;
        }
        /// <summary>
        /// 删除Html解析config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="link">html链接</param>
        /// <returns></returns>
        public int DeleteHtmlNode(string path, string link)
        {
            return he.DeleteWebNode(path, link);
        }
        /// <summary>
        /// 更新Html解析config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="Link">html链接</param>
        /// <param name="Rules">解析规则</param>
        /// <returns></returns>
        public int UpdateHtmlNode(string path,string oldlink, string Link, string Rules)
        {
            return he.UpdateWebNode(path, oldlink, Link, Rules);
        }
        /// <summary>
        /// 显示Html解析config的单点信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="Link">html链接</param>
        /// <param name="Rules">解析规则</param>
        /// <returns></returns>
        public int ShowHtmlSingleNode(string path, string Link, out string Rules)
        {
            return he.ShowWebXml(path, Link, out Rules);
        }
        #endregion

        #region FtpBan处理

        /// <summary>
        /// 插入FtpBan一个节点
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="banlist">要插入的banlist</param>
        /// <returns></returns>
        public int CreateBanNode(string path, string banlist)
        {         
            return xmlop.InsertBanNode(path, banlist);
        }
        /// <summary>
        /// 显示FtpBan的全部信息
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="banlist">banlist</param>
        /// <returns></returns>
        public int ShowBanNode(string path, out List<string> banlist)
        { 
            return xmlop.ShowBanXml(path, out banlist);
        }
        /// <summary>
        /// 删除FtpBan的一个节点
        /// </summary>
        /// <param name="path">config路径</param>
        /// <param name="banlist">要删除的banlist</param>
        /// <returns></returns>
        public int DeleteBanNode(string path, string banlist)
        {
            return xmlop.DeleteBanNode(path, banlist);
        }

        #endregion
    }
}
