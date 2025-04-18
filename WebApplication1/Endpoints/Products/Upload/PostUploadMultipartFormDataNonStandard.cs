using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SlimEndpoints.AOT;
using System.Reflection;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Endpoints.Products.Upload;

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-9.0#bind-to-collections-and-complex-types-from-forms
public record struct PostUploadRequest
{
    public int Id { get; set; }
    //Complex MultipartFormData can be handled manually
    //Automatic json deserialization from multipart section is not supported for minimal apis
    //Parse as string could be a good alternative, then deserialize manually
    //public string Datax { get; set; }
    //public IFormFile file { get; set; }
}

public class PostUploadBindingRequest
{
    public int Id { get; set; }
    //UploadData has a BindAsync method that can be used to parse the request
    //Do not use [FromForm] attribute because Binding Precedence will try to handle UploadData parameter as form data standard
    //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-9.0#binding-precedence
    public UploadData Datax { get; set; }
    public IFormFile file { get; set; }
}

public record UploadData(string name = "", string description = "")
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public static async ValueTask<UploadData?> BindAsync(HttpContext httpContext,
        ParameterInfo parameter) => await httpContext.GetMultipartFormJsonSection<UploadData>(parameter);
}

public record UploadDataReturn(string name = "", string description = "", string fileText = "");

[SlimEndpoint("/upload/{id:int}", [SlimEndpoints.AOT.HttpMethods.Post], group: "Products")]
public class PostUploadMultipartFormDataNonStandard(IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions) : SlimEndpoint<PostUploadRequest, IResult>
{
    public override void Configure(RouteHandlerBuilder builder)
    {
        builder.DisableAntiforgery();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public override async Task<IResult> HandleAsync(HttpContext httpContext, PostUploadRequest request, CancellationToken cancellationToken)
    {
        UploadData? data = new();
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
        return Results.Ok(new UploadDataReturn(data!.name, data.description, fileText));
    }
}

[SlimEndpoint("/uploadbinding/{id:int}", [SlimEndpoints.AOT.HttpMethods.Post], group: "Products")]
public class PostUploadMultipartFormDataNonStandardBinding1 : 
    SlimEndpoint<PostUploadBindingRequest, IResult>
{
    public override void Configure(RouteHandlerBuilder builder)
    {
        builder.DisableAntiforgery();
    }
    public override IResult Validate(PostUploadBindingRequest request)
    {
        return base.Validate(request);
    }
    public override async Task<IResult> HandleAsync(HttpContext httpContext, PostUploadBindingRequest request, CancellationToken cancellationToken)
    {
        var fileText = "";
        if (request.file != null)
        {
            using var reader = new StreamReader(
                request.file.OpenReadStream(),
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: false);
            fileText = await reader.ReadToEndAsync(cancellationToken);
        }
        return Results.Ok(new UploadDataReturn(request.Datax.name, request.Datax.description, fileText));
    }
}
