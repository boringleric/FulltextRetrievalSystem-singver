using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XapianPart
{
    /// <summary>
    /// Html规则设置和提取
    /// </summary>
    public class HtmlExtract
    {
        public struct RulesStruct
        {
            public string Link;         //html链接
            public string Rules;        //对应页面解析规则
        }
        public void InitialConfig(string path)
        {
            //判断是否存在文件
            if (File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length == 0)
                {
                    //存在但没有内容就初始化设置
                    try
                    {
                        var xDoc = new XDocument(new XElement("WebRules"));

                        //默认是缩进格式化的xml，而无须格式化设置
                        xDoc.Save(path);
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e);
                    }
                }

            }
            else
            {
                //不存在就初始化设置
                try
                {
                    var xDoc = new XDocument(new XElement("WebRules"));

                    //默认是缩进格式化的xml，而无须格式化设置
                    xDoc.Save(path);
                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                }
            }
        }

        #region 插入html规则节点

        public int InsertWebNode(string path, string Link, string Rules)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                XElement newNode = new XElement("Website",
                        new XElement("Link", Link),
                        new XElement("Rules", Rules));
                rootNode.Add(newNode);
                //保存节点
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion

        #region 修改html规则节点

        public int UpdateWebNode(string path,string oldlink, string Link, string Rules)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Website")
                                                    where target.Element("Link").Value == oldlink
                                                    select target;
                XElement node = targetNodes.First();
                //更新数据
                node.Element("Rules").SetValue(Rules);
                node.Element("Link").SetValue(Link);
                //保存
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        #endregion

        #region 显示html规则数据
        public int ShowWebXml(string path, out List<RulesStruct> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
//                path = @"D:\Backup\HtmlRulesBackup.config";
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("Website")
                                              select target;
                targetNodes = new List<RulesStruct>();
                foreach (var item in Nodes)
                {
                    //取值并重置
                    RulesStruct ns = new RulesStruct();
                    string Link = item.Element("Link").ToString();
                    Link = Link.Replace("</Link>", "");
                    Link = Link.Replace("<Link>", "");
                    ns.Link = Link;

                    string Rules = item.Element("Rules").ToString();
                    Rules = Rules.Replace("</Rules>", "");
                    Rules = Rules.Replace("<Rules>", "");
                    ns.Rules = Rules;

                    targetNodes.Add(ns);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }

        public int ShowWebXml(string path, string Link, out string Rules)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Website")
                                                    where target.Element("Link").Value == Link
                                                    select target;
                XElement node = targetNodes.First();

                Rules = node.Element("Rules").ToString();
                Rules = Rules.Replace("</Rules>", "");
                Rules = Rules.Replace("<Rules>", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        #endregion

        #region 删除html规则节点

        public int DeleteWebNode(string path, string Link)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Website")
                                                    where target.Element("Link").Value == Link
                                                    select target;
                XElement node = targetNodes.First();

                //将获得的节点集合中的每一个节点依次从它相应的父节点中删除
                targetNodes.Remove();
                //保存对xml的更改操作
                rootNode.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return 1;
        }
        #endregion


    }
}
