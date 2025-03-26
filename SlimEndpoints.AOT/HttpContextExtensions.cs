using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;

namespace SlimEndpoints.AOT;

public static class HttpContextExtensions
{
    public static bool TryGetMediaTypeHeaderValue(this HttpContext httpContext, string mediaType, out MediaTypeHeaderValue? contentType) => 
        MediaTypeHeaderValue.TryParse(httpContext.Request.ContentType, out contentType)
            && (contentType?.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase) ?? false);

    public static bool TryGetBoundary(this MediaTypeHeaderValue contentType, int lengthLimit, out string? boundaryString)
    {
        boundaryString = null;
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
        if (StringSegment.IsNullOrEmpty(boundary))
        {
            return false;
        }
        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }
        boundaryString = boundary.ToString();
        return true;
    }

    public static bool TryGetMediaTypeHeaderValue(this MultipartSection section, out MediaTypeHeaderValue? sectionType) =>
        MediaTypeHeaderValue.TryParse(section.ContentType, out sectionType);
}
