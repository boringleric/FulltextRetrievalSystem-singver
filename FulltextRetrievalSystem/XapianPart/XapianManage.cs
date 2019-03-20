using System;
using System.IO;

namespace XapianPart
{
    /// <summary>
    /// xapian数据库管理
    /// </summary>
    public class XapianManage
    {
        public struct XapDBStats
        {
            public string DBSize;              //数据库大小
            public uint DocCount;              //文档数量
            public double DocAveLength;        //平均长度
            public uint DocLastId;             //最后一个id
        }
        /// <summary>
        /// 数据库大小转换
        /// </summary>
        /// <param name="len">数据长度</param>
        /// <returns></returns>
        public string ConvertBytes(long len)
        {
            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
        /// <summary>
        /// 展示数据库状态
        /// </summary>
        /// <param name="dbname">数据库本地链接</param>
        /// <param name="XapDBStats">数据库的状态</param>
        /// <returns></returns>
        public int Show_db_stats(string dbname, out XapDBStats XapDBStats)
        {
            XapDBStats.DBSize = "0";
            XapDBStats.DocAveLength = 0;
            XapDBStats.DocCount = 0;
            XapDBStats.DocLastId = 0;
            string DBName = dbname;
            try
            {

                Xapian.Database database;
                long len = 0;
                database = new Xapian.Database(DBName);

                DirectoryInfo TheFolder = new DirectoryInfo(DBName);
                foreach (FileInfo fi in TheFolder.GetFiles())
                {
                    len += fi.Length;
                }

                XapDBStats.DBSize = ConvertBytes(len);              //数据库大小
                XapDBStats.DocAveLength = database.GetAvLength();   //平均长度
                XapDBStats.DocCount = database.GetDocCount();       //数据条数
                XapDBStats.DocLastId = database.GetLastDocId();     //最后一条文档

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception: " + e.ToString());
                return -1;
            }

            return 0;
        }
    }
}
