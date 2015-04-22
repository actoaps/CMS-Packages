using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using Composite.Community.Blog.Models;
using Composite.Core;
using Composite.Core.Logging;
using Composite.Core.WebClient.Renderings.Page;
using Composite.Data;
using Composite.Data.Types;

namespace Composite.Community.Blog
{
    public class BlogFacade
    {
	    public static string PathSpecial = "_";

	    private static string _requestPathInvalidCharacters = null;
		public static string RequestPathInvalidCharacters {
			get
			{
				if(_requestPathInvalidCharacters == null)
				{
					var section = HttpContext.Current.GetSection("system.web/httpRuntime") as System.Web.Configuration.HttpRuntimeSection;
					_requestPathInvalidCharacters = (section.RequestPathInvalidCharacters ?? string.Empty).Replace(",","");
				}
				return _requestPathInvalidCharacters;
			}
		}
		

        public static IEnumerable<Tag> GetTagCloud(double minFontSize, double maxFontSize, DataReference<IPage> blogPage,
                                                   bool isGlobal)
        {
            Guid currentPageId = blogPage == null ? PageRenderer.CurrentPageId : blogPage.Data.Id;

            if (isGlobal)
            {
                currentPageId = Guid.Empty;
            }

            List<string> blog = currentPageId == Guid.Empty
                                    ? DataFacade.GetData<Entries>().Select(b => b.Tags).ToList()
                                    : DataFacade.GetData<Entries>()
                                                .Where(b => b.PageId == currentPageId)
                                                .Select(b => b.Tags)
                                                .ToList();
            var dcTags = new Dictionary<string, int>();

            foreach (
                string tag in
                    blog.Where(b => !string.IsNullOrEmpty(b)).SelectMany(b => b.Split(','), (b, s) => s.Trim()))
            {
                if (dcTags.ContainsKey(tag))
                {
                    dcTags[tag] = dcTags[tag] + 1;
                }
                else
                {
                    dcTags.Add(tag, 1);
                }
            }

            return from d in dcTags
                   let minOccurs = dcTags.Values.Min()
                   let maxOccurs = dcTags.Values.Max()
                   let occurencesOfCurrentTag = d.Value
                   let weight =
                       (Math.Log(occurencesOfCurrentTag) - Math.Log(minOccurs))/
                       (Math.Log(maxOccurs) - Math.Log(minOccurs))
                   let fontSize = minFontSize + Math.Round((maxFontSize - minFontSize)*weight)
                   select new Tag {Title = d.Key, FontSize = fontSize, Rel = d.Value};
        }

        public static IEnumerable<Archive> GetArchive(DataReference<IPage> blogPage, bool isGlobal)
        {
            Guid currentPageId = blogPage == null ? PageRenderer.CurrentPageId : blogPage.Data.Id;
            IQueryable<Archive> result = DataFacade.GetData<Entries>()
                                                   .Where(c => isGlobal ? c.PageId != null : c.PageId == currentPageId)
                                                   .GroupBy(c => new {c.Date.Year, c.Date.Month})
                                                   .Select(
                                                       b =>
                                                       new Archive
                                                           {
                                                               Date = new DateTime(b.Key.Year, b.Key.Month, 1),
                                                               Count = b.Select(x => x.Date.Year).Count()
                                                           });
            return result.OrderByDescending(e => e.Date);
        }

        public static IEnumerable<Entries> GetEntries(bool isGlobal)
        {
            using (var dataConnection = new DataConnection())
            {
                Expression<Func<Entries, bool>> filter = GetBlogFilterFromUrl(isGlobal);
                return dataConnection.Get<Entries>().Where(filter).OrderByDescending(e => e.Date);
            }
        }

        public static IEnumerable<Authors> GetAuthours()
        {
            using (var dataConnection = new DataConnection())
            {
                return dataConnection.Get<Authors>();
            }
        }

