using System.Collections.Generic;

namespace UwebServer
{
	interface IHeaders
	{
		string ContentType { get; }
		string? this[string key] { get; }
		IEnumerable<KeyValuePair<string, KeyValuePair<string, string>>> Raw { get; }
	}
}