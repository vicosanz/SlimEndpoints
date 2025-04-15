using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints.Products.PostSlug;

public record PostSlugRequest(string Slug, SlugData Data);

public record SlugData (string Name);

[SlimEndpoint("/slug/{slug:regex(^[a-z0-9_-]+$)}", [HttpMehotds.Post], group: "Products")]
public class PostSlug : SlimEndpoint<PostSlugRequest, IResult>
{
    public override Task<IResult> HandleAsync(HttpContext httpContext, PostSlugRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Results.Ok());
    }
}
