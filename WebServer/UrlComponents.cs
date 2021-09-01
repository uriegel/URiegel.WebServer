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

		public UrlComponents(string completeUrl, string url)
		{
            var urlWithoutQuery = completeUrl;
            if (completeUrl.Contains('?'))
			{
                var pos = completeUrl.IndexOf('?');
                Parameters = GetParameters(completeUrl).ToDictionary(n => n.Key, n => n.Value);
                urlWithoutQuery = completeUrl[..pos];
            }
			else
				Parameters = new Dictionary<string, string>();

            var path = !string.IsNullOrEmpty(url)
                ? urlWithoutQuery.Length > url.Length + 1
                    ? urlWithoutQuery[(url.Length + 1)..]
                    : ""
                : "";
            Path = Uri.UnescapeDataString(path.Replace('+', ' '));
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
