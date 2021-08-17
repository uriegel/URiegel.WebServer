using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UwebServer
{
    static class Json
    {
        public static JsonSerializerSettings DefaultSettings { get; } = new() 
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
    }
}