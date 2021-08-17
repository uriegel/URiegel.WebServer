using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UwebServer
{
    public class UrlComponents
    {
        public string Path;
		public Dictionary<string, string> Parameters;

		public UrlComponents(string query)
		{
			Path = "";

			if (!string.IsNullOrEmpty(query) && query.Contains('?'))
			{
				var pos = query.IndexOf('?');
				if (pos >= 0)
				{
					Path = query.Substring(0, pos) ?? "";
					Parameters = GetParameters(query).ToDictionary(n => n.Key, n => n.Value);
				}
				else
				{
					Path = query ?? "";
					Parameters = new Dictionary<string, string>();
				}
			}
			else
			{
				Path = query ?? "";
				Parameters = new Dictionary<string, string>();
			}
		}

        KeyValuePair<string, string>[] GetParameters(string urlParameterString)
		{
			var mc = urlParameterRegex.Matches(urlParameterString);
			return mc.OfType<Match>().Select(n => new KeyValuePair<string, string>(n.Groups["key"].Value,
				Uri.UnescapeDataString(UnescapeSpaces(n.Groups["value"].Value)))).ToArray();
		}

		static string UnescapeSpaces(string uri) => uri.Replace('+', ' ');
		static readonly Regex urlPartsRegex = new(@"(http://[^/]+)?(?:/(?<ResourcePart>[^<>?&/#\""]+))+(?:\?(?<Parameters>.+))?", RegexOptions.Compiled);
		static readonly Regex urlParameterRegex = new(@"(?<key>[^&?]*?)=(?<value>[^&?]*)", RegexOptions.Compiled);
	}
}
