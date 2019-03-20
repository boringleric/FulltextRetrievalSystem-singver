using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PushFunction
{
    /// <summary>
    /// 用户订阅信息配置文件
    /// </summary>
    public class UserSubscribe
    {
        public struct SubStruct
        {
            public string SearchWord;
            public string AddTime;
            public int SubCount;
        }
        /// <summary>
        /// 初始化用户订阅配置文件
        /// </summary>
        /// <param name="path">配置文件存放路径</param>
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
                        var xDoc = new XDocument(new XElement("Subscribe"));
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
                    var xDoc = new XDocument(new XElement("Subscribe"));
                    //默认是缩进格式化的xml，而无须格式化设置
                    xDoc.Save(path);
                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                }
            }
        }
        #region 插入Sub节点

        public int InsertSubNode(string path, string SubWord)
        {
            InitialConfig(path);
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Word")
                                                    select target;

                XElement node1 = targetNodes.LastOrDefault();
                if (node1 == null)
                {
                    //无数据插入
                    DateTime DateTimestart = DateTime.Now;
                    XElement newNode = new XElement("Word", new XAttribute("SearchWord", SubWord),
                        new XElement("SubCount", 1),
                        new XElement("AddTime", DateTimestart.ToString("yyyy/MM/dd")));
                    rootNode.Add(newNode);
                }
                else
                {
                    //有数据新增
                    XElement a = (XElement)node1.FirstNode;
                    int value = int.Parse(a.Value);
                    DateTime DateTimestart = DateTime.Now;
                    XElement newNode = new XElement("Word", new XAttribute("SearchWord", SubWord),
                        new XElement("SubCount", ++value),
                        new XElement("AddTime", DateTimestart.ToString("yyyy/MM/dd")));
                    rootNode.Add(newNode);
                }
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

        #region 修改Sub节点

        public int UpdateSubNode(string path, int SubCount, string SubWord)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Word")
                                                    where target.Element("SubCount").Value == SubCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();
                //更新数据
                node.Attribute("SearchWord").SetValue(SubWord);
                DateTime DateTimestart = DateTime.Now;
                node.Element("AddTime").SetValue(DateTimestart.ToString("yyyy/MM/dd"));
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
        public int UpdateSubNodeonlytime(string path)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Word")
                                                    select target;
                foreach (var item in targetNodes)
                {
                    DateTime DateTimestart = DateTime.Now;
                    //更新数据
                    item.Element("AddTime").SetValue(DateTimestart.ToString("yyyy/MM/dd"));
                }             

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

        #region 显示Sub数据

        public int ShowSubXml(string path, out List<SubStruct> targetNodes)
        {
            try
            {
                InitialConfig(path);
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> Nodes = from target in rootNode.Descendants("Word")
                                              select target;
                targetNodes = new List<SubStruct>();
                foreach (var item in Nodes)
                {
                    //取值并重置
                    SubStruct ns = new SubStruct();
                    string AddTime = item.Element("AddTime").ToString();
                    AddTime = AddTime.Replace("</AddTime>", "");
                    AddTime = AddTime.Replace("<AddTime>", "");
                    ns.AddTime = AddTime;

                    string SubCount = item.Element("SubCount").ToString();
                    SubCount = SubCount.Replace("</SubCount>", "");
                    SubCount = SubCount.Replace("<SubCount>", "");
                    ns.SubCount = int.Parse(SubCount);

                    string SearchWord = item.Attribute("SearchWord").ToString();
                    SearchWord = SearchWord.Replace("\"", "");
                    SearchWord = SearchWord.Replace("SearchWord=", "");
                    ns.SearchWord = SearchWord;

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
        
        public int ShowSubXml(string path, int SubCount, out string SubWord, out string AddTime)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Word")
                                                    where target.Element("SubCount").Value == SubCount.ToString()
                                                    select target;
                XElement node = targetNodes.First();

                AddTime = node.Element("AddTime").ToString();
                AddTime = AddTime.Replace("</AddTime>", "");
                AddTime = AddTime.Replace("<AddTime>", "");

                SubWord = node.Attribute("SubWord").ToString();
                SubWord = SubWord.Replace("\"", "");
                SubWord = SubWord.Replace("SubWord=", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 1;
        }
        #endregion

        #region 删除Sub节点

        public int DeleteSubNode(string path, int SubCount)
        {
            try
            {
                //定义并从xml文件中加载节点（根节点）
                XElement rootNode = XElement.Load(path);

                //查询语句: 获得根节点下Net子节点
                IEnumerable<XElement> targetNodes = from target in rootNode.Descendants("Word")
                                                    where target.Element("SubCount").Value == SubCount.ToString()
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