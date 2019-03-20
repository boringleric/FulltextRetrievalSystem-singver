using JiebaNet.Analyser;
using JiebaNet.Segmenter;

namespace XapianPart
{
    /// <summary>
    /// 中文分词，使用jieba.net
    /// </summary>
    public class ChineseSeg
    {
        /// <summary>
        /// jieba.net分词，为检索专用，分的更细
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <param name="strout">输出字符串</param>
        /// <returns></returns>
        public string JiebaSeg(string str)
        {
            var segmenter = new JiebaSegmenter();
            var segments = segmenter.CutForSearch(str);         
            return string.Join(" ", segments);
        }
        /// <summary>
        /// jieba.net分词，不用来检索
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <param name="strout">输出字符串</param>
        /// <returns></returns>
        public string JiebaSegnotSearch(string str)
        {
            var segmenter = new JiebaSegmenter();
            var segments = segmenter.Cut(str);
            return string.Join(" ", segments);
        }
        /// <summary>
        /// jieba.net分词，找关键词
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <param name="strout">输出10个关键词</param>
        /// <returns></returns>
        public string JiebaKey(string str)
        {
           string strout = "";
              var extractor = new TfidfExtractor().ExtractTags(str, 10, Constants.NounAndVerbPos);
            // 提取前十个仅包含名词和动词的关键词
            foreach (var keyword in extractor)
            {
                strout += (keyword + " ");
            }
            return strout;
        }
    }
}