        public static Expression<Func<Entries, bool>> GetBlogFilterFromUrl(bool isGlobal)
        {
            Guid currentPageId = isGlobal ? Guid.Empty : PageRenderer.CurrentPageId;
            Expression<Func<Entries, bool>> filter = f => (currentPageId == Guid.Empty || f.PageId == currentPageId);

            string[] pathInfoParts = GetPathInfoParts();
            if (pathInfoParts != null)
            {
                int year;
                //TODO: this is very primitive and not ideal way of checking if it's year or tag: "2010" or "ASP.NET"
                if (int.TryParse(pathInfoParts[1], out year) && (pathInfoParts[1].Length == 4))
                {
                    filter = f => f.Date.Year == year && (currentPageId == Guid.Empty || f.PageId == currentPageId);
                }
                else
                {
                    string tag = Decode(pathInfoParts[1]);

                    // filter below replaced becauase of LINQ2SQL problems
                    //filter = f => f.Tags.Split(',').Any(t => t == tag) && f.PageId == currentPageId;
                    filter =
                        f =>
                        ((f.Tags.Contains("," + tag + ",")) || f.Tags.StartsWith(tag + ",") ||
                         (f.Tags.EndsWith("," + tag)) || f.Tags.Equals(tag)) &&
                        (currentPageId == Guid.Empty || f.PageId == currentPageId);
                }

                if (pathInfoParts.Length > 2)
                {
                    int month = Int32.Parse(pathInfoParts[2]);

                    if (pathInfoParts.Length > 3)
                    {
                        int day = Int32.Parse(pathInfoParts[3]);

                        if (pathInfoParts.Length > 4)
                        {
                            var blogDate = new DateTime(year, month, day);

                            string title = pathInfoParts[4];
                            filter =
                                f =>
                                f.Date.Date == blogDate && f.TitleUrl == title &&
                                (currentPageId == Guid.Empty || f.PageId == currentPageId);
                        }
                        else
                        {
                            filter =
                                f =>
                                f.Date.Year == year && f.Date.Month == month && f.Date.Day == day &&
                                (currentPageId == Guid.Empty || f.PageId == currentPageId);
                        }
                    }
                    else
                    {
                        filter =
                            f =>
                            f.Date.Year == year && f.Date.Month == month &&
                            (currentPageId == Guid.Empty || f.PageId == currentPageId);
                    }
                }
            }

            return filter;
        }

        public static string GetUrlFromTitle(string title)
        {
            const string autoRemoveChars = @",./\?#!""@+'`´*():;$%&=¦§";
            var generated = new StringBuilder();

            foreach (char c in title.Where(c => autoRemoveChars.IndexOf(c) == -1))
            {
                generated.Append(c);
            }

            string url = generated.ToString().Replace(" ", "-");

            return url;
        }

        public static string GetBlogUrl(DateTime date, string title, Guid pageId, string pageUrl = "")
        {
            if (string.IsNullOrEmpty(pageUrl))
            {
                pageUrl = GetPageUrlById(pageId);
            }

            return string.Format("{0}{1}", pageUrl, GetBlogPath(date, title));
        }

        public static string GetBlogPath(DateTime date, string title)
        {
            return string.Format("/{0}/{1}", CustomDateFormatCulture(date, "yyyy/MM/dd", "en-US"),
                                 GetUrlFromTitle(title));
        }

        public static string GetCurrentPageUrl()
        {
            using (var dataConnection = new DataConnection())
            {
                var sitemapNavigator = new SitemapNavigator(dataConnection);
                return sitemapNavigator.CurrentPageNode.Url;
            }
        }

        public static string GetPageUrlById(Guid pageId)
        {
            using (var dataConnection = new DataConnection())
            {
                var sitemapNavigator = new SitemapNavigator(dataConnection);
                return sitemapNavigator.GetPageNodeById(pageId).Url;
            }
        }

        public static string[] GetPathInfoParts()
        {
            // Expecting '/yyyy/mm/dd/title' OR /tag
            string pathInfo = GetPathInfo();

            return pathInfo != null && pathInfo.Contains("/") ? pathInfo.Split('/') : null;
        }

        private static string GetPathInfo()
        {
            Type pageRoute = typeof (IData).Assembly.GetType("Composite.Core.Routing.Pages.C1PageRoute", false);

            if (pageRoute == null)
            {
                // Support for version 2.1.1+
                return new UrlBuilder(HttpContext.Current.Request.RawUrl).PathInfo;
            }

            // Support for version 2.1.3+
            string result = (pageRoute
                                 .GetMethod("GetPathInfo", BindingFlags.Public | BindingFlags.Static)
                                 .Invoke(null, new object[0]) as string) ?? string.Empty;

            if (result != string.Empty)
            {
                pageRoute
                    .GetMethod("RegisterPathInfoUsage", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[0]);
            }

            return result;
        }

