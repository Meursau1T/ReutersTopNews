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
        static string topNewsUrl = "https://www.reuters.com/news/archive/newsOne";
        static string dataPath = "./data";
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
            if(!File.Exists(dataPath)){
                return false; 
            }else{
                return true;
            }
        }
        static bool needUpdate(){
            FileInfo fi = new FileInfo(dataPath);
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
            foreach(string child in target){
                if(showNumber){
                    Console.Write("[{0}] ",cnt);
                }
                cnt += 1;
                if(wrap > 0){
                    Console.WriteLine(wrapLine(child,wrap));
                }else{
                    Console.WriteLine(child);
                }
                Console.WriteLine();
            }
        }
        static (List<string>,List<string>,List<string>) load(string url,bool refresh = false){
            List<string> articles;
            string rawContent;
            if(refresh){
                rawContent = getHtmlStr(url);
                System.IO.File.WriteAllText(dataPath,rawContent);
            }else if(existData() && !needUpdate()){
                rawContent = System.IO.File.ReadAllText(dataPath);
            }else{
                rawContent = getHtmlStr(url);
                System.IO.File.WriteAllText(dataPath,rawContent);
            }
            articles = getArticleList(rawContent);
            List<string> titles = getContentList(articles,getArticleTitle);
            List<string> urls = getContentList(articles,getArticleUrl);
            return (articles,titles,urls);
        }
        public class Options 
        {
            [Option('l',"list",Required =false,HelpText ="List top news.")]
            public bool isList { get; set; }
            [Option('p',"page",Default="-1",Required =false,HelpText ="Load page n.")]
            public string getPage { get; set; }
            [Option('g',"goto",Default="-1",Required =false,HelpText ="Open specific article.")]
            public string getNumber { get; set; }
            [Option('w',"wrap",Default="100",Required =false,HelpText ="Set length of a single line. Set to -1 to output without wrapping.")]
            public string getWrap { get; set; }
        }

        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }
        private static void Run(Options options) {
            int pageNumber = Int32.Parse(options.getPage);
            int articleNumber = Int32.Parse(options.getNumber);
            List<string> articles, titles, urls;
            if(pageNumber == -1){
                (articles, titles, urls) = load(topNewsUrl);
            }else{
                string newURL = topNewsUrl + $"?view=page&page={pageNumber}&pageSize=10";
                (articles, titles, urls) = load(newURL,true);
                print(titles,true);
            }
            if (options.isList) {
                print(titles,true);
            } else if (articleNumber >= 0){
                print(getFullArticle(urls[articleNumber]),false,Int32.Parse(options.getWrap));
            }
        }
    }
}
