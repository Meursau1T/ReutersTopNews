using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace ReutersTopNews
{
    class Program
    {
        public class Settings{
            static public string topNewsUrl = "https://www.reuters.com/news/archive/newsOne";
            static public string worldNewsUrl = "https://www.reuters.com/world";
            static public string financeNewsUrl = "https://www.reuters.com/finance";
            static public string breakingViewsUrl = "https://www.reuters.com/breakingviews";
            static public string techNewsUrl = "https://www.reuters.com/tech";
            static public string lifeUrl = "https://www.reuters.com/lifestyle";
            static public string dataFolder = Environment.ExpandEnvironmentVariables("%TMP%/reuters/");
            static public string dataPath = dataFolder + "/data";
        }
        // General
        static List<string> getContentList(List<string> articles, Func<string,string> contentFunc){
            List<string> listOfAttribute = new List<string>();
            foreach(string article in articles){
                listOfAttribute.Add(contentFunc(article));
            }
            return listOfAttribute;
        }
        static string getFirstContent(string source,Regex rx,string name){
            MatchCollection matches = rx.Matches(source);
            GroupCollection groups = matches[0].Groups;
            return groups[name].Value;
        }
        static List<string> splitFileBy(string source,Regex rx){
            List<string> contentList = new List<string>();
            MatchCollection matches =  rx.Matches(source);
            foreach(Match match in matches){
                contentList.Add(match.Value);
            }
            return contentList;
        }
        static string getHtmlStr(string URL){
            WebClient web = new WebClient();
            string htmlStr = web.DownloadString(URL);
            return htmlStr;
        }
        static bool existData(){
            if(!File.Exists(Settings.dataPath)){
                return false; 
            }else{
                return true;
            }
        }
        static bool needUpdate(){
            FileInfo fi = new FileInfo(Settings.dataPath);
            var writeTime = fi.LastWriteTime;
            var time = System.DateTime.Now;
            var diffHours = (time - writeTime).TotalHours;
            if(diffHours > 1){
                return true;
            }else{
                return false;
            }
        }
        static string wrapLine(string str,int length){
            string res = str;
            for(int i = length ; i < str.Length ; i+=length){
                if(res[i-1] == ' ' || res[i] == ' '){
                    res = res.Insert(i,"\n");
                }else{
                    res = res.Insert(i,"-\n");
                }
            }
            return res;
        }
        static int newFolderIfNotExist(){
            if(!Directory.Exists(Settings.dataFolder)){
                Directory.CreateDirectory(Settings.dataFolder);
            }
            return 0;
        }


        // Article list
        static List<string> getArticleList(string htmlStr){
            Regex rx = new Regex(@"<article+.*?>(?<content>.*?)</article>", RegexOptions.Singleline);
            return splitFileBy(htmlStr,rx);
        }
        static string getArticleUrl(string article){
            Regex rx = new Regex(@"<a href=""(?<url>.*?)"">",RegexOptions.Singleline);
            return getFirstContent(article,rx,"url");
        }
        static string getArticleTitle(string article){
            Regex rx = new Regex(@"<h\d\sclass=.*?title.>\s*(?<title>.*?)</h.>",RegexOptions.Singleline);
            return getFirstContent(article,rx,"title");
        }
        // Content page
        static List<string> getParagraphList(string htmlStr){
            Regex rx = new Regex(@"<p class=.Paragraph-paragraph+.*?>(?<content>.*?)</p>",RegexOptions.Singleline);
            return splitFileBy(htmlStr,rx);
        }
        static string getParagraphContent(string paragraphHTML){
            Regex rx = new Regex(@"<p class=.Paragraph-paragraph.*?>(?<content>.*?)</p>",RegexOptions.Singleline);
            return getFirstContent(paragraphHTML,rx,"content");
        }
        static List<string> getFullArticle(string articleUrl){
            articleUrl = "https://www.reuters.com" + articleUrl;
            List<string> paragraphs = getParagraphList(getHtmlStr(articleUrl));
            List<string> contents = getContentList(paragraphs,getParagraphContent);
            return contents;
        }


        // Operation
        static void print(List<string> target,bool showNumber = false,int wrap = -1){
            int cnt = 0;
            Action<string> printWithNumber = x => {
                Console.WriteLine("[{0}] {1}",cnt,x);
                cnt += 1;
            };
            Action<string> printWrap = x => Console.WriteLine(wrapLine(x,wrap));
            Action<string> printRaw = x => Console.WriteLine(x);
            switch ((showNumber,wrap)){
                case (true,-1):
                    target.ForEach(printWithNumber);
                    break;
                case (false,> 0):
                    target.ForEach(printWrap);    
                    break;
                default:
                    target.ForEach(printRaw);
                    break;
            }
        }
        static (List<string>,List<string>,List<string>) load(string url,bool refresh = false){
            List<string> articles;
            string rawContent;
            if(!refresh && existData() && !needUpdate()){
                rawContent = System.IO.File.ReadAllText(Settings.dataPath);
            } else{
                rawContent = getHtmlStr(url);
                System.IO.File.WriteAllText(Settings.dataPath,rawContent);
            }
            articles = getArticleList(rawContent);
            List<string> titles = getContentList(articles,getArticleTitle);
            List<string> urls = getContentList(articles,getArticleUrl);
            return (articles,titles,urls);
        }
        public class Options 
        {
            [Option('l',"list",Default=true,Required =false,HelpText ="List top news.")]
            public bool isList { get; set; }
            [Option('p',"page",Default="-1",Required =false,HelpText ="Load page n.")]
            public string getPage { get; set; }
            [Option('g',"goto",Default="-1",Required =false,HelpText ="Open specific article.")]
            public string getNumber { get; set; }
            [Option('w',"wrap",Default="100",Required =false,HelpText ="Set length of a single line. Set to 0 to output without wrapping.")]
            public string getWrap { get; set; }
            [Option('r',"refresh",Default=false,Required = false, HelpText = "Refresh list of article.")]
            public bool isRefresh { get; set; }
            [Option('s',"source",Default="world",Required = false, HelpText = "Choose news source from [top | world | tech | finance | breakingviews] news.")]
            public string getSource { get; set; }
        }
        public static string selectURL(Options options){
            switch (options.getSource){
                case "top":
                    return Settings.topNewsUrl;
                case "world":
                    return Settings.worldNewsUrl;
                case "tech":
                    return Settings.techNewsUrl;
                case "finance":
                    return Settings.financeNewsUrl;
                case "breakingviews":
                    return Settings.breakingViewsUrl;
                default:
                    return Settings.worldNewsUrl;    
            }
        }
        private static void Run(Options options) {
            int pageNumber = Int32.Parse(options.getPage);
            int articleNumber = Int32.Parse(options.getNumber);
            List<string> articles, titles, urls;
            string sourceUrl = selectURL(options);
            if(pageNumber == -1){
                (articles, titles, urls) = load(sourceUrl,refresh:options.isRefresh);
            }else{
                string newURL = sourceUrl + $"?view=page&page={pageNumber}&pageSize=10";
                (articles, titles, urls) = load(newURL,refresh:true);
            }
            if (articleNumber < 0) {
                print(titles,true);
            } else {
                print(getFullArticle(urls[articleNumber]),false,Int32.Parse(options.getWrap));
            }
        }
        static void Main(string[] args) {
            newFolderIfNotExist();
            Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }
    }
}
