using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Features=System.Collections.Generic.Dictionary<string, bool>;

namespace Rooletochka {
    public class Analyzer {
        // Names of features for each page on site.
        //
        private const string TAG_BODY = "tagBody";
        private const string TAG_HTML = "tagHtml";
        private const string TAG_HEAD = "tagHead";
        private const string TAG_TITLE = "tagTitle";
        private const string INLINE_JS = "inlineJs";
        private const string INLINE_CSS = "inlineCss";

        private const byte MAX_CHILD_PAGE_IN_REPORT = 15;
        private const int SLEEP_TIME = 1000;
        private readonly string _url;
        private readonly string _content;
        private List<String> _pages;

        public string Url {
            get { return _url; }
        }

        public string Content {
            get { return _content; }
        }

        public Analyzer(string url, bool isMainPage = true) {
            _url = NormalizeUrl(url);

            if (IsCorrectURL(_url)) {
                WebClient client = new WebClient();
                _content = client.DownloadString(_url).ToLower();

                if (isMainPage) _pages = GetPages(_content, _url);
                else _pages = new List<string>();
            }
        }

        // URL from database comes with fixed (relatively large) length, and
        // just part of it is real address, and other part is filled with
        // spaces. Moreover, the URL may be written with last '/' or without it,
        // so it looks necessary to lead input URL to some standart form.
        //
        private string NormalizeUrl(string url) {
            url = url.Trim();
            if (url[url.Length - 1] == '/') {
                return url.Remove(url.Length - 1).ToLower();
            }
            return url;
        }

        public Report Analyze(int reportId) {
            Report report = new Report(reportId);
            report.MainUrl = Url;
            report.RobotsTxt = CheckRobotsTxt(Url);
            Thread.Sleep(SLEEP_TIME);
            report.Error404 = CheckError404(Url);
            Thread.Sleep(SLEEP_TIME);
            report.Redirect = CheckMirror(Url);

            report.mainPageResult = this.AnalyzePage(Url);
            Features result = new Features();
            int count = 0;
            foreach (string page in _pages) {
                try {
                    Analyzer analyzer = new Analyzer(page, false);
                    result = analyzer.AnalyzePage(analyzer.Url);
                    report.AddCheckedPage(result, page);
                    count++;
                    if (count == MAX_CHILD_PAGE_IN_REPORT) break;
                    Thread.Sleep(SLEEP_TIME);
                }
                catch (Exception ex) {
                    Console.WriteLine(@"method: Report Analyze(...)\n {0}\n,
                        stackTrace{1} ", ex.Message, ex.StackTrace);
                }
            }
            return report;
        }

        private bool IsCorrectURL(string url) {
            Uri correctUrl;
            return (Uri.TryCreate(url, UriKind.Absolute, out correctUrl) &&
                correctUrl.Scheme == Uri.UriSchemeHttp);
        }

        // check on correct url and than link != link to the file, except .php
        private bool IsCorrectLink(string link) {
            if (!IsCorrectURL(link)) return false;

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
        private Features AnalyzePage(string url) {
            Features result = new Features();
            result[TAG_BODY] = CheckBodyTag();
            result[TAG_HEAD] = CheckHeadTag();
            result[TAG_TITLE] = CheckTitleTags();
            result[TAG_HTML] = CheckHtmlTag();
            result[INLINE_JS] = CheckInlineJS();
            result[INLINE_CSS] = CheckInlineCSS();
            return result;
        }

        private List<String> GetPages(string content, string url) {
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
                if (!link.Contains(url)) { continue; }
                if (IsCorrectLink(link)) { pages.Add(link); }
            }
            return pages;
        }

        private string GenerateRandomString(int size) {
            string result = "";
            while (result.Length < size) {
                result += Path.GetRandomFileName();
            }
            return result.Remove(size - 1);
        }

        #region Methods for checking common rules

        public bool CheckRobotsTxt(string url) {
            string str = url + "/robots.txt";

            const bool redirect = false;
            int statusCode = CheckStatusCode(str, redirect);
            if (statusCode > 400 || statusCode == 0) return false;
            return true;
        }

        public bool CheckError404(string url) {
            string str = url + "/" + GenerateRandomString(42);

            bool redirect = true;
            int statusCode = CheckStatusCode(str, redirect);
            return (statusCode == 404);
        }

        public bool CheckMirror(string url) {
            bool redirect = false;
            int statusCode = CheckStatusCode(url, redirect);
            return (statusCode == 301 || statusCode == 302);
        }

        private int CheckStatusCode(string url, bool redirect) {
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

        private bool CheckInlineJS() {
            string pattern = @"<script.*?>";
            Regex rgx = new Regex(pattern);
            MatchCollection matches = rgx.Matches(Content);
            foreach (Match match in matches) {
                string value = match.ToString();
                if (value.Contains("src") && value.Contains(".js")) continue;
                return false;
            }
            return true;
        }

        private bool CheckInlineCSS() {
            string pattern = @"style\s*=\s*"".*?""";
            Regex rgx = new Regex(pattern);
            return (rgx.Matches(Content).Count == 0);
        }

        private bool CheckTitleTags() {
            for (int i=0; i<6; i++) {
                if (CheckTag("h"+i)) { return true; }
            }
            return false;
        }

        private bool CheckHtmlTag() {
            return CheckTag("html");
        }

        private bool CheckBodyTag() {
            return CheckTag("body");
        }

        private bool CheckHeadTag() {
            return CheckTag("head");
        }

        // Check page for opening and closing tags.
        // Example: CheckTag("html") will check page for <html> and </html>.
        //
        private bool CheckTag(string tag) {
            return Content.Contains("<" + tag) &&
                Content.Contains("</" + tag + ">");
        }

        #endregion
    }
}