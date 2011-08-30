using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Composite.Data;
using Composite.Data.Types;

namespace Composite.Feeds.YoutubeReader
{
	public static class InlineMethodFunction
	{
		public static string GetFeedURL(string Username, int ItemsPerPage, string StartIndex, string FeedType)
		{
			string urlTemplate = string.Empty;
			if (FeedType == "channel")
				urlTemplate = "http://gdata.youtube.com/feeds/api/videos?alt=rss&author={0}&max-results={1}&start-index={2}";
			if (FeedType == "favorites")
				urlTemplate = "http://gdata.youtube.com/feeds/api/users/{0}/favorites?alt=rss&max-results={1}&start-index={2}";

			return string.Format(urlTemplate, Username, ItemsPerPage, StartIndex);
		}
	}
}
