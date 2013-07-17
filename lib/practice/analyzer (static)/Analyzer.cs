using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Features = System.Collections.Generic.Dictionary<string, bool>;

namespace Rooletochka {
    public struct Site {
        public string mainUrl;
        public string content;
        public List<String> pages;
    }

    internal static class Analyzer {
        // Names of features for each page on site.
        //
        private const string TAG_BODY = "tagBody";
        private const string TAG_HTML = "tagHtml";
        private const string TAG_HEAD = "tagHead";
        private const string TAG_TITLE = "tagTitle";
        private const string INLINE_JS = "inlineJs";
        private const string INLINE_CSS = "inlineCss";
        private const byte MAX_CHILD_PAGE_IN_REPORT = 15;

        // URL from database comes with fixed (relatively large) length, and
        // just part of it is real address, and other part is filled with
        // spaces. Moreover, the URL may be written with last '/' or without it,
        // so it looks necessary to lead input URL to some standart form.
        //
        private static string NormalizeUrl(string url) {
            url = url.Trim();
            if (url[url.Length - 1] == '/') {
                return url.Remove(url.Length - 1).ToLower();
            }
            return url;
        }

        public static Report Analyze(int reportId, string url) {
            Site site = new Site();
            site.mainUrl = NormalizeUrl(url);
            if (IsCorrectURL(site.mainUrl)) {
                site.content = GetContent(url);
                site.pages = GetPages(site.content, site.mainUrl);
            }
            else {
                throw new Exception("Uncorrect url");
            }

            Report report = new Report(reportId);
            report.MainUrl = site.mainUrl;
            report.RobotsTxt = CheckRobotsTxt(site.mainUrl);
            Thread.Sleep(1000);
            report.Error404 = CheckError404(site.mainUrl);
            Thread.Sleep(1000);
            report.Redirect = CheckMirror(site.mainUrl);
            report.mainPageResult = AnalyzePage(site);
            int count = 0;
            foreach (string urlPage in site.pages) {
                try {
                    Features result = AnalyzePage(GetContent(urlPage));
                    report.AddCheckedPage(result, urlPage);
                    count++;
                    if (count == MAX_CHILD_PAGE_IN_REPORT) {
                        break;
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception ex) {
                    Console.WriteLine(@"method: Analyzer.Analyze()\n {0}\n,", ex.Message);
                }
            }
            return report;
        }

        private static bool IsCorrectURL(string url) {
            Uri correctUrl;
            return (Uri.TryCreate(url, UriKind.Absolute, out correctUrl) &&
                    correctUrl.Scheme == Uri.UriSchemeHttp);
        }

        // check on correct url and than link != link to the file, except .php
        private static bool IsCorrectLink(string link) {
            if (!IsCorrectURL(link)) {
                return false;
            }

            string buffer = "";
            int i = link.Length - 1;
            while (i >= 0 && link[i] != '.' && link[i] != '/') {
                buffer = link[i] + buffer;
                i--;
            }
            if (buffer.Length <= 3) {
                return (buffer.ToLower() == "php");
            }
            return true;
        }

        // Analyze one page of site.
        //
        private static Features AnalyzePage(Site site) {
            return AnalyzePage(site.content);
        }

        private static Features AnalyzePage(string content) {
            Features result = new Features();
            result[TAG_BODY] = CheckBodyTag(content);
            result[TAG_HEAD] = CheckHeadTag(content);
            result[TAG_TITLE] = CheckTitleTags(content);
            result[TAG_HTML] = CheckHtmlTag(content);
            result[INLINE_JS] = CheckInlineJS(content);
            result[INLINE_CSS] = CheckInlineCSS(content);
            return result;
        }

        private static List<String> GetPages(string content, string url) {
            url = url.TrimEnd('/');
            List<String> pages = new List<String>();
            string pattern = @"<a.*?href\s*=(['""][^""]*['""])";
            Regex rgx = new Regex(pattern);
            MatchCollection matches = rgx.Matches(content);

            foreach (Match match in matches) {
                string link = Regex.Replace(match.ToString(),
                                            @"<a.*?href\s*=(['""][^""]*['""])", @"$1",
                                            RegexOptions.IgnoreCase);
                link = link.Trim("\"".ToCharArray());
                if (link.Length <= 2 || Regex.IsMatch(link, @"^//")) {
                    continue;
                }
                if ((link[0] == '/') || Regex.IsMatch(link, @"^\./")) {
                    link = url + link;
                }
                if (!link.Contains(url)) {
                    continue;
                }
                if (IsCorrectLink(link)) {
                    pages.Add(link);
                }
            }
            return pages;
        }

        private static string GetContent(string url) {
            WebRequest req = WebRequest.Create(new Uri(url));
            req.Timeout = 20000;
            WebResponse resp = req.GetResponse();
            StreamReader str = new StreamReader(resp.GetResponseStream(), Encoding.Default);
            string content = str.ReadToEnd();
            str.Close();
            resp.Close();
            return content;
        }

        private static string GenerateRandomString(int size) {
            string result = "";
            while (result.Length < size) {
                result += Path.GetRandomFileName();
            }
            return result.Remove(size - 1);
        }

        #region Methods for checking common rules

        public static bool CheckRobotsTxt(string url) {
            string str = url + "/robots.txt";

            const bool redirect = false;
            int statusCode = CheckStatusCode(str, redirect);
            if (statusCode > 400 || statusCode == 0) {
                return false;
            }
            return true;
        }

        public static bool CheckError404(string url) {
            string str = url + "/" + GenerateRandomString(42);

            bool redirect = true;
            int statusCode = CheckStatusCode(str, redirect);
            return (statusCode == 404);
        }

        public static bool CheckMirror(string url) {
            bool redirect = false;
            int statusCode = CheckStatusCode(url, redirect);
            return (statusCode == 301 || statusCode == 302);
        }

        private static int CheckStatusCode(string url, bool redirect) {
            try {
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
                webRequest.AllowAutoRedirect = redirect;

                // Timeout of request (default timeout = 100s).
                //
                webRequest.Timeout = 50000;
                HttpWebResponse response = (HttpWebResponse) webRequest.GetResponse();
                int wRespStatusCode;
                wRespStatusCode = (int) response.StatusCode;
                return wRespStatusCode;
            }
            catch (WebException we) {
                try {
                    int wRespStatusCode = (int) ((HttpWebResponse) we.Response).StatusCode;
                    return wRespStatusCode;
                }
                catch (NullReferenceException e) {
                    Console.WriteLine(e.Message);
                    return 0;
                }
            }
        }

        #endregion

        #region Methods for checking Html tags (true - OK, false - page needs corrections)

        private static bool CheckInlineJS(string content) {
            string pattern = @"<script.*?>";
            Regex rgx = new Regex(pattern);
            MatchCollection matches = rgx.Matches(content);
            foreach (Match match in matches) {
                string value = match.ToString();
                if (value.Contains("src") && value.Contains(".js")) {
                    continue;
                }
                return false;
            }
            return true;
        }

        private static bool CheckInlineCSS(string content) {
            string pattern = @"style\s*=\s*"".*?""";
            Regex rgx = new Regex(pattern);
            return (rgx.Matches(content).Count == 0);
        }

        private static bool CheckTitleTags(string content) {
            for (int i = 0; i < 6; i++) {
                if (CheckTag(content, "h" + i)) {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckHtmlTag(string content) {
            return CheckTag(content, "html");
        }

        private static bool CheckBodyTag(string content) {
            return CheckTag(content, "body");
        }

        private static bool CheckHeadTag(string content) {
            return CheckTag(content, "head");
        }

        // Check page for opening and closing tags.
        // Example: CheckTag("html") will check page for <html> and </html>.
        //
        private static bool CheckTag(string content, string tag) {
            return content.Contains("<" + tag) &&
                   content.Contains("</" + tag + ">");
        }

        #endregion
    }
}