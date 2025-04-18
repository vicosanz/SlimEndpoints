using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints.Products.PostSlug2
{
    public class PostSlug2Request : Body
    {
        public string Slug { get; set; } 
        public UserNameClaim? UserName { get; set; }
    }

    public class Body  
    {
        [FromBody]
        public int Id { get; set; }
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Default)]
        public string Name { get; set; }
    }

    [SlimEndpoint("/slug2/{slug:regex(^[a-z0-9_-]+$)}", [SlimEndpoints.AOT.HttpMethods.Post], group: "Products")]
    public class PostSlug2 : SlimEndpoint<PostSlug2Request, IResult>
    {
        public override Task<IResult> HandleAsync(HttpContext httpContext, PostSlug2Request request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Results.Ok());
        }
    }
}
