using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints.Products.Upload
{
    public class PostUploadStdRequest
    {
        public int Id { get; set; }
        [FromForm]
        public string Name { get; set; }
        [FromForm]
        public IFormFile? Photo { get; set; }
        [FromForm]
        public string __RequestVerificationToken { get; set; }
        //public IAntiforgery antiforgery { get; set; }
    }

    [SlimEndpoint("/uploadStd/{id:int}", [HttpMehotds.Post], group: "Products")]
    public class PostUploadMultipartFormDataStandard(IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions) : SlimEndpoint<PostUploadStdRequest, IResult>
    {
        public override void Configure(RouteHandlerBuilder builder)
        {
            //builder.DisableAntiforgery();
        }
        public override async Task<IResult> HandleAsync(HttpContext httpContext, PostUploadStdRequest request, CancellationToken cancellationToken)
        {
            return Results.Ok();
        }
    }
}
