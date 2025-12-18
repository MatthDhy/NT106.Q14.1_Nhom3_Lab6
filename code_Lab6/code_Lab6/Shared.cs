using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace code_Lab6
{
    public enum Shared
    {
        MENU,
        ORDER,
        GET_ODERS,
        PAY,
        AUTH_CUSTOMER,
        AUTH_STAFF,
        QUIT
    }
    public class MessageEnvelope
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Shared Type { get; set; }
        public string Payload { get; set; }
    }

    public static class JsonHelper
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string Serialize(object obj) => JsonSerializer.Serialize(obj, Options);
        public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }
}
