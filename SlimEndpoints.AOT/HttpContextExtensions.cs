using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

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

    public static async ValueTask<T?> GetMultipartFormJsonSection<T>(this HttpContext httpContext, ParameterInfo parameter)
    {
        //ENABLE BUFFERING FOR MULTIPLE READS
        httpContext.Request.EnableBuffering();

        T? result = default;
        if (!httpContext.TryGetMediaTypeHeaderValue(ContentTypes.MultipartFormData, out MediaTypeHeaderValue? contentType))
        {
            throw new ArgumentException("Incorrect mime-type");
        }

        if (!contentType!.TryGetBoundary(256, out string? boundary))
        {
            throw new Exception("Incorrect boundary");
        }

        //RESET POSITION TO START, DONT FORGET TO RESET POSITION TO THE NEXT MIDDLEWARE
        httpContext.Request.Body.Position = 0;
        //RESET POSITION TO START, DONT FORGET TO RESET POSITION TO THE NEXT MIDDLEWARE

        try
        {
            var multipartReader = new MultipartReader(boundary!, httpContext.Request.Body);
            while (await multipartReader.ReadNextSectionAsync() is { } section)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition!.Name == parameter.Name)
                {
                    if (section.ContentType != ContentTypes.ApplicationJson)
                    {
                        throw new ArgumentException($"Invalid content type {section.ContentType} in section {contentDisposition!.Name}");
                    }
                    var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>();
                    result = await JsonSerializer.DeserializeAsync<T>(section.Body, jsonOptions.Value.SerializerOptions);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            //RESET POSITION TO START, DONT FORGET TO RESET POSITION TO THE NEXT MIDDLEWARE
            httpContext.Request.Body.Position = 0;
            //RESET POSITION TO START, DONT FORGET TO RESET POSITION TO THE NEXT MIDDLEWARE
        }
        return result;
    }

}
