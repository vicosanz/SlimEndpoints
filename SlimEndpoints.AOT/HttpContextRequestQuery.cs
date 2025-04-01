using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using static SlimEndpoints.AOT.PrimitiveValuesFromString;

namespace SlimEndpoints.AOT;

public static class HttpContextRequestQueryExtensions
{
    public static bool TryGetRequestQueryComplexValue<T>(this HttpContext httpContext, string key, 
        ParseFromStringDelegate<T> parser, out T? value)
    {
        value = default;
        if (httpContext.Request.Query.TryGetValue(key, out var val))
        {
            if (val.Count == 0) return false;
            try
            {
                value = parser(val[0]!);
                return true;
            }
            catch
            {
            }
        }
        return false;
    }

    public static bool TryGetRequestQueryValue<T>(this HttpContext httpContext, string key, out T? value) => 
        TryGetRequestQueryComplexValue(httpContext, key, ParseFromString<T>, out value);

    public static bool TryGetRequestQueryComplexValues<T>(this HttpContext httpContext, string key,
        ParseFromStringDelegate<T> parser, out IEnumerable<T> value)
    {
        value = [];
        if (httpContext.Request.Query.TryGetValue(key, out var val))
        {
            if (val.Count == 0) return false;
            try
            {
                foreach (var item in val)
                {
                    value = value.Append(parser(item!));
                }
                return true;
            }
            catch
            {
                value = default!;
            }
        }
        return false;
    }

    public static bool TryGetRequestQueryValues<T>(this HttpContext httpContext, string key, out IEnumerable<T> values) => 
        TryGetRequestQueryComplexValues(httpContext, key, ParseFromString<T>, out values!);

}
