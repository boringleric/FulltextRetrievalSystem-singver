namespace WebCommon
{
    public class XapianManage
    {
        public struct XapManDBStats
        {
            public string DBSize;              //数据库大小
            public uint DocCount;              //文档数量
            public double DocAveLength;        //平均长度
            public uint DocLastId;             //最后一个id
        }
        /// <summary>
        /// 展示xapian状态
        /// </summary>
        /// <param name="dbname">数据库链接</param>
        /// <param name="XapDBStats">返回的结构体</param>
        /// <returns></returns>
        public int Show_db_stat(string dbname, out XapManDBStats XapDBStats)
        {
            XapianPart.XapianManage.XapDBStats XapDBStat = new XapianPart.XapianManage.XapDBStats();
            XapianPart.XapianManage px = new XapianPart.XapianManage();
            int ret = px.Show_db_stats(dbname, out XapDBStat);   //调用数据库检查函数，获得数据库相关信息                 
            if (ret == -1)
            {
                XapDBStats.DBSize = "0";        //数据库大小
                XapDBStats.DocAveLength = 0;    //文档数量
                XapDBStats.DocCount = 0;        //平均长度
                XapDBStats.DocLastId = 0;       //最后一个id
                return 0;
            }
            XapDBStats.DBSize = XapDBStat.DBSize;
            XapDBStats.DocAveLength = XapDBStat.DocAveLength;
            XapDBStats.DocCount = XapDBStat.DocCount;
            XapDBStats.DocLastId = XapDBStat.DocLastId;
            return 1;
        }
    }
}
