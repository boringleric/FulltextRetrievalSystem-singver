# FulltextRetrievalSystem
毕业设计，全文检索系统代码 - 非分布式

**需要注意：VS开启IIS64位版本！**

使用的库有：
中文解析：Jieba.net, Web爬虫：Abot, FTP爬虫：Winscp, 局域网爬虫：Netapi32.dll, excel解析：NPOI, word和ppt解析：DotMaysWind.Office, pdf解析：itextsharp, 日志：log4net  

LocalDB存放着本地检索数据库文件夹，有部分爬虫爬取的测试数据在里面。  

FulltextRetrievalSystem/packages/Xapian.1.2.23/的_XapianSharp.dll和zlib1.dll需手动复制到执行文件夹下，修改的xapian代码见本人的XapianModified项目；除此之外，jieba.net的resource文件夹也要复制到同一文件夹下，否则中文解析会出错。  

CrawlPart是爬虫操作相关文件夹，PushFunction是邮件推送文件夹，ScheduledTask是任务爬虫和任务推送文件夹，XapianPart是数据库文件夹，WebView是网站，WebCommon是数据交互层。  

如果要配置路径之类的东西，请注意修改web.config或者app.config的对应内容！  

登录管理员账号admin@123.com，密码是Pa$$w0rd，随便玩，反正没啥权限……  

代码天长日久没动过了，把它们拿出来通通风，如果有bug，这是很可能的，请见谅！
