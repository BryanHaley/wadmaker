using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shared.JSON
{
    /// <summary>
    /// A type info resolver that can be used when unused code is trimmed.
    /// It requires that, for any custom types, there are converters registered in the serialization options.
    /// </summary>
    public class NoReflectionJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            => JsonTypeInfo.CreateJsonTypeInfo(type, options);
    }
}
