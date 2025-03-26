using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SlimEndpoints.AOT;
using System.Text.Json;

namespace WebApplication1.Endpoints.Products.Upload
{
    public class PostUploadRequest
    {
        public int Id { get; set; }
        //Complex MultipartFormData can be handled manually
        //public UploadData Datax { get; set; }
    }

    public record UploadData(string name = "", string description = "", string fileText = "");

    [SlimEndpoint("/upload/{id:int}", [HttpMehotds.Post], group: "Products")]
    public class PostUploadMultipartFormDataNonStandard(IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions) : SlimEndpoint<PostUploadRequest, IResult>
    {
        public override void Configure(RouteHandlerBuilder builder)
        {
            //builder.Accepts<UploadData>(ContentTypes.MultipartFormData);
        }
        public override async Task<IResult> HandleAsync(HttpContext httpContext, PostUploadRequest request, CancellationToken cancellationToken)
        {
            UploadData data = new();
            var fileText = string.Empty;

            if (!httpContext.TryGetMediaTypeHeaderValue(ContentTypes.MultipartFormData, out MediaTypeHeaderValue? contentType))
            {
                return Results.BadRequest("Incorrect mime-type");
            }

            if (!contentType!.TryGetBoundary(256, out string? boundary))
            {
                return Results.BadRequest("Incorrect boundary");
            }

            var multipartReader = new MultipartReader(boundary!, httpContext.Request.Body);
            while (await multipartReader.ReadNextSectionAsync(cancellationToken) is { } section)
            {
                //if (!section.TryGetMediaTypeHeaderValue(out MediaTypeHeaderValue? sectionType))
                //{
                //    return Results.BadRequest("Invalid content type in section " + section.ContentType);
                //}

                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition!.Name == "Datax")
                {
                    if (section.ContentType != ContentTypes.ApplicationJson)
                    {
                        return Results.BadRequest($"Invalid content type {section.ContentType} in section {contentDisposition!.Name}");
                    }
                    data = await JsonSerializer.DeserializeAsync<UploadData>(section.Body, jsonOptions.Value.SerializerOptions, cancellationToken);
                }
                if (contentDisposition!.Name == "file")
                {
                    if (section.ContentType != ContentTypes.TextPlain)
                    {
                        return Results.BadRequest($"Invalid content type {section.ContentType} in section {contentDisposition!.Name}");
                    }
                    fileText = await section.ReadAsStringAsync(cancellationToken);
                }
            }
            return Results.Ok(data with { fileText = fileText });
        }
    }
}