        public static string GetFullPath(string path)
        {
            HttpRequest request = HttpContext.Current.Request;
            return (new Uri(request.Url, Path.Combine(request.ApplicationPath, path))).OriginalString;
        }

        public static string CustomDateFormat(DateTime date, string dateFormat)
        {
            return date.ToString(dateFormat, CultureInfo.CurrentCulture);
        }

        public static string CustomDateFormatCulture(DateTime date, string dateFormat, string cultureName)
        {
            return date.ToString(dateFormat, CultureInfo.CreateSpecificCulture(cultureName));
        }

        public static bool Validate(string regularExpression, object value, bool isRequired)
        {
            if (value == null)
            {
                return isRequired ? false : true;
            }
            {
                return String.IsNullOrEmpty(value.ToString())
                           ? false
                           : Regex.IsMatch(value.ToString(), regularExpression);
            }
        }

        public static void SendMail(string to, string from, string subject, string body)
        {
            try
            {
                var mail = new MailMessage();
                mail.To.Add(to);
                mail.From = new MailAddress(from);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.BodyEncoding = Encoding.UTF8;
                mail.SubjectEncoding = Encoding.UTF8;
                var smtpMail = new SmtpClient();
                smtpMail.Send(mail);
            }
            catch (Exception ex)
            {
                LoggingService.LogInformation("Composite.Community.Blog.BlogFacade.SendMail",
                                              "Error while sending Email. Check mail settings in web.config file.");
                LoggingService.LogError("Composite.Community.Blog.BlogFacade.SendMail", ex);
            }
        }

        public static string GetCurrentCultureName()
        {
            return CultureInfo.CurrentCulture.Name.ToLower();
        }

        public static string GetCurrentPath()
        {
            string[] pathInfoParts = GetPathInfoParts();
            string path = string.Empty;
            if (pathInfoParts != null)
            {
                int year;

                if (int.TryParse(pathInfoParts[1], out year))
                {
                    path = string.Format("/{0}", pathInfoParts[1]);

                    if (pathInfoParts.Length > 2)
                    {
                        int month;
                        if (int.TryParse(pathInfoParts[2], out month))
                        {
                            path = string.Format("{0}/{1}", path, pathInfoParts[2]);
                        }
                    }
                }
                else
                {
                    path = string.Format("/{0}", pathInfoParts[1]);
                }
            }

            return path;
        }

        public static void SetTitleUrl(object sender, DataEventArgs dataEventArgs)
        {
            var entry = (Entries) dataEventArgs.Data;
            entry.TitleUrl = GetUrlFromTitle(entry.Title);
        }


		public static string Decode(string text)
		{
			text = text.Replace(PathSpecial + PathSpecial, PathSpecial + "00");
			text = RequestPathInvalidCharacters.Aggregate(text, (current, c) => current.Replace(PathSpecial + Convert.ToByte(c).ToString("x2"), c.ToString()));
			text = text.Replace(PathSpecial+"00", PathSpecial);
			return text;
		}

        public static string Encode(string text)
        {
			text = text.Replace(PathSpecial, PathSpecial + PathSpecial);
			text = RequestPathInvalidCharacters.Aggregate(text, (current, c) => current.Replace(c.ToString(), PathSpecial + Convert.ToByte(c).ToString("x2")));
	        return Uri.EscapeDataString(text);
        }

        public static void ClearRssFeedCache(object sender, DataEventArgs dataEventArgs)
        {
            var entry = (Entries) dataEventArgs.Data;
            if (entry != null)
            {
                HttpRuntime.Cache.Remove(string.Format(BlogRssFeed.CacheRSSKeyTemplate, entry.PageId,
                                                       GetCurrentCultureName()));
            }
        }

        public static string GetFullBlogUrl(DateTime date, string title)
        {
            Guid pageId = SitemapNavigator.CurrentPageId;
            string pageUrl = GetPageUrlById(pageId);
            pageUrl = GetFullPath(pageUrl);
            return GetBlogUrl(date, title, pageId, pageUrl);
        }

        public static List<string> GetBlogTags(string tags)
        {
            List<string> blogTags = tags.Split(',').Where(t => !string.IsNullOrEmpty(t)).Select(t => t).ToList();

            return blogTags;
        }

        public static bool IsBlogList()
        {
            string[] pathInfoParts = GetPathInfoParts();

            return pathInfoParts != null && (pathInfoParts.Count() == 5 ? true : false);
        }

        public static void SetNoCache()
        {
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        }
    }
}